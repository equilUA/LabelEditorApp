using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelEditorApp
{
    [Serializable]
    public abstract class CanvasObject
    {
        // Basic properties: position, size, and additional styling.
        public Rectangle Bounds { get; set; }
        public float Rotation { get; set; }  // In degrees.
        public float Opacity { get; set; } = 1.0f;
        public Color StrokeColor { get; set; } = Color.Black;
        public Color FillColor { get; set; } = Color.Transparent;

        // For layering; a more complete design might use a dedicated Z-index.
        public int ZIndex { get; set; }

        // Wrap drawing inside a transformation (translation and rotation).
        public void DrawTransformed(Graphics g, Action<Graphics> drawAction)
        {
            GraphicsState state = g.Save();
            // Move the origin to the center of the object's bounds.
            g.TranslateTransform(Bounds.X + Bounds.Width / 2f, Bounds.Y + Bounds.Height / 2f);
            g.RotateTransform(Rotation);
            g.TranslateTransform(-Bounds.Width / 2f, -Bounds.Height / 2f);
            drawAction(g);
            g.Restore(state);
        }

        public abstract void Draw(Graphics g);
    }

    [Serializable]
    public class TextCanvasObject : CanvasObject
    {
        public string Text { get; set; }
        public Font Font { get; set; } = new Font("Arial", 12);
        public Color ForeColor { get; set; } = Color.Black;

        public override void Draw(Graphics g)
        {
            DrawTransformed(g, graphics =>
            {
                using (Brush brush = new SolidBrush(Color.FromArgb((int)(Opacity * 255), ForeColor)))
                {
                    graphics.DrawString(Text, Font, brush, new PointF(0, 0));
                }
            });
        }
    }

    [Serializable]
    public class ImageCanvasObject : CanvasObject
    {
        public string ImagePath { get; set; }

        [NonSerialized]
        private Image _image;
        // We provide a setter also if you want to cache the image.

        [JsonIgnore]
        public Image Image
        {
            get
            {
                if (_image == null && !string.IsNullOrEmpty(ImagePath))
                {
                    _image = Image.FromFile(ImagePath);
                }
                return _image;
            }
            set { _image = value; }
        }

        public override void Draw(Graphics g)
        {
            if (Image != null)
            {
                // Determine the center of the image.
                PointF center = new PointF(Bounds.X + Bounds.Width / 2f,
                                           Bounds.Y + Bounds.Height / 2f);
                // Save the current graphics state.
                GraphicsState state = g.Save();

                // Translate the origin to the center of the object.
                g.TranslateTransform(center.X, center.Y);
                // Rotate by the object's Rotation property (in degrees).
                g.RotateTransform(Rotation);

                // Draw the image centered at the origin.
                g.DrawImage(Image, -Bounds.Width / 2, -Bounds.Height / 2, Bounds.Width, Bounds.Height);

                // Restore the graphics state.
                g.Restore(state);
            }
        }
    }

    [Serializable]
    public class QRCodeCanvasObject : CanvasObject
    {
        public string Data { get; set; }
        [NonSerialized]
        private Image _qrImage;
        public Image QRImage
        {
            get
            {
                if (_qrImage == null && !string.IsNullOrEmpty(Data))
                    _qrImage = QRCodeHelper.GenerateQRCode(Data, new Size(Bounds.Width, Bounds.Height));
                return _qrImage;
            }
        }

        public override void Draw(Graphics g)
        {
            DrawTransformed(g, graphics =>
            {
                if (QRImage != null)
                    graphics.DrawImage(QRImage, new Rectangle(0, 0, Bounds.Width, Bounds.Height));
            });
        }
    }

    [Serializable]
    public class ShapeCanvasObject : CanvasObject
    {
        // Path to the source SVG.
        public string SvgPath { get; set; }
        // For native vector export.
        [NonSerialized]
        public Svg.SvgDocument SvgDocument;
        // Bitmap snapshot for display purposes.
        [NonSerialized]
        public Image BackgroundImage;

        public override void Draw(Graphics g)
        {
            DrawTransformed(g, graphics =>
            {
                if (BackgroundImage != null)
                    graphics.DrawImage(BackgroundImage, new Rectangle(0, 0, Bounds.Width, Bounds.Height));
            });
        }
    }
}
