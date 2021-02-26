using EpubSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SysImg = System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Folder2Epub
{
    public abstract class BaseFolder2Epub
    {
        #region Properties
        public Settings Settings { get; set; }
        public string GenerationLog { get; private set; }
        private string _LastStatus;
        public string LastStatus {
            get { return _LastStatus; }
            set {
                _LastStatus = value;
                GenerationLog += value + " \n";
                OnUpdate(value);
            } }
        #endregion
        public event Action<string> OnUpdate = delegate { };
        public event Action<int, int> OnProgress = delegate { };
        internal string FileType;

        public BaseFolder2Epub(string settingsPath, string fileType)
        {
            this.Settings = GetSettings(settingsPath);
            this.FileType = fileType;
        }

        public BaseFolder2Epub(Settings settings, string fileType)
        {
            this.Settings = settings;
            this.FileType = fileType;
        }

        #region Abstract methods
        public abstract string ConvertToHtml(FileInfo file, EpubWriter writer);
        #endregion

        #region Virtual Methods

        internal virtual IEnumerable<string> CustomSort(IEnumerable<string> list)
        {
            if (list.Count() == 0)
            {
                return list;
            }
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'), RegexOptions.Compiled)
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }
        internal virtual Settings GetSettings(string settingsPath)
        {
            Settings settings = null;

            if (!File.Exists(settingsPath))
            {
                settings = new Settings()
                {
                    Author = "Author name goes here",
                    Title = Path.GetFileName(Environment.CurrentDirectory),
                    Path = "./",
                    Cover = "cover.png",
                    WaitAtEnd = true
                };

                LastStatus = "No settings detected. Creating default settings file at location.";
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                LastStatus = $"Settings file created. {settingsPath} Please edit the file and run again to generate.";
                return null;
            }
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
            return settings;
        }
        #endregion

        public void GenerateEpub()
        {
            var files = GetFiles(this.Settings);
            GenerateEpub(this.Settings, files);
            this.LastStatus = "Generation Complete";
        }
        public void GenerateEpub(params string[] files)
        {
            GenerateEpub(this.Settings, files);
            this.LastStatus = "Generation Complete";
        }

        public void GenerateEpub(Settings options, string[] files)
        {
            EpubWriter writer = new EpubWriter();
            writer.AddAuthor(options.Author);

            if (!string.IsNullOrWhiteSpace(options.Cover))
            {
                using (var ms = new MemoryStream())
                {
                    try
                    {
                        var img = Image.FromFile(options.GetPathStr(options.Cover), true);
                        img.Save(ms, img.RawFormat);
                        writer.SetCover(ms.ToArray(), ImageFormat.Png);
                        writer.AddChapter("Cover", GeneratePageTemplate("<img src='cover.png' />"));
                    }
                    catch (FileNotFoundException e)
                    {
                        LastStatus = $"Failed to find Cover Image at {options.GetPathStr(options.Cover)}. Defaulting to blank cover image. If you have a proper cover, please provide a relative path to it in the settings json file";
                        var img = new Bitmap(1, 1);
                        img.SetPixel(0, 0, Color.White);
                        img.Save(ms, SysImg.ImageFormat.Png);
                        writer.SetCover(ms.ToArray(), ImageFormat.Png);
                    }
                }
            }
            writer.SetTitle(options.Title);

            int maxLength = files.Length * 2 + 1;

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                FileInfo info = new FileInfo(file);
                string text = string.Empty;
                try
                {
                    OnProgress(i * 2, maxLength);
                    text = ConvertToHtml(info, writer);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to convert file: {file}", ex);
                }
                writer.AddChapter(GetChapterName(info, text), text);
                OnProgress(i * 2 + 1, maxLength);
            }

            if (files.Length == 0)
            {
                string path = Path.GetDirectoryName(options.Path);
                if (path == ".") path = Environment.CurrentDirectory;
                LastStatus = $"No {FileType} files found in {path}";
            }

            writer.Write(options.GetTitlePath());
            OnProgress(maxLength, maxLength);
        }

        public virtual string GetChapterName(FileInfo info, string text)
        {
            return info.Name.Replace($".{FileType}", "");
        }

        public string GeneratePageTemplate(string body)
        {
            string returnValue =
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"" lang=""en"">
  <head>
  <meta charset=""UTF-8"" />
  <title></title>
  <link rel=""stylesheet"" type=""text/css"" href=""style.css"" />
  </head>
	<body>
";
            returnValue += body;

            returnValue += @"
	</body>
</html>
";
            return returnValue;
        }


        public string[] GetFiles()
        {
            return GetFiles(this.Settings);
        }
        #region internal Methods

        internal string[] GetFiles(Settings settings)
        {
            return GetFiles(settings.Path);
        }

        //Technically overridable, but future use case would probably be better implemented through modification.
        internal virtual string[] GetFiles(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            string[] files = directory.GetFiles("*."+FileType, SearchOption.TopDirectoryOnly)
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                .Select(f => f.FullName).ToArray();

            return CustomSort(files).ToArray();
        }

        #endregion

    }

}
