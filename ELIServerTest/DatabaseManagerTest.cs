using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ELIServer;

namespace ELIServerTest
{
    [TestClass]
    public class DatabaseManagerTest
    {
        DatabaseManager databaseManager;
        MockDataGenerator mockDataGenerator;

        [TestInitialize]
        public void SetUpDatabase()
        {
            databaseManager = new DatabaseManager();
            mockDataGenerator = new MockDataGenerator();
        }

        [TestCleanup]
        public void CloseDatabase()
        {
            databaseManager.Close();
        }


        [TestMethod]
        public void GetUserByUsernameAndPassword_Existing_ShouldReturnFilledDictionary()
        {
            // Insert the mock user "user 1".
            mockDataGenerator.InsertUser1();
            // Load the user from the database using the GetUserByUsernameAndPassword() method.
            var result = databaseManager.GetUserByUsernameAndPassword(mockDataGenerator.user1["USER_NAME"], mockDataGenerator.user1["PASSWORD"]);
            // Assert that the correct username is present.
            Assert.IsTrue(result.ContainsKey("USER_NAME"));
            Assert.IsTrue(result.ContainsValue(mockDataGenerator.user1["USER_NAME"]));
            // Assert that the correct ID is present.
            Assert.IsTrue(result.ContainsKey("ID"));
            Assert.IsTrue(result.ContainsValue(mockDataGenerator.user1["ID"]));
            // Assert that the correct FIRST_NAME is present.
            Assert.IsTrue(result.ContainsKey("FIRST_NAME"));
            Assert.IsTrue(result.ContainsValue(mockDataGenerator.user1["FIRST_NAME"]));
            // Assert that the correct LAST_NAME is present.
            Assert.IsTrue(result.ContainsKey("LAST_NAME"));
            Assert.IsTrue(result.ContainsValue(mockDataGenerator.user1["LAST_NAME"]));
            Assert.IsTrue(result.ContainsKey("TYPE"));
            Assert.IsTrue(result.ContainsValue("user"));
            // Remove the user from the list.
            mockDataGenerator.RemoveUser1();

        }

        [TestMethod]
        public void GetUserByUsernameAndPassword_NotExisting_ShouldReturnEmptyDictionary()
        {
            var result = databaseManager.GetUserByUsernameAndPassword(mockDataGenerator.user1["USER_NAME"], mockDataGenerator.user1["PASSWORD"]);
            // Assert that the key "USER_NAME" is present.
            Assert.IsFalse(result.ContainsKey("USER_NAME"));
            // Assert that the key "ID" is present.
            Assert.IsFalse(result.ContainsKey("ID"));
            // Assert that the key "FIRST_NAME" is present.
            Assert.IsFalse(result.ContainsKey("FIRST_NAME"));
            // Assert that the key "LAST_NAME" is present.
            Assert.IsFalse(result.ContainsKey("LAST_NAME"));
            // Assert that the "TYPE" key is present
            Assert.IsTrue(result.ContainsKey("TYPE"));
            Assert.IsTrue(result.ContainsValue("user"));
        }

        /// <summary>
        /// Tests the GetAllCitiesWithLatLon();
        /// </summary>
        [TestMethod]
        public void GetAllCitiesWithLatLon_ShouldNotBeEmpty()
        {
            // Retrieve a list of cities using the database class.
            var list = databaseManager.GetAllCitiesWithLatLon();
            // Assert that the list is not empty.
            Assert.IsTrue(list.Count > 0);
            foreach (var dictionary in list)
            {
                // Assert that all keys are present.
                Assert.IsTrue(dictionary.ContainsKey("CITY"));
                Assert.IsTrue(dictionary.ContainsKey("LAT"));
                Assert.IsTrue(dictionary.ContainsKey("LON"));
                Assert.IsTrue(dictionary.ContainsKey("ID"));
                Assert.IsTrue(dictionary.ContainsKey("TYPE"));
                Assert.IsTrue(dictionary["TYPE"].Equals( "city"));
            }
        }

        /// <summary>
        /// Tests the SetVideoCallConnection() method.
        /// </summary>
        [TestMethod]
        public void TestSetVideoCallConnection_NonExitingCallConnection_ShouldPass()
        {
            // Insert mock tables
            mockDataGenerator.InsertTable1();
            mockDataGenerator.InsertTable2();
            // Insert 
            databaseManager.SetVideoCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);

            var testDbHelper = new TestDatabaseHelper();
            var reader = testDbHelper.GetCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);
        

            // Assert that the reader is returned.
            Assert.IsNotNull(reader);
            // Assert that there is data in the reader.
            Assert.IsTrue(reader.HasRows);
            // Read the first row.
            reader.Read();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Get the column name.
                var name = reader.GetName(i);
                // Assert that the name is either "TABLE_NUMBER" or "HOTEL_ID".
                Assert.IsTrue(name.Equals("TABLE_NUMBER_1") || name.Equals("TABLE_NUMBER_2"));
                // Get the value of the column.
                var value = reader.GetValue(i);    
                // Assert that the values are those of the inserted table.
                Assert.IsTrue(value.Equals(int.Parse(mockDataGenerator.table1["TABLE_NUMBER"])) || value.Equals(int.Parse(mockDataGenerator.table2["TABLE_NUMBER"])));
            }

            //Close the connection created by the user.
            testDbHelper.CloseConnection();

            //Remove the mock data.
            testDbHelper.RemoveCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);
            mockDataGenerator.DeleteTable1();
            mockDataGenerator.DeleteTable2();
        }

        [TestMethod]
        public void TestRemoveVideoCallConnection_ShouldReturnEmptyReader()
        {
            // Insert mock tables
            mockDataGenerator.InsertTable1();
            mockDataGenerator.InsertTable2();
            // Insert 
            databaseManager.SetVideoCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);
            // Remove
            databaseManager.RemoveVideoCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);
            // Get the inserted call connection
            var testDbHelper = new TestDatabaseHelper();
            var reader = testDbHelper.GetCallConnection(mockDataGenerator.table1["TABLE_NUMBER"], mockDataGenerator.table2["TABLE_NUMBER"]);
            // Try to read the reader, since the reader.HasRows does not work.
            try
            {
                reader.Read();
                Assert.Fail();
            }catch
            {
                //Ignore the catch, this is expected.
            }
            // Close the connection created by GetCallConnection().
            testDbHelper.CloseConnection();
            // Delete mock rows.
            mockDataGenerator.DeleteTable1();
            mockDataGenerator.DeleteTable2();
        }

        [TestMethod]
        public void TestConnectGuestToTable_And_DisconnectGuestFromTable()
        {
            var databaseHelper = new TestDatabaseHelper();
            //Insert the mock data.
            mockDataGenerator.InsertTable1();
            mockDataGenerator.InsertUser1();
            // Insert the record.
            databaseManager.ConnectGuestToTable(mockDataGenerator.user1["ID"], mockDataGenerator.table1["TABLE_NUMBER"]);
            // Load the record
            var reader = databaseHelper.GetGuestToTableConnection(mockDataGenerator.user1["ID"], mockDataGenerator.table1["TABLE_NUMBER"]);
            reader.Read();
            // Loop through records.
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                // Assert that the name of the column is either "TABLE_ID" or "GUEST_ID".
                Assert.IsTrue(name.Equals("TABLE_ID") || name.Equals("GUEST_ID"));
                var value = reader.GetValue(i);
                // Assert that the value of the column is the same as the inserted values.
                Assert.IsTrue(value.Equals(int.Parse(mockDataGenerator.user1["ID"])) || value.Equals(int.Parse(mockDataGenerator.table1["TABLE_NUMBER"])));
            }
            // Close connection for GetGuestToTableConnection.
            databaseHelper.CloseConnection();
            // Remove the record using DatabaseManager.
            databaseManager.DisconnectGuestFromTable(mockDataGenerator.user1["ID"], mockDataGenerator.table1["TABLE_NUMBER"]);
            // Load the record (should be empty).
            reader = databaseHelper.GetGuestToTableConnection(mockDataGenerator.user1["ID"], mockDataGenerator.table1["TABLE_NUMBER"]);
            // Try to read the reader. This should throw an exception since the reader is empty.
            try
            {
                reader.Read();
                Assert.Fail();
            }
            catch
            {
                //Ignore the catch, this is expected.
            }
            // Close connection for GetGuestToTableConnection
            databaseHelper.CloseConnection();
            // Remove mock data
            mockDataGenerator.RemoveUser1();
            mockDataGenerator.DeleteTable1();
        }








    }
}
