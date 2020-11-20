using NLog;
using RadiusR_Customer_Website.Models.ViewModels.Supports;
using RezaB.Web.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Controllers
{
    public class SupportController : BaseController
    {
        Logger generalLogger = LogManager.GetLogger("general");
        // GET: Support
        public ActionResult SupportRequests(int? page)
        {
            var supportRequests = new List<SupportRequestsVM>();
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SubscriptionID = User.GiveUserId();
                var results = db.SupportRequests
                    .Where(m => m.SubscriptionID == SubscriptionID && m.IsVisibleToCustomer == true)
                    .OrderByDescending(m => m.StateID == (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress)
                    .ThenByDescending(m => m.Date)
                    .Select(m => new SupportRequestsVM()
                    {
                        ID = m.ID,
                        ApprovalDate = m.CustomerApprovalDate,
                        Date = m.Date,
                        SupportNo = m.SupportPin,
                        State = m.StateID,
                        SupportRequestType = m.TypeID == null ? null : m.SupportRequestType.Name,
                        SupportRequestSubType = m.SubTypeID == null ? null : m.SupportRequestSubType.Name
                    }).AsQueryable();
                SetupPages(page, ref results, 10);
                ViewBag.HasOpenRequest = HasOpenRequest();
                return View(results.ToList());
            }
        }
        [HttpGet]
        public ActionResult NewRequest(int? RequestTypeId = null)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {

                if (HasOpenRequest())
                {
                    TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.HasActiveRequest;
                    return RedirectToAction("SupportRequests", "Support");
                }
                var supportRequests = db.SupportRequestTypes.Where(m => !m.IsStaffOnly).ToArray();
                if (Request.IsAjaxRequest())
                {
                    var list = db.SupportRequestSubTypes
                        .Where(m => m.SupportRequestTypeID == RequestTypeId).Select(m => new { Text = m.Name, Value = m.ID.ToString() });
                    return Json(list.ToArray(), JsonRequestBehavior.AllowGet);
                }
                var newRequest = new NewRequestVM
                {
                    RequestTypeList = supportRequests.Select(m => new SelectListItem { Text = m.Name, Value = m.ID.ToString() })
                };
                return View(newRequest);
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewRequest(NewRequestVM newRequest)
        {
            if (!ModelState.IsValid)
            {
                TempData["Errors"] = string.Format(RadiusRCustomerWebSite.Localization.Validation.Required, RadiusRCustomerWebSite.Localization.Common.Message);
            }
            else
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    if (HasOpenRequest())
                    {
                        TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.HasActiveRequest;
                        return RedirectToAction("SupportRequests", "Support");
                    }
                    db.SupportRequests.Add(new RadiusR.DB.SupportRequest()
                    {
                        Date = DateTime.Now,
                        IsVisibleToCustomer = true,
                        StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress,
                        TypeID = newRequest.RequestTypeId,
                        SubTypeID = newRequest.SubRequestTypeId,
                        SupportPin = CreateSupportPin(),
                        SubscriptionID = User.GiveUserId(),
                        SupportRequestProgresses =
                        {
                            new RadiusR.DB.SupportRequestProgress()
                            {
                                Date = DateTime.Now,
                                IsVisibleToCustomer = true,
                                Message = newRequest.Description,
                                ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.Create
                            }
                        }
                    });
                    db.SaveChanges();
                }
                TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.TaskCompleted;
                generalLogger.Warn("Created new request. UserId : " + User.GiveUserId());
            }
            return RedirectToAction("SupportRequests");
        }
        public ActionResult SupportDetails(long ID)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SupportProgress = db.SupportRequests.Find(ID);
                if (SupportProgress != null && SupportProgress.SubscriptionID == User.GiveUserId())
                {
                    var PassedTime = db.AppSettings.Where(m => m.Key == "SupportRequestPassedTime").Select(m => m.Value).FirstOrDefault();
                    var PassedTimeSpan = new TimeSpan(0, Convert.ToInt32(PassedTime.Split(':')[0]), Convert.ToInt32(PassedTime.Split(':')[1]), Convert.ToInt32(PassedTime.Split(':')[2]), 0);
                    var result = new SupportMessagesVM()
                    {
                        SupportDisplayType = RequestProgressState(ID),
                        //HasOpenRequest = HasOpenRequest(),
                        CustomerApprovalDate = SupportProgress.CustomerApprovalDate,
                        ID = ID,
                        State = SupportProgress.StateID,
                        SupportDate = SupportProgress.Date,
                        SupportNo = SupportProgress.SupportPin,
                        SupportRequestName = SupportProgress.TypeID == null ? "" : SupportProgress.SupportRequestType.Name,
                        SupportRequestSummary = SupportProgress.SubTypeID == null ? "" : SupportProgress.SupportRequestSubType.Name,
                        //IsPassedRequestOpenTime = (DateTime.Now - SupportProgress.SupportRequestProgresses
                        //.OrderByDescending(m => m.Date).Select(m => m.Date).FirstOrDefault()) > PassedTimeSpan ? true : false,
                        SupportMessageList = SupportProgress.SupportRequestProgresses.OrderByDescending(m => m.Date).Select(m => new SupportMessageList()
                        {
                            Message = m.Message,
                            MessageDate = m.Date,
                            SenderName = m.AppUserID == null ? User.Identity.Name.Length > 20 ? User.Identity.Name.Substring(0, 20) + "..." : User.Identity.Name
                            : RadiusRCustomerWebSite.Localization.Common.Agent,
                            IsCustomer = m.AppUserID == null ? true : false
                        })
                    };
                    return View(result);
                }
                else
                {
                    return ReturnErrorUrl(Url.Action("SupportRequests", "Support"));
                }
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewSupportMessage(long ID, string Message, bool ForOpenRequest = false, bool IsSolved = false)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SupportProgress = db.SupportRequests.Find(ID);
                if (SupportProgress != null && SupportProgress.SubscriptionID == User.GiveUserId())
                {
                    if (string.IsNullOrEmpty(Message))
                    {
                        return ReturnErrorUrl(Url.Action("SupportDetails", "Support", new { ID = ID }), string.Format(RadiusRCustomerWebSite.Localization.Validation.Required, RadiusRCustomerWebSite.Localization.Common.Message));
                    }
                    if (IsSolved)
                    {
                        SupportProgress.CustomerApprovalDate = DateTime.Now;
                        SupportProgress.StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done;
                        db.SaveChanges();
                        generalLogger.Warn("Problem is solved . User id : " + User.GiveUserId() + " Request Id : " + ID);
                        TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.TaskCompleted;
                    }
                    if (ForOpenRequest)
                    {
                        if (HasOpenRequest())
                        {
                            TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.HasActiveRequest;
                            return RedirectToAction("SupportRequests", "Support");
                        }
                        SupportProgress.StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress;
                        db.SaveChanges();
                        SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                        {
                            ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.ChangeState,
                            Date = DateTime.Now,
                            IsVisibleToCustomer = true,
                            Message = Message,
                            OldState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done,
                            NewState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress
                        });
                        db.SaveChanges();
                        generalLogger.Warn("Opened request again. Request id : " + ID + " User id : " + User.GiveUserId());
                        TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.OpenedRequestAgain;
                    }
                    else
                    {
                        SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                        {
                            ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.Create,
                            Date = DateTime.Now,
                            IsVisibleToCustomer = true,
                            Message = Message
                        });
                        db.SaveChanges();
                        generalLogger.Warn("Send Message. Request id : " + ID + " User id : " + User.GiveUserId());
                        if (!IsSolved)
                        {
                            TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.SendMessage;
                        }
                    }
                }
                else
                {
                    generalLogger.Warn("Wrong subscription id. User id : " + User.GiveUserId() + " and user id : " + SupportProgress.SubscriptionID + " in progress. Request Id : " + ID);
                    TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.Failed;
                }
            }
            return RedirectToAction("SupportDetails", "Support", new { ID = ID });
        }
        public ActionResult ReturnErrorUrl(string url, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                TempData["Errors"] = message;
            }
            return Redirect(url);
        }
        [HttpPost]
        public ActionResult SupportStatus()
        {
            try
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    var PassedTime = db.AppSettings.Where(m => m.Key == "SupportRequestPassedTime").Select(m => m.Value).FirstOrDefault();
                    var PassedTimeSpan = new TimeSpan(0, Convert.ToInt32(PassedTime.Split(':')[0]), Convert.ToInt32(PassedTime.Split(':')[1]), Convert.ToInt32(PassedTime.Split(':')[2]), 0);
                    var subscriptionId = User.GiveUserId();
                    var IsPassedTime = (DateTime.Now - db.SupportRequestProgresses.Where(s => s.SupportRequest.SubscriptionID == subscriptionId).OrderByDescending(s => s.Date).Select(s => s.Date).FirstOrDefault()) < PassedTimeSpan ? false : true;
                    var SupportRequests = db.SupportRequests
                        .Where(m => m.SubscriptionID == subscriptionId
                        && m.SupportRequestProgresses.OrderByDescending(s => s.Date).FirstOrDefault().AppUserID != null
                        && IsPassedTime == false && m.CustomerApprovalDate == null);
                    return Json(new { openRequestCount = SupportRequests.Count(), requestIds = SupportRequests.Select(m => m.ID).ToArray() }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { openRequest = 0, requestIds = new long[] { } }, JsonRequestBehavior.AllowGet);
            }
        }
        #region private methods
        private readonly Random _random = new Random();
        private string CreateSupportPin()
        {
            var keyData = "0123456789QWERTYUIPASDFGHJKLZXCVBNM0123456789";
            var pin = string.Empty;
            while (pin.Length < 12)
            {
                var current = _random.Next(0, keyData.Length - 1);
                pin += keyData[current];
                //keyData.Remove(current, 1);
            }
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                if (db.SupportRequests.Where(m => m.SupportPin == pin).FirstOrDefault() != null)
                {
                    CreateSupportPin();
                }
            }
            return pin;
        }
        private bool HasOpenRequest()
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var subscriptionId = User.GiveUserId();
                var HasOpenRequest = db.SupportRequests
                    .Where(m => m.StateID == (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress && subscriptionId == m.SubscriptionID)
                    .FirstOrDefault() != null;
                return HasOpenRequest;
            }
        }
        private SupportRequestDisplayTypes RequestProgressState(long SupportRequestID)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SupportRequest = db.SupportRequests.Find(SupportRequestID);
                if (SupportRequest.CustomerApprovalDate == null)
                {
                    if (SupportRequest.StateID == (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done)
                    {
                        var PassedTime = db.AppSettings.Where(m => m.Key == "SupportRequestPassedTime").Select(m => m.Value).FirstOrDefault();
                        var PassedTimeSpan = new TimeSpan(0, Convert.ToInt32(PassedTime.Split(':')[0]), Convert.ToInt32(PassedTime.Split(':')[1]), Convert.ToInt32(PassedTime.Split(':')[2]), 0);
                        var subscriptionId = User.GiveUserId();
                        var IsPassedTime = (DateTime.Now - SupportRequest.SupportRequestProgresses.OrderByDescending(s => s.Date).Select(s => s.Date).FirstOrDefault()) < PassedTimeSpan ? false : true;
                        if (IsPassedTime)
                        {
                            return SupportRequestDisplayTypes.NoneDisplay;
                        }
                        else
                        {

                            if (HasOpenRequest())
                            {
                                return SupportRequestDisplayTypes.NoneDisplay;
                            }
                            else
                            {
                                return SupportRequestDisplayTypes.OpenRequestAgainDisplay;
                            }
                        }
                    }
                    else
                    {
                        return SupportRequestDisplayTypes.AddNoteDisplay;
                    }
                }
                else
                {
                    return SupportRequestDisplayTypes.NoneDisplay;
                }
            }
        }
        #endregion
    }
}