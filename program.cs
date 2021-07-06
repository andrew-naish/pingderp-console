using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace pingderp_console
{

    public static class ConsoleWrite
    {
        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: {0}", message);
            Console.ResetColor();
        }
    }

    public static class ConfigXml
    {
        public static List<Derpling> AllDerplings = new();

        public static class Settings
        {
            public static int RefreshTime { get; set; } = 1500;
        }

        public static void Load()
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

                // otherwise create a derpling and add to list
                var newDerpling = new Derpling(name, host);
                AllDerplings.Add(newDerpling);
            }

        }

    }

    public class Derpling
    {
        public Derpling(string displayName, string host)
        {
            DisplayName = displayName;
            worker = new PingWorker(host);
        }

        private string DisplayName { get; set; }
        private PingResult lastPingResult;
        private readonly PingWorker worker;

        public void NewPing()
        {
            var result = worker.PingAndRecord();
            lastPingResult = result;
        }

        public void WritePing()
        {

            // start a new ping
            string pingErrorMessage = null;
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

        public long RoundtripTime { get; set; }
        public long Average { get; set; }
        public long Minimum { get; set; }
        public long Maximum { get; set; }
    }

    internal class PingWorker
    {
        private string Host { get; set; }
        private List<long> results = new();

        public PingWorker(string host)
        {
            Host = host;
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

        public PingResult PingAndRecord()
        {
            long result = SendPing(Host);
            results.Add(result);

            return new PingResult(result, (long)results.Average(), results.Min(), results.Max());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            // load config xml
            try
            {
                ConfigXml.Load();
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Config file not found!");
                Environment.Exit(1);
            }

            // loop
            while (true)
            {
                // clear console
                Console.Clear();

                // show each ping
                for (var i = 0; i < ConfigXml.AllDerplings.Count; i++)
                {
                    ConfigXml.AllDerplings[i].WritePing();
                    Console.WriteLine();
                }

                // sleep for refresh time
                System.Threading.Thread.Sleep(ConfigXml.Settings.RefreshTime);
            }

        }
    }
}
