using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TestDistributedTransactions.Utils;
using TestDistributedTransactions.Models;
using System.Collections.Concurrent;
using Microsoft.Azure.ServiceBus;

namespace TestDistributedTransactions.Activities
{
    public class Transaction3
    {
        public ConcurrentDictionary<string, TransactionState> activityStates = new ConcurrentDictionary<string, TransactionState>();

        [FunctionName("Transaction3Orchestrator")]
        public async Task<TransactionState> ExecuteTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activityName = "ExecuteTransaction3";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };

            return activityStates.AddOrUpdate(transactionState.ActivityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("Transaction3OrchestratorCancellation")]
        public async Task<TransactionState> CancelTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activityName = "CancelTransaction3";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };
            
            return activityStates.AddOrUpdate(activityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("ExecuteTransaction3")]
        public static bool ExecuteTransaction3([ActivityTrigger] string message, ILogger log)
        {
            bool hasFailed = false;

            if (!String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 7);
            }

            if (hasFailed)            
            {
                LogHelpers.LogFailedTransaction("Failure in Transaction3");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.UpdateCount3");              
            }

            return true;
        }

        [FunctionName("CancelTransaction3")]
        public static bool CancelTransaction3([ActivityTrigger] string message, ILogger log)
        {
            bool hasFailed = false;

            if (String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 7);
            }

            if (hasFailed)
            {
                LogHelpers.LogFailedTransaction("Failure in CancelTransaction3");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.RollbackCount3");
            }

            return true;
        }      
    }
}
