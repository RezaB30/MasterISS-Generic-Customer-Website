using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.VPOSToken
{
    public abstract class PaymentTokenBase
    {
        public long SubscriberId { get; set; }
    }
}