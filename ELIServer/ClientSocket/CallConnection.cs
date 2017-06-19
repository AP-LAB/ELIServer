using ELIServer.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ELIServer
{
    /// <summary>
    /// \brief This class represents a video call connection.
    /// 
    /// The class contains two ClientMessageSocket instances, client1 and client2. 
    /// A connection is established between those clients.
    /// When an instance is created, the clients will immediately start streaming to eachother.
    /// </summary>
    public class CallConnection
    {
        private ClientMessageSocket client1;
        private ClientMessageSocket client2;
        private bool running;

        /// <summary>
        /// \brief Create a new instance of CallConnection.
        /// 
        /// When the instance is created, the database will be updated accordingly.
        /// </summary>
        /// <param name="_client1">An object of the ClientMessageSocket class.</param>
        /// <param name="_client2">An object of the ClientMessageSocket class.</param>
        public CallConnection(ClientMessageSocket _client1, ClientMessageSocket _client2)
        {
            client1 = _client1;
            client2 = _client2;
            client1.isCalling = true;
            client2.isCalling = true;
            var dbManager = new DatabaseManager();
            dbManager.SetVideoCallConnection(client1.clientID, client2.clientID);
            dbManager.Close();
            SendStartMessaged();
            SetThreads();
        }

        /// <summary>
        /// Send a "Start recording" message to both clients.
        /// </summary>
        private void SendStartMessaged()
        {
            /// Create a dynamic message object.
            dynamic returnMessage = new ExpandoObject();
            /// Add values to the message object.
            returnMessage.TYPE = "Message";
            returnMessage.MESSAGE = "Start recording";
            // Serialize and send the message.
            client1.SendMessage(JsonConvert.SerializeObject(returnMessage));
            client2.SendMessage(JsonConvert.SerializeObject(returnMessage));
        }


        /// <summary>
        /// \brief Extract the streams from the clients and start streaming to both clients.
        /// 
        /// The streaming is done on two seperate threads.
        /// The streaming itself is done using the StreamTo() method.
        /// </summary>        
        public void SetThreads()
        {
            NetworkStream stream1 = client1.GetInnerClient().GetStream();
            NetworkStream stream2 = client2.GetInnerClient().GetStream();
            new Thread(unused => StreamTo(stream1, stream2)).Start();            
            new Thread(unused => StreamTo(stream2, stream1)).Start();
        }

        /// <summary>
        /// \brief Start streaming from one stream to another.
        /// 
        /// This is repeated as long as both clients are connected.
        /// When the connection on one of the clients is closed, the CloseConnection() method is called.
        /// </summary>
        /// <param name="inStream">The input stream to get data from.</param>
        /// <param name="outStream">The output stream to pass data to.</param>
        private async void StreamTo(NetworkStream inStream, NetworkStream outStream)
        {
            while (client1.IsConnected() && client2.IsConnected())
            {
                while (inStream.DataAvailable)
                {
                    await inStream.CopyToAsync(outStream);
                }
            }
            CloseConnection();
        }

        /// <summary>
        /// \brief This method is called when the callconnection is closed.
        /// Set the isCalling state of the clients to false and remove the CallConnection from the callConnectios list in the MessageSocketManager class
        /// </summary>
        private void CloseConnection()
        {
            client1.isCalling = false;
            client2.isCalling = false;
            MessageSocketManager.RemoveCallConnection(this);

        }

        /// <summary>
        /// Get the first client of the connection.
        /// This client is of the ClientMessageSocket class.
        /// </summary>
        /// <returns>An object of the class ClientMessageSocket</returns>
        internal ClientMessageSocket GetClient1()
        {
            return client1;
        }

        /// <summary>
        /// Get the second client of the connection.
        /// This client is of the ClientMessageSocket class.
        /// </summary>
        /// <returns>An object of the class ClientMessageSocket</returns>
        internal ClientMessageSocket GetClient2()
        {
            return client2;
        }
        
    }
}
