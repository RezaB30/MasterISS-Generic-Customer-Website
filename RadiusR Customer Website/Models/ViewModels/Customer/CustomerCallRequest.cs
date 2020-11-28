using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models.ViewModels.Customer
{
    public class CustomerCallRequest
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "CustomerName")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string CustomerName { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "PhoneNo")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        [RegularExpression("^(05|5)([0-9]{2})([0-9]{3})([0-9]{2})([0-9]{2})$", ErrorMessageResourceName = "NotValid", ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Common))]
        public string PhoneNo { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Description")]
        //[Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string Description { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Captcha")]
        [Required(ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation), ErrorMessageResourceName = "Required")]
        public string Captcha { get; set; }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"{CustomerName} ",
                $"{PhoneNo} ",
                $"{Description} ",
            });
        }
    }
}