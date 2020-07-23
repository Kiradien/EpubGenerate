using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DocXFolderToEpub
{
    public class Settings
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }

        public string Path { get; set; }

        public bool WaitAtEnd { get; set; }

        public string GetPathStr(string item)
        {
            if (item.IndexOf(":") >= 0)
            {
                return item;
            }
            return $"{Path}/{item}";
        }
        public string GetTitleForPath() => Regex.Replace(Title, @"/[^\w\s()-]/gi", "", RegexOptions.Compiled);
        public string GetTitlePath()
        {
            string outputDir = $"{Path}/output/";
            System.IO.Directory.CreateDirectory(outputDir);
            return $"{outputDir}{GetTitleForPath()}.epub";
        }
    }
}
