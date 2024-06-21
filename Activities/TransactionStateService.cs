using System;
using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using TestDistributedTransactions.Models;

namespace TestDistributedTransactions.Activities
{
    public static class TransactionStateService
    {
        public static ConcurrentDictionary<string, TransactionState> Transactions = new ConcurrentDictionary<string, TransactionState>();

        [FunctionName("GetTransactionState")]
        public static TransactionState GetTransactionState([ActivityTrigger] string activityName, ILogger log)
        {
            // return a Transaction object from storage
            if (Transactions.ContainsKey(activityName))
            {
                return Transactions[activityName];
            }

            return new TransactionState { ActivityName = activityName };
        }

        [FunctionName("SaveTransactionState")]
        public static void SaveTransactionState([ActivityTrigger] TransactionState transactionState, ILogger log)
        {
            Transactions.AddOrUpdate(transactionState.ActivityName, transactionState, (k, o) => transactionState);
        }

        public static void LogDistributedTransactionsState()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================================================================================================");
            Console.WriteLine(" ");
            Console.WriteLine("Final state for all transaction is displayed below: ");
            Console.WriteLine(" ");

            foreach (var transaction in Transactions)
            {
                Console.WriteLine("Transaction Name : " + transaction.Value.ActivityName + " - Commit state is: " + (transaction.Value.IsTransactionSuccessful ? "Success" : "Failure") + " ");
            }

            Console.WriteLine("============================================================================================================================");
            Console.WriteLine(" ");

            Transactions.Clear();
        }
    }
}
