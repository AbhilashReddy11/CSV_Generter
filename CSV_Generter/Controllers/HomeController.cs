using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using PdfSharp.Drawing;

using System.IO.Compression;
using CSV_Generter.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Net.Mime;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Ionic.Zip;
using ZipFile = Ionic.Zip.ZipFile;
using PdfWriter = iTextSharp.text.pdf.PdfWriter;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;

namespace CSV_Generter.Controllers
{
    public class HomeController : Controller
    {
        private const string API_KEY = "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";
        private static readonly HttpClient client = new HttpClient();


        private readonly IWebHostEnvironment _hostingEnvironment;

        //public HomeController(IWebHostEnvironment hostingEnvironment)
        //{
        //    _hostingEnvironment = hostingEnvironment;
        //}
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            var model = new InputOutputModel();

            if (TempData["ChatHistory"] != null)
            {
                var chatHistory = (List<string>)TempData["ChatHistory"];
                model.Prompt = chatHistory[chatHistory.Count - 2].Substring(6);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeFile(InputOutputModel model, List<IFormFile> files)
        {
            try
            {
                if (files != null && files.Count > 0)
                {
                    var invalidFiles = files.Where(file => !file.FileName.EndsWith(".csv")).ToList();
                    if (invalidFiles.Count > 0)
                    {
                        ModelState.AddModelError("", "Please upload only CSV files.");
                        return View("Index", model);
                    }

                    // Prepare the data for OpenAI API request
                    var fileContents = new StringBuilder();
                    foreach (var file in files)
                    {
                        using (var reader = new StreamReader(file.OpenReadStream()))
                        {
                            fileContents.AppendLine($"{file.FileName}: {reader.ReadToEnd()}");
                        }
                    }

                    // Use the CSV file contents in the prompt
                    var options = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new
                            {
                                role = "system",
                                content = "File Analysis"
                            },
                            new
                            {
                                role = "user",
                                content = $"{fileContents}\n{model.Prompt}\nnote: give only response which only has result.dont give process,description and explaination"
                            }
                        },
                        max_tokens = 3500,
                        temperature = 0.2
                    };

                    var json = JsonConvert.SerializeObject(options);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    string result = jsonResponse.choices[0].message.content;

                    model.Response = result;

                    if (TempData["ChatHistory"] == null)
                    {
                        TempData["ChatHistory"] = new List<string>();
                    }

                    var chatHistory = (List<string>)TempData["ChatHistory"];
                    chatHistory.Add($"{GetCsvFileNamesAsString(files)}");
                    chatHistory.Add($"User: {model.Prompt}");
                    chatHistory.Add($"AI: {result}");

                    TempData["GeneratedResponse"] = result;

                    return View("Index", model);
                }
                else
                {
                    // No files uploaded, use model.Prompt directly
                    var options = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new
                            {
                                role = "system",
                                content = "File Analysis"
                            },
                            new
                            {
                                role = "user",
                                content = $"{model.Prompt}\nnote: give only response which only has result.dont give process,description and explaination"
                            }
                        },
                        max_tokens = 3500,
                        temperature = 0.2
                    };

                    var json = JsonConvert.SerializeObject(options);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    string result = jsonResponse.choices[0].message.content;

                    model.Response = result;

                    if (TempData["ChatHistory"] == null)
                    {
                        TempData["ChatHistory"] = new List<string>();
                    }

                    var chatHistory = (List<string>)TempData["ChatHistory"];
                    chatHistory.Add($"User: {model.Prompt}");
                    chatHistory.Add($"AI: {result}");

                    TempData["GeneratedResponse"] = result;

                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content("An error occurred during file analysis.");
            }
        }

        private string GetCsvFileNamesAsString(List<IFormFile> files)
        {
            var fileNames = files.Select(file => file.FileName).ToList();
            return string.Join(", ", fileNames);
        }

        public IActionResult DownloadResponse()
        {
            if (TempData.ContainsKey("GeneratedResponse"))
            {
                var responseContent = TempData["GeneratedResponse"] as string;
                if (!string.IsNullOrEmpty(responseContent))
                {
                    var responseFileName = $"{Guid.NewGuid().ToString()}.csv";

                    var contentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileNameStar = responseFileName,
                        FileName = responseFileName
                    };
                    Response.Headers.Add(HeaderNames.ContentDisposition, contentDisposition.ToString());

                    // Set the content type for CSV download
                    Response.ContentType = "text/csv";

                    // Convert the response content to CSV format (assuming it is already in CSV format)
                    var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));
                    return new FileStreamResult(responseStream, "text/csv");
                }
            }

            return Content("Response not found.");
        }


        public IActionResult DownloadPdfs(string Response)
        {
            // Create a configuration to handle reading the CSV data
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);

            // Create a CSV reader
            using (var reader = new StringReader(Response))
            using (var csv = new CsvReader(reader, config))
            {
                // Read the CSV data and convert it to a list of dictionaries
                var records = csv.GetRecords<dynamic>();

                // Create a list to store the PDF file paths
                var pdfFiles = new List<string>();

                // Process each row and generate PDFs with column-value pairs
                foreach (var record in records)
                {
                    // Generate the PDF content from column-value pairs
                    var pdfContent = new StringBuilder();

                    foreach (var property in ((IDictionary<string, object>)record))
                    {
                        var column = property.Key;
                        var value = property.Value;

                        pdfContent.AppendLine($"{column}: {value}");
                    }

                    // Generate the PDF from the content and store it in a temporary folder
                    byte[] pdfBytes = GeneratePdfFromText(pdfContent.ToString());
                    string fileName = $"{Guid.NewGuid().ToString()}.pdf";
                    string tempFolder = Path.Combine(Path.GetTempPath(), "PDFs");
                    Directory.CreateDirectory(tempFolder);
                    string filePath = Path.Combine(tempFolder, fileName);
                    System.IO.File.WriteAllBytes(filePath, pdfBytes);
                    pdfFiles.Add(filePath);
                }

                // Create a memory stream to hold the ZIP file
                using (MemoryStream zipStream = new MemoryStream())
                {
                    // Create a ZIP archive and add the PDFs to it
                    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var pdfFile in pdfFiles)
                        {
                            string entryName = Path.GetFileName(pdfFile);
                            zip.CreateEntryFromFile(pdfFile, entryName);
                        }
                    }

                    // Set the Content-Disposition header to suggest the file name
                    string zipFileName = "PDFs.zip";

                    // Return the ZIP file as a file for download
                    return File(zipStream.ToArray(), "application/zip", zipFileName);
                }
            }
        }


            private byte[] GeneratePdfFromText(string text)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document();
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);

                doc.Open();
                doc.Add(new Paragraph(text));
                doc.Close();

                return ms.ToArray();
            }
        }

    }
}

