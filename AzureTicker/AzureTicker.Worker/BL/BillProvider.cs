using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.WindowsAzure;

namespace AzureTicker.Worker.BL
{
    public static class BillProvider
    {
        const string loginUrl = "https://login.live.com/login.srf?cbcxt=azu&vv=910&lc=1033&wa=wsignin1.0&wtrealm=urn:federation:MicrosoftOnline&wctx=wa%3Dwsignin1.0%26rpsnv%3D2%26ct%3D1347293063%26rver%3D6.1.6206.0%26wp%3DSAPI%26wreply%3Dhttps:%252F%252Faccount.windowsazure.com%252FSubscriptions%26lc%3D1033%26id%3D500867%26cbcxt%3Dazu";

        private static string GetAccessToken(string userName, string password)
        {
            string retVal = null;

            CookieContainer cookies = new CookieContainer();

            string HTML = null;
            string postUrl = null;
            string nextRequestBody = null;


            // Build and make the initial get request
            HttpWebRequest httpRequest = BuildInitialSiteRequest();
            using (HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse())
            {
                ExtractResponseCookiesToCookieContainer(ref cookies, response, 11, "http://login.live.com");
                HTML = GetHTMLFromResponse(response);
                nextRequestBody = BuildTokenPostFromInitialSiteRequest(HTML, userName, password, out postUrl);
            }


            //POST to the specified URI with the appropraite cookies 
            HttpWebRequest httpRequest2 = BuildPostRequest(ref cookies, postUrl, nextRequestBody);
            // Get the response
            using (HttpWebResponse response2 = (HttpWebResponse)httpRequest2.GetResponse())
            {
                // Get the html
                HTML = GetHTMLFromResponse(response2);
                ExtractResponseCookiesToCookieContainer(ref cookies, response2, 8, "http://login.live.com");
                nextRequestBody = BuildPostBodyFromSecondRequest(HTML);
            }

            //POST back to the initial uri with the post body from the 2nd request 
            HttpWebRequest httpRequest3 = BuildThirdRequest(cookies, httpRequest, nextRequestBody);
            using (HttpWebResponse response3 = (HttpWebResponse)httpRequest3.GetResponse())
            {
                HTML = GetHTMLFromResponse(response3, true);
                ExtractResponseCookiesToCookieContainer(ref cookies, response3, 12, "http://login.live.com");
            }
            return retVal;
        }

        private static HttpWebRequest BuildThirdRequest(CookieContainer cookies, HttpWebRequest httpRequest, string requestBody)
        {
            string POSTANON = "https://login.microsoftonline.com/login.srf";

            //POST the Info and Accept the Bungie Cookies
            httpRequest = (HttpWebRequest)HttpWebRequest.Create(POSTANON);

            httpRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1.8) Gecko/20100202 Firefox/3.5.8 (.NET CLR 3.5.30729)";
            httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            httpRequest.Headers.Add("Accept-Language", "en-us,en;q=0.5");
            httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            httpRequest.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            httpRequest.Headers.Add("Keep-Alive", "115");
            httpRequest.Referer = loginUrl;
            httpRequest.CookieContainer = cookies;
            httpRequest.ContentType = "application/x-www-form-urlencoded";
            httpRequest.Method = WebRequestMethods.Http.Post;

            httpRequest.Expect = null;

            using (Stream ostream = httpRequest.GetRequestStream())
            {
                byte[] buffer = ASCIIEncoding.ASCII.GetBytes(requestBody);
                ostream.Write(buffer, 0, buffer.Length);
                ostream.Close();
            }
            return httpRequest;
        }

        private static string BuildPostBodyFromSecondRequest(string HTML)
        {
            string formBody = null;

            int startIndex = HTML.IndexOf("<form");
            int endIndex = HTML.LastIndexOf("</form>");
            formBody = HTML.Substring(startIndex, endIndex - startIndex + 7);

            string[] tokens = formBody.Split(new string[] { "<input" }, StringSplitOptions.None);


            string requestBody = string.Empty;

            for (int i = 1; i < tokens.Length; i++)
            {
                var currToken = tokens[i].TrimEnd('>');
                string id = ExtractAttributeValue(currToken, "id");
                string val = ExtractAttributeValue(currToken, "value");

                requestBody += id + "=" + HttpUtility.UrlEncode(val) + "&";
            }
            requestBody = requestBody.TrimEnd('&');

            return requestBody;
        }

        private static HttpWebRequest BuildPostRequest(ref CookieContainer cookies, string postUrl, string nextRequestBody)
        {

            HttpWebRequest retVal = (HttpWebRequest)HttpWebRequest.Create(postUrl);

            retVal.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1.8) Gecko/20100202 Firefox/3.5.8 (.NET CLR 3.5.30729)";
            retVal.Headers.Add("Accept-Language", "en-us,en;q=0.5");
            retVal.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            retVal.Headers.Add("Keep-Alive", "300");
            retVal.CookieContainer = cookies;
            retVal.Referer = loginUrl;
            retVal.ContentType = "application/x-www-form-urlencoded";
            retVal.Method = WebRequestMethods.Http.Post;

            using (Stream ostream = retVal.GetRequestStream())
            {
                //used to convert strings into bytes
                byte[] buffer = ASCIIEncoding.ASCII.GetBytes(nextRequestBody);
                ostream.Write(buffer, 0, buffer.Length);
                ostream.Close();
            }

            return retVal;
        }

        private static string BuildTokenPostFromInitialSiteRequest(string HTML, string userName, string password, out string postUrl)
        {
            ////Get the PPSX value
            string PPSX = "PassportR";

            //Get this random PPFT value
            string PPFT = HTML.Remove(0, HTML.IndexOf("PPFT"));
            PPFT = PPFT.Remove(0, PPFT.IndexOf("value") + 7);
            PPFT = PPFT.Substring(0, PPFT.IndexOf("\""));

            string scriptBody = null;
            var scriptMatch = Regex.Match(HTML, "<script type=\"text/javascript\">var ServerData = {(.)*};</script>");
            if (scriptMatch.Success)
            {
                scriptBody = scriptMatch.Value;
            }

            int firstBracket = scriptBody.IndexOf('{');
            int lastBracket = scriptBody.LastIndexOf('}');
            scriptBody = scriptBody.Substring(firstBracket, lastBracket - firstBracket + 1);

            var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            dynamic serverData = jsonSerializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new StringReader(scriptBody)));

            postUrl = serverData.urlPost;

            string requestToken = string.Format("login={0}&passwd={1}&PPSX={2}&LoginOptions=2&PPFT={3}", userName, password, PPSX, PPFT);
            return requestToken;
        }

        private static void ExtractResponseCookiesToCookieContainer(ref CookieContainer cookies, HttpWebResponse response, int cookieHeaderIndex, string cookieDomain)
        {
            //gets the cookies (they are set in the eleventh header)
            string[] strCookies = response.Headers.GetValues(cookieHeaderIndex);

            string name, value;
            System.Net.Cookie manualCookie;
            for (int i = 0; i < strCookies.Length; i++)
            {
                name = strCookies[i].Substring(0, strCookies[i].IndexOf("="));
                value = strCookies[i].Substring(strCookies[i].IndexOf("=") + 1, strCookies[i].IndexOf(";") - strCookies[i].IndexOf("=") - 1);
                manualCookie = new System.Net.Cookie(name, "\"" + value + "\"");

                Uri manualURL = new Uri(cookieDomain);
                cookies.Add(manualURL, manualCookie);
            }

        }


        private static string GetHTMLFromResponse(HttpWebResponse response, bool isGZipped = false)
        {
            string retVal = null;
            if (isGZipped)
            {
                using (GZipStream gStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(gStream))
                    {
                        retVal = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    retVal = reader.ReadToEnd();
                }

            }
            return retVal;
        }

        private static HttpWebRequest BuildInitialSiteRequest()
        {
            Uri url = new Uri(loginUrl);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Timeout = 30000;
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1.8) Gecko/20100202 Firefox/3.5.8 (.NET CLR 3.5.30729)";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
            request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            request.Headers.Add("Keep-Alive", "300");
            request.Referer = "http://login.microsoftonline.com/";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            request.Method = WebRequestMethods.Http.Get;
            return request;
        }

        private static string ExtractAttributeValue(string currToken, string attribKey)
        {
            int swr = currToken.IndexOf(attribKey + "=\"") + attribKey.Length + 2;
            int ewr = currToken.IndexOf("\"", swr);

            string val = currToken.Substring(swr, ewr - swr);
            return val;
        }

        private static string GetPPFTValue(string sftInputTag)
        {
            var doc = System.Xml.Linq.XDocument.Parse(sftInputTag);
            return doc.Root.Attribute("value").Value;
        }

    }
}
