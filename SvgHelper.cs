using System.Drawing;
using System.IO;
using Svg; // Install the Svg package via NuGet

namespace LabelEditorApp
{
    public static class SvgHelper
    {
        public static Bitmap LoadSvg(string filePath, Size size)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("SVG file not found", filePath);

            using (var svgDoc = SvgDocument.Open(filePath))
            {
                svgDoc.ShapeRendering = SvgShapeRendering.GeometricPrecision;

                // Create a bitmap with the desired size.
                Bitmap bmp = new Bitmap(size.Width, size.Height);

                // Draw the SVG document onto the bitmap using a Graphics object.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Optionally, set a white background.
                    g.Clear(Color.White);
                    svgDoc.Draw(g);
                }

                return bmp;
            }
        }
    }
}
