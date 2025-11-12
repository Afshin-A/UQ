
using System.Collections.Concurrent;

namespace FileDedupeApp.core
{

    /// <summary>
    ///  Consumes file paths from a BlockingCollection<string>, computes their hashes, and adds FileEntry objects to another BlockingCollection<FileEntry>
    /// </summary>
    public class FileHasher(BlockingCollection<FileEntry> inputQueue, BlockingCollection<FileEntry> outputQueue, Func<string, string> computeHashMethod)
    {
        private BlockingCollection<FileEntry> _inputQueue = inputQueue;
        private BlockingCollection<FileEntry> _outputQueue = outputQueue;
        private readonly Func<string, string> _computeHashMethod = computeHashMethod;

        public void Start(int workerCount)
        {
            var tasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
            {
                // internally uses atomic operations to ensure thread-safety
                // enforces blocking behavior 
                foreach (var metadata in _inputQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        string hash = _computeHashMethod(metadata.Path);
                        // TODO: create a new FileEntry or update existing one and add to output queue?
                        // will the latter approach cause issues with multiple threads updating the same object?
                        _outputQueue.Add(new FileEntry(metadata.Path, metadata.Size, hash));
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add proper logging 
                        // Log the error and continue processing other files
                        Console.Error.WriteLine($"Error hashing file {metadata}: {ex.Message}");
                    }
                }
            })).ToArray();
            
            Task.WhenAll(tasks).ContinueWith(_ => _outputQueue.CompleteAdding());
        }
    }
}