using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Net;

namespace vSphereHostShutdown
{
    class Vim25Client
    {
        CookieAwareWebClient client;
        uint sessid;
        uint reqid;
        string server;

        string BuildRequest(Action<XmlWriter> bodyfunc)
        {
            reqid++;
            StringWriter strwriter = new StringWriter();
            XmlTextWriter xmlwriter = new XmlTextWriter(strwriter);
            xmlwriter.WriteStartElement("soap", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlwriter.WriteStartElement("soap", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            xmlwriter.WriteStartElement("operationID");
            xmlwriter.WriteString(String.Format("{0:X8}-{1:X8}", sessid, reqid));
            xmlwriter.WriteEndElement();
            xmlwriter.WriteEndElement();
            xmlwriter.WriteStartElement("soap", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            bodyfunc(xmlwriter);
            xmlwriter.WriteEndElement();
            xmlwriter.WriteEndElement();
            xmlwriter.Close();
            return strwriter.ToString();
        }

        public Vim25Client(string server, string username, string password)
        {
            byte[] buf = new byte[4];
            new Random().NextBytes(buf);
            this.sessid = BitConverter.ToUInt32(buf, 0);
            this.server = server;
            this.reqid = 0;
            this.client = new CookieAwareWebClient();
            this.client.Headers.Add("User-Agent: TCEO vSphere Host Shutdown");
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            try
            {
                Login(username, password);
            }
            catch (WebException ex)
            {
                throw new System.Security.Authentication.AuthenticationException("Authentication failed", ex);
            }
        }

        string SendRequest(Action<XmlWriter> func)
        {
            string envelope = BuildRequest(func);
            return client.UploadString("https://" + server + "/sdk", envelope);
        }

        string Login(string username, string password)
        {
            return SendRequest(writer =>
            {
                writer.WriteStartElement("Login", "urn:vim25");
                writer.WriteStartElement("_this");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "ManagedObjectReference");
                writer.WriteAttributeString("type", "SessionManager");
                writer.WriteAttributeString("serverGuid", "");
                writer.WriteString("ha-sessionmgr");
                writer.WriteEndElement();
                writer.WriteElementString("userName", username);
                writer.WriteElementString("password", password);
                writer.WriteElementString("locale", "en_US");
                writer.WriteEndElement();
            });
        }

        public string LogUserEvent(string entitytype, string entity, string message)
        {
            return SendRequest(writer =>
            {
                writer.WriteStartElement("LogUserEvent", "urn:vim25");
                writer.WriteStartElement("_this");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "ManagedObjectReference");
                writer.WriteAttributeString("type", "EventManager");
                writer.WriteAttributeString("serverGuid", "");
                writer.WriteString("ha-eventmgr");
                writer.WriteEndElement();
                writer.WriteStartElement("entity");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "ManagedObjectReference");
                writer.WriteAttributeString("type", entitytype);
                writer.WriteAttributeString("serverGuid", "");
                writer.WriteString(entity);
                writer.WriteEndElement();
                writer.WriteElementString("msg", message);
                writer.WriteEndElement();
            });
        }

        public string ShutdownHost_Task(bool force)
        {
            return SendRequest(writer =>
            {
                writer.WriteStartElement("ShutdownHost_Task", "urn:vim25");
                writer.WriteStartElement("_this");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "ManagedObjectReference");
                writer.WriteAttributeString("type", "HostSystem");
                writer.WriteAttributeString("serverGuid", "");
                writer.WriteString("ha-host");
                writer.WriteEndElement();
                writer.WriteElementString("force", force.ToString());
                writer.WriteEndElement();
            });
        }
    }
}
