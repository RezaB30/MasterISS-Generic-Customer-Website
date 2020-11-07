using RadiusR_Customer_Website.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class AutoPaymentCardViewModel
    {

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "CardholderName")]
        [Required(ErrorMessage = "*")]
        [RegularExpression(@"^([\p{L}|\.]+\s[\p{L}|\.]+)+$", ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        [MaxLength(150, ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "MaxLength")]
        public string CardholderName { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "CardNumber")]
        [Required(ErrorMessage = "*")]
        [CardNumber(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string CardNo { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ExpirationDateMonth")]
        [Required(ErrorMessage = "*")]
        [CardExpirationMonth(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string ExpirationMonth { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ExpirationDateYear")]
        [Required(ErrorMessage = "*")]
        [CardExpirationYear(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string ExpirationYear { get; set; }
    }
}