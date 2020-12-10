using RezaB.Web.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusR_Customer_Website.Models.ViewModels.Supports
{
    public class SupportRequestsVM
    {
        public long ID { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportRequestType")]
        [UIHint("TextWithPlaceholder")]
        public string SupportRequestType { get; set; } //  ex: fatura
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportRequestSubType")]
        [UIHint("TextWithPlaceholder")]
        public string SupportRequestSubType { get; set; } // ex: Faturamı ödeyemiyorum
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportNo")]
        public string SupportNo { get; set; } // can be id
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd.MM.yyyy}")]
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "Date")]        
        public DateTime Date { get; set; } // dd.MM.yyyy
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "ApprovalDate")]
        public DateTime? ApprovalDate { get; set; } // dd.MM.yyyy completed date
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "State")]
        [EnumType(typeof(RadiusR.DB.Enums.SupportRequests.SupportRequestStateID), typeof(RadiusR.Localization.Lists.SupportRequests.SupportRequestStateID))]
        [UIHint("LocalizedList")]
        public short State { get; set; }
    }
}
