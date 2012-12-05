using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SeleniumTest
{
    public class NetUtil
    {
        public static void AddEventLogEntry(string source, string log, string message)
        {
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, log);

            EventLog.WriteEntry(source, message);
        }

    }
}
