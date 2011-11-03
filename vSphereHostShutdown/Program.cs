using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;

namespace vSphereHostShutdown
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: {0} <server> <username> <password>", System.Reflection.Assembly.GetExecutingAssembly().GetName());
                return;
            }
            try
            {
                Vim25Client client = new Vim25Client(args[0], args[1], args[2]);
                client.LogUserEvent("HostSystem", "ha-host", "Shutdown Test");
                client.ShutdownHost_Task(true);
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
                        return;
                    }
                }
                Console.Write(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }
    }
}
