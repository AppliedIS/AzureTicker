using AzureTicker.Worker.Model.Repositories;
using AzureTicker.Worker.Model.TableStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;

namespace AzureTicker.NotificationWorker.Controllers
{
    public class NotificationController: ApiController
    {

        [HttpGet]
        public string GetExistingUsername(string rowKey, string notificationUri)
        {
            string retVal = string.Empty;
            using (INotificationRepository rep = new NotificationRepository())
            {
                Notification notification = rep.GetByRowKey(rowKey);
                if (notification != null)
                {
                    retVal = notification.UserName;
                    notification.NotificationUri = notificationUri;
                    notification.LastVerification = DateTime.UtcNow;
                    rep.Update(notification);
                    rep.SaveChanges();
                }
            }
            return retVal;
        }

        [HttpPost]
        public bool RemoveExistingAccount(string rowKey)
        {
            bool retVal = false;
            using (INotificationRepository rep = new NotificationRepository())
            {
                retVal = rep.Delete(rowKey);
                rep.SaveChanges();
            }
            return retVal;
        }

        [HttpPost]
        public string Post(Notification notification)
        {
            string retVal = string.Empty;
            notification.PartitionKey = "partitionName";
            using (INotificationRepository rep = new NotificationRepository())
            {
                rep.AddOrUpdate(notification);
                rep.SaveChanges();
                retVal = notification.RowKey;
            }
            return retVal;
        }

    }
}
