using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Drawing;
using MonitorCommunication;


namespace Monitor_Server
{
    class Connection
    {
        private TcpClient _tcpClient;

        // The thread that will send information to the client
        private Thread _thrSender;
        private StreamReader _srReceiver;
        private StreamWriter _swSender;
        private string _currUser;

        /// <summary>
        /// The jpeg image that is created from the memStream
        /// </summary>
        private Bitmap _jpegImage;
        /// <summary>
        /// The object that handles the communication with the client
        /// </summary>
        private Communication _communication;


        /// <summary>
        /// The constructor of the class takes in a TCP connection
        /// </summary>
        /// <param name="tcpCon">The tcp object that has our connection</param>
        public Connection(TcpClient tcpCon)
        {
            _tcpClient = tcpCon;
            // The thread that accepts the client and awaits messages
            _thrSender = new Thread(_AcceptClient);

            // The thread calls the AcceptClient() method
            _thrSender.Start();
        }


        /// <summary>
        /// Closes the connection to the client
        /// </summary>
        private void _CloseConnection()
        {
            // Close the currently open objects
            _tcpClient.Close();
            _srReceiver.Close();
            _swSender.Close();
        }


        /// <summary>
        /// Occures when a new client is accepted
        /// </summary>
        private void _AcceptClient()
        {
            try
            {
                _communication = new Communication(_tcpClient.GetStream());
                _srReceiver = new System.IO.StreamReader(_tcpClient.GetStream());
                _swSender = new System.IO.StreamWriter(_tcpClient.GetStream());

                // Read the account information from the client
                Communication.DataType type = _communication.Read();
                if (type == Communication.DataType.MESSAGE)
                    _currUser = _communication.MessageReceived;


                // We got a response from the client
                if (_currUser != "" || _currUser == null)
                {
                    // Store the user name in the hash table
                    if (MonitorServer.htUsers.Contains(_currUser) == true)
                    {
                        // CONNECTION_FAILED means not connected
                        _communication.Write(Communication.Command.CONNECTION_FAILED, "This username already exists.");
                        _CloseConnection();
                        return;
                    }
                    else if (_currUser.ToLower() == "administrator")
                    {
                        // CONNECTION_FAILED means not connected
                        _communication.Write(Communication.Command.CONNECTION_FAILED, "This username is reserved.");
                        _CloseConnection();
                        return;
                    }
                    else
                    {
                        // Connected successfully
                        _communication.Write(Communication.Command.CONNECTION_SUCCESS); // Change to Command later

                        // Add the user to the hash tables and start listening for messages from him
                        MonitorServer.AddUser(_tcpClient, _currUser);
                    }
                }
                else
                {
                    _communication.Write(Communication.Command.CONNECTION_FAILED, "You must provide a user name for this connection");
                    _CloseConnection();
                    return;
                }

                // Read the data from the client
                _ReadStream(_currUser);
            }
            catch (IOException)
            {
                _CloseConnection(); // Something happened and we lost the connection (close the streams)
            }
        }


        /// <summary>
        /// Once the client is accepted, continue to receive images that the client sends
        /// </summary>
        /// <param name="currUser">The name of the user that is connected</param>
        private void _ReadStream(string currUser)
        {
            try
            {
                Communication.DataType type;
                while ((type = _communication.Read()) != Communication.DataType.NONE)
                {
                    if (type == Communication.DataType.IMAGE)
                    {
                        _jpegImage = (Bitmap)_communication.ImageReceived;
                        MonitorServer.SendMessage(currUser, "Got the image");
                        MonitorServer.UpdateImage(currUser, _jpegImage);
                    }
                }
            }
            catch (IOException)
            {
                // If anything went wrong with this user, disconnect him
                MonitorServer.RemoveUser(_tcpClient);
            }
            catch (SocketException)
            {
                MonitorServer.RemoveUser(_tcpClient);
            }
        }
    }
}
