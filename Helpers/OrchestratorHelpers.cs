
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using TestDistributedTransactions;
using TestDistributedTransactions.Models;

public static class OrchestratorHelpers
{
    public static async Task<bool> CommonOrchestrator(IDurableOrchestrationContext context, string activityName)
    {
        var message = context.GetInput<string>();

        var transactionResult = await context.CallActivityAsync<bool>(activityName, message);

        return transactionResult;
    }
    public static async Task ExecuteActivitiesAndWait(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations, Task<TransactionState>[] tasks)
    {
        int i = 0;

        foreach (var orchestration in transactionOrchestrations)
        {
            // call orchestrations in parallel

            string message = context.GetInput<string>();

            tasks[i] = context.CallSubOrchestratorAsync<TransactionState>(orchestration.Item1, message);

            i++;
        }
        await Task.WhenAll(tasks);
    }   

    public static async Task<bool> CancelActivities(IDurableOrchestrationContext context, Tuple<string, int>[] transactionOrchestrations, Task<TransactionState>[] tasks)
    {
        bool hasFailed = false;
        int i = 0;

        foreach (var orchestration in transactionOrchestrations)
        {
            var transactionResult = tasks[i].Result.IsTransactionSuccessful;

            // if the transaction failed, cancel the other transactions
            if (transactionResult)
            {
                hasFailed = true;

                string message = context.GetInput<string>();

                await context.CallSubOrchestratorAsync<TransactionState>(orchestration + "Cancellation", null, message);
            }

            i++;
        }

        return hasFailed;
    }    
    
    public static bool IsMessageSetToFail(string message, int failInterval)
    {
        bool hasFailed = false;

        TestMessage testMessage = JsonConvert.DeserializeObject<TestMessage>(message);

        if (testMessage != null)
        {
            Int32.TryParse(testMessage.Message, out int result);
            if (result % failInterval == 0)
            {
                hasFailed = true;
            }
        }

        return hasFailed;
    }    

    public static async Task SendMessageToDeadLetterQueue(ServiceBusReceivedMessage message, string messageContent)
    {
        try
        {
            StringBuilder stringBuiler = new StringBuilder();                                 

            await Orchestrator.MessageActions.DeadLetterMessageAsync(message,
            new Dictionary<string, object>
            {
                    {"DeadLetterReason", "TransactionError"},
                    {"DeadLetterErrorDescription", messageContent}
            });
        }
        catch (Exception ex)
        {
            LogHelpers.LogException(ex);
        }
    }
}