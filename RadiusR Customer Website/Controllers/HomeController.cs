using RadiusR.DB;
using RadiusR.DB.Utilities.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using RezaB.Web.Authentication;
using RadiusR_Customer_Website.Models;
//using RadiusR_Manager.Models.ViewModels;
using RadiusR.SMS;
using NLog;
using RadiusR.DB.Enums;
using RadiusR.DB.Enums.RecurringDiscount;
using RadiusR.DB.ModelExtentions;
using RezaB.Web.VPOS;
using System.Collections.Specialized;
using RadiusR.API.MobilExpress.DBAdapter;
using RadiusR.API.MobilExpress.DBAdapter.AdapterClient;
using RadiusR.SystemLogs;
using RadiusR.VPOS;
using RadiusR_Customer_Website.VPOSToken;
using RezaB.Data.Formating;
using RadiusR.DB.Enums.SupportRequests;

namespace RadiusR_Customer_Website.Controllers
{
    public class HomeController : BaseController
    {
        Logger paymentLogger = LogManager.GetLogger("payments");
        Logger unpaidLogger = LogManager.GetLogger("unpaid");
        Logger generalLogger = LogManager.GetLogger("general");
        Logger TTErrorslogger = LogManager.GetLogger("TTErrors");
        public ActionResult Index()
        {
            HomePageViewModel results = null;
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbClient = db.Subscriptions.Find(User.GiveUserId());
                var unpaidBills = dbClient.Bills.Where(bill => bill.BillStatusID == (short)BillState.Unpaid).ToList();
                var clientUsage = dbClient.GetPeriodUsageInfo(dbClient.GetCurrentBillingPeriod(ignoreActivationDate: true), db);
                results = new HomePageViewModel()
                {
                    BillsTotal = unpaidBills.Sum(bill => bill.GetPayableCost()).ToString("###,##0.00"),
                    BillCount = unpaidBills.Count(),
                    Download = clientUsage.Download,
                    Upload = clientUsage.Upload
                };
            }
            return View(results);
        }

        public ActionResult MyDocuments()
        {
            return RedirectToAction("Index");
        }
        public ActionResult BillsAndPayments(int? page)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbClient = db.Subscriptions.Find(User.GiveUserId());
                var firstUnpaidBill = dbClient.Bills.Where(bill => bill.BillStatusID == (short)BillState.Unpaid).OrderBy(bill => bill.IssueDate).FirstOrDefault();
                var results = dbClient.Bills.OrderByDescending(bill => bill.IssueDate).Select(bill => new PaymentsAndBillsViewModel()
                {
                    ID = bill.ID,
                    ServiceName = bill.BillFees.Any(bf => bf.FeeTypeID == (short)FeeType.Tariff) ? bill.BillFees.FirstOrDefault(bf => bf.FeeTypeID == (short)FeeType.Tariff).Description : "-",
                    BillDate = bill.IssueDate,
                    LastPaymentDate = bill.DueDate,
                    Total = bill.GetPayableCost().ToString("###,##0.00"),
                    Status = (BillState)bill.BillStatusID,
                    CanBePaid = firstUnpaidBill != null && bill.ID == firstUnpaidBill.ID,
                    HasEArchiveBill = bill.EBill != null && bill.EBill.EBillType == (short)EBillType.EArchive
                }).AsQueryable();

                SetupPages(page, ref results, 10);
                ViewBag.HasUnpaidBills = firstUnpaidBill != null;
                ViewBag.IsPrePaid = !dbClient.HasBilling;
                // quota
                if (dbClient.Service.CanHaveQuotaSale)
                {
                    ViewBag.CanBuyQuota = true;
                    ViewBag.QuotaPackages = db.QuotaPackages.Select(q => new QuotaPackageViewModel()
                    {
                        ID = q.ID,
                        _amount = q.Amount,
                        _price = q.Price,
                        Name = q.Name
                    }).ToArray();
                }
                // errors
                if (!string.IsNullOrEmpty(Session["POSErrorMessage"] as string))
                {
                    ViewBag.POSErrorMessage = Session["POSErrorMessage"];
                    Session.Remove("POSErrorMessage");
                }
                if (!string.IsNullOrEmpty(Session["POSSuccessMessage"] as string))
                {
                    ViewBag.POSSuccessMessage = Session["POSSuccessMessage"];
                    Session.Remove("POSSuccessMessage");
                }
                if (TempData.ContainsKey("ServiceError"))
                    ViewBag.ServiceError = TempData["ServiceError"];
                // view credits
                var credits = dbClient.SubscriptionCredits.Select(credit => credit.Amount).DefaultIfEmpty(0m).Sum();
                if (credits > 0m)
                    ViewBag.ClientCredits = credits;
                return View(results.ToList());
            }
        }

        public ActionResult ChangeSubClient(long id)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                // current subscription
                var currentClient = db.Subscriptions.Find(User.GiveUserId());
                // target subscription
                var targetClient = db.Subscriptions.Find(id);
                if (currentClient.Customer.CustomerIDCard.TCKNo != targetClient.Customer.CustomerIDCard.TCKNo && currentClient.Customer.ContactPhoneNo != targetClient.Customer.ContactPhoneNo)
                {
                    return RedirectToAction("Index");
                }
                // find customers
                var dbCustomers = db.Customers.Where(c => c.CustomerIDCard.TCKNo == targetClient.Customer.CustomerIDCard.TCKNo || c.ContactPhoneNo == targetClient.Customer.ContactPhoneNo).ToArray();

                AuthController.SignoutUser(Request.GetOwinContext());
                AuthController.SignInUser(targetClient, dbCustomers, Request.GetOwinContext());
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult PaymentSelection(long? id)
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("Payment", new { id = id });

            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                var payableAmount = GetPayableAmount(dbSubscription, id);
                if (payableAmount == 0m)
                {
                    var billIDs = id.HasValue ? new[] { id.Value } : dbSubscription.Bills.Where(b => b.PaymentTypeID == (short)PaymentType.None).Select(b => b.ID).ToArray();
                    PayBills(db, dbSubscription, id, PaymentType.Cash);
                    db.SystemLogs.Add(SystemLogProcessor.BillPayment(billIDs, null, dbSubscription.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), PaymentType.Cash));
                    db.SaveChanges();
                    return RedirectToAction("BillsAndPayments");
                }
                var dbCustomer = dbSubscription.Customer;

                var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                {
                    IP = Request.UserHostAddress,
                    UserAgent = Request.UserAgent
                });

                var response = client.GetCards(dbCustomer);
                if (response.InternalException != null || response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                {
                    if (response.Response.ResponseCode == RezaB.API.MobilExpress.Response.ResponseCodes.CardNotFound)
                        return RedirectToAction("Payment", new { id = id });
                    ViewBag.ServiceError = RadiusRCustomerWebSite.Localization.Common.PaymentWithCardNotAvailable;
                    return View();
                }
                ViewBag.CardsList = response.Response.CardList;

                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PaymentSelection(long? id, string token)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                var dbCustomer = dbSubscription.Customer;
                var payableAmount = GetPayableAmount(dbSubscription, id);
                if (payableAmount == 0m)
                    return RedirectToAction("PaymentSelection");
                var dbBills = dbSubscription.Bills.Where(b => b.BillStatusID == (short)BillState.Unpaid).ToList();
                if (id.HasValue)
                    dbBills = dbBills.Where(b => b.ID == id).ToList();

                var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                {
                    IP = Request.UserHostAddress,
                    UserAgent = Request.UserAgent
                });

                var response = client.PayBill(dbCustomer, payableAmount, token);
                if (response.InternalException != null)
                {
                    generalLogger.Warn(response.InternalException, "Error calling 'DeleteCard' from MobilExpress client");
                    TempData["ServiceError"] = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                    return RedirectToAction("BillsAndPayments");
                }
                if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                {
                    TempData["ServiceError"] = response.Response.ErrorMessage;
                    return RedirectToAction("BillsAndPayments");
                }
                db.PayBills(dbBills, PaymentType.MobilExpress, BillPayment.AccountantType.Admin);
                var smsService = new SMSService();
                db.SMSArchives.AddSafely(smsService.SendSubscriberSMS(dbSubscription, SMSType.PaymentDone, new Dictionary<string, object>()
                {
                    { SMSParamaterRepository.SMSParameterNameCollection.BillTotal, payableAmount }
                }));
                db.SystemLogs.Add(SystemLogProcessor.BillPayment(dbBills.Select(b => b.ID), null, dbSubscription.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), PaymentType.MobilExpress));

                db.SaveChanges();

                return RedirectToAction("BillsAndPayments");
            }
        }

        [HttpGet]
        public ActionResult Payment(long? id)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                // to prevent logouts before payment
                {
                    AuthController.SignoutUser(Request.GetOwinContext());
                    AuthController.SignInCurrentUserAgain(Request.GetOwinContext());
                }
                var payableAmount = GetPayableAmount(dbSubscription, id);
                if (payableAmount == 0)
                    return RedirectToAction("BillsAndPayments");

                var tokenKey = VPOSTokenManager.RegisterPaymentToken(new BillPaymentToken()
                {
                    SubscriberId = User.GiveUserId().Value,
                    BillID = id
                });

                var VPOSModel = VPOSManager.GetVPOSModel(
                    //Url.Action("SuccessfulPayRepost", null, new { id = id }, Request.Url.Scheme),
                    //Url.Action("FailedPayRepost", null, new { id = id }, Request.Url.Scheme),
                    Url.Action("VPOSSuccess", null, new { id = tokenKey }, Request.Url.Scheme),
                    Url.Action("VPOSFail", null, new { id = tokenKey }, Request.Url.Scheme),
                    payableAmount,
                    dbSubscription.Customer.Culture.Split('-').FirstOrDefault(),
                    dbSubscription.SubscriberNo + "-" + dbSubscription.ValidDisplayName);
                return View(viewName: "3DHostPayment", model: VPOSModel);
            }
        }

        public ActionResult AutomaticPayment()
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            using (RadiusREntities db = new RadiusREntities())
            {
                var dbCustomer = db.Subscriptions.Find(User.GiveUserId()).Customer;
                var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                {
                    IP = Request.UserHostAddress.ToString(),
                    UserAgent = Request.UserAgent
                });
                var response = client.GetCards(dbCustomer);
                if (response.InternalException != null)
                {
                    generalLogger.Warn(response.InternalException, "Error calling 'GetCards' from MobilExpress client");
                    ViewBag.ServiceError = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                    return View();
                }
                IEnumerable<CustomerAutomaticPaymentViewModel.CardViewModel> cards = Enumerable.Empty<CustomerAutomaticPaymentViewModel.CardViewModel>();
                if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                {
                    if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.CardNotFound && response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.CustomerNotFound)
                    {
                        ViewBag.ServiceError = response.Response.ErrorMessage;
                        return View();
                    }
                }
                else
                {
                    cards = response.Response.CardList.Select(cl => new CustomerAutomaticPaymentViewModel.CardViewModel()
                    {
                        MaskedCardNo = cl.MaskedCardNumber,
                        Token = cl.CardToken,
                        HasAutoPayments = false
                    }).ToArray();
                }

                var subscriptions = dbCustomer.Subscriptions.Where(s => !s.IsCancelled).ToArray();

                var expiredOrInvalidCards = subscriptions.Where(m => m.MobilExpressAutoPayment != null).Select(s => s.MobilExpressAutoPayment).Where(meap => !cards.Select(c => c.Token).Contains(meap.CardToken));
                db.MobilExpressAutoPayments.RemoveRange(expiredOrInvalidCards);
                db.SaveChanges();
                var autoPayments = subscriptions.Select(s => new CustomerAutomaticPaymentViewModel.AutomaticPaymentViewModel()
                {
                    SubscriberID = s.ID,
                    SubscriberNo = s.SubscriberNo,
                    Card = s.MobilExpressAutoPayment != null ? new CustomerAutomaticPaymentViewModel.CardViewModel()
                    {
                        MaskedCardNo = response.Response.CardList.FirstOrDefault(cl => cl.CardToken == s.MobilExpressAutoPayment.CardToken).MaskedCardNumber,
                        Token = s.MobilExpressAutoPayment.CardToken
                    } : null
                }).ToArray();
                foreach (var card in cards)
                {
                    card.HasAutoPayments = autoPayments.Where(ap => ap.Card != null).Any(ap => ap.Card.Token == card.Token);
                }

                return View(new CustomerAutomaticPaymentViewModel()
                {
                    Cards = cards,
                    AutomaticPayments = autoPayments
                });
            }
        }

        [HttpGet]
        public ActionResult AddCard()
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            return View();
        }

        [HttpPost]
        public ActionResult AddCard(AutoPaymentCardViewModel card)
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            if (ModelState.IsValid)
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                    var rand = new Random();
                    var smsCode = rand.Next(100000, 1000000).ToString();
                    var smsClient = new SMSService();
                    smsClient.SendSubscriberSMS(dbSubscription, SMSType.MobilExpressAddRemoveCard, new Dictionary<string, object>() {
                        { SMSParamaterRepository.SMSParameterNameCollection.SMSCode, smsCode }
                    });
                    TempData["smsCode"] = smsCode;
                    return View(viewName: "AddCardSMSCheck", model: card);
                }
            }
            return View(card);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult AddCardSMSCheck(AutoPaymentCardViewModel card, string smsCode)
        {
            if (!TempData.ContainsKey("smsCode"))
            {
                return View(viewName: "AddCard", model: card);
            }
            if (smsCode != TempData["smsCode"].ToString())
            {
                var retries = TempData.ContainsKey("smsRetries") ? TempData["smsRetries"] as int? ?? 0 : 0;
                retries++;
                if (retries > 3)
                    return View(viewName: "AddCard", model: card);
                TempData.Keep("smsCode");
                TempData["smsRetries"] = retries;
                ViewBag.WrongPassword = RadiusRCustomerWebSite.Localization.Common.SMSPasswordWrong;
                return View(card);
            }
            TempData.Remove("smsRetries");

            if (ModelState.IsValid)
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    var dbCustomer = db.Subscriptions.Find(User.GiveUserId()).Customer;
                    var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                    {
                        IP = Request.UserHostAddress.ToString(),
                        UserAgent = Request.UserAgent
                    });

                    var response = client.SaveCard(dbCustomer, new RadiusR.API.MobilExpress.DBAdapter.AdapterParameters.AdapterCard()
                    {
                        CardHolderName = card.CardholderName,
                        CardNumber = card.CardNo.Replace("-", ""),
                        CardMonth = Convert.ToInt32(card.ExpirationMonth),
                        CardYear = Convert.ToInt32("20" + card.ExpirationYear)
                    });

                    if (response.InternalException != null)
                    {
                        generalLogger.Warn(response.InternalException, "Error calling 'SaveCard' from MobilExpress client");
                        ViewBag.ServiceError = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                        return View(viewName: "AddCard", model: card);
                    }
                    if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                    {
                        ViewBag.ServiceError = response.Response.ErrorMessage;
                        return View(viewName: "AddCard", model: card);
                    }

                    var cardNo = card.CardNo.Replace("-", "");
                    db.SystemLogs.Add(SystemLogProcessor.AddCreditCard(dbCustomer.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), cardNo.Substring(0, 6) + "******" + cardNo.Substring(12)));
                    db.SaveChanges();

                    return RedirectToAction("AutomaticPayment");
                }
            }

            return View(viewName: "AddCard", model: card);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult RemoveCard(string id)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                if (db.MobilExpressAutoPayments.Any(ap => ap.CardToken == id))
                {
                    return RedirectToAction("AutomaticPayment");
                }
                var rand = new Random();
                var smsCode = rand.Next(100000, 1000000).ToString();
                var smsClient = new SMSService();
                smsClient.SendSubscriberSMS(dbSubscription, SMSType.MobilExpressAddRemoveCard, new Dictionary<string, object>() {
                        { SMSParamaterRepository.SMSParameterNameCollection.SMSCode, smsCode }
                    });
                TempData["smsCode"] = smsCode;
                return View(viewName: "RemoveCardSMSCheck", model: id);
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult RemoveCardSMSCheck(string id, string smsCode)
        {
            if (!TempData.ContainsKey("smsCode"))
            {
                return View("AutomaticPayment");
            }
            if (smsCode != TempData["smsCode"].ToString())
            {
                var retries = TempData.ContainsKey("smsRetries") ? TempData["smsRetries"] as int? ?? 0 : 0;
                retries++;
                if (retries > 3)
                    return View("AutomaticPayment");
                TempData.Keep("smsCode");
                TempData["smsRetries"] = retries;
                ViewBag.WrongPassword = RadiusRCustomerWebSite.Localization.Common.SMSPasswordWrong;
                return View(model: id);
            }
            TempData.Remove("smsRetries");

            using (RadiusREntities db = new RadiusREntities())
            {
                if (db.MobilExpressAutoPayments.Any(ap => ap.CardToken == id))
                {
                    return RedirectToAction("AutomaticPayment");
                }
                var dbCustomer = db.Subscriptions.Find(User.GiveUserId()).Customer;
                var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                {
                    IP = Request.UserHostAddress,
                    UserAgent = Request.UserAgent
                });

                // get card info
                var cards = client.GetCards(dbCustomer);
                if (cards.InternalException != null)
                {
                    generalLogger.Warn(cards.InternalException, "Error calling 'GetCards' from MobilExpress client");
                    TempData["ServiceError"] = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                    return RedirectToAction("AutomaticPayment");
                }
                if (cards.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                {
                    TempData["ServiceError"] = cards.Response.ErrorMessage;
                    return RedirectToAction("AutomaticPayment");
                }
                var targetCard = cards.Response.CardList.FirstOrDefault(c => c.CardToken == id);

                // delete card
                var response = client.DeleteCard(dbCustomer, id);
                if (response.InternalException != null)
                {
                    generalLogger.Warn(response.InternalException, "Error calling 'DeleteCard' from MobilExpress client");
                    TempData["ServiceError"] = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                    return RedirectToAction("AutomaticPayment");
                }
                if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                {
                    TempData["ServiceError"] = response.Response.ErrorMessage;
                    return RedirectToAction("AutomaticPayment");
                }

                db.SystemLogs.Add(SystemLogProcessor.RemoveCreditCard(dbCustomer.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), targetCard.MaskedCardNumber));
                db.SaveChanges();

                return RedirectToAction("AutomaticPayment");
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult ActivateAutomaticPayment(long id, string token)
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            ViewBag.PaymentTypes = new SelectList(new RezaB.Data.Localization.LocalizedList<AutoPaymentType, RadiusR.Localization.Lists.AutoPaymentType>().GetList(), "Key", "Value");
            return View(new ActivateAutomaticPaymentViewModel()
            {
                CardToken = token,
                SubscriptionID = id
            });
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult ActivateAutomaticPaymentConfirm(ActivateAutomaticPaymentViewModel automaticPayment)
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            if (ModelState.IsValid)
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    var dbSubscription = db.Subscriptions.Find(automaticPayment.SubscriptionID);
                    var currentCustomer = db.Subscriptions.Find(User.GiveUserId()).Customer;
                    if (dbSubscription.CustomerID != currentCustomer.ID || dbSubscription.IsCancelled)
                    {
                        return RedirectToAction("AutomaticPayment");
                    }

                    var client = new MobilExpressAdapterClient(MobilExpressSettings.MobilExpressMerchantKey, MobilExpressSettings.MobilExpressAPIPassword, new ClientConnectionDetails()
                    {
                        IP = Request.UserHostAddress,
                        UserAgent = Request.UserAgent
                    });

                    var response = client.GetCards(currentCustomer);
                    if (response.InternalException != null)
                    {
                        generalLogger.Warn(response.InternalException, "Error calling 'GetCards' from MobilExpress client");
                        TempData["ServiceError"] = RadiusRCustomerWebSite.Localization.Common.GeneralError;
                        return RedirectToAction("AutomaticPayment");
                    }
                    if (response.Response.ResponseCode != RezaB.API.MobilExpress.Response.ResponseCodes.Success)
                    {
                        TempData["ServiceError"] = response.Response.ErrorMessage;
                        return RedirectToAction("AutomaticPayment");
                    }
                    var currentCard = response.Response.CardList.FirstOrDefault(c => c.CardToken == automaticPayment.CardToken);
                    if (currentCard == null)
                        return RedirectToAction("AutomaticPayment");

                    dbSubscription.MobilExpressAutoPayment = new MobilExpressAutoPayment()
                    {
                        CardToken = automaticPayment.CardToken,
                        PaymentType = automaticPayment.PaymentType
                    };

                    db.SaveChanges();

                    var smsClient = new SMSService();
                    db.SMSArchives.AddSafely(smsClient.SendSubscriberSMS(dbSubscription, SMSType.MobilExpressActivation, new Dictionary<string, object> {
                        { SMSParamaterRepository.SMSParameterNameCollection.CardNo, currentCard.MaskedCardNumber }
                    }));
                    db.SystemLogs.Add(SystemLogProcessor.ActivateAutomaticPayment(dbSubscription.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), "MobilExpress"));
                    db.SaveChanges();
                }
            }

            return RedirectToAction("AutomaticPayment");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeactivateAutomaticPayment(long id)
        {
            if (!MobilExpressSettings.MobilExpressIsActive)
                return RedirectToAction("BillsAndPayments");

            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(id);
                var currentCustomer = db.Subscriptions.Find(User.GiveUserId()).Customer;
                if (dbSubscription.CustomerID != currentCustomer.ID)
                {
                    return RedirectToAction("AutomaticPayment");
                }

                db.MobilExpressAutoPayments.Remove(dbSubscription.MobilExpressAutoPayment);
                db.SaveChanges();

                var client = new SMSService();
                db.SMSArchives.AddSafely(client.SendSubscriberSMS(dbSubscription, SMSType.MobilExpressDeactivation));
                db.SystemLogs.Add(SystemLogProcessor.DeactivateAutomaticPayment(dbSubscription.ID, SystemLogInterface.CustomerWebsite, User.GiveClientSubscriberNo(), "MobilExpress"));
                db.SaveChanges();

                return RedirectToAction("AutomaticPayment");
            }
        }

        public ActionResult TariffAndTraffic()
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                var monthlyUsage = dbSubscription.RadiusDailyAccountings.GroupBy(daily => daily.Date).OrderByDescending(dailyGroup => dailyGroup.Key).Select(dailyGroup => new
                {
                    groupingKey = new
                    {
                        year = dailyGroup.Key.Year,
                        month = dailyGroup.Key.Month
                    },
                    download = dailyGroup.Sum(daily => daily.DownloadBytes),
                    upload = dailyGroup.Sum(daily => daily.UploadBytes)
                }).GroupBy(monthlyGroup => monthlyGroup.groupingKey).Select(monthlyGroup => new UsageInfoViewModel()
                {
                    _year = monthlyGroup.Key.year,
                    _month = monthlyGroup.Key.month,
                    Download = monthlyGroup.Sum(dailyGroup => dailyGroup.download),
                    Upload = monthlyGroup.Sum(dailyGroup => dailyGroup.upload)
                }).OrderByDescending(usage => usage._year).ThenByDescending(usage => usage._month).Take(3).AsQueryable();
                var monthlyUsageResults = new List<ClientUsageInfoViewModel>();
                foreach (var usageInfo in monthlyUsage)
                {
                    monthlyUsageResults.Add(new ClientUsageInfoViewModel()
                    {
                        _month = usageInfo._month,
                        _year = usageInfo._year,
                        Date = usageInfo.Date,
                        TotalDownload = usageInfo.Download,
                        TotalUpload = usageInfo.Upload
                    });
                }
                var clientUsage = dbSubscription.GetPeriodUsageInfo(dbSubscription.GetCurrentBillingPeriod(ignoreActivationDate: true), db);
                var results = new HomePageViewModel()
                {
                    ServiceName = dbSubscription.Service.Name,
                    MonthlyUsage = monthlyUsageResults,
                    Download = clientUsage.Download,
                    Upload = clientUsage.Upload
                };
                return View(results);
            }
        }
        public ActionResult ConnectionStatus()
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var Subscription = db.Subscriptions.Find(User.GiveUserId());
                if (Subscription == null)
                    return PartialView("Error");

                var domainCache = RadiusR.DB.DomainsCache.DomainsCache.GetDomainByID(Subscription.DomainID);
                if (domainCache.TelekomCredential == null)
                {
                    return RedirectToAction("Index");
                }
                if (Request.IsAjaxRequest())
                {
                    RezaB.TurkTelekom.WebServices.TTOYS.TTOYSServiceClient client = new RezaB.TurkTelekom.WebServices.TTOYS.TTOYSServiceClient(domainCache.TelekomCredential.XDSLWebServiceUsernameInt, domainCache.TelekomCredential.XDSLWebServicePassword);
                    var Result = client.Check(Subscription.SubscriptionTelekomInfo.SubscriptionNo);
                    if (Result.InternalException != null)
                    {
                        TTErrorslogger.Error(Result.InternalException, "Error telekom line state");
                        return PartialView("Error");
                    }
                    var model = new Models.ViewModels.Home.ConnectionStatusViewModel()
                    {
                        ConnectionStatus = (short)Result.Data.OperationStatus,
                        CurrentDownload = Result.Data.CurrentDown,
                        CurrentUpload = Result.Data.CurrentUp,
                        XDSLNo = Subscription.SubscriptionTelekomInfo.SubscriptionNo,
                        XDSLType = Subscription.SubscriptionTelekomInfo.XDSLType,
                        DownloadMargin = Result.Data.NoiseRateDown,
                        UploadMargin = Result.Data.NoiseRateUp
                    };
                    return PartialView("_ConnectionStatusPartial", model);
                }
                else
                {
                    return View(new Models.ViewModels.Home.ConnectionStatusViewModel());
                }
            }
        }

        public ActionResult Services()
        {
            return RedirectToAction("Index");
        }

        public ActionResult PersonalInfo()
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var subscription = db.Subscriptions.Find(User.GiveUserId());
                var viewResults = new PersonalInfoViewModel()
                {
                    EMail = subscription.Customer.Email,
                    PhoneNo = subscription.Customer.ContactPhoneNo,
                    ValidDisplayName = subscription.ValidDisplayName,
                    InstallationAddress = subscription.Address.AddressText,
                    Username = subscription.Username,
                    Password = subscription.RadiusPassword,
                    ReferenceNo = subscription.ReferenceNo,
                    TTSubscriberNo = subscription.SubscriptionTelekomInfo != null ? subscription.SubscriptionTelekomInfo.SubscriptionNo : null,
                    PSTN = subscription.SubscriptionTelekomInfo != null && !string.IsNullOrWhiteSpace(subscription.SubscriptionTelekomInfo.PSTN) ? subscription.SubscriptionTelekomInfo.PSTN : null
                };
                return View(viewResults);
            }
        }

        [ValidateAntiForgeryToken]
        public ActionResult EArchivePDF(long id)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbBill = db.Bills.Find(id);
                if (dbBill == null || dbBill.EBill == null || dbBill.EBill.EBillType != (short)EBillType.EArchive)
                    return RedirectToAction("BillsAndPayments");
                if (dbBill.Subscription.ID != User.GiveUserId())
                    return RedirectToAction("BillsAndPayments");
                var serviceClient = new RezaB.NetInvoice.Wrapper.NetInvoiceClient(AppSettings.EBillCompanyCode, AppSettings.EBillApiUsername, AppSettings.EBillApiPassword);
                var response = serviceClient.GetEArchivePDF(dbBill.EBill.ReferenceNo);
                if (response.PDFData == null)
                    return RedirectToAction("BillsAndPayments");

                return File(response.PDFData, "application/pdf", RadiusRCustomerWebSite.Localization.Common.EArchivePDFFileName + "_" + dbBill.IssueDate.ToString("yyyy-MM-dd") + ".pdf");
            }
        }

        public ActionResult BuyQuota(int id)
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(User.GiveUserId());
                var dbQuota = db.QuotaPackages.Find(id);
                if (dbSubscription == null || dbQuota == null || !dbSubscription.Service.CanHaveQuotaSale)
                    return RedirectToAction("BillsAndPayments");

                var tokenKey = VPOSTokenManager.RegisterPaymentToken(new QuotaSaleToken()
                {
                    SubscriberId = User.GiveUserId().Value,
                    PackageID = id
                });

                var VPOSModel = VPOSManager.GetVPOSModel(
                    //Url.Action("QuotaBuySuccess", null, new { id = dbQuota.ID }, Request.Url.Scheme),
                    //Url.Action("QuotaBuyFail", null, new { id = dbQuota.ID }, Request.Url.Scheme),
                    Url.Action("VPOSSuccess", null, new { id = tokenKey }, Request.Url.Scheme),
                    Url.Action("VPOSFail", null, new { id = tokenKey }, Request.Url.Scheme),
                    dbQuota.Price,
                    dbSubscription.Customer.Culture.Split('-').FirstOrDefault(),
                    dbSubscription.ValidDisplayName);
                return View(viewName: "3DHostPayment", model: VPOSModel);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult VPOSSuccess(string id)
        {
            var paymentToken = VPOSTokenManager.RetrievePaymentToken(id);
            if (paymentToken == null)
            {
                Session["POSErrorMessage"] = RadiusRCustomerWebSite.Localization.Common.InvalidTokenKey;
                return RedirectToAction("BillsAndPayments");
            }
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(paymentToken.SubscriberId);
                if (dbSubscription == null)
                {
                    Session["POSErrorMessage"] = RadiusRCustomerWebSite.Localization.Common.SubscriberNotFound;
                    return RedirectToAction("BillsAndPayments");
                }

                //------------ bill payment ---------------
                if (paymentToken is BillPaymentToken)
                {
                    var billPaymentToken = (BillPaymentToken)paymentToken;

                    var payableAmount = GetPayableAmount(dbSubscription, billPaymentToken.BillID);
                    // billed sub
                    if (dbSubscription.HasBilling)
                    {
                        var billIds = billPaymentToken.BillID.HasValue ? new[] { billPaymentToken.BillID.Value } : dbSubscription.Bills.Where(b => b.BillStatusID == (short)BillState.Unpaid).Select(b => b.ID).ToArray();

                        paymentLogger.Debug("Received successfull payment for clientId= {1}, billId= {2} with total of {3}:" + Environment.NewLine + "{0}",
                                            string.Join(Environment.NewLine, Request.Form.AllKeys.Select(key => key + ": " + Request.Form[key])),
                                            dbSubscription.SubscriberNo,
                                            billPaymentToken.BillID.HasValue ? billPaymentToken.BillID.Value.ToString() : string.Join(",", billIds),
                                            payableAmount.ToString());

                        var unpaidBills = dbSubscription.Bills.Where(bill => bill.BillStatusID == (short)BillState.Unpaid).ToList();
                        if (billPaymentToken.BillID.HasValue)
                            unpaidBills = unpaidBills.Where(b => b.ID == billPaymentToken.BillID).ToList();

                        db.PayBills(unpaidBills, PaymentType.VirtualPos, BillPayment.AccountantType.Admin);
                        SMSService SMSSerivce = new SMSService();
                        db.SMSArchives.AddSafely(SMSSerivce.SendSubscriberSMS(dbSubscription, SMSType.PaymentDone, new Dictionary<string, object>()
                        {
                            { SMSParamaterRepository.SMSParameterNameCollection.BillTotal, payableAmount }
                        }));
                        db.SystemLogs.Add(SystemLogProcessor.BillPayment(billIds, null, dbSubscription.ID, SystemLogInterface.CustomerWebsite, dbSubscription.SubscriberNo, PaymentType.VirtualPos));
                        db.SaveChanges();
                    }
                    // pre-paid sub
                    else
                    {
                        paymentLogger.Debug("Received successfull payment for clientId= {1} with total of {2}:" + Environment.NewLine + "{0}",
                                            string.Join(Environment.NewLine, Request.Form.AllKeys.Select(key => key + ": " + Request.Form[key])),
                                            dbSubscription.SubscriberNo,
                                            payableAmount.ToString());

                        db.ExtendClientPackage(dbSubscription, 1, PaymentType.VirtualPos, BillPayment.AccountantType.Admin);
                        SMSService SMSAsync = new SMSService();
                        db.SMSArchives.AddSafely(SMSAsync.SendSubscriberSMS(dbSubscription, SMSType.ExtendPackage, new Dictionary<string, object>()
                        {
                            { SMSParamaterRepository.SMSParameterNameCollection.ExtendedMonths, "1" }
                        }));
                        db.SystemLogs.Add(SystemLogProcessor.ExtendPackage(null, dbSubscription.ID, SystemLogInterface.CustomerWebsite, billPaymentToken.SubscriberId.ToString(), 1));
                        db.SaveChanges();
                    }
                }
                //------------ quota sale ---------------
                else if (paymentToken is QuotaSaleToken)
                {
                    var quotaSaleToken = (QuotaSaleToken)paymentToken;

                    var dbQuota = db.QuotaPackages.Find(quotaSaleToken.PackageID);
                    if (dbSubscription != null || dbQuota != null || dbSubscription.Service.CanHaveQuotaSale)
                    {
                        paymentLogger.Debug("Received successfull payment for clientId= {1} with total of {2}:" + Environment.NewLine + "{0}",
                                            string.Join(Environment.NewLine, Request.Form.AllKeys.Select(key => key + ": " + Request.Form[key])),
                                            dbSubscription.SubscriberNo,
                                            dbQuota.Price.ToString());
                        var quotaDescription = RateLimitFormatter.ToQuotaDescription(dbQuota.Amount, dbQuota.Name);
                        dbSubscription.SubscriptionQuotas.Add(new SubscriptionQuota()
                        {
                            AddDate = DateTime.Now,
                            Amount = dbQuota.Amount
                        });

                        dbSubscription.Bills.Add(new Bill()
                        {
                            BillFees = new[]
                            {
                                new BillFee()
                                {
                                    InstallmentCount = 1,
                                    CurrentCost = dbQuota.Price,
                                    Fee = new Fee()
                                    {
                                        Date = DateTime.Now.Date,
                                        FeeTypeID = (short)FeeType.Quota,
                                        Description = quotaDescription,
                                        InstallmentBillCount = 1,
                                        Cost = dbQuota.Price,
                                        SubscriptionID = dbSubscription.ID
                                    }
                                }
                            }.ToList(),
                            DueDate = DateTime.Now.Date,
                            IssueDate = DateTime.Now.Date,
                            BillStatusID = (short)BillState.Paid,
                            PaymentTypeID = (short)PaymentType.VirtualPos,
                            Source = (short)BillSources.Manual,
                            PayDate = DateTime.Now
                        });

                        db.SystemLogs.Add(SystemLogProcessor.AddSubscriptionQuota(null, dbSubscription.ID, SystemLogInterface.CustomerWebsite, quotaSaleToken.SubscriberId.ToString(), quotaDescription));

                        SMSService SMSAsync = new SMSService();
                        db.SMSArchives.AddSafely(SMSAsync.SendSubscriberSMS(dbSubscription, SMSType.PaymentDone, new Dictionary<string, object>()
                        {
                            { SMSParamaterRepository.SMSParameterNameCollection.BillTotal, dbQuota.Price }
                        }));

                        db.SaveChanges();
                    }
                }
            }

            Session["POSSuccessMessage"] = RadiusRCustomerWebSite.Localization.Common.POSSuccessMessage;
            return RedirectToAction("BillsAndPayments");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult VPOSFail(string id)
        {
            var paymentToken = VPOSTokenManager.RetrievePaymentToken(id);
            if (paymentToken == null)
            {
                Session["POSErrorMessage"] = RadiusRCustomerWebSite.Localization.Common.InvalidTokenKey;
                return RedirectToAction("BillsAndPayments");
            }
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(paymentToken.SubscriberId);
                if (dbSubscription == null)
                {
                    Session["POSErrorMessage"] = RadiusRCustomerWebSite.Localization.Common.SubscriberNotFound;
                    return RedirectToAction("BillsAndPayments");
                }

                //------------ bill payment ---------------
                if (paymentToken is BillPaymentToken)
                {
                    var billPaymentToken = (BillPaymentToken)paymentToken;

                    var unpaidBills = dbSubscription.Bills.Where(bill => bill.BillStatusID == (short)BillState.Unpaid).ToList();

                    if (billPaymentToken.BillID.HasValue)
                    {
                        unpaidBills = unpaidBills.Where(bill => bill.ID == billPaymentToken.BillID).ToList();
                    }
                    var clientCredits = dbSubscription.SubscriptionCredits.Sum(credit => credit.Amount);
                    var amount = unpaidBills.Sum(bill => bill.GetPayableCost()) - clientCredits;
                    if (!dbSubscription.HasBilling)
                    {
                        amount = dbSubscription.Service.Price;
                    }
                    // make changes to database
                    unpaidLogger.Debug("Unsuccessfull payment for clientId= {1}, billId= {2} with total of {3}:" + Environment.NewLine + "{0}",
                        string.Join(Environment.NewLine, Request.Form.AllKeys.Select(key => key + ": " + Request.Form[key])),
                        dbSubscription.SubscriberNo,
                        billPaymentToken.BillID.HasValue ? billPaymentToken.BillID.Value.ToString() : string.Join(",", unpaidBills.Select(bill => bill.ID.ToString())),
                        amount.ToString());
                }
                //------------ quota sale ---------------
                else if (paymentToken is QuotaSaleToken)
                {
                    var quotaSaleToken = (QuotaSaleToken)paymentToken;
                    var dbQuota = db.QuotaPackages.Find(id);

                    unpaidLogger.Debug("Unsuccessfull payment for clientId= {1} with total of {2}:" + Environment.NewLine + "{0}",
                        string.Join(Environment.NewLine, Request.Form.AllKeys.Select(key => key + ": " + Request.Form[key])),
                        dbSubscription.SubscriberNo,
                        dbQuota.Price.ToString());
                }
            }

            Session["POSErrorMessage"] = GetErrorMessage();
            return RedirectToAction("BillsAndPayments");
        }

        public ActionResult SpecialOffers(int? page)
        {
            var currentSubId = User.GiveUserId();
            var minDate = DateTime.Now.Date.AddYears(-1);
            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = db.Subscriptions.Find(currentSubId);
                var viewResults = db.RecurringDiscounts.Where(rd => rd.SubscriptionID == dbSubscription.ID).Where(rd => rd.ReferrerRecurringDiscount != null || rd.ReferringRecurringDiscounts.Any())
                    .OrderByDescending(rd => rd.CreationTime)
                    .Select(rd => new SpecialOffersReportViewModel()
                    {
                        IsCancelled = rd.IsDisabled,
                        StartDate = rd.CreationTime,
                        TotalCount = rd.ApplicationTimes,
                        UsedCount = rd.AppliedRecurringDiscounts.Where(ard => ard.ApplicationState == (short)RecurringDiscountApplicationState.Applied).Count(),
                        MissedCount = rd.AppliedRecurringDiscounts.Where(ard => ard.ApplicationState == (short)RecurringDiscountApplicationState.Passed).Count(),
                        ReferenceNo = rd.ReferrerRecurringDiscount != null ? rd.ReferrerRecurringDiscount.Subscription.ReferenceNo : rd.ReferringRecurringDiscounts.Any() ? rd.ReferringRecurringDiscounts.FirstOrDefault().Subscription.ReferenceNo : null,
                        ReferralSubscriberState = rd.ReferrerRecurringDiscount != null ? rd.ReferrerRecurringDiscount.Subscription.State : rd.ReferringRecurringDiscounts.Any() ? rd.ReferringRecurringDiscounts.FirstOrDefault().Subscription.State : (short?)null,
                    });

                ViewBag.TotalCount = viewResults.Select(r => r.TotalCount).DefaultIfEmpty(0).Sum();
                ViewBag.TotalUsed = viewResults.Select(r => r.UsedCount).DefaultIfEmpty(0).Sum();
                ViewBag.TotalMissed = viewResults.Select(r => r.MissedCount).DefaultIfEmpty(0).Sum();
                ViewBag.TotalRemaining = viewResults.Where(r => !r.IsCancelled).Select(r => r.TotalCount - (r.UsedCount + r.MissedCount)).DefaultIfEmpty(0).Sum();

                ViewBag.TotalRow = new SpecialOffersReportViewModel()
                {
                    TotalCount = viewResults.Select(r => r.TotalCount).DefaultIfEmpty(0).Sum(),
                    UsedCount = viewResults.Select(r => r.UsedCount).DefaultIfEmpty(0).Sum(),
                    MissedCount = viewResults.Select(r => r.MissedCount).DefaultIfEmpty(0).Sum(),
                };

                SetupPages(page, ref viewResults);
                ////var rawData = db.RecurringDiscounts.Include(rd => rd.Bills.Select(b => b.BillFees)).Where(rd => rd.SubscriptionID == currentSubId && (rd.ReferrerRecurringDiscount != null || rd.ReferringRecurringDiscounts.Any()) && rd.Bills.Any(b => b.IssueDate >= minDate)).ToArray();
                //var rawData = db.Bills
                //    .OrderBy(b => b.IssueDate)
                //    .Include(b => b.BillFees)
                //    .Include(b => b.AppliedRecurringDiscounts.Select(rd=>rd.RecurringDiscount.ReferrerRecurringDiscount.Subscription))
                //    .Include(b => b.AppliedRecurringDiscounts.Select(rd => rd.RecurringDiscount.ReferringRecurringDiscounts.Select(rd2 => rd2.Subscription)))
                //    .Where(b => b.SubscriptionID == currentSubId && b.IssueDate >= minDate && b.AppliedRecurringDiscounts.Any(rd => rd.RecurringDiscount.ReferrerRecurringDiscount != null || rd.RecurringDiscount.ReferringRecurringDiscounts.Any()))
                //    .ToArray();
                //var discounts = rawData.SelectMany(b => b.AppliedRecurringDiscounts.Select(ard => ard.RecurringDiscount).Select(rd => rd)).OrderBy(rd => rd.CreationTime).ToArray();
                //var viewResults = new List<SpecialOffersReportViewModel>();
                //foreach (var discount in discounts)
                //{
                //    var currentViewModel = new SpecialOffersReportViewModel()
                //    {
                //        CreationDate = discount.CreationTime,
                //        ReferenceNo = discount.ReferrerRecurringDiscount != null ? discount.ReferrerRecurringDiscount.Subscription.ReferenceNo : discount.ReferringRecurringDiscounts.FirstOrDefault().Subscription.ReferenceNo,
                //        TotalCount = discount.IsDisabled ? discount.AppliedRecurringDiscounts.Count() : discount.ApplicationTimes,
                //        UsedCount = discount.AppliedRecurringDiscounts.Count(),
                //        Bills = new List<DiscountRelatedBillViewModel>()
                //    };
                //    foreach (var bill in rawData)
                //    {
                //        var currentRelatedBill = new DiscountRelatedBillViewModel()
                //        {
                //            ID = bill.ID,
                //            IssueDate = bill.IssueDate,
                //            Amount = 0m
                //        };
                //        if(discount.AppliedRecurringDiscounts.Select(ard=> ard.Bill).Any(b=>b.ID == bill.ID))
                //        {
                //            currentRelatedBill.Amount = discount.DiscountType == (short)RecurringDiscountType.Static
                //                ? discount.Amount
                //                : discount.DiscountType == (short)RecurringDiscountType.Percentage
                //                ? discount.ApplicationType == (short)RecurringDiscountApplicationType.FeeBased ? bill.BillFees.FirstOrDefault(bf=>bf.FeeTypeID == discount.FeeTypeID).CurrentCost * discount.Amount : discount.ApplicationType == (short)RecurringDiscountApplicationType.BillBased ? bill.GetTotalCost() * discount.Amount : 0m
                //                : 0m;

                //            if (discount.DiscountType == (short)RecurringDiscountType.Percentage)
                //                currentRelatedBill.Percentage = discount.Amount;
                //        }
                //        currentViewModel.Bills.Add(currentRelatedBill);
                //    }
                //    viewResults.Add(currentViewModel);
                //}

                return View(viewResults.ToArray());
            }
        }

        #region Private Methods

        private string GetErrorMessage()
        {
            return Request.Form[VPOSManager.GetErrorMessageParameterName()];
        }

        private decimal GetPayableAmount(Subscription dbSubscription, long? billId)
        {
            // pre-paid sub
            if (!dbSubscription.HasBilling)
            {
                return dbSubscription.GetSubscriberPackageExtentionUnitPrice();
            }
            // billed sub
            var creditsAmount = dbSubscription.SubscriptionCredits.Sum(credit => credit.Amount);
            var bills = dbSubscription.Bills.Where(bill => bill.BillStatusID == (short)BillState.Unpaid).OrderBy(bill => bill.IssueDate).AsEnumerable();
            if (billId.HasValue)
                bills = bills.Where(bill => bill.ID == billId.Value);
            if (!bills.Any())
                return 0m;

            var billsAmount = bills.Sum(bill => bill.GetPayableCost());
            if (!dbSubscription.HasBilling)
            {
                billsAmount = dbSubscription.Service.Price;
            }

            return Math.Max(0m, billsAmount - creditsAmount);
        }

        private BillPayment.ResponseType PayBills(RadiusREntities db, Subscription dbSubscription, long? billId, PaymentType paymentType)
        {
            var bills = dbSubscription.Bills.Where(b => b.PaymentTypeID == (short)PaymentType.None);
            if (billId.HasValue)
                bills = bills.Where(b => b.ID == billId);
            if (!bills.Any())
                return BillPayment.ResponseType.Success;

            return db.PayBills(bills, paymentType, BillPayment.AccountantType.Admin);
        }

        #endregion
    }
}