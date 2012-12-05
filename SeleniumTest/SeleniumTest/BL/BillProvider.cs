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
using OpenQA.Selenium;
using OpenQA.Selenium.IE;

namespace SeleniumTest
{
    public static class BillProvider
    {
        
        internal static string GetBillAmount(string userName, string password)
        {
            string retVal = "Unavailable";

            //Clean up any previous instances 
            Process[] testServerProcesses = Process.GetProcessesByName("IEDriverServer");
            foreach (Process process in testServerProcesses)
            {
                process.Kill();
            }

            Process[] ieProcesses = Process.GetProcessesByName("iexplore");
            foreach (Process process in ieProcesses)
            {
                process.Kill();
            }

            try
            {
                using (IWebDriver driver = new InternetExplorerDriver())
                {
                    driver.Navigate().GoToUrl("https://account.windowsazure.com/Subscriptions");
                    IWebElement usernameTB = null;
                    IWebElement pwTB = null;
                    try
                    {
                        usernameTB = driver.FindElement(By.Id("i0116"));                        
                    }
                    catch
                    {
                        retVal = "Username input not found.";                        
                    }
                    if (usernameTB != null)
                    {
                        usernameTB.SendKeys(userName);                       
                        try 
                        { 
                            pwTB = driver.FindElement(By.Id("i0118"));                            
                        }
                        catch 
                        { 
                            retVal = "Password input not found.";                            
                        }
                        if (pwTB != null)
                        {
                            pwTB.SendKeys(TickerEncryption.Utility.Decrypt(password, Properties.Settings.Default.Thumb1, Properties.Settings.Default.Thumb2));                            
                            System.Threading.Thread.Sleep(5000);
                            IWebElement loginBtn = driver.FindElement(By.Id("idSIButton9"));
                            loginBtn.Click();                            

                            bool loggedInSuccessfully = false;
                            int iterationCount = 1;
                            System.Threading.Thread.Sleep(2000);
                            while (loggedInSuccessfully == false && iterationCount <= 5)
                            {
                                try
                                {
                                    IWebElement subscriptionContent = driver.FindElement(By.Id("subscriptions-list"));
                                    IWebElement firstSubscriptionLink = subscriptionContent.FindElement(By.TagName("a"));
                                    firstSubscriptionLink.Click();
                                    IWebElement charged = driver.FindElement(By.ClassName("subscription-estimated-cost"));
                                    retVal = charged.Text;
                                    loggedInSuccessfully = true;
                                    NetUtil.AddEventLogEntry("BillProvider", "Application", "Account balance populated - " + userName + ".");
                                    System.Threading.Thread.Sleep(2000);
                                    try
                                    {
                                        IWebElement logoutBtn = driver.FindElement(By.Id("header-sign-in"));
                                        logoutBtn.Click();
                                    }
                                    catch
                                    {
                                        NetUtil.AddEventLogEntry("BillProvider", "Application", "Unable to click logout button - " + userName + ".");
                                    }
                                }
                                catch
                                {
                                    //Exception here means login button is still processing, subscription info not loaded.  Try clicking login button again.

                                    loginBtn.Click();
                                    System.Threading.Thread.Sleep(2000);
                                    IWebElement subscriptionContent = driver.FindElement(By.Id("subscriptions-list"));
                                    IWebElement firstSubscriptionLink = subscriptionContent.FindElement(By.TagName("a"));
                                    firstSubscriptionLink.Click();
                                    IWebElement charged = driver.FindElement(By.ClassName("subscription-estimated-cost"));
                                    retVal = charged.Text;
                                    loggedInSuccessfully = true;
                                    NetUtil.AddEventLogEntry("BillProvider", "Application", "Account balance populated - " + userName + ".");
                                    try
                                    {
                                        IWebElement logoutBtn = driver.FindElement(By.Id("header-sign-in"));
                                        logoutBtn.Click();
                                    }
                                    catch
                                    {
                                        NetUtil.AddEventLogEntry("BillProvider", "Application", "Unable to click logout button - " + userName + ".");
                                    }
                                                         
                                }

                                iterationCount++;
                            }
                            if (!loggedInSuccessfully)
                            {
                                retVal = "Error loading subscription information.";
                                NetUtil.AddEventLogEntry("BillProvider", "Application", "Error loading subscription balance after retry - " + userName + ".");
                            }

                        }

                    }
                    driver.Quit();
                }
            }
            catch(Exception ex)
            {
                NetUtil.AddEventLogEntry("BillProvider", "Application", "Unable start IE driver: " + ex.Message);
            }
                                   
            return retVal;
        }

    }
}
