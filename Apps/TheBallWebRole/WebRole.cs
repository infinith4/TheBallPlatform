using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.ApplicationServices;
using Microsoft.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TheBall.Infra.WebServerManager;

namespace TheBallWebRole
{
    public class WebRole : RoleEntryPoint
    {
        private const string SiteContainerName = "tb-sites";
        //private const string PathTo7Zip = @"d:\bin\7z.exe";
        private const string PathTo7Zip = @"E:\TheBallInfra\7z\7z.exe";

        private CloudStorageAccount StorageAccount;
        private CloudBlobClient BlobClient;
        private CloudBlobContainer SiteContainer;
        private volatile bool IsRunning = false;
        private volatile bool TaskIsDone = true;

        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            var connStr = CloudConfigurationManager.GetSetting("StorageConnectionString");

            StorageAccount = CloudStorageAccount.Parse(connStr);
            BlobClient = StorageAccount.CreateCloudBlobClient();
            SiteContainer = BlobClient.GetContainerReference(SiteContainerName);

            IsRunning = true;
            Task.Factory.StartNew(SyncWebsitesFromStorage);
            
            return base.OnStart();
        }

        public override void OnStop()
        {
            IsRunning = false;
            while(!TaskIsDone)
                Thread.Sleep(200);
            base.OnStop();
        }

        //private static string StorageConnectionString => CloudConfigurationManager.GetSetting("StorageConnectionString");

        private static string TempSitesRootFolder
        {
            get
            {
                var localResource = RoleEnvironment.GetLocalResource("TempSites");
                return localResource.RootPath;
            }
        }

        private static string LiveSitesRootFolder
        {
            get
            {
                var localResource = RoleEnvironment.GetLocalResource("Sites");
                return localResource.RootPath;
            }
        }

        private void SyncWebsitesFromStorage()
        {
            while (IsRunning)
            {
                TaskIsDone = false;
                var blobs = SiteContainer.ListBlobs(null, true, BlobListingDetails.Metadata);
                foreach (CloudBlockBlob blob in blobs)
                {
                    string fileName = blob.Name;
                    string hostAndSiteName = Path.GetFileNameWithoutExtension(fileName);
                    string tempFile = Path.Combine(TempSitesRootFolder, blob.Name);
                    FileInfo currentFile = new FileInfo(tempFile);
                    var blobLastModified = blob.Properties.LastModified.GetValueOrDefault().UtcDateTime;
                    if (!currentFile.Exists || currentFile.LastWriteTimeUtc != blobLastModified)
                    {
                        try
                        {
                            blob.DownloadToFile(tempFile, FileMode.Create);
                            currentFile.Refresh();
                            currentFile.LastWriteTimeUtc = blobLastModified;
                            UpdateIISSite(tempFile, TempSitesRootFolder, hostAndSiteName, LiveSitesRootFolder);
                        }
                        catch
                        {
                            
                        }
                    }
                }
                if(IsRunning)
                    Thread.Sleep(10000);
            }
            TaskIsDone = true;
        }

        private void UpdateIISSite(string tempZipFile, string tempSitesRootFolder, string hostAndSiteName, string liveSitesRootFolder)
        {
            string tempDirName = Path.Combine(tempSitesRootFolder, hostAndSiteName);
            var tempDirectory = new DirectoryInfo(tempDirName);
            if(tempDirectory.Exists)
                tempDirectory.Delete(true);
            tempDirectory.Create();
            var unzipProcInfo = new ProcessStartInfo(PathTo7Zip, String.Format(@"x ..\{0}.zip", hostAndSiteName));
            unzipProcInfo.WorkingDirectory = tempDirName;
            var unzipProc = Process.Start(unzipProcInfo);
            unzipProc.WaitForExit();

            string fullLivePath = Path.Combine(liveSitesRootFolder, hostAndSiteName);
            IISSupport.UpdateSite(fullLivePath, hostAndSiteName, () =>
            {
                string sourceFolder = Path.Combine(tempSitesRootFolder, hostAndSiteName);
                string targetFolder = fullLivePath;
                ProcessStartInfo startInfo = new ProcessStartInfo(@"d:\windows\system32\robocopy.exe", String.Format(@"/MIR ""{0}"" ""{1}""", sourceFolder,
                    targetFolder));
                startInfo.UseShellExecute = true;
                var proc = Process.Start(startInfo);
                proc.WaitForExit();
            });
        }
    }
}
