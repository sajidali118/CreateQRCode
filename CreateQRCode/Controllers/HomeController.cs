using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using CreateQRCode.Models;
using Emgu.CV;
using Emgu.CV.Ocl;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.Mvc;
using Tesseract;
using ZXing;
using ZXing.Common;

namespace CreateQRCode.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detect()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Detect_(IFormFile IdImage)
        {
            if (IdImage == null || IdImage.Length == 0)
            {
                ViewBag.Result = "No file selected.";
                return View();
            }

            var uploadsPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "temp");
            Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, Guid.NewGuid().ToString() + Path.GetExtension(IdImage.FileName));

            await using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await IdImage.CopyToAsync(fs);
            }

            string ocrText;
            try
            {
                // Load both English + Arabic trained data
                using var ocr = new TesseractEngine("./tessdata", "eng+ara", EngineMode.Default);
                using var img = Pix.LoadFromFile(filePath);
                using var page = ocr.Process(img);
                ocrText = page.GetText();
            }
            catch (Exception ex)
            {
                ViewBag.Result = "OCR failed: " + ex.Message;
                return View();
            }

            // Identify side
            string result = DetectCardSide(ocrText);

            ViewBag.Result = result;
            ViewBag.Text = ocrText;

            // Optional: delete temp file
            System.IO.File.Delete(filePath);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Detect(IFormFile FrontFile, IFormFile BackFile)
        {
            if (FrontFile == null || BackFile == null)
            {
                ViewBag.Error = "Please upload both front and back images.";
                return View();
            }

            // Allowed extensions and MIME types
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var allowedMimeTypes = new[] { "image/jpeg", "image/png" };


            // Helper method for validation
            bool IsValid(IFormFile file)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return allowedExtensions.Contains(ext) && allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant());
            }

            if (!IsValid(FrontFile))
            {
                ViewBag.Error = "Front side must be a JPEG or PNG image.";
                return View();
            }

            if (!IsValid(BackFile))
            {
                ViewBag.Error = "Back side must be a JPEG or PNG image.";
                return View();
            }

            var uploadsPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "temp");
            Directory.CreateDirectory(uploadsPath);

            // Save both temporarily
            var frontPath = Path.Combine(uploadsPath, Guid.NewGuid() + Path.GetExtension(FrontFile.FileName));
            var backPath = Path.Combine(uploadsPath, Guid.NewGuid() + Path.GetExtension(BackFile.FileName));

            await using (var fs = new FileStream(frontPath, FileMode.Create))
                await FrontFile.CopyToAsync(fs);
            await using (var fs = new FileStream(backPath, FileMode.Create))
                await BackFile.CopyToAsync(fs);

            // Process both sides
            var frontText = RunOcr(frontPath);
            var backText = RunOcr(backPath);

            //var detectedFront = DetectCardSide(frontText);
            //var detectedBack = DetectCardSide(backText);
            var detectedFront = DetectSide(frontPath, frontText);
            var detectedBack = DetectSide(backPath, backText);

            var name = GetMatch(frontText, @"Name[:\s]+([A-Za-z\s]+)");
            var dob = GetMatch(frontText, @"Date\s*of\s*Birth[:\s]+([\d/]+)");
            var nationality = GetMatch(frontText, @"Nationality[:;\s]+([A-Za-z]+)");
            var issueDate = GetMatch(frontText, @"Issue\s*Date[:\s]+([\d/]+)");
            var expiryDate = GetMatch(frontText, @"Expiry\s*Date[:\s]+([\d/]+)");

            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"Date of Birth: {dob}");
            Console.WriteLine($"Nationality: {nationality}");
            Console.WriteLine($"Issue Date: {issueDate}");
            Console.WriteLine($"Expiry Date: {expiryDate}");
            
            ViewBag.FrontText = $"Name: {name} <br/> Date of Birth: {dob} <br/> Nationality: {nationality} <br/> Issue Date: {issueDate} <br/> Expiry Date: {expiryDate} ";
            ViewBag.FrontTextResult = frontText;
            ViewBag.BackText = backText;
            ViewBag.FrontResult = detectedFront;
            ViewBag.BackResult = detectedBack;

            // optional cleanup
            if(System.IO.File.Exists(frontPath))
                System.IO.File.Delete(frontPath);
            if (System.IO.File.Exists(backPath))
                System.IO.File.Delete(backPath);

            return View();
        }

        private string RunOcr(string imagePath)
        {
            try
            {
                using var engine = new TesseractEngine("./tessdata", "eng+ara", EngineMode.Default);
                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img);
                return page.GetText();
            }
            catch (Exception ex)
            {
                return $"[OCR Error: {ex.Message}]";
            }
        }


        private string DetectCardSide(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Unknown";

            text = text.ToLower();

            // Front side typically has words like “emirates id”, “identity number”, “united arab emirates”
            if (text.Contains("emirates id") ||
                text.Contains("identity number") ||
                text.Contains("united arab emirates"))
            {
                return "Front Side";
            }

            // Back side has “card number”, “date of issue”, barcode numbers, etc.
            if (text.Contains("card number") ||
                text.Contains("issue date") ||
                text.Contains("expiry") ||
                text.Contains("po box") ||
                System.Text.RegularExpressions.Regex.IsMatch(text, @"\b\d{9,}\b"))
            {
                return "Back Side";
            }

            return "Unknown";
        }

        private string DetectSide(string imagePath, string ocrText)
        {
            // --- OCR Heuristics ---
            var text = (ocrText ?? string.Empty).ToLower();
            bool textLooksFront =
                text.Contains("emirates id") ||
                text.Contains("identity number") ||
                text.Contains("united arab emirates");
            bool textLooksBack =
                text.Contains("card number") ||
                text.Contains("issue date") ||
                text.Contains("expiry");

            // --- Face Detection ---
            bool hasFace = false;
            try
            {
                var cascadePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "models", "haarcascade_frontalface_default.xml");
                using var classifier = new CascadeClassifier(cascadePath);
                using var img = new Image<Bgr, byte>(imagePath);
                var gray = img.Convert<Gray, byte>();
                var faces = classifier.DetectMultiScale(gray, 1.1, 4, new System.Drawing.Size(60, 60));
                hasFace = faces.Length > 0;
            }
            catch { /* ignore */ }

            // --- Barcode Detection ---
            bool hasBarcode = false;
            try
            {
                var reader = new BarcodeReader();
                using (var bmp = new System.Drawing.Bitmap(imagePath))
                {
                    var result = reader.Decode(bmp);
                    hasBarcode = result != null;
                } // 👈 file released here

                //var reader = new BarcodeReader()
                //{
                //    AutoRotate = true,
                //    //Options = new DecodingOptions { TryHarder = true }
                //};
                //var bmp = new Bitmap(imagePath);
                //var result = reader.Decode(bmp);
                //hasBarcode = result != null;
            }
            catch { /* ignore */ }

            // --- Combine evidence ---
            if (hasFace || textLooksFront)
                return "Front Side";
            if (hasBarcode || textLooksBack)
                return "Back Side";

            return "Unknown";
        }

        string GetMatch(string ocrText, string pattern)
        {
            var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
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
