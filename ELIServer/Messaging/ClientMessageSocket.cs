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
    public class ClientMessageSocket
    {
        private TcpClient innerClient;
        public String clientID;
        public String userID;

        public ClientMessageSocket(TcpClient client)
        {
            innerClient = client;
            // When the client arrives, the id is unknown.
            // The first message send should contain an "ClientID".
            // This ID is the clientID
            String initialMessage = GetStringFromNetworkStream(client.GetStream());
            dynamic deserializedMessage = JsonConvert.DeserializeObject(initialMessage);
            clientID = deserializedMessage.ClientID;
            userID = deserializedMessage.ID;

            //Handle all incoming messages from this point

            new Thread(new ThreadStart(HandleIncomingMessagesClient)).Start(); ;
        }

        /// <summary>
        /// Handle all incoming messages for this client.
        /// This is done using the static MessageHandler.HandleIncomingJsonMessage() method.
        /// When the clients' connection closes, the client will be remove from the connected clients list in the MessageSocketManager.
        /// This method is called in the constructor method of ClientMessageSocket.
        /// Do not await this method.
        /// </summary>
        /// <returns>A task that can be awaited.</returns>
        private void HandleIncomingMessagesClient()
        {
            while (innerClient.Connected && IsConnected())
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

        private bool IsConnected()
        {
            bool connected = true;
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

            return connected;
        }


        internal TcpClient GetInnerClient()
        {
            return innerClient;
        }

        /// <summary>
        /// Recieve a message from the network stream.
        /// The first 4 element of the string should contain the size of the following bytes.
        /// The method will hang if te size specified is bigger than the real size, because it waits for all bytes.
        /// The size is in little endian order and the message is encoded in UTF8.
        /// </summary>
        /// <param name="stream">The stream to gather data from.</param>
        /// <returns>The string that follows the 4 bytes, thus the message send.</returns>
        private String GetStringFromNetworkStream(NetworkStream stream)
        {
            // The first 4 bytes are the size of the string that will follow.
            byte[] sizeRead = new byte[4];
            stream.Read(sizeRead, 0, 4);

            // Convert the read byte array to the size of the following string
            int size = BitConverter.ToInt32(sizeRead, 0);

            // Read the string that will follow with a byte[] that has the same size as the size gathered.
            byte[] bytesReceived = new byte[size];
            stream.Read(bytesReceived, 0, size);

            // Convert the byte[] to string and return it.
            return Encoding.UTF8.GetString(bytesReceived);
        }

        /// <summary>
        /// Send a message to the client of this object.
        /// The first 4 bytes should contain the lenght of the message.
        /// The size is in little endian order and the message is encoded in UTF8.
        /// </summary>
        /// <param name="message">The message to send to the client</param>
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
            innerClient.GetStream().Write(returnArray, 0, returnArray.Length);
        }
        
    }
}
