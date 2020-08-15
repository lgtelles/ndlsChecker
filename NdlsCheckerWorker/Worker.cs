using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NdlsCheckerWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _factory;

        public Worker(ILogger<Worker> logger, IHttpClientFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await CheckAvailableDates();
                
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task CheckAvailableDates()
        {
            var client = _factory.CreateClient();
            
            var request  = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("https://booking.ndls.ie/index.php");

            var dict = new Dictionary<string, string> {{"Centre", "15"}};

            request.Content = new FormUrlEncodedContent(dict);


            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                await ParseContent(result);
            }
        }

        private async Task ParseContent(string htmlSource)
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);

            var document = await context.OpenAsync(req=> req.Content(htmlSource));

            var availableDates = document.QuerySelectorAll(".AvailableDate");

            foreach (var item in availableDates)
            {
                if (item.InnerHtml.Contains("Aug"))
                {
                    await FireAlarm();
                }
                
                
            }

        }

        private async Task FireAlarm()
        {
            
        }
    }
}