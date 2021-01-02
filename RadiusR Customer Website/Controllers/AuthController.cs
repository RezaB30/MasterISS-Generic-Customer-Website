﻿using Microsoft.Owin;
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
using RadiusR.DB.Settings;
using System.Net;
using RadiusR_Customer_Website.GenericCustomerServiceReference;

namespace RadiusR_Customer_Website.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseController
    {
        GenericCustomerServiceReference.GenericCustomerServiceClient client = new GenericCustomerServiceReference.GenericCustomerServiceClient();
        [HttpGet]
        // GET: Auth
        public ActionResult DirectLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST: Auth
        public ActionResult DirectLogin([Bind(Include = "CustomerCode")] LoginViewModel login)
        {
            ModelState.Remove("SMSPassword");
            ModelState.Remove("Password");
            if (ModelState.IsValid)
            {
                if (login.CustomerCode.StartsWith("0"))
                    login.CustomerCode = login.CustomerCode.TrimStart('0');

                var baseRequest = new GenericServiceSettings();
                var result = client.CustomerAuthentication(new GenericCustomerServiceReference.CustomerServiceAuthenticationRequest()
                {
                    Culture = baseRequest._culture,
                    Rand = baseRequest._rand,
                    Username = baseRequest._username,
                    Hash = baseRequest.hash,
                    AuthenticationParameters = new GenericCustomerServiceReference.AuthenticationRequest()
                    {
                        PhoneNo = login.CustomerCode,
                        TCK = login.CustomerCode
                    }
                });
                if (result.ResponseMessage.ErrorCode == 0)
                {
                    return View("SMSConfirm", login);
                }
                else
                {
                    ModelState.AddModelError("CustomerCode", result.ResponseMessage.ErrorMessage);
                }
            }
            return View(login);
        }
        public ActionResult Login()
        {
            return RedirectToAction("DirectLogin");
            //return View();
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Login([Bind(Include = "CustomerCode,Password")] LoginViewModel login)
        //{
        //    ModelState.Remove("SMSPassword");
        //    if (ModelState.IsValid)
        //    {
        //        //var CurrentCaptcha = Session["LoginCaptcha"] as string;
        //        //if (login.Captcha != CurrentCaptcha)
        //        //{
        //        //    ModelState.AddModelError("Captcha", string.Format(RadiusRCustomerWebSite.Localization.Common.NotValid, RadiusRCustomerWebSite.Localization.Common.Captcha));
        //        //    login.Captcha = string.Empty;
        //        //    return View(login);
        //        //}
        //        if (login.CustomerCode.StartsWith("0"))
        //            login.CustomerCode = login.CustomerCode.TrimStart('0');
        //        using (RadiusREntities db = new RadiusREntities())
        //        {
        //            // find customers
        //            var dbCustomers = db.Customers.Where(c => c.CustomerIDCard.TCKNo == login.CustomerCode || c.ContactPhoneNo == login.CustomerCode).ToArray();
        //            // select a subscriber
        //            var dbClient = dbCustomers.SelectMany(c => c.Subscriptions).FirstOrDefault();

        //            if (dbCustomers.Count() > 0 && dbClient != null)
        //            {

        //                // if need to send a new password
        //                if (string.IsNullOrEmpty(dbClient.OnlinePassword) || !dbClient.OnlinePasswordExpirationDate.HasValue)
        //                    return RedirectToAction("DirectLogin");
        //                if (dbClient.OnlinePassword != login.Password)
        //                {
        //                    ModelState.AddModelError("Password", RadiusRCustomerWebSite.Localization.Common.SMSPasswordWrong);
        //                    return View(login);
        //                }
        //                // sign in
        //                SignInUser(dbClient, dbCustomers, Request.GetOwinContext());
        //                return Redirect(GetRedirectUrl(Request.QueryString["ReturnUrl"]));
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("CustomerCode", RadiusRCustomerWebSite.Localization.Common.ClientNotFound);
        //            }
        //        }
        //    }
        //    return View(login);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SMSConfirm([Bind(Include = "CustomerCode,SMSPassword")] LoginViewModel login)
        {
            ModelState.Remove("Password");
            if (ModelState.IsValid)
            {
                if (login.CustomerCode.StartsWith("0"))
                    login.CustomerCode = login.CustomerCode.TrimStart('0');

                var baseRequest = new GenericServiceSettings();
                var result = client.AuthenticationSMSConfirm(new GenericCustomerServiceReference.CustomerServiceAuthenticationSMSConfirmRequest()
                {
                    Culture = baseRequest._culture,
                    Hash = baseRequest.hash,
                    Rand = baseRequest._rand,
                    Username = baseRequest._username,
                    AuthenticationSMSConfirmParameters = new GenericCustomerServiceReference.AuthenticationSMSConfirmRequest()
                    {
                        CustomerCode = login.CustomerCode,
                        SMSPassword = login.SMSPassword
                    }
                });
                if (result.ResponseMessage.ErrorCode == 0)
                {
                    // sign in
                    SignInUser(result.AuthenticationSMSConfirmResponse.ValidDisplayName, result.AuthenticationSMSConfirmResponse.ID.ToString(), result.AuthenticationSMSConfirmResponse.SubscriberNo, result.AuthenticationSMSConfirmResponse.RelatedCustomers, Request.GetOwinContext());
                    return Redirect(GetRedirectUrl(Request.QueryString["ReturnUrl"]));
                }
                else
                {
                    ModelState.AddModelError("CustomerCode", result.ResponseMessage.ErrorMessage);
                }
                //using (RadiusREntities db = new RadiusREntities())
                //{
                //    // find customers
                //    var dbCustomers = db.Customers.Where(c => c.CustomerIDCard.TCKNo == login.CustomerCode || c.ContactPhoneNo == login.CustomerCode).ToArray();
                //    // select a subscriber
                //    var dbClient = dbCustomers.SelectMany(c => c.Subscriptions).FirstOrDefault();

                //    if (dbCustomers.Count() > 0 && dbClient != null)
                //    {

                //        // if need to send a new password
                //        if (string.IsNullOrEmpty(dbClient.OnlinePassword) || !dbClient.OnlinePasswordExpirationDate.HasValue)
                //            return RedirectToAction("Login");
                //        if (dbClient.OnlinePassword != login.SMSPassword)
                //        {
                //            ModelState.AddModelError("SMSPassword", RadiusRCustomerWebSite.Localization.Common.SMSPasswordWrong);
                //            return View(login);
                //        }
                //        // sign in
                //        SignInUser(dbClient, dbCustomers, Request.GetOwinContext());
                //        return Redirect(GetRedirectUrl(Request.QueryString["ReturnUrl"]));
                //    }
                //    else
                //    {
                //        ModelState.AddModelError("CustomerCode", RadiusRCustomerWebSite.Localization.Common.ClientNotFound);
                //    }
                //}
            }
            //ViewBag.SMSWarning = string.Format(RadiusRCustomerWebSite.Localization.Common.SMSWarningMessage, AppSettings.OnlinePasswordDuration.Hours);
            return View(login);
        }
        //public ActionResult GetPassword()
        //{
        //    return View();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult GetPassword([Bind(Include = "CustomerCode")] LoginViewModel login)
        //{
        //    ModelState.Remove("Password");
        //    ModelState.Remove("SMSPassword");
        //    if (ModelState.IsValid)
        //    {
        //        if (login.CustomerCode.StartsWith("0"))
        //            login.CustomerCode = login.CustomerCode.TrimStart('0');
        //        using (RadiusREntities db = new RadiusREntities())
        //        {
        //            var dbClients = db.Subscriptions.Where(sub => (sub.Customer.CustomerIDCard.TCKNo == login.CustomerCode) || sub.Customer.ContactPhoneNo == login.CustomerCode).ToList();
        //            if (dbClients.Count() > 0)
        //            {
        //                // if need to send a new password
        //                var validPasswordClient = dbClients.FirstOrDefault(client => client.OnlinePassword != null && client.OnlinePasswordExpirationDate != null);
        //                if (validPasswordClient == null || validPasswordClient.OnlinePasswordExpirationDate < DateTime.Now)
        //                {
        //                    var randomPassword = new Random().Next(100000, 1000000).ToString("000000");
        //                    dbClients.ForEach(client => client.OnlinePassword = randomPassword);
        //                    dbClients.ForEach(client => client.OnlinePasswordExpirationDate = DateTime.Now.Add(CustomerWebsiteSettings.OnlinePasswordDuration));
        //                    validPasswordClient = dbClients.FirstOrDefault();
        //                    SMSService SMS = new SMSService();
        //                    SMS.SendGenericSMS(validPasswordClient.Customer.ContactPhoneNo, validPasswordClient.Customer.Culture, rawText: string.Format(RadiusRCustomerWebSite.Localization.Common.PasswordSMS, validPasswordClient.OnlinePassword, CustomerWebsiteSettings.OnlinePasswordDuration.Hours));
        //                    //SMS.SendPlainText(new[] { validPasswordClient }, string.Format(RadiusRCustomerWebSite.Localization.Common.PasswordSMS, validPasswordClient.OnlinePassword, AppSettings.OnlinePasswordDuration.Hours));
        //                    db.SaveChanges();
        //                }
        //                //ViewBag.SMSWarning = string.Format(RadiusRCustomerWebSite.Localization.Common.SMSWarningMessage, AppSettings.OnlinePasswordDuration.Hours);
        //                return RedirectToAction("Login");
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("CustomerCode", RadiusRCustomerWebSite.Localization.Common.ClientNotFound);
        //                return View(login);
        //            }
        //        }
        //    }
        //    return View(login);
        //}
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
        internal static void SignInUser(string ValidDisplayName, string ID, string SubscriberNo, IEnumerable<string> relatedCustomers, IOwinContext context)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, ValidDisplayName),
                new Claim(ClaimTypes.NameIdentifier, ID),
                new Claim(ClaimTypes.SerialNumber, SubscriberNo),
                new Claim("SubscriptionBag", string.Join(";", relatedCustomers))
            }, "ApplicationCookie");
            context.Authentication.SignIn(identity);
        }

        internal static void SignInCurrentUserAgain(IOwinContext context)
        {
            GenericCustomerServiceClient client = new GenericCustomerServiceClient();
            var baseRequest = new GenericServiceSettings();
            var subId = context.Authentication.User.GiveUserId();
            var result = client.SubscriptionBasicInfo(new CustomerServiceBaseRequest()
            {
                Culture = baseRequest._culture,
                Username = baseRequest._username,
                Rand = baseRequest._rand,
                Hash = baseRequest.hash,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = subId
                }
            });
            SignInUser(result.AuthenticationSMSConfirmResponse.ValidDisplayName, result.AuthenticationSMSConfirmResponse.ID.ToString(), result.AuthenticationSMSConfirmResponse.SubscriberNo, result.AuthenticationSMSConfirmResponse.RelatedCustomers, context);
        }
        internal static void SignoutUser(IOwinContext context)
        {
            context.Authentication.SignOut();
        }
    }
}