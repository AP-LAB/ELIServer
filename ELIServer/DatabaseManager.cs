using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELIServer
{
    class DatabaseManager
    {

        private string connectionString =
                        "User=SYSDBA;" +
                        "Password=ELI;" +
                        "Database='C:/Users/0891435/DATA/ELI_CLIENT_DATABASE.FDB';" +
                        "DataSource=145.24.222.19;" +
                        "Port=8001;" +
                        "Dialect=3";

        private FbConnection connection;

        public DatabaseManager()
        {
            connection = new FbConnection(connectionString);
            connection.Open();
        }

        public Dictionary<string, string> GetUserByUsernameAndPassword(String username, String password)
        {
            String sql = String.Format(@"SELECT a.ID, a.FIRST_NAME, a.LAST_NAME, a.USER_NAME, a.LAST_LOG_IN
                        FROM GUESTS a
                        WHERE a.USER_NAME = '{0}' AND a.""PASSWORD"" = '{1}';", username, password);
            var command = new FbCommand(sql, connection);
            var dictionary = GetDictionaryFromFbDataReaderWithOneResult(command.ExecuteReader());
            dictionary.Add("TYPE", "user");
            return dictionary;
        }
        
        private Dictionary<string, string> GetDictionaryFromFbDataReaderWithOneResult(FbDataReader reader)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    list.Add(reader.GetName(i), reader[i].ToString());
                }
            }
            reader.Close();
            return list;
        }

        private List<Dictionary<string, string>> GetDictionaryFromFbDataReaderWithManyResults(FbDataReader reader)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            while (reader.Read())
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader[i].ToString());
                }
                list.Add(dictionary);
            }
            reader.Close();
            return list;
        }

        public List<Dictionary<string, string>> GetAllCitiesWithLatLon()
        {
            //TODO 
            // Get list of cities
            // Display cities on map
            var sql = @"SELECT DISTINCT a.CITY, a.LAT, a.LON, a.ID
                        FROM HOTELS a";
            var command = new FbCommand(sql, connection);
            return GetDictionaryFromFbDataReaderWithManyResults(command.ExecuteReader());
        }



        public void Close()
        {
            connection.Close();
        }

        public void SetVideoCallConnection(string client1Id, string client2Id)
        {
            var sql = String.Format(@"INSERT INTO VIDEO_CALL_CONNECTIONS (CLIENT_1_ID, CLIENT_2_ID)
                                     VALUES (
                                    '{0}', 
                                    '{1}'
                                    )", client1Id, client2Id);

            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public void RemoveVideoCallConnection(string client1Id, string client2Id)
        {
            var sql = String.Format(@"DELETE FROM VIDEO_CALL_CONNECTIONS WHERE 
                                        CLIENT_1_ID = '{0}' 
                                        AND CLIENT_2_ID = '{1}'", client1Id, client2Id);

            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
        }

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
            catch (FbException ex)
            {
                //TODO this could not be done because the connection already exists.
            }            
        }

        public void DisconnectGuestFromTable(string guestId, string tableId)
        {
            try
            {
                string sql = String.Format(@"DELETE FROM TABLES_GUESTS WHERE TABLE_ID = '{0}' AND GUEST_ID = '{1}';", tableId, guestId);
                var command = new FbCommand(sql, connection);
                command.ExecuteNonQuery();
            }
            catch (FbException ex)
            {
                //TODO this could not be done because the connection does not exist.
            }
        }




    }
}
