using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace NdlsCheckerWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly NdlsConfigOptions _ndlsConfigOptions;
        private readonly PushBulletConfigOptions _pushBulletConfigOptions;
        private Regex _regexConfirm = new Regex("confirmBooking\\(\\'(?<availableDate>([0-9]|-|:| )*)\\'\\)");

        public Worker(ILogger<Worker> logger, IHttpClientFactory factory, IOptions<NdlsConfigOptions> ndlsConfigOptions, IOptions<PushBulletConfigOptions> pushBulletOptions)
        {
            _logger = logger;
            _factory = factory;
            _ndlsConfigOptions = ndlsConfigOptions.Value;
            _pushBulletConfigOptions = pushBulletOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await CheckAvailableDates();
                
                await Task.Delay(90000, stoppingToken);
            }
        }

        private async Task CheckAvailableDates()
        {
            await GetIndividualDate("Leopardstown", "15");
            await GetIndividualDate("CityWest", "16");
            await GetIndividualDate("Swords", "17");
            await GetIndividualDate("ClareHall", "42");
            await GetIndividualDate("Athlone", "22");
            await GetIndividualDate("Mullingar", "35");
        }

        private async Task GetIndividualDate(string centreName, string centreCode)
        {
            var client = _factory.CreateClient();

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("https://booking.ndls.ie/index.php");

            var dict = new Dictionary<string, string> {{"Centre", centreCode}};

            request.Content = new FormUrlEncodedContent(dict);


            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                await ParseContent(result, centreName);
            }
        }

        private async Task ParseContent(string htmlSource, string centreName)
        {
            foreach (Match match in _regexConfirm.Matches(htmlSource))
            {
                var date = match.Groups["availableDate"].Value;

                var dateTime = DateTime.Parse(date);
                
                if (dateTime >= _ndlsConfigOptions.GetNoEarlierThan() && dateTime < _ndlsConfigOptions.GetNoLaterThan())
                {
                    await FireAlarm(date, centreName);
                }
            }
        }

        private async Task FireAlarm(string datetime, string centreName)
        {
            var client = _factory.CreateClient();
            
            var request  = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("https://api.pushbullet.com/v2/pushes");
            
            
            request.Headers.Add("Access-Token", _pushBulletConfigOptions.AccessToken);
            

            request.Content = new StringContent($"{{\"body\":\"Carai borracha, deu bom em {centreName} as {datetime}!\",\"title\":\"Corre la truta\",\"type\":\"note\"}}", Encoding.UTF8, "application/json");


            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
            }
        }
    }
}