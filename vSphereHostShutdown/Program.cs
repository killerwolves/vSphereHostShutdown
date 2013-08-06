﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace vSphereHostShutdown
{
    class Program
    {
        static void WriteLogEntry(string message, EventLogEntryType type)
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

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: {0} <server> <username> <password>", System.Reflection.Assembly.GetExecutingAssembly().GetName());
                return;
            }


            WriteLogEntry("Initiating host shutdown", EventLogEntryType.Information);

            try
            {
                Vim25Client client = new Vim25Client(args[0], args[1], args[2]);
                client.LogUserEvent("HostSystem", "ha-host", "Shutdown Test");
                client.ShutdownHost_Task(true);
                Console.WriteLine("Shutdown initiated");
                WriteLogEntry("Host shutdown initiated", EventLogEntryType.Information);
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse)
                {
                    HttpWebResponse response = ex.Response as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        string xml = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                        Console.Write(xml);
                        WriteLogEntry("Host shutdown failed\n\nReturned XML:\n" + xml, EventLogEntryType.Error);
                        return;
                    }
                }
                else
                {
                    WriteLogEntry("HTTP Exception caught attempting host shutdown\n\nException details:\n" + ex.ToString(), EventLogEntryType.Error);
                }
                Console.Write(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                WriteLogEntry("Exception caught attempting host shutdown\n\nException details:\n" + ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
