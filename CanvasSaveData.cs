using System;
using System.Collections.Generic;
using System.Drawing;

namespace LabelEditorApp
{
    [Serializable]
    public class CanvasSaveData
    {
        public Size CanvasSize { get; set; }
        public List<CanvasObject> Objects { get; set; }
    }
}
