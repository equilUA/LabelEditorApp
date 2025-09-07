using QRCoder;
using System.Drawing;

namespace LabelEditorApp
{
    public static class QRCodeHelper
    {
        public static Image GenerateQRCode(string text, Size size)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            using (var qrCodeImage = qrCode.GetGraphic(20))
            {
                // Resize the QR code to match the desired size.
                return new Bitmap(qrCodeImage, size);
            }
        }
    }
}
