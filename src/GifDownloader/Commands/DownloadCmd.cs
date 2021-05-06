using GifDownloader.Models;
using GifDownloader.Settings;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GifDownloader.Commands
{
    [Command(Name = "download", Description = "download gifs from urls")]
    class DownloadCmd : GifDownloaderBaseCmd
    {
        [Option(CommandOptionType.SingleValue, ShortName = "if", LongName = "inputWithName", Description = "gif urls with name json file path", ValueName = "url with name json file path", ShowInHelpText = true)]
        public string InputWithName { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "i", LongName = "input", Description = "gif urls json file path", ValueName = "url json file path", ShowInHelpText = true)]
        public string Input { get; set; }
        [Option(CommandOptionType.MultipleValue, ShortName = "mu", LongName = "multiple url", Description = "gif multiple url", ValueName = "multiple url", ShowInHelpText = true)]
        public string[] MultipleUrl { get; set; }
        [Option(CommandOptionType.SingleValue, ShortName = "u", LongName = "url", Description = "gif single url", ValueName = "single url", ShowInHelpText = true)]
        public string SingleUrl { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "o", LongName = "output", Description = "output path", ValueName = "output path", ShowInHelpText = true)]
        public string Output { get; set; }

        public DownloadCmd(ILogger<DownloadCmd> logger, IConsole console, IOptions<GlobalSettings> settings)
        {
            _logger = logger;
            _console = console;
            _settings = settings;
        }
        private GifDownloaderCmd Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(InputWithName)       
                || string.IsNullOrEmpty(Input)       
                || MultipleUrl != null      
                || string.IsNullOrEmpty(SingleUrl))
            {
                Output = Prompt.GetString($"What is the path of output folder? default is ", _settings.Value.DefaultOutputPath);
            }
            try
            {   
                var urls = SetUrls();
                if (urls == null)
                {
                    return 1;
                }
                if (!Directory.Exists(Output))
                {
                    Directory.CreateDirectory(Output);
                }
                _logger.LogInformation($"{urls.Count} urls found");
                for (int i = 0; i < urls.Count; i++)
                {
                    var extension = Path.GetExtension(urls[i].Url);
                    if (extension.ToLower() == ".gif" || extension.ToLower() == ".gifv")
                    {
                        using (WebClient client = new WebClient())
                        {
                            urls[i].Url = urls[i].Url.Replace(".gifv", ".gif");
                            var filename = !string.IsNullOrEmpty(urls[i].Name) ? urls[i].Name + Path.GetExtension(urls[i].Url) : Path.GetFileName(urls[i].Url);
                            _logger.LogInformation($"{i+1}/{urls.Count}) {filename} download starting..." );
                            client.DownloadFile(new Uri(urls[i].Url), Output + "/" + filename);
                            _logger.LogInformation($"{i+1}/{urls.Count}) {filename} download finished...");
                        }
                    }
                    else
                    {
                        var srcList = FetchFromUrl(urls[i].Url);
                        for (int j = 0; j < srcList.Count; j++)
                        {
                            if (extension.ToLower() == ".gif" || extension.ToLower() == ".gifv")
                            {
                                using (WebClient client = new WebClient())
                                {
                                    urls[i].Url = urls[i].Url.Replace(".gifv", ".gif");
                                    var filename = !string.IsNullOrEmpty(urls[i].Name) ? urls[i].Name + Path.GetExtension(srcList[j]) : Path.GetFileName(srcList[j]);
                                    _logger.LogInformation($"{i + 1} - {j + 1}/{urls.Count}) {filename} download starting...");
                                    client.DownloadFile(new Uri(srcList[j]), Output + "/" + filename);
                                    _logger.LogInformation($"{i + 1} - {j + 1}/{urls.Count}) {filename} download finished...");
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                OnException(ex);
                return 1;
            }
        }
        private List<UrlModel> SetUrls()
        {
            var result = new List<UrlModel>();
            if (!string.IsNullOrEmpty(InputWithName) || !string.IsNullOrEmpty(Input))
            {
                var filePath = InputWithName ?? Input;
                using (StreamReader r = new StreamReader(filePath))
                {
                    string json = r.ReadToEnd();
                    result = JsonConvert.DeserializeObject<List<UrlModel>>(json);
                }
            } 
            else if (MultipleUrl != null)
            {
                foreach (var item in MultipleUrl)
                {
                    result.Add(new UrlModel() { Url = item });
                }
            }
            else if (!string.IsNullOrEmpty(SingleUrl))
            {
                result.Add(new UrlModel() { Url = SingleUrl });
            }
            else
            {
                Console.WriteLine("Input parameter is invalid");
                return null;
            }
            return result;
        }
        private List<string> FetchFromUrl(string url)
        {
            string htmlString = "";
            using (WebClient client = new WebClient())
                htmlString = client.DownloadString(url); //This is an example source for base64 img src, you can change this directly to your source.

            List<string> listOfImgdata = new List<string>();
            string regexImgSrc = @"<img[^>]*?src\s*=\s*[""']?([^'"" >]+?)[ '""][^>]*?>";
            MatchCollection matchesImgSrc = Regex.Matches(htmlString, regexImgSrc, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in matchesImgSrc)
            {
                string href = m.Groups[1].Value;
                listOfImgdata.Add(href);
            }
            return listOfImgdata;
        }
    }
}
