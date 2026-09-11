using Microsoft.VisualBasic;
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
using System.Windows.Threading;
using System.Xml.Serialization;

namespace PersianLinkBoard
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersianLinkBoard");
        private readonly string dataFile;
        private readonly PersianCalendar persianCalendar = new PersianCalendar();
        private readonly DispatcherTimer clockTimer;

        public ObservableCollection<LinkItem> Links { get; } = new ObservableCollection<LinkItem>();
        public ICollectionView LinksView { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            dataFile = Path.Combine(dataFolder, "links.xml");

            LoadLinks();
            LinksView = CollectionViewSource.GetDefaultView(Links);
            LinksView.Filter = FilterLink;
            DataContext = this;
            UpdateCount();
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LinksView?.Refresh();
        }

        private void AddLink_Click(object sender, RoutedEventArgs e)
        {
            string title = Interaction.InputBox("نام لینک را وارد کنید:", "افزودن لینک", "لینک جدید");
            if (string.IsNullOrWhiteSpace(title)) return;

            string url = Interaction.InputBox("آدرس کامل لینک را وارد کنید:", "افزودن لینک", "https://");
            if (string.IsNullOrWhiteSpace(url)) return;
            url = NormalizeUrl(url);

            string category = Interaction.InputBox("دسته‌بندی را وارد کنید:", "افزودن لینک", "عمومی");
            if (string.IsNullOrWhiteSpace(category)) category = "عمومی";

            Links.Add(new LinkItem(title.Trim(), url.Trim(), category.Trim()));
            SaveLinks();
            LinksView.Refresh();
            UpdateCount();
        }

        private void EditLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null) return;

            string title = Interaction.InputBox("نام لینک:", "ویرایش لینک", link.Title ?? string.Empty);
            if (string.IsNullOrWhiteSpace(title)) return;
            string url = Interaction.InputBox("آدرس لینک:", "ویرایش لینک", link.Url ?? string.Empty);
            if (string.IsNullOrWhiteSpace(url)) return;
            string category = Interaction.InputBox("دسته‌بندی:", "ویرایش لینک", link.Category ?? "عمومی");

            link.Title = title.Trim();
            link.Url = NormalizeUrl(url.Trim());
            link.Category = string.IsNullOrWhiteSpace(category) ? "عمومی" : category.Trim();

            SaveLinks();
            LinksView.Refresh();
        }

        private void DeleteLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null) return;
            if (MessageBox.Show("این لینک حذف شود؟", "حذف لینک", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            Links.Remove(link);
            SaveLinks();
            LinksView.Refresh();
            UpdateCount();
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            var link = (sender as Button)?.Tag as LinkItem;
            if (link == null || string.IsNullOrWhiteSpace(link.Url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(link.Url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("باز کردن لینک ممکن نشد.\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string NormalizeUrl(string url)
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
            return "https://" + url;
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
                        if (saved != null)
                        {
                            foreach (var item in saved) Links.Add(item);
                            return;
                        }
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
            catch (Exception ex)
            {
                MessageBox.Show("ذخیره اطلاعات انجام نشد.\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            DateText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}", persianCalendar.GetYear(now), persianCalendar.GetMonth(now), persianCalendar.GetDayOfMonth(now));
        }

        private void UpdateCount()
        {
            CountText.Text = Links.Count + " لینک";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("بخش تنظیمات در نسخه بعدی شامل تم، اندازه کارت‌ها، رفتار باز شدن لینک و مدیریت دسته‌بندی‌ها خواهد بود.", "تنظیمات", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Persian LinkBoard\nنسخه 0.1\nلانچر لینک‌های کاربردی برای ویندوز", "درباره برنامه", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
