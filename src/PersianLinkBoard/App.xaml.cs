using System;
using System.Collections.Generic;
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
            MergeHtmlDatasetOnce();
            base.OnStartup(e);
        }

        private static void MergeHtmlDatasetOnce()
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersianLinkBoard");
                var file = Path.Combine(folder, "links.xml");
                var marker = Path.Combine(folder, "html-seed-v1.done");
                Directory.CreateDirectory(folder);

                if (File.Exists(marker)) return;

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

                var keys = new HashSet<string>(links.Select(BuildKey), StringComparer.OrdinalIgnoreCase);
                foreach (var item in HtmlSeedData.Create())
                {
                    if (keys.Add(BuildKey(item))) links.Add(item);
                }

                using (var stream = File.Create(file)) serializer.Serialize(stream, links);
                File.WriteAllText(marker, "ok");
            }
            catch
            {
                // MainWindow keeps its normal fallback behavior if migration fails.
            }
        }

        private static string BuildKey(LinkItem item)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var url = (item.Url ?? string.Empty).Trim().TrimEnd('/');
            var category = (item.Category ?? string.Empty).Trim();
            return title + "\n" + url + "\n" + category;
        }
    }
}
