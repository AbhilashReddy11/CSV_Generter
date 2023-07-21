using CSV_Generter.Models;
using Microsoft.AspNetCore.Mvc;

using System.Diagnostics;

using System.Text;

using Microsoft.Net.Http.Headers;

using Newtonsoft.Json;

namespace CSV_Generter.Controllers

{
    public class HomeController : Controller
    {
        private const string API_KEY = "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";
        private static readonly HttpClient client = new HttpClient();
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
    }

}