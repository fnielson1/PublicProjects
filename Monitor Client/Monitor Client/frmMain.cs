using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Configuration;
using Microsoft.Win32;
using RawInput;


namespace Monitor_Client
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// The callback delegate to update the message
        /// </summary>
        /// <param name="message">The message object to update with</param>
        private delegate void UpdateMessageCallback(MessageChangedEventArgs message);
        /// <summary>
        /// The callback delegate to update the connection state
        /// </summary>
        /// <param name="connectionState">The connection state object to update with</param>
        private delegate void UpdateConnectionStateCallback(ConnectionStateChangedEventArgs connectionState);
        /// <summary>
        /// The callback delegate to capture the images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private delegate void CaptureCallback(object sender, EventArgs e);
        /// <summary>
        /// The callback delegate to connect to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private delegate void ConnectCallback(object sender, EventArgs e);
        /// <summary>
        /// The callback delegate to show the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private delegate void KeyPressCallback(object sender, KeyEventArgs e);

    
        /// <summary>
        /// The path to the config file
        /// </summary>
        private readonly string CONFIG_FILEPATH = "";
        /// <summary>
        /// Error for when the config file couldn't be found
        /// </summary>
        private const string ERROR_CONFIG_FILE_NOT_FOUND = "We could not read the following configuration file: \n";
        /// <summary>
        /// Error for when a key was attempted to be accessed that did not exist
        /// </summary>
        private const string ERROR_CONFIG_READ = "We could not read the a value from the configuration file";
        /// <summary>
        /// The key to get the server ip from the config file
        /// </summary>
        private const string CONFIG_SERVER_IP = "serverip";
        /// <summary>
        /// The key to get the password to connect to the server
        /// </summary>
        private const string CONFIG_SERVER_PASSWORD = "password";
        /// <summary>
        /// The key to get the desired image quality
        /// </summary>
        private const string CONFIG_IMAGE_QUALITY = "imagequality";


        /// <summary>
        /// How long to wait in between captures (in ms)
        /// </summary>
        private const int CAPTURE_WAIT = 5000;
        /// <summary>
        /// What key char must be pressed to show the form
        /// </summary>
        private const byte SHOW_FORM = 0x1B; 

        /// <summary>
        /// The quality of the images to send
        /// </summary>
        private int _imageQuality = 10;

        /// <summary>
        /// The ip address of the server to connect to
        /// </summary>
        private IPAddress _serverIp;

        /// <summary>
        /// The object that handles the connection to the server
        /// </summary>
        private MonitorClient _server;

        /// <summary>
        /// The object to read the config file
        /// </summary>
        private Configuration _config;

        /// <summary>
        /// Used for receiving input from the keyboard
        /// </summary>
        private InputDevice _id;
        


        #region Form
        public frmMain()
        {
            InitializeComponent();

            // Hook the events that are fired to our functions
            MonitorClient.MessageChanged += new MonitorClient.MessageChangedEventHandler(client_MessageChanged);
            MonitorClient.ConnectionStateChanged += new MonitorClient.ConnectionStateChangedEventHandler(client_ConnectionStateChanged);

            // Create a new InputDevice object, get the number of
            // keyboards, and register the method which will handle the 
            // InputDevice KeyPressed event
            _id = new InputDevice(this.Handle);
            _id.EnumerateDevices(); // Get the keyboard(s)
            _id.HookedKeys.Add(Keys.L);
            _id.KeyDown += new InputDevice.DeviceEventHandler(KeyInput_KeyDown);

            // Add the keys that we want to hook
            lblShow.Text = "Press " + Keys.Alt.ToString() + " + " + Keys.L.ToString() + " to show this form.";
            
            try
            {
                CONFIG_FILEPATH = GetFilePathRegistry();
                _config = ConfigurationManager.OpenExeConfiguration(CONFIG_FILEPATH);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(ERROR_CONFIG_FILE_NOT_FOUND + CONFIG_FILEPATH);
                this.Close(); // Close the program as the config file couldn't be read
            }

            // Start the background worker
            bgdWorker.RunWorkerAsync();
        }

        /// <summary>
        /// When the user presses the correct keys, show the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyInput_KeyDown(object sender, InputDevice.KeyControlEventArgs e)
        {
            if (_id.HookedKeys.Contains(e.Keyboard.pressedKey) && e.Keyboard.modifiedKey == Keys.Alt)
                this.Show();
        }

        
        /// <summary>
        /// The WndProc is overridden to allow InputDevice to intercept
        /// messages to the window and thus catch WM_INPUT messages
        /// </summary>
        /// <param name="message"></param>
        protected override void WndProc(ref Message message)
        {
            if (_id != null)
                _id.ProcessMessage(message);
            base.WndProc(ref message);
        }


        /// <summary>
        /// Hide the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuFileHide_Click(object sender, EventArgs e)
        {
            this.Hide();
        }


        /// <summary>
        /// When the form size changes, check the window state. If minimized, call this.Hide()
        /// and change it back to the normal state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.Hide();
            }
        }


        /// <summary>
        /// When the form is shown, hide it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_Shown(object sender, EventArgs e)
        {
            // Hide the form from the user
            this.Hide();

            try
            {
                // Load the configuration settings
                if (IPAddress.TryParse(_config.AppSettings.Settings[CONFIG_SERVER_IP].Value, out _serverIp))
                    txtIpAddress.Text = _serverIp.ToString();
                int.TryParse(_config.AppSettings.Settings[CONFIG_IMAGE_QUALITY].Value, out _imageQuality);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(ERROR_CONFIG_READ);
                this.Close();
            }
        }


        /// <summary>
        /// Catches the event that will then display the message
        /// </summary>
        /// <param name="e">The object containing the string message</param>
        private void _UpdateMessage(MessageChangedEventArgs e)
        {
            LogMessage(e.EventMessage);
        }



        /// <summary>
        /// Catches the event that will tell us what state the connection is in 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateConnectionState(ConnectionStateChangedEventArgs e)
        {
            // If we just got disconnected from the server, change the GUI
            if (e.ConnectionState == ConnectionStateChangedEventArgs.State.DISCONNECTED)
            {
                // Do anything you want when the connection disconnects
            }
        }


        /// <summary>
        /// Catches the event that updates the message and then invokes the 
        /// delegate to update the message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that has the message to update with</param>
        public void client_MessageChanged(object sender, MessageChangedEventArgs e)
        {
            this.Invoke(new UpdateMessageCallback(this._UpdateMessage), e);
        }


        /// <summary>
        /// Catches the event that updates the clients connection state and then 
        /// invokes the delegate to update the connection state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that has the current connection state</param>
        public void client_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.Invoke(new UpdateConnectionStateCallback(this.UpdateConnectionState), e);
        }


        /// <summary>
        /// Applies the new ip address to the server and connects it 
        /// if it was connected prior
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress newServerIp = IPAddress.Parse(txtIpAddress.Text);

                if (_serverIp == null || !_serverIp.Equals(newServerIp))
                {
                    _serverIp = newServerIp;
                    _config.AppSettings.Settings[CONFIG_SERVER_IP].Value = _serverIp.ToString();
                    _config.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("appSettings");

                    // Connect the server
                    if (_server != null)
                    {
                        if (_server.Connected)
                        {
                            _server.ChangeIPAddress(_serverIp);
                            _server.Connect();
                        }
                        else
                            _server.ChangeIPAddress(_serverIp);
                    }
                }
            }
            catch (FormatException ex)
            {
                LogMessage(ex.Message);
            }
        }


        /// <summary>
        /// Sets the ip address textbox to it's old value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if(_serverIp != null)
                txtIpAddress.Text = _serverIp.ToString();
        }


        /// <summary>
        /// Get a screenshot of the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Capture_Click(object sender, EventArgs e)
        {
            if (_server != null && _server.Connected)
            {
                // Capture the image and send it to the server
                _server.Capture();
            }
        }


        /// <summary>
        /// Toggles connecting and disconnecting to the monitor server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connect_Click(object sender, EventArgs e)
        {
            try
            {
                // Create a client instance if there is none yet
                if (_server == null)
                {
                    if (_serverIp == null)
                        return;
                    else
                        _server = new MonitorClient(_serverIp, new Size(800, 600), 10);
                }
              

                // Connect this client to the server
                if (!_server.Connected)
                {
                    _server.ChangeIPAddress(_serverIp);
                    _server.Connect();
                }
                else
                    _server.Disconnect();
            }
            catch (FormatException ex)
            {
                LogMessage(ex.Message);
            }
        }


        /// <summary>
        /// Logs the any messages that we want
        /// </summary>
        /// <param name="message">The message to write</param>
        private void LogMessage(string message)
        {
            txtLog.AppendText(message + "\n");
        }


        /// <summary>
        /// Exit the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuMainFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Unhook the events as the form is closing and invoking anything throws an error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Avoid closing the form only if it's the user trying to close and didn't use File->Exit
            if (!this.mnuMainFileExit.Checked && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                LogMessage("Use:\n File -> Exit\n to exit program\n\n");
                return; // Stop the exit as the user didn't hit the exit button
            }
            
            // Stop catchng events
            MonitorClient.ConnectionStateChanged -= client_ConnectionStateChanged;
            MonitorClient.MessageChanged -= client_MessageChanged;

            // Closes the connections, streams, etc.
            if (_server != null && _server.Connected)
                _server.Disconnect();

            // Stop the background worker from capturing screenshots
            bgdWorker.Dispose();
        }
        #endregion Form


        #region Background Worker
        /// <summary>
        /// A background worker that captures and sends the images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgdWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(CAPTURE_WAIT);
                    if (_server != null && _server.Connected)
                        this.Invoke(new CaptureCallback(Capture_Click), new object[] { null, new EventArgs() });
                    else if (_server == null || _server.Connected == false)
                        this.Invoke(new ConnectCallback(Connect_Click), new object[] { null, new EventArgs() });
                }
                catch (ObjectDisposedException)
                {
                    // The form was closed but the bgdworker was still running so this exception was thrown
                    // TODO: Figure out how to stop this from happening. Until then, ignore the exception
                }
            }
        }
        #endregion Background Worker


        #region Methods
        /// <summary>
        /// Returns the file path of this program by finding it in the registry
        /// </summary>
        private string GetFilePathRegistry()
        {
            string path = @"SOFTWARE\";

            if (Environment.Is64BitOperatingSystem)
                path += @"Wow6432Node\"; // If it's a 64bit operating system, the path is slightly different here
            path += @"Microsoft\Windows\CurrentVersion\Run";
            RegistryKey runKey = Registry.LocalMachine.OpenSubKey(path);
            return runKey.GetValue("Monitor Client").ToString().Trim('\"');
        }
        #endregion
    }
}
