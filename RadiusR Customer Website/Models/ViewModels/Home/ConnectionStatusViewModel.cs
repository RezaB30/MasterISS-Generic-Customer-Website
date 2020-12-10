using RezaB.Web.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models.ViewModels.Home
{
    public class ConnectionStatusViewModel
    {
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "TTSubscriberNo")]
        public string XDSLNo { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "ConnectionType")]
        [EnumType(typeof(RezaB.TurkTelekom.WebServices.XDSLType), typeof(RadiusR.Localization.Lists.XDSLType))]
        [UIHint("LocalizedList")]
        public short XDSLType { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "DownloadSpeed")]
        [UIHint("TextWithPlaceholder")]
        public string CurrentDownload { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "UploadSpeed")]
        [UIHint("TextWithPlaceholder")]
        public string CurrentUpload { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "ConnectionStatus")]
        [EnumType(typeof(RezaB.TurkTelekom.WebServices.TTOYS.TTOYSServiceClient.OperationStatus), typeof(RadiusR.Localization.Lists.TTLineState))]
        [UIHint("LocalizedList")]
        public short ConnectionStatus { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "IPAddress")]
        public string IPAddress => Utilities.InternalUtilities.GetUserIP();

    }
}