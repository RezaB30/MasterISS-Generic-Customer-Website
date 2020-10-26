using RadiusR_Customer_Website.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class CardPaymentViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "CardNumber")]
        [Required(ErrorMessage ="*")]
        [CardNumber(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string CardNumber { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ExpirationDateYear")]
        [Required(ErrorMessage ="*")]
        [CardExpirationYear(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string ExpirationDateYear { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ExpirationDateMonth")]
        [Required(ErrorMessage = "*")]
        [CardExpirationMonth(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string ExpirationDateMonth { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "CVV")]
        [Required(ErrorMessage ="*")]
        [CardCVV(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "NotValid")]
        public string CVV { get; set; }
    }
}