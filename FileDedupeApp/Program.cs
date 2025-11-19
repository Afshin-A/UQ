
// See https://aka.ms/new-console-template for more information
using FileDedupeApp.core;



using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

/*
// Console.WriteLine(@"              
// Welcome   to    .oooooo.     
// `888      `8  d8P'   `Y8b     
//  888       8  888      888    
//  888       8  888      888    
//  888       8  888      888    
//  `88.    .8'  `88b    d88b    
//    `YbodP'     `Y8bood8P'Ybd'
// ");
Console.WriteLine("Starting File Dedupe Application");

// @ is a verbatim string literal, useful for Windows paths. it treats backslashes as normal characters, so we don't have to escape them.
var directories = args.Length > 0 ? args : [Environment.GetEnvironmentVariable("TestDirectory") ?? throw new ArgumentException("No directories provided via command line arguments or environment variable")];
// var directories = args.Length > 0 ? args : [@"C:\Users\Afshin\Downloads\val2017\val2017-subset"];
Console.WriteLine($"Accepted root directories: {string.Join(", ", directories)}");
UQ.Run(directories);
*/




namespace FileDedupeApp
{
    /*class Program

    {
        public static async Task Main(string[] args)
        {
            // establish connection to configuration sources for Serilog
            var appConfiguration = new ConfigurationBuilder();
            BuildConfiguration(appConfiguration, args);
            var configuration = appConfiguration.Build();
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration) // read settings from configuration sources
                .Enrich.FromLogContext() // add extra info from serilog to logs
                .WriteTo.ColoredConsole()
                .CreateLogger();

            Log.Logger.Information("Starting Photo UQ Application");

            // NOTE: what's the difference between RunAsync and RunConsoleAsync?
            var host =  CreateHostBuilder(args).Build().RunAsync();
            
            // REWORK
            var service = ActivatorUtilities.CreateInstance<DuplicateFinderService>(Host.Services);
            // service.StartAsync();
        }

        static void BuildConfiguration(IConfigurationBuilder builder, string[] args)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables() // environment variables override json settings
            .AddCommandLine(args); // command-line args override other settings
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<FileHasher>();
                    services.AddSingleton<DirectoryScanner>();
                    services.AddTransient<IDuplicateFinderService, DuplicateFinderService>();
                    // services.AddHostedService<IDuplicateFinderService, DuplicateFinderService>();
                })
                .UseSerilog(); // integrate Serilog with Microsoft.Extensions.Logging
            return host;
        }

    } */


    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine(@"              
                Welcome   to    .oooooo.     
                888       8   d8P'   `Y8b     
                888       8  888      888    
                888       8  888      888    
                888       8  888      888    
                `88.    .8'  `88b    d88b    
                `YbodPY'     `Y8bood8P'Ybd'
            ");

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args);
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
                           .AddFilter("FileDedupeApp.core.DuplicateFinderService", LogLevel.Information);
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<UQOptions>(hostContext.Configuration);
                    services.AddTransient<DirectoryScanner>();
                    services.AddTransient<FileHasher>();
                    services.AddHostedService<DuplicateFinderService>();
                })
                .Build()
                .RunAsync();
        }
    }
    
}
