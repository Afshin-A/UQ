using System.Collections.Concurrent;


namespace FileDedupeApp.core
{

    /// <summary>
    ///  Scans directories and adds file paths to a BlockingCollection<string>
    /// </summary>
    public class DirectoryScanner
    {
        // A thread-safe collection to hold file paths. Provides coarse-grained blocking for producer and consumer threads.
        private readonly BlockingCollection<string> _fileQueue;
        public DirectoryScanner(BlockingCollection<string> fileQueue)
        {
            _fileQueue = fileQueue;
        }

        public void Scan(IEnumerable<string> rootPaths)
        {
            foreach (var root in rootPaths)
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    _fileQueue.Add(file);
                }
            }
            // why is this necessary? it is to signal to consumer threads that no more items will be added, so they won't hang indefinitely waiting for more items.
            _fileQueue.CompleteAdding();
        }
    }
}