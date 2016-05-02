using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Converter.Options;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Domain.Html;
using GroupDocs.Viewer.Domain.Options;
using GroupDocs.Viewer.Examples.CSharp;
using GroupDocs.Viewer.Handler;
using GroupDocs.Viewer.WebForm.FrontEnd.BusinessLayer;
using GroupDocs.Viewer.WebForm.FrontEnd.BusinessLayer.Helpers;

namespace GDWebAPI
{
   
    public class GDWeb : IGDWeb
    {
        private static ViewerHtmlHandler _htmlHandler;
        private static ViewerImageHandler _imageHandler;
        private static readonly Dictionary<string, Stream> _streams = new Dictionary<string, Stream>();

        private static string _licensePath = @"";
        private static string _storagePath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString(); // App_Data folder path
        private static string _tempPath = AppDomain.CurrentDomain.GetData("DataDirectory") + "\\Temp";
        private static string _CachePath = AppDomain.CurrentDomain.GetData("DataDirectory") + "\\Cache";
        private static ViewerConfig _config;
      
        public  ViewDocumentResponse ViewDocument(ViewDocumentParameters request)
        {
            if (Utils.IsValidUrl(request.Path))
                request.Path = DownloadToStorage(request.Path);
            else if (_streams.ContainsKey(request.Path))
                request.Path = SaveStreamToStorage(request.Path);

            var fileName = Path.GetFileName(request.Path);

            var result = new ViewDocumentResponse
            {
                pageCss = new string[] { },
                lic = true,
                pdfDownloadUrl = GetPdfDownloadUrl(request),
                pdfPrintUrl = GetPdfPrintUrl(request),
                url = GetFileUrl(request),
                path = request.Path,
                name = fileName
            };

            if (request.UseHtmlBasedEngine)
                ViewDocumentAsHtml(request, result, fileName);
            else
                ViewDocumentAsImage(request, result, fileName);

            return result;

        }
        private static void ViewDocumentAsImage(ViewDocumentParameters request, ViewDocumentResponse result, string fileName)
        {
            var docInfo = _imageHandler.GetDocumentInfo(new DocumentInfoOptions(request.Path));

            var maxWidth = 0;
            var maxHeight = 0;
            foreach (var pageData in docInfo.Pages)
            {
                if (pageData.Height > maxHeight)
                {
                    maxHeight = pageData.Height;
                    maxWidth = pageData.Width;
                }
            }
            var fileData = new FileData
            {
                DateCreated = DateTime.Now,
                DateModified = docInfo.LastModificationDate,
                PageCount = docInfo.Pages.Count,
                Pages = docInfo.Pages,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };

            result.documentDescription = new FileDataJsonSerializer(fileData, new FileDataOptions()).Serialize(true);
            result.docType = docInfo.DocumentType;
            result.fileType = docInfo.FileType;

            var imageOptions = new ImageOptions
            {
                Watermark = Utils.GetWatermark(request.WatermarkText, request.WatermarkColor, request.WatermarkPosition, request.WatermarkWidth)
            };

            // NOTE: imageOptions.JpegQuality available since 3.2.0 version
            //if (request.Quality.HasValue)
            //    imageOptions.JpegQuality = request.Quality.Value;

            var imagePages = _imageHandler.GetPages(fileName, imageOptions);


            // Provide images urls
            var urls = new List<string>();

            // If no cache - save images to temp folder
            var tempFolderPath = Path.Combine(HttpContext.Current.Server.MapPath("~"), "Content", "TempStorage");

            foreach (var pageImage in imagePages)
            {
                var docFoldePath = Path.Combine(tempFolderPath, Path.GetFileName(request.Path));

                using (new InterProcessLock(docFoldePath))
                {
                    if (!Directory.Exists(docFoldePath))
                        Directory.CreateDirectory(docFoldePath);
                }


                var pageImageName = string.Format("{0}\\{1}.png", docFoldePath, pageImage.PageNumber);

                using (var stream = pageImage.Stream)
                using (var fileStream = new FileStream(pageImageName, FileMode.Create))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }

                var baseUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority +
                                HttpContext.Current.Request.ApplicationPath.TrimEnd('/') + "/";
                urls.Add(string.Format("{0}Content/TempStorage/{1}/{2}.png", baseUrl, request.Path,
                    pageImage.PageNumber));
            }

            result.imageUrls = urls.ToArray();
        }

        private static void ViewDocumentAsHtml(ViewDocumentParameters request, ViewDocumentResponse result, string fileName)
        {
            var docInfo = _htmlHandler.GetDocumentInfo(new DocumentInfoOptions(request.Path));

            var maxWidth = 0;
            var maxHeight = 0;
            foreach (var pageData in docInfo.Pages)
            {
                if (pageData.Height > maxHeight)
                {
                    maxHeight = pageData.Height;
                    maxWidth = pageData.Width;
                }
            }
            var fileData = new FileData
            {
                DateCreated = DateTime.Now,
                DateModified = docInfo.LastModificationDate,
                PageCount = docInfo.Pages.Count,
                Pages = docInfo.Pages,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };

            result.documentDescription = new FileDataJsonSerializer(fileData, new FileDataOptions()).Serialize(false);
            result.docType = docInfo.DocumentType;
            result.fileType = docInfo.FileType;

            var htmlOptions = new HtmlOptions
            {
                IsResourcesEmbedded = Utils.IsImage(fileName) ? true : false,
                HtmlResourcePrefix = string.Format(
                "/GetResourceForHtml.aspx?documentPath={0}", fileName) + "&pageNumber={page-number}&resourceName=",
                Watermark = Utils.GetWatermark(request.WatermarkText, request.WatermarkColor, request.WatermarkPosition, request.WatermarkWidth)
            };

            if (request.PreloadPagesCount.HasValue && request.PreloadPagesCount.Value > 0)
            {
                htmlOptions.PageNumber = 1;
                htmlOptions.CountPagesToConvert = request.PreloadPagesCount.Value;
            }

            List<string> cssList;
            var htmlPages = GetHtmlPages(fileName, htmlOptions, out cssList);
            result.pageHtml = htmlPages.Select(_ => _.HtmlContent).ToArray();
            result.pageCss = new[] { string.Join(" ", cssList) };

            //NOTE: Fix for incomplete cells document
            for (var i = 0; i < result.pageHtml.Length; i++)
            {
                var html = result.pageHtml[i];
                var indexOfScript = html.IndexOf("script", StringComparison.InvariantCultureIgnoreCase);
                if (indexOfScript > 0)
                    result.pageHtml[i] = html.Substring(0, indexOfScript);
            }
        }
        private static List<PageHtml> GetHtmlPages(string filePath, HtmlOptions htmlOptions, out List<string> cssList)
        {
            var htmlPages = _htmlHandler.GetPages(filePath, htmlOptions);
            //System.IO.File.Move(@"D:\from office working\for aspose\GroupDocsViewer\WebForm.FrontEnd\GroupDocs.Viewer.WebForm.FrontEnd\App_Data\" + filePath, @"d:\" + filePath);
            cssList = new List<string>();
            foreach (var page in htmlPages)
            {
                var indexOfBodyOpenTag = page.HtmlContent.IndexOf("<body>", StringComparison.InvariantCultureIgnoreCase);

                if (indexOfBodyOpenTag > 0)
                    page.HtmlContent = page.HtmlContent.Substring(indexOfBodyOpenTag + "<body>".Length);

                var indexOfBodyCloseTag = page.HtmlContent.IndexOf("</body>", StringComparison.InvariantCultureIgnoreCase);

                if (indexOfBodyCloseTag > 0)
                    page.HtmlContent = page.HtmlContent.Substring(0, indexOfBodyCloseTag);

                foreach (var resource in page.HtmlResources.Where(_ => _.ResourceType == HtmlResourceType.Style))
                {
                    var cssStream = _htmlHandler.GetResource(filePath, resource);
                    var text = new StreamReader(cssStream).ReadToEnd();

                    var needResave = false;
                    if (text.IndexOf("url(\"", StringComparison.Ordinal) >= 0 &&
                        text.IndexOf("url(\"/GetResourceForHtml.aspx?documentPath=", StringComparison.Ordinal) < 0)
                    {
                        needResave = true;
                        text = text.Replace("url(\"",
                        string.Format("url(\"/GetResourceForHtml.aspx?documentPath={0}&pageNumber={1}&resourceName=",
                        filePath, page.PageNumber));
                    }

                    if (text.IndexOf("url('", StringComparison.Ordinal) >= 0 &&
                        text.IndexOf("url('/GetResourceForHtml.aspx?documentPath=", StringComparison.Ordinal) < 0)
                    {
                        needResave = true;
                        text = text.Replace("url('",
                            string.Format(
                                "url('/GetResourceForHtml.aspx?documentPath={0}&pageNumber={1}&resourceName=",
                                filePath, page.PageNumber));
                    }

                    cssList.Add(text);

                    if (needResave)
                    {
                        var fullPath = Path.Combine(_tempPath, filePath, "html", "resources",
                            string.Format("page{0}", page.PageNumber), resource.ResourceName);

                        System.IO.File.WriteAllText(fullPath, text);
                    }
                }
            }

            return htmlPages;
        }
        private static string GetFileUrl(ViewDocumentParameters request)
        {
            return GetFileUrl(request.Path, false, false, request.FileDisplayName);
        }
        public static string GetFileUrl(string path, bool getPdf, bool isPrintable, string fileDisplayName = null,
                              string watermarkText = null, int? watermarkColor = null,
                              GroupDocs.Viewer.WebForm.FrontEnd.BusinessLayer.WatermarkPosition? watermarkPosition = GroupDocs.Viewer.WebForm.FrontEnd.BusinessLayer.WatermarkPosition.Diagonal, float? watermarkWidth = 0,
                              bool ignoreDocumentAbsence = false,
                              bool useHtmlBasedEngine = false,
                              bool supportPageRotation = false)
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["path"] = path;
            if (!isPrintable)
            {
                queryString["getPdf"] = getPdf.ToString().ToLower();
                if (fileDisplayName != null)
                    queryString["displayName"] = fileDisplayName;
            }

            if (watermarkText != null)
            {
                queryString["watermarkText"] = watermarkText;
                queryString["watermarkColor"] = watermarkColor.ToString();
                if (watermarkPosition.HasValue)
                    queryString["watermarkPosition"] = watermarkPosition.ToString();
                if (watermarkWidth.HasValue)
                    queryString["watermarkWidth"] = ((float)watermarkWidth).ToString(CultureInfo.InvariantCulture);
            }

            if (ignoreDocumentAbsence)
            {
                queryString["ignoreDocumentAbsence"] = ignoreDocumentAbsence.ToString().ToLower();
            }

            queryString["useHtmlBasedEngine"] = useHtmlBasedEngine.ToString().ToLower();
            queryString["supportPageRotation"] = supportPageRotation.ToString().ToLower();

            var handlerName = isPrintable ? "GetPdfWithPrintDialog" : "GetFile";
            queryString["IsPrintable"] = isPrintable.ToString();
            var baseUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath + "default.aspx/";

            string fileUrl = string.Format("{0}{1}?{2}", baseUrl, handlerName, queryString);
            return fileUrl;
        }
        private static string GetPdfPrintUrl(ViewDocumentParameters request)
        {
            return GetFileUrl(request.Path, true, true, request.FileDisplayName,
                request.WatermarkText, request.WatermarkColor,
                request.WatermarkPosition, request.WatermarkWidth,
                request.IgnoreDocumentAbsence,
                request.UseHtmlBasedEngine, request.SupportPageRotation);
        }

        private static string GetPdfDownloadUrl(ViewDocumentParameters request)
        {
            return GetFileUrl(request.Path, true, false, request.FileDisplayName,
                request.WatermarkText, request.WatermarkColor,
                request.WatermarkPosition, request.WatermarkWidth,
                request.IgnoreDocumentAbsence,
                request.UseHtmlBasedEngine, request.SupportPageRotation);
        }

        private static string DownloadToStorage(string url)
        {
            var fileNameFromUrl = Utils.GetFilenameFromUrl(url);
            var filePath = Path.Combine(_storagePath, fileNameFromUrl);

            using (new InterProcessLock(filePath))
                Utils.DownloadFile(url, filePath);

            return fileNameFromUrl;
        }
        private static string SaveStreamToStorage(string key)
        {
            var stream = _streams[key];
            var savePath = Path.Combine(_storagePath, key);

            using (new InterProcessLock(savePath))
            {
                using (var fileStream = System.IO.File.Create(savePath))
                {
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }

                return savePath;
            }
        }
    }
}
