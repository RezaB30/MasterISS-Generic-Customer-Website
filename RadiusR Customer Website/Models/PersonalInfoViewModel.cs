using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class PersonalInfoViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ClientName")]
        [UIHint("TextWithPlaceholder")]
        public string ValidDisplayName { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "PhoneNo")]
        [UIHint("TextWithPlaceholder")]
        public string PhoneNo { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Email")]
        [UIHint("TextWithPlaceholder")]
        public string EMail { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "InstallationAddress")]
        [UIHint("TextWithPlaceholder")]
        public string InstallationAddress { get; set; }


        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "InternetUsername")]
        [UIHint("InvisibleText")]
        //[UIHint("TextWithPlaceholder")]
        public string Username { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "InternetPassword")]
        [UIHint("InvisibleText")]
        //[UIHint("TextWithPlaceholder")]
        public string Password { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "TTSubscriberNo")]
        [UIHint("TextWithPlaceholder")]
        public string TTSubscriberNo { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ReferenceNo")]
        public string ReferenceNo { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "PSTN")]
        public string PSTN { get; set; }
    }
}