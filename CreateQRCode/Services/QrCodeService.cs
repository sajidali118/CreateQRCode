using CreateQRCode.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public string GenerateQrCodeWithLogo__(string requestURL, string fileName)
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
                        // Get QR code as byte array
                        byte[] qrCodeImage = qrCode.GetGraphic(20);

                        using (var ms = new MemoryStream(qrCodeImage))
                        using (var tempBitmap = new Bitmap(ms))
                        {
                            // ✅ Clone QR to non-indexed format (so we can draw on it)
                            using (var qrBitmap = new Bitmap(tempBitmap.Width, tempBitmap.Height, PixelFormat.Format32bppArgb))
                            {
                                using (var g = Graphics.FromImage(qrBitmap))
                                {
                                    g.DrawImage(tempBitmap, 0, 0);
                                }

                                // ✅ Load and draw logo if available
                                string logoPath = Path.Combine(_env.WebRootPath, "images", "logo-white.png");
                                if (System.IO.File.Exists(logoPath))
                                {
                                    using (var logo = new Bitmap(logoPath))
                                    {
                                        int logoSize = qrBitmap.Width / 5; // 20% of QR width
                                        using (var resizedLogo = new Bitmap(logo, new Size(logoSize, logoSize)))
                                        using (var g = Graphics.FromImage(qrBitmap))
                                        {
                                            int x = (qrBitmap.Width - resizedLogo.Width) / 2;
                                            int y = (qrBitmap.Height - resizedLogo.Height) / 2;
                                            g.DrawImage(resizedLogo, x, y, resizedLogo.Width, resizedLogo.Height);
                                        }
                                    }
                                }

                                // ✅ Save final QR with logo
                                qrBitmap.Save(filePath, ImageFormat.Png);
                            }
                        }

                        return $"/qrcodes/{fileName}";
                    }
                }
            }
        }
        public string GenerateQrCodeWithLogo(string requestURL, string fileName)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                // Ensure wwwroot/qrcodes exists
                string folderPath = Path.Combine(_env.WebRootPath, "qrcodes");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                fileName = $"qr_{fileName}.png";
                string filePath = Path.Combine(folderPath, fileName);

                using (var qrCodeData = qrGenerator.CreateQrCode(requestURL, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        // Generate QR bytes
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);

                        using (var ms = new MemoryStream(qrCodeBytes))
                        using (var tempBitmap = new Bitmap(ms))
                        {
                            // Clone into editable format
                            using (var qrBitmap = new Bitmap(tempBitmap.Width, tempBitmap.Height, PixelFormat.Format32bppArgb))
                            using (var g = Graphics.FromImage(qrBitmap))
                            {
                                g.DrawImage(tempBitmap, 0, 0, tempBitmap.Width, tempBitmap.Height);

                                string logoPath = Path.Combine(_env.WebRootPath, "images", "logo-black.png");
                                if (File.Exists(logoPath))
                                {
                                    using (var logo = new Bitmap(logoPath))
                                    {
                                        int logoSize = qrBitmap.Width / 5; // 20% of QR width
                                        int x = (qrBitmap.Width - logoSize) / 2;
                                        int y = (qrBitmap.Height - logoSize) / 2;

                                        // ✅ Draw rectangular white background
                                        int padding = 10; // padding around logo box
                                        Rectangle rect = new Rectangle(x - padding, y - padding, logoSize + (padding * 2), logoSize + (padding * 2));

                                        // Optional: rounded corners
                                        using (GraphicsPath path = new GraphicsPath())
                                        {
                                            int radius = 15;
                                            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                                            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                                            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                                            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                                            path.CloseAllFigures();

                                            using (SolidBrush brush = new SolidBrush(Color.White))
                                            {
                                               // g.FillPath(brush, path);
                                                g.FillRectangle(Brushes.White, rect);

                                            }
                                        }

                                        // ✅ Draw logo
                                        using (var resizedLogo = new Bitmap(logo, new Size(logoSize, logoSize)))
                                        {
                                            g.CompositingQuality = CompositingQuality.HighQuality;
                                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                            g.SmoothingMode = SmoothingMode.AntiAlias;

                                            g.DrawImage(resizedLogo, x, y, logoSize, logoSize);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"⚠️ Logo not found at: {logoPath}");
                                }

                                // Save final QR with logo box
                                qrBitmap.Save(filePath, ImageFormat.Png);
                            }
                        }

                        return $"/qrcodes/{fileName}";
                    }
                }
            }
        }
        public string GenerateQrCodeWithLogo_(string requestURL, string fileName)
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
                        // Get QR code as byte array
                        byte[] qrCodeImage = qrCode.GetGraphic(20);
                        using (var ms = new MemoryStream(qrCodeImage))
                        using (var qrBitmap = new Bitmap(ms))
                        {
                            // ✅ Load and draw logo if available
                            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo-black.png");
                            if (System.IO.File.Exists(logoPath))
                            {
                                using (var logo = new Bitmap(logoPath))
                                {
                                    int logoSize = qrBitmap.Width / 5; // 20% of QR width
                                    using (var resizedLogo = new Bitmap(logo, new Size(logoSize, logoSize)))
                                    using (Graphics g = Graphics.FromImage(qrBitmap))
                                    {
                                        // Calculate centered logo position
                                        int x = (qrBitmap.Width - resizedLogo.Width) / 2;
                                        int y = (qrBitmap.Height - resizedLogo.Height) / 2;
                                        g.DrawImage(resizedLogo, x, y, resizedLogo.Width, resizedLogo.Height);
                                    }
                                }
                            }

                            // ✅ Save final image
                            qrBitmap.Save(filePath, ImageFormat.Png);
                        }

                        return $"/qrcodes/{fileName}";
                    }
                }
            }
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

                string wifiPayload = $"WIFI:T:{wifiQr.AuthType};S:{wifiQr.SSID};P:{wifiQr.Password};;";

                //var wifiPayload = new PayloadGenerator.WiFi(wifiQr.SSID, wifiQr.Password, authentication, wifiQr.Hidden);

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
