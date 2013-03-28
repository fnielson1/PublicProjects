using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Configuration;

namespace Monitor_Server
{
    public partial class frmServer : Form
    {
        /// <summary>
        /// The callback delegate to update the status
        /// </summary>
        /// <param name="strMessage">The message to update with</param>
        private delegate void UpdateStatusCallback(string strMessage);
        /// <summary>
        /// The callback delegate to update the image
        /// </summary>
        /// <param name="img">The image to update with</param>
        private delegate void UpdateImageCallback(ImageChangedEventArgs img);
        /// <summary>
        /// The callback delegate to update the client's action
        /// </summary>
        /// <param name="clientAction">The action to update with</param>
        private delegate void UpdateClientStatusCallback(ClientStatusEventArgs clientStatus);


        /// <summary>
        /// The key to get the server ip from the config file
        /// </summary>
        private const string CONFIG_SERVER_IP = "serverip";
        /// <summary>
        /// The path to the configuration file
        /// </summary>
        private const string CONFIG_FILEPATH = @"Monitor Server.exe";


        /// <summary>
        /// The ip address of the server
        /// </summary>
        private IPAddress _serverIp;
        
        /// <summary>
        /// The monitor server 
        /// </summary>
        private MonitorServer _mainServer;

        /// <summary>
        /// The object to read the configuration file
        /// </summary>
        private Configuration _config;

        public frmServer()
        {
            InitializeComponent();

            // Hook the StatusChanged event handler to mainServer_StatusChanged
            MonitorServer.StatusChanged += new MonitorServer.StatusChangedEventHandler(mainServer_StatusChanged);
            // Hook the ImageChanged event handler to mainServer_ImageChanged
            MonitorServer.ImageChanged += new MonitorServer.ImageChangedEventHandler(mainServer_ImageChanged);
            // Hook the ClientAction event handler to mainServer_ClientAction
            MonitorServer.ClientAction += new MonitorServer.ClientActionEventHandler(mainServer_ClientAction);

            try
            {
                _config = ConfigurationManager.OpenExeConfiguration(CONFIG_FILEPATH);
            }
            catch (ConfigurationErrorsException ex)
            {
                MessageBox.Show(ex.Message + "\n" + CONFIG_FILEPATH);
            }
        }


        /// <summary>
        /// When the form is shown, update the server ip address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmServer_Shown(object sender, EventArgs e)
        {
            try
            {
                // Load the configuration settings
                if (IPAddress.TryParse(_config.AppSettings.Settings[CONFIG_SERVER_IP].Value, out _serverIp))
                    txtIpAddress.Text = _serverIp.ToString();
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("We could not read the following configuration file: \n" + CONFIG_FILEPATH);
                this.Close();
            }
        }


        /// <summary>
        /// Call the method in cMonitorServer that will start the server listening for connections.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnListen_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress newServerIp = IPAddress.Parse(txtIpAddress.Text);

                // Change the ip address in the config file if the new one is different
                if (_serverIp == null || !_serverIp.Equals(newServerIp))
                {
                    _serverIp = newServerIp;
                    _config.AppSettings.Settings[CONFIG_SERVER_IP].Value = _serverIp.ToString();
                    _config.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("appSettings");
                }

                // Don't try to have the server listen twice.
                if (_mainServer != null && _mainServer.Running)
                {
                    _mainServer.RequestStop();
                    txtLog.AppendText("Stopped listening\r\n");
                    btnListen.Text = "Start Listening";
                }
                else
                {
                    // Create a new instance of the MonitorServer object
                    _mainServer = new MonitorServer(_serverIp);

                    // Start listening for connections
                    _mainServer.StartListening();

                    // Show that we started to listen for connections
                    if (_mainServer.Running)
                    {
                        txtLog.AppendText("Monitoring for connections...\r\n");
                        btnListen.Text = "Stop Listening";
                    }
                }
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Catches the event that will then update the log with the passed message
        /// </summary>
        /// <param name="strMessage">A string containing the message</param>
        private void _UpdateMessage(string strMessage)
        {
            // Updates the log with the message
            txtLog.AppendText(strMessage + "\r\n");
        }


        /// <summary>
        /// Catches the event that will then update the image with the image that the client sent. Only
        /// updates the image from the client that is selected in the user's list
        /// </summary>
        /// <param name="e">The image that was received from the stream</param>
        private void _UpdateImage(ImageChangedEventArgs e)
        {
            if(lstUsers.SelectedItems.ContainsKey(e.UserName))
                pctImage.Image = e.ImageScreenshot;
        }


        /// <summary>
        /// The callback function that will update a client's actions
        /// </summary>
        /// <param name="clientStatus">The object containing the action that the client performed</param>
        private void _UpdateClientStatus(ClientStatusEventArgs clientStatus)
        {
            if (clientStatus.Status == ClientStatusEventArgs.ClientStatus.CONNECTED)
                lstUsers.Items.Add(clientStatus.UserName, clientStatus.UserName, "");
            else if (clientStatus.Status == ClientStatusEventArgs.ClientStatus.DISCONNECTED)
                lstUsers.Items.RemoveByKey(clientStatus.UserName);
        }


        /// <summary>
        /// This is hooked to the status change event so that if it fires, this gets called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that has the message that was sent from the client</param>
        public void mainServer_StatusChanged(object sender, MessageChangedEventArgs e)
        {
            // Call the method that updates the form
            this.Invoke(new UpdateStatusCallback(this._UpdateMessage), e.Message);
        }


        /// <summary>
        /// This is hooked to the image change event so that if it fires, this gets called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that has the image that was sent from the client</param>
        public void mainServer_ImageChanged(object sender, ImageChangedEventArgs e)
        {
            // Call the method that updates the form
            this.Invoke(new UpdateImageCallback(this._UpdateImage), e);
        }


        /// <summary>
        /// The is hooked to the client action event so that if it fires, this gets called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object containing the action that the client performed</param>
        private void mainServer_ClientAction(object sender, ClientStatusEventArgs e)
        {
            this.Invoke(new UpdateClientStatusCallback(this._UpdateClientStatus), e);
        }


        /// <summary>
        /// Catches the form closing event and closes the server connection if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_mainServer != null && _mainServer.Running)
            {
                // Closes the connections, streams, etc.
                _mainServer.RequestStop();
            }
        }
    }
}
