using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;

namespace pingderp_console
{
    public static class ConfigXml
    {
        public static List<PingDerpling> AllDerplings = new();

        public static void LoadAndCreateDerplings()
        {
            // load xml
            XDocument xdoc = XDocument.Load("config.xml");

            // check and set RefreshTime; if xml is set to less than the minumum allowed value - disregard it.
            var refreshTime = (int)xdoc.Root.Element("Settings").Element("RefreshTime");
            if (refreshTime >= Settings.RefreshTime)
            {
                Settings.RefreshTime = refreshTime;
            }

            // check and set derplings
            foreach (var derpling in xdoc.Root.Element("PingDerplings").Descendants())
            {
                var name = (string)derpling.Attribute("name");
                var host = (string)derpling.Attribute("host");

                // skip this item if either are null
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(host))
                {
                    ConsoleWrite.Warning("Not adding derpling because either the name or the value are blank.");
                    continue;
                }

                // update database
                var databaseHostId = Database.InsertHost(name, host);

                // otherwise create a derpling and add to list
                var newDerpling = new PingDerpling(name, host, databaseHostId);
                AllDerplings.Add(newDerpling);
            }
        }

        public static class Settings
        {
            public static int RefreshTime { get; set; } = 1500;
        }
    }

    public static class Database
    {
        private static SQLiteConnection dbConnection;
        private static string DatabaseName { get; set; } = "results.sqlite";

        public static void Initialise()
        {
            // create db file if it does not exist
            if (!File.Exists(DatabaseName))
            {
                SQLiteConnection.CreateFile(DatabaseName);
            }

            // connect
            dbConnection = new SQLiteConnection($"Data Source={DatabaseName};Version=3;");
            dbConnection.Open();

            // recreate hosts table
            SendCommand("DROP TABLE IF EXISTS hosts");
            SendCommand("CREATE TABLE hosts(host_id INTEGER PRIMARY KEY, DisplayName text, Host text)");

            // recreate results table
            SendCommand("DROP TABLE IF EXISTS ping_results");
            SendCommand("CREATE TABLE ping_results( " +
                
                // columns
                "result_id INTEGER PRIMARY KEY," +
                //"Host text NOT NULL," +
                "RTT int NOT NULL," +
                "host_id int NOT NULL," +

                // set foreign key
                "FOREIGN KEY (host_id) REFERENCES hosts(host_id)" +

            ")");
        }

        public static long InsertHost(string DisplayName, string Host)
        {
            // add to database and retreive row id
            SendCommand($"INSERT INTO hosts(DisplayName, Host) VALUES('{DisplayName}', '{Host}')");
            var rowID = dbConnection.LastInsertRowId;

            // return row id
            return rowID;
        }

        public static void InsertResult(long HostId, long Result)
        {
            var Query = "INSERT INTO ping_results(host_id, RTT) VALUES(@host_id, @rtt)";

            var dbCommand = new SQLiteCommand(dbConnection)
            {
                CommandText = Query
            };

            dbCommand.Parameters.AddWithValue("@host_id", HostId);
            dbCommand.Parameters.AddWithValue("@rtt", Result);

            dbCommand.Prepare();
            _ = dbCommand.ExecuteNonQuery();
        }

        internal static StatsResult GetStatistics(long HostId)
        {
            var Query = $"SELECT CAST(round(avg(RTT),0) AS bigint) as 'avg', CAST(round(MIN(RTT),0) AS bigint) as 'min', CAST(round(MAX(RTT),0) AS bigint) as 'max' FROM ping_results WHERE host_id = '{HostId}'";
            var dbCommand = new SQLiteCommand(dbConnection)
            {
                CommandText = Query
            };

            var queryResult = dbCommand.ExecuteReader();
            queryResult.Read();

            return new StatsResult
            {
                Average = (long)queryResult["avg"],
                Minimum = (long)queryResult["min"],
                Maximum = (long)queryResult["max"]
            };
        }

        private static void SendCommand(string Query)
        {
            var dbCommand = new SQLiteCommand(dbConnection)
            {
                CommandText = Query
            };
            _ = dbCommand.ExecuteNonQuery();
        }

        internal class StatsResult
        {
            internal long Average { get; set; }
            internal long Maximum { get; set; }
            internal long Minimum { get; set; }
        }
    }
}