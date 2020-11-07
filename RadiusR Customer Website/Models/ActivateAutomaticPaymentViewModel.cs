using RadiusR.DB.Enums;
using RezaB.Web.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class ActivateAutomaticPaymentViewModel
    {
        [Required]
        public string CardToken { get; set; }

        [Required]
        public long SubscriptionID { get; set; }

        [Required]
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "PaymentType")]
        [EnumType(typeof(AutoPaymentType), typeof(RadiusR.DB.Enums.AutoPaymentType))]
        [UIHint("LocalizedList")]
        public short PaymentType { get; set; }
    }
}