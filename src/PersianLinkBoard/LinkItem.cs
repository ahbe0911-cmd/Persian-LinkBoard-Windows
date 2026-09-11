using System;
using System.Xml.Serialization;

namespace PersianLinkBoard
{
    [Serializable]
    public class LinkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public bool IsPinned { get; set; }

        [XmlIgnore]
        public string PinGlyph { get { return IsPinned ? "★" : "☆"; } }

        [XmlIgnore]
        public string DisplayInitial
        {
            get
            {
                var value = (Title ?? string.Empty).Trim();
                return value.Length == 0 ? "↗" : value.Substring(0, 1).ToUpperInvariant();
            }
        }

        [XmlIgnore]
        public string HostName
        {
            get
            {
                Uri uri;
                if (Uri.TryCreate(Url, UriKind.Absolute, out uri))
                {
                    var host = uri.Host ?? string.Empty;
                    if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);
                    return host;
                }
                return Url ?? string.Empty;
            }
        }

        public LinkItem() { }

        public LinkItem(string title, string url, string category)
        {
            Title = title;
            Url = url;
            Category = category;
        }
    }
}
