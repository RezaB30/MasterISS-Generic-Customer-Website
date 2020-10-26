using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace RadiusR_Customer_Website
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                 name: "Culture",
                 url: "{lang}/{controller}/Language/{culture}/{sender}",
                 constraints: new { lang = @"(\w{2})|(\w{2}-\w{2})" },
                 defaults: new
                 {
                     controller = "Home",
                     action = "Language",
                     lang = "tr-tr",
                     culture = "tr-tr",
                     sender = "Index"
                 });

            routes.MapRoute(
                name: "Default",
                url: "{lang}/{controller}/{action}/{id}",
                constraints: new { lang = @"(\w{2})|(\w{2}-\w{2})" },
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional,
                    lang = "tr-tr"
                });
        }
    }
}
