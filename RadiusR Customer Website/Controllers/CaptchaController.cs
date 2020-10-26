using RezaB.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadiusR_Customer_Website.Controllers
{
    public class CaptchaController : Controller
    {
        [AllowAnonymous]
        // GET: Captcha
        public ActionResult Index()
        {
            var captchaPair = Captcha.Generate();
            Session.Add("captcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
    }
}