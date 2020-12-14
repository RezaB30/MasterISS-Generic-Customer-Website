using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Models.ViewModels.Supports
{
    public class NewRequestVM
    {
        //public NewRequestVM()
        //{
        //    SubRequestTypeList = new List<SelectListItem>();
        //    RequestTypeList = new List<SelectListItem>();
        //}
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportRequestType")]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation))]
        public int? RequestTypeId { get; set; }
        //public IEnumerable<SelectListItem> RequestTypeList { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Models.Model), Name = "SupportRequestSubType")]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation))]
        public int? SubRequestTypeId { get; set; }
        //public IEnumerable<SelectListItem> SubRequestTypeList { get; set; }
        [Display(ResourceType = typeof(RadiusRCustomerWebSite.Localization.Common), Name = "Message")]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(RadiusRCustomerWebSite.Localization.Validation))]
        public string Description { get; set; }
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new string[]
            {
                $"RequestTypeId : {RequestTypeId}",
                $"SubRequestTypeId : {SubRequestTypeId}",
                $"Description : {Description}"
            });
        }
    }
}