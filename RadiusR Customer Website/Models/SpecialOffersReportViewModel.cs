using RezaB.Web.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class SpecialOffersReportViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ReferenceNo")]
        public string ReferenceNo { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "StartDate")]
        [UIHint("MonthAndYear")]
        public DateTime StartDate { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "EndDate")]
        [UIHint("MonthAndYear")]
        public DateTime EndDate
        {
            get
            {
                return StartDate.AddMonths(TotalCount);
            }
        }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "UsedCount")]
        public int UsedCount { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "TotalCount")]
        public int TotalCount { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "MissedCount")]
        public int MissedCount { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "RemainingCount")]
        public int RemainingCount { get { return IsCancelled ? 0 : TotalCount - (UsedCount + MissedCount); } }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ThisPeriod")]
        public bool IsApplicableThisPeriod
        {
            get
            {
                return RemainingCount > 0 && !IsCancelled;
            }
        }

        public bool IsCancelled { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "SubscriberState")]
        [EnumType(typeof(Models.Enums.CustomerState), typeof(RadiusR.Localization.Lists.CustomerState))]
        [UIHint("LocalizedList")]
        public short? ReferralSubscriberState { get; set; }

    }
}