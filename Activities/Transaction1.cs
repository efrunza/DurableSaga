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
    public class Transaction1
    {
        public ConcurrentDictionary<string, TransactionState> activityStates = new ConcurrentDictionary<string, TransactionState>();

        [FunctionName("Transaction1Orchestrator")]
        public async Task<TransactionState> ExecuteTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string message = context.GetInput<string>();

            var activityName = "ExecuteTransaction1";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };

            return activityStates.AddOrUpdate(transactionState.ActivityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("Transaction1OrchestratorCancellation")]
        public async Task<TransactionState>CancelTransaction(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activityName = "CancelTransaction1";

            var result = await OrchestratorHelpers.CommonOrchestrator(context, activityName);

            var transactionState = new TransactionState { ID = context.InstanceId, ActivityName = activityName, IsTransactionSuccessful = result };

            return activityStates.AddOrUpdate(activityName, transactionState, (k, o) => transactionState);
        }

        [FunctionName("ExecuteTransaction1")]
        public bool ExecuteTransaction1([ActivityTrigger] string message, ILogger log)
        {

            bool hasFailed = false;
            
            if(!String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 3);
            }          

            if (hasFailed)
            {
                LogHelpers.LogFailedTransaction("Failure in Transaction1");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.UpdateCount1");
            }

            return true;
        }      

        [FunctionName("CancelTransaction1")]
        public bool CancelTransaction1([ActivityTrigger] string message, ILogger log)
        {           
            LogHelpers.LogRollbackTransaction("Rollback transaction 1");

            bool hasFailed = false;

            if (!String.IsNullOrEmpty(message))
            {
                hasFailed = OrchestratorHelpers.IsMessageSetToFail(message, 7);
            }

            if (hasFailed)
            {
                LogHelpers.LogFailedTransaction("Failure in CancelTransaction1");
                return false;
            }
            else
            {
                DAL.ExecuteUpdateSP("dbo.RollbackCount1");
            }

            return true;
        }
       
    }
}
