using grzyClothTool.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for AutoSaveIndicator.xaml
    /// </summary>
    public partial class AutoSaveIndicator : UserControl
    {
        public AutoSaveIndicator()
        {
            InitializeComponent();
            DataContext = this;
            LocalizationHelper.LanguageChanged += (_, _) => UpdateTooltip(RemainingSeconds);
        }

        public static readonly DependencyProperty RemainingSecondsProperty =
            DependencyProperty.Register("RemainingSeconds", typeof(int), typeof(AutoSaveIndicator), new PropertyMetadata(0, OnRemainingSecondsChanged));

        public int RemainingSeconds
        {
            get { return (int)GetValue(RemainingSecondsProperty); }
            set { SetValue(RemainingSecondsProperty, value); }
        }

        private static void OnRemainingSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoSaveIndicator indicator)
            {
                indicator.UpdateTooltip((int)e.NewValue);
            }
        }

        private void UpdateTooltip(int seconds)
        {
            TooltipBorder.ToolTip = seconds > 0
                ? LocalizationHelper.Format("Автосохранение через {0} с", seconds)
                : LocalizationHelper.Translate("Сохранение");
        }

        public void UpdateProgress(double percentage)
        {
            double clamped = Math.Max(0, Math.Min(percentage, 100));
            double radius = 10;
            double circumference = 2 * Math.PI * radius;

            if (clamped <= 0)
            {
                ProgressPath.StrokeDashArray = new DoubleCollection { circumference, 0 };
                ProgressPath.StrokeDashOffset = circumference;
                return;
            }

            double visible = (clamped / 100.0) * circumference;
            double hidden = circumference - visible;

            ProgressPath.StrokeDashArray = [visible, hidden];
            ProgressPath.StrokeDashOffset = circumference;
        }
    }
}

