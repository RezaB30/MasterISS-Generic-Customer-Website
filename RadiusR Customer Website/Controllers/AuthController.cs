using Microsoft.Owin;
using RadiusR.DB;
using RadiusR.SMS;
using RezaB.Web.Authentication;
using RadiusR_Customer_Website.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Security.Principal;

namespace RadiusR_Customer_Website.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseController
    {
        [HttpGet]
        // GET: Auth
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Auth
        public ActionResult Login([Bind(Include = "CustomerCode,Captcha")] LoginViewModel login)
        {
            ModelState.Remove("SMSPassword");

            if (ModelState.IsValid)
            {
                var CurrentCaptcha = Session["LoginCaptcha"] as string;
                if (login.Captcha != CurrentCaptcha)
                {
                    ModelState.AddModelError("Captcha", string.Format(RadiusRCustomerWebSite.Localization.Common.NotValid, RadiusRCustomerWebSite.Localization.Common.Captcha));
                    login.Captcha = string.Empty;
                    return View(login);
                }
                if (login.CustomerCode.StartsWith("0"))
                    login.CustomerCode = login.CustomerCode.TrimStart('0');
                using (RadiusREntities db = new RadiusREntities())
                {
                    var dbClients = db.Subscriptions.Where(sub => (sub.Customer.CustomerIDCard.TCKNo == login.CustomerCode) || sub.Customer.ContactPhoneNo == login.CustomerCode).ToList();
                    if (dbClients.Count() > 0)
                    {
                        // if need to send a new password
                        var validPasswordClient = dbClients.FirstOrDefault(client => client.OnlinePassword != null && client.OnlinePasswordExpirationDate != null);
                        if (validPasswordClient == null || validPasswordClient.OnlinePasswordExpirationDate < DateTime.Now)
                        {
                            var randomPassword = new Random().Next(100000, 1000000).ToString("000000");
                            dbClients.ForEach(client => client.OnlinePassword = randomPassword);
                            dbClients.ForEach(client => client.OnlinePasswordExpirationDate = DateTime.Now.Add(AppSettings.OnlinePasswordDuration));
                            validPasswordClient = dbClients.FirstOrDefault();
                            SMSService SMS = new SMSService();
                            SMS.SendGenericSMS(validPasswordClient.Customer.ContactPhoneNo, validPasswordClient.Customer.Culture, rawText: string.Format(RadiusRCustomerWebSite.Localization.Common.PasswordSMS, validPasswordClient.OnlinePassword, AppSettings.OnlinePasswordDuration.Hours));
                            //SMS.SendPlainText(new[] { validPasswordClient }, string.Format(RadiusRCustomerWebSite.Localization.Common.PasswordSMS, validPasswordClient.OnlinePassword, AppSettings.OnlinePasswordDuration.Hours));
                            db.SaveChanges();
                        }
                        //ViewBag.SMSWarning = string.Format(RadiusRCustomerWebSite.Localization.Common.SMSWarningMessage, AppSettings.OnlinePasswordDuration.Hours);
                        return View("SMSConfirm", login);
                    }
                    else
                    {
                        ModelState.AddModelError("CustomerCode", RadiusRCustomerWebSite.Localization.Common.ClientNotFound);
                    }
                }
            }
            return View(login);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SMSConfirm([Bind(Include = "CustomerCode,SMSPassword")] LoginViewModel login)
        {
            ModelState.Remove("Captcha");
            if (ModelState.IsValid)
            {
                if (login.CustomerCode.StartsWith("0"))
                    login.CustomerCode = login.CustomerCode.TrimStart('0');
                using (RadiusREntities db = new RadiusREntities())
                {
                    // find customers
                    var dbCustomers = db.Customers.Where(c => c.CustomerIDCard.TCKNo == login.CustomerCode || c.ContactPhoneNo == login.CustomerCode).ToArray();
                    // select a subscriber
                    var dbClient = dbCustomers.SelectMany(c => c.Subscriptions).FirstOrDefault();

                    if (dbCustomers.Count() > 0 && dbClient != null)
                    {

                        // if need to send a new password
                        if (string.IsNullOrEmpty(dbClient.OnlinePassword) || !dbClient.OnlinePasswordExpirationDate.HasValue)
                            return RedirectToAction("Login");
                        if (dbClient.OnlinePassword != login.SMSPassword)
                        {
                            ModelState.AddModelError("SMSPassword", RadiusRCustomerWebSite.Localization.Common.SMSPasswordWrong);
                            return View(login);
                        }
                        // sign in
                        SignInUser(dbClient, dbCustomers, Request.GetOwinContext());
                        return Redirect(GetRedirectUrl(Request.QueryString["ReturnUrl"]));
                    }
                    else
                    {
                        ModelState.AddModelError("CustomerCode", RadiusRCustomerWebSite.Localization.Common.ClientNotFound);
                    }
                }
            }
            //ViewBag.SMSWarning = string.Format(RadiusRCustomerWebSite.Localization.Common.SMSWarningMessage, AppSettings.OnlinePasswordDuration.Hours);
            return View(login);
        }
        public ActionResult LogOut()
        {
            SignoutUser(Request.GetOwinContext());
            return RedirectToAction("Login");
        }


        private string GetRedirectUrl(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return Url.Action("Index", "Home");
            }
            return returnUrl;
        }
        internal static void SignInUser(Subscription dbSubscription, IEnumerable<Customer> relatedCustomers, IOwinContext context)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, dbSubscription.ValidDisplayName),
                new Claim(ClaimTypes.NameIdentifier, dbSubscription.ID.ToString()),
                new Claim(ClaimTypes.SerialNumber, dbSubscription.SubscriberNo),
                new Claim("SubscriptionBag", string.Join(";", relatedCustomers.SelectMany(c => c.Subscriptions).Select(s => s.ID + "," + s.SubscriberNo)))
            }, "ApplicationCookie");
            context.Authentication.SignIn(identity);
        }

        internal static void SignInCurrentUserAgain(IOwinContext context)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var subId = context.Authentication.User.GiveUserId();
                var dbSubscription = db.Subscriptions.Include(s => s.Customer.CustomerIDCard).FirstOrDefault(s => s.ID == subId);
                // find customers
                var dbCustomers = db.Customers.Where(c => c.CustomerIDCard.TCKNo == dbSubscription.Customer.CustomerIDCard.TCKNo || c.ContactPhoneNo == dbSubscription.Customer.ContactPhoneNo).ToArray();
                context.Authentication.SignOut();
                SignInUser(dbSubscription, dbCustomers, context);
            }
        }
        internal static void SignoutUser(IOwinContext context)
        {
            context.Authentication.SignOut();
        }
    }
}