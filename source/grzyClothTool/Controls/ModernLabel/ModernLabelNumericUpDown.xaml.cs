using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    public partial class ModernLabelNumericUpDown : ModernLabelBaseControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
            .Register("Label", typeof(string), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata("Числовое поле"));

        public static readonly DependencyProperty ValueProperty = DependencyProperty
            .Register("Value", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M, OnValuePropertyChanged));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty
            .Register("Minimum", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M, OnBoundsPropertyChanged));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty
            .Register("Maximum", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M, OnBoundsPropertyChanged));

        public static readonly DependencyProperty IncrementProperty = DependencyProperty
            .Register("Increment", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(1.0M));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public decimal Minimum
        {
            get { return (decimal)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public decimal Maximum
        {
            get { return (decimal)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public decimal Increment
        {
            get { return (decimal)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        public ModernLabelNumericUpDown()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateButtonStates();
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernLabelNumericUpDown)d;
            OnUpdate(d, e);
            control.Dispatcher.BeginInvoke(new Action(control.UpdateButtonStates));
        }

        private static void OnBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ModernLabelNumericUpDown)d).UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (NumericSurface?.Template == null)
            {
                return;
            }

            if (NumericSurface.Template.FindName("UpButtonElement", NumericSurface) is Button up)
            {
                up.IsEnabled = Value < Maximum;
            }

            if (NumericSurface.Template.FindName("DownButtonElement", NumericSurface) is Button down)
            {
                down.IsEnabled = Value > Minimum;
            }
        }

        private void Number_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TextBox edits (digits, decimal separators, paste and deletes)
            // do not pass through the increment/decrement buttons. Mark the
            // dependency-property update as user-initiated as well.
            if (e.Key != Key.Up && e.Key != Key.Down)
            {
                IsUserInitiated = true;
            }

            if (e.Key == Key.Up)
            {
                IncrementValue(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                DecrementValue(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void IncrementValue(object sender, RoutedEventArgs e)
        {
            if (Value + Increment <= Maximum)
            {
                IsUserInitiated = true;
                Value += Increment;
            }
            UpdateButtonStates();
        }

        private void DecrementValue(object sender, RoutedEventArgs e)
        {
            if (Value - Increment >= Minimum)
            {
                IsUserInitiated = true;
                Value -= Increment;
            }
            UpdateButtonStates();
        }
    }
}