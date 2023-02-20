using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KinAndCarta.Connect.Webhooks.Services
{
    public class WebHookSender : BackgroundService
    {
        private readonly WebHookQueue _queue;
        private readonly ILogger<WebHookSender> _logger;
        public WebHookSender(WebHookQueue queue, ILogger<WebHookSender> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield(); // This will prevent background service from blocking start up of application
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var taskToRun = _queue.Dequeue(tokenSource.Token);
                    taskToRun();
                }
                catch (OperationCanceledException)
                {
                    // execution cancelled
                }
                catch (Exception e)
                {
                    _logger.LogError("Error while processing WebHook: { errorMessage }", e.Message);
                }
            }
        }
    }
}
