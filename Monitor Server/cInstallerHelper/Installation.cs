using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Configuration.Install;
using System.Configuration;
using System.IO;
using NetFwTypeLib;
using System.Security.AccessControl;


namespace InstallerHelper
{
    [RunInstaller(true)]
    public partial class Installation : Installer
    {
        /// <summary>
        /// The name of the exe program
        /// </summary>
        private const string exeName = "Monitor Server.exe";


        public Installation()
        {

        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // Variables
            string configName = exeName + ".config";

            // The ip address of the server
            string serverIp = Context.Parameters["serverip"];
            string portNumber = Context.Parameters["portnumber"];

            // Create the config file
            string dirPath = new DirectoryInfo(Context.Parameters["assemblypath"].ToString()).Parent.FullName;
            string exeFilePath = Path.Combine(dirPath, exeName);
            string configPath = Path.Combine(dirPath, configName);
            string imagePath = Path.Combine(dirPath, "images");
        

            // Create the directories
            Directory.CreateDirectory(imagePath);

            // Edit the config file and move it
            Configuration config = ConfigurationManager.OpenExeConfiguration(exeFilePath);
            config.AppSettings.Settings.Add("serverip", serverIp);
            config.AppSettings.Settings.Add("portnumber", portNumber);
            config.AppSettings.Settings.Add("password", "");
            config.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");


            // Add a firewall exception
            AuthorizeProgram("Monitor Server.exe", exeFilePath, NET_FW_SCOPE_.NET_FW_SCOPE_LOCAL_SUBNET, NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);
            

            // Add write permissions to the image folder
            DirectorySecurity ds = Directory.GetAccessControl(imagePath);
            ds.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.Read | FileSystemRights.Write, AccessControlType.Allow));
            Directory.SetAccessControl(imagePath, ds); // Finally, write the permissions

            // Add write permission to the folder so we can write to the config file
            ds = Directory.GetAccessControl(dirPath);
            ds.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.Read | FileSystemRights.Write, AccessControlType.Allow));
            Directory.SetAccessControl(dirPath, ds);

            // Add the write permission to the config file itself
            FileSecurity fs = File.GetAccessControl(configPath);
            fs.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify, AccessControlType.Allow));
            File.SetAccessControl(configPath, fs);
        }


        /// <summary>
        /// When we are uninstalling, remove the the necessary data
        /// </summary>
        /// <param name="savedState"></param>
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            base.Uninstall(savedState);

            string dirPath = new DirectoryInfo(Context.Parameters["assemblypath"].ToString()).Parent.FullName;
            string filePath = Path.Combine(dirPath, exeName);

            // Remove the firewall rule
            AuthorizeProgram(exeName, filePath, NET_FW_SCOPE_.NET_FW_SCOPE_LOCAL_SUBNET, NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY, false);
            // Delete the images directory
            Directory.Delete(Path.Combine(dirPath, "images"), true); 
        }


        /// <summary>
        /// Returns the firewall manager object
        /// </summary>
        /// <returns></returns>
        public static INetFwMgr WinFirewallManager()
        {
            Type type = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
            return Activator.CreateInstance(type) as INetFwMgr;
        }

        /// <summary>
        /// Authorizes the program that we want with the firewall
        /// </summary>
        /// <param name="title">The title of the program</param>
        /// <param name="path">The path to the file</param>
        /// <param name="scope">What is the scope it will work in (local, public, etc)</param>
        /// <param name="ipver">The kind of IvP it will work on</param>
        /// <param name="authorize">De-authorize the program instead</param>
        /// <returns></returns>
        public bool AuthorizeProgram(string title, string path, NET_FW_SCOPE_ scope, NET_FW_IP_VERSION_ ipver, bool authorize = true)
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication");
            INetFwAuthorizedApplication authapp = Activator.CreateInstance(type) as INetFwAuthorizedApplication;
            authapp.Name = title;
            authapp.ProcessImageFileName = path;
            authapp.Scope = scope;
            authapp.IpVersion = ipver;
            authapp.Enabled = true;
            INetFwMgr mgr = WinFirewallManager();
           
            try
            {
                if (authorize)
                    mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(authapp);
                else
                    mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.Write(ex.Message);
                return false;
            }
            return true;
        }
    }
}
