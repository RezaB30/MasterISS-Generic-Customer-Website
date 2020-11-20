using RezaB.Web.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusR_Customer_Website.Models.ViewModels.Supports
{
    public class SupportMessagesVM
    {
        public long ID { get; set; }
        public string SupportRequestName { get; set; } //  ex: fatura
        public string SupportRequestSummary { get; set; } // ex: Faturamı ödeyemiyorum
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportNo")]
        public string SupportNo { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportDate")]
        public DateTime SupportDate { get; set; }
        public DateTime? CustomerApprovalDate { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "State")]
        [EnumType(typeof(RadiusR.DB.Enums.SupportRequests.SupportRequestStateID), typeof(RadiusR.Localization.Lists.SupportRequests.SupportRequestStateID))]
        [UIHint("LocalizedList")]
        public short State { get; set; }
        public IEnumerable<SupportMessageList> SupportMessageList { get; set; }
        public string Message { get; set; }
        public SupportRequestDisplayTypes SupportDisplayType { get; set; }
    }
    public class SupportMessageList
    {
        public string Message { get; set; }
        [UIHint("DateTimeHour")]
        public DateTime MessageDate { get; set; }
        public string SenderName { get; set; } // agent or customer
        public bool IsCustomer { get; set; }
    }
    //public class SupportFileList
    //{
    //    public string FileUrl { get; set; }
    //    public string FileName { get; set; }
    //}
}
