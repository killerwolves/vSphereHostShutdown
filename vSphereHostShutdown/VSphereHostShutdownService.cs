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
    public class VSphereServerSection : ConfigurationSection
    {
        [ConfigurationProperty("servers")]
        public VSphereServerCollection Servers { get { return (VSphereServerCollection)base["servers"]; } set { base["printers"] = value; } }
    }

    [ConfigurationCollection(typeof(VSphereServerElement))]
    public class VSphereServerCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType { get { return ConfigurationElementCollectionType.BasicMapAlternate; } }

        protected override string ElementName { get { return "server"; } }
        protected override bool IsElementName(string elementName) { return elementName == ElementName; }
        public override bool IsReadOnly() { return false; }
        protected override ConfigurationElement CreateNewElement() { return new VSphereServerElement(); }
        protected override object GetElementKey(ConfigurationElement element) { return ((VSphereServerElement)element).Name; }
        public VSphereServerElement this[int idx] { get { return (VSphereServerElement)base.BaseGet(idx); } }
    }

    public class VSphereServerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name { get { return (string)base["name"]; } set { base["name"] = value; } }

        [ConfigurationProperty("host", IsRequired = true)]
        public string Host { get { return (string)base["host"]; } set { base["host"] = value; } }

        [ConfigurationProperty("username", IsRequired = true)]
        public string Username { get { return (string)base["username"]; } set { base["username"] = value; } }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password { get { return (string)base["password"]; } set { base["password"] = value; } }
    }

    public class VSphereServer
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    [DesignerCategory("Code")]
    public class VSphereHostShutdownService : ServiceBase
    {
        protected string LogFile;

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

        protected IEnumerable<VSphereServer> GetServers()
        {
            var Section = ((VSphereServerSection)ConfigurationManager.GetSection("serverConfiguration"));
            var ServerCollection = Section.Servers;
            var ServerElements = ServerCollection.OfType<VSphereServerElement>();
            return ServerElements.Select(s => new VSphereServer { Name = s.Name, Host = s.Host, Username = s.Username, Password = s.Password });
        }

        protected override void OnStart(string[] args)
        {
            Logger.WriteLogEntry("Starting vSphereHostShutdown Service", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            Logger.WriteLogEntry("Stopping vSphereHostShutdown Service", EventLogEntryType.Information);
        }

        protected override void OnPreShutdown()
        {
            Logger.WriteLogEntry("Shutdown in vSphereHostShutdown Service", EventLogEntryType.Information);
            RunStandalone();
        }

        public override void RunStandalone(params string[] args)
        {
            Logger.WriteLogEntry("Initiating host shutdown", EventLogEntryType.Information);

            if (Server != null && Username != null && Password != null)
            {
                ShutdownHost(new VSphereServer { Name = Server, Host = Server, Username = Username, Password = Password });
            }
            else
            {
                foreach (VSphereServer server in GetServers())
                {
                    ShutdownHost(server);
                }
            }
        }

        protected void ShutdownHost(VSphereServer server)
        {
            try
            {
                Vim25Client client = new Vim25Client(server.Host, server.Username, server.Password);
                client.LogUserEvent("HostSystem", "ha-host", "Shutdown Test");
                client.ShutdownHost_Task(true);
                Logger.WriteLogEntry(String.Format("Host {0} shutdown initiated", server.Name), EventLogEntryType.Information);
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
                        Logger.WriteLogEntry(String.Format("Host {0} shutdown failed\n\nReturned XML:\n{1}", server.Name, xml), EventLogEntryType.Error);
                        return;
                    }
                }
                else
                {
                    Logger.WriteLogEntry("HTTP Exception caught attempting host shutdown\n\nException details:\n" + ex.ToString(), EventLogEntryType.Error);
                }
                Console.Write(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                Logger.WriteLogEntry("Exception caught attempting host shutdown\n\nException details:\n" + ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
