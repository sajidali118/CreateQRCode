using QRCoder;

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
                // var url = $"{requestURL.TrimEnd('/')}/Home/{liceNumber}/TourGuideProfile?id={Guid.NewGuid()}";

                string filePathPhysical = $"/Content/Files/{fileName}-QR.png";

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
    }
}
