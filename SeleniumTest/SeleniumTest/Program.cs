using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;

namespace SeleniumTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string action = args[0];
            if (action != null)
            {
                switch(action)
                {
                    case "UpdateBalance":
                        Notifier.UpdateBillNotifications();
                        break;
                    case "SendNotifications":
                        Notifier.SendBillNotifications();
                        break;
                }                
            }
        }

    }
}
