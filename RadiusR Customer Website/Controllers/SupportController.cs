using NLog;
//using RadiusR_Customer_Website.GenericCustomerServiceReference;
using MasterISS.CustomerService.GenericCustomerServiceReference;
using RadiusR_Customer_Website.Models.ViewModels.Supports;
using RezaB.Web.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Controllers
{
    public class SupportController : BaseController
    {
        Logger generalLogger = LogManager.GetLogger("general");
        GenericCustomerServiceClient client = new GenericCustomerServiceClient();
        // GET: Support
        public ActionResult SupportRequests(int? page)
        {
            var baseRequest = new GenericServiceSettings();
            var response = client.GetSupportList(new CustomerServiceBaseRequest()
            {
                Culture = baseRequest.Culture,
                Username = baseRequest.Username,
                Rand = baseRequest.Rand,
                Hash = baseRequest.Hash,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = User.GiveUserId()
                }
            });
            var results = response.ResponseMessage.ErrorCode != 0 ? Enumerable.Empty<SupportRequestsVM>().AsQueryable() : response.GetCustomerSupportListResponse.OrderByDescending(m => m.State == 1).ThenByDescending(m => m.Date).Select(m => new SupportRequestsVM()
            {
                ID = m.ID,
                ApprovalDate = Utilities.InternalUtilities.DateTimeConverter.ParseDateTime(m.ApprovalDate),
                Date = Utilities.InternalUtilities.DateTimeConverter.ParseDateTime(m.Date).Value,
                SupportNo = m.SupportNo,
                State = m.StateText,
                SupportRequestType = m.SupportRequestType,
                SupportRequestSubType = m.SupportRequestSubType
            }).AsQueryable();
            var hasOpenBaseRequest = new GenericServiceSettings();
            var hasOpenResponse = client.SupportHasActiveRequest(new CustomerServiceBaseRequest()
            {
                Culture = hasOpenBaseRequest.Culture,
                Username = hasOpenBaseRequest.Username,
                Rand = hasOpenBaseRequest.Rand,
                Hash = hasOpenBaseRequest.Hash,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = User.GiveUserId()
                }
            });
            if (hasOpenResponse.ResponseMessage.ErrorCode != 0)
            {
                //get error message
            }
            SetupPages(page, ref results, 10);
            ViewBag.HasOpenRequest = hasOpenResponse.ResponseMessage.ErrorCode != 0 ? true : hasOpenResponse.HasActiveRequest;
            return View(results.ToList());
        }
        [HttpGet]
        public ActionResult NewRequest()
        {
            var hasActiveBase = new GenericServiceSettings();

            var hasActiveRequest = client.SupportHasActiveRequest(new CustomerServiceBaseRequest()
            {
                Culture = hasActiveBase.Culture,
                Hash = hasActiveBase.Hash,
                Username = hasActiveBase.Username,
                Rand = hasActiveBase.Rand,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = User.GiveUserId()
                }
            });
            if (hasActiveRequest.ResponseMessage.ErrorCode != 0)
            {
                return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), hasActiveRequest.ResponseMessage.ErrorMessage);
            }
            if (hasActiveRequest.ResponseMessage.ErrorCode == 0 && hasActiveRequest.HasActiveRequest == true)
            {
                return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), hasActiveRequest.ResponseMessage.ErrorMessage);
            }
            ViewBag.RequestTypes = GetSupportTypes();
            ViewBag.SubRequestTypes = new SelectList(Enumerable.Empty<object>());
            return View();
        }
        [HttpPost]
        public ActionResult GetSubRequestTypes(int RequestTypeId)
        {
            var list = GetSupportSubTypes(RequestTypeId).Select(sst => new { Text = sst.Text, Value = sst.Value }).ToList();
            list.Insert(0, new { Text = RadiusRCustomerWebSite.Localization.Common.SelectionSupportSubType, Value = "" });
            return Json(list.ToArray());
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewRequest(NewRequestVM newRequest, HttpPostedFileBase[] attachments)
        {
            if (newRequest.Description != null)
                newRequest.Description = newRequest.Description.Trim(new char[] { ' ', '\n', '\r' });
            ModelState.Clear();
            TryValidateModel(newRequest);
            if (!ModelState.IsValid)
            {
                ViewBag.RequestTypes = GetSupportTypes(newRequest.RequestTypeId);
                ViewBag.SubRequestTypes = GetSupportSubTypes(newRequest.RequestTypeId, newRequest.SubRequestTypeId);
                return View(newRequest);
            }
            else
            {
                attachments = attachments != null ? attachments.Where(att => att != null).FirstOrDefault() == null ? null : attachments : attachments;
                if (HasOpenRequest())
                {
                    return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Common.HasActiveRequest);
                }
                var genericAppSetting = new ServiceUtilities().CustomerWebsiteGenericSettings();
                var fileMaxSize = genericAppSetting.GenericAppSettings == null ? 5242880 : genericAppSetting.GenericAppSettings.FileMaxSize;
                var fileMaxCount = genericAppSetting.GenericAppSettings == null ? 20 : genericAppSetting.GenericAppSettings.FileMaxCount;
                if (attachments != null && attachments.Where(att => att.ContentLength > fileMaxSize).FirstOrDefault() != null)
                {
                    return ReturnMessageUrl(Url.Action("NewRequests", "Support"), string.Format(RadiusRCustomerWebSite.Localization.Common.FileSizeError, (fileMaxSize / 1000000)));
                }
                List<MasterISS.CustomerService.GenericCustomerServiceReference.Attachment> attachmentList = new List<MasterISS.CustomerService.GenericCustomerServiceReference.Attachment>();
                if (attachments != null && attachments.Count() != 0 && attachments.Count() <= fileMaxCount)
                {
                    foreach (var item in attachments)
                    {
                        using (var binaryReader = new BinaryReader(item.InputStream))
                        {
                            var attachmentByte = binaryReader.ReadBytes(item.ContentLength);
                            System.IO.FileInfo fileInfo = new System.IO.FileInfo(item.FileName);
                            var fileExtension = fileInfo.Extension.Replace(".", "");
                            attachmentList.Add(new MasterISS.CustomerService.GenericCustomerServiceReference.Attachment()
                            {
                                FileContent = attachmentByte,
                                FileExtention = fileExtension,
                                FileName = item.FileName
                            });
                        }
                    }
                }
                var baseRequest = new GenericServiceSettings();
                var register = client.SupportRegister(new CustomerServiceSupportRegisterRequest()
                {
                    Culture = baseRequest.Culture,
                    Hash = baseRequest.Hash,
                    Rand = baseRequest.Rand,
                    SupportRegisterParameters = new SupportRegisterRequest()
                    {
                        Description = newRequest.Description,
                        RequestTypeId = newRequest.RequestTypeId,
                        SubRequestTypeId = newRequest.SubRequestTypeId,
                        SubscriptionId = User.GiveUserId(),
                        Attachments = attachmentList.ToArray()
                    },
                    Username = baseRequest.Username
                });
                if (register.ResponseMessage.ErrorCode != 0)
                {
                    return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), register.ResponseMessage.ErrorMessage);
                }
                generalLogger.Warn($"Created new request. UserId : {User.GiveUserId()} {Environment.NewLine} Parameters => {Environment.NewLine}{newRequest}");
            }
            return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), RadiusRCustomerWebSite.Localization.Validation.TaskCompleted);
        }
        public ActionResult SupportDetails(long ID)
        {
            var baseRequest = new GenericServiceSettings();
            var supportDetailResponse = client.GetSupportDetailMessages(new CustomerServiceSupportDetailMessagesRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                SupportDetailMessagesParameters = new SupportDetailMessagesRequest()
                {
                    SupportId = ID,
                    SubscriptionId = User.GiveUserId()
                }
            });
            var attachments = new ServiceUtilities().GetSupportAttachmentList(ID);
            var genericAppSetting = new ServiceUtilities().CustomerWebsiteGenericSettings();
            var fileMaxCount = genericAppSetting.GenericAppSettings == null ? 20 : genericAppSetting.GenericAppSettings.FileMaxCount;
            var fileMaxSize = genericAppSetting.GenericAppSettings == null ? 5242880 : genericAppSetting.GenericAppSettings.FileMaxSize;
            ViewBag.FileMaxSize = fileMaxSize;
            ViewBag.CanSendFile = false;
            if (attachments.GetSupportAttachmentList == null || attachments.GetSupportAttachmentList.Count() == 0 || attachments.GetSupportAttachmentList.Count() < fileMaxCount)
            {
                ViewBag.CanSendFile = true;
            }
            if (supportDetailResponse.ResponseMessage.ErrorCode != 0)
            {
                return ReturnMessageUrl(Url.Action("SupportRequests", "Support"));
            }
            if (supportDetailResponse.ResponseMessage.ErrorCode == 0 && supportDetailResponse.SupportDetailMessagesResponse != null)
            {
                var result = new SupportMessagesVM()
                {
                    SupportDisplayType = (SupportRequestDisplayTypes)supportDetailResponse.SupportDetailMessagesResponse.SupportRequestDisplayType.SupportRequestDisplayTypeId,
                    CustomerApprovalDate = Utilities.InternalUtilities.DateTimeConverter.ParseDateTime(supportDetailResponse.SupportDetailMessagesResponse.CustomerApprovalDate),
                    ID = ID,
                    State = supportDetailResponse.SupportDetailMessagesResponse.State.StateName,
                    SupportDate = Utilities.InternalUtilities.DateTimeConverter.ParseDateTime(supportDetailResponse.SupportDetailMessagesResponse.SupportDate).Value,
                    SupportNo = supportDetailResponse.SupportDetailMessagesResponse.SupportNo,
                    SupportRequestName = supportDetailResponse.SupportDetailMessagesResponse.SupportRequestName,
                    SupportRequestSummary = supportDetailResponse.SupportDetailMessagesResponse.SupportRequestSubName,
                    SupportMessageList = supportDetailResponse.SupportDetailMessagesResponse.SupportMessages.OrderByDescending(m => m.MessageDate).Select(m => new SupportMessageList()
                    {
                        Message = m.Message,
                        MessageDate = Utilities.InternalUtilities.DateTimeConverter.ParseDateTime(m.MessageDate).Value,
                        SenderName = m.IsCustomer ? User.Identity.Name.Length > 20 ? User.Identity.Name.Substring(0, 20) + "..." : User.Identity.Name
                          : RadiusRCustomerWebSite.Localization.Common.Agent,
                        IsCustomer = m.IsCustomer,
                        StageId = m.StageId
                    })
                };
                return View(result);
            }
            return ReturnMessageUrl(Url.Action("SupportRequests", "Support"));
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult NewSupportMessage(RequestSupportMessage requestMessage, HttpPostedFileBase[] attachments)
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
            attachments = attachments != null ? attachments.Where(att => att != null).FirstOrDefault() == null ? null : attachments : attachments;
            var genericAppSetting = new ServiceUtilities().CustomerWebsiteGenericSettings();
            var fileMaxSize = genericAppSetting.GenericAppSettings == null ? 5242880 : genericAppSetting.GenericAppSettings.FileMaxSize;
            if (attachments != null && attachments.Where(att => att.ContentLength > fileMaxSize).FirstOrDefault() != null)
            {
                return ReturnMessageUrl(Url.Action("SupportResults", "Support", new { requestMessage.ID }), string.Format(RadiusRCustomerWebSite.Localization.Common.FileSizeError, (fileMaxSize / 1000000)));
            }
            //
            List<MasterISS.CustomerService.GenericCustomerServiceReference.Attachment> getAttachments = new List<Attachment>();
            if (attachments != null && attachments.Count() != 0)
            {
                var fileMaxCount = genericAppSetting.GenericAppSettings == null ? 20 : genericAppSetting.GenericAppSettings.FileMaxCount;
                var attachmentsList = new ServiceUtilities().GetSupportAttachmentList(requestMessage.ID);
                if (attachmentsList.GetSupportAttachmentList == null || attachmentsList.GetSupportAttachmentList.Count() == 0
                    || (attachments.Count() + attachmentsList.GetSupportAttachmentList.Count()) <= fileMaxCount)
                {
                    foreach (var item in attachments)
                    {
                        using (var binaryReader = new BinaryReader(item.InputStream))
                        {
                            var attachmentByte = binaryReader.ReadBytes(item.ContentLength);
                            var fileInfo = new System.IO.FileInfo(item.FileName);
                            var fileExtension = fileInfo.Extension.Replace(".", "");
                            getAttachments.Add(new Attachment()
                            {
                                FileContent = attachmentByte,
                                FileName = item.FileName,
                                FileExtention = fileExtension
                            });
                            //var saveAttachment = new ServiceUtilities().SaveSupportAttachment(newMessageResponse.SendSupportMessageResponse.Value, item.FileName, attachmentByte, fileExtension, requestMessage.ID);
                            //generalLogger.Info($"Save Attachment Service Response | Error Code : {saveAttachment.ResponseMessage.ErrorCode} - Error Message : {saveAttachment.ResponseMessage.ErrorMessage}");
                        }
                    }
                }
                else
                {
                    return ReturnMessageUrl(Url.Action("SupportResults", "Support", new { requestMessage.ID }), RadiusRCustomerWebSite.Localization.Common.FileUploadError);
                }
            }
            //
            var baseRequest = new GenericServiceSettings();
            var newMessageResponse = client.SendSupportMessage(new CustomerServiceSendSupportMessageRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                SendSupportMessageParameters = new SendSupportMessageRequest()
                {
                    Attachments = getAttachments.ToArray(),
                    Message = requestMessage.Message,
                    SubscriptionId = User.GiveUserId(),
                    SupportId = requestMessage.ID,
                    SupportMessageType = requestMessage.ForAddNote == true ? (int)SupportMesssageTypes.AddNote
                    : requestMessage.IsSolved == true ? (int)SupportMesssageTypes.ProblemSolved : (int)SupportMesssageTypes.OpenRequestAgain
                }
            });
            generalLogger.Debug($"NewSupportMessage response -> ErrorCode : {newMessageResponse.ResponseMessage.ErrorCode} - Error Message : {newMessageResponse.ResponseMessage.ErrorMessage}");
            if (newMessageResponse.ResponseMessage.ErrorCode == 5) // has active request
            {
                return ReturnMessageUrl(Url.Action("SupportRequests", "Support"), newMessageResponse.ResponseMessage.ErrorMessage);
            }
            return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { requestMessage.ID }), newMessageResponse.ResponseMessage.ErrorMessage);
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
                var baseRequest = new GenericServiceSettings();
                var statusResponse = client.SupportStatus(new CustomerServiceBaseRequest()
                {
                    Culture = baseRequest.Culture,
                    Hash = baseRequest.Hash,
                    Rand = baseRequest.Rand,
                    Username = baseRequest.Username,
                    SubscriptionParameters = new BaseSubscriptionRequest()
                    {
                        SubscriptionId = User.GiveUserId()
                    }
                });
                generalLogger.Debug($"SupportStatus response -> ErrorCode : {statusResponse.ResponseMessage.ErrorCode} - Error Message : {statusResponse.ResponseMessage.ErrorMessage}");
                if (statusResponse.ResponseMessage.ErrorCode == 0 && statusResponse.SupportStatusResponse != null)
                {
                    return Json(new { openRequestCount = statusResponse.SupportStatusResponse.Count, requestIds = new long[] { statusResponse.SupportStatusResponse.StageId.GetValueOrDefault(0) } }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { openRequest = 0, requestIds = new long[] { } }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { openRequest = 0, requestIds = new long[] { } }, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpPost]
        public ActionResult GetSupportAttachments(long supportId)
        {
            var attachments = new ServiceUtilities().GetSupportAttachmentList(supportId);
            if (attachments.GetSupportAttachmentList == null)
            {
                return Json(Enumerable.Empty<object>(), JsonRequestBehavior.AllowGet);
            }
            return Json(attachments.GetSupportAttachmentList.Select(a => new { FileName = a.FileName, ServerName = a.ServerSideName, StageId = a.StageId }).ToArray(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult DownloadSupportAttachment(long? supportId, string fileName)
        {
            var attachment = new ServiceUtilities().GetSupportAttachment(supportId, fileName);
            if (attachment.ResponseMessage.ErrorCode != 0)
            {
                return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { ID = supportId }), attachment.ResponseMessage.ErrorMessage);
            }
            if (attachment.GetSupportAttachment == null)
            {
                return ReturnMessageUrl(Url.Action("SupportDetails", "Support", new { ID = supportId }), RadiusRCustomerWebSite.Localization.Common.InternalErrorDescription);
            }
            return File(attachment.GetSupportAttachment.FileContent, attachment.GetSupportAttachment.MIMEType, $"{attachment.GetSupportAttachment.FileName}.{attachment.GetSupportAttachment.FileExtention}");
        }
        #region private methods        
        private bool HasOpenRequest()
        {
            var hasActiveBase = new GenericServiceSettings();

            var hasActiveRequest = client.SupportHasActiveRequest(new CustomerServiceBaseRequest()
            {
                Culture = hasActiveBase.Culture,
                Hash = hasActiveBase.Hash,
                Username = hasActiveBase.Username,
                Rand = hasActiveBase.Rand,
                SubscriptionParameters = new BaseSubscriptionRequest()
                {
                    SubscriptionId = User.GiveUserId()
                }
            });
            if (hasActiveRequest.ResponseMessage.ErrorCode == 0)
            {
                return hasActiveRequest.HasActiveRequest != null && hasActiveRequest.HasActiveRequest.Value;
            }
            return false;
        }
        private string ModelErrorMessages(ModelStateDictionary ModelState)
        {
            return string.Join(Environment.NewLine, ModelState.Values.Select(m => string.Join(Environment.NewLine, m.Errors.Where(s => !string.IsNullOrEmpty(s.ErrorMessage)).Select(s => $"<div class='text-red'>{s.ErrorMessage}<div>"))));
        }
        private SelectList GetSupportTypes(object selectedValue = null)
        {
            var baseRequest = new GenericServiceSettings();
            var response = client.GetSupportTypes(new CustomerServiceSupportTypesRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                SupportTypesParameters = new SupportTypesRequest()
                {
                    IsDisabled = false,
                    IsStaffOnly = false
                }
            });
            if (response.ResponseMessage.ErrorCode != 0)
            {
                return new SelectList(Enumerable.Empty<object>());
            }
            return new SelectList(response.ValueNamePairList.Select(pair => new { Name = pair.Name, Value = pair.Value }), "Value", "Name", selectedValue);
        }
        private SelectList GetSupportSubTypes(int? SupportTypeId, object selectedValue = null)
        {
            var baseRequest = new GenericServiceSettings();
            var response = client.GetSupportSubTypes(new CustomerServiceSupportSubTypesRequest()
            {
                Culture = baseRequest.Culture,
                Hash = baseRequest.Hash,
                Rand = baseRequest.Rand,
                Username = baseRequest.Username,
                SupportSubTypesParameters = new SupportSubTypesRequest()
                {
                    IsDisabled = false,
                    SupportTypeID = SupportTypeId
                }
            });
            if (response.ResponseMessage.ErrorCode != 0)
            {
                return new SelectList(Enumerable.Empty<object>());
            }
            return new SelectList(response.ValueNamePairList.Select(pair => new { Name = pair.Name, Value = pair.Value }), "Value", "Name", selectedValue);
        }
        #endregion
    }
}