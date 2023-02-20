using EPiServer;
using EPiServer.Security;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KinAndCarta.Connect.Webhooks.Setup
{
    [MenuProvider]
    public class WebhookMenuItemProvider : IMenuProvider
    {
        public IEnumerable<MenuItem> GetMenuItems()
        {
            var menuItems = new List<MenuItem>
            {
                new UrlMenuItem("Webhooks",
                MenuPaths.Global + "/cms/cmsMenuItem",
                Paths.ToResource("KinAndCarta.Connect.Webhooks", "/WebhookAdmin/Index"))
                {
                    SortIndex = SortIndex.First + 25,
                    IsAvailable = (request) => true //PrincipalInfo.HasAdminAccess
                },

                new UrlMenuItem("Webhooks",
                MenuPaths.Global + "/cms/cmsMenuItem",
                Paths.ToResource("KinAndCarta.Connect.Webhooks", "/WebhookAdmin/Edit"))
                {
                    SortIndex = int.MaxValue,
                    IsAvailable = (request) => false
                }
            };

            return menuItems;
        }
    }
}