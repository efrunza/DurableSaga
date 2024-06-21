
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TestDistributedTransactions.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.ServiceBus;
using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Linq;

namespace TestDistributedTransactions
{
    public class Orchestrator
    {
        private readonly bool _bRunParallel = false;

        public static ServiceBusMessageActions MessageActions { get; private set; }

        // keeps a dictionary of all transactions errors per each message
        public static ConcurrentDictionary<string, string> DistributedTransactionErrors = new ConcurrentDictionary<string, string>();

        // Triggered by the messages received in the Service Bus queue

        [FunctionName("QueueStart")]
        public async Task Run(
        [ServiceBusTrigger("testdistributedqueue", Connection = "ServiceBusConnectionString")]        
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            // will be used to send a message to the dead letter queue
            MessageActions = messageActions;

            string bodyMessage = Encoding.UTF8.GetString(message.Body);
            var inputMessage = JsonConvert.DeserializeObject<TestMessage>(bodyMessage);

            // starts the SagaOrchestrator that can be called directly via Http trigger as well.
            await starter.StartNewAsync("SagaOrchestrator1", null, input: bodyMessage);

            await ProcessMessage(message, inputMessage);

        }

        // Called directly from the SagaClient via Http or via the QueueStart from above

        [FunctionName("SagaOrchestrator1")]
        public async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            StringBuilder stringBuilder = new StringBuilder();

            var message = context.GetInput<string>();

            LogInputMessage(message);

            // create the list of the activities that executes transactions 
            Tuple<string, int>[] transactionOrchestrations = 
            {
                new Tuple<string, int> ("Transaction1Orchestrator", 1 ),
                new Tuple<string, int> ("Transaction2Orchestrator", 2 ),
                new Tuple<string, int> ("Transaction3Orchestrator", 3 )
            };

            var hasTransactionsFailed = false;

            if (!_bRunParallel)
            {
                //Execute activities / transactions in sequence
                hasTransactionsFailed = await ExecuteActivitiesInSequence(context, transactionOrchestrations);
            }
            else
            {
                //Execute activities / transactions in parallel
                hasTransactionsFailed = await ExecuteActivitiesInParallel(context, transactionOrchestrations);

            }

            if (hasTransactionsFailed)
            {
                await UpdateErrorsIntiDictionary(context, stringBuilder, message, transactionOrchestrations);
            }
            else
            {
                LogSuccessMessage(context, transactionOrchestrations);
            }

            return stringBuilder.ToString();
        }

        public static async Task<bool> ExecuteActivitiesInSequence(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations)
        {
            bool hasFailed = false;

            var orderedTransactions = transactionOrchestrations.OrderByDescending(x => x.Item2);

            // call orchestrations in sequence based on the order of the transactions descending
            foreach (var orchestration in orderedTransactions)
            {
                string message = context.GetInput<string>();

                var inputMessage = JsonConvert.DeserializeObject<TestMessage>(message);

                var result = await context.CallSubOrchestratorAsync<TransactionState>(orchestration.Item1, null, message);
                LogHelpers.LogTransactionState(orchestration.Item1, result);
                if (!result.IsTransactionSuccessful)
                {
                    hasFailed = true;
                    LogHelpers.LogCancellingTransaction(orchestration.Item1);
                    foreach (var transactionToCancel in orderedTransactions)
                    {
                        if (transactionToCancel.Item1 != orchestration.Item1)
                        {
                            await context.CallSubOrchestratorAsync<TransactionState>(transactionToCancel.Item1 + "Cancellation", null, message);
                        }
                    }
                    break;
                }
            }

            return hasFailed;
        }     

        public static async Task<bool> ExecuteActivitiesInParallel(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations)
        {
            var tasks = new Task<TransactionState>[transactionOrchestrations.Count()];

            // execute activities in parallel
            await OrchestratorHelpers.ExecuteActivitiesAndWait(context, transactionOrchestrations, tasks);

            //if any of the transactions failed, cancel the other transactions
            var hasFailed = await OrchestratorHelpers.CancelActivities(context, transactionOrchestrations, tasks);

            return hasFailed;
        }       

        private static async Task ProcessMessage(ServiceBusReceivedMessage message, TestMessage inputMessage)
        {
            var errorMessage = String.Empty;

            if (DistributedTransactionErrors.ContainsKey(inputMessage.MessageID))
            {
                errorMessage = DistributedTransactionErrors[inputMessage.MessageID].ToString();
            }

            // if there are errors during rollback, send the message to the dead letter queue
            // when the delivery count has reached the maximum value

            if (!String.IsNullOrEmpty(errorMessage))
            {                
                if (message.DeliveryCount < 10)
                {
                    throw new Exception(errorMessage);
                }
                else if (message.DeliveryCount == 10)
                {
                    await OrchestratorHelpers.SendMessageToDeadLetterQueue(message, errorMessage);
                }
            }
            else
            {
                await MessageActions.CompleteMessageAsync(message);

                // remove the message from the dictionary                
                DistributedTransactionErrors.TryRemove(inputMessage.MessageID, out string _);
            }
        }

        private static async Task UpdateErrorsIntiDictionary(IDurableOrchestrationContext context, StringBuilder stringBuilder, string message, Tuple<string, int>[] transactionOrchestrations)
        {
            var states = await GetTransactionsStates(context, transactionOrchestrations);

            foreach (var state in states)
            {
                if (!state.IsTransactionSuccessful)
                {
                    stringBuilder.Append(String.Format("Failed to perform transaction {0}.", state.ActivityName));
                }
            }

            var inputMessage = JsonConvert.DeserializeObject<TestMessage>(message);
            DistributedTransactionErrors.AddOrUpdate(inputMessage.MessageID, stringBuilder.ToString(), (k, o) => stringBuilder.ToString());
        }
        private static async Task<List<TransactionState>> GetTransactionsStates(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations)
        {
            List<TransactionState> states = new List<TransactionState>();

            string message = context.GetInput<string>();

            var orderedTransactions = transactionOrchestrations.OrderByDescending(x => x.Item2);

            foreach (var orchestration in orderedTransactions)
            {
                // call orchestrations in sequence
                var result1 = await context.CallSubOrchestratorAsync<TransactionState>(orchestration.Item1, null, message);
                states.Add(result1);

                var result2 = await context.CallSubOrchestratorAsync<TransactionState>(orchestration.Item1 + "Cancellation", null, message);
                states.Add(result2);
            }
            return states;
        }

        private static void LogSuccessMessage(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations)
        {
            string message = context.GetInput<string>();

            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            var inputMessage = JsonConvert.DeserializeObject<TestMessage>(message);
            LogHelpers.LogDistributedTransactionsSuccessMessage(inputMessage, transactionOrchestrations);
        }

        private static void LogInputMessage(String message)
        {

            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            var inputMessage = JsonConvert.DeserializeObject<TestMessage>(message);
            LogHelpers.LogInputMessage(inputMessage);
        }      

    }
}