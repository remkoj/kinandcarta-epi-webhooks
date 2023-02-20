using System;
using System.Collections.Generic;
using System.Linq;

namespace KinAndCarta.Connect.Webhooks.Models
{
    public class EventType
    {
        public string Key { get; set; }
        public bool ImpactsDescendants { get; set; }
    }
}