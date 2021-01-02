﻿using EpubSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Folder2Epub
{
    public class Txt2Epub : BaseFolder2Epub
    {
        public Txt2Epub(string settings) : base(settings, "txt")
        {

        }
        public Txt2Epub(Settings settings) : base(settings, "txt")
        {

        }

        public override string ConvertToHtml(FileInfo file, EpubWriter writer)
        {
            this.LastStatus = $"Processing File: {file.Name}";
            string chapterName = file.Name.Substring(0, file.Name.LastIndexOf('.'));
            string[] lines = File.ReadAllLines(file.FullName);
            string html = this.GeneratePageTemplate($"<h2>{chapterName}</h2>" + String.Join("\n", lines.Select(item => $"<p>{item}</p>")));
            return html;
        }
    }
}
