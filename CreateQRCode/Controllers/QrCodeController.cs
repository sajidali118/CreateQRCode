using System.Diagnostics;
using CreateQRCode.Models;
using CreateQRCode.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreateQRCode.Controllers
{
    public class QrCodeController : Controller
    {
        private readonly ILogger<QrCodeController> _logger;
        private readonly QrCodeService _qrCodeService;


        public QrCodeController(ILogger<QrCodeController> logger, QrCodeService qrCodeService)
        {
            _logger = logger;
            _qrCodeService = qrCodeService;
        }

        public IActionResult Generate()
        {
            CreateQRCodelVM create =new CreateQRCodelVM();
            return View(create);
        }
        [HttpPost]
        public IActionResult Generate(CreateQRCodelVM create)
        {
            if (string.IsNullOrWhiteSpace(create.URL))
                return BadRequest("URL is required.");

            if (string.IsNullOrWhiteSpace(create.FileName))
                return BadRequest("File Name is required.");

            var qrCodeImage = _qrCodeService.GenerateQrCode(create.URL, create.FileName);
            if (qrCodeImage != null)
            {
                var filePathPhysical = $"{qrCodeImage}";

                create.QrCodeImage = filePathPhysical;
            }
            return View(create);
        }

        public IActionResult GenerateWiFi()
        {
            WifiQrModel create = new WifiQrModel();
            return View(create);
        }

        [HttpPost]
        public IActionResult GenerateWiFi(WifiQrModel create)
        {
            if (string.IsNullOrWhiteSpace(create.SSID))
                return BadRequest("SSID is required.");

            if (string.IsNullOrWhiteSpace(create.Password))
                return BadRequest("Password is required.");

            var qrCodeImage = _qrCodeService.GenerateWifiQrCodeAndSave(create);
            if (qrCodeImage != null)
            {
                var filePathPhysical = $"{qrCodeImage}";

                create.QrCodePath = filePathPhysical;
            }
            return View(create);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
