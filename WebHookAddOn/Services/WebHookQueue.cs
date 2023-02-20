using KinAndCarta.Connect.Webhooks.Models;
using System.Collections.Concurrent;
using System.Threading;

namespace KinAndCarta.Connect.Webhooks.Services
{
    public delegate WebhookExecutionResponse WebHookCallback();

    public class WebHookQueue
    {
        private readonly BlockingCollection<WebHookCallback> _tasks;

        public WebHookQueue() => _tasks = new BlockingCollection<WebHookCallback>(new ConcurrentQueue<WebHookCallback>());

        public void Enqueue(WebHookCallback callback) => _tasks.Add(callback);

        public WebHookCallback Dequeue(CancellationToken token) => _tasks.Take(token);
    }
}
