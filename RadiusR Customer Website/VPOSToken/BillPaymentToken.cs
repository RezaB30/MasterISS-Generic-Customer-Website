using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.VPOSToken
{
    public class BillPaymentToken : PaymentTokenBase
    {
        public long? BillID { get; set; }
    }
}