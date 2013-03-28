using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Drawing;

namespace MonitorCommunication
{
    /// <summary>
    /// A class that handles the stream communication between the client and the server.
    /// Allows us to know if a command is being received, or an image, or a message, etc
    /// </summary>
    public class Communication
    {
        /// <summary>
        /// An enum that holds various commands that can be sent to the client
        /// (such as CHANGEIP, RESTART, etc)
        /// </summary>
        public enum Command
        {
            NONE,
            CONNECTION_SUCCESS,
            CONNECTION_FAILED
        }

        /// <summary>
        /// An enum that tells what type of data is being sent or received
        /// in the stream
        /// </summary>
        public enum DataType
        {
            NONE,
            COMMAND,
            MESSAGE,
            IMAGE
        }

        /// <summary>
        /// Error message for when the stream to the client cannot be read
        /// </summary>
        private const string ERROR_STREAM_READ = "The stream cannot be read as it was closed or another error happened";
        /// <summary>
        /// How many bytes are used to specify the type of data being sent 
        /// </summary>
        private const int DATA_TYPE_BYTE_COUNT = 4; // Seems like BitConverter always returns 4 bytes
        /// <summary>
        /// How many bytes are used to specify the number of bytes to read from the stream
        /// </summary>
        private const int STREAM_BYTE_COUNT = 4;
        /// <summary>
        /// How many bytes are used in a Command
        /// </summary>
        private const int COMMAND_BYTE_COUNT = 4;
        /// <summary>
        /// The number of bytes to get from the stream with each read 
        /// </summary>
        private const int READ_BLOCK_SIZE = 1;

        /// <summary>
        /// The stream that points to the client's stream
        /// </summary>
        private Stream _stream;
        /// <summary>
        /// The encoding to use on our strings
        /// </summary>
        private Encoding _encoding = new ASCIIEncoding();

        /// <summary>
        /// The byte array that holds the value that specifies what type of data is being sent
        /// or read
        /// </summary>
        private byte[] _dataTypeByte;
        /// <summary>
        /// The byte array that holds the value that specifies the number of bytes to send
        /// or read
        /// </summary>
        private byte[] _dataCountByte;
        /// <summary>
        /// The bytes that are read from the stream
        /// </summary>
        private byte[] _clientBuffer = new byte[READ_BLOCK_SIZE];

        /// <summary>
        /// The stream that contains the image which is read from the client stream
        /// </summary>
        private MemoryStream _imageStream;

        /// <summary>
        /// Holds the command that was received
        /// </summary>
        private Command _command = Command.NONE;
        /// <summary>
        /// Holds the message that was received
        /// </summary>
        private string _message = null;
        /// <summary>
        /// Holds the image that was received
        /// </summary>
        private Image _image = null;



        /// <summary>
        /// Takes a stream and handles the communication on the stream
        /// </summary>
        /// <param name="stream">The stream to communicate on</param>
        public Communication(Stream stream)
        {
            _stream = stream;
        }


        /// <summary>
        /// Closes the connection to the client
        /// </summary>
        public void Close()
        {
            // Close the currently open objects
            _stream.Close();
        }



        /// <summary>
        /// Writes a string to the stream using the defined protocals (DataType
        /// is DataType.MESSAGE)
        /// </summary>
        /// <param name="str">The string to write</param>
        public void Write(string str)
        {
            Write(_encoding.GetBytes(str), DataType.MESSAGE);
        }


        /// <summary>
        /// Writes a Command to the stream using the defined protocals (DataTpe
        /// is DataType.COMMAND)
        /// </summary>
        /// <param name="cmd">The Command to write</param>
        /// <param name="str">An optional string to write to the stream</param>
        public void Write(Command cmd, string str = null)
        {
            byte[] bufferCmd = BitConverter.GetBytes((int)cmd);
            byte[] bufferStr;
            byte[] buffer;

            // If the string is not null, add it to the buffer
            if (str != null)
            {
                bufferStr = _encoding.GetBytes(str);
                buffer = new byte[bufferCmd.Length + bufferStr.Length];
                Buffer.BlockCopy(bufferStr, 0, buffer, COMMAND_BYTE_COUNT, bufferStr.Length);
            }
            else
                buffer = new byte[bufferCmd.Length];

            Buffer.BlockCopy(bufferCmd, 0, buffer, 0, bufferCmd.Length);
            Write(buffer, DataType.COMMAND);
        }


        /// <summary>
        /// Writes a byte array to the stream using the defined protocals
        /// </summary>
        /// <param name="dataBuffer">The byte array to write to the stream</param>
        /// <param name="type">The type of data that is being written</param>
        public void Write(byte[] dataBuffer, DataType type)
        {
            try
            {
                if (type == DataType.NONE)
                    throw new ArgumentException("Cannot write a NONE enum type to the stream", "type");

                byte[] buffer = new byte[DATA_TYPE_BYTE_COUNT + STREAM_BYTE_COUNT + dataBuffer.Length];  // The byte buffer for sending the data

                _dataTypeByte = BitConverter.GetBytes((int)type);                       // Get the type of data we're sending
                _dataCountByte = BitConverter.GetBytes(dataBuffer.Length);              // Get the number of bytes needed to send the image
                Buffer.BlockCopy(_dataTypeByte, 0, buffer, 0, DATA_TYPE_BYTE_COUNT);    // Copy the type of data
                Buffer.BlockCopy(_dataCountByte, 0, buffer, DATA_TYPE_BYTE_COUNT, STREAM_BYTE_COUNT);  // Copy the stream length
                Buffer.BlockCopy(dataBuffer, 0, buffer, DATA_TYPE_BYTE_COUNT + STREAM_BYTE_COUNT, dataBuffer.Length); // Copy the data itself

                _stream.Write(buffer, 0, buffer.Length);  // Write the bytes to a stream
                _stream.Flush();
            }
            catch (SocketException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
        }


        /// <summary>
        /// Reads the data from the client's stream (if any) and returns what the data's
        /// type was
        /// </summary>
        /// <returns>The type of data that was received</returns>
        public DataType Read()
        {
            try
            {
                _dataTypeByte = new byte[DATA_TYPE_BYTE_COUNT];
                if (_stream.Read(_dataTypeByte, 0, DATA_TYPE_BYTE_COUNT) != 0)
                {
                    byte[] buffer;
                    DataType type;
                    int dataCount;

                    _dataCountByte = new byte[STREAM_BYTE_COUNT];

                    type = (DataType)BitConverter.ToInt32(_dataTypeByte, 0); // Get the data type
                    _stream.Read(_dataCountByte, 0, STREAM_BYTE_COUNT);
                    dataCount = BitConverter.ToInt32(_dataCountByte, 0);

                    // Check what kind of data has been sent, and read it accordingly
                    switch (type)
                    {
                        case DataType.COMMAND:
                            buffer = _ReadCommand(dataCount);
                            _SetCommand(buffer);
                            break;
                        case DataType.MESSAGE:
                            buffer = _Read(dataCount);
                            _SetMessage(buffer);
                            break;
                        case DataType.IMAGE:
                            buffer = _Read(dataCount);
                            _SetImage(buffer, dataCount);
                            break;
                    }

                    return type;
                }
                return DataType.NONE; // Shouldn't ever hit here, but must return a value
            }
            catch (SocketException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
        }


        /// <summary>
        /// Reads the stream into the buffer and returns it
        /// </summary>
        /// <param name="count">The amount of data to write</param>
        /// <returns>The data that was read in a byte array</returns>
        private byte[] _Read(int count)
        {
            byte[] buffer = new byte[count];

            // Read the data READ_BLOCK_SIZE byte(s) at a time
            for (int index = 0; index < count; index += READ_BLOCK_SIZE)
            {
                if (!_stream.CanRead)
                    throw new IOException(ERROR_STREAM_READ); // If we cant read the stream, throw an IO error

                _stream.Read(_clientBuffer, 0, READ_BLOCK_SIZE); // Copy from the network stream into the buffer
                Buffer.BlockCopy(_clientBuffer, 0, buffer, index, READ_BLOCK_SIZE); // Copy buffer and append it to the buffer that will have all the data
            }

            return buffer;
        }


        /// <summary>
        /// Reads the command into the buffer, and then reads the string into a new buffer
        /// and sets the message
        /// </summary>
        /// <param name="count">The amount of data to write</param>
        /// <returns>The data that was read in a byte array</returns>
        private byte[] _ReadCommand(int count)
        {
            byte[] cmdBuffer = new byte[COMMAND_BYTE_COUNT]; // The command can be only this many bytes long

            // Read the data READ_BLOCK_SIZE byte(s) at a time
            for (int index = 0; index < COMMAND_BYTE_COUNT; index += READ_BLOCK_SIZE)
            {
                _stream.Read(_clientBuffer, 0, READ_BLOCK_SIZE); // Copy from the network stream into the buffer
                Buffer.BlockCopy(_clientBuffer, 0, cmdBuffer, index, READ_BLOCK_SIZE); // Copy buffer and append it to the buffer that will have all the data
            }

            // If a string was sent as well, read it
            if (count - COMMAND_BYTE_COUNT > 0)
            {
                int strCount = count - COMMAND_BYTE_COUNT;
                byte[] strBuffer = new byte[strCount]; // A buffer large enough to hold the string 

                for (int index = 0; index < strCount; index += READ_BLOCK_SIZE)
                {
                    _stream.Read(_clientBuffer, 0, READ_BLOCK_SIZE); // Copy from the network stream into the buffer
                    Buffer.BlockCopy(_clientBuffer, 0, strBuffer, index, READ_BLOCK_SIZE); // Copy buffer and append it to the buffer that will have all the data
                }
                _SetMessage(strBuffer); // Set the message that was sent
            }

            return cmdBuffer;
        }


        /// <summary>
        /// Takes a byte array and converts it to the command enum and sets the variable
        /// </summary>
        /// <param name="buffer">The byte array that has the command</param>
        private void _SetCommand(byte[] buffer)
        {
            _command = (Command)BitConverter.ToInt32(buffer, 0);
        }


        /// <summary>
        /// Takes a byte array and converts it to a string and sets the variable
        /// </summary>
        /// <param name="buffer">The byte array that has the string</param>
        private void _SetMessage(byte[] buffer)
        {
            _message = _encoding.GetString(buffer);
        }


        /// <summary>
        /// Takes a byte array and writes it to a memory stream and then converts
        /// it to an image and sets the variable
        /// </summary>
        /// <param name="buffer">The byte array that has the image</param>
        /// <param name="count">The number of bytes to read</param>
        private void _SetImage(byte[] buffer, int count)
        {
            _imageStream = new MemoryStream();
            _imageStream.Write(buffer, 0, count);

            // If the stream isn't empty, create an image from it
            if (_imageStream.Position > 0)
                _image = new Bitmap(_imageStream); // Using the memory stream, create an image
        }


        /// <summary>
        /// Get the command that was received from the client (Command.NONE if no command was received or the previous command)
        /// </summary>
        public Command CommandReceived { get { return _command; } }


        /// <summary>
        /// Get the message that was received from the server (null if no message was received or the previous message)
        /// </summary>
        public string MessageReceived { get { return _message; } }


        /// <summary>
        /// Get the image that was received from the client (null if no image was received or the previous image)
        /// </summary>
        public Image ImageReceived { get { return _image; } }

    }
}
