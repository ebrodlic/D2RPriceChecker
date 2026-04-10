using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using D2RPriceChecker.Features.Traderie;

namespace D2RPriceChecker.ViewModels
{
    public class OverlayViewModel
    {
        // Top section: OCR results
        public ObservableCollection<string> OcrLines { get; set; } = new();

        // Bottom section: Trades info
        public ObservableCollection<Trade> Trades { get; set; } = new();
    }
}
