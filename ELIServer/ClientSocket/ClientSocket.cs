using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ELIServer
{
    public class ClientSocket
    {
        TcpClient fromClient;
        TcpClient toClient;
        private bool isListening = false;

        public async void CreateNewClientByType(TcpClient client)
        {
            //Todo read client and create new client by the type of data passed

            //Add the client to the listening server list of clients
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            stream.Read(buffer, 0, client.ReceiveBufferSize);

            var message = System.Text.Encoding.UTF8.GetString(buffer);

            //Remove empty characters
            message = message.TrimEnd(new char[] { (char)0 });

            //TODO read correctly + specify amount of bytes in buffer?
            if (message == "Receiver")
            {
                SetToClient(client);
            }
            else if (message == "Sender")
            {
                SetFromClient(client);
            }
            else
            {
                Debug.WriteLine("Not a valid client!!! Client message was: " + message);
            }

            if (fromClient != null && toClient != null && !isListening)
            {
                StartListening();
            }

        }

        public void SetFromClient(TcpClient client)
        {
            fromClient = client;
        }

        public void SetToClient(TcpClient client)
        {
            toClient = client;
        }

        public async Task StartListening()
        {
            isListening = true;
            NetworkStream inStream = fromClient.GetStream();
            NetworkStream outStream = toClient.GetStream();

            while (fromClient.Connected && toClient.Connected)
            {
                while (inStream.DataAvailable)
                {
                    inStream.CopyTo(outStream);
                }
            }
            Debug.WriteLine("Lost connection");

        }
    }
}
