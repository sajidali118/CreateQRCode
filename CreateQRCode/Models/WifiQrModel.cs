using CreateQRCode.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CreateQRCode.Models
{
    public class WifiQrModel 
    {
        
        [BindProperty]
        public string SSID { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string AuthType { get; set; } = "WPA";

        [BindProperty]
        public bool Hidden { get; set; }

        public string QrCodePath { get; set; }

       
    }
}
