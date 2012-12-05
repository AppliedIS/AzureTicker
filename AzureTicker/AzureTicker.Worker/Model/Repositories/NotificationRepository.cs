using AzureTicker.Worker.Model.TableStorage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TickerEncryption;

namespace AzureTicker.Worker.Model.Repositories
{
    class NotificationRepository : INotificationRepository, IDisposable
    {
        CloudStorageAccount storageAccount = null;
        CloudTableClient tableClient = null;
        TableServiceContext context = null;

        private const string tableName = "tableName";

        public NotificationRepository()
        {
            // Retrieve storage account from connection-string
            storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting(Constants.DataStorageConnectionStringKey));

            // Create the table client
            tableClient = storageAccount.CreateCloudTableClient();
            tableClient.CreateTableIfNotExist(tableName);
            
            // Get the data service context
            context = tableClient.GetDataServiceContext();

        }


        public string AddOrUpdate(Notification notification)
        {
            string key = string.Empty;
            var existingNotification = GetByRowKey(notification.RowKey);
            if (existingNotification == null)
            {
                key = Guid.NewGuid().ToString();
                notification.RowKey = key;
                notification.Password = TickerEncryption.Utility.Encrypt(notification.Password, CloudConfigurationManager.GetSetting(Constants.Thumbprint1), CloudConfigurationManager.GetSetting(Constants.Thumbprint2));
                notification.BalanceString = "New";
                notification.LastVerification = DateTime.UtcNow;
                context.AddObject(tableName, notification);    
            }
            else
            {
                existingNotification.UserName = notification.UserName;
                existingNotification.Password = TickerEncryption.Utility.Encrypt(notification.Password, CloudConfigurationManager.GetSetting(Constants.Thumbprint1), CloudConfigurationManager.GetSetting(Constants.Thumbprint2));
                existingNotification.NotificationUri = notification.NotificationUri;
                notification.LastVerification = DateTime.UtcNow;
                key = existingNotification.RowKey;
                context.UpdateObject(existingNotification);
            }
            return key;
        }

        public void Update(Notification notification)
        {
            context.UpdateObject(notification);
        }


        public bool Delete(string rowKey)
        {
            var existingNotification = GetByRowKey(rowKey);
            if (existingNotification != null)
            {
                context.DeleteObject(existingNotification);
                return true;
            }
            else
                return false;
        }

        public void Clear()
        {
            var entities = List();
            foreach (var entity in entities)
            {
                Delete(entity.RowKey);
            }
        }

        public Notification GetByRowKey(string rowKey)
        {
            if (!string.IsNullOrEmpty(rowKey))
            {
                return
                    (from n in context.CreateQuery<Notification>(tableName) select n).AsTableServiceQuery<Notification>().Where(n => n.RowKey == rowKey).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Notification> List()
        {
            CloudTableQuery<Notification> partitionQuery =
                (from n in context.CreateQuery<Notification>(tableName)
                select n).AsTableServiceQuery<Notification>();

            return partitionQuery.AsEnumerable();
        }

        public void SaveChanges()
        {
            context.SaveChangesWithRetries();
        }

        public void Dispose()
        {
            if (context != null)
            {
                context = null;
            }
        }

        public void UpdateBalance(Notification notification, string newBalance)
        {
            notification.LastUpdate = DateTime.UtcNow;
            notification.BalanceString = newBalance;
            context.UpdateObject(notification);
        }

        public IEnumerable<Notification> GetNotificationsToUpdate()
        {
            DateTime nw = DateTime.UtcNow;
            return (from n in context.CreateQuery<Notification>(tableName) select n).AsTableServiceQuery<Notification>().Where(n => (n.LastUpdate < nw.AddDays(-1) || n.BalanceString == "New")).AsEnumerable();
        }

        public IEnumerable<Notification> GetNotificationsToSend()
        {
            DateTime nw = DateTime.UtcNow;
            return (from n in context.CreateQuery<Notification>(tableName) select n).AsTableServiceQuery<Notification>().Where(n => (n.BalanceString != "New")).AsEnumerable();
        }



    }
}
