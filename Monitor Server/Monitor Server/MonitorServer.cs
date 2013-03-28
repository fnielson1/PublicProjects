using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;
using System.Drawing;


namespace Monitor_Server
{
    #region Event Classes
    /// <summary>
    /// Holds the arguments for the StatusChanged event
    /// </summary>
    public class MessageChangedEventArgs : EventArgs
    {
        // The argument we're interested in is a message describing the event
        private string _message;


        /// <summary>
        /// Constructor for setting the event message
        /// </summary>
        /// <param name="message">The message in the event</param>
        public MessageChangedEventArgs(string message)
        {
            _message = message;
        }


        /// <summary>
        /// Property for retrieving the event message
        /// </summary>
        public string Message { get { return _message; } }

    }


    /// <summary>
    /// Holds the argument for the ImageChanged event
    /// </summary>
    public class ImageChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The username of the client that sent the image
        /// </summary>
        private string _userName;

        /// <summary>
        /// The argument we are interested in is an image that was sent from the client
        /// </summary>
        private Image _image;


        /// <summary>
        /// Constructor for setting the event image
        /// </summary>
        /// <param name="userName">The username of the client that sent the image</param>
        /// <param name="img">The image to send</param>
        public ImageChangedEventArgs(string userName, Image img)
        {
            _userName = userName;
            _image = img;
        }

        /// <summary>
        /// Property for retrieving the username of the client that sent the image
        /// </summary>
        public string UserName { get { return _userName; } }


        /// <summary>
        /// Property for retrieving the event image
        /// </summary>
        public Image ImageScreenshot { get { return _image; } }

    }


    /// <summary>
    /// Holds the arguments for the UserChanged Event
    /// </summary>
    public class ClientStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Specify whether the user connected, disconnected, etc
        /// </summary>
        public enum ClientStatus
        {
            CONNECTED,
            DISCONNECTED
        }


        /// <summary>
        /// The name of the client
        /// </summary>
        private string _userName;


        /// <summary>
        /// What action the client did (connect, disconnect, etc)
        /// </summary>
        private ClientStatus _status;


        /// <summary>
        /// Constructor for setting up the event
        /// </summary>
        /// <param name="userName">The name of the client</param>
        /// <param name="status">What action the user did (connect, disconnect, etc)</param>
        public ClientStatusEventArgs(string userName, ClientStatus status)
        {
            _userName = userName;
            _status = status;
        }


        /// <summary>
        /// Property for retrieving the name of the user
        /// </summary>
        public string UserName { get { return _userName; } }


        /// <summary>
        /// Property for retrieving the action the user performed (connect, disconnect, etc)
        /// </summary>
        public ClientStatus Status { get { return _status; } }
    }
    #endregion Event Classes


    class MonitorServer
    {
        /// <summary>
        /// This delegate is needed to specify the parameters we're passing with our status changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that contains the message from the status change</param>
        public delegate void StatusChangedEventHandler(object sender, MessageChangedEventArgs e);
        /// <summary>
        /// This delegate is needed to specify the parameters we're passing with our image changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that contains the image from the image change</param>
        public delegate void ImageChangedEventHandler(object sender, ImageChangedEventArgs e);
        /// <summary>
        /// This delegate is needed to specify the parameters we're passing with our user action event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The object that contains the tcp client's username and action</param>
        public delegate void ClientActionEventHandler(object sender, ClientStatusEventArgs e);


        /// <summary>
        /// The event that will notify the form when a user has sent a message
        /// </summary>
        public static event StatusChangedEventHandler StatusChanged;
        /// <summary>
        /// The object that will contain the message for when we send a message 
        /// </summary>
        private static MessageChangedEventArgs _statusEvent;

        /// <summary>
        /// The event that will notify the form when a user has sent an image
        /// </summary>
        public static event ImageChangedEventHandler ImageChanged;
        /// <summary>
        /// The object that will contain the image that is sent
        /// </summary>
        private static ImageChangedEventArgs _imageEvent;

        /// <summary>
        /// The event that will notify the form when a client has performed an action (connect, disconnect, etc)
        /// </summary>
        public static event ClientActionEventHandler ClientAction;
        /// <summary>
        /// The object that will contain the client's username and action
        /// </summary>
        private static ClientStatusEventArgs _clientActionEvent;


        /// <summary>
        /// This hash table stores users and connections (browsable by user)
        /// </summary>
        public static Hashtable htUsers;

        /// <summary>
        /// This hash table stores connections and users (browsable by connection)
        /// </summary>
        public static Hashtable htConnections;

        /// <summary>
        /// The maximum number of users that we can have at one time
        /// </summary>
        private static int _maxNumUsers;

        /// <summary>
        /// Will tell the while loop to keep monitoring for connections
        /// </summary>
        private static bool _serverRunning = false;


        /// <summary>
        /// The folder that has all the images
        /// </summary>
        private const string IMAGE_FOLDER = "images";


        /// <summary>
        /// The ip address for the server to listen on 
        /// </summary>
        private IPAddress _ipAddress;
        /// <summary>
        /// The port number for the server to listen on
        /// </summary>
        private int _portNumber;
        /// <summary>
        /// The client object that is used to handle the connection once it occurs
        /// </summary>
        private TcpClient _tcpClient;

        /// <summary>
        /// The thread that will hold the connection listener
        /// </summary>
        private Thread _thrListener;

        /// <summary>
        /// The TCPListener object that listens for connections
        /// </summary>
        private TcpListener _tlsClient;



        /// <summary>
        /// The constructor sets the IP address to the one specified
        /// </summary>
        /// <param name="ipAddress">The ip address for the server to listen on (the local ip should be used)</param>
        /// <param name="portNumber">The port number to connect at</param>
        /// <param name="maxNumUsers">The maximum number of users that can connect to this server</param>
        public MonitorServer(IPAddress ipAddress, int portNumber = 8080, int maxNumUsers = 30)
        {
            _ipAddress = ipAddress;
            _portNumber = portNumber;
            _maxNumUsers = maxNumUsers;
            htUsers = new Hashtable(_maxNumUsers); // Create a hash table with a maximum allowed users
            htConnections = new Hashtable(_maxNumUsers);
        }


        #region Event Methods
        /// <summary>
        /// This is called when we want to raise the StatusChanged event
        /// </summary>
        /// <param name="e">The StatusChangedEventArgs object that has the message inside it</param>
        public static void OnStatusChanged(MessageChangedEventArgs e)
        {
            // Set statusHandler to equal StatusChanged as it is the delegate that points to
            // the methods to call (if any)
            StatusChangedEventHandler statusHandler = StatusChanged;

            if (statusHandler != null)
            {
                // Invoke the delegate
                statusHandler(null, e);
            }
        }


        /// <summary>
        /// This is called when we want to raise the ImageChanged event
        /// </summary>
        /// <param name="e">The ImageChangedEventArgs object that has the image inside it</param>
        public static void OnImageChanged(ImageChangedEventArgs e)
        {
            ImageChangedEventHandler imageHandler = ImageChanged;

            if (imageHandler != null)
            {
                // Invoke the delegate
                imageHandler(null, e);
            }
        }


        /// <summary>
        /// Update the image on our application that came from the client and send it to other 
        /// clients that should have it
        /// </summary>
        /// <param name="userName">The name of the client</param>
        /// <param name="img">The image that came from the client</param>
        public static void UpdateImage(string userName, Bitmap img)
        {
            // Write the image to a file
            _FileWriteImage(userName, img);

            // Update the image in our application
            _imageEvent = new ImageChangedEventArgs(userName, img);
            OnImageChanged(_imageEvent);
        }


        /// <summary>
        /// Writes the images sent to a file 
        /// </summary>
        /// <param name="userName">The name of the user that sent the file</param>
        /// <param name="img">The image to write</param>
        private static void _FileWriteImage(string userName, Bitmap img)
        {
            string userFilePath = _MakeValidFileName(userName);
            string datePatt = @"M-d-yyyy";
            string timePatt = @"hh.mm.ss";
            string imageName = DateTime.Now.ToString(timePatt); // The image name should be the time it was created

            // Create the directory path
            userFilePath = Path.Combine(IMAGE_FOLDER, userFilePath);
            userFilePath = Path.Combine(userFilePath, DateTime.Now.ToString(datePatt));
            Directory.CreateDirectory(userFilePath); // Create the necessary directories (if they don't already exist)

            // Finally, add the file name to it
            userFilePath = Path.Combine(userFilePath, imageName);
            userFilePath += ".jpeg";

            img.Save(userFilePath);
        }


        /// <summary>
        /// Takes a string and removes all invalid characters
        /// </summary>
        /// <param name="value">The string to work on</param>
        /// <returns>The fix string</returns>
        private static string _MakeValidFileName(string value)
        {
            StringBuilder sb = new StringBuilder(value);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char item in invalid)
            {
                sb.Replace(item.ToString(), "");
            }
            return sb.ToString();
        }




        /// <summary>
        /// This is called when we want to raise the ClientAction event
        /// </summary>
        /// <param name="e">The object that has the user's action inside it</param>
        public static void OnClientAction(ClientStatusEventArgs e)
        {
            ClientActionEventHandler clientActionHandler = ClientAction;

            if (clientActionHandler != null)
                clientActionHandler(null, e);
        }


        /// <summary>
        /// Update the action that the client (user) performed (connect, disconnect, etc).
        /// </summary>
        /// <param name="userName">The name of the client</param>
        /// <param name="action">The action that client performed</param>
        public static void UpdateClientAction(string userName, ClientStatusEventArgs.ClientStatus action)
        {
            _clientActionEvent = new ClientStatusEventArgs(userName, action);
            OnClientAction(_clientActionEvent);
        }
        #endregion Event Methods


        #region Methods
        /// <summary>
        /// Changes the ip address that the server is listening on. The server is 
        /// disconnected when this method is called
        /// </summary>
        /// <param name="ipAddress">The IP address to change to</param>
        public void ChangeIPAdress(IPAddress ipAddress)
        {
            if (_serverRunning)
                RequestStop(); // Can't change the ip address while running
            _ipAddress = ipAddress;
        }


        /// <summary>
        /// Changes the port number that the server is listening on. The server is 
        /// disconnected when this method is called
        /// </summary>
        /// <param name="portNumber">The Port number to change to</param>
        public void ChangePortNumber(int portNumber)
        {
            if (_serverRunning)
                RequestStop(); // Can't change the port number while running
            _portNumber = portNumber;
        }


        /// <summary>
        /// Add the user to the hash tables
        /// </summary>
        /// <param name="tcpUser">The tcp object of the user</param>
        /// <param name="strUsername">The user name of the user</param>
        public static void AddUser(TcpClient tcpUser, string strUsername)
        {
            // First add the username and associated connection to both hash tables
            MonitorServer.htUsers.Add(strUsername, tcpUser);
            MonitorServer.htConnections.Add(tcpUser, strUsername);

            UpdateClientAction(strUsername, ClientStatusEventArgs.ClientStatus.CONNECTED); // Fire the event saying the client has connected
            SendAdminMessage(htConnections[tcpUser] + " has joined us"); // Tell of the new connection to all other users and to the server form
        }



        /// <summary>
        /// Remove the user from the hash tables
        /// </summary>
        /// <param name="tcpUser">The tcp object of the user</param>
        public static void RemoveUser(TcpClient tcpUser)
        {
            if (_serverRunning == false)
                return;

            // If the user is there
            if (htConnections[tcpUser] != null)
            {
                string userName = MonitorServer.htConnections[tcpUser].ToString();
                tcpUser.Close(); // Close the connection
                // Remove the user from the hash tables
                MonitorServer.htUsers.Remove(MonitorServer.htConnections[tcpUser]);
                MonitorServer.htConnections.Remove(tcpUser);

                UpdateClientAction(userName, ClientStatusEventArgs.ClientStatus.DISCONNECTED); // Fire the event saying the client has disconnected
                SendAdminMessage(userName + " has left us"); // Finally, show the information and tell the other users about the disconnection
            }
        }


        /// <summary>
        /// Send administrative messages
        /// </summary>
        /// <param name="Message">The message to send</param>
        public static void SendAdminMessage(string Message)
        {
            StreamWriter swSenderSender;

            // First of all, show in our application who says what
            _statusEvent = new MessageChangedEventArgs("Administrator: " + Message);
            OnStatusChanged(_statusEvent);


            // Create an array of TCP clients, the size of the number of users we have
            TcpClient[] tcpClients = new TcpClient[MonitorServer.htUsers.Count];

            // Copy the TcpClient objects into the array
            MonitorServer.htUsers.Values.CopyTo(tcpClients, 0);

            // Loop through the list of TCP clients
            for (int i = 0; i < tcpClients.Length; i++)
            {
                // Try sending a message to each
                try
                {
                    // If the message is blank or the connection is null, break out
                    if (Message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }

                    // Send the message to the current user in the loop
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine("Administrator: " + Message);
                    swSenderSender.Flush();
                    swSenderSender = null;

                }
                catch // If there was a problem, the user is not there anymore, remove him
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }


        /// <summary>
        /// Send messages from one user to all the others
        /// </summary>
        /// <param name="From">The user name of the user who sent the message</param>
        /// <param name="Message">The message that the user sent</param>
        public static void SendMessage(string From, string Message)
        {
            StreamWriter swSenderSender;

            // First of all, show in our application who says what
            _statusEvent = new MessageChangedEventArgs(From + " says: " + Message);
            OnStatusChanged(_statusEvent);



            // Create an array of TCP clients, the size of the number of users we have
            TcpClient[] tcpClients = new TcpClient[MonitorServer.htUsers.Count];

            // Copy the TcpClient objects into the array
            MonitorServer.htUsers.Values.CopyTo(tcpClients, 0);

            // Loop through the list of TCP clients
            for (int i = 0; i < tcpClients.Length; i++)
            {
                // Try sending a message to each
                try
                {
                    // If the message is blank or the connection is null, break out
                    if (Message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }

                    // Send the message to the current user in the loop
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine(From + " says: " + Message);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch // If there was a problem, the user is not there anymore, remove him
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }


        /// <summary>
        /// Starts the server listening for clients
        /// </summary>
        public void StartListening()
        {
            try
            {
                // Get the IP of the address to listen to (should be the local one)
                IPAddress ipaLocal = _ipAddress;

                // Create the TCP listener object using the IP of the server and the specified port
                _tlsClient = new TcpListener(ipaLocal, _portNumber);

                // Start the TCP listener and listen for connections
                _tlsClient.Start();

                // The while loop will check for true in this before checking for connections
                _serverRunning = true;


                // Start the new tread that hosts the listener
                _thrListener = new Thread(_KeepListening);
                _thrListener.Start();
            }
            catch (SocketException ex)
            {
                SendAdminMessage(ex.Message);
            }
        }


        /// <summary>
        /// Keep the server listening for clients
        /// </summary>
        private void _KeepListening()
        {
            // While the server is running
            while (_serverRunning == true)
            {
                try
                {
                    // Accept a pending connection
                    _tcpClient = _tlsClient.AcceptTcpClient();

                    // Create a new instance of Connection
                    Connection newConnection = new Connection(_tcpClient);
                }
                catch (SocketException ex)
                {
                    if (ex.Message != "A blocking operation was interrupted by a call to WSACancelBlockingCall")
                        SendAdminMessage(ex.Message);
                }
            }
        }


        /// <summary>
        /// Stops the server. Clearing all users from it.
        /// </summary>
        public void RequestStop()
        {
            _serverRunning = false; // Say that we're not running
            // Stop the listeners
            if(_tlsClient != null)
                _tlsClient.Stop();
            if (_thrListener != null)
            _thrListener.Abort();

            // Create an array of TCP clients, the size of the number of users we have
            TcpClient[] tcpClients = new TcpClient[MonitorServer.htUsers.Count];

            // Copy the TcpClient objects into the array
            MonitorServer.htUsers.Values.CopyTo(tcpClients, 0);

            // Now, close the connections and remove the users from our hash tables
            foreach (TcpClient tcpUser in tcpClients)
            {
                // Close the connection, remove the user from the hash table, and fire an event saying the client isn't connected
                string userName = MonitorServer.htConnections[tcpUser].ToString();
                tcpUser.Close();
                MonitorServer.htUsers.Remove(MonitorServer.htConnections[tcpUser]);
                MonitorServer.htConnections.Remove(tcpUser);
                UpdateClientAction(userName, ClientStatusEventArgs.ClientStatus.DISCONNECTED); // Fire the event saying the client isn't connected (just removed him)
            }
        }
        #endregion Methods


        /// <summary>
        /// Get the ip address of the server that this client is connected to
        /// </summary>
        public IPAddress IPAddress { get { return _ipAddress; } }


        /// <summary>
        /// Get the port number of the server that this client is connected to
        /// </summary>
        public int PortNumber { get { return _portNumber; } }


        /// <summary>
        /// A read-only property that specifies if the server is listening for connections or not
        /// </summary>
        public bool Running { get { return _serverRunning; } }
    }
}

