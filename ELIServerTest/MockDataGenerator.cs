using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSubstitute;
using FirebirdSql;
using FirebirdSql.Data.FirebirdClient;

namespace ELIServerTest
{
    class MockDataGenerator
    {

        public Dictionary<String, String> user1 = new Dictionary<string, string> {
            { "ID", "1000"},
            { "FIRST_NAME", "User1"},
            { "LAST_NAME", "Test"},
            { "USER_NAME", "userName1"},
            { "PASSWORD", "Test"}
        };

        public Dictionary<String, String> user2 = new Dictionary<string, string> {
            { "ID", "2000"},
            { "FIRST_NAME", "User2"},
            { "LAST_NAME", "Test2"},
            { "USER_NAME", "userName2"},
            { "PASSWORD", "Test"}
        };

        public Dictionary<String, String> table1 = new Dictionary<string, string> {
            { "TABLE_NUMBER", "1000"},
            { "HOTEL_ID", "2"}
        };

        public Dictionary<String, String> table2 = new Dictionary<string, string> {
            { "TABLE_NUMBER", "2000"},
            { "HOTEL_ID", "3"}
        };



        public void InsertUser1()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.InsertUser(user1["ID"], user1["FIRST_NAME"], user1["LAST_NAME"], user1["USER_NAME"], user1["PASSWORD"]);
        }

        public void InsertUser2()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.InsertUser(user2["ID"], user2["FIRST_NAME"], user2["LAST_NAME"], user2["USER_NAME"], user2["PASSWORD"]);
        }

        public void RemoveUser1()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.RemoveUser(user1["ID"]);
        }

        public void RemoveUser2()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.RemoveUser(user2["ID"]);
        }

        public FbDataReader SelectUser1()
        {
            var dbHelper = new TestDatabaseHelper();
            return dbHelper.GetReaderForUser(user1["ID"]);
        }

        public void InsertTable1()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.InsertTable(table1["TABLE_NUMBER"], table1["HOTEL_ID"]);
        }

        public void InsertTable2()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.InsertTable(table2["TABLE_NUMBER"], table2["HOTEL_ID"]);
        }

        public void DeleteTable1()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.DeleteTable(table1["TABLE_NUMBER"], table1["HOTEL_ID"]);
        }

        public void DeleteTable2()
        {
            var dbHelper = new TestDatabaseHelper();
            dbHelper.DeleteTable(table2["TABLE_NUMBER"], table2["HOTEL_ID"]);
        }

        

    }
}
