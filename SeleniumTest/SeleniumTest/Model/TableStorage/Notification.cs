using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeleniumTest.Model.TableStorage
{
    public class Notification: Microsoft.WindowsAzure.StorageClient.TableServiceEntity
    {
        public Notification()
        {
        }

        // Partition Key is the same constant (all in the same partition) which is set server side

        [JsonIgnore]
        public override string PartitionKey
        {
            get
            {
                return base.PartitionKey;
            }
            set
            {
                base.PartitionKey = value;
            }
        }

        // Row Key is the App ID
        public override string RowKey
        {
            get
            {
                return base.RowKey;
            }
            set
            {
                base.RowKey = value;
            }
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public string NotificationUri { get; set; }
        public DateTime ? LastUpdate { get; set; }
        public DateTime ? LastVerification { get; set; }
        public string BalanceString { get; set; }

    }
}
