using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelEditorApp
{
    public static class QRCodeHelper
    {
        public static Image GenerateQRCode(string text, Size size)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            // Resize the QR code to match the desired size.
            Bitmap resized = new Bitmap(qrCodeImage, size);
            return resized;
        }
    }
}
