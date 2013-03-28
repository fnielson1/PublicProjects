using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace EditRegistry
{
    // THIS DOESN'T WORK!!! ONLY HERE FOR UNDERSTANDING HOW TO EDIT THE REGISTRY!!!
    class Program
    {
        /// <summary>
        /// Adds the monitor client to the registry so that it will start up 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string appPath, command;
            bool isInstall = false;
            
            // Get the path to this console app
            appPath = Environment.GetCommandLineArgs()[0];
            appPath = Path.GetDirectoryName(appPath);

            // Check if we are installing or not (install is default)
            if (args.Length >= 1)
                command = args[0];
            else
                command = "-install";

            // Find if we are to uninstall the registry key or not
            switch (command)
            {
                case "-install":
                    isInstall = true;
                    break;
                case "-uninstall":
                    isInstall = false;
                    break;
            }
            _RegisterInStartup(isInstall, "Monitor Client", appPath);
        }

        private static void _RegisterInStartup(bool isChecked, string appName, string appPath)
        {
            appPath = "\""  + appPath + @"\" + appName +  ".exe\"";
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue(appName, appPath);
            }
            else
            {
                registryKey.DeleteValue(appName);
            }
            Console.WriteLine(isChecked);
            Console.WriteLine(registryKey.ToString() + "\n");
            Console.WriteLine(appName + "\n" + appPath);
            Console.ReadLine();
        }

    }
}
