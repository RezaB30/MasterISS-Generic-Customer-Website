using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusR_Customer_Website.Models.ViewModels.Supports
{
    public class SupportTypes
    {

    }
    //public enum ChangeType  // son değişikliği yapan
    //{
    //    Customer = 1,
    //    Agent = 2
    //}
    //public enum SupportState
    //{
    //    Processing = 1,
    //    Completed = 2
    //}
    //public enum SupportMessageSenderType
    //{
    //    Customer = 1,
    //    Agent = 2
    //}
    //public enum RequestTypes
    //{
    //    Transfer = 1,
    //    Fault = 2,
    //    Invoice = 3,
    //    Tariff = 4,
    //    Freeze = 5,
    //    StaticIP = 6,
    //    Others = 7
    //}
    public enum SupportRequestDisplayTypes
    {
        NoneDisplay = 1,
        OpenRequestAgainDisplay = 2,
        AddNoteDisplay = 3
    }
}
