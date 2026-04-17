using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.UI.ViewModels
{
    // In /ViewModels or /Models, depending on your choice
    public class OcrLine
    {
        public string Text { get; set; }  // OCR line text
        public bool IsHighlighted { get; set; }  // Flag indicating if the line should be highlighted

        public OcrLine(string text, bool highlight)
        {
            Text = text;
            IsHighlighted = highlight;
        }
    }
}
