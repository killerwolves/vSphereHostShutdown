using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace vSphereHostShutdown
{
    public static class Logger
    {
        public static void WriteLogEntry(string message, EventLogEntryType type)
        {
            try
            {
                if (!EventLog.SourceExists("vSphereHostShutdown"))
                {
                    EventLog.CreateEventSource("vSphereHostShutdown", "application");
                }

                EventLog.WriteEntry("vSphereHostShutdown", message, type);
            }
            catch
            {
                Console.WriteLine("Unable to write to event log");
            }
        }
    }
}
