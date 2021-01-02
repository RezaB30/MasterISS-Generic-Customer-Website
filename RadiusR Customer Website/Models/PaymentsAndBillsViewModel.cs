using RadiusR.DB.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class PaymentsAndBillsViewModel
    {
        public long ID { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "ServiceName")]
        public string ServiceName { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "IssueDate")]
        public DateTime BillDate { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "LastPaymentDate")]
        public DateTime LastPaymentDate { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Total")]
        [UIHint("Currency")]
        public string Total { get; set; }

        //public BillState Status { get; set; }
        public short Status { get; set; }

        public bool CanBePaid { get; set; }

        public bool HasEArchiveBill { get; set; }
    }
}