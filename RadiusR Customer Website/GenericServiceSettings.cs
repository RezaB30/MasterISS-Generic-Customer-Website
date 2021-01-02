using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace RadiusR_Customer_Website
{
    public class GenericServiceSettings
    {
        public string _culture { get; set; }
        public string _rand { get; set; }
        public string _username { get; set; }
        public GenericServiceSettings()
        {
            _culture = Thread.CurrentThread.CurrentUICulture.Name;
            _rand = Guid.NewGuid().ToString();
            _username = "onur";
        }
        public string hash { get { return HashUtilities.CalculateHash<SHA1>(_username + _rand + HashUtilities.CalculateHash<SHA1>("123456")); } }
    }
    public static class HashUtilities
    {
        public static string CalculateHash<HAT>(string value) where HAT : HashAlgorithm
        {
            HAT algorithm = (HAT)HashAlgorithm.Create(typeof(HAT).Name);
            var calculatedHash = string.Join(string.Empty, algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(b => b.ToString("x2")));
            return calculatedHash;
        }
    }
}