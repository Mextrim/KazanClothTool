using Material.Icons;
using Material.Icons.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using grzyClothTool.Helpers;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Browsable catalog of the Material Design Icons shipped with Material.Icons.WPF.
    /// Clicking an icon copies its XAML markup to the clipboard.
    /// </summary>
    public partial class IconCatalogWindow : Window
    {
        private const int MaxVisibleIcons = 320;
        private const string SourceUrl = "https://www.figma.com/community/file/878585965681562011/material-design-icons";

        private readonly IReadOnlyList<IconCatalogItem> _allIcons;
        private bool _isLoading;

        public ObservableCollection<IconCatalogItem> VisibleIcons { get; } = [];

        public double IconSize { get; private set; } = 20;

        public IconCatalogWindow()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);

            DataContext = this;
            _allIcons = MaterialIconCatalog.GetAll();
            IconItems.ItemsSource = VisibleIcons;

            Loaded += IconCatalogWindow_Loaded;
            Closed += IconCatalogWindow_Closed;
        }

        private void IconCatalogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            try
            {
                ApplyFilter(string.Empty);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void IconCatalogWindow_Closed(object sender, EventArgs e)
        {
            IconCatalogHelper.Close(this);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            ApplyFilter(SearchBox.Text);
        }

        private void SizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SizeComboBox.SelectedItem is ComboBoxItem item
                && double.TryParse(item.Tag?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                IconSize = parsed;
            }
        }

        private void ApplyFilter(string query)
        {
            string normalized = (query ?? string.Empty).Trim();

            IEnumerable<IconCatalogItem> source = _allIcons;
            if (normalized.Length > 0)
            {
                source = source.Where(icon => icon.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase));
            }

            List<IconCatalogItem> matches = source.Take(MaxVisibleIcons).ToList();

            VisibleIcons.Clear();
            foreach (IconCatalogItem icon in matches)
            {
                VisibleIcons.Add(icon);
            }

            bool isEmpty = VisibleIcons.Count == 0;
            EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            IconItems.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;

            if (isEmpty)
            {
                EmptyText.Text = LocalizationHelper.Format("Ничего не найдено: {0}", normalized);
                CountText.Text = LocalizationHelper.Format("Иконок: 0");
                return;
            }

            CountText.Text = LocalizationHelper.Format("Показано: {0} из {1}", VisibleIcons.Count, _allIcons.Count);
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: IconCatalogItem icon })
            {
                return;
            }

            try
            {
                Clipboard.SetText(icon.BuildMarkup(IconSize));
                StatusText.Text = LocalizationHelper.Format("Скопировано: {0}", icon.Name);
            }
            catch (Exception)
            {
                StatusText.Text = LocalizationHelper.Format("Не удалось скопировать разметку: {0}", icon.Name);
            }
        }

        private void SourceLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(SourceUrl) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // Opening a browser must never break the catalog.
            }
        }
    }

    public sealed class IconCatalogItem
    {
        public IconCatalogItem(MaterialIconKind kind)
        {
            Kind = kind;
            Name = kind.ToString();
        }

        public MaterialIconKind Kind { get; }

        public string Name { get; }

        public string ToolTip => Name;

        public string BuildMarkup(double size) =>
            $"<icons:MaterialIcon Kind=\"{Name}\" Width=\"{size:0}\" Height=\"{size:0}\" />";
    }

    /// <summary>
    /// Caches the Material Design Icons metadata exposed by Material.Icons.WPF.
    /// </summary>
    public static class MaterialIconCatalog
    {
        private static IReadOnlyList<IconCatalogItem>? _cache;

        public static IReadOnlyList<IconCatalogItem> GetAll()
        {
            if (_cache is not null)
            {
                return _cache;
            }

            // MaterialIconKind contains aliases sharing the same underlying value, so group
            // by the numeric value first to avoid duplicate rows for one glyph.
            _cache = Enum.GetValues(typeof(MaterialIconKind))
                .Cast<MaterialIconKind>()
                .GroupBy(kind => Convert.ToInt32(kind, System.Globalization.CultureInfo.InvariantCulture))
                .Select(group => group.First())
                .Where(kind => MaterialIconDataProvider.Instance.ProvideData(kind) is not null)
                .OrderBy(kind => kind.ToString(), StringComparer.Ordinal)
                .Select(kind => new IconCatalogItem(kind))
                .ToList();

            return _cache;
        }
    }

    public static class IconCatalogHelper
    {
        private static IconCatalogWindow? _window;

        public static void Open()
        {
            if (_window is null)
            {
                _window = new IconCatalogWindow();
            }

            if (!_window.IsVisible)
            {
                _window.Owner = Application.Current?.MainWindow;
                _window.Show();
            }

            _window.Activate();
        }

        public static void Close(IconCatalogWindow window)
        {
            if (ReferenceEquals(_window, window))
            {
                _window = null;
            }
        }
    }
}
