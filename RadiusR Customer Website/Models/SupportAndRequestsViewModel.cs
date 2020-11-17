using RezaB.Web.CustomAttributes;
using RadiusR.DB.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class SupportAndRequestsViewModel
    {
        public long ID { get; set; }

        public long ClientID { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Message")]
        [MaxLength(250, ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "MaxLength")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string Message { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "State")]
        [EnumType(typeof(SupportRequestStateID), typeof(RadiusR.Localization.Lists.SubscriptionSupportRequestStateID))]
        [UIHint("LocalizedList")]
        public short StateID { get; set; }

        public long? TaskID { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Date")]
        public DateTime Date { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Details")]
        public string AdminResponse { get; set; }
    }
}