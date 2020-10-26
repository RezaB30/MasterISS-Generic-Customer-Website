using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class CustomerAutomaticPaymentViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Cards")]
        public IEnumerable<CardViewModel> Cards { get; set; }

        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "AutomaticPaymentSubscriptions")]
        public IEnumerable<AutomaticPaymentViewModel> AutomaticPayments { get; set; }

        public class CardViewModel
        {
            [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "MaskedCardNo")]
            public string MaskedCardNo { get; set; }

            public string Token { get; set; }

            public bool HasAutoPayments { get; set; }
        }

        public class AutomaticPaymentViewModel
        {
            [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "SubscriberNo")]
            public string SubscriberNo { get; set; }

            public long SubscriberID { get; set; }

            public CardViewModel Card { get; set; }
        }
    }
}