// See https://aka.ms/new-console-template for more information
using FileDedupeApp.core;


Console.WriteLine("Starting File Dedupe Application");

// @ is a verbatim string literal, useful for Windows paths. it treats backslashes as normal characters, so we don't have to escape them.
var directories = args.Length > 0 ? args : [Environment.GetEnvironmentVariable("TestDirectory") ?? throw new ArgumentException("No directories provided via command line arguments or environment variable")];
Console.WriteLine($"Accepted root directories: {string.Join(", ", directories)}");

var scanManager = new ScanManager();
scanManager.Run(directories);
