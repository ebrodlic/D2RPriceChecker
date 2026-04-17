using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D2RPriceChecker.Core.Pricing;
using D2RPriceChecker.Core.Traderie.Domain;
using D2RPriceChecker.Core.Traderie.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;


namespace D2RPriceChecker.ViewModels
{
    public partial class OverlayViewModel : ObservableObject
    {  

        // Top section: OCR results
        public ObservableCollection<string> OcrLines { get; set; } = new();

  

        // Bottom section: Trades info
        public ObservableCollection<Trade> Trades { get; set; } = new();

        public TradeActivityInfo Activity { get; set; } = new();

        public double PricePrediction { get; set; } = 0;

        public string PricePredictionHint { get; set; } = "";


        public string PriceGroupsDisplay =>
            Trades.FirstOrDefault()?.PriceGroups == null
                ? "-"
                : string.Join(" OR ",
                    Trades.First().PriceGroups.Select(g =>
                        string.Join(" + ",
                            g.Prices.Select(p => $"{p.Quantity}x {p.Name}")
                        )
                    ));

        [ObservableProperty]
        private bool isRuneInfoVisible;

        [ObservableProperty]
        private bool isRuneInfoPinned;


        [RelayCommand]
        private void ToggleRuneInfo()
        {
            IsRuneInfoVisible = !IsRuneInfoVisible;
        }

        [RelayCommand]
        private void ToggleRuneInfoPinned()
        {
            IsRuneInfoPinned = !IsRuneInfoPinned;

            // If pinned ON → force visible
            // If pinned OFF → revert to hover-only behavior
            IsRuneInfoVisible = IsRuneInfoPinned;
        }

        public void RuneInfoHoverEnter()
        {
            if (!IsRuneInfoPinned)
                IsRuneInfoVisible = true;
        }

        public void RuneInfoHoverLeave()
        {
            if (!IsRuneInfoPinned)
                IsRuneInfoVisible = false;
        }

        public ObservableCollection<RuneValue> RuneValuesDisplay
        {
            get
            {
                return Statistics?.RuneValues != null
                    ? new ObservableCollection<RuneValue>(Statistics.RuneValues)
                    : new ObservableCollection<RuneValue>();
            }
        }


        // Mid section: Trade/prices statistics
        private TradeStatistics _statistics { get; set; }

        public TradeStatistics? Statistics
        {
            get => _statistics;
            set
            {
                _statistics = value;
                OnPropertyChanged();
                NotifyStatisticsChanged();
             
               
            }
        }

        public void RefreshPriceGroupsDisplay()
        {
            OnPropertyChanged(nameof(PriceGroupsDisplay));
        }

       
        public void RecalculateActivity()
        {
            var calc = new TradeActivityCalculator();

            Activity = calc.Calculate(Trades.ToList());

            NotifyActivityChanged();

            OnPropertyChanged(nameof(Activity));
            OnPropertyChanged(nameof(ActivityBorderBrush));
            OnPropertyChanged(nameof(ActivityTextBrush));
        }

        private void NotifyActivityChanged()
        {
            if (Activity == null)
                return;

            Color baseColor = Activity.Level switch
            {
                ActivityLevel.Dead => Color.FromRgb(90, 90, 90),     // gray
                ActivityLevel.Low => Color.FromRgb(176, 0, 32),     // red
                ActivityLevel.Medium => Color.FromRgb(230, 126, 34),   // orange
                ActivityLevel.High => Color.FromRgb(241, 196, 15),   // yellow
                ActivityLevel.VeryHigh => Color.FromRgb(46, 204, 113),   // green
                _ => Color.FromRgb(90, 90, 90)
            };

            ActivityBorderBrush = new SolidColorBrush(baseColor);
            ActivityTextBrush = new SolidColorBrush(Lighten(baseColor, 0.5));
        }

        private Color Lighten(Color color, double factor = 0.5)
        {
            return Color.FromRgb(
                (byte)(color.R + (255 - color.R) * factor),
                (byte)(color.G + (255 - color.G) * factor),
                (byte)(color.B + (255 - color.B) * factor)
            );
        }

        public Brush ActivityBorderBrush { get; set; }
        public Brush ActivityTextBrush { get; set; }

        public string FloorDisplay =>
            Statistics == null
                ? "-"
                : $"{Statistics.Percentiles.Floor:0.##} Ist";

        public string TypicalDisplay =>
            Statistics == null
                ? "-"
                : $"{Statistics.Percentiles.Typical:0.##} Ist";

        public string GoodDisplay =>
            Statistics == null
                ? "-"
                : $"{Statistics.Percentiles.Good:0.##} Ist";      

        public string HighDisplay =>
            Statistics == null
                ? "-"
                : $"{Statistics.Percentiles.High:0.##} Ist";
        public string FloorEstimateDisplay =>
          Statistics == null
              ? "-"
              : $"~{ GetRuneHint(Statistics.Percentiles.Floor)}";

        public string TypicalEstimateDisplay =>
         Statistics == null
             ? "-"
             : $"~{GetRuneHint(Statistics.Percentiles.Typical)}";

        public string GoodEstimateDisplay =>
      Statistics == null
          ? "-"
          : $"~{GetRuneHint(Statistics.Percentiles.Good)}";


        public string HighEstimateDisplay =>
           Statistics == null
               ? "-"
               : $"~{GetRuneHint(Statistics.Percentiles.High)}";  


        public bool HasStatistics => Statistics != null;


        private void NotifyStatisticsChanged()
        {
            OnPropertyChanged(nameof(HasStatistics));
            OnPropertyChanged(nameof(FloorDisplay));
            OnPropertyChanged(nameof(TypicalDisplay));
            OnPropertyChanged(nameof(GoodDisplay));
            OnPropertyChanged(nameof(HighDisplay));
            OnPropertyChanged(nameof(FloorEstimateDisplay));
            OnPropertyChanged(nameof(TypicalEstimateDisplay));
            OnPropertyChanged(nameof(GoodEstimateDisplay));
            OnPropertyChanged(nameof(HighEstimateDisplay));
            OnPropertyChanged(nameof(RuneValuesDisplay));
        }

        public void RefreshPricePrediction()
        {
            var table = new RuneValueTable(Statistics.RuneValues);
            var prediction = new PricePredictionService(table);

            PricePrediction = prediction.Predict(OcrLines.ToList(), Trades.ToList());
            PricePredictionHint = "~" + GetRuneHint(PricePrediction) + " ";

            OnPropertyChanged(nameof(PricePrediction));
            OnPropertyChanged(nameof(PricePredictionHint));

        }




        private string GetRuneHint(double value)
        {
            if (Statistics?.RuneValues == null) return "";

            var closest = Statistics.RuneValues
                .OrderBy(r => Math.Abs(r.IstValue - value))
                .First();

            return closest.ShortName;
        }
    }

    public class PriceGroupDisplay
    {
        public string Text { get; set; } = "";
        public bool IsOr { get; set; }
    }
}
