using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Drawing;
using QRCoder;
using QRCodeGenerator = QRCoder.QRCodeGenerator;
using QRCodeData = QRCoder.QRCodeData;

namespace QRTrial.Controllers
{
    public class HomeController(IWebHostEnvironment env) : Controller
    {
        private readonly IWebHostEnvironment _env = env;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadImage(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                // Save uploaded file
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // Generate modified image
                var res = AddQRCodeToImage(filePath, "https://yourwebsite.com/rsvp");

                // Display modified image
                ViewBag.ImagePath = "/uploads/" + "modified_" + uniqueFileName;
                return File(res, "image/png", "modified_image.png");
            }

            return View("Index");
        }

        private static byte[] AddQRCodeToImage(string inputImagePath, string qrText)
        {
            using var inputStream = System.IO.File.OpenRead(inputImagePath);
            using var originalBitmap = SKBitmap.Decode(inputStream);

            // Create SKSurface to draw over
            using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
            var canvas = surface.Canvas;

            // Draw original image onto canvas
            canvas.DrawBitmap(originalBitmap, 0, 0);


            // Generate QR Code with fixed size of 38x38 pixels
            int qrSize = 38;
            using var qrBitmap = GenerateQRCode(qrText);

            // Resize the QR code to 38x38 if necessary
            using var resizedQR = qrBitmap.Resize(new SKImageInfo(qrSize, qrSize), SKFilterQuality.High);

            // Determine bottom-right position
            int x = originalBitmap.Width - qrSize - 20;  // 20 is the margin from the right edge
            int y = originalBitmap.Height - qrSize - 20; // 20 is the margin from the bottom edge



            //// Generate QR Code
            //int qrSize = originalBitmap.Width / 5; // QR Code size is 20% of image width
            //using var qrBitmap = GenerateQRCode(qrText);

            //// Determine bottom-right position
            //int x = originalBitmap.Width - qrSize - 20;
            //int y = originalBitmap.Height - qrSize - 20;

            // Draw QR code on the image
            canvas.DrawBitmap(qrBitmap, x, y);

            // Get the final image
            using var outputImage = surface.Snapshot();
            using var data = outputImage.Encode(SKEncodedImageFormat.Png, 100);
            using var memoryStream = new MemoryStream();
            data.SaveTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream.ToArray();
        }


        private static SKBitmap GenerateQRCode(string text)
        {
            QRCodeGenerator qrGenerator = new();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            using MemoryStream ms1 = new(qrCodeAsPngByteArr);
            Bitmap bitmap = new(ms1);

            using var ms = new MemoryStream();
            // Save the System.Drawing.Bitmap to a memory stream
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            // Create an SKBitmap from the memory stream
            return SKBitmap.Decode(ms);
        }

    }
}
