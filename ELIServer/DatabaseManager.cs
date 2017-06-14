using FirebirdSql.Data.FirebirdClient;
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
        string connectionString =
                        "User=SYSDBA;" +
                        "Password=ELI;" +
                        "Database=C:/Users/0891435/DATA/ELI_CLIENT_DATABASE.FDB;" +
                        "DataSource=145.24.222.19;" +
                        "Port=3050;" +
                        "Dialect=3";

        public DatabaseManager()
        {
            FbConnection connection = new FbConnection(connectionString);
            connection.Open();
            Debug.WriteLine("ServerVersion" + connection.ServerVersion);
            Debug.WriteLine("State" + connection.State);

        }

    }
}
