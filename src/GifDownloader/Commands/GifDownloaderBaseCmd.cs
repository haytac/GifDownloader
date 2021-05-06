using McMaster.Extensions.CommandLineUtils;
using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Xsl;
using Microsoft.Extensions.Options;
using GifDownloader.Settings;

namespace GifDownloader.Commands
{
    [HelpOption("--help")]
    abstract class GifDownloaderBaseCmd
    {
        protected ILogger _logger;
        protected IConsole _console;
        protected IOptions<GlobalSettings> _settings;

        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            return Task.FromResult(0);
        }

        protected void OnException(Exception ex)
        {
            OutputError(ex.Message);
            _logger.LogError(ex.Message);
            _logger.LogDebug(ex, ex.Message);
        }
        protected void OutputError(string message)
        {
            _console.BackgroundColor = ConsoleColor.Red;
            _console.ForegroundColor = ConsoleColor.White;
            _console.Error.WriteLine(message);
            _console.ResetColor();
        }

        protected string GetUrlFileExtension(string url)
        {
            return System.IO.Path.GetExtension(url);
        }
    }
}
