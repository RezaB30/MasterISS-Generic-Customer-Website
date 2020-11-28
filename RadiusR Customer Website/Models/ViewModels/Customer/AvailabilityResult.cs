using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models.ViewModels.Customer
{
    public class AvailabilityResult
    {
        public bool IsSuccess { get; set; }
        public int AdslSpeed { get; set; }
        public int VdslSpeed { get; set; }
        public int FiberSpeed { get; set; }
        public string AdslDistance { get; set; }
        public string VdslDistance { get; set; }
        public string FiberDistance { get; set; }
        public RezaB.TurkTelekom.WebServices.Availability.AvailabilityServiceClient.PortState AdslPortState { get; set; }
        public RezaB.TurkTelekom.WebServices.Availability.AvailabilityServiceClient.PortState VdslPortState { get; set; }
        public RezaB.TurkTelekom.WebServices.Availability.AvailabilityServiceClient.PortState FiberPortState { get; set; }
        public string address { get; set; }
        public string AdslSVUID { get; set; }
        public string VdslSVUID { get; set; }
        public string FiberSVUID { get; set; }
        public DateTime? Datetime { get; set; }
        public string QueryKey { get; set; }
        public string IPAddress { get; set; }
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"BBK : {QueryKey} ",
                $"Adsl Speed : {AdslSpeed} ",
                $"Vdsl Speed : {VdslSpeed} ",
                $"Fiber Speed : {FiberSpeed} ",
                $"Adsl Port : {AdslPortState} ",
                $"Vdsl Port : {VdslPortState} ",
                $"Fiber Port : {FiberPortState} "
            });
        }
    }
}