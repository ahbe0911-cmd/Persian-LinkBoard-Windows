using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace PersianLinkBoard
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersianLinkBoard");
        private readonly string dataFile;
        private readonly string settingsFile;
        private readonly PersianCalendar persianCalendar = new PersianCalendar();
        private readonly DispatcherTimer clockTimer;
        private Point dragStartPoint;
        private LinkItem draggedLink;
        private bool showSeconds = true;
        private bool compactMode = false;
        private double cardWidth = 236;

        public ObservableCollection<LinkItem> Links { get; } = new ObservableCollection<LinkItem>();
        public ICollectionView LinksView { get; private set; }
        public double CardWidth
        {
            get { return cardWidth; }
            set
            {
                if (Math.Abs(cardWidth - value) < 0.1) return;
                cardWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardWidth)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            dataFile = Path.Combine(dataFolder, "links.xml");
            settingsFile = Path.Combine(dataFolder, "settings.txt");
            LoadSettings();
            LoadLinks();
            LinksView = CollectionViewSource.GetDefaultView(Links);
            LinksView.Filter = FilterLink;
            DataContext = this;
            ApplySettings();
            UpdateStats();
            UpdateClock();
            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (s, e) => UpdateClock();
            clockTimer.Start();
        }

        private bool FilterLink(object item)
        {
            var link = item as LinkItem;
            if (link == null) return false;
            var q = (SearchBox?.Text ?? string.Empty).Trim();
            if (q.Length == 0) return true;
            return (link.Title ?? string.Empty).IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0
                || (link.Url ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (link.Category ?? string.Empty).IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { LinksView?.Refresh(); }

        private void AddLink_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LinkEditorWindow { Owner = this };
            if (dialog.ShowDialog() != true) return;
            Links.Add(new LinkItem(dialog.LinkTitle, dialog.LinkUrl, dialog.LinkCategory));
            SaveLinks();
            LinksView.Refresh();
            UpdateStats();
        }

        private void EditLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null) return;
            var dialog = new LinkEditorWindow(link.Title, link.Url, link.Category, true) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            link.Title = dialog.LinkTitle;
            link.Url = dialog.LinkUrl;
            link.Category = dialog.LinkCategory;
            SaveLinks();
            LinksView.Refresh();
            UpdateStats();
        }

        private void DeleteLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null) return;
            if (MessageBox.Show("این لینک حذف شود؟", "حذف لینک", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Links.Remove(link);
            SaveLinks();
            LinksView.Refresh();
            UpdateStats();
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
            draggedLink = (sender as Border)?.Tag as LinkItem;
        }

        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || draggedLink == null) return;
            Point current = e.GetPosition(null);
            Vector diff = dragStartPoint - current;
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            DragDrop.DoDragDrop((DependencyObject)sender, draggedLink, DragDropEffects.Move);
        }

        private void Card_Drop(object sender, DragEventArgs e)
        {
            var target = (sender as Border)?.Tag as LinkItem;
            var source = e.Data.GetData(typeof(LinkItem)) as LinkItem;
            if (source == null || target == null || ReferenceEquals(source, target)) return;
            int oldIndex = Links.IndexOf(source);
            int newIndex = Links.IndexOf(target);
            if (oldIndex < 0 || newIndex < 0) return;
            Links.Move(oldIndex, newIndex);
            SaveLinks();
            LinksView.Refresh();
            draggedLink = null;
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null || string.IsNullOrWhiteSpace(link.Url)) return;
            try { Process.Start(new ProcessStartInfo(link.Url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("باز کردن لینک ممکن نشد.\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            var groups = Links.GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "عمومی" : x.Category)
                .OrderBy(x => x.Key).Select(x => x.Key + "  —  " + x.Count() + " لینک").ToArray();
            MessageBox.Show(groups.Length == 0 ? "هنوز دسته‌بندی‌ای وجود ندارد." : string.Join("\n", groups), "دسته‌بندی‌ها", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadLinks()
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                if (File.Exists(dataFile))
                {
                    var serializer = new XmlSerializer(typeof(ObservableCollection<LinkItem>));
                    using (var stream = File.OpenRead(dataFile))
                    {
                        var saved = serializer.Deserialize(stream) as ObservableCollection<LinkItem>;
                        if (saved != null) { foreach (var item in saved) Links.Add(item); return; }
                    }
                }
            }
            catch { }
            Links.Add(new LinkItem("گوگل", "https://www.google.com", "جستجو"));
            Links.Add(new LinkItem("گیت‌هاب", "https://github.com", "توسعه"));
            Links.Add(new LinkItem("ChatGPT", "https://chatgpt.com", "هوش مصنوعی"));
            Links.Add(new LinkItem("یوتیوب", "https://www.youtube.com", "ویدئو"));
            SaveLinks();
        }

        private void SaveLinks()
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                var serializer = new XmlSerializer(typeof(ObservableCollection<LinkItem>));
                using (var stream = File.Create(dataFile)) serializer.Serialize(stream, Links);
            }
            catch (Exception ex) { MessageBox.Show("ذخیره اطلاعات انجام نشد.\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void LoadSettings()
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                if (!File.Exists(settingsFile)) return;
                foreach (var line in File.ReadAllLines(settingsFile))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    bool value;
                    if (!bool.TryParse(parts[1], out value)) continue;
                    if (parts[0] == "Topmost") Topmost = value;
                    else if (parts[0] == "ShowSeconds") showSeconds = value;
                    else if (parts[0] == "CompactMode") compactMode = value;
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                File.WriteAllLines(settingsFile, new[] { "Topmost=" + Topmost, "ShowSeconds=" + showSeconds, "CompactMode=" + compactMode });
            }
            catch { }
        }

        private void ApplySettings()
        {
            CardWidth = compactMode ? 205 : 236;
            LayoutModeText.Text = compactMode ? "فشرده" : "استاندارد";
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString(showSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture);
            DateText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}", persianCalendar.GetYear(now), persianCalendar.GetMonth(now), persianCalendar.GetDayOfMonth(now));
        }

        private void UpdateStats()
        {
            int categories = Links.Select(x => string.IsNullOrWhiteSpace(x.Category) ? "عمومی" : x.Category).Distinct().Count();
            CountText.Text = Links.Count + " لینک";
            CategoryCountText.Text = categories + " دسته";
            DashboardLinksText.Text = Links.Count.ToString(CultureInfo.InvariantCulture);
            DashboardCategoriesText.Text = categories.ToString(CultureInfo.InvariantCulture);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window { Title = "تنظیمات لینک‌برد", Width = 390, Height = 310, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = (System.Windows.Media.Brush)Application.Current.Resources["PanelBrush"], FlowDirection = FlowDirection.RightToLeft };
            var panel = new StackPanel { Margin = new Thickness(22) };
            panel.Children.Add(new TextBlock { Text = "تنظیمات نمایش", FontSize = 21, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 18) });
            var topmostBox = new CheckBox { Content = "همیشه روی سایر پنجره‌ها باشد", IsChecked = Topmost, Margin = new Thickness(0, 6, 0, 6) };
            var secondsBox = new CheckBox { Content = "نمایش ثانیه در ساعت", IsChecked = showSeconds, Margin = new Thickness(0, 6, 0, 6) };
            var compactBox = new CheckBox { Content = "چیدمان فشرده کارت‌ها", IsChecked = compactMode, Margin = new Thickness(0, 6, 0, 6) };
            panel.Children.Add(topmostBox); panel.Children.Add(secondsBox); panel.Children.Add(compactBox);
            var saveButton = new Button { Content = "ذخیره تنظیمات", Background = (System.Windows.Media.Brush)Application.Current.Resources["AccentBrush"], Margin = new Thickness(0, 22, 0, 0) };
            saveButton.Click += (s, args) => { Topmost = topmostBox.IsChecked == true; showSeconds = secondsBox.IsChecked == true; compactMode = compactBox.IsChecked == true; ApplySettings(); SaveSettings(); dialog.DialogResult = true; dialog.Close(); };
            panel.Children.Add(saveButton); dialog.Content = panel; dialog.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Persian LinkBoard\nنسخه 0.4\nفرم حرفه‌ای لینک + نشانه سایت + Drag & Drop + Vazirmatn\nWindows 8.1 / 10 / 11", "درباره برنامه", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
