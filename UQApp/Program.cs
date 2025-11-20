
// See https://aka.ms/new-console-template for more information
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
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
            Console.WriteLine("""
            Welcome to UQ
            A File Uniquifier Tool
            """);
            Console.WriteLine();

            // Collect directory/root arguments (flags or bare paths) before host build
            var rootPaths = ParseRootPaths(args);

            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("""
                You've entered help mode. Here's how to use UQ:
                Usage:
                dotnet run -- [paths...] [options]
                
                Examples:
                dotnet run -- C:\Photos D:\Backup
                dotnet run -- --root ""C:\Data"" --workers 12
                
                Options:
                --root <path>      Root directory to scan (can be used multiple times)
                --workers <n>      Number of worker threads (default: CPU count)
                --min-size <bytes> Minimum file size to consider
                --help             Show this help
                """);
                return;
            }

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Only scalar switches mapped here; directory flags handled manually
                    var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "--workers", "Workers" },
                        { "--min-size", "MinSize" }
                    };

                    Console.WriteLine($"hosting env: {hostingContext.HostingEnvironment.EnvironmentName}");
                    Console.WriteLine($"system env: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

                    config
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddInMemoryCollection(rootPaths.Select((p, i) => new KeyValuePair<string,string?>($"Roots:{i}", p)))
                        .AddCommandLine(args, switchMappings);

                    // Fallback if neither CLI nor config specifies Roots
                    var built = config.Build();
                    if (!built.GetSection("Roots").GetChildren().Any())
                    {
                        config.AddInMemoryCollection(new [] { new KeyValuePair<string,string?>("Roots:0", Directory.GetCurrentDirectory()) });
                    }
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
                    services.Configure<UQOptions>(hostContext.Configuration);
                    services.AddTransient<DirectoryScanner>();
                    services.AddTransient<FileHasher>();
                    services.AddHostedService<UQService>();
                })
                .Build()
                .RunAsync();
        }

        private static List<string> ParseRootPaths(string[] args)
        {
            var results = new List<string>();
            var dirFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "-d", "-dir", "--root", "--roots" };

            for (int i = 0; i < args.Length; i++)
            {
                var current = args[i];
                if (dirFlags.Contains(current))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        var pathArg = args[++i];
                        AddPath(results, pathArg);
                    }
                    continue;
                }

                if (!current.StartsWith("-"))
                {
                    AddPath(results, current);
                }
            }

            return results;
        }
        private static void AddPath(List<string> list, string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return;
                try
                {
                    var full = Path.GetFullPath(raw);
                    if (!list.Contains(full, StringComparer.OrdinalIgnoreCase))
                        list.Add(full);
                }
                catch (Exception)
                {
                    
                }
            }
    }
}
