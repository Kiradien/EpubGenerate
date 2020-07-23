using DocumentFormat.OpenXml.Packaging;
using EpubSharp;
using OpenXmlPowerTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SysImg = System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DocXFolderToEpub
{
    class Program
    {
        //Need to run "msbuild /t:ILMerge" in VS CMD Prompt for integrated executable.
        static void Main(string[] args)
        {
            Console.WriteLine("Batch DocX to epub converter thrown together by Kiradien.");
            Console.WriteLine("This application will convert all docx files in a specified folder into a single epub. Folder is specified through json settings.");
            Console.WriteLine();
            Console.WriteLine($"Json Settings file can be passed as commandline argument, {Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}_settings.json by default.");
            Console.WriteLine("If the specified or default json file does not exist, it will be created when first run, defaulting to the current directory.");
            Console.WriteLine();
            Console.WriteLine("Please make any desired changes to the settings json file.");
            Console.WriteLine("Please direct any questions to Kiradien@gmail.com or @Kiradien#0001");
            Console.WriteLine("--------------------");
            Console.WriteLine();
            try
            {
                string settingsPath = $"{Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}_settings.json";
                if (args.Length > 0 && args[0].EndsWith("json"))
                {
                    settingsPath = args[0];
                }
                Settings settings = GetSettings(settingsPath);


                if (settings != null)
                {
                    GenerateEpub(settings);
                    Console.WriteLine();
                    Console.WriteLine("Processing Complete. Press any key to continue");
                }
                else
                {
                    Console.WriteLine("Please press any key to continue");
                }
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("-----------------------");
                Console.WriteLine(e.Message);
                Console.WriteLine("-----------------------");
                Console.WriteLine("An unhandled exception occurred while generating your epub.");
                Console.ReadKey();
            }
        }

        public static void GenerateEpub(Settings options)
        {
            EpubWriter writer = new EpubWriter();
            writer.AddAuthor(options.Author);

            using (var ms = new MemoryStream())
            {
                try
                {
                    var img = Image.FromFile(options.GetPathStr(options.Cover), true);
                    img.Save(ms, img.RawFormat);
                    writer.SetCover(ms.ToArray(), ImageFormat.Png);
                    writer.AddChapter("Cover", GeneratePage("<img src='cover.png' />"));
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine($"Failed to find Cover Image at {options.GetPathStr(options.Cover)}. Defaulting to blank cover image. If you have a proper cover, please provide a relative path to it in the settings json file");
                    Console.WriteLine();
                    var img = new Bitmap(1, 1);
                    img.SetPixel(0, 0, Color.White);
                    img.Save(ms, SysImg.ImageFormat.Png);
                    writer.SetCover(ms.ToArray(), ImageFormat.Png);
                }
            }
            writer.SetTitle(options.Title);

            var files = CustomSort(Directory.GetFiles(options.Path, "*.docx", SearchOption.TopDirectoryOnly)).ToArray();
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                writer.AddChapter(info.Name.Replace(".docx", ""), ConvertToHtml(info, writer));
            }

            if (files.Length == 0)
            {
                string path = Path.GetDirectoryName(options.Path);
                if (path == ".") path = Environment.CurrentDirectory;
                Console.WriteLine($"No docx files found in {path}");
            }

            writer.Write(options.GetTitlePath());
        }

        public static string ConvertToHtml(FileInfo file, EpubWriter writer)
        {
            Console.WriteLine($"Processing File: {file.Name}");
            byte[] byteArray = File.ReadAllBytes(file.FullName);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(byteArray, 0, byteArray.Length);
                using (WordprocessingDocument wDoc = WordprocessingDocument.Open(memoryStream, true))
                {
                    var pageTitle = file.FullName;
                    var part = wDoc.CoreFilePropertiesPart;
                    if (part != null)
                    {
                        pageTitle = (string)part.GetXDocument().Descendants(DC.title).FirstOrDefault() ?? file.FullName;
                    }

                    HtmlConverterSettings settings = new HtmlConverterSettings()
                    {
                        AdditionalCss = "body { margin: 1cm auto; max-width: 20cm; padding: 0; }",
                        PageTitle = pageTitle,
                        FabricateCssClasses = true,
                        CssClassPrefix = "pt-",
                        RestrictToSupportedLanguages = false,
                        RestrictToSupportedNumberingFormats = false,
                        ImageHandler = imageInfo =>
                        {
                            string extension = imageInfo.ContentType.Split('/')[1].ToLower();
                            SysImg.ImageFormat imageFormat = null;
                            if (extension == "png")
                                imageFormat = SysImg.ImageFormat.Png;
                            else if (extension == "gif")
                                imageFormat = SysImg.ImageFormat.Gif;
                            else if (extension == "bmp")
                                imageFormat = SysImg.ImageFormat.Bmp;
                            else if (extension == "jpeg")
                                imageFormat = SysImg.ImageFormat.Jpeg;
                            else if (extension == "tiff")
                            {
                                // Convert tiff to gif.
                                extension = "gif";
                                imageFormat = SysImg.ImageFormat.Gif;
                            }
                            else if (extension == "x-wmf")
                            {
                                extension = "wmf";
                                imageFormat = SysImg.ImageFormat.Wmf;
                            }

                            // If the image format isn't one that we expect, ignore it,
                            // and don't return markup for the link.
                            if (imageFormat == null)
                                return null;

                            string fileName = $"{Guid.NewGuid()}.{extension}";
                            string filePath = $"images/{fileName}";

                            using (var ms = new MemoryStream())
                            {
                                imageInfo.Bitmap.Save(ms, SysImg.ImageFormat.Png);
                                writer.AddFile(filePath, ms.ToArray(), EpubSharp.Format.EpubContentType.ImagePng);
                            }

                            XElement ximg = new XElement(Xhtml.img,
                                new XAttribute(NoNamespace.src, filePath),
                                imageInfo.ImgStyleAttribute,
                                imageInfo.AltText != null ?
                                    new XAttribute(NoNamespace.alt, imageInfo.AltText) : null);
                            return ximg;
                        }
                    };
                    XElement htmlElement = HtmlConverter.ConvertToHtml(wDoc, settings);

                    var html = new XDocument(
                        new XDocumentType("html", null, null, null),
                        htmlElement);


                    var htmlString = html.ToString(SaveOptions.DisableFormatting);
                    return htmlString;
                }
            }
        }

        public static string GeneratePage(string body)
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

        public static Settings GetSettings(string settingsPath)
        {
            Settings settings = null;

            if (!File.Exists(settingsPath))
            {
                settings = new Settings()
                {
                    Author = "Author name goes here",
                    Title = Path.GetFileName(Environment.CurrentDirectory),
                    Path = "./",
                    Cover = "cover.jpg",
                    WaitAtEnd = true
                };

                Console.WriteLine("No settings detected. Creating default settings file at location.");
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                Console.WriteLine($"Settings file created. {settingsPath} Please edit the file and run again to generate.");
                return null;
            }
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
            return settings;
        }

        public static IEnumerable<string> CustomSort(IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'), RegexOptions.Compiled)
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }
    }
}
