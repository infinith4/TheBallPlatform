﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheBall.CORE;
using TheBall.Interface;

namespace TheBall.Infra.TheBallWorkerConsole
{
    public class WorkerSupervisor
    {
        private readonly string ConfigRootFolder;
        private readonly Stream HostPollingStream;
        private readonly WorkerConsoleConfig WorkerConfig;

        private WorkerSupervisor(Stream hostPollingStream, WorkerConsoleConfig workerConfig, string configRootFolder)
        {
            HostPollingStream = hostPollingStream;
            WorkerConfig = workerConfig;
            ConfigRootFolder = configRootFolder;
        }

        internal static async Task<WorkerSupervisor> Create(Stream hostPollingStream, string workerConfigFullPath)
        {
            var workerConfig = await WorkerConsoleConfig.GetConfig(workerConfigFullPath);
            string configRootFolder = Path.GetDirectoryName(workerConfigFullPath);
            return new WorkerSupervisor(hostPollingStream, workerConfig, configRootFolder);
        }

        internal async Task RunWorkerLoop(AnonymousPipeClientStream pipeStream, String workerConfigFullPath)
        {
            var reader = pipeStream != null ? new StreamReader(pipeStream) : null;
            const int MaxParallelExecutingTasks = 3;
            try
            {
                var instances = WorkerConfig.InstancePollItems;

                var instanceNames = WorkerConfig.InstancePollItems.Select(item => item.InstanceHostName).ToArray();

                string startupLogPath = Path.Combine(Program.AssemblyDirectory, "ConsoleStartupLog.txt");
                var startupMessage = "Starting up process (UTC): " + DateTime.UtcNow.ToString() + " for instances: " +
                                     String.Join(", ", instanceNames);
                File.WriteAllText(startupLogPath, startupMessage);

                var pipeMessageAwaitable = reader?.ReadToEndAsync();

                bool keepWorkerRunning = true;
                var currentTasks = instances.Select(getInstancePollingTask).ToArray();
                List<Task> executingOperations = new List<Task>();

                while (keepWorkerRunning)
                {
                    List<Task> awaitList = new List<Task>();
                    if (pipeMessageAwaitable != null)
                        awaitList.Add(pipeMessageAwaitable);
                    awaitList.AddRange(currentTasks.Select(item => item.Item2));
                    awaitList.AddRange(executingOperations);
                    await Task.WhenAny(awaitList);

                    bool isCanceling = pipeMessageAwaitable != null && pipeMessageAwaitable.IsCompleted;
                    if (!isCanceling)
                    {
                        var completedPollingTasks =
                            awaitList.Where(item => item != pipeMessageAwaitable && item.IsCompleted).ToArray();
                        currentTasks = replaceCompletedTasks(currentTasks, completedPollingTasks);
                    }
                    else
                    {
                        foreach (var taskItem in currentTasks)
                            taskItem.Item3.Cancel();

                        await Task.WhenAll(awaitList);
                        var completedPollingTasks =
                            awaitList.Where(item => item != pipeMessageAwaitable && !item.IsCanceled).ToArray();

                        // ReleaseLocked
                        await releaseLocks(
                            completedPollingTasks.Cast<Task<LockInterfaceOperationsByOwnerReturnValue>>()
                                .Select(item => item.Result)
                                .ToArray());

                        // Cancel & allow processing of all lock-obtained and thus completed
                        var pipeMessage = pipeMessageAwaitable.Result;
                        var shutdownLogPath = Path.Combine(Program.AssemblyDirectory, "ConsoleShutdownLog.txt");
                        File.WriteAllText(shutdownLogPath,
                            "Quitting for message (UTC): " + pipeMessage + " " + DateTime.UtcNow.ToString());
                        keepWorkerRunning = false;
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                    pipeStream.Dispose();
                }
            }
        }

        private async Task releaseLocks(LockInterfaceOperationsByOwnerReturnValue[] lockOperationResults)
        {
            var releaseTasks =
                lockOperationResults
                    .Select(lockItem => lockItem.LockBlobFullPath)
                    .Select(blobPath => StorageSupport.GetOwnerBlobReference(SystemOwner.CurrentSystem, blobPath))
                    .Select(blob => blob.DeleteIfExistsAsync()).ToArray();
            await Task.WhenAll(releaseTasks);
        }

        private static Tuple<InstancePollItem, Task, CancellationTokenSource>[] replaceCompletedTasks(
            Tuple<InstancePollItem, Task, CancellationTokenSource>[] currentTasks, Task[] completedPollingTasks)
        {
            var toReplace = currentTasks.Where(item => completedPollingTasks.Contains(item.Item2)).ToArray();
            var toKeep = currentTasks.Where(item => toReplace.Contains(item) == false).ToArray();
            var replacements = toReplace.Select(item =>
            {
                var instancePollItem = item.Item1;
                return getInstancePollingTask(instancePollItem);
            }).ToArray();
            return toKeep.Concat(replacements).ToArray();
        }

        private static Tuple<InstancePollItem, Task, CancellationTokenSource> getInstancePollingTask(InstancePollItem instancePollItem)
        {
            var pollingTask = getPollingTask(instancePollItem);
            return new Tuple<InstancePollItem, Task, CancellationTokenSource>(instancePollItem, pollingTask.Item1, pollingTask.Item2);
        }

        private static Tuple<Task, CancellationTokenSource> getPollingTask(InstancePollItem instancePollItem)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            Task result = Task.Run(async () =>
            {
                InformationContext.InitializeToLogicalContext(SystemOwner.CurrentSystem, instancePollItem.InstanceHostName);
                bool keepRunning = !cancellationToken.IsCancellationRequested;
                while (keepRunning)
                {
                    Console.WriteLine("Polling..." + DateTime.Now.ToLongTimeString());
                    var obtainedLock = await LockInterfaceOperationsByOwner.ExecuteAsync();
                    if (obtainedLock != null)
                    {
                        Console.WriteLine("Found: " + obtainedLock.OperationIDs.Length + " operations...");
                        return obtainedLock;
                    }
                    else
                        await Task.Delay(1000);
                    keepRunning = !cancellationToken.IsCancellationRequested;
                }
                return null;
            }, cancellationTokenSource.Token);
            return new Tuple<Task, CancellationTokenSource>(result, cancellationTokenSource);
        }

    }
}