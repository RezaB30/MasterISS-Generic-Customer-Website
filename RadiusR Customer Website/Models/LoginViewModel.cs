using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class LoginViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "LoginCustomerCode")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string CustomerCode { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "LoginSMSPassword")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), ErrorMessageResourceName = "SMSPasswordWrong")]
        public string SMSPassword { get; set; }

        //[Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Captcha")]
        //[Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        //public string Captcha { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Password")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string Password { get; set; }
    }
}