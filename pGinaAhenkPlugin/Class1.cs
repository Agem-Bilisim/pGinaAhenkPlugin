using System;
using System.ServiceProcess;
using System.Text;
using pGina.Shared.Types;
using log4net;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

/*
* See https://github.com/pgina/pgina/wiki/Plugin-Tutorial
* for more info!
*/
namespace pGinaAhenkPlugin
{
    public class PluginImpl : pGina.Shared.Interfaces.IPluginAuthenticationGateway, pGina.Shared.Interfaces.IPluginEventNotifications
    {
        private ILog m_logger;

        public PluginImpl()
        {
            m_logger = LogManager.GetLogger("pGina.Plugin.AhenkPlugin");
        }

        public string Description
        {
            get
            {
                return "Ahenk pGina Plugin for policy processing & logging";
            }
        }

        public string Name
        {
            get
            {
                return "Ahenk pGina Plugin";
            }
        }

        private static readonly Guid m_uuid = new Guid("D5C4D580-B3B2-4B0F-9244-1FF4B7F797E0");

        public Guid Uuid
        {
            get
            {
                return m_uuid;
            }
        }

        public string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public BooleanResult AuthenticatedUserGateway(SessionProperties properties)
        {
            UserInformation userInfo = properties.GetTrackedSingle<UserInformation>();
            m_logger.InfoFormat("User successfully authenticated {0}", userInfo.Username);

            return new BooleanResult() { Success = true };


        }

        public void SessionChange(SessionChangeDescription changeDescription, SessionProperties properties)
        {
            m_logger.InfoFormat("Session change. reaseon: {0}, id:{1}", changeDescription.Reason, changeDescription.SessionId);
            UserInformation userInfo = properties.GetTrackedSingle<UserInformation>();
            /*
            * See https://msdn.microsoft.com/en-us/library/system.serviceprocess.sessionchangedescription.aspx
            * for session change description
            */
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                    m_logger.InfoFormat("User logged on {0}", userInfo.Username);
                    RunScript(String.Format("ahenk.exe login {0} {1} {2}", userInfo.Username, "win", ":0"));
                    break;
                case SessionChangeReason.SessionLogoff:
                case SessionChangeReason.SessionLock:
                    m_logger.InfoFormat("User logged off {0}", userInfo.Username);
                    RunScript(String.Format("ahenk.exe logout {0} ", userInfo.Username));
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }

        public void Starting()
        {
        }

        public void Stopping()
        {
        }


        public string RunScript(string scriptText)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(scriptText);
            pipeline.Commands.Add("Out-String");

            Collection<PSObject> results = pipeline.Invoke();
            runspace.Close();

            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in results)
            {
                stringBuilder.AppendLine(obj.ToString());
            }

            return stringBuilder.ToString();
        }
    }
}