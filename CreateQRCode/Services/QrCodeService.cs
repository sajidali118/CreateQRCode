using CreateQRCode.Models;
using QRCoder;
using System.Drawing.Imaging;
using static QRCoder.PayloadGenerator.WiFi;

namespace CreateQRCode.Services
{
    public class QrCodeService
    {
        private readonly IWebHostEnvironment _env;

        public QrCodeService(IWebHostEnvironment env)
        {
            _env = env;
        }
        public string GenerateQrCode(string requestURL, string fileName)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {

                // Create wwwroot/qrcodes if it doesn't exist
                string folderPath = Path.Combine(_env.WebRootPath, "qrcodes");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Unique file name
                fileName = $"qr_{fileName}.png";
                string filePath = Path.Combine(folderPath, fileName);


                using (var qrCodeData = qrGenerator.CreateQrCode(requestURL, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20);
                        using var stream = new MemoryStream();

                        System.IO.File.WriteAllBytesAsync(filePath, qrCodeImage);
                        if (qrCodeImage != null)
                        {
                            return $"/qrcodes/{fileName}";
                        }
                    }
                }
            }
            return "";
        }

        public string GenerateWifiQrCodeAndSave(WifiQrModel wifiQr)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                // Create wwwroot/qrcodes if it doesn't exist
                string folderPath = Path.Combine(_env.WebRootPath, "qrcodes");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Unique file name
                var fileName = $"qr_wifi.png";
                string filePath = Path.Combine(folderPath, fileName);

                // Generate WiFi QR code payload
                var authentication = Authentication.nopass;

                if (wifiQr.AuthType=="WPA2")
                {
                    authentication = Authentication.WPA2;
                }
                else if (wifiQr.AuthType=="WEP")
                {
                    authentication = Authentication.WEP;
                }
                else if (wifiQr.AuthType=="WPA")
                {
                    authentication =Authentication.WPA;
                }

                var wifiPayload = new PayloadGenerator.WiFi(wifiQr.SSID, wifiQr.Password, authentication, wifiQr.Hidden);

                string payload = wifiPayload.ToString();

                using (var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20);
                        using var stream = new MemoryStream();

                        File.WriteAllBytesAsync(filePath, qrCodeImage);
                        if (qrCodeImage != null)
                        {
                            return $"/qrcodes/{fileName}";
                        }
                    }
                }

                return "";
            }
        }
    }
}
