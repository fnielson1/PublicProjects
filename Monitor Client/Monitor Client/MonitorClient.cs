using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Drawing;
using MonitorCommunication;


namespace Monitor_Client
{
    #region Event Classes
    /// <summary>
    /// Holds the arguments for the StatusChanged event
    /// </summary>
    public class MessageChangedEventArgs : EventArgs
    {
        // The argument we're interested in is a message describing the event
        private string _eventMsg;


        /// <summary>
        /// Constructor for setting the event message
        /// </summary>
        /// <param name="strEventMsg">The message in the event</param>
        public MessageChangedEventArgs(string strEventMsg) { _eventMsg = strEventMsg; }


        /// <summary>
        /// Property for retrieving and setting the event message
        /// </summary>
        public string EventMessage { get { return _eventMsg; } set { _eventMsg = value; } }
    }


    public class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Specify the connection's state (connected, disconnected, etc)
        /// </summary>
        public enum State
        {
            CONNECTED,
            DISCONNECTED
        }

        /// <summary>
        /// Holds the state of the connection
        /// </summary>
        private State _state;


        /// <summary>
        /// The state of the connection to set
        /// </summary>
        /// <param name="state">The enum representing the connection's state</param>
        public ConnectionStateChangedEventArgs(State state) { _state = state; }


        /// <summary>
        /// Get or set the connection's state
        /// </summary>
        public State ConnectionState { get { return _state; } set { _state = value; } }
    }
    #endregion Event Classes


    #region Monitor Client
    /// <summary>
    /// The class that is used for sending the screenshot capture images to the server
    /// </summary>
    public class MonitorClient
    {
        /// <summary>
        /// This delegate is needed to specify the parameters we're passing with our message changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that contains the message from the status change</param>
        public delegate void MessageChangedEventHandler(object sender, MessageChangedEventArgs e);

        /// <summary>
        /// This delgete is needed to specify the parameters we're passing to our connection state changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionStateChangedEventArgs e);


        /// <summary>
        /// The event that will notify the listeners when a user has sent a message
        /// </summary>
        public static event MessageChangedEventHandler MessageChanged;
        /// <summary>
        /// The object that will contain the message for when we send a message 
        /// </summary>
        private static MessageChangedEventArgs _messageEvent; // This doesn't have to be static, but it saves the trouble of creating a new variable each time

        /// <summary>
        /// The event that will notify the listeners when the state of the connection has changed
        /// </summary>
        public static event ConnectionStateChangedEventHandler ConnectionStateChanged;
        /// <summary>
        /// The object that will contain the connection's state enum
        /// </summary>
        private static ConnectionStateChangedEventArgs _connectionStateEvent;


        /// <summary>
        /// The connection to the server was lost for an unknown reason
        /// </summary>
        private const string ERROR_CONNECTION_CLOSED_UNKNOWN = "The connection to the server was lost for an unknown reason";
        /// <summary>
        /// The connection to the server was lost as the server couldn't be contacted
        /// </summary>
        private const string ERROR_SERVER_COULD_NOT_BE_CONTACTED = "The connection to the server was lost as the server couldn't be contacted";
        /// <summary>
        /// The number of seconds to wait for a connection to be established
        /// </summary>
        private const int CONNECT_WAIT_TIME = 2; // Seconds
        /// <summary>
        /// The error code for a SecketException
        /// </summary>
        private const int ERROR_CODE_SOCKET = 10061;
        private const int IMAGE_QUALITY_MIN = 0;
        private const int IMAGE_QUALITY_MAX = 100;
        private const int PORT_NUMBER_MIN = 0;
        private const int PORT_NUMBER_MAX = 65000;


        /// <summary>
        /// The thread that allows us to receive messages from the monitor server
        /// </summary>
        private Thread _thrMessaging;
        /// <summary>
        /// For connecting to the monitor server
        /// </summary>
        private TcpClient _tcpServer;
        /// <summary>
        /// The object used for communicating with the client
        /// </summary>
        private Communication _communication;
        /// <summary>
        /// The ip address of the server
        /// </summary>
        private IPAddress _serverIp;
        /// <summary>
        /// The desired size that the image should be 
        /// </summary>
        private Size _imageSize; 

        /// <summary>
        /// The port number to connect on
        /// </summary>
        private int _portNumber;
        /// <summary>
        /// The quality of the image when capturing
        /// </summary>
        private int _imageQuality;
        /// <summary>
        /// Whether we are connected or not
        /// </summary>
        private bool _connected;
        /// <summary>
        /// The username is the ip of this computer
        /// </summary>
        private string _userName;


        /// <summary>
        /// The constructor that sets the ip address, port number, etc
        /// </summary>
        /// <param name="serverIp">The ip of the server to connect to</param>
        /// <param name="imageSize">The size that the image should be before being sent to the server</param>
        /// <param name="portNumber">The port number to connect to on the server</param>
        /// <param name="userName">The user name used to register with the server (if null, use local host's ip address)</param>
        public MonitorClient(IPAddress serverIp, Size imageSize, int imageQuality = 10, int portNumber = 8080, string userName = null)
        {

            _serverIp = serverIp;
            _portNumber = portNumber;
            _imageSize = imageSize;
            _imageQuality = imageQuality;
            
            // Make sure the info is valid
            if(userName == null)
                _userName = _GetLocalIpAddress().ToString();
            if (imageQuality <= IMAGE_QUALITY_MIN || imageQuality > IMAGE_QUALITY_MAX)
                throw new ArgumentOutOfRangeException(String.Format("The image quality when converting the image must be between {0} and {1}", IMAGE_QUALITY_MIN, IMAGE_QUALITY_MAX));
            if (_portNumber <= PORT_NUMBER_MIN || _portNumber > PORT_NUMBER_MAX)
                throw new ArgumentOutOfRangeException(String.Format("The port number must be greater than {0} and less than {1}", PORT_NUMBER_MIN, PORT_NUMBER_MAX));
        }


        #region Event Methods
        /// <summary>
        /// This is called when we want to raise the MessageChanged event
        /// </summary>
        /// <param name="e">The MessageChangedEventArgs object that has the message inside it</param>
        private void _OnMessageChanged(MessageChangedEventArgs e)
        {
            MessageChangedEventHandler messageHandler = MessageChanged;

            if (messageHandler != null)
            {
                // Invoke the delegate
                messageHandler(this, e);
            }
        }


        /// <summary>
        /// Updates the messages on what's happening. This could be an error or some other message
        /// </summary>
        /// <param name="message">The message the to update with</param>
        private void _UpdateMessage(string message)
        {
            // Set our static MessageChangedEventArgs to equal a new object that contains 
            // the message we want to fire
            _messageEvent = new MessageChangedEventArgs(message);
            _OnMessageChanged(_messageEvent);
        }


        /// <summary>
        /// This is called when we want to raise the ConnectionStateChanged event
        /// </summary>
        /// <param name="e">The ConnectionStateChangedEventArgs object that has the connection's state</param>
        private void _OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChangedEventHandler stateHandler = ConnectionStateChanged;
            if (stateHandler != null)
                stateHandler(this, e);
        }


        /// <summary>
        /// Updates the state of our connection
        /// </summary>
        /// <param name="state">The connection state to update with</param>
        private void _UpdateConnectionState(ConnectionStateChangedEventArgs.State state)
        {
            _connectionStateEvent = new ConnectionStateChangedEventArgs(state);
            _OnConnectionStateChanged(_connectionStateEvent);
        }
        #endregion Event Methods


        #region Methods
        /// <summary>
        /// Changes the ip address that the client is connected to. The client (if connected) is 
        /// disconnected when this method is called
        /// </summary>
        /// <param name="ipAddress">The IP address to change to</param>
        public void ChangeIPAddress(IPAddress serverIp)
        {
            if (_connected)
                _CloseConnection();
            _serverIp = serverIp;
        }


        /// <summary>
        /// Changes the port number that the client is connected to. The client (if connected) is 
        /// disconnected when this method is called
        /// </summary>
        /// <param name="portNumber">The port number to change to</param>
        public void ChangePortNumber(int portNumber)
        {
            if (_connected)
                _CloseConnection();
            _portNumber = portNumber;
        }


        /// <summary>
        /// Connects the client to the server if not already connected
        /// </summary>
        public void Connect()
        {
            // Connect this client to the server
            if (!_connected)
                _InitializeConnection();
        }


        /// <summary>
        /// Disconnects the client from the server if connected
        /// </summary>
        public void Disconnect()
        {
            if (_connected)
                _CloseConnection("Disconnected successfully");
        }


        /// <summary>
        /// Connects the monitor client (this) to the monitor server
        /// </summary>
        private void _InitializeConnection()
        {
            // Start a new TCP connections to the monitor server
            _tcpServer = new TcpClient();

            IAsyncResult connectResult = _tcpServer.BeginConnect(_serverIp, _portNumber, null, null);
            WaitHandle wh = connectResult.AsyncWaitHandle;
            try
            {
                // if the connection takes too long to connect, throw an exception
                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(CONNECT_WAIT_TIME), false))
                {
                    _tcpServer.Close();
                    throw new SocketException(ERROR_CODE_SOCKET); // Couldn't connect. Throw error with SockectException error code
                }

                _tcpServer.EndConnect(connectResult); // Complete the connection
                _communication = new Communication(_tcpServer.GetStream()); // Get the stream so we can communicate on it
                _communication.Write(_userName); // Send the desired username to the server

                // Start the thread for receiving messages and further communication
                _thrMessaging = new Thread(new ThreadStart(_ReceiveMessages));
                _thrMessaging.Start();

                // Set that we are connected
                _connected = true;
            }
            catch (SocketException ex)
            {
                _UpdateMessage(ex.Message);
            }
            catch (IOException ex)
            {
                _UpdateMessage(ex.Message);
            }
            finally
            {
                wh.Close();
            }
        }


        /// <summary>
        /// Get a screenshot of the screen and send it to the server if connected
        /// </summary>
        public void Capture()
        {
            if (_connected)
            {
                // Get the screen shot
                Bitmap jpegImage = Screenshot.GetScreenshotPrimary(_imageQuality);
                Bitmap jpegImageResize = Screenshot.ResizeImage(jpegImage, _imageSize);

                // Send the image to the server
                _SendImage(jpegImageResize, 100);
            }
        }


        /// <summary>
        /// Writes an image to the stream that is connected to the server
        /// </summary>
        /// <param name="img">The image to send</param>
        /// <param name="quality">How much quality to keep in the image sent</param>
        private void _SendImage(Image img, int quality)
        {
            try
            {
                if (_tcpServer.Connected == true)
                    Screenshot.WriteJpegToStream(img, _tcpServer.GetStream(), quality);
                else
                    _CloseConnection(ERROR_CONNECTION_CLOSED_UNKNOWN);
            }
            catch (IOException ex)
            {
                if (ex.InnerException.Message == "An established connection was aborted by the software in your host machine")
                    _CloseConnection(ERROR_SERVER_COULD_NOT_BE_CONTACTED);
                else
                    _UpdateMessage(ex.Message);
            }
        }


        /// <summary>
        /// Receives the messages from the server and then uses the delegate to update the log
        /// </summary>
        // THE CODE BELOW USES SOME FORM METHODS. CHANGE THEM
        private void _ReceiveMessages()
        {
            string conResponse = null;
            Communication.DataType type;
            Communication.Command command = Communication.Command.NONE;

            type = _communication.Read();
            if(type == Communication.DataType.COMMAND)
            {
                command = _communication.CommandReceived;
                conResponse = _communication.MessageReceived;
            }


            // If the command was CONNECTION_SUCCESS, we connected successfully
            if (command == Communication.Command.CONNECTION_SUCCESS)
            {
                _UpdateConnectionState(ConnectionStateChangedEventArgs.State.CONNECTED); // Fire the event to show that we are connected
                _UpdateMessage("Connected successfully"); // Show that we connected sucessfully
            }
            else // If the command is CONNECTION_FAILED, the connection was unsuccessful
            {
                string reason = "Not Connected: " + conResponse;
                _UpdateConnectionState(ConnectionStateChangedEventArgs.State.DISCONNECTED); // Fire the event to show that we could not connect
                _CloseConnection(reason);
            }

            // While we are successfully connected, read incoming lines from the server
            while (_connected)
            {
                try
                {
                    // Show the messages in the log TextBox
                    //this.Invoke(new UpdateLogCallback(this.UpdateLog), srReceiver.ReadLine());
                }
                catch (IOException ex)
                {
                    // Display the message if it's not complaining that it was blocked. (This only seems to happen when we close the client)
                    if (ex.InnerException.Message != "A blocking operation was interrupted by a call to WSACancelBlockingCall" &&
                        ex.InnerException.Message != "An existing connection was forcibly closed by the remote host")
                        _UpdateMessage(ex.Message);
                }
                catch (ObjectDisposedException)
                {
                    // This occurs when we try to update the log and the form as been closed. Just ignore because the application is done.
                }
            }
        }


        /// <summary>
        /// Closes the connection with the server
        /// </summary>
        /// <param name="strReason">The reason the connection was closed</param>
        private void _CloseConnection(string strReason = null)
        {
            try
            {
                // Close the objects
                _connected = false;
                _communication.Close();
                _tcpServer.Close();

                // Fire the event to say that the connection is closed
                _UpdateConnectionState(ConnectionStateChangedEventArgs.State.DISCONNECTED);
                // Let us know why the connection was closed
                if(strReason != null)
                    _UpdateMessage(strReason);
            }
            catch (NullReferenceException)
            {
                // This seems to only happen if you disconnect and quit quickly. Since we're closing, it doesn't matter
            }
        }


        /// <summary>
        /// Returns the ip address of the local machine
        /// </summary>
        /// <returns>The first IPv4 address that it finds. Null if none are found</returns>
        private IPAddress _GetLocalIpAddress()
        {
            // Get a list of the ip address of this machine
            IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            for (int index = 0; index < ipAddresses.Length; ++index)
            {
                if (ipAddresses[index].AddressFamily == AddressFamily.InterNetwork)
                    return ipAddresses[index];
            }

            return null;
        }
        #endregion Methods


        /// <summary>
        /// Get the ip address of the server that this client is connected to
        /// </summary>
        public IPAddress IPAddress { get { return _serverIp; } }


        /// <summary>
        /// Get the port number of the server that this client is connected to
        /// </summary>
        public int PortNumber { get { return _portNumber; } }


        /// <summary>
        /// Return true if the client is connected to the server
        /// </summary>
        public bool Connected { get { return _connected; } }
    }
    #endregion Monitor Client
}
