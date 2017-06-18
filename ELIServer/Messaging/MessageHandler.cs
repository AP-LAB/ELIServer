using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ELIServer.Messaging
{
    /// <summary>
    /// This abstract class provides is used for handling incoming message from a ClientMessageSocket instance.
    /// The starting point of this class is the HandleIncomingJsonMessage() method.
    /// All methods in this class are static.
    /// </summary>
    public abstract class MessageHandler
    {
        /// <summary>
        /// \brief Handle the incoming message according to  the given type.
        /// 
        /// The incoming messages is handled according to the type specified in the message.
        /// The type of message should be included in the "message_type" JSON value.
        /// Possible values are:
        /// - LogIn
        /// - LogOut
        /// - GetRandomTable
        /// - GetAllCities
        /// 
        /// (In the future other types will be added.)
        /// Please ensure that the message_type key is present.
        /// 
        /// The types will be handled using the according methods in the MessageHandler class, namely:
        /// - LogIn: LogIn()
        /// - LogOut: LogOut()
        /// - GetRandomTable: GetRandomTable()
        /// - GetAllCities: GetAllCities()
        /// - Other values: ReturnUnknownTypeMessage()
        /// 
        /// The message that needs to be handled is parsed using JsonConvert.DeserializeObject().
        /// Please ensure that the message is an string in JSON format.
        /// </summary>
        /// <param name="client">An instance of a ClientMessageSocket object where return messages should be send to.</param>
        /// <param name="message">The message to handle.</param>
        public static void HandleIncomingJsonMessage(ClientMessageSocket client, String message)
        {
            try{
                // Deserialize the message.
                dynamic jsonObject = JsonConvert.DeserializeObject(message);
                // Get the message_type of the message.
                String type = jsonObject.message_type.ToString();
                // Handle the message.
                // If the message type is unknown, no action will be taken.
                switch (type)
                {
                    case "LogIn":
                        LogIn(jsonObject, client);
                        break;
                    case "LogOut":
                        LogOut(jsonObject);
                        break;
                    case "GetRandomTable":
                        GetRandomTable(jsonObject, client);
                        break;
                    case "GetAllCities":
                        GetAllCities(client);
                        break;
                    default:
                        ReturnUnknownTypeMessage(client);
                        break;
                }
            }
            catch(JsonSerializationException ex)
            {
                //Not a JSON Object
                ReturnReadableMessageErrorMessage(client, ex.Message);
            }            
        }

        /// <summary>
        /// Return a "UnknownType" error message to the client.
        /// </summary>
        /// <param name="client">The ClientMessageSocket client to send the message to.</param>
        private static void ReturnUnknownTypeMessage(ClientMessageSocket client)
        {
            /// Create a dynamic message object.
            dynamic returnMessage = new ExpandoObject();
            /// Add values to the message object.
            returnMessage.TYPE = "Error";
            returnMessage.ERROR_TYPE = "UnknownType";
            returnMessage.MESSAGE = "The type of this message is unknown.";
            // Serialize and send the message.
            client.SendMessage(JsonConvert.SerializeObject(returnMessage));
        }

        /// <summary>
        /// Return a "UnreadableMessage" error message to the client.
        /// </summary>
        /// <param name="client">The ClientMessageSocket client to send the message to.</param>
        private static void ReturnReadableMessageErrorMessage(ClientMessageSocket client, String errorMessage)
        {
            /// Create a dynamic message object.
            dynamic returnMessage = new ExpandoObject();
            /// Add values to the message object.
            returnMessage.TYPE = "Error";
            returnMessage.ERROR_TYPE = "UnreadableMessage";
            returnMessage.MESSAGE = 
                String.Format("The received message is unreadable. Are you sure you passed a JSON string? The error message is: \"{0}\"", errorMessage);
            // Serialize and send the message.
            client.SendMessage(JsonConvert.SerializeObject(returnMessage));
        }

        /// <summary>
        /// \brief Load all cities from the database and send a JSON object to the client.
        /// 
        /// The return message will be a JSON object with a "RESULTS" key and an array of the cities.
        /// See the release docs for more information about the return message.
        /// </summary>
        /// <param name="client">The ClientMessageSocket client to send the message to.</param>
        private static void GetAllCities(ClientMessageSocket client)
        {
            // Get all cities from the database
            var dbManager = new DatabaseManager();
            var results = dbManager.GetAllCitiesWithLatLon();
            dbManager.Close();
            // Add the results to the JSON value "RESULTS"
            dynamic returnDyn = new ExpandoObject();
            returnDyn.RESULTS = results;
            // Convert the JsonObject to a serialized string
            String jsonString = JsonConvert.SerializeObject(returnDyn);
            // Send the message to the client.
            client.SendMessage(jsonString);
        }

        /// <summary>
        /// \brief Create a new CallConnection between the specified client and a random client.
        /// 
        /// The random client is selected using the MessageSocketManager.GetRandomPendingConnection() method.
        /// When a CallConnection is created, the callConnections list in the MessageSocketManager class will be updated accordingly.
        /// If no random client is found, the client will be stored in the pendingRandomCallConnections list of the MessageSocketManager class.
        /// This will be done using the MessageSocketManager.AddPendingRandomCallClient();
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="client">The ClientMessageSocket client to connect the CallConnection to.</param>
        private static void GetRandomTable(dynamic message, ClientMessageSocket client)
        {
            var clientId = message.ClientID;
            var randomTable = MessageSocketManager.GetRandomPendingConnection();

            // If another random request is lined up, use that table.
            // Otherwise, place this table in the wait list.
            if (randomTable != null)
            {
                var callConnection = new CallConnection(client, randomTable);
                // Update the callConnections list in the MessageSocketManager class accordingly.
                MessageSocketManager.AddCallConnection(callConnection);
            }
            else
            {
                // Add the client to the pendingRandomCallConnections list of the MessageSocketManager class.
                MessageSocketManager.AddPendingRandomCallClient(client);
            }
        }

        /// <summary>
        /// This method checks the user credentials that where passed through the message.
        /// If the combination of username and password exists in the database, the users information is returned to the ClientMessageSocket client.
        /// If not, an error message is send. This is done using the ReturnIncorrectUserNameOrPasswordMessage() method.
        /// 
        /// Note: This method is not secure. It should be altered before the application is released.
        /// </summary>
        /// <param name="jsonObject">A dynamic object that was created using the JsonConvert.DeserializeObject() method.</param>
        /// <param name="client">The ClientMessageSocket client to send the return message to.</param>
        private static void LogIn(dynamic jsonObject, ClientMessageSocket client)
        {
            // Extract the data needed.
            var password = jsonObject.PassWord;
            var username = jsonObject.UserName;
            var logInTime = jsonObject.LogInTime; //yyyy-MM-dd HH:mm:ss, ignored for now
            var tableId = jsonObject.ClientID;
            // Load the requested user from the database in order to check the credentials.
            var dbManager = new DatabaseManager();
            Dictionary<string, string> userDictionary = dbManager.GetUserByUsernameAndPassword(username, password);
            // If the dictionary does not contain the "FIRST_NAME", "LAST_NAME" or "USER_NAME" keys, 
            // an error message should be returned.
            if (!userDictionary.ContainsKey("FIRST_NAME") 
                || !userDictionary.ContainsKey("LAST_NAME") 
                || !userDictionary.ContainsKey("USER_NAME")) 
            {
                ReturnIncorrectUserNameOrPasswordMessage(client);
                return;
            }
            //Add the connection between te user and the table to the database.
            dbManager.ConnectGuestToTable(userDictionary["ID"], tableId);
            dbManager.Close();
            client.SendMessage(JsonConvert.SerializeObject(userDictionary));
        }

        /// <summary>
        /// Return a "IncorrectUserNameOrPassword" error message to the client.
        /// </summary>
        /// <param name="client">The ClientMessageSocket client to send the message to.</param>
        private static void ReturnIncorrectUserNameOrPasswordMessage(ClientMessageSocket client)
        {
            /// Create a dynamic message object.
            dynamic returnMessage = new ExpandoObject();
            /// Add values to the message object.
            returnMessage.TYPE = "Error";
            returnMessage.ERROR_TYPE = "IncorrectUserNameOrPassword";
            returnMessage.MESSAGE = "The username or password is incorrect.";               
            // Serialize and send the message.
            client.SendMessage(JsonConvert.SerializeObject(returnMessage));
        }

        /// <summary>
        /// Log the user and client out. 
        /// The ClientMessageSocket client is removed from the connectedSockets list in the MessageSocketManager and the connection is removed from the database.
        /// 
        /// The LogOut message should containt the following values:
        /// - ClientID: the ID of the table
        /// - ID: the ID of the user
        /// An MissingFieldException  will be thrown if the message does not contain these values.
        /// </summary>
        /// <param name="jsonObject">The LogOut message</param>
        private static void LogOut(dynamic jsonObject)
        {
            // Search for the client in the connectedSockets list of the MessageSocketManager class.
            var client = MessageSocketManager.GetClientMessageSocketById(jsonObject.ClientID);
            // Remove the client from the connectedSockets list if it is not null.
            if (client != null) MessageSocketManager.RemoveClientFromConnectedSockets(client);
            // Remove the connection from the database.
            var dbManager = new DatabaseManager();
            dbManager.DisconnectGuestFromTable(jsonObject.ID, jsonObject.ClientID);
            dbManager.Close();
        }

    }
}
