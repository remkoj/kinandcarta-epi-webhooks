using EPiServer;
using EPiServer.ContentApi.Core.Serialization;
using EPiServer.ContentApi.Core.Serialization.Internal;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using KinAndCarta.Connect.Webhooks.Data;
using KinAndCarta.Connect.Webhooks.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using EPiServer.ContentApi.Core.Configuration;
using EPiServer.Cms.Shell;
using System.Globalization;
using EPiServer.Web;
using Microsoft.Extensions.Hosting.Internal;
using System.Net.Http;
using KinAndCarta.Connect.Webhooks.Services;

namespace KinAndCarta.Connect.Webhooks.Extensions
{
    public static class WebhookExtensions
    {
        private static readonly Lazy<IContentConverterResolver> _iContentConverterResolver = new Lazy<IContentConverterResolver>(() => ServiceLocator.Current.GetInstance<IContentConverterResolver>());
        private static readonly Lazy<IContentRepository> _contentRepository = new Lazy<IContentRepository>(() => ServiceLocator.Current.GetInstance<IContentRepository>());
        private static readonly Lazy<ContentApiOptions> _contentApiOptions = new Lazy<ContentApiOptions>(() => ServiceLocator.Current.GetInstance<ContentApiOptions>());
        private static readonly Lazy<WebHookQueue> _webHookQueue = new Lazy<WebHookQueue>(() => ServiceLocator.Current.GetInstance<WebHookQueue>());
        private static readonly int _maxDescendents = int.Parse(ConfigurationManager.AppSettings["WebhookMaxDescendents"] ?? "250");

        public static WebhookExecutionResponse Execute(this Webhook webhook, IContent content, EventType eventType, IWebhookRepository repo, Dictionary<string, object> extraData = null)
        {
            var res = new WebhookExecutionResponse { Response = string.Empty, Success = false };
            try
            {
                IContentConverter mapper = _iContentConverterResolver.Value.Resolve(content);
                var refs = _contentRepository.Value.GetReferencesToContent(content.ContentLink, eventType.ImpactsDescendants);
                var payload = new WebhookDetailedPayload(content, eventType.Key, DateTime.UtcNow);
                var referencedBy = new List<ContentInfo>();
                referencedBy.AddRange(refs.Select(x => new ContentInfo(_contentRepository.Value.Get<IContent>(x.OwnerID, x.OwnerLanguage))));
                if (eventType.ImpactsDescendants)
                {
                    var descendents = _contentRepository.Value.GetDescendents(content.ContentLink);
                    foreach (var descendent in descendents.Take(_maxDescendents))
                    {
                        if (!referencedBy.Any(x => x.ContentId.Equals(descendent.ID)))
                        {
                            referencedBy.Add(new ContentInfo(_contentRepository.Value.Get<IContent>(descendent)));
                        }
                    }
                }
                payload.ReferencedBy = referencedBy.GroupBy(x => x.ContentId).Select(x => x.FirstOrDefault());
                payload.Content = mapper.Convert(content, new ConverterContext(
                    content.ContentLink,
                    CultureInfo.GetCultureInfo(content.LanguageBranch()),
                    _contentApiOptions.Value,
                    ContextMode.Default,
                    string.Empty, string.Empty, true)
                    );
                payload.ExtraData = extraData;

                var url = repo.ReplacePlaceholders(webhook.Url);
                var uri = new Uri(url);
                using (var hc = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, uri);
                    foreach (var header in webhook.Headers ?? new Dictionary<string, string>())
                    {
                        request.Headers.Add(repo.ReplacePlaceholders(header.Key), repo.ReplacePlaceholders(header.Value));
                    }
                    request.Headers.Add(HttpRequestHeader.ContentType.ToString(), "application/json");
                    request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = hc.Send(request);
                    res.Response = response.Content.ReadAsStringAsync().Result;
                    res.Success = true;
                }
                webhook.LastResult = $"OK\n{res.Response}";
            }
            catch (WebException ex)
            {
                res.Response = $"{ex?.Message ?? string.Empty}\n\n{ex?.StackTrace ?? string.Empty}";
                webhook.LastResult = $"ERROR ({ex.Status})\n\n{ex?.Message ?? string.Empty}\n\n{ex?.StackTrace ?? string.Empty}";
            }
            catch (Exception ex)
            {
                res.Response = $"{ex?.Message ?? string.Empty}\n\n{ex?.StackTrace ?? string.Empty}";
                webhook.LastResult = $"ERROR\n\n{ex?.Message ?? string.Empty}\n\n{ex?.StackTrace ?? string.Empty}";
            }
            webhook.LastExecuted = DateTime.UtcNow;
            repo.SaveWebhook(webhook);
            return res;
        }

        public static void TriggerWebhooks(this IContent content, EventType eventType, IWebhookRepository repo, Dictionary<string, object> extraData = null)
        {
            var webhooks = repo.GetWebhooksForContentEvent(content, eventType);
            if (!webhooks.Any()) return;
            foreach (var webhook in webhooks)
                _webHookQueue.Value.Enqueue(() => webhook.Execute(content, eventType, repo, extraData));
        }
    }
}