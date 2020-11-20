using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class ClientUsageInfoViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Date")]
        public DateTime Date { get; set; }

        public int? _month { get; set; }

        public int? _year { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "TotalDownload")]
        [UIHint("FormattedBytes")]
        public decimal TotalDownload { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "TotalUpload")]
        [UIHint("FormattedBytes")]
        public decimal TotalUpload { get; set; }
    }
}