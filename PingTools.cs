using System;
using System.Net.NetworkInformation;

namespace pingderp_console
{
    public class PingDerpling
    {
        private readonly PingWorker worker;

        private PingResult lastPingResult;
        public PingDerpling(string displayName, string host, long databaseHostId)
        {
            // set properties
            DisplayName = displayName;
            Host = host;
            DatabaseHostId = databaseHostId;

            // prepare worker
            worker = new PingWorker(host, databaseHostId);
        }

        public string DisplayName { get; private set; }
        public string Host { get; private set; }
        public long DatabaseHostId { get; set; }

        public void NewPing()
        {
            var result = worker.PingAndRecord();
            lastPingResult = result;
        }
        public void WritePing()
        {
            string pingErrorMessage = null;

            // start a new ping
            try
            {
                NewPing();
            }
            catch (PingException e)
            {
                pingErrorMessage = e.Message;
            }

            // console output: title
            Console.WriteLine("> {0}", DisplayName);

            // subtitle if there was an error
            if (pingErrorMessage != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("LAST PING - ERROR");
                Console.ResetColor();
            }

            // console output: the rest
            Console.WriteLine("");
            Console.WriteLine("Latency : {0}ms", lastPingResult.RoundtripTime);
            Console.WriteLine("Average : {0}ms", lastPingResult.Average);
            Console.WriteLine("    Min : {0}ms", lastPingResult.Minimum);
            Console.WriteLine("    Max : {0}ms", lastPingResult.Maximum);
        }
    }

    public class PingResult
    {
        public PingResult(long roundtripTime)
        {
            RoundtripTime = roundtripTime;
        }

        public PingResult(long roundtripTime, long average, long minimum, long maximum)
        {
            RoundtripTime = roundtripTime;
            Average = average;
            Minimum = minimum;
            Maximum = maximum;
        }

        public long Average { get; set; }
        public long Maximum { get; set; }
        public long Minimum { get; set; }
        public long RoundtripTime { get; set; }
    }

    internal class PingWorker
    {
        public PingWorker(string host, long host_id)
        {
            Host = host;
            HostId = host_id;
        }

        private string Host { get; set; }
        private long HostId { get; set; }

        public PingResult PingAndRecord()
        {
            // get ping
            long result = SendPing(Host);

            // add result to database
            Database.InsertResult(HostId, result);

            // query database for stats
            var stats = Database.GetStatistics(HostId);

            return new PingResult(result, stats.Average, stats.Minimum, stats.Maximum);
        }

        private static long SendPing(string host)
        {
            //  TTL 128
            PingOptions pingOptions = new(128, true);

            // 32 byte buffer (empty)
            byte[] buffer = new byte[32];

            // create new ping instance
            Ping pingSender = new();

            // calculate timeout - 80% of refresh time.
            var timeout = (ConfigXml.Settings.RefreshTime * 80) / 100;

            PingReply reply = pingSender.Send(host, timeout, buffer, pingOptions);

            return reply.RoundtripTime;
        }
    }
}