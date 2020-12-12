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

            connection.On<string, string>("Coin", (inputcabid, insertinfo) =>
            {
                int[] insertinfoarr = { }; // count, low-time-ms, high-time-ms
                try
                {
                    insertinfoarr = insertinfo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(q => int.Parse(q.Trim())).ToArray();
                    if (insertinfoarr.Length != 3) throw new Exception("");
                }
                catch (Exception)
                {
                    _logger.LogInformation($"Coin method input invalid, at: {DateTimeOffset.Now}");
                }

                _logger.LogInformation($"{insertinfoarr[0]} coin(s) inserted, low-time: {insertinfoarr[1]}, high-time: {insertinfoarr[2]} at: {DateTimeOffset.Now}");
                if (inputcabid != cabid) return;
                Gpio.Coin(insertinfoarr[0], insertinfoarr[1], insertinfoarr[2]);
            });


            try
            {
                await connection.StartAsync();
                _logger.LogInformation($"Connection started, at: {DateTimeOffset.Now}");
                Gpio.SetConnectionStatus(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Initial connection failed: {ex.Message}. at: { DateTimeOffset.Now}");
                Environment.Exit(-1);
                return;
            }


            while (!stoppingToken.IsCancellationRequested)
            {

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
