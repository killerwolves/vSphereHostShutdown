using System;
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
        static void Main(string[] args)
        {
            using (VSphereHostShutdownService svc = new VSphereHostShutdownService())
            {
                if (args.Length == 3)
                {
                    svc.Server = args[0];
                    svc.Username = args[1];
                    svc.Password = args[2];
                    svc.RunStandalone();
                }
                else
                {
                    svc.Run(args);
                }
            }
        }
    }
}
