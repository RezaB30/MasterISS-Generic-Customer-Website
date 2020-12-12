using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusR.API.AddressQueryAdapter
{
    public static class AddressAPISettings
    {
        private static string addressAPIUsername;

        public static string AddressAPIUsername
        {
            get
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    addressAPIUsername = db.AppSettings.Where(m => m.Key == "AddressAPIUsername").Select(m => m.Value).FirstOrDefault() ?? string.Empty;
                    return addressAPIUsername;
                }

            }
        }
        private static string addressAPIPassword;

        public static string AddressAPIPassword
        {
            get
            {
                using (var db = new RadiusR.DB.RadiusREntities())
                {
                    addressAPIPassword = db.AppSettings.Where(m => m.Key == "AddressAPIPassword").Select(m => m.Value).FirstOrDefault() ?? string.Empty;
                    return addressAPIPassword;
                }

            }
        }
    }
}
