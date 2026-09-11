using System;
using System.Windows;

namespace PersianLinkBoard
{
    public partial class LinkEditorWindow : Window
    {
        public string LinkTitle { get; private set; }
        public string LinkUrl { get; private set; }
        public string LinkCategory { get; private set; }

        public LinkEditorWindow(string title = "", string url = "", string category = "عمومی", bool isEdit = false)
        {
            InitializeComponent();
            HeadingText.Text = isEdit ? "ویرایش لینک" : "افزودن لینک جدید";
            Title = isEdit ? "ویرایش لینک" : "افزودن لینک";
            TitleBox.Text = title ?? string.Empty;
            UrlBox.Text = string.IsNullOrWhiteSpace(url) ? "https://" : url;
            CategoryBox.Text = string.IsNullOrWhiteSpace(category) ? "عمومی" : category;
            Loaded += (s, e) =>
            {
                TitleBox.Focus();
                TitleBox.SelectAll();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var title = (TitleBox.Text ?? string.Empty).Trim();
            var url = (UrlBox.Text ?? string.Empty).Trim();
            var category = (CategoryBox.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ShowError("عنوان لینک نمی‌تواند خالی باشد.");
                TitleBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(url) || url == "https://")
            {
                ShowError("آدرس سایت را وارد کنید.");
                UrlBox.Focus();
                return;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                ShowError("آدرس واردشده معتبر نیست. نمونه: https://example.com");
                UrlBox.Focus();
                return;
            }

            LinkTitle = title;
            LinkUrl = parsed.AbsoluteUri;
            LinkCategory = string.IsNullOrWhiteSpace(category) ? "عمومی" : category;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}
