using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.ServiceProcess;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace vSphereHostShutdown
{
    [DesignerCategory("Code")]
    public abstract class ServiceBase : System.ServiceProcess.ServiceBase
    {
        public const int SERVICE_ACCEPT_PRESHUTDOWN = 0x100;
        public const int SERVICE_CONTROL_PRESHUTDOWN = 0xf;

        protected ServiceStartType StartType;
        protected string[] Dependencies;

        public ServiceBase()
        {
            this.CanPauseAndContinue = IsOverriden((Action)OnPause) && IsOverriden((Action)OnContinue);
            this.CanStop = IsOverriden((Action)OnStop);
            this.CanShutdown = IsOverriden((Action)OnShutdown);
            this.CanHandlePowerEvent = IsOverriden((Func<PowerBroadcastStatus, bool>)OnPowerEvent);
            this.CanHandleSessionChangeEvent = IsOverriden((Action<SessionChangeDescription>)OnSessionChange);
            this.StartType = ServiceStartType.AutoStart;
            this.Dependencies = null;

            if (IsOverriden((Action)OnPreShutdown))
            {
                FieldInfo acceptedCommandsFieldInfo = typeof(System.ServiceProcess.ServiceBase).GetField("acceptedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
                if (acceptedCommandsFieldInfo != null)
                {
                    int value = (int)acceptedCommandsFieldInfo.GetValue(this);
                    acceptedCommandsFieldInfo.SetValue(this, value | SERVICE_ACCEPT_PRESHUTDOWN);
                }
            }
        }

        public void Install(string[] args)
        {
            using (var servicemanager = NativeServiceManager.Open())
            {
                using (var service = servicemanager.CreateService(this.ServiceName, this.ServiceName, "\"" + Assembly.GetExecutingAssembly().Location + "\" -service", ServiceRights.AllAccess, StartType))
                {
                    service.Start(args);
                }
            }
        }

        public void Start(string[] args)
        {
            using (var servicemanager = NativeServiceManager.Open())
            {
                using (var service = servicemanager.OpenService(this.ServiceName, ServiceRights.AllAccess))
                {
                    service.Start(args);
                }
            }
        }

        public void Uninstall()
        {
            using (var servicemanager = NativeServiceManager.Open())
            {
                using (var service = servicemanager.OpenService(this.ServiceName, ServiceRights.AllAccess))
                {
                    service.Stop();
                    service.Delete();
                }
            }
        }

        public void RunService()
        {
            ServiceBase.Run(this);
        }

        public abstract void RunStandalone(params string[] args);

        public int Run(params string[] args)
        {
            if (args.Length == 1 && args[0].Length >= 2)
            {
                if ("-install".StartsWith(args[0]))
                {
                    Install(new string[] { });
                    return 0;
                }
                else if ("-uninstall".StartsWith(args[0]))
                {
                    Uninstall();
                    return 0;
                }
                else if ("-run".StartsWith(args[0]))
                {
                    Start(new string[] { });
                    return 0;
                }
                else if ("-console".StartsWith(args[0]))
                {
                    RunStandalone();
                    return 0;
                }
                else if ("-service".StartsWith(args[0]))
                {
                    RunService();
                    return 0;
                }
            }

            Console.WriteLine("Usage: {0} <-i|-u|-c|-r>", Assembly.GetExecutingAssembly().Location);
            Console.WriteLine();
            Console.WriteLine("-i{nstall}      Install service");
            Console.WriteLine("-u{ninstall}    Uninstall service");
            Console.WriteLine("-c{onsole}      Run standalone");
            Console.WriteLine("-r{un}          Start service");

            return 1;
        }

        private bool IsOverriden(MethodInfo method)
        {
            var name = method.Name;
            var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var mi = this.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
            return mi.DeclaringType == this.GetType();
        }

        private bool IsOverriden(Delegate action)
        {
            return IsOverriden(action.Method);
        }

        protected override void OnCustomCommand(int command)
        {
            if (command == SERVICE_CONTROL_PRESHUTDOWN)
            {
                OnPreShutdown();
            }
            else
            {
                base.OnCustomCommand(command);
            }
        }

        protected virtual void OnPreShutdown()
        {
        }
    }
}
