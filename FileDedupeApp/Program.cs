// See https://aka.ms/new-console-template for more information
using FileDedupeApp.core;

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
ScanManager.Run(directories);
