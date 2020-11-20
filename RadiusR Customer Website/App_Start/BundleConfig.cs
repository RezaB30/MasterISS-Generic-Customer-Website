using System.Web;
using System.Web.Optimization;

namespace RadiusR_Customer_Website
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/Custom/captcha.js",
                        "~/Scripts/Custom/header-popups.js",
                        "~/Scripts/Custom/confirm-box.js",
                        "~/Scripts/Custom/initialize-ui.js",
                        "~/Scripts/Custom/menu-zip-button.js",
                        "~/Scripts/Custom/notification.js"
                        ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/css/main.css",
                "~/Content/css/captcha.css",
                "~/Content/css/data-table.css",
                "~/Content/css/media.css"
                ));

            bundles.Add(new StyleBundle("~/Content/css/login").Include(
                "~/Content/css/main.css",
                "~/Content/css/login.css",
                "~/Content/css/captcha.css",
                "~/Content/css/media.css"
                ));
        }
    }
}
