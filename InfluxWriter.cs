using System;
using System.Threading.Tasks;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Microsoft.Extensions.Configuration;

namespace ContactEnergyPoller
{
    public class InfluxWriter
    {
        private readonly string _influxDbUrl;
        private readonly string _influxDatabase;

        public InfluxWriter(IConfiguration configuration)
        {
            _influxDbUrl = configuration.GetSection("InfluxDb").GetSection("Url").Value;
            _influxDatabase = configuration.GetSection("InfluxDb").GetSection("Database").Value;
        }

        public async Task WriteAsync(LineProtocolPayload payload)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Saving payload to InfluxDb: {_influxDbUrl}");
            var influxClient = new LineProtocolClient(new Uri(_influxDbUrl), _influxDatabase);
            var result = await influxClient.WriteAsync(payload);
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Response from InfluxDb: Success={result.Success}, ErrorMessage={result.ErrorMessage}");


        }
    }
}
