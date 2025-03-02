﻿using NLog;
using RadiusR.DB;
using RadiusR.DB.Settings;
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
                        SupportRequestType = m.SupportRequestType.Name,
                        SupportRequestSubType = m.SupportRequestSubType.Name
                    }).AsQueryable();
                SetupPages(page, ref results, 10);
                ViewBag.HasOpenRequest = HasOpenRequest();
                return View(results.ToList());
            }
        }
        [HttpGet]
        public ActionResult NewRequest()
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                if (HasOpenRequest())
                {
                    return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Common.HasActiveRequest);
                }
                ViewBag.RequestTypes = new SelectList(db.SupportRequestTypes.Where(m => !m.IsStaffOnly && !m.IsDisabled).Select(m => new { Value = m.ID, Name = m.Name }).ToArray(), "Value", "Name");
                ViewBag.SubRequestTypes = new SelectList(Enumerable.Empty<object>());
                return View();
            }
        }
        [HttpPost]
        public ActionResult GetSubRequestTypes(int RequestTypeId)
        {
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var list = db.SupportRequestSubTypes
                           .Where(m => m.SupportRequestTypeID == RequestTypeId && !m.IsDisabled).Select(m => new { Text = m.Name, Value = m.ID.ToString() }).ToList();
                list.Insert(0, new { Text = RadiusRCustomerWebSite.Localization.Common.SelectionSupportSubType, Value = "" });
                return Json(list.ToArray());
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewRequest(NewRequestVM newRequest)
        {
            if (newRequest.Description != null)
                newRequest.Description = newRequest.Description.Trim(new char[] { ' ', '\n', '\r' });
            ModelState.Clear();
            TryValidateModel(newRequest);
            if (!ModelState.IsValid)
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    var supportRequests = db.SupportRequestTypes.Where(m => !m.IsStaffOnly && !m.IsDisabled).ToArray();
                    var supportSubRequests = db.SupportRequestSubTypes.Where(m => m.SupportRequestTypeID == newRequest.RequestTypeId).ToArray();
                    ViewBag.RequestTypes = new SelectList(db.SupportRequestTypes.Where(m => !m.IsStaffOnly && !m.IsDisabled).Select(m => new { Value = m.ID, Name = m.Name }).ToArray(), "Value", "Name", newRequest.RequestTypeId);
                    ViewBag.SubRequestTypes = new SelectList(db.SupportRequestSubTypes.Where(m => m.SupportRequestTypeID == newRequest.RequestTypeId).Select(m => new { Value = m.ID, Name = m.Name }).ToArray(), "Value", "Name", newRequest.SubRequestTypeId);
                    return View(newRequest);
                }

                //return ReturnMessageUrl(Url.Action("NewRequest", "Support", new { newRequest.RequestTypeId, newRequest.SubRequestTypeId }), ModelErrorMessages(ModelState));
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
                        TypeID = (int)newRequest.RequestTypeId,
                        SubTypeID = (int)newRequest.SubRequestTypeId,
                        SupportPin = RadiusR.DB.RandomCode.CodeGenerator.GenerateSupportRequestPIN(),
                        SubscriptionID = User.GiveUserId(),
                        SupportRequestProgresses =
                        {
                            new RadiusR.DB.SupportRequestProgress()
                            {
                                Date = DateTime.Now,
                                IsVisibleToCustomer = true,
                                Message = newRequest.Description.Trim(new char[]{ ' ' , '\n' , '\r' }),
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
                    var PassedTimeSpan = CustomerWebsiteSettings.SupportRequestPassedTime;
                    var result = new SupportMessagesVM()
                    {
                        SupportDisplayType = RequestProgressState(ID),
                        CustomerApprovalDate = SupportProgress.CustomerApprovalDate,
                        ID = ID,
                        State = SupportProgress.StateID,
                        SupportDate = SupportProgress.Date,
                        SupportNo = SupportProgress.SupportPin,
                        SupportRequestName = SupportProgress.SupportRequestType.Name,
                        SupportRequestSummary = SupportProgress.SupportRequestSubType.Name,
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
            if (requestMessage.Message != null)
                requestMessage.Message = requestMessage.Message.Trim(new char[] { ' ', '\n', '\r' });
            if (requestMessage.IsSolved)
            {
                if (string.IsNullOrEmpty(requestMessage.Message))
                    requestMessage.Message = RadiusRCustomerWebSite.Localization.Common.ProblemSolved;
            }
            ModelState.Clear();
            TryValidateModel(requestMessage);
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
                    var PassedTimeSpan = CustomerWebsiteSettings.SupportRequestPassedTime;
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
                    var PassedTimeSpan = CustomerWebsiteSettings.SupportRequestPassedTime;
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
            return string.Join(Environment.NewLine, ModelState.Values.Select(m => string.Join(Environment.NewLine, m.Errors.Where(s => !string.IsNullOrEmpty(s.ErrorMessage)).Select(s => $"<div class='text-red'>{s.ErrorMessage}<div>"))));
        }
        #endregion
    }
}