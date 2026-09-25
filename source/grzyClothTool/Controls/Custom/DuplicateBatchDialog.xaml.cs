using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    public class DuplicateBatchItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public GDrawable Drawable { get; set; }
        public List<GDrawable> ExistingDuplicates { get; set; }

        private bool _shouldAdd = true;
        public bool ShouldAdd
        {
            get => _shouldAdd;
            set
            {
                if (_shouldAdd != value)
                {
                    _shouldAdd = value;
                    OnPropertyChanged();
                }
            }
        }

        public DuplicateBatchItem()
        {
            LocalizationHelper.LanguageChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(NewDrawableName));
                OnPropertyChanged(nameof(DuplicateOfText));
                OnPropertyChanged(nameof(TypeDisplay));
            };
        }

        public string NewDrawableName => System.IO.Path.GetFileName(Drawable?.FilePath)
            ?? LocalizationHelper.Translate("Неизвестно");

        public string DuplicateOfText
        {
            get
            {
                if (ExistingDuplicates == null || ExistingDuplicates.Count == 0)
                    return LocalizationHelper.Translate("Совпадающих элементов не найдено");

                var names = ExistingDuplicates.Select(d => d.Name).Take(3);
                var text = LocalizationHelper.Format("Дубликат: {0}", string.Join(", ", names));
                if (ExistingDuplicates.Count > 3)
                    text += " " + LocalizationHelper.Format("(ещё {0})", ExistingDuplicates.Count - 3);
                return text;
            }
        }

        public string TypeDisplay => LocalizationHelper.Translate(Drawable?.IsProp == true ? "Пропс" : "Компонент");

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DuplicateBatchResult
    {
        public bool Cancelled { get; set; }
        public List<GDrawable> DrawablesToAdd { get; set; } = [];
        public List<GDrawable> DrawablesToSkip { get; set; } = [];
    }

    public partial class DuplicateBatchDialog : Window
    {
        private readonly List<DuplicateBatchItem> _items;
        private DuplicateBatchResult _result;

        public DuplicateBatchDialog(List<DuplicateBatchItem> duplicateItems)
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);
            LocalizationHelper.LanguageChanged += (_, _) => Dispatcher.BeginInvoke(new Action(() =>
            {
                LocalizationHelper.ApplyTo(this);
                UpdateSummary();
            }));
            Owner = Application.Current.MainWindow;

            _items = duplicateItems;
            DuplicatesList.ItemsSource = _items;

            UpdateSummary();

            foreach (var item in _items)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DuplicateBatchItem.ShouldAdd))
                        UpdateSummary();
                };
            }
        }

        private void UpdateSummary()
        {
            var selectedCount = _items.Count(x => x.ShouldAdd);
            SummaryText.Text = LocalizationHelper.Format("Будет добавлено дубликатов: {0} из {1}", selectedCount, _items.Count);
        }

        public static DuplicateBatchResult Show(List<DuplicateBatchItem> duplicateItems)
        {
            var dialog = new DuplicateBatchDialog(duplicateItems);
            dialog.ShowDialog();
            return dialog._result ?? new DuplicateBatchResult { Cancelled = true };
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.ShouldAdd = true;
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.ShouldAdd = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _result = new DuplicateBatchResult
            {
                Cancelled = true,
                DrawablesToAdd = [],
                DrawablesToSkip = _items.Select(x => x.Drawable).ToList()
            };
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            _result = new DuplicateBatchResult
            {
                Cancelled = false,
                DrawablesToAdd = _items.Where(x => x.ShouldAdd).Select(x => x.Drawable).ToList(),
                DrawablesToSkip = _items.Where(x => !x.ShouldAdd).Select(x => x.Drawable).ToList()
            };
            Close();
        }

        private void OnCaptionPress(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
