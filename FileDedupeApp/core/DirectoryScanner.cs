using System.Collections.Concurrent;
using System.Runtime.CompilerServices;


namespace FileDedupeApp.core
{

    /// <summary>
    ///  Given a list of root paths, scans directories recursively and adds files to a map where key is file size and value is list of FileMetaData (path and size).
    /// </summary>
    public class DirectoryScanner(Dictionary<long, List<FileEntry>> sizeGroups)
    {
        private Dictionary<long, List<FileEntry>> _sizeGroups = sizeGroups;

        public void Scan(IEnumerable<string> rootPaths)
        {
            foreach (var root in rootPaths)
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        // TOOD: Remove this
                        // Console.WriteLine($"Scanning file: {file}");
                        var fileInfo = new FileInfo(file);
                        // checks if the key exists, but does not throw an exception if it doesn't, reducing overhead
                        // out variables are defiend by the method. The method outputs them to the caller. They become available to use by the caller
                        // In this case, if the key doesn't exists, we assign a new list to the out variable group
                        if (!_sizeGroups.TryGetValue(
                            fileInfo.Length,
                            out var group
                        ))
                        {
                            group = new List<FileEntry>();
                            _sizeGroups[fileInfo.Length] = group;
                        }
                        // we then add an item to the out variable
                        group.Add(new FileEntry(path: fileInfo.FullName, size: fileInfo.Length));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
            }
        }
    }        
}