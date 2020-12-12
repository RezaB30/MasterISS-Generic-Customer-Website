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
            var captchaPair = RezaB.Web.Captcha.CaptchaImageGenerator.Generate(_parameters);
            //var captchaPair = Captcha.Generate();
            Session.Add("captcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
        [AllowAnonymous]
        public ActionResult AvailabilityCaptcha(int id)
        {
            //var captchaPair = Captcha.Generate();
            var captchaPair = RezaB.Web.Captcha.CaptchaImageGenerator.Generate(_parameters);
            Session.Add("AvailabilityCaptcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
        [AllowAnonymous]
        public ActionResult LoginCaptcha()
        {
            //var captchaPair = Captcha.Generate();
            var captchaPair = RezaB.Web.Captcha.CaptchaImageGenerator.Generate(_parameters);
            Session.Add("LoginCaptcha", captchaPair.Key.ToLower());
            var stream = new MemoryStream();
            captchaPair.Image.Save(stream, ImageFormat.Png);
            return File(stream.ToArray(), "image/png");
        }
        private RezaB.Web.Captcha.CaptchaImageParameters _parameters
        {
            get
            {
                return new RezaB.Web.Captcha.CaptchaImageParameters()
                {
                    CharacterCount = 4,
                    Fonts = new System.Drawing.FontFamily[] { new System.Drawing.FontFamily("Arial") },
                    CharacterPallete = "0123456789",
                    FontAlpha = 200,
                    FontSize = 30f,
                    ImageDimentions = new System.Drawing.Size(265, 50),
                    NoisePercentage = 0.1f
                };
            }
        }
    }
}