using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ELIServer
{
    /// <summary>
    /// This class is used for retrieving and sending data to the database.
    /// The connectionString is included in the App.config file on the server.
    /// </summary>
    public class DatabaseManager
    {
        private FbConnection connection;

        /// <summary>
        /// Default constuctor.
        /// The connection is created and opened in this constuctor using the connection string returned by GetConnectionString().
        /// </summary>
        public DatabaseManager()
        {            
            connection = new FbConnection(GetConnectionString());
            connection.Open();
        }

        /// <summary>
        /// This constuctor is used for testing.
        /// </summary>
        /// <param name="connectionString">The FireBird connection string to the database.</param>
        public DatabaseManager(String connectionString)
        {
            connection = new FbConnection(connectionString);
            connection.Open();
        }

        /// <summary>
        /// Load the connection string from the App.config file.
        /// </summary>
        /// <returns>The connection string.</returns>
        private String GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["ELIDatabase"].ConnectionString;
        }

        /// <summary>
        /// Load the guest by the specified username and password from the database.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <param name="password">The password to search for.</param>
        /// <returns>A dictionary with the user, or an dictionary with only the type: "user" pair.</returns>
        public Dictionary<string, string> GetUserByUsernameAndPassword(String username, String password)
        {
            var sql = String.Format(@"SELECT a.ID, a.FIRST_NAME, a.LAST_NAME, a.USER_NAME, a.LAST_LOG_IN
                        FROM GUESTS a
                        WHERE a.USER_NAME = '{0}' AND a.""PASSWORD"" = '{1}';", username, password);
            var command = new FbCommand(sql, connection);
            var dictionary = GetDictionaryFromFbDataReaderWithOneResult(command.ExecuteReader(), "user");
            return dictionary;
        }

        /// <summary>
        /// Create a dictionary from colums in the first row in the reader.
        /// If there are no rows, return an empty dictionary.
        /// </summary>
        /// <param name="reader">The reader that contains the data.</param>
        // <returns>A dictionary with the column names and values.</returns>
        private Dictionary<string, string> GetDictionaryFromFbDataReaderWithOneResult(FbDataReader reader, String type)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader[i].ToString());
                }
            }
            // Add the type of the dictonary.
            dictionary.Add("TYPE", type);
            reader.Close();
            return dictionary;
        }

        /// <summary>
        /// Create a list of dictonaries that represent the returned row from the database.
        /// The keys are the column names and the values are the values from the database.
        /// </summary>
        /// <param name="reader">The reader that contains the data.</param>
        /// <returns>A list with dictionaries that contain the column name and value.</returns>
        private List<Dictionary<string, string>> GetDictionaryFromFbDataReaderWithManyResults(FbDataReader reader, String type)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            while (reader.Read())
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                // Store every field in the read row in the dictionary.
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader[i].ToString());
                }
                // Add the type of the dictonary.
                dictionary.Add("TYPE", type);
                list.Add(dictionary);
            }
            reader.Close();
            return list;
        }

        /// <summary>
        /// Load all cities from the database.
        /// The data that is loaded is the cities:
        /// - Name
        /// - Latitude
        /// - Longitude
        /// - Id
        /// </summary>
        /// <returns>A list with dictionaries that contain the city objects.</returns>
        public List<Dictionary<string, string>> GetAllCitiesWithLatLon()
        {
            var sql = @"SELECT DISTINCT a.CITY, a.LAT, a.LON, a.ID
                        FROM HOTELS a";
            var command = new FbCommand(sql, connection);
            return GetDictionaryFromFbDataReaderWithManyResults(command.ExecuteReader(), "city");
        }

        /// <summary>
        /// Close the database connection.
        /// </summary>
        public void Close()
        {
            connection.Close();
        }

        /// <summary>
        /// Insert a CallConnection record in the database.
        /// </summary>
        /// <param name="client1Id">The first client in the CallConnection instance.</param>
        /// <param name="client2Id">The second client in the CallConnection instance.</param>
        public void SetVideoCallConnection(string client1Id, string client2Id)
        {
            var sql = String.Format(@"INSERT INTO VIDEO_CALL_CONNECTIONS (TABLE_NUMBER_1, TABLE_NUMBER_2)
                                     VALUES (
                                    '{0}', 
                                    '{1}'
                                    )", client1Id, client2Id);
            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Remove the CallConnection records with the specified client ids from the database.
        /// </summary>
        /// <param name="client1Id">The first client in the CallConnection instance.</param>
        /// <param name="client2Id">The second client in the CallConnection instance.</param>
        public void RemoveVideoCallConnection(string client1Id, string client2Id)
        {
            var sql = String.Format(@"DELETE FROM VIDEO_CALL_CONNECTIONS WHERE 
                                        TABLE_NUMBER_1 = '{0}' 
                                        AND TABLE_NUMBER_2 = '{1}'", client1Id, client2Id);

            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Insert the connection between a guest and a client (table) into the database.
        /// </summary>
        /// <param name="guestId">The ID of the guest.</param>
        /// <param name="tableId">The ID of the client (table).</param>
        public void ConnectGuestToTable(string guestId, string tableId)
        {
            try
            {
                string sql = String.Format(@"INSERT INTO TABLES_GUESTS (TABLE_ID, GUEST_ID)
                         VALUES (
                        '{0}', 
                        '{1}'
                        )", tableId, guestId);
                var command = new FbCommand(sql, connection);
                command.ExecuteNonQuery();
            }
            catch (FbException)
            {
                //This could not be done because the connection already exists.
            }            
        }

        /// <summary>
        /// Remove the connection between a guest and a client (table) from the database.
        /// </summary>
        /// <param name="guestId">The ID of the guest.</param>
        /// <param name="tableId">The ID of the client (table).</param>
        public void DisconnectGuestFromTable(string guestId, string tableId)
        {
            try
            {
                string sql = String.Format(@"DELETE FROM TABLES_GUESTS WHERE TABLE_ID = '{0}' AND GUEST_ID = '{1}';", tableId, guestId);
                var command = new FbCommand(sql, connection);
                command.ExecuteNonQuery();
            }
            catch (FbException)
            {
                //This could not be done because the connection does not exist.
            }
        }
        

    }
}
