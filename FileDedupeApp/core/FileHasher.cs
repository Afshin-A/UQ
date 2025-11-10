
using System.Collections.Concurrent;
using System.Security.Cryptography;
namespace FileDedupeApp.core
{

    /// <summary>
    ///  Consumes file paths from a BlockingCollection<string>, computes their hashes, and adds FileEntry objects to another BlockingCollection<FileEntry>
    /// </summary>
    public class FileHasher(BlockingCollection<string> inputQueue, BlockingCollection<FileEntry> outputQueue)
    {
        private readonly BlockingCollection<string> _inputQueue = inputQueue;
        private readonly BlockingCollection<FileEntry> _outputQueue = outputQueue;
        
        // public FileHasher(BlockingCollection<string> inputQueue, BlockingCollection<FileEntry> outputQueue)
        // {
        //     _inputQueue = inputQueue;
        //     _outputQueue = outputQueue;
        // }

        public void Start(int degreeOfParallelism)
        {
            var tasks = Enumerable.Range(0, degreeOfParallelism).Select(_ => Task.Run(() =>
            {
                // internally uses atomic operations to ensure thread-safety
                // enforces blocking behavior 
                // 
                foreach (var filePath in _inputQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        string hash = ComputeHash(filePath);
                        long fileSize = new FileInfo(filePath).Length;
                        _outputQueue.Add(new FileEntry(filePath, fileSize, hash));
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add proper logging 
                        // Log the error and continue processing other files
                        Console.Error.WriteLine($"Error hashing file {filePath}: {ex.Message}");
                    }
                }
            })).ToArray();
            // when all the threads are done, mark the output queue will not be getting any more items. 
            // after the remaining items are consumed, consumer threads are allowed to gracefully exit. 
            // that way, they don't hang indefinitely waiting for more items that will never come, avoiding potential deadlocks.
            Task.WhenAll(tasks).ContinueWith(_ => _outputQueue.CompleteAdding());
        }

        private string ComputeHash(string filePath)
        {
            // using is syntactic sugar for try/finally to ensure disposal/closing of the stream and hasher streams
            using var stream = File.OpenRead(filePath);
            using var hasher = SHA256.Create();
            var hashBytes = hasher.ComputeHash(stream);
            return Convert.ToHexString(hashBytes);
        }
    }
}