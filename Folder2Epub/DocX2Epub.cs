using EpubSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using SysImg = System.Drawing.Imaging;
using System.Xml.Linq;

namespace Folder2Epub
{
    public class DocX2Epub : BaseFolder2Epub
    {
        public DocX2Epub(string settingsPath) : base(settingsPath, "docx")
        {

        }
        public DocX2Epub(Settings settings) : base(settings, "docx")
        {

        }

        public override string ConvertToHtml(FileInfo file, EpubWriter writer)
        {
            this.LastStatus = $"Processing File: {file.Name}";
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
    }
}
