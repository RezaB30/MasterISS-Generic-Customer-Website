using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models.ViewModels.Customer
{
    public class AvailabilityResultViewModel
    {
        [Display(Name = "PortState", ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common))]
        public string PortState { get; set; }
        [Display(Name = "AvailabilityMaxSpeed", ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common))]
        public string MaxSpeed { get; set; }
        [Display(Name = "SVUID", ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common))]
        public string SVUID { get; set; }
        public bool IsSuccess { get; set; }
    }
}