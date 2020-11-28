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
            //var captchaPair = RezaB.Web.Captcha.CaptchaImageGenerator.Generate(new RezaB.Web.Captcha.CaptchaImageParameters()
            //{
            //    CharacterCount = 6,
            //    Fonts = new System.Drawing.FontFamily[] { new System.Drawing.FontFamily("Arial") },
            //    CharacterPallete = "0123456789",
            //    FontAlpha = 200,
            //    FontSize = 24f,
            //    ImageDimentions = new System.Drawing.Size(200, 100),
            //    NoisePercentage = 0.25f
            //});
            var captchaPair = Captcha.Generate();
            Session.Add("captcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
        [AllowAnonymous]
        public ActionResult AvailabilityCaptcha(int id)
        {
            var captchaPair = Captcha.Generate();
            Session.Add("AvailabilityCaptcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
    }
}