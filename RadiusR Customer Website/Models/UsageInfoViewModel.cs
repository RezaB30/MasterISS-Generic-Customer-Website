using System;
using System.ComponentModel.DataAnnotations;

namespace RadiusR_Customer_Website.Models
{
    internal class UsageInfoViewModel
    {
        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Date")]
        public DateTime Date { get; set; }

        public int? _month { get; set; }

        public int? _year { get; set; }

        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Download")]
        [UIHint("FormattedBytes")]
        public decimal Download { get; set; }

        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Upload")]
        [UIHint("FormattedBytes")]
        public decimal Upload { get; set; }

        [Display(ResourceType = typeof(RadiusR.Localization.Model.RadiusR), Name = "Total")]
        [UIHint("FormattedBytes")]
        public decimal Total
        {
            get
            {
                return Download + Upload;
            }
        }
    }
}