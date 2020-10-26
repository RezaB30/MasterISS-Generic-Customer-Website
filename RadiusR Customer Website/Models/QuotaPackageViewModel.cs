using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadiusR_Customer_Website.Models
{
    public class QuotaPackageViewModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        [UIHint("TrafficLimit")]
        public string Amount { get; set; }

        public decimal? _price { get; set; }

        [UIHint("Currency")]
        public string Price
        {
            get
            {
                return _price.HasValue ? _price.Value.ToString("0.00") : "-";
            }
            set
            {
                decimal parsed;
                if (decimal.TryParse(value, out parsed))
                    _price = parsed;
                else
                    _price = null;
            }
        }

        public long? _amount
        {
            get
            {
                long parsed;
                if (long.TryParse(Amount, out parsed))
                    return parsed;
                return null;
            }
            set
            {
                Amount = value.HasValue ? value.Value.ToString() : null;
            }
        }
    }
}