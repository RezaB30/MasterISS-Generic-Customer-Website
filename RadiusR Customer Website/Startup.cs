using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: OwinStartupAttribute(typeof(RadiusR_Customer_Website.Startup))]
namespace RadiusR_Customer_Website
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            CookieAuthenticationProvider provider = new CookieAuthenticationProvider();

            var originalHandler = provider.OnApplyRedirect;

            //Our logic to dynamically modify the path (maybe needs some fine tuning)
            provider.OnApplyRedirect = context =>
            {
                var mvcContext = new HttpContextWrapper(HttpContext.Current);
                var routeData = RouteTable.Routes.GetRouteData(mvcContext);

                //Get the current language  
                RouteValueDictionary routeValues = new RouteValueDictionary();
                routeValues.Add("lang", routeData.Values["lang"]);

                //Reuse the RetrunUrl
                Uri uri = new Uri(context.RedirectUri);
                string returnUrl = HttpUtility.ParseQueryString(uri.Query)[context.Options.ReturnUrlParameter];
                routeValues.Add(context.Options.ReturnUrlParameter, returnUrl);

                //Overwrite the redirection uri
                context.RedirectUri = url.Action("DirectLogin", "Auth", routeValues);
                originalHandler.Invoke(context);
            };
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString(url.Action("DirectLogin", "Auth")),
                //Set the Provider
                Provider = provider,
                CookieName = "RadiusRCustomer",
                ExpireTimeSpan = Properties.Settings.Default.CookieExpiration,
                CookieSameSite = SameSiteMode.None,
                CookieHttpOnly = true,                
                CookieSecure = CookieSecureOption.SameAsRequest
            });
        }
    }
}