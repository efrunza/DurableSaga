
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TestDistributedTransactions.Utils;
using TestDistributedTransactions.Models;
using System.Collections.Concurrent;

namespace TestDistributedTransactions.Activities
{
    public class Transaction2
    {
        public ConcurrentDictionary<string, TransactionState> activityStates = new ConcurrentDictionary<string, TransactionState>();

        [FunctionName("Transaction2Orchestrator")]
        public async Task<TransactionState> ExecuteTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activityName = "ExecuteTransaction2";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };

            return activityStates.AddOrUpdate(transactionState.ActivityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("Transaction2OrchestratorCancellation")]
        public async Task<TransactionState> CancelTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activityName = "CancelTransaction2";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };

            return activityStates.AddOrUpdate(activityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("ExecuteTransaction2")]
        public static bool ExecuteTransaction2([ActivityTrigger] string message, ILogger log)
        {
            bool hasFailed = false;

            if (!String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 5);
            }

            if (hasFailed)
            {
                LogHelpers.LogFailedTransaction("Failure in Transaction2");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.UpdateCount2");
            }
                        
            return true;
        }

        [FunctionName("CancelTransaction2")]
        public static bool CancelTransaction2([ActivityTrigger] string message, ILogger log)
        {
            LogHelpers.LogRollbackTransaction("Rollback transaction 2");

            bool hasFailed = false;

            if (!String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 7);
            }

            if (hasFailed)
            {
                LogHelpers.LogFailedTransaction("Failure in CancelTransaction2");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.RollbackCount2");
            }

            return true;
        }
        
    }
}
