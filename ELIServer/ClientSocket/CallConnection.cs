using ELIServer.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ELIServer
{
    public class CallConnection
    {
        ClientMessageSocket client1;
        ClientMessageSocket client2;

        public CallConnection(ClientMessageSocket _client1, ClientMessageSocket _client2)
        {
            client1 = _client1;
            client2 = _client2;
            var dbManager = new DatabaseManager();
            dbManager.SetVideoCallConnection(client1.clientID, client2.clientID);
            dbManager.Close();
            SetThreads();
        }


        /// <summary>
        /// Extract the streams frim the clients and start streaming to both clients.
        /// </summary>
        public void SetThreads()
        {
            NetworkStream stream1 = client1.GetInnerClient().GetStream();
            NetworkStream stream2 = client2.GetInnerClient().GetStream();
            new Thread(unused => StreamTo(stream1, stream2)).Start();            
            new Thread(unused => StreamTo(stream2, stream1)).Start();
        }

        private async void StreamTo(NetworkStream inStream, NetworkStream outStream)
        {
            while (client1.GetInnerClient().Connected && client2.GetInnerClient().Connected)
            {
                while (inStream.DataAvailable)
                {
                    await inStream.CopyToAsync(outStream);
                }
            }

            MessageSocketManager.RemoveCallConnection(this);
        }

        internal ClientMessageSocket GetClient1()
        {
            return client1;
        }


        internal ClientMessageSocket GetClient2()
        {
            return client2;
        }
        
    }
}
