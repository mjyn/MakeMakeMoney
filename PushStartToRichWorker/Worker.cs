using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;

namespace PushStartToRichWorker
{
    class MyRetryPolicy : IRetryPolicy
    {

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.PreviousRetryCount < 4)
                return TimeSpan.FromSeconds(3);
            else
                return TimeSpan.FromSeconds(30);
        }
    }
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        static HubConnection connection;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (connection.State != HubConnectionState.Disconnected)
                await connection.StopAsync();
            Gpio.SetConnectionStatus(false);
            return;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Gpio.SetConnectionStatus(false);

            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            string cabid, host;
            try
            {
                var configstrs = System.IO.File.ReadAllLines("config.txt");
                cabid = configstrs[0];
                host = configstrs[1];
            }
            catch
            {
                _logger.LogError("Bad config, at: {time}", DateTimeOffset.Now);
                return;
            }

            connection = new HubConnectionBuilder()
                .WithUrl($"http://{host}/cabhub?cabid={cabid}")
                .WithAutomaticReconnect(new MyRetryPolicy())
                .Build();

            connection.Reconnecting += error =>
            {
                Gpio.SetConnectionStatus(false);
                _logger.LogWarning("Connection lost, at: {time}", DateTimeOffset.Now);
                // Notify users the connection was lost and the client is reconnecting.
                // Start queuing or dropping messages.

                return Task.CompletedTask;
            };
            connection.Reconnected += connectionId =>
            {
                Gpio.SetConnectionStatus(true);
                _logger.LogInformation("Connection resumed, at: {time}", DateTimeOffset.Now);

                // Notify users the connection was reestablished.
                // Start dequeuing messages queued while reconnecting if any.

                return Task.CompletedTask;
            };

            connection.On<string, string>("Coin", (inputcabid, count) =>
            {
                _logger.LogInformation($"{count} coin(s) inserted at {DateTimeOffset.Now}.");
                if (inputcabid != cabid) return;
                int cnt = int.Parse(count);
                Gpio.Coin(cnt);
            });


            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connection started");
                Gpio.SetConnectionStatus(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            while (!stoppingToken.IsCancellationRequested)
            {

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
