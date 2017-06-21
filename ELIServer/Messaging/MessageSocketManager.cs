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
    /// <summary>
    /// This class handles all incoming TCP client connections.
    /// When an instance of the MessageSocketManager class is created, StartListening() is called on a new Thread.
    /// The class has a couple of static lists:
    /// - connectedSockets: a collection of the currently connected ClientMessageSocket instances.
    /// - callConnections: a collection of the currently live CallConnection instances.
    /// - pendingRandomCallConnections: a collection of pending ClientMessageSocket instance that have requested a random call partner.
    /// </summary>
    public class MessageSocketManager
    {
        private static List<ClientMessageSocket> connectedSockets = new List<ClientMessageSocket>(); //!< A collection of the currently connected ClientMessageSocket instances.
        private static List<CallConnection> callConnections = new List<CallConnection>(); //!< A collection of the currently live CallConnection instances.
        private static List<ClientMessageSocket> pendingRandomCallConnections = new List<ClientMessageSocket>(); //!<A collection of pending ClientMessageSocket instance that have requested a random call partner.
        private static MainWindow mainWindow; //!< A MainWindow instance where the state of the client lists can be updated.
        private TcpListener listener = null; //!< The TcpListener that is used to listen for new connections.
        private int port = 8005; //!< The port to listen to.
        private string hostname = "127.0.0.1"; //!< The hostname to listen to.
        private bool running = false; //!< A boolean that represents the state of the MessageSocketManager instance.
        private Thread thread; //!< The thread that is used for the StartListening() method.        

        /// <summary>
        /// The constuctor for the MessageSocketManager class.
        /// The values for mainWindow, listener, running and thread are set in the constructor.
        /// The listener is created using the port an hostname global variables.
        /// The StartListening() method is called on a new thread. This thread is stored in the global variable thread.
        /// </summary>
        /// <param name="_mainWindow">A MainWindow instance where the state of the client lists can be updated.</param>
        public MessageSocketManager(MainWindow _mainWindow)
        {
            mainWindow = _mainWindow;
            // Parse IP address
            IPAddress localAddr = IPAddress.Parse(hostname);
            // Create a new instance of TcpListener
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();            
            // Create a new thread to handle the incoming messages.
            thread = new Thread(new ThreadStart(StartListening));
            thread.Start();
            // When everything is done, the running boolean is set to true.
            running = true;
        }

        /// <summary>
        /// This method checks for new pending TcpClient connections.
        /// When a new connection is detected, a ClientMessageSocket instance is added to the connectedSockets list.
        /// The MainWindow is also updated using the SetNumberOfConnectedClients() method.
        /// </summary>
        private void StartListening()
        {
            while (running)
            {
                // Check if new connection is pending.
                if (listener.Pending())
                {
                    // Get the client.
                    var client = listener.AcceptTcpClient();
                    // Create a ClientMessageSocket and store it in the connectedSockets list.
                    connectedSockets.Add(new ClientMessageSocket(client));
                    // Update MainWindow.
                    mainWindow.SetNumberOfConnectedClients(connectedSockets.Count());
                }
            }            
        }

        /// <summary>
        /// Removes a ClientMessageSocket instance from the connectedSockets list.
        /// If the ClientMessageSocket instance is used in a CallConnection instance, this instance is removed too.
        /// The mainWindow is also updated.
        /// </summary>
        /// <param name="client">The ClientMessageSocket instance to remove.</param>
        public static void RemoveClientFromConnectedSockets(ClientMessageSocket client)
        {
            if (connectedSockets.Contains(client)) { 
                // Close the connection 
                client.GetInnerClient().Close();
                // Remove the ClientMessageSocket instance from connectedSockets.
                connectedSockets.Remove(client);
                // Find a CallConnection with the client.
                var callConnection = GetCallConnectionWithCient(client);
                // If a CallConnection is found, remove the CallConnection.
                if (callConnection != null) RemoveCallConnection(callConnection);
                // Update MainWindow.
                mainWindow.SetNumberOfConnectedClients(connectedSockets.Count());
                // Remove the client from the pendingRandomCallConnections list.
                RemovePendingRandomCallConnection(client);
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

        /// <summary>
        /// Add a CallConnection to the callConnections list.
        /// </summary>
        /// <param name="callConnection">An instance of the CallConnection class</param>
        public static void AddCallConnection(CallConnection callConnection)
        {
            // Make sure the CallConnection is not present in the callConnections already.
            if (!callConnections.Contains(callConnection))
            {
                callConnections.Add(callConnection);
                // Update MainWindow.
                mainWindow.SetNumberOfConnectedCalls(callConnections.Count());
            }
        }

        /// <summary>
        /// Remove a CallConnection instance from the callConnections list and remove the connection from the database.
        /// </summary>
        /// <param name="callConnection">A CallConnection instance.</param>
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

        /// <summary>
        /// Remove a ClientMessageSocket from the pendingRandomCallConnections list.
        /// </summary>
        /// <param name="client">The ClientMessageSocket to remove</param>
        public static void RemovePendingRandomCallConnection(ClientMessageSocket client)
        {
            // Ensure that the client is in the pendingRandomCallConnections list.
            if (pendingRandomCallConnections.Contains(client))
            {
                pendingRandomCallConnections.Remove(client);
            }
        }

        /// <summary>
        /// Get a pending ClientMessageSocket instance that has requested a random call connection.
        /// If the pendingRandomCallConnections list is empty, null is returned.
        /// When a ClientMessageSocket instance is found, it is removed from the pendingRandomCallConnections list.
        /// </summary>
        /// <returns>A ClientMessageSocket instance or null</returns>
        public static ClientMessageSocket GetRandomPendingConnection()
        {           
            if (pendingRandomCallConnections.Any())
            {
                // Get the first pending ClientMessageSocket instance in the list.
                ClientMessageSocket clientMessageSocket = pendingRandomCallConnections.First();
                // Remove the ClientMessageSocket instance from pendingRandomCallConnections.
                RemovePendingRandomCallConnection(clientMessageSocket);
                // Update the MainWindow.
                mainWindow.SetNumberOfPendingCalls(pendingRandomCallConnections.Count());
                return clientMessageSocket;
            }
            else
            {
                // If the pendingRandomCallConnections list is empty, return null.
                return null;
            }         
        }

        /// <summary>
        /// Add a ClientMessageSocket instance that has send a random connection request to the pendingRandomCallConnections.
        /// The instance will only be added if it does not exist in the pendingRandomCallConnections list.
        /// </summary>
        /// <param name="client">A ClientMessageSocket instance</param>
        public static void AddPendingRandomCallClient(ClientMessageSocket client)
        {
            // Only add if the pendingRandomCallConnections does not contain the client already.
            if (!pendingRandomCallConnections.Contains(client))
            {
                pendingRandomCallConnections.Add(client);
                // Update the MainWindow.
                mainWindow.SetNumberOfPendingCalls(pendingRandomCallConnections.Count());
            }
        }

        /// <summary>
        /// Find a CallConnection instance that uses the given client.
        /// If it does not exist, null will be return.
        /// </summary>
        /// <param name="client">The ClientMessageSocket instance to find.</param>
        /// <returns>The CallConnection instance or null.</returns>
        private static CallConnection GetCallConnectionWithCient(ClientMessageSocket client)
        {
            return callConnections.Where(x => x.GetClient1().Equals(client) || x.GetClient2().Equals(client)).FirstOrDefault();
        }

    }
}
