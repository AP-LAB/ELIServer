using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ELIServerTest
{
    class TestDatabaseHelper
    {
        private string connectionString =
                "User=SYSDBA;" +
                "Password=ELI;" +
                "Database='C:/Users/0891435/DATA/ELI_CLIENT_DATABASE.FDB';" +
                "DataSource=145.24.222.19;" +
                "Port=8001;" +
                "Dialect=3";

        private FbConnection connection;

        public TestDatabaseHelper()
        {
            connection = new FbConnection(connectionString);    
        }

        public void InsertUser(String id, String firstName, String lastName, String userName, String password)
        {
            connection.Open();
            var sql = String.Format(@"INSERT INTO GUESTS (ID, FIRST_NAME, LAST_NAME, USER_NAME, ""PASSWORD"")
                         VALUES(
                        '{0}',
                        '{1}',
                        '{2}',
                        '{3}',
                        '{4}'                        
                        )", id, firstName, lastName, userName, password);
            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        public void RemoveUser(String id)
        {
            connection.Open();
            var sql = String.Format(@"DELETE FROM GUESTS WHERE ID = '{0}'", id);
            var command = new FbCommand(sql, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        public FbDataReader GetReaderForUser(String ID)
        {
            connection.Open();
            var sql = String.Format(@"SELECT FROM GUESTS WHERE ID = '{0}'", ID);
            var command = new FbCommand(sql, connection);
            var reader = command.ExecuteReader();
            connection.Close();
            return reader;
        }

        public void InsertTable(String tableNumber, String hotelId)
        {
            connection.Open();
            var sql = String.Format(@"INSERT INTO TABLES (TABLE_NUMBER, HOTEL_ID)
                     VALUES (
                    '{0}', 
                    '{1}'
                    )", tableNumber, hotelId);
            new FbCommand(sql, connection).ExecuteNonQuery();
            connection.Close();
        }

        public void DeleteTable(String tableNumber, String hotelId)
        {
            connection.Open();
            var sql = String.Format(@"DELETE FROM TABLES WHERE TABLE_NUMBER = '{0}' AND HOTEL_ID = '{1}'"
                        , tableNumber, hotelId);
            new FbCommand(sql, connection).ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// Gets a call connection from the database.
        /// Warning: this method does not close the connection to the database.
        /// You need to close it using CloseConnection() on this object.
        /// </summary>
        /// <param name="tableNumber1">Table number 1</param>
        /// <param name="tableNumber2">Table number 2</param>
        /// <returns>A reader with the retreived rows. This reader should return only 1 row.</returns>
        public FbDataReader GetCallConnection(String tableNumber1, String tableNumber2)
        {
            connection.Open();
            var sql = String.Format(@"SELECT * FROM VIDEO_CALL_CONNECTIONS WHERE TABLE_NUMBER_1 = '{0}' AND TABLE_NUMBER_2 = '{1}'"
                        , tableNumber1, tableNumber2);
            var reader = new FbCommand(sql, connection).ExecuteReader();           
            return reader;
        }

        public void CloseConnection()
        {
            connection.Close();
        }

        public void RemoveCallConnection(String tableNumber1, String tableNumber2)
        {
            connection.Open();
            var sql = String.Format(@"DELETE FROM VIDEO_CALL_CONNECTIONS WHERE TABLE_NUMBER_1 = '{0}' AND TABLE_NUMBER_2 = '{1}'"
                        , tableNumber1, tableNumber2);
            new FbCommand(sql, connection).ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// Get the connection between a guest and a table.
        /// Warning: this method does not close the connection to the database.
        /// You need to close it using CloseConnection() on this object.
        /// </summary>
        /// <param name="tableNumber">The table number</param>
        /// <param name="guestId">The id of the guest</param>
        /// <returns>A reader with the retreived rows. This reader should return only 1 row.</returns>
        public FbDataReader GetGuestsTablesConnection(String tableNumber, String guestId)
        {
            connection.Open();
            var sql = String.Format(@"SELECT * FROM TABLES_GUESTS WHERE TABLE_ID = '{0}' AND GUEST_ID = '{1}'"
                        , tableNumber, guestId);
            var reader = new FbCommand(sql, connection).ExecuteReader();
            return reader;
        }


        /// <summary>
        /// Get the connection between a guest and a table.
        /// Warning: this method does not close the connection to the database.
        /// You need to close it using CloseConnection() on this object.
        /// </summary>
        /// <param name="tableNumber">The table number</param>
        /// <param name="guestId">The id of the guest</param>
        /// <returns>A reader with the retreived rows. This reader should return only 1 row.</returns>
        public FbDataReader GetGuestToTableConnection(String tableNumber, String guestId)
        {
            connection.Open();
            var sql = String.Format(@"SELECT * FROM TABLES_GUESTS WHERE TABLE_ID = '{0}' AND GUEST_ID = '{1}'"
                        , tableNumber, guestId);
            var reader = new FbCommand(sql, connection).ExecuteReader();
            return reader;
        }




    }
}
