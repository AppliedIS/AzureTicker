using SeleniumTest.Model.TableStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace SeleniumTest
{
    class Notifier
    {

        private const string _secret = "";
        private const string _package_sid = "";

        public static void SendBillNotifications()
        {
            using (INotificationRepository rep = new NotificationRepository())
            {
                var notifications = rep.GetNotificationsToSend();
                foreach (var n in notifications)
                {
                    GetBillAndSendNotification(n);
                }
            }            
        }

        public static void UpdateBillNotifications()
        {
            using (INotificationRepository rep = new NotificationRepository())
            {
                var notifications = rep.GetNotificationsToUpdate();
                //var notifications = rep.List();
                foreach (var n in notifications)
                {
                    if (n.LastVerification > DateTime.UtcNow.AddDays(-30))
                    {
                        string balanceString = BillProvider.GetBillAmount(n.UserName, n.Password);
                        rep.UpdateBalance(n, balanceString);
                        rep.SaveChanges();
                    }
                    else
                    {
                        rep.Delete(n.RowKey);
                        rep.SaveChanges();
                    }
                }
            }
        }


        private static void GetBillAndSendNotification(Notification notification)
        {            
            string amount = notification.BalanceString;
            TimeSpan span = DateTime.UtcNow.Subtract((DateTime)notification.LastVerification);            
            int days = span.Days;
            string verificationSummary1 = string.Empty;
            string verificationSummary2 = string.Empty;
            string verificationSummary3 = string.Empty;
            if (days > 24)
            {
                verificationSummary1 = "Action Required:";
                verificationSummary2 = "Last verified " + days; 
                verificationSummary3 = "days ago.";                                
            }

            string tileData = string.Format("<tile><visual><binding template=\"TileSquareText01\"><text id=\"1\">{0}</text><text id=\"2\">{1}</text><text id=\"3\">{2}</text><text id=\"4\">{3}</text></binding></visual></tile>", amount, verificationSummary1, verificationSummary2, verificationSummary3);

            // Post the value to the notification url
            PostToWns(_secret, _package_sid, notification.NotificationUri, tileData, "wns/tile");
        }

        //// Post to WNS
        private static string PostToWns(string secret, string sid, string uri, string xml, string type = "wns/tile")
        {
            try
            {
                // You should cache this access token
                var accessToken = GetAccessToken(secret, sid);

                byte[] contentInBytes = Encoding.UTF8.GetBytes(xml);

                HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
                request.Method = "POST";
                request.Headers.Add("X-WNS-Type", type);
                request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken.AccessToken));

                using (Stream requestStream = request.GetRequestStream())
                    requestStream.Write(contentInBytes, 0, contentInBytes.Length);

                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                    return webResponse.StatusCode.ToString();
            }
            catch (WebException webException)
            {
                return "EXCEPTION: " + webException.Message;
            }
            catch (Exception ex)
            {
                return "EXCEPTION: " + ex.Message;
            }
        }

        public class Channel
        {
            public int Id { get; set; }


            [DataMember(Name = "uri")]
            public string Uri { get; set; }
        }

        [DataContract]
        public class OAuthToken
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
        }

        private static OAuthToken GetOAuthTokenFromJson(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(OAuthToken));
                var oAuthToken = (OAuthToken)ser.ReadObject(ms);
                return oAuthToken;
            }
        }

        protected static OAuthToken GetAccessToken(string secret, string sid)
        {
            var urlEncodedSecret = HttpUtility.UrlEncode(secret);
            var urlEncodedSid = HttpUtility.UrlEncode(sid);

            var body =
              String.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com", urlEncodedSid, urlEncodedSecret);

            string response;
            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                response = client.UploadString("https://login.live.com/accesstoken.srf", body);
            }
            return GetOAuthTokenFromJson(response);
        }
    }


}
