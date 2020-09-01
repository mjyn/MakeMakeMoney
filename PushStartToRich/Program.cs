using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace PushStartToRich
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
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            string cabid, host;
            try
            {
                var configstrs = System.IO.File.ReadAllLines("config.txt");
                cabid = configstrs[0];
                host = configstrs[1];
            }
            catch
            {
                Console.WriteLine("bad config.");
                return -1;
            }

            var connection = new HubConnectionBuilder()
                .WithUrl($"http://{host}/cabhub?cabid={cabid}")
                .WithAutomaticReconnect(new MyRetryPolicy())
                .Build();

            connection.Reconnecting += error =>
            {
                Gpio.SetConnectionStatus(false);

                // Notify users the connection was lost and the client is reconnecting.
                // Start queuing or dropping messages.

                return Task.CompletedTask;
            };
            connection.Reconnected += connectionId =>
            {
                Gpio.SetConnectionStatus(true);

                // Notify users the connection was reestablished.
                // Start dequeuing messages queued while reconnecting if any.

                return Task.CompletedTask;
            };

            connection.On<string, string>("Coin", (inputcabid, count) =>
            {
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

            Console.ReadLine();
            return 0;
        }
    }
}
