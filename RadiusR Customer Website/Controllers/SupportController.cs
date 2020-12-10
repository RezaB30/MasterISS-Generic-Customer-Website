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
                    return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Common.HasActiveRequest);
                }
                var supportRequests = db.SupportRequestTypes.Where(m => !m.IsStaffOnly && !m.IsDisabled).ToArray();
                var supportSubRequests = db.SupportRequestSubTypes.Where(m => m.SupportRequestTypeID == RequestTypeId).ToArray();
                if (Request.IsAjaxRequest())
                {
                    var list = db.SupportRequestSubTypes
                        .Where(m => m.SupportRequestTypeID == RequestTypeId && !m.IsDisabled).Select(m => new { Text = m.Name, Value = m.ID.ToString() });
                    return Json(list.ToArray(), JsonRequestBehavior.AllowGet);
                }
                var newRequest = new NewRequestVM
                {
                    RequestTypeList = supportRequests.Select(m => new SelectListItem { Text = m.Name, Value = m.ID.ToString() }),
                    SubRequestTypeList = supportSubRequests.Select(m => new SelectListItem { Text = m.Name, Value = m.ID.ToString() })
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
                return ReturnMessageUrl(Url.Action("NewRequest", "Support"), ModelErrorMessages(ModelState));
            }
            else
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    if (HasOpenRequest())
                    {
                        return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Common.HasActiveRequest);
                    }
                    db.SupportRequests.Add(new RadiusR.DB.SupportRequest()
                    {
                        Date = DateTime.Now,
                        IsVisibleToCustomer = true,
                        StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress,
                        TypeID = newRequest.RequestTypeId,
                        SubTypeID = newRequest.SubRequestTypeId,
                        SupportPin = RadiusR.DB.RandomCode.CodeGenerator.GenerateSupportRequestPIN(),
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
                generalLogger.Warn($"Created new request. UserId : {User.GiveUserId()} {Environment.NewLine} Parameters => {Environment.NewLine}{newRequest}");
            }
            return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Validation.TaskCompleted);
        }
        public ActionResult SupportDetails(long ID)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SupportProgress = db.SupportRequests.Find(ID);
                if (SupportProgress != null && SupportProgress.SubscriptionID == User.GiveUserId() && SupportProgress.IsVisibleToCustomer)
                {
                    var PassedTime = db.AppSettings.Where(m => m.Key == "SupportRequestPassedTime").Select(m => m.Value).FirstOrDefault();
                    var PassedTimeSpan = new TimeSpan(0, Convert.ToInt32(PassedTime.Split(':')[0]), Convert.ToInt32(PassedTime.Split(':')[1]), Convert.ToInt32(PassedTime.Split(':')[2]), 0);
                    var result = new SupportMessagesVM()
                    {
                        SupportDisplayType = RequestProgressState(ID),
                        CustomerApprovalDate = SupportProgress.CustomerApprovalDate,
                        ID = ID,
                        State = SupportProgress.StateID,
                        SupportDate = SupportProgress.Date,
                        SupportNo = SupportProgress.SupportPin,
                        SupportRequestName = SupportProgress.TypeID == null ? "" : SupportProgress.SupportRequestType.Name,
                        SupportRequestSummary = SupportProgress.SubTypeID == null ? "" : SupportProgress.SupportRequestSubType.Name,
                        SupportMessageList = SupportProgress.SupportRequestProgresses.Where(m => m.IsVisibleToCustomer).OrderByDescending(m => m.Date).Select(m => new SupportMessageList()
                        {
                            Message = m.Message,
                            MessageDate = m.Date,
                            SenderName = m.AppUserID == null ? User.Identity.Name.Length > 20 ? User.Identity.Name.Substring(0, 20) + "..." : User.Identity.Name
                              : RadiusRCustomerWebSite.Localization.Common.Agent,
                            IsCustomer = m.AppUserID == null
                        })
                    };
                    return View(result);
                }
                else
                {
                    return ReturnMessageUrl(Url.Action("SupportRequests", "Support"));
                }
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewSupportMessage(RequestSupportMessage requestMessage)
        {
            if (!ModelState.IsValid)
            {
                return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }), ModelErrorMessages(ModelState));
            }
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var SupportProgress = db.SupportRequests.Find(requestMessage.ID);
                if (SupportProgress != null && SupportProgress.SubscriptionID == User.GiveUserId() && SupportProgress.IsVisibleToCustomer)
                {
                    if (requestMessage.IsSolved)
                    {
                        var CurrentState = (RadiusR.DB.Enums.SupportRequests.SupportRequestStateID)SupportProgress.StateID;
                        SupportProgress.CustomerApprovalDate = DateTime.Now;
                        SupportProgress.StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done;
                        if (CurrentState == RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done)
                        {
                            SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                            {
                                ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.Create,
                                Date = DateTime.Now,
                                IsVisibleToCustomer = true,
                                Message = requestMessage.Message
                            });
                        }
                        else
                        {
                            SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                            {
                                ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.ChangeState,
                                Date = DateTime.Now,
                                IsVisibleToCustomer = true,
                                Message = requestMessage.Message,
                                OldState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress,
                                NewState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done
                            });
                        }

                        db.SaveChanges();
                        generalLogger.Warn($"Problem is solved . User id : {User.GiveUserId()} {Environment.NewLine} Request Id : {requestMessage.ID}");
                        return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }), RadiusRCustomerWebSite.Localization.Validation.TaskCompleted);
                    }
                    if (requestMessage.ForOpenRequest)
                    {
                        if (HasOpenRequest())
                        {
                            return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Common.HasActiveRequest);
                        }
                        SupportProgress.StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress;
                        SupportProgress.AssignedGroupID = null;
                        SupportProgress.RedirectedGroupID = null;
                        SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                        {
                            ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.ChangeState,
                            Date = DateTime.Now,
                            IsVisibleToCustomer = true,
                            Message = requestMessage.Message,
                            OldState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done,
                            NewState = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress
                        });
                        db.SaveChanges();
                        generalLogger.Warn($"Opened request again. Request id : {requestMessage.ID} {Environment.NewLine} User id : {User.GiveUserId()}");
                        return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }),
                            RadiusRCustomerWebSite.Localization.Validation.OpenedRequestAgain);
                    }
                    if (requestMessage.ForAddNote)
                    {
                        SupportProgress.SupportRequestProgresses.Add(new RadiusR.DB.SupportRequestProgress()
                        {
                            ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.Create,
                            Date = DateTime.Now,
                            IsVisibleToCustomer = true,
                            Message = requestMessage.Message
                        });
                        db.SaveChanges();
                        generalLogger.Warn($"Send Message. Request id : {requestMessage.ID} {Environment.NewLine} User Id : {User.GiveUserId()}");
                        return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }),
                            RadiusRCustomerWebSite.Localization.Validation.SendMessage);
                        //if (!requestMessage.IsSolved)
                        //{
                        //    TempData["Errors"] = RadiusRCustomerWebSite.Localization.Validation.SendMessage;
                        //}
                    }
                }
                else
                {
                    generalLogger.Warn($"Wrong subscription id. User id : {User.GiveUserId()} {Environment.NewLine} Subscription Id : {SupportProgress.SubscriptionID} in progress. {Environment.NewLine} Request Id : {requestMessage.ID}");
                    return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }),
                            RadiusRCustomerWebSite.Localization.Validation.Failed);
                }
            }
            return RedirectToAction("SupportDetails", "Support", new { requestMessage.ID });
        }
        public ActionResult ReturnMessageUrl(string url, string message = null)
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
                    var CurrentSupportProgress = db.SupportRequestProgresses.Where(m => m.SupportRequest.SubscriptionID == subscriptionId && m.SupportRequest.IsVisibleToCustomer).OrderByDescending(m => m.Date).FirstOrDefault();
                    var IsPassedTime = false;
                    if (CurrentSupportProgress != null && CurrentSupportProgress.SupportRequest.StateID == (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.Done && ((DateTime.Now - CurrentSupportProgress.Date) > PassedTimeSpan))
                    {
                        IsPassedTime = true;
                    }
                    var SupportRequests = db.SupportRequests.OrderByDescending(m => m.Date).Where(m => m.SubscriptionID == subscriptionId && m.IsVisibleToCustomer).FirstOrDefault();
                    var IsAppUser = SupportRequests == null ? false : SupportRequests.SupportRequestProgresses.Where(m => m.IsVisibleToCustomer).OrderByDescending(m => m.Date).FirstOrDefault().AppUserID != null ? true : false;
                    var count = 0;
                    List<long> requestIds = new List<long>();
                    if (SupportRequests != null && IsAppUser && !IsPassedTime && SupportRequests.CustomerApprovalDate == null)
                    {
                        requestIds.Add(SupportRequests.ID);
                        count = 1;
                    }
                    return Json(new { openRequestCount = count, requestIds = requestIds.ToArray() }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { openRequest = 0, requestIds = new long[] { } }, JsonRequestBehavior.AllowGet);
            }
        }
        #region private methods        
        private bool HasOpenRequest()
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var subscriptionId = User.GiveUserId();
                var HasOpenRequest = db.SupportRequests
                    .Where(m => m.IsVisibleToCustomer && m.StateID == (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress && subscriptionId == m.SubscriptionID)
                    //.FirstOrDefault() != null;
                    .Any();
                return HasOpenRequest;
            }
        }
        private SupportRequestDisplayTypes RequestProgressState(long SupportRequestID)
        {
            using var db = new RadiusR.DB.RadiusREntities();
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
        private string ModelErrorMessages(ModelStateDictionary ModelState)
        {
            return string.Join(string.Empty, ModelState.Values.Select(m => string.Join(Environment.NewLine, m.Errors.Where(s => !string.IsNullOrEmpty(s.ErrorMessage)).Select(s => s.ErrorMessage))));
        }
        #endregion
    }
}