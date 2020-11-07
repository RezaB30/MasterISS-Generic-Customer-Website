using NLog;
using RadiusR.DB;
using RezaB.Web.Authentication;
using RadiusR_Customer_Website.Properties;
//using RadiusR_Manager.Models.RadiusViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using RezaB.Web;
using RezaB.Web.Extentions;

namespace RadiusR_Customer_Website.Controllers
{
    public abstract class BaseController : Controller
    {
        private static Logger logger = LogManager.GetLogger("main");
        private static Logger requestLogger = LogManager.GetLogger("requestLogger");

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            // log request
            if (Request.IsAuthenticated)
            {
                var cookies = new List<string>();
                foreach (var key in Request.Cookies.AllKeys)
                {
                    cookies.Add(key + ": " + Request.Cookies[key].Value);
                }
                var eventLog = new LogEventInfo(LogLevel.Trace, "requestLogger", "Request:");
                eventLog.Properties["subscriberNo"] = User.GiveUserId();
                eventLog.Properties["url"] = Request.Url.AbsoluteUri;
                eventLog.Properties["userAgent"] = Request.UserAgent;
                eventLog.Properties["httpMethod"] = Request.HttpMethod;
                eventLog.Properties["userHostAddress"] = Request.UserHostAddress;
                eventLog.Properties["userCookies"] = string.Join(Environment.NewLine, cookies);
                requestLogger.Log(eventLog);
            }
            else
            {
                var cookies = new List<string>();
                foreach (var key in Request.Cookies.AllKeys)
                {
                    cookies.Add(key + ": " + Request.Cookies[key].Value);
                }
                var eventLog = new LogEventInfo(LogLevel.Trace, "requestLogger", "Request:");
                eventLog.Properties["subscriberNo"] = "anonymus";
                eventLog.Properties["url"] = Request.Url.AbsoluteUri;
                eventLog.Properties["userAgent"] = Request.UserAgent;
                eventLog.Properties["httpMethod"] = Request.HttpMethod;
                eventLog.Properties["userHostAddress"] = Request.UserHostAddress;
                eventLog.Properties["userCookies"] = string.Join(Environment.NewLine, cookies);
                requestLogger.Log(eventLog);
            }
        }
        protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
        {


            //Localization in Base controller:

            string lang = CookieTools.getCulture(Request.Cookies);

            var routeData = RouteData.Values;
            var routeCulture = routeData.Where(r => r.Key == "lang").FirstOrDefault();
            if (string.IsNullOrEmpty((string)routeCulture.Value) || routeCulture.Value.ToString() != lang)
            {
                routeData.Remove("lang");
                routeData.Add("lang", lang);

                Thread.CurrentThread.CurrentUICulture =
                Thread.CurrentThread.CurrentCulture =
                CultureInfo.GetCultureInfo(lang);

                Response.RedirectToRoute(routeData);
            }
            else
            {
                lang = (string)RouteData.Values["lang"];

                Thread.CurrentThread.CurrentUICulture =
                    Thread.CurrentThread.CurrentCulture =
                    CultureInfo.GetCultureInfo(lang);
            }

            ViewBag.Version = Settings.Default.Version;
            ViewBag.Copyright = Settings.Default.Copyright;
            ViewBag.WebSite = Settings.Default.WebSite;
            ViewBag.CompanyTitle = Settings.Default.CompanyTitle;
            return base.BeginExecuteCore(callback, state);
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            logger.Error(filterContext.Exception);            
            //var error = ErrorHandler.GetMessage(filterContext.Exception, Request.IsLocal);
            //filterContext.ExceptionHandled = true;
            ////filterContext.HttpContext.Response.Clear();
            //filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ////filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            //filterContext.Result = Error(error.Message, error.Details);
        }

        [AllowAnonymous]
        [HttpGet, ActionName("Language")]
        public virtual ActionResult Language(string culture, string sender)
        {
            CookieTools.SetCultureInfo(Response.Cookies, culture);

            Dictionary<string, object> responseParams = new Dictionary<string, object>();
            Request.QueryString.CopyTo(responseParams);
            responseParams.Add("lang", culture);

            return RedirectToAction(sender, new RouteValueDictionary(responseParams));
        }

        public virtual ActionResult Error(string message, string details)
        {
            if (Request.IsAjaxRequest())
            {
                return Json(new { Code = 1, Message = message, Details = details }, JsonRequestBehavior.AllowGet);
            }
            ViewBag.Message = message;
            ViewBag.Details = details;
            return View("ErrorDialogBox");
        }

        protected void SetupPages<T>(int? page, ref IQueryable<T> viewResults, int? rowCount = null)
        {
            rowCount = rowCount ?? AppSettings.TableRows;
            var totalCount = viewResults.Count();
            var pagesCount = Math.Ceiling((float)totalCount / (float)rowCount);
            ViewBag.PageCount = pagesCount;
            ViewBag.PageTotalCount = totalCount;

            if (!page.HasValue)
            {
                page = 0;
            }

            viewResults = viewResults.PageData(page.Value, rowCount.Value);
        }

        protected string ViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}