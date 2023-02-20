using System;
using System.Collections.Generic;
using System.Linq;

namespace KinAndCarta.Connect.Webhooks.Models
{
    public class WebhookExecutionResponse
    {
        public bool Success { get; set; }
        public string Response { get; set; }
    }
}