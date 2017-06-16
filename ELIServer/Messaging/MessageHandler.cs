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
    class MessageHandler
    {
        ClientMessageSocket client;
        
        public static void HandleIncomingJsonMessage(ClientMessageSocket client, String message)
        {
            try{
                dynamic jsonObject = JsonConvert.DeserializeObject(message);
                var type = jsonObject.message_type.ToString(); ;

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
                }
            }
            catch
            {
                throw;
                //Not a JSON Object
            }            
        }

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
            client.SendMessage(jsonString);
        }

        private static void GetRandomTable(dynamic jsonObject, ClientMessageSocket client)
        {
            var clientId = jsonObject.ClientID;
            var randomTable = MessageSocketManager.GetRandomPendingConnection();

            // If another random request is lined up, use that table.
            // Otherwise, place this table in the wait list.
            if (randomTable != null)
            {
                var callConnection = new CallConnection(client, randomTable);
                MessageSocketManager.AddCallConnection(callConnection);
            }
            else
            {
                MessageSocketManager.AddPendingCallClient(client);
            }
        }

        private static void LogIn(dynamic jsonObject, ClientMessageSocket client)
        {
            var password = jsonObject.PassWord;
            var username = jsonObject.UserName;
            var logInTime = jsonObject.LogInTime; //yyyy-MM-dd HH:mm:ss
            var tableId = jsonObject.ClientID;

            var dbManager = new DatabaseManager();
            Dictionary<string, string> userDictionary = dbManager.GetUserByUsernameAndPassword(username, password);
            
            if (userDictionary.Count == 0)
            {
                dynamic error = new ExpandoObject();
                error.type = "error";
                error.message = "The requested user could not be found...";
                ReturnMessage(JsonConvert.SerializeObject(error), client);
                return;
            }


            dbManager.ConnectGuestToTable(userDictionary["ID"], tableId);

            dbManager.Close();            
            
            ReturnMessage(JsonConvert.SerializeObject(userDictionary), client);
                      
        }

        private static void LogOut(dynamic jsonObject)
        {

            var dbManager = new DatabaseManager();
            dbManager.DisconnectGuestFromTable(jsonObject.ID, jsonObject.ClientID);
            dbManager.Close();
        }

        private static void ReturnMessage(string message, ClientMessageSocket client)
        {
            dynamic returnJsonObject = new ExpandoObject();
            returnJsonObject.Message = message;
            client.SendMessage(JsonConvert.SerializeObject(message));
        }

    }
}
