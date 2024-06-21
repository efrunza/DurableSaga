using TestDistributedTransactions.Activities;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestDistributedTransactions.Models
{
    public class TransactionState
    {
        public string ID { get; set; }

        public string ActivityName { get; set; }

        public bool IsTransactionSuccessful { get; set; }

    }
}
