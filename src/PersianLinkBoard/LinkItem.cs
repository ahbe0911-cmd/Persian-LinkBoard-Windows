using System;

namespace PersianLinkBoard
{
    [Serializable]
    public class LinkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }

        public LinkItem() { }

        public LinkItem(string title, string url, string category)
        {
            Title = title;
            Url = url;
            Category = category;
        }
    }
}
