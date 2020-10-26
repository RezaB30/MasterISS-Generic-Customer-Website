using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace RadiusR_Customer_Website
{
    public static class LanguageLinkHelper
    {
        /// <summary>
        /// Creates a link for changing language
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText">link text</param>
        /// <param name="culture">culture name</param>
        /// <returns></returns>
        public static string LanguageLink(this UrlHelper url, string culture, ViewContext context)
        {
            var currentRouteData = context.RouteData.Values;
            HttpContext.Current.Request.QueryString.CopyTo(currentRouteData);
            object action;
            currentRouteData.TryGetValue("action", out action);
            currentRouteData["sender"] = action;
            currentRouteData["culture"] = culture;

            return url.Action("Language", currentRouteData);
        }
    }
}