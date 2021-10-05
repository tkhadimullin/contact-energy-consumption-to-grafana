using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ContactEnergyPoller
{
    public static class Program
    {
        private static Worker _worker;
        private static IConfiguration _configuration;
        private static int _sleepTimeout;
        private static bool _isConsoleOutput;
        private static bool _influxDbEnabled;

        public static async Task Main(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "-d", "backtrackDate" },
                { "--date", "backtrackDate" },
            };
            
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appSettings.json", true, true)
                .AddEnvironmentVariables("CONTACT_")
                .AddCommandLine(args, switchMappings)
                .Build();

            _sleepTimeout = int.Parse(_configuration.GetSection("App").GetSection("SleepTimeout").Value);
            _isConsoleOutput = bool.Parse(_configuration.GetSection("App").GetSection("ConsoleOutput").Value);
            _influxDbEnabled = bool.Parse(_configuration.GetSection("InfluxDb").GetSection("Enabled").Value);

            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting up");

            _worker = new Worker(_configuration);
            if (_configuration["backtrackDate"] != null)
            {
                var backtrackDate = DateTime.Parse(_configuration["backtrackDate"]);
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Getting consumption for {backtrackDate}");
                
                await HandleJob(backtrackDate);

                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Done");
            }
            else
            {
                while (true)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting loop run...");
                    await HandleJob();
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sleeping for {_sleepTimeout} ms...");
                    Thread.Sleep(_sleepTimeout);
                }
            }
        }

        private static async Task HandleJob(DateTime? date = null)
        {
            var payload = await _worker.InvokeAsync(date);

            if (payload == null)
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] No payload to store. Skipping");
            }
            else
            {
                if (_influxDbEnabled)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] No payload to store. Skipping");
                    var writer = new InfluxWriter(_configuration);
                    await writer.WriteAsync(payload);
                }

                if (_isConsoleOutput)
                {
                    var wr = new StringWriter();
                    payload.Format(wr);
                    Console.WriteLine(wr.GetStringBuilder().ToString());
                }
            }
        }
    }
}
