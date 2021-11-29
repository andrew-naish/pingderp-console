using System;

namespace pingderp_console
{
    public static class ConsoleWrite
    {
        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: {0}", message);
            Console.ResetColor();
        }

        public static void Heading(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("{0}", message);
            Console.ResetColor();
        }

        public static void Space()
        {
            // lel
            Console.WriteLine("");
        }

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: {0}", message);
            Console.ResetColor();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {

            // init database
            ConsoleWrite.Heading("Initialising Database");
            try
            {
                Database.Initialise();
                ConsoleWrite.Space();
            }
            catch (Exception ex)
            {
                ConsoleWrite.Error($"Initialising database: {ex.Message}");
                Environment.Exit(1010);
            }

            // load config xml and update hosts table in database
            ConsoleWrite.Heading("Loading XML");
            try
            {
                ConfigXml.LoadAndCreateDerplings();
                ConsoleWrite.Space();
            }
            catch (Exception ex)
            {
                if (ex is System.IO.FileNotFoundException)
                {
                    ConsoleWrite.Error("Config file not found");
                }
                else
                {
                    ConsoleWrite.Error($"Loading config file: {ex.Message}");
                }
                Environment.Exit(1000);
            }

            // countdown to view errors/warns (if there were any)
            Console.Write("Starting in .. ");
            for (int i = 4; i > 0; i--)
            {
                if (i != 4) { Console.Write("\b"); }
                Console.Write($"{i}");
                System.Threading.Thread.Sleep(1000);
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