using NLog;
using RadiusR_Customer_Website.Models.ViewModels.Customer;
using RezaB.TurkTelekom.WebServices;
using RezaB.TurkTelekom.WebServices.Address;
using RezaB.TurkTelekom.WebServices.Availability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Controllers
{
    //[Authorize(Roles = "Closed")]
    [AllowAnonymous]
    public class CustomerController : BaseController
    {
        Logger customer = LogManager.GetLogger("customer");
        Logger infrastructureLogger = LogManager.GetLogger("infrastructure");
        // GET: Customer
        public ActionResult CallRequest()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult CallRequest(CustomerCallRequest CallRequest)
        {
            if (ModelState.IsValid)
            {
                var captcha = Session["captcha"] ?? string.Empty;
                if (captcha as string != CallRequest.Captcha.ToLower())
                {
                    ModelState.AddModelError("Captcha", string.Format(RadiusRCustomerWebSite.Localization.Common.NotValid, RadiusRCustomerWebSite.Localization.Common.Captcha));
                    return View(CallRequest);
                }
                // call me function check phone number 
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    var hasOpenRequset = db.SupportRequestProgresses.Where(m => m.Message.Contains(CallRequest.PhoneNo) && m.SupportRequest.StateID == (int)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress && m.SupportRequest.TypeID == 2).Any();
                    if (hasOpenRequset)
                    {
                        TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.HasActiveRequest;
                        return View(CallRequest);
                    }
                }
                RegisterCallRequest(CallRequest);
                TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.CallMeRequestCompleted;
                customer.Info(CallRequest.ToString());
                return RedirectToAction("CallRequest");
            }
            return View(CallRequest);
        }
        public ActionResult CallRequestByBBK()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult CallRequestByBBK(CustomerCallRequest CallRequest)
        {
            if (Session["GetAvailabilityResult"] != null)
            {
                AvailabilityResult availability = Session["GetAvailabilityResult"] as AvailabilityResult;
                CallRequest.Description = availability.ToString();
            }
            if (ModelState.IsValid)
            {
                var captcha = Session["captcha"] ?? string.Empty;
                if (captcha as string != CallRequest.Captcha.ToLower())
                {
                    ModelState.AddModelError("Captcha", string.Format(RadiusRCustomerWebSite.Localization.Common.NotValid, RadiusRCustomerWebSite.Localization.Common.Captcha));
                    return View(CallRequest);
                }
                // call me function check phone number 
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    var hasOpenRequset = db.SupportRequestProgresses.Where(m => m.Message.Contains(CallRequest.PhoneNo) && m.SupportRequest.StateID == (int)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress && m.SupportRequest.TypeID == 2).Any();
                    if (hasOpenRequset)
                    {
                        TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.HasActiveRequest;
                        return View(CallRequest);
                    }
                }
                RegisterCallRequest(CallRequest);
                Session.Remove("GetAvailabilityResult"); // remove infrastructure infoes
                TempData["Errors"] = RadiusRCustomerWebSite.Localization.Common.CallMeRequestCompleted;
                customer.Info(CallRequest.ToString());
                return RedirectToAction("InfrastructureByBBK");
            }
            return View(CallRequest);
        }
        private void RegisterCallRequest(CustomerCallRequest CallRequest)
        {
            using var db = new RadiusR.DB.RadiusREntities();
            db.SupportRequests.Add(new RadiusR.DB.SupportRequest()
            {
                Date = DateTime.Now,
                IsVisibleToCustomer = false,
                StateID = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestStateID.InProgress,
                TypeID = 2, // " BENİ ARA " ID or get from name 
                SubTypeID = null,
                SupportPin = RadiusR.DB.RandomCode.CodeGenerator.GenerateSupportRequestPIN(),
                SubscriptionID = null,
                SupportRequestProgresses =
                        {
                            new RadiusR.DB.SupportRequestProgress()
                            {
                                Date = DateTime.Now,
                                IsVisibleToCustomer = false,
                                Message =$"{RadiusRCustomerWebSite.Localization.Common.CustomerName} : {CallRequest.CustomerName}{Environment.NewLine}" +
                                $"{RadiusRCustomerWebSite.Localization.Common.PhoneNo} : {CallRequest.PhoneNo}{Environment.NewLine}" +
                                $"{RadiusRCustomerWebSite.Localization.Common.Description} : {CallRequest.Description}",
                                ActionType = (short)RadiusR.DB.Enums.SupportRequests.SupportRequestActionTypes.Create
                            }
                        }
            });
            db.SaveChanges();
        }
        RadiusR.API.AddressQueryAdapter.AddressServiceAdapter AddressService = new RadiusR.API.AddressQueryAdapter.AddressServiceAdapter();
        public ActionResult InfrastructureByBBK()
        {
            Session.Remove("GetAvailabilityResult");
            Models.ViewModels.Customer.InfrastructureBBK infrastructure = new Models.ViewModels.Customer.InfrastructureBBK();
            var GetProvince = AddressService.GetProvinces(); //client.GetProvincesList();
            var ProvinceList = new SelectList(GetProvince.Data.Select(m => new { Value = m.Code, Text = m.Name }), "Value", "Text");
            infrastructure.Province = ProvinceList;
            return View(infrastructure);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetDistrict(long code)
        {
            var GetDistrict = AddressService.GetProvinceDistricts(code);
            if (GetDistrict.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetDistrict.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetRegions(long code)
        {
            var GetRegions = AddressService.GetDistrictRuralRegions(code);
            if (GetRegions.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetRegions.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetNeighborhoods(long code)
        {
            var GetNeighborhoods = AddressService.GetRuralRegionNeighbourhoods(code);
            if (GetNeighborhoods.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetNeighborhoods.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetStreets(long code)
        {
            var GetStreets = AddressService.GetNeighbourhoodStreets(code);
            if (GetStreets.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetStreets.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetBuildings(long code)
        {
            var GetBuildings = AddressService.GetStreetBuildings(code);
            if (GetBuildings.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetBuildings.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetApartments(long code)
        {
            var GetApartments = AddressService.GetBuildingApartments(code);
            if (GetApartments.Data == null)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            return Json(GetApartments.Data.Select(m => new { text = m.Name, value = m.Code }), JsonRequestBehavior.AllowGet);
        }
        public Models.ViewModels.Customer.AvailabilityResult GetAvailability(string bbk)
        {
            string TTUsername, TTPassword;
            using (var db = new RadiusR.DB.RadiusREntities())
            {
                var TelekomInfoes = db.TelekomAccessCredentials.Where(m => m.DomainID == 1).FirstOrDefault();
                TTUsername = TelekomInfoes.XDSLWebServiceUsername;
                TTPassword = TelekomInfoes.XDSLWebServicePassword;
            }
            var client = new AvailabilityServiceClient(long.Parse(TTUsername), TTPassword);
            var xdslTypeAdsl = AvailabilityServiceClient.XDSLType.ADSL;
            var xdslTypeVdsl = AvailabilityServiceClient.XDSLType.VDSL;
            var xdslTypeFiber = AvailabilityServiceClient.XDSLType.Fiber;
            var queryType = AvailabilityServiceClient.QueryType.BBK;
            ServiceResponse<AvailabilityServiceClient.AvailabilityDescription> availabAdsl = null, AvailabVdsl = null, AvailabFiber = null;
            List<Thread> threads = new List<Thread>();
            Thread ThreadAdsl = new Thread(() => { availabAdsl = client.Check(xdslTypeAdsl, queryType, bbk); });
            Thread ThreadVdsl = new Thread(() => { AvailabVdsl = client.Check(xdslTypeVdsl, queryType, bbk); });
            Thread ThreadFiber = new Thread(() => { AvailabFiber = client.Check(xdslTypeFiber, queryType, bbk); });
            ThreadAdsl.Start();
            ThreadVdsl.Start();
            ThreadFiber.Start();
            threads.Add(ThreadAdsl);
            threads.Add(ThreadVdsl);
            threads.Add(ThreadFiber);
            foreach (var item in threads)
            {
                item.Join();
            }
            availabAdsl = availabAdsl.InternalException != null ? client.Check(xdslTypeAdsl, queryType, bbk) : availabAdsl;
            AvailabVdsl = AvailabVdsl.InternalException != null ? client.Check(xdslTypeVdsl, queryType, bbk) : AvailabVdsl;
            AvailabFiber = AvailabFiber.InternalException != null ? client.Check(xdslTypeFiber, queryType, bbk) : AvailabFiber;
            if (availabAdsl.InternalException != null && AvailabFiber.InternalException != null && AvailabVdsl.InternalException != null)
            {
                return new AvailabilityResult()
                {
                    IsSuccess = false
                };
            }
            bool altyapiAdsl = availabAdsl.InternalException != null ? false : availabAdsl.Data.Description.ErrorMessage == null ? availabAdsl.Data.Description.HasInfrastructure.Value : false;
            bool altyapiVdsl = AvailabVdsl.InternalException != null ? false : AvailabVdsl.Data.Description.ErrorMessage == null ? AvailabVdsl.Data.Description.HasInfrastructure.Value : false;
            bool altyapiFiber = AvailabFiber.InternalException != null ? false : AvailabFiber.Data.Description.ErrorMessage == null ? AvailabFiber.Data.Description.HasInfrastructure.Value : false;
            var speedAdsl = altyapiAdsl ? availabAdsl.Data.Description.DSLMaxSpeed.Value : 0;
            var speedVdsl = altyapiVdsl ? AvailabVdsl.Data.Description.DSLMaxSpeed.Value : 0;
            var speedFiber = altyapiFiber ? AvailabFiber.Data.Description.DSLMaxSpeed.Value : 0;
            AddressServiceClient addressServiceClient = new AddressServiceClient(long.Parse(TTUsername), TTPassword);
            var address = addressServiceClient.GetAddressFromCode(Convert.ToInt64(bbk));
            return new Models.ViewModels.Customer.AvailabilityResult()
            {
                IsSuccess = true,
                Datetime = DateTime.Now,
                IPAddress = Utilities.InternalUtilities.GetUserIP(),
                QueryKey = bbk,
                AdslSVUID = availabAdsl != null ? availabAdsl.Data.Description.SVUID : "",
                FiberSVUID = AvailabFiber != null ? AvailabFiber.Data.Description.SVUID : "",
                VdslSVUID = AvailabVdsl != null ? AvailabVdsl.Data.Description.SVUID : "",
                address = address.Data == null ? "" : address.Data.AddressText,
                AdslSpeed = speedAdsl,
                FiberSpeed = speedFiber,
                VdslSpeed = speedVdsl,
                AdslDistance = availabAdsl.Data != null ? availabAdsl.Data.Description.Distance + " M" : "??",
                VdslDistance = AvailabVdsl.Data != null ? AvailabVdsl.Data.Description.Distance + " M" : "??",
                FiberDistance = AvailabFiber.Data != null ? AvailabFiber.Data.Description.Distance + " M" : "??",
                AdslPortState = availabAdsl.Data != null ? availabAdsl.Data.Description.PortState : AvailabilityServiceClient.PortState.NotAvailable,
                FiberPortState = AvailabFiber.Data != null ? AvailabFiber.Data.Description.PortState : AvailabilityServiceClient.PortState.NotAvailable,
                VdslPortState = AvailabVdsl.Data != null ? AvailabVdsl.Data.Description.PortState : AvailabilityServiceClient.PortState.NotAvailable
            };
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetAvailabilityResult(string BBK)
        {
            var result = GetAvailability(BBK.ToString());
            infrastructureLogger.Warn(result);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("IsSuccess", RadiusRCustomerWebSite.Localization.Validation.Failed);
                ViewBag.IsSuccess = false;
                return View(new AvailabilityResultViewModel()
                {
                    MaxSpeed = "??",
                    PortState = "??",
                    SVUID = "??"
                });
            }
            Session["GetAvailabilityResult"] = result;
            if (result.FiberSpeed != 0)
            {
                return View(new AvailabilityResultViewModel()
                {
                    MaxSpeed = $"{result.FiberSpeed / 1024} MBPS",
                    PortState = RadiusR.Localization.Lists.AvailabilityResult.ResourceManager.GetString((result.FiberPortState).ToString()),
                    SVUID = result.FiberSVUID
                });
            }
            if (result.VdslSpeed == 0 && result.AdslSpeed == 0)
            {
                ViewBag.InfrastructureState = false;
                ModelState.AddModelError("SVUID", RadiusRCustomerWebSite.Localization.Validation.InfrastructureNotFound);
                return View(new AvailabilityResultViewModel());
            }
            return View(new AvailabilityResultViewModel()
            {
                MaxSpeed = result.AdslSpeed > result.VdslSpeed ? $"{result.AdslSpeed / 1024} MBPS" : $"{result.VdslSpeed / 1024} MBPS",
                SVUID = result.AdslSpeed > result.VdslSpeed ? result.AdslSVUID : result.VdslSVUID,
                PortState = result.AdslSpeed > result.VdslSpeed
                ? RadiusR.Localization.Lists.AvailabilityResult.ResourceManager.GetString((result.AdslPortState).ToString())
                : RadiusR.Localization.Lists.AvailabilityResult.ResourceManager.GetString((result.VdslPortState).ToString())
            });
        }
        #region ajax operations

        readonly Random random = new Random();
        [HttpPost]
        public ActionResult CheckCaptcha(string Captcha)
        {
            if (string.IsNullOrEmpty(Captcha) || Captcha.ToLower() != Session["AvailabilityCaptcha"] as string)
            {
                var validate = "<div><span class='field-validation-error'>" + string.Format(RadiusRCustomerWebSite.Localization.Common.NotValid, RadiusRCustomerWebSite.Localization.Common.Captcha) + "</span></div>";
                return Json(new { captchaImg = "<img width='265' height='50' src='" + Url.Action("AvailabilityCaptcha", "Captcha", new { id = random.Next(1, 999999999) }) + "' />" + validate, IsSuccess = false }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { IsSuccess = true }, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}