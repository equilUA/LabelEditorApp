using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json; // Make sure you have Newtonsoft.Json installed
using Newtonsoft.Json.Linq;

namespace LabelEditorApp
{
    public enum HandleType
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        Rotation
    }

    public partial class LabelEditor : Form
    {
        // Canvas objects and undo/redo stacks.
        private List<CanvasObject> canvasObjects = new List<CanvasObject>();
        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();

        // Top panel for Undo/Redo.
        private Button btnUndo;
        private Button btnRedo;

        // Left TabControl for options.
        private TabControl tabOptions;
        private TabPage tabText;
        private TabPage tabImage;
        private TabPage tabShape;
        private TabPage tabQRCode;
        private TabPage tabDocument;

        // --- Text Tab Controls ---
        private Button btnAddCustomText;
        private Label lblEditText;
        private TextBox txtEditText;
        private ComboBox cmbFontFamily;
        private NumericUpDown numFontSize;
        private Button btnTextColor;
        private CheckBox chkBold;
        private CheckBox chkItalic;
        private TextBox inlineEditTextBox = null;

        // --- Document Tab Controls (for canvas sizing and design save/load) ---
        private Button btnSaveDesign;
        private Button btnLoadDesign;
        private Label lblCanvasWidth;
        private Label lblCanvasHeight;
        private NumericUpDown numCanvasWidth;
        private NumericUpDown numCanvasHeight;
        private Button btnApplyCanvasSizeDocument;
        private ComboBox cmbCanvasUnit;

        // --- Canvas Panel ---
        private Panel canvasPanel;

        // --- Bottom Panel for Print Preview & Export ---
        private Panel pnlBottom;
        private Button btnPrintPreview;
        private Button btnExportSVG;

        // Context menu for canvas objects (layering/deletion).
        private ContextMenuStrip contextMenu;

        // Context menu for AI text operations.
        private ContextMenuStrip aiContextMenu;

        // Variables to support drag–drop, resizing, and rotation.
        private CanvasObject selectedObject;
        private Point mouseDownLocation;
        private bool isDragging = false;
        private HandleType currentHandle = HandleType.None;
        private PointF initialMouseObjectSpace;
        private Rectangle originalBounds;
        private float initialRotation;
        private float initialAngle;
        private const int HANDLE_SIZE = 10;
        private float originalTextFontSize = 0;

        public LabelEditor()
        {
            InitializeComponent();
            // Attach the Load event so that the designer code isn’t modified
            this.Load += LabelEditor_Load;
            // For demonstration, add a sample text object.
            var sampleText = new TextCanvasObject
            {
                Text = "RO#: [RONumber]",
                Font = new Font("Arial", 16),
                ForeColor = Color.Black,
                Bounds = new Rectangle(100, 100, 200, 30),
                Rotation = 0
            };
            canvasObjects.Add(sampleText);
            // Populate the unit selection after all controls are created.
            cmbCanvasUnit.SelectedIndex = 0;
            SaveStateForUndo();
        }
        private void LabelEditor_Load(object sender, EventArgs e)
        {
            // Populate the font family ComboBox at runtime
            foreach (FontFamily ff in FontFamily.Families)
                cmbFontFamily.Items.Add(ff.Name);

            if (cmbFontFamily.Items.Count > 0)
                cmbFontFamily.SelectedItem = "Arial";
        }
        private void InitializeComponent()
        {
            // Set up form properties.
            this.Text = "Label Editor";
            this.Width = 1000;
            this.Height = 900;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // --- Bottom Panel for Print Preview & Export ---
            pnlBottom = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.LightGray
            };
            btnPrintPreview = new Button()
            {
                Text = "Print Preview",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };
            btnPrintPreview.Click += BtnPrintPreview_Click;
            pnlBottom.Controls.Add(btnPrintPreview);
            btnExportSVG = new Button()
            {
                Text = "Export Native SVG",
                Location = new Point(140, 10),
                Size = new Size(150, 30)
            };
            btnExportSVG.Click += BtnExportSVG_Click;
            pnlBottom.Controls.Add(btnExportSVG);

            btnUndo = new Button()
            {
                Text = "Undo",
                Location = new Point(300, 10),
                Size = new Size(75, 30)
            };
            btnUndo.Click += BtnUndo_Click;
            pnlBottom.Controls.Add(btnUndo);
            btnRedo = new Button()
            {
                Text = "Redo",
                Location = new Point(400, 10),
                Size = new Size(75, 30)
            };
            btnRedo.Click += BtnRedo_Click;
            pnlBottom.Controls.Add(btnRedo);

            this.Controls.Add(pnlBottom);

            // --- Left TabControl for Options ---
            tabOptions = new TabControl()
            {
                Dock = DockStyle.Right,
                Width = 250
            };

            // ----- Text Tab -----
            tabText = new TabPage("Text");

            // --- Static Variable Buttons (added before custom text) ---
            Button btnVarRONumber = new Button()
            {
                Text = "RO # [RONumber]",
                Location = new Point(10, 10),
                Size = new Size(220, 30)
            };
            btnVarRONumber.Click += (s, e) => InsertStaticVariable("RO # [RONumber]");
            tabText.Controls.Add(btnVarRONumber);

            Button btnVarDate = new Button()
            {
                Text = "Date: [Date]",
                Location = new Point(10, 45),
                Size = new Size(220, 30)
            };
            btnVarDate.Click += (s, e) => InsertStaticVariable("Date: [Date]");
            tabText.Controls.Add(btnVarDate);

            Button btnVarMileage = new Button()
            {
                Text = "Mileage: [Mileage]",
                Location = new Point(10, 80),
                Size = new Size(220, 30)
            };
            btnVarMileage.Click += (s, e) => InsertStaticVariable("Mileage: [Mileage]");
            tabText.Controls.Add(btnVarMileage);

            Button btnVarOilDesc = new Button()
            {
                Text = "Oil: [OilDesc]",
                Location = new Point(10, 115),
                Size = new Size(220, 30)
            };
            btnVarOilDesc.Click += (s, e) => InsertStaticVariable("Oil: [OilDesc]");
            tabText.Controls.Add(btnVarOilDesc);

            Button btnVarNextDate = new Button()
            {
                Text = "Next Date: [NextDate]",
                Location = new Point(10, 150),
                Size = new Size(220, 30)
            };
            btnVarNextDate.Click += (s, e) => InsertStaticVariable("Next Date: [NextDate]");
            tabText.Controls.Add(btnVarNextDate);

            Button btnVarNextMileage = new Button()
            {
                Text = "Next Mileage: [NextMileage]",
                Location = new Point(10, 185),
                Size = new Size(220, 30)
            };
            btnVarNextMileage.Click += (s, e) => InsertStaticVariable("Next Mileage: [NextMileage]");
            tabText.Controls.Add(btnVarNextMileage);

            Button btnVarNotes = new Button()
            {
                Text = "[Notes]",
                Location = new Point(10, 220),
                Size = new Size(220, 30)
            };
            btnVarNotes.Click += (s, e) => InsertStaticVariable("[Notes]");
            tabText.Controls.Add(btnVarNotes);

            // --- Now add a spacer and then the Add Custom Text button ---
            Button btnAddCustomText = new Button()
            {
                Text = "Add Custom Text",
                Location = new Point(10, 260),
                Size = new Size(220, 30)
            };
            btnAddCustomText.Click += BtnAddCustomText_Click;
            tabText.Controls.Add(btnAddCustomText);

            // --- Label and TextBox for Editing ---
            Label lblEditText = new Label()
            {
                Text = "Edit Text:",
                Location = new Point(10, 300),
                AutoSize = true
            };
            tabText.Controls.Add(lblEditText);

            txtEditText = new TextBox()
            {
                Location = new Point(10, 320),
                Size = new Size(220, 60),
                Multiline = true
            };
            txtEditText.TextChanged += TxtEditText_TextChanged;
            tabText.Controls.Add(txtEditText);

            // Context menu with AI-powered actions
            aiContextMenu = new ContextMenuStrip();
            ToolStripMenuItem rewriteItem = new ToolStripMenuItem("Rewrite with AI");
            rewriteItem.Click += RewriteItem_Click;
            ToolStripMenuItem summarizeItem = new ToolStripMenuItem("Summarize with AI");
            summarizeItem.Click += SummarizeItem_Click;
            ToolStripMenuItem translateItem = new ToolStripMenuItem("Translate to Spanish");
            translateItem.Click += TranslateItem_Click;
            aiContextMenu.Items.AddRange(new ToolStripItem[] { rewriteItem, summarizeItem, translateItem });
            txtEditText.ContextMenuStrip = aiContextMenu;

            // --- Formatting Controls ---
            // Font Family ComboBox
            cmbFontFamily = new ComboBox()
            {
                Location = new Point(10, 390),
                Width = 220
            };
            // (Populate cmbFontFamily in your Form_Load event to avoid designer issues.)
            tabText.Controls.Add(cmbFontFamily);

            // Font Size NumericUpDown.
            numFontSize = new NumericUpDown()
            {
                Location = new Point(10, 425),
                Width = 50,
                Minimum = 8,
                Maximum = 72,
                Value = 16
            };
            numFontSize.ValueChanged += NumFontSize_ValueChanged;
            tabText.Controls.Add(numFontSize);

            // Replace Bold and Italic check boxes with toggle buttons.
            Button btnBold = new Button()
            {
                Text = "B",
                Location = new Point(70, 425),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat
            };
            btnBold.Click += BtnBold_Click;
            tabText.Controls.Add(btnBold);

            Button btnItalic = new Button()
            {
                Text = "I",
                Location = new Point(100, 425),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat
            };
            btnItalic.Click += BtnItalic_Click;
            tabText.Controls.Add(btnItalic);

            btnTextColor = new Button()
            {
                Text = "Text Color",
                Location = new Point(130, 425),
                Size = new Size(80, 30)
            };
            btnTextColor.Click += BtnTextColor_Click;
            tabText.Controls.Add(btnTextColor);

            // ----- Image Tab (stub) -----
            tabImage = new TabPage("Image");
            Button btnUploadImage = new Button()
            {
                Text = "Upload Image",
                Location = new Point(10, 10),
                Size = new Size(200, 30)
            };
            btnUploadImage.Click += BtnUploadImage_Click;
            tabImage.Controls.Add(btnUploadImage);

            // ----- Shape Tab (stub) -----
            tabShape = new TabPage("Shape");
            Button btnLoadShape = new Button()
            {
                Text = "Load Shape (SVG)",
                Location = new Point(10, 10),
                Size = new Size(200, 30)
            };
            btnLoadShape.Click += BtnLoadShape_Click;
            tabShape.Controls.Add(btnLoadShape);

            // ----- QR Code Tab (stub) -----
            tabQRCode = new TabPage("QR Code");
            Button btnAddQRCode = new Button()
            {
                Text = "Add QR Code",
                Location = new Point(10, 10),
                Size = new Size(200, 30)
            };
            btnAddQRCode.Click += BtnAddQRCode_Click;
            tabQRCode.Controls.Add(btnAddQRCode);

            // ----- Document Tab -----
            tabDocument = new TabPage("Document");
            btnSaveDesign = new Button()
            {
                Text = "Save Design (JSON)",
                Location = new Point(10, 10),
                Size = new Size(200, 30)
            };
            btnSaveDesign.Click += BtnSaveJson_Click;
            tabDocument.Controls.Add(btnSaveDesign);

            btnLoadDesign = new Button()
            {
                Text = "Load Design (JSON)",
                Location = new Point(10, 50),
                Size = new Size(200, 30)
            };
            btnLoadDesign.Click += BtnLoadJson_Click;
            tabDocument.Controls.Add(btnLoadDesign);

            lblCanvasWidth = new Label()
            {
                Text = "Canvas Width:",
                Location = new Point(10, 100),
                AutoSize = true
            };
            tabDocument.Controls.Add(lblCanvasWidth);

            numCanvasWidth = new NumericUpDown()
            {
                Location = new Point(10, 120),
                Width = 80,
                Minimum = 100,
                Maximum = 2000,
                Value = 1000
            };
            tabDocument.Controls.Add(numCanvasWidth);

            lblCanvasHeight = new Label()
            {
                Text = "Canvas Height:",
                Location = new Point(100, 100),
                AutoSize = true
            };
            tabDocument.Controls.Add(lblCanvasHeight);

            numCanvasHeight = new NumericUpDown()
            {
                Location = new Point(100, 120),
                Width = 80,
                Minimum = 100,
                Maximum = 2000,
                Value = 700
            };
            tabDocument.Controls.Add(numCanvasHeight);

            // Add the unit selection ComboBox
            cmbCanvasUnit = new ComboBox()
            {
                Location = new Point(190, 120),
                Width = 40,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCanvasUnit.Items.AddRange(new object[] { "px", "in", "cm", "mm" });
            cmbCanvasUnit.SelectedIndexChanged += cmbCanvasUnit_SelectedIndexChanged;
            cmbCanvasUnit.SelectedIndex = 0; // default: pixels
            tabDocument.Controls.Add(cmbCanvasUnit);

            btnApplyCanvasSizeDocument = new Button()
            {
                Text = "Apply Canvas Size",
                Location = new Point(10, 150),
                Size = new Size(170, 30)
            };
            btnApplyCanvasSizeDocument.Click += BtnApplyCanvasSize_Document_Click;
            tabDocument.Controls.Add(btnApplyCanvasSizeDocument);

            // Add all tabs to the TabControl.
            tabOptions.TabPages.Add(tabText);
            tabOptions.TabPages.Add(tabImage);
            tabOptions.TabPages.Add(tabShape);
            tabOptions.TabPages.Add(tabQRCode);
            tabOptions.TabPages.Add(tabDocument);
            this.Controls.Add(tabOptions);

            // --- Canvas Panel (Fill remaining area, placed after the TabControl) ---
            canvasPanel = new Panel()
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(10, 10),
                Size = new Size(1000, 700),
                BackColor = Color.White
            };
            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += CanvasPanel_MouseUp;
            canvasPanel.DoubleClick += CanvasPanel_DoubleClick;
            this.Controls.Add(canvasPanel);

            // --- Setup Context Menu for Canvas Objects ---
            contextMenu = new ContextMenuStrip();
            ToolStripMenuItem bringForwardItem = new ToolStripMenuItem("Bring Forward");
            bringForwardItem.Click += BringForwardItem_Click;
            ToolStripMenuItem sendBackwardItem = new ToolStripMenuItem("Send Backward");
            sendBackwardItem.Click += SendBackwardItem_Click;
            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += DeleteItem_Click;
            contextMenu.Items.Add(bringForwardItem);
            contextMenu.Items.Add(sendBackwardItem);
            contextMenu.Items.Add(deleteItem);
        }


        #region Undo/Redo and State Management

        private void SaveStateForUndo()
        {
            try
            {
                // Serialize the current state.
                string jsonState = JsonConvert.SerializeObject(canvasObjects, Formatting.Indented,
                     new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                undoStack.Push(jsonState);
                // Clear redo stack on new change.
                redoStack.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving state: " + ex.Message);
            }
        }

        private void RestoreState(string jsonState)
        {
            try
            {
                List<CanvasObject> restored = JsonConvert.DeserializeObject<List<CanvasObject>>(jsonState,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                canvasObjects = restored;
                canvasPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error restoring state: " + ex.Message);
            }
        }

        private void BtnUndo_Click(object sender, EventArgs e)
        {
            try
            {
                if (undoStack.Count > 0)
                {
                    // Save current state in redo stack.
                    string currentState = JsonConvert.SerializeObject(canvasObjects, Formatting.Indented,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                    redoStack.Push(currentState);

                    // Restore last state.
                    string prevState = undoStack.Pop();
                    RestoreState(prevState);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during Undo: " + ex.Message);
            }
        }

        private void BtnRedo_Click(object sender, EventArgs e)
        {
            try
            {
                if (redoStack.Count > 0)
                {
                    string nextState = redoStack.Pop();
                    // Save current state in undo stack.
                    string currentState = JsonConvert.SerializeObject(canvasObjects, Formatting.Indented,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                    undoStack.Push(currentState);

                    RestoreState(nextState);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during Redo: " + ex.Message);
            }
        }

        #endregion

        #region Canvas Panel Events

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            // Fill the entire client area with white.
            e.Graphics.FillRectangle(Brushes.White, 0, 0, canvasPanel.ClientSize.Width, canvasPanel.ClientSize.Height);

            // Draw each object.
            foreach (var obj in canvasObjects)
            {
                obj.Draw(e.Graphics);
                if (obj == selectedObject)
                {
                    var handles = GetResizeHandles(obj);
                    foreach (var handle in handles)
                    {
                        e.Graphics.FillRectangle(Brushes.White, handle.Value);
                        e.Graphics.DrawRectangle(Pens.Black, handle.Value.X, handle.Value.Y, handle.Value.Width, handle.Value.Height);
                    }
                }
            }

            // Draw a solid black border (2 pixels thick) around the canvas.
            Rectangle borderRect = new Rectangle(0, 0, canvasPanel.ClientSize.Width - 1, canvasPanel.ClientSize.Height - 1);
            using (Pen pen = new Pen(Color.Black, 2))
            {
                e.Graphics.DrawRectangle(pen, borderRect);
            }
        }

        private Dictionary<HandleType, RectangleF> GetResizeHandles(CanvasObject obj)
        {
            Dictionary<HandleType, RectangleF> handles = new Dictionary<HandleType, RectangleF>();
            Rectangle bounds = obj.Bounds;
            float angle = obj.Rotation;
            PointF center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            // Unrotated corner points and midpoints.
            PointF topLeft = new PointF(bounds.Left, bounds.Top);
            PointF topRight = new PointF(bounds.Right, bounds.Top);
            PointF bottomLeft = new PointF(bounds.Left, bounds.Bottom);
            PointF bottomRight = new PointF(bounds.Right, bounds.Bottom);
            PointF top = new PointF((bounds.Left + bounds.Right) / 2f, bounds.Top);
            PointF bottom = new PointF((bounds.Left + bounds.Right) / 2f, bounds.Bottom);
            PointF left = new PointF(bounds.Left, (bounds.Top + bounds.Bottom) / 2f);
            PointF right = new PointF(bounds.Right, (bounds.Top + bounds.Bottom) / 2f);

            // Rotate these points.
            topLeft = RotatePoint(topLeft, center, angle);
            topRight = RotatePoint(topRight, center, angle);
            bottomLeft = RotatePoint(bottomLeft, center, angle);
            bottomRight = RotatePoint(bottomRight, center, angle);
            top = RotatePoint(top, center, angle);
            bottom = RotatePoint(bottom, center, angle);
            left = RotatePoint(left, center, angle);
            right = RotatePoint(right, center, angle);

            // Rotation handle – offset above the top midpoint.
            float rotationOffset = 20;
            float vx = top.X - center.X, vy = top.Y - center.Y;
            float len = (float)Math.Sqrt(vx * vx + vy * vy);
            float scale = (len + rotationOffset) / len;
            PointF rotationPoint = new PointF(center.X + vx * scale, center.Y + vy * scale);

            handles[HandleType.TopLeft] = new RectangleF(topLeft.X - HANDLE_SIZE / 2, topLeft.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.Top] = new RectangleF(top.X - HANDLE_SIZE / 2, top.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.TopRight] = new RectangleF(topRight.X - HANDLE_SIZE / 2, topRight.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.Right] = new RectangleF(right.X - HANDLE_SIZE / 2, right.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.BottomRight] = new RectangleF(bottomRight.X - HANDLE_SIZE / 2, bottomRight.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.Bottom] = new RectangleF(bottom.X - HANDLE_SIZE / 2, bottom.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.BottomLeft] = new RectangleF(bottomLeft.X - HANDLE_SIZE / 2, bottomLeft.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.Left] = new RectangleF(left.X - HANDLE_SIZE / 2, left.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
            handles[HandleType.Rotation] = new RectangleF(rotationPoint.X - HANDLE_SIZE / 2, rotationPoint.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);

            return handles;
        }

        private PointF RotatePoint(PointF point, PointF center, float angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            float x = (float)(dx * Math.Cos(angleRadians) - dy * Math.Sin(angleRadians)) + center.X;
            float y = (float)(dx * Math.Sin(angleRadians) + dy * Math.Cos(angleRadians)) + center.Y;
            return new PointF(x, y);
        }

        // Convert a screen point to the object's unrotated space.
        private PointF TransformPointToObjectSpace(PointF pt, CanvasObject obj)
        {
            PointF center = new PointF(obj.Bounds.X + obj.Bounds.Width / 2f, obj.Bounds.Y + obj.Bounds.Height / 2f);
            float dx = pt.X - center.X;
            float dy = pt.Y - center.Y;
            double rad = obj.Rotation * Math.PI / 180.0;
            float localX = (float)(dx * Math.Cos(rad) + dy * Math.Sin(rad));
            float localY = (float)(-dx * Math.Sin(rad) + dy * Math.Cos(rad));
            return new PointF(localX + obj.Bounds.Width / 2f, localY + obj.Bounds.Height / 2f);
        }

        private void CanvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                // Right-click: show context menu if on an object.
                if (e.Button == MouseButtons.Right)
                {
                    foreach (var obj in canvasObjects)
                    {
                        if (obj.Bounds.Contains(e.Location))
                        {
                            selectedObject = obj;
                            contextMenu.Show(canvasPanel, e.Location);
                            return;
                        }
                    }
                }
                // Check handles of selected object.
                if (selectedObject != null)
                {
                    var handles = GetResizeHandles(selectedObject);
                    foreach (var handle in handles)
                    {
                        if (handle.Value.Contains(e.Location))
                        {
                            currentHandle = handle.Key;
                            originalBounds = selectedObject.Bounds;
                            if (currentHandle == HandleType.Rotation)
                            {
                                PointF center = new PointF(selectedObject.Bounds.X + selectedObject.Bounds.Width / 2f,
                                                             selectedObject.Bounds.Y + selectedObject.Bounds.Height / 2f);
                                initialAngle = (float)Math.Atan2(e.Y - center.Y, e.X - center.X);
                                initialRotation = selectedObject.Rotation;
                            }
                            else
                            {
                                initialMouseObjectSpace = TransformPointToObjectSpace(e.Location, selectedObject);
                                if (selectedObject is TextCanvasObject txtObj)
                                {
                                    // Store the initial font size.
                                    originalTextFontSize = txtObj.Font.Size;
                                }
                            }
                            return;
                        }
                    }
                }
                // Selection by left-click.
                foreach (var obj in canvasObjects)
                {
                    if (obj.Bounds.Contains(e.Location))
                    {
                        selectedObject = obj;
                        isDragging = true;
                        mouseDownLocation = e.Location;
                        return;
                    }
                }
                // Click on empty canvas clears the selection.
                selectedObject = null;
                canvasPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in MouseDown: " + ex.Message);
            }
        }

        private void CanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (currentHandle != HandleType.None && selectedObject != null)
                {
                    if (currentHandle == HandleType.Rotation)
                    {
                        PointF center = new PointF(selectedObject.Bounds.X + selectedObject.Bounds.Width / 2f,
                                                     selectedObject.Bounds.Y + selectedObject.Bounds.Height / 2f);
                        float currentAngle = (float)Math.Atan2(e.Y - center.Y, e.X - center.X);
                        float angleDelta = currentAngle - initialAngle;
                        selectedObject.Rotation = initialRotation + angleDelta * 180 / (float)Math.PI;
                    }
                    else
                    {
                        PointF currMouseObjSpace = TransformPointToObjectSpace(e.Location, selectedObject);
                        float deltaX = currMouseObjSpace.X - initialMouseObjectSpace.X;
                        float deltaY = currMouseObjSpace.Y - initialMouseObjectSpace.Y;
                        Rectangle newBounds = originalBounds;
                        switch (currentHandle)
                        {
                            case HandleType.TopLeft:
                                newBounds.X = originalBounds.X + (int)deltaX;
                                newBounds.Y = originalBounds.Y + (int)deltaY;
                                newBounds.Width = originalBounds.Width - (int)deltaX;
                                newBounds.Height = originalBounds.Height - (int)deltaY;
                                break;
                            case HandleType.Top:
                                newBounds.Y = originalBounds.Y + (int)deltaY;
                                newBounds.Height = originalBounds.Height - (int)deltaY;
                                break;
                            case HandleType.TopRight:
                                newBounds.Y = originalBounds.Y + (int)deltaY;
                                newBounds.Width = originalBounds.Width + (int)deltaX;
                                newBounds.Height = originalBounds.Height - (int)deltaY;
                                break;
                            case HandleType.Right:
                                newBounds.Width = originalBounds.Width + (int)deltaX;
                                break;
                            case HandleType.BottomRight:
                                newBounds.Width = originalBounds.Width + (int)deltaX;
                                newBounds.Height = originalBounds.Height + (int)deltaY;
                                break;
                            case HandleType.Bottom:
                                newBounds.Height = originalBounds.Height + (int)deltaY;
                                break;
                            case HandleType.BottomLeft:
                                newBounds.X = originalBounds.X + (int)deltaX;
                                newBounds.Width = originalBounds.Width - (int)deltaX;
                                newBounds.Height = originalBounds.Height + (int)deltaY;
                                break;
                            case HandleType.Left:
                                newBounds.X = originalBounds.X + (int)deltaX;
                                newBounds.Width = originalBounds.Width - (int)deltaX;
                                break;
                        }
                        if (newBounds.Width < 20) newBounds.Width = 20;
                        if (newBounds.Height < 20) newBounds.Height = 20;
                        selectedObject.Bounds = newBounds;
                        // If the object is a text object, adjust its font size:
                        if (selectedObject is TextCanvasObject textObj)
                        {
                            // Calculate scale factor using width ratio.
                            float scale = (float)newBounds.Width / (float)originalBounds.Width;
                            float newFontSize = originalTextFontSize * scale;

                            // Optionally, clamp the new font size to a reasonable range.
                            if (newFontSize < 8)
                                newFontSize = 8;
                            if (newFontSize > 72)
                                newFontSize = 72;

                            // Update the font of the text object (preserving the current style).
                            textObj.Font = new Font(textObj.Font.FontFamily, newFontSize, textObj.Font.Style);
                        }
                    }
                    canvasPanel.Invalidate();
                }
                else if (isDragging && selectedObject != null)
                {
                    int dx = e.X - mouseDownLocation.X;
                    int dy = e.Y - mouseDownLocation.Y;
                    selectedObject.Bounds = new Rectangle(selectedObject.Bounds.X + dx,
                                                          selectedObject.Bounds.Y + dy,
                                                          selectedObject.Bounds.Width,
                                                          selectedObject.Bounds.Height);
                    mouseDownLocation = e.Location;
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in MouseMove: " + ex.Message);
            }
        }

        private void CanvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            currentHandle = HandleType.None;
        }

        #endregion

        #region Tab Control and Button Event Handlers

        #region Tab Options Events

        private void BtnAddCustomText_Click(object sender, EventArgs e)
        {
            try
            {
                var txtObj = new TextCanvasObject
                {
                    Text = "New Custom Text",
                    Font = new Font("Arial", 16),
                    ForeColor = Color.Black,
                    Bounds = new Rectangle(150, 150, 200, 30),
                    Rotation = 0
                };
                canvasObjects.Add(txtObj);
                selectedObject = txtObj;
                txtEditText.Text = txtObj.Text;
                SaveStateForUndo();
                canvasPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding custom text: " + ex.Message);
            }
        }

        private void TxtEditText_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    txtObj.Text = txtEditText.Text;
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editing text: " + ex.Message);
            }
        }

        private async void RewriteItem_Click(object sender, EventArgs e)
        {
            string prompt = $"Rewrite the following text:\n\n{txtEditText.Text}";
            await ReplaceTextWithOpenAIAsync(prompt);
        }

        private async void SummarizeItem_Click(object sender, EventArgs e)
        {
            string prompt = $"Summarize the following text:\n\n{txtEditText.Text}";
            await ReplaceTextWithOpenAIAsync(prompt);
        }

        private async void TranslateItem_Click(object sender, EventArgs e)
        {
            string prompt = $"Translate the following text into Spanish:\n\n{txtEditText.Text}";
            await ReplaceTextWithOpenAIAsync(prompt);
        }

        private async Task ReplaceTextWithOpenAIAsync(string prompt)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        MessageBox.Show("OPENAI_API_KEY is not set.");
                        return;
                    }

                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    var requestBody = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        }
                    };

                    string json = JsonConvert.SerializeObject(requestBody);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    string responseText = await response.Content.ReadAsStringAsync();

                    var obj = JObject.Parse(responseText);
                    string result = (string)obj["choices"]?[0]?["message"]?["content"];
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        txtEditText.Text = result.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("OpenAI request failed: " + ex.Message);
            }
        }

        private void CmbFontFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    string newFontFamily = cmbFontFamily.SelectedItem.ToString();
                    FontStyle style = txtObj.Font.Style;
                    txtObj.Font = new Font(newFontFamily, txtObj.Font.Size, style);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error changing font: " + ex.Message);
            }
        }

        private void NumFontSize_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    float newSize = (float)numFontSize.Value;
                    FontStyle style = txtObj.Font.Style;
                    txtObj.Font = new Font(txtObj.Font.FontFamily, newSize, style);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error changing font size: " + ex.Message);
            }
        }

        private void BtnTextColor_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    using (ColorDialog colorDlg = new ColorDialog())
                    {
                        if (colorDlg.ShowDialog() == DialogResult.OK)
                        {
                            txtObj.ForeColor = colorDlg.Color;
                            SaveStateForUndo();
                            canvasPanel.Invalidate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error changing text color: " + ex.Message);
            }
        }

        private void ChkBold_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    FontStyle style = txtObj.Font.Style;
                    if (chkBold.Checked)
                        style |= FontStyle.Bold;
                    else
                        style &= ~FontStyle.Bold;
                    txtObj.Font = new Font(txtObj.Font.FontFamily, txtObj.Font.Size, style);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error toggling Bold: " + ex.Message);
            }
        }

        private void ChkItalic_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject is TextCanvasObject txtObj)
                {
                    FontStyle style = txtObj.Font.Style;
                    if (chkItalic.Checked)
                        style |= FontStyle.Italic;
                    else
                        style &= ~FontStyle.Italic;
                    txtObj.Font = new Font(txtObj.Font.FontFamily, txtObj.Font.Size, style);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error toggling Italic: " + ex.Message);
            }
        }

        private void BtnApplyCanvasSize_Document_Click(object sender, EventArgs e)
        {
            try
            {
                if (canvasPanel == null)
                    return;

                // Get the unit conversion factor.
                double factor = GetConversionFactor(cmbCanvasUnit.SelectedItem.ToString());
                // Calculate new dimensions (in pixels) based on selected unit.
                int newWidth = (int)(numCanvasWidth.Value) * (int)(factor);
                int newHeight = (int)(numCanvasHeight.Value) * (int)(factor);
                canvasPanel.Size = new Size(newWidth, newHeight);
                SaveStateForUndo();
                canvasPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying canvas size: " + ex.Message);
            }
        }

        #endregion

        // For buttons in other tabs, we call the corresponding methods.
        private void BtnLoadShape_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "SVG Files|*.svg" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Bitmap svgImage = SvgHelper.LoadSvg(ofd.FileName, canvasPanel.Size);
                    var shapeObj = new ShapeCanvasObject
                    {
                        SvgPath = ofd.FileName,
                        BackgroundImage = svgImage,
                        Bounds = new Rectangle(0, 0, canvasPanel.Width, canvasPanel.Height)
                    };
                    shapeObj.SvgDocument = Svg.SvgDocument.Open(ofd.FileName);
                    canvasObjects.RemoveAll(x => x is ShapeCanvasObject);
                    canvasObjects.Add(shapeObj);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading SVG: " + ex.Message);
            }
        }

        private void BtnAddQRCode_Click(object sender, EventArgs e)
        {
            using (QRCodeForm qrForm = new QRCodeForm())
            {
                if (qrForm.ShowDialog() == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(qrForm.QRText))
                    {
                        var qrObj = new QRCodeCanvasObject
                        {
                            Data = qrForm.QRText,
                            Bounds = new Rectangle(100, 100, 150, 150),
                            Rotation = 0
                        };
                        canvasObjects.Add(qrObj);
                        SaveStateForUndo();
                        canvasPanel.Invalidate();
                    }
                    else
                    {
                        MessageBox.Show("QR text cannot be empty.");
                    }
                }
            }
        }

        private void BtnSaveJson_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON Files|*.json" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // Create a wrapper object for saving both canvas size and objects.
                    CanvasSaveData data = new CanvasSaveData()
                    {
                        CanvasSize = canvasPanel.Size,
                        Objects = canvasObjects
                    };

                    string json = JsonConvert.SerializeObject(data, Formatting.Indented,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Design saved successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving JSON: " + ex.Message);
            }
        }

        private void BtnLoadJson_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON Files|*.json" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string json = File.ReadAllText(ofd.FileName);
                    CanvasSaveData data = JsonConvert.DeserializeObject<CanvasSaveData>(json,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                    canvasObjects = data.Objects;

                    // Update the canvas size according to the saved data.
                    canvasPanel.Size = data.CanvasSize;
                    cmbCanvasUnit.SelectedIndex = 0;
                    // Also update any size controls, if needed:
                    numCanvasWidth.Value = data.CanvasSize.Width;
                    numCanvasHeight.Value = data.CanvasSize.Height;

                    canvasPanel.Invalidate();
                    MessageBox.Show("Design loaded successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading JSON: " + ex.Message);
            }
        }

        private void BtnPrintPreview_Click(object sender, EventArgs e)
        {
            try
            {
                var variables = new Dictionary<string, string>()
                {
                    { "RONumber", "1523" },
                    { "Mileage", "10005" },
                    { "OilDesc", "5W 40" },
                    { "Date", "11/17/2023" },
                    { "NextDate", "05/17/2024" },
                    { "NextMileage", "20005" },
                    { "Notes", "NOTES" }
                };

                PrintPreviewForm preview = new PrintPreviewForm(canvasObjects, variables, canvasPanel.Size);
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Print Preview: " + ex.Message);
            }
        }

        private void BtnExportSVG_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "SVG Files|*.svg" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var shapeObj = canvasObjects.OfType<ShapeCanvasObject>().FirstOrDefault();
                    if (shapeObj != null && shapeObj.SvgDocument != null)
                    {
                        shapeObj.SvgDocument.Write(sfd.FileName);
                        MessageBox.Show("Native SVG exported successfully!");
                    }
                    else
                    {
                        MessageBox.Show("No shape object with native SVG found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting SVG: " + ex.Message);
            }
        }

        // Bring the selected object forward (move it to the end of the list).
        private void BringForwardItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject != null)
                {
                    canvasObjects.Remove(selectedObject);
                    canvasObjects.Add(selectedObject);
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error bringing forward: " + ex.Message);
            }
        }

        // Send the selected object backward (move it to the beginning of the list).
        private void SendBackwardItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject != null)
                {
                    canvasObjects.Remove(selectedObject);
                    canvasObjects.Insert(0, selectedObject);
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending backward: " + ex.Message);
            }
        }

        // Delete the selected object from the canvas.
        private void DeleteItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObject != null)
                {
                    canvasObjects.Remove(selectedObject);
                    selectedObject = null;
                    canvasPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting object: " + ex.Message);
            }
        }

        private void BtnUploadImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ImageCanvasObject imageObj = new ImageCanvasObject();
                    imageObj.ImagePath = ofd.FileName;
                    // Now that there's a setter, this line is allowed:
                    imageObj.Image = Image.FromFile(ofd.FileName);
                    imageObj.Bounds = new Rectangle(100, 100, imageObj.Image.Width, imageObj.Image.Height);

                    canvasObjects.Add(imageObj);
                    SaveStateForUndo();
                    canvasPanel.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error uploading image: " + ex.Message);
                }
            }
        }

        private void CanvasPanel_DoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = e as MouseEventArgs;
            if (me == null)
                return;

            // Iterate in reverse order so that topmost object is checked first.
            foreach (var obj in canvasObjects.Reverse<CanvasObject>())
            {
                if (obj is TextCanvasObject && obj.Bounds.Contains(me.Location))
                {
                    selectedObject = obj;

                    // Create and configure the inline editing TextBox.
                    inlineEditTextBox = new TextBox();
                    inlineEditTextBox.Multiline = true;
                    inlineEditTextBox.Text = ((TextCanvasObject)obj).Text;
                    inlineEditTextBox.Font = ((TextCanvasObject)obj).Font;
                    inlineEditTextBox.ForeColor = ((TextCanvasObject)obj).ForeColor;
                    inlineEditTextBox.Bounds = obj.Bounds;

                    // When editing is finished, update the text.
                    inlineEditTextBox.Leave += InlineEditTextBox_Leave;
                    inlineEditTextBox.KeyDown += InlineEditTextBox_KeyDown;

                    // Place the TextBox on the canvas.
                    canvasPanel.Controls.Add(inlineEditTextBox);
                    inlineEditTextBox.BringToFront();
                    inlineEditTextBox.Focus();
                    break;
                }
            }
        }

        private void InlineEditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EndInlineEditing();
                e.SuppressKeyPress = true; // Prevents a ding sound.
            }
        }

        private void InlineEditTextBox_Leave(object sender, EventArgs e)
        {
            EndInlineEditing();
        }

        private void EndInlineEditing()
        {
            // If the inline editor is already null or disposed, exit immediately.
            if (inlineEditTextBox == null || inlineEditTextBox.IsDisposed)
                return;

            // Unsubscribe from events to avoid reentrancy.
            inlineEditTextBox.Leave -= InlineEditTextBox_Leave;
            inlineEditTextBox.KeyDown -= InlineEditTextBox_KeyDown;

            // Update the text of the text canvas object if possible.
            if (selectedObject is TextCanvasObject textObj)
            {
                textObj.Text = inlineEditTextBox.Text;
            }

            SaveStateForUndo();

            // Remove the inline editor control if it's present.
            if (canvasPanel.Controls.Contains(inlineEditTextBox))
            {
                canvasPanel.Controls.Remove(inlineEditTextBox);
            }

            // Dispose and set to null.
            inlineEditTextBox.Dispose();
            inlineEditTextBox = null;

            canvasPanel.Invalidate();
        }

        private void InsertStaticVariable(string variableText)
        {
            // Create a new text object with the static variable text.
            TextCanvasObject newTextObj = new TextCanvasObject()
            {
                Text = variableText,
                Font = new Font("Arial", (float)numFontSize.Value),
                ForeColor = Color.Black,
                Bounds = new Rectangle(150, 150, 200, 30),
                Rotation = 0
            };
            canvasObjects.Add(newTextObj);

            SaveStateForUndo();
            canvasPanel.Invalidate();
        }

        private void BtnBold_Click(object sender, EventArgs e)
        {
            if (selectedObject is TextCanvasObject textObj)
            {
                FontStyle style = textObj.Font.Style;
                Button btn = sender as Button;
                if (!style.HasFlag(FontStyle.Bold))
                {
                    style |= FontStyle.Bold;
                    btn.BackColor = Color.LightBlue; // toggle color to indicate "pressed"
                }
                else
                {
                    style &= ~FontStyle.Bold;
                    btn.BackColor = SystemColors.Control;
                }
                textObj.Font = new Font(textObj.Font.FontFamily, textObj.Font.Size, style);
                SaveStateForUndo();
                canvasPanel.Invalidate();
            }
        }

        private void BtnItalic_Click(object sender, EventArgs e)
        {
            if (selectedObject is TextCanvasObject textObj)
            {
                FontStyle style = textObj.Font.Style;
                Button btn = sender as Button;
                if (!style.HasFlag(FontStyle.Italic))
                {
                    style |= FontStyle.Italic;
                    btn.BackColor = Color.LightBlue;
                }
                else
                {
                    style &= ~FontStyle.Italic;
                    btn.BackColor = SystemColors.Control;
                }
                textObj.Font = new Font(textObj.Font.FontFamily, textObj.Font.Size, style);
                SaveStateForUndo();
                canvasPanel.Invalidate();
            }
        }

        private void cmbCanvasUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Guard against canvasPanel being null.
            if (canvasPanel == null)
                return;

            // Update the numeric up/down ranges based on the selected unit.
            UpdateNumericRangeForCanvas();

            // Get the conversion factor for the selected unit.
            double factor = GetConversionFactor(cmbCanvasUnit.SelectedItem.ToString());
            // Update the numeric controls with the canvas size in the selected unit.
            if (factor != 0)
            {
                // Convert the current canvasPanel dimensions (in pixels) to the selected unit.
                decimal newWidthValue = (decimal)(canvasPanel.Width / factor);
                decimal newHeightValue = (decimal)(canvasPanel.Height / factor);

                // Clamp to ensure the value is within the new Minimum and Maximum.
                if (newWidthValue < numCanvasWidth.Minimum)
                    newWidthValue = numCanvasWidth.Minimum;
                if (newWidthValue > numCanvasWidth.Maximum)
                    newWidthValue = numCanvasWidth.Maximum;
                if (newHeightValue < numCanvasHeight.Minimum)
                    newHeightValue = numCanvasHeight.Minimum;
                if (newHeightValue > numCanvasHeight.Maximum)
                    newHeightValue = numCanvasHeight.Maximum;

                numCanvasWidth.Value = newWidthValue;
                numCanvasHeight.Value = newHeightValue;
            }
        }

        /// <summary>
        /// Returns a conversion factor to convert the given unit into pixels.
        /// Assumes a standard DPI of 96.
        /// </summary>
        private double GetConversionFactor(string unit)
        {
            switch (unit)
            {
                case "in":  // inches to pixels
                    return 96.0;
                case "cm":  // centimeters to pixels (1 in = 2.54 cm)
                    return 96.0 / 2.54;
                case "mm":  // millimeters to pixels (1 in = 25.4 mm)
                    return 96.0 / 25.4;
                case "px":
                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Adjusts the Minimum and Maximum of the numeric up/down controls based on the selected unit.
        /// Assumes the original pixel range is from 100 to 2000.
        /// </summary>
        private void UpdateNumericRangeForCanvas()
        {
            // The original pixel range.
            double pixelMin = 100;
            double pixelMax = 2000;

            // Get the conversion factor for the current unit.
            double factor = GetConversionFactor(cmbCanvasUnit.SelectedItem.ToString());

            // Calculate the new range for the numeric controls in the selected unit.
            decimal newMin = (decimal)(pixelMin / factor);
            decimal newMax = (decimal)(pixelMax / factor);

            numCanvasWidth.Minimum = newMin;
            numCanvasWidth.Maximum = newMax;
            numCanvasHeight.Minimum = newMin;
            numCanvasHeight.Maximum = newMax;
        }

        private void UpdateTextFormattingButtons()
        {
            // Assume you have stored references to btnBold and btnItalic at class level.
            if (selectedObject is TextCanvasObject textObj)
            {
                // Example: update Bold button appearance.
                // (Assuming btnBold is declared at class level; if not, you may need to get it from tabText.Controls.)
                //btnBold.BackColor = textObj.Font.Style.HasFlag(FontStyle.Bold) ? Color.LightBlue : SystemColors.Control;
                //btnItalic.BackColor = textObj.Font.Style.HasFlag(FontStyle.Italic) ? Color.LightBlue : SystemColors.Control;
            }
        }
        #endregion

    }
}