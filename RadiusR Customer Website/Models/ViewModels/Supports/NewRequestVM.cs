﻿using System;
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
        public NewRequestVM()
        {
            SubRequestTypeList = new List<SelectListItem>();
            RequestTypeList = new List<SelectListItem>();
        }
        public int? RequestTypeId { get; set; }
        public IEnumerable<SelectListItem> RequestTypeList { get; set; }
        public int? SubRequestTypeId { get; set; }
        public IEnumerable<SelectListItem> SubRequestTypeList { get; set; }
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