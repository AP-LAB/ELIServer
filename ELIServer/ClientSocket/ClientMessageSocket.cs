using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ELIServer.Messaging
{
    /// <summary>
    /// \brief A class that communicates with a TcpClient object.
    /// 
    /// This class is used as a wrapper for a TcpClient object.
    /// It also represents a connected user and the client that the user is using.
    /// The object contains the following global variables:
    /// - innerClient: the TcpClient to communicate with.
    /// - clientID: the ID of the client (table).
    /// - userID: the ID of the connected user.
    /// </summary>
    public class ClientMessageSocket
    {
        private TcpClient innerClient;  //!< The inner TcpClient client.
        public String clientID;         //!< The ID of the connected client/table.
        private String userID;           //!< The ID of the connected user.
        public bool isCalling = false;  //!< An boolean that represents the call state of the client.

        /// <summary>
        /// \brief Create a new instance of ClientMessageSocket.
        /// 
        /// When the ClientMessageSocket object is created, an intitial message will be requested.
        /// This message is handled in the GetInitialMessage() method.
        /// After the intitial message is handled, all incoming messages will be handled using the HandleIncomingMessagesFromClient() method.
        /// </summary>
        /// <param name="client">The TcpClient that will be the inner TcpClient.</param>
        public ClientMessageSocket(TcpClient client)
        {
            innerClient = client;
            // Get the initial message.
            GetInitialMessage();
            //Handle all incoming messages from this point.
            new Thread(new ThreadStart(HandleIncomingMessagesFromClient)).Start(); ;
        }

        /// <summary>
        /// \brief Handle the first messages that is send to the innerClient object.
        /// 
        /// Get the initial message that is send to the innerClient object.
        /// The clientID and userID will be gathered from this message.
        /// Note: Make sure the initial message contains the "ClientID"(clientID) and "ID" (userID) tokens and values.    
        /// </summary>
        public void GetInitialMessage(){
            // When the client arrives, the id is unknown.
            // The first message send should contain an "ClientID".
            // This ID is the clientID.
            String initialMessage = GetStringFromNetworkStream(innerClient.GetStream());
            dynamic deserializedMessage = JsonConvert.DeserializeObject(initialMessage);
            clientID = deserializedMessage.ClientID;
            userID = deserializedMessage.ID;
        }

        /// <summary>
        /// \brief Handle all incoming messages for this client.
        /// 
        /// This is done using the static MessageHandler.HandleIncomingJsonMessage() method.
        /// When the clients' connection closes, the client will be remove from the connected clients list in the MessageSocketManager.
        /// This method is called in the constructor method of ClientMessageSocket.
        /// </summary>
        private void HandleIncomingMessagesFromClient()
        {
            while (innerClient.Connected && IsConnected() && !isCalling)
            {
                var stream = innerClient.GetStream();
                if (stream.DataAvailable)
                {
                    MessageHandler.HandleIncomingJsonMessage(this, GetStringFromNetworkStream(stream));
                }                
            }

            // When the connection is closed, remove this client from  the list in MessageSocketManager.
            MessageSocketManager.RemoveClientFromConnectedSockets(this);
        }

        /// <summary>
        /// \brief Check the connection to the innerClient.
        /// 
        /// The check is done using the Poll() method.
        /// </summary>
        /// <returns>If the client is connected, true. Otherwise, false.</returns>
        internal bool IsConnected()
        {
            bool connected = true;
            try
            {
                // Detect if client disconnected
                if (innerClient.Client.Poll(0, SelectMode.SelectRead))
                {

                    byte[] buffer = new byte[1];
                    if (innerClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        connected = false;
                    }
                }
            }
            catch(Exception ex)
            {
                connected = false;
            }

            return connected;
        }

        /// <summary>
        /// A getter for the inner TcpClient.
        /// </summary>
        /// <returns>The inner TcpClient.</returns>
        internal TcpClient GetInnerClient()
        {
            return innerClient;
        }

        /// <summary>
        /// \brief Recieve a message from the network stream.
        /// 
        /// The first 4 element of the string should contain the size of the following bytes.
        /// The method will hang if te size specified is bigger than the real size, because it waits for all bytes.
        /// The size is in little endian order and the message is encoded in UTF8.
        /// </summary>
        /// <param name="stream">The stream to gather data from.</param>
        /// <returns>The string that follows the 4 bytes, thus the message send.</returns>
        private String GetStringFromNetworkStream(NetworkStream stream)
        {
        
            ///TODO if the system is big endian, reverse
            
            // The first 4 bytes are the size of the string that will follow.
            byte[] sizeRead = new byte[4];
            stream.Read(sizeRead, 0, 4);
            sizeRead = ReverseByteArrayIfNotLittleEndian(sizeRead);
            // Convert the read byte array to the size of the following string
            int size = BitConverter.ToInt32(sizeRead, 0);

            // Read the string that will follow with a byte[] that has the same size as the size gathered.
            byte[] bytesReceived = new byte[size];
            stream.Read(bytesReceived, 0, size);
            bytesReceived = ReverseByteArrayIfNotLittleEndian(bytesReceived);
            
            // Convert the byte[] to string and return it.
            return Encoding.UTF8.GetString(bytesReceived);
        }

        /// <summary>
        /// This methods reverses the input array if the byte order of the current system is not little endian.
        /// If the byte order is little endian, the original array is returend.
        /// </summary>
        /// <param name="array">The array to be reversed.</param>
        /// <returns></returns>
        private byte[] ReverseByteArrayIfNotLittleEndian(byte[] array)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return array.Reverse().ToArray();
            }
            else
            {
                return array;
            }
        }

        /// <summary>
        /// \brief Send a message to the client of this object.
        /// 
        /// The first 4 bytes will contain the lenght of the message.
        /// The size is in little endian order and the message is encoded in UTF8.
        /// </summary>
        /// <param name="message">The message to send to the client.</param>
        public void SendMessage(String message)
        {
            // The byte array for the message
            byte[] messageByteArray = Encoding.UTF8.GetBytes(message);
            // The byte array that represents the size of the messageByteArray
            byte[] sizeBytesArray = BitConverter.GetBytes((uint)messageByteArray.Length);
            //The byte array to send
            byte[] returnArray = new byte[sizeBytesArray.Length + messageByteArray.Length];
            //Copy both sizeBytesArray and messageByteArray to returnArray
            System.Buffer.BlockCopy(sizeBytesArray, 0, returnArray, 0, sizeBytesArray.Length);
            System.Buffer.BlockCopy(messageByteArray, 0, returnArray, 4, messageByteArray.Length);
            //Write the returnArray to the client

            byte[] reversedArray = ReverseByteArrayIfNotLittleEndian(returnArray);
            innerClient.GetStream().Write(returnArray, 0, reversedArray.Length);
        }
        
    }
}
