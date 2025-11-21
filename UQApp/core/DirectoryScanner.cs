using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace UQApp.core
{
    public interface IDirectoryScanner
    {
        IEnumerable<List<FileEntry>>? Scan(IEnumerable<string> rootPaths, CancellationToken ct);
    }


    /// <summary>
    /// Scans directories for files and groups them by file size.
    /// </summary>
    /// <remarks>
    /// This class is responsible for traversing the specified root directories,
    /// collecting files, and grouping them by their sizes. It is designed to be
    /// cancellation-aware and logs relevant information and errors during scanning.
    /// </remarks>
    /// <param name="logger">
    /// The logger instance used for logging informational, warning, and error messages.
    /// </param>
    public class DirectoryScanner(ILogger<DirectoryScanner> logger, IOptions<UQOptions> options) : IDirectoryScanner
    {
        // NOTE: Use ConcurrentDictionary for thread safety if switching to parallel scanning in the future
        // private ConcurrentDictionary<long, ConcurrentBag<FileEntry>> _sizeGroups = new();
        private readonly ILogger<DirectoryScanner> _logger = logger;
        private readonly IOptions<UQOptions> _options = options;

        /// <summary>
        /// Scans the specified root directories for files, grouping them by file size.
        /// Returns groups of <see cref="FileEntry"/> objects where more than one file shares the same size.
        /// Handles cancellation requests and logs relevant information and errors.
        /// </summary>
        /// <param name="sizeGroups">
        /// A dictionary mapping file sizes to lists of <see cref="FileEntry"/> objects.
        /// This dictionary will be populated during the scan.
        /// </param>
        /// <param name="rootPaths">
        /// An enumerable collection of root directory paths to scan recursively for files.
        /// </param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used to observe cancellation requests during scanning.
        /// </param>
        /// <returns>
        /// An enumerable of lists of <see cref="FileEntry"/> objects, each list containing files with identical sizes and more than one entry.
        /// Returns <c>null</c> if the operation is cancelled.
        /// </returns>
        public IEnumerable<List<FileEntry>>? Scan(IEnumerable<string> rootPaths, CancellationToken ct)
        {
            // NOTE: Use Dictionary for now, switch to ConcurrentDictionary if parallelizing scanning
            // var sizeGroups = new ConcurrentDictionary<long, ConcurrentBag<FileEntry>>();
            var sizeGroups = new Dictionary<long, List<FileEntry>>();
            foreach (var root in rootPaths)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Directory scanning cancelled.");
                    return null;
                }
                SearchOption searchOption = _options.Value.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                string findPattern = _options.Value.Find;
                foreach (var file in Directory.EnumerateFiles(root, findPattern, searchOption))
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("Directory scanning cancelled.");
                        return null;
                    }
                    try
                    {
                        // TOOD: Remove this
                        // Console.WriteLine($"Scanning file: {file}");
                        var fileInfo = new FileInfo(file);

                        // Skip empty files
                        if (fileInfo.Length == 0)
                        {
                            continue;
                        }

                        // checks if the key exists, but does not throw an exception if it doesn't, reducing overhead
                        // out variables are defiend by the method. The method outputs them to the caller. They become available to use by the caller
                        // In this case, if the key doesn't exists, we assign a new list to the out variable group
                        if (!sizeGroups.TryGetValue(
                            fileInfo.Length,
                            out var group
                        ))
                        {
                            group = new List<FileEntry>();
                            sizeGroups[fileInfo.Length] = group;
                        }
                        // we then add an item to the out variable
                        group.Add(new FileEntry(path: fileInfo.FullName, size: fileInfo.Length));
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        _logger.LogWarning($"Access denied to file {file}: {e.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing file {file}: {ex.Message}");
                    }
                }
            }

            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Directory scanning cancelled.");
                return null;
            }

            // go through key-value pairs and select those with more than one entry in the value, then put the value in a list
            return sizeGroups
                .Where(kv => kv.Value.Count > 1)
                .Select(kv => kv.Value);

        }
    }
}