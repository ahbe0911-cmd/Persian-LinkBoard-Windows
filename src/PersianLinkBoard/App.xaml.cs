using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace PersianLinkBoard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            MergeHtmlDataset();
            base.OnStartup(e);
        }

        private static void MergeHtmlDataset()
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersianLinkBoard");
                var file = Path.Combine(folder, "links.xml");
                Directory.CreateDirectory(folder);

                var links = new ObservableCollection<LinkItem>();
                var serializer = new XmlSerializer(typeof(ObservableCollection<LinkItem>));
                if (File.Exists(file))
                {
                    using (var stream = File.OpenRead(file))
                    {
                        var saved = serializer.Deserialize(stream) as ObservableCollection<LinkItem>;
                        if (saved != null) links = saved;
                    }
                }

                foreach (var item in HtmlSeedData.Create())
                {
                    bool exists = links.Any(x =>
                        string.Equals((x.Title ?? string.Empty).Trim(), item.Title, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals((x.Url ?? string.Empty).TrimEnd('/'), (item.Url ?? string.Empty).TrimEnd('/'), StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((x.Category ?? string.Empty).Trim(), item.Category, StringComparison.CurrentCultureIgnoreCase));
                    if (!exists) links.Add(item);
                }

                using (var stream = File.Create(file)) serializer.Serialize(stream, links);
            }
            catch
            {
                // MainWindow keeps its existing fallback behavior if migration cannot run.
            }
        }
    }
}
