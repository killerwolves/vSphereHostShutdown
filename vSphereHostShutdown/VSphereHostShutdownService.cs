using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Net;
using System.Xml;
using System.IO;
using System.Reflection;

namespace vSphereHostShutdown
{
    [DesignerCategory("Code")]
    public class VSphereHostShutdownService : ServiceBase
    {
        protected string LogFile;

        protected void WriteLogEntry(string message, EventLogEntryType type)
        {
            File.AppendAllLines(LogFile, new string[] { message });

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

        public string Server;
        public string Username;
        public string Password;

        public VSphereHostShutdownService()
        {
            ServiceName = "VSphereHostShutdown";
            Server = ConfigurationManager.AppSettings["server"];
            Username = ConfigurationManager.AppSettings["username"];
            Password = ConfigurationManager.AppSettings["password"];
            StartType = ServiceStartType.DemandStart;
            LogFile = Assembly.GetExecutingAssembly().Location + ".log";
        }

        protected override void OnStart(string[] args)
        {
            WriteLogEntry("Starting vSphereHostShutdown Service", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            WriteLogEntry("Stopping vSphereHostShutdown Service", EventLogEntryType.Information);
        }

        protected override void OnPreShutdown()
        {
            WriteLogEntry("Shutdown in vSphereHostShutdown Service", EventLogEntryType.Information);
            RunStandalone();
        }

        public override void RunStandalone(params string[] args)
        {
            WriteLogEntry("Initiating host shutdown", EventLogEntryType.Information);

            try
            {
                Vim25Client client = new Vim25Client(Server, Username, Password);
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
