using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Models.ViewModels.Customer
{
    public class InfrastructureBBK
    {
        public InfrastructureBBK()
        {
            Province = new List<SelectListItem>();
            District = new List<SelectListItem>();
            Region = new List<SelectListItem>();
            Neighborhood = new List<SelectListItem>();
            Street = new List<SelectListItem>();
            Building = new List<SelectListItem>();
            Apartment = new List<SelectListItem>();
        }
        public long provinceId { get; set; }
        public IEnumerable<SelectListItem> Province { get; set; }
        public long districtId { get; set; }
        public IEnumerable<SelectListItem> District { get; set; }
        public long regionId { get; set; }
        public IEnumerable<SelectListItem> Region { get; set; }
        public long neighborhoodId { get; set; }
        public IEnumerable<SelectListItem> Neighborhood { get; set; }
        public long streetId { get; set; }
        public IEnumerable<SelectListItem> Street { get; set; }
        public long buildingId { get; set; }
        public IEnumerable<SelectListItem> Building { get; set; }
        public long apartmentId { get; set; }
        public IEnumerable<SelectListItem> Apartment { get; set; }
        public long BBK { get; set; }
        public string Captcha { get; set; }
    }
}