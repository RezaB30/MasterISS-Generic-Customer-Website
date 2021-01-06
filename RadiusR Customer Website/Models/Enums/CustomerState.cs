using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models.Enums
{
    public enum CustomerState
    {
        Registered = 1,
        Reserved = 2,
        Active = 3,
        Disabled = 4,
        Cancelled = 5,
        PreRegisterd = 6,
    }
}