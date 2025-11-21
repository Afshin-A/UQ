
// See https://aka.ms/new-console-template for more information
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UQApp.core;

namespace UQApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));

            Console.WriteLine("""
            Welcome to UQ
            A File Uniquifier Tool
            """);
            Console.WriteLine();
            

            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("""
                You've entered help mode. Create an issue or pull request @ https://github.com/Afshin-A/UQ
                Usage:
                dotnet run -- [paths...] [options]
                
                Examples:
                dotnet run -- C:\Photos D:\Backup --workers 8
                dotnet run -- --roots:0 "C:\Data" --roots:1 "D:\MoreData" --min-size 1048576
                
                Options:
                --roots:i <path>            Root directory to scan (can be used multiple times)
                -w, --workers <n>           Number of worker threads (default: CPU count)
                -r, --recursive <bool>      Scan directories recursively
                -f, --find <regex>          Find duplicates without deleting
                -v, --version               Show version information
                -h, --help                  Show this help message
                """);
                return;
            }

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var roots = new List<string>();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].StartsWith('-'))
                        {
                            break;
                        }
                        else
                        {
                            roots.Add(args[i]);
                        }
                    }
                    // Only scalar switches mapped here; directory flags handled manually
                    var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "--workers", "Workers" },
                        { "-w", "Workers" },
                        { "-r", "Recursive" },
                        { "-f", "Find" }

                    };

                    Console.WriteLine($"hosting env: {hostingContext.HostingEnvironment.EnvironmentName}");
                    Console.WriteLine($"system env: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

                    config
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddInMemoryCollection(roots.Select((root, index) => new KeyValuePair<string, string?>($"Roots:{index}", root)))
                        .AddCommandLine(args, switchMappings);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                    logging.AddFilter("Microsoft", LogLevel.Warning)
                           .AddFilter("System", LogLevel.Warning)
                           .AddFilter("UQApp.core.UQService", LogLevel.Information);
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<UQOptions>(hostContext.Configuration); // bind configuration key-values to UQOptions
                    services.AddTransient<DirectoryScanner>();
                    services.AddTransient<FileHasher>();
                    services.AddHostedService<UQService>();
                })
                .Build()
                .RunAsync();
        }
    }
}
