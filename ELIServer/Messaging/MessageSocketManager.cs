using ELIServer.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ELIServer
{   
    class MessageSocketManager
    {
        private static List<ClientMessageSocket> connectedSockets = new List<ClientMessageSocket>();
        private static List<CallConnection> callConnections = new List<CallConnection>();
        private static List<ClientMessageSocket> pendingCallConnections = new List<ClientMessageSocket>();
        private static MainWindow mainWindow;

        TcpListener listener = null;
        int port = 30000;
        string hostname = "127.0.0.1";
        bool running = false;
        Thread thread;
        

        public MessageSocketManager(MainWindow _mainWindow)
        {
            mainWindow = _mainWindow;
            // Parse IP address
            IPAddress localAddr = IPAddress.Parse(hostname);
            // Create a new instance of TcpListener
            listener = new TcpListener(localAddr, port);

            listener.Start();
            running = true;
            // Create a new thread to handle the incoming messages.
            thread = new Thread(new ThreadStart(StartListening));
            thread.Start();

        }


        private void StartListening()
        {
            while (running)
            {
                //Check if new conne
                if (listener.Pending())
                {
                    var client = listener.AcceptTcpClient();
                    connectedSockets.Add(new ClientMessageSocket(client));
                    mainWindow.SetNumberOfConnectedClients(connectedSockets.Count());
                }
            }
            
        }

        public static void RemoveClientFromConnectedSockets(ClientMessageSocket client)
        {
            if (connectedSockets.Contains(client))
            {
                connectedSockets.Remove(client);
                var callConnection = GetCallConnectionWithCient(client);
                if (callConnection != null) RemoveCallConnection(callConnection);
                mainWindow.SetNumberOfConnectedClients(connectedSockets.Count());
            }
        }

        /// <summary>
        /// Searches a socket with the given ID in the connectedSockets list.
        /// </summary>
        /// <param name="clientID">The ID to search for.</param>
        /// <returns>The found ClientMessageSocket</returns>
        public static ClientMessageSocket GetClientMessageSocketById(String clientID)
        {
            return connectedSockets.Where(x => x.clientID.Equals(clientID)).FirstOrDefault();
        }

        public static void AddCallConnection(CallConnection callConnection)
        {
            if (!callConnections.Contains(callConnection))
            {
                callConnections.Add(callConnection);
                mainWindow.SetNumberOfConnectedCalls(callConnections.Count());
            }
        }

        public static void RemoveCallConnection(CallConnection callConnection)
        {
            if (callConnections.Contains(callConnection))
            {
                var dbManager = new DatabaseManager();
                dbManager.RemoveVideoCallConnection(callConnection.GetClient1().clientID, callConnection.GetClient2().clientID);
                dbManager.Close();
                callConnections.Remove(callConnection);
                mainWindow.SetNumberOfConnectedCalls(callConnections.Count());
            }
        }

        public static ClientMessageSocket GetRandomPendingConnection()
        {
            if (pendingCallConnections.Any())
            {
                ClientMessageSocket clientMessageSocket = pendingCallConnections.First();
                pendingCallConnections.Remove(clientMessageSocket);
                mainWindow.SetNumberOfPendingCalls(pendingCallConnections.Count());
                return clientMessageSocket;
            }
            else
            {
                return null;
            }         
        }

        public static void AddPendingCallClient(ClientMessageSocket client)
        {
            if (!pendingCallConnections.Contains(client))
            {
                pendingCallConnections.Add(client);
                mainWindow.SetNumberOfPendingCalls(pendingCallConnections.Count());
            }
        }

        private static CallConnection GetCallConnectionWithCient(ClientMessageSocket client)
        {
            return callConnections.Where(x => x.GetClient1().Equals(client) || x.GetClient2().Equals(client)).FirstOrDefault();
        }


    }
}
