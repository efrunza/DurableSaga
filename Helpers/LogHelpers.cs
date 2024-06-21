using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using TestDistributedTransactions.Models;

public static class LogHelpers
{
    private static int counter;
    private static readonly object locker = new object();

    public static void LogInputMessage(TestMessage message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        var logMessage = String.Format("Message received has ID: {0} and content: {1}", message.MessageID, message.Message);
        LogTransactionCommon(logMessage);
    }

    public static void LogTransactionState(string transaction, TransactionState result)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        var logMessage = String.Format("Executed {0}. Transaction: {1}", transaction, result.IsTransactionSuccessful);
        LogTransactionCommon(logMessage);
    }

    public static void LogCancellingTransaction(string transaction)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        var logMessage = String.Format("Transaction {0} failed. Cancelling previously executed transactions.", transaction);
        LogTransactionCommon(logMessage);
    }

    public static void LogFailedTransaction(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        LogTransactionCommon(message);
    }

    public static void LogRollbackTransaction(string message)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        LogTransactionCommon(message);
    }
    public static void LogException(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        LogTransactionCommon(ex.Message);
    }

    public static void IncrementCounter()
    {
        lock (locker)
        {
            counter++;
        }
    }

    public static int GetCounter()
    {
        lock (locker)
        {
            return counter;
        }
    }
    public static void LogTransactionCommon(string message)
    {        
        IncrementCounter();
        var counter = GetCounter();

        if (counter % 2 != 0)
        {
            Console.WriteLine("===============================");
        }
      
        Console.WriteLine(" ");
        //Console.WriteLine(String.Format("Log Count: {0}", GetCounter().ToString()));
        Console.WriteLine(" ");
        Console.WriteLine(message);

        if (counter % 2 == 0)
        {
            Console.WriteLine(" ");
        }
        else
        {
            Console.WriteLine(" ");
            Console.WriteLine("===============================");
        }
    }

    public static void LogDistributedTransactionsState(List<TransactionState> states)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("============================================================================================================================");
        Console.WriteLine(" ");
        Console.WriteLine("Final state for all transaction is displayed below: ");
        Console.WriteLine(" ");

        foreach (var state in states)
        {
            Console.WriteLine("Transaction Name : " + state.ActivityName + " - Commit state is: " + (state.IsTransactionSuccessful ? "Success" : "Failure") + " ");
        }

        Console.WriteLine("============================================================================================================================");
        Console.WriteLine(" ");
    }
    public static void LogDistributedTransactionsSuccessMessage(TestMessage message, Tuple<string, int>[] transactionOrchestrations)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("============================================================================================================================");
        Console.WriteLine(" ");
        Console.WriteLine("Final state for all transaction is displayed below: ");
        Console.WriteLine(" ");

        lock(transactionOrchestrations)
        {
            foreach (var orchestration in transactionOrchestrations)
            {
                Console.WriteLine("Transaction Name : {0} has been completed successfully for the message {1}.", orchestration.Item1, message.Message);
            }
        }
      
        Console.WriteLine("============================================================================================================================");
        Console.WriteLine(" ");
    }


}