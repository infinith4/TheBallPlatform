using System;
using System.IO;
using AaltoGlobalImpact.OIP;

namespace TheBall.Payments
{
    public class GrantPlanAccessToAccountImplementation
    {
        public static GroupSubscriptionPlan GetTarget_GroupSubscriptionPlan(string planName)
        {
            return GroupSubscriptionPlan.RetrieveFromOwnerContent(InformationContext.CurrentOwner, planName);
        }

        public static void ExecuteMethod_GrantAccessToAccountForPlanGroups(string accountId, GroupSubscriptionPlan groupSubscriptionPlan)
        {
            foreach (var groupID in groupSubscriptionPlan.GroupIDs)
            {
                GrantPaidAccessToGroup.Execute(new GrantPaidAccessToGroupParameters
                {
                    AccountID = accountId,
                    GroupID = groupID
                });
            }
        }
    }
}