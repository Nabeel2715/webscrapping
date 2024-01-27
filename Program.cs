using Abot2.Poco;

using AbotX2.Crawler;

using HtmlAgilityPack;

using MongoDB.Driver;

using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

using System.Text.RegularExpressions;
using System.Xml;

using static System.Net.Mime.MediaTypeNames;

namespace scraper
{

    public class CustomHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(200);

            return httpClient;
        }
    }

    internal class Program
    {

        private static string ChatGPTAPIKey { get; set; } = "YOUR_API_KEY";


        static async Task Main(string[] args)
        {


            await CrawlWebsite();

            var connectionString = "mongodb://localhost:27017/";

            if (connectionString == null)
            {
                Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
                Environment.Exit(0);
            }
            var client = new MongoClient(connectionString);

            var db = ScraperContext.Create(client.GetDatabase("scraperdb"));




            List<KeyValuePair<string, string>> imgScrs = new List<KeyValuePair<string, string>>();

            foreach (HTMLDocument document in db.HTMLDocuments.Where(a => !string.IsNullOrEmpty(a.FullHTML)))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(document.FullHTML);
                var nodes = doc.DocumentNode.SelectNodes(@"//img[@src]");
                if (nodes != null)
                    foreach (var img in nodes)
                    {
                        imgScrs.Add(new KeyValuePair<string, string>(document.RelativePath, img.GetAttributeValue("src", "NONE")));
                    }
            }

            var a = imgScrs.Distinct().ToList();




            //int i = 1;

            //int total = db.HTMLDocuments.Count(a => !string.IsNullOrEmpty(a.ArticleText) && string.IsNullOrEmpty(a.ExtractedArticleTextInMarkdownFormat));
            //foreach (HTMLDocument document in db.HTMLDocuments.Where(a => !string.IsNullOrEmpty(a.ArticleText) && string.IsNullOrEmpty(a.ExtractedArticleTextInMarkdownFormat)))
            //{
            //    Console.WriteLine($"Currently on #{i++} out of {total} : " + document.RelativePath);

            //    try
            //    {
            //        document.ExtractedArticleTextInMarkdownFormat = await PromptChatGPT("", "Read the following text extracted from the HTML page and extract the article text from it but format the text in markdown format:\n\n\n" + document.ArticleText);

            //        db.SaveChanges();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
        }
        public async static Task<string> PromptChatGPT(string systemPrompt, string userPrompt)
        {
            OpenAIAPI api = new OpenAIAPI(ChatGPTAPIKey); // shorthand

            api.HttpClientFactory = new CustomHttpClientFactory();


            var chat = api.Chat.CreateConversation();

            ChatRequest cr = new ChatRequest()
            {
                Model = Model.ChatGPTTurbo_16k,
                Temperature = 0.9,
                Messages = new ChatMessage[]
                {
                                new ChatMessage(ChatMessageRole.System, systemPrompt),
                                new ChatMessage(ChatMessageRole.User, userPrompt)
                }
            };

            var result = await api.Chat.CreateChatCompletionAsync(cr);

            if (result.Choices.Any())
                return result.Choices.First().Message.Content;

            return "";
        }

        static List<HTMLDocument> GetHtmlFiles(string directory)
        {
            List<HTMLDocument> htmlFiles = new List<HTMLDocument>();
            List<string> fileEntries = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Where(a => Path.GetExtension(a).ToLower().Contains(".htm") || Path.GetExtension(a).ToLower().Contains(".php")).ToList();

            foreach (var filePath in fileEntries)
            {
                string relativePath = GetRelativePath(directory, filePath);
                string content = File.ReadAllText(filePath);

                htmlFiles.Add(new HTMLDocument
                {
                    RelativePath = relativePath,
                    FullHTML = content
                });
            }

            return htmlFiles;
        }

        static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            return baseUri.MakeRelativeUri(fullUri).ToString();
        }

        public static async Task CrawlWebsite()
        {
            CrawlerX crawler = new CrawlerX(new AbotX2.Poco.CrawlConfigurationX()
            {
                CrawlTimeoutSeconds = 30,
                MaxConcurrentThreads = 10,
                HttpRequestTimeoutInSeconds = 60,
                MaxCrawlDepth = 10,
                MaxPagesToCrawl = 10
            });

            crawler.PageCrawlCompleted += Crawler_PageCrawlCompleted;

            var result = await crawler.CrawlAsync(new Uri("https://web.archive.org/web/20060702212644/http://shakefire.com/"));
        }

        private static void Crawler_PageCrawlCompleted(object? sender, Abot2.Crawler.PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            var connectionString = "mongodb://localhost:27017/";

            if (connectionString == null)
            {
                Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
                Environment.Exit(0);
            }
            var client = new MongoClient(connectionString);

            var db = ScraperContext.Create(client.GetDatabase("scraperdb"));
            db.HTMLDocuments.Add(new HTMLDocument()
            {
                Id = Guid.NewGuid(),
                FullHTML = crawledPage.Content.Text,
                RelativePath = crawledPage.Uri.AbsolutePath,
            });
            db.SaveChanges();

        }
    }
}
