using LabelEditorApp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace LabelEditorApp
{
    public partial class PrintPreviewForm : Form
    {
        private PictureBox pictureBox;
        private Button btnPrint;
        private List<CanvasObject> objects;
        private Dictionary<string, string> variables;
        private Size canvasSize;
        private Bitmap previewImage;
        private PrintDocument printDoc;

        public PrintPreviewForm(List<CanvasObject> canvasObjects, Dictionary<string, string> variables, Size canvasSize)
        {
            this.objects = canvasObjects;
            this.variables = variables;
            this.canvasSize = canvasSize;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Print Preview";
            this.Width = 850;
            this.Height = 700;
            this.StartPosition = FormStartPosition.CenterScreen;

            // PictureBox to display the preview.
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            this.Controls.Add(pictureBox);

            // Print button at the bottom.
            btnPrint = new Button
            {
                Text = "Print",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            btnPrint.Click += BtnPrint_Click;
            this.Controls.Add(btnPrint);

            this.Load += PrintPreviewForm_Load;

            // Initialize the PrintDocument object.
            printDoc = new PrintDocument();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void PrintPreviewForm_Load(object sender, EventArgs e)
        {
            // Create a preview image that uses the same canvas dimensions.
            previewImage = new Bitmap(canvasSize.Width, canvasSize.Height);
            using (Graphics g = Graphics.FromImage(previewImage))
            {
                g.Clear(Color.White);
                foreach (var obj in objects)
                {
                    if (obj is TextCanvasObject textObj)
                    {
                        // Get the center of the object's bounds
                        PointF center = new PointF(
                            textObj.Bounds.X + textObj.Bounds.Width / 2f,
                            textObj.Bounds.Y + textObj.Bounds.Height / 2f);

                        // Save the current graphics state
                        GraphicsState state = g.Save();

                        // Translate the coordinate system to the center of the text object
                        g.TranslateTransform(center.X, center.Y);

                        // Rotate using the text object's rotation (in degrees)
                        g.RotateTransform(textObj.Rotation);

                        // Since the rotation is applied around the center, draw the text with an offset so that it is centered.
                        // For this, we draw the string at (-width/2, -height/2). You can adjust these offsets as needed.
                        PointF drawPoint = new PointF(-textObj.Bounds.Width / 2f, -textObj.Bounds.Height / 2f);
                        // Replace variable placeholders.
                        string replacedText = textObj.Text;
                        foreach (var pair in variables)
                        {
                            replacedText = replacedText.Replace($"[{pair.Key}]", pair.Value);
                        }
                        using (Brush brush = new SolidBrush(textObj.ForeColor))
                        {
                            g.DrawString(replacedText, textObj.Font, brush, drawPoint);
                        }
                        // Restore the graphics state so that the transformations do not affect subsequent drawing
                        g.Restore(state);
                    }
                    else
                    {
                        // For all other objects (images, QR codes, shapes, etc.), use their Draw method.
                        obj.Draw(g);
                    }
                }
            }
            pictureBox.Image = previewImage;
        }

        // Handle the Print button click event.
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            using (PrintDialog printDlg = new PrintDialog())
            {
                printDlg.Document = printDoc;
                if (printDlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        printDoc.Print();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error printing: " + ex.Message);
                    }
                }
            }
        }

        // This event draws the preview image on the printed page.
        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Option: You could scale the image to fit the page bounds.
            // Here we assume we want to print at the actual canvas size.
            e.Graphics.DrawImage(previewImage, 0, 0, previewImage.Width, previewImage.Height);
        }
    }
}