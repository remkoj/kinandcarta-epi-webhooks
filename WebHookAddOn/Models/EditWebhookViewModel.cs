using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinAndCarta.Connect.Webhooks.Models
{
    public class EditWebhookViewModel
    {
        public WebhookPostModel Webhook { get; set; }
        public List<SelectListItem> ContentTypes { get; set; }
        public string CurrentContentName { get; set; }
        public List<int> CurrentContentAncestors { get; set; }
        public List<SelectListItem> EventTypes { get; set; }

    }
}