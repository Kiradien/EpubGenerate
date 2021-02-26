using EpubSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Folder2Epub
{
    public class Html2Epub : BaseFolder2Epub
    {
        public Html2Epub(string settings) : this(settings, "html")
        {

        }
        public Html2Epub(Settings settings) : this(settings, "html")
        {

        }

        public Html2Epub(string settings, string ext) : base(settings, ext)
        {

        }
        public Html2Epub(Settings settings, string ext) : base(settings, ext)
        {

        }

        public override string ConvertToHtml(FileInfo file, EpubWriter writer)
        {
            this.LastStatus = $"Processing File: {file.Name}";
            string[] lines = File.ReadAllLines(file.FullName);
            string html;

            if (lines.Any(item => item.IndexOf("<html") > -1) && lines.Any(item => item.IndexOf("</html") > -1))
            {
                html = String.Join("\n", lines.Select(item => $"{item}"));
            }
            else
            {
                string chapterName = file.Name.Substring(0, file.Name.LastIndexOf('.'));
                html = this.GeneratePageTemplate($"<h2>{chapterName}</h2>" + String.Join("\n", lines.Select(item => $"<p>{item}</p>")));
            }
            return html;
        }

        public override string GetChapterName(FileInfo info, string text)
        {
            //var wb = new WebBrowser();
            var matches = new Regex("<title>[\\s\\n]*(.*)[\\s\\n]*</title>", RegexOptions.IgnoreCase).Match(text);
            if (matches.Groups.Count > 1 && matches.Groups[1].Value.Trim().Length > 0)
            {
                return matches.Groups[1].Value;
            }
            return base.GetChapterName(info, text);
        }
    }
}
