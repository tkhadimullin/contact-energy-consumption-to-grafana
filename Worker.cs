using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ContactEnergyPoller.Models;
using InfluxDB.LineProtocol.Payload;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ContactEnergyPoller
{
    public class Worker
    {
        private readonly HttpClient _client;


        private readonly string _influxMeasurement;

        private readonly bool _isConsoleOutput;
        private readonly string _loginUrl;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _contractId;
        private readonly string _contractLocation;

        public Worker(IConfiguration configuration)
        {
            _influxMeasurement = configuration.GetSection("InfluxDb").GetSection("Measurement").Value;

            _loginUrl = configuration["ContactApi:LoginUrl"];
            _userName = configuration["ContactApi:UserName"];
            _password = configuration["ContactApi:Password"];
            _contractId = configuration["ContactApi:ContractId"];
            _contractLocation = configuration["ContactApi:ContractLocation"];

            var apiToken = configuration["ContactApi:ApiKey"];

            _client = new HttpClient
            {
                BaseAddress = new Uri(configuration["ContactApi:BaseURL"])
            };

            _client.DefaultRequestHeaders.Add("x-api-key", apiToken);
        }

        public async Task<LineProtocolPayload> InvokeAsync(DateTime? date = null)
        {
            DateTime queryDate = date ?? DateTime.UtcNow;
            Console.WriteLine($"Getting usage for {queryDate}");

            var loginResponse = await _client.PostAsync(_loginUrl, new StringContent($"{{\"UserName\": \"{_userName}\", \"Password\": \"{_password}\", \"AllowNewOlsSiteAccess\": \"true\", \"NewOlsSiteAccessValue\": \"true\"}}", Encoding.UTF8, "application/json"));
            loginResponse.EnsureSuccessStatusCode();
            dynamic login = JsonConvert.DeserializeObject(await loginResponse.Content.ReadAsStringAsync());
            if (!string.Equals(login.IsSuccessful.ToString(), "true", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"Error authenticating: {login.Errors}");
                return null;
            }

            string authToken = login.Data.Token.ToString();
            _client.DefaultRequestHeaders.TryAddWithoutValidation("authorization", authToken);
            Console.WriteLine($"Got Auth token");

            var contractsToQuery = new Dictionary<string, string> {{_contractId, _contractLocation}};

            var payload = new LineProtocolPayload();

            foreach (var contract in contractsToQuery.Keys)
            {
                var from = queryDate.Date.AddDays(-1).ToString("yyyy-MM-dd");
                var to = queryDate.Date.AddDays(-1).ToString("yyyy-MM-dd");
                Console.WriteLine($"Querying usage for today on {contract} from {from} to {to}...");
                var usageResponse = await _client.PostAsync($"/usage/{contract}?interval=hourly&from={from}&to={to}", new StringContent(""));
                usageResponse.EnsureSuccessStatusCode();
                var usage = JsonConvert.DeserializeObject<List<Usage>>(await usageResponse.Content.ReadAsStringAsync());

                foreach (var item in usage)
                {
                    var point = new LineProtocolPoint(
                        _influxMeasurement,
                        new Dictionary<string, object>
                        {
                            { "totalPower", item.Value },
                            { "power", item.Value-item.UnchargedValue },
                            { "freePower", item.UnchargedValue },
                        },
                        new Dictionary<string, string>
                        {
                            {"ContractId", contract},
                            {"Address", contractsToQuery[contract]},
                        }
                        ,
                        item.Date.ToUniversalTime());
                    payload.Add(point);
                }
            }

            return payload;
        }
    }
}
