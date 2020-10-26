using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class HomePageViewModel
    {
        [Display(ResourceType = typeof(RadiusR.Localization.Model.FreeRadius), Name = "ServiceName")]
        public string ServiceName { get; set; }

        public int BillCount { get; set; }

        [UIHint("Currency")]
        public string BillsTotal { get; set; }

        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Download")]
        [UIHint("FormattedBytes")]
        public decimal Download { get; set; }

        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Upload")]
        [UIHint("FormattedBytes")]
        public decimal Upload { get; set; }

        public IEnumerable<ClientUsageInfoViewModel> MonthlyUsage { get; set; }
    }
}