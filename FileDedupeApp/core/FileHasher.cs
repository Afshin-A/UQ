using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FileDedupeApp.core
{
    public interface IFileHasher
    {
        Task Start(BlockingCollection<FileEntry> inputQueue,
            BlockingCollection<FileEntry> outputQueue,
            Func<string, string> computeHashMethod,
            int workerCount,
            CancellationToken ct);
    }

    /// <summary>
    ///  Uses the producer-consumer multi-threading pattern to process files from a BlockingCollection. computes their hashes, and adds FileEntry objects to another BlockingCollection<FileEntry>
    /// </summary>
    public class FileHasher(ILogger<FileHasher> logger) : IFileHasher
    {
        private readonly ILogger<FileHasher> _logger = logger;

        /// <summary>
        /// Starts the file hashing process using multiple worker tasks.
        /// Consumes <see cref="FileEntry"/> objects from the <paramref name="inputQueue"/>, computes their hash using the provided <paramref name="computeHashMethod"/>,
        /// and adds the resulting <see cref="FileEntry"/> (with hash) to the <paramref name="outputQueue"/>.
        /// The process is performed concurrently using the specified <paramref name="workerCount"/>.
        /// Supports cancellation via <paramref name="ct"/>.
        /// </summary>
        /// <param name="inputQueue">A blocking collection containing file entries to be hashed.</param>
        /// <param name="outputQueue">A blocking collection to receive file entries with computed hashes.</param>
        /// <param name="computeHashMethod">A function that computes the hash for a given file path.</param>
        /// <param name="workerCount">The number of concurrent worker tasks to use for hashing.</param>
        /// <param name="ct">A cancellation token to signal cancellation of the operation.</param>
        public async Task Start(
            BlockingCollection<FileEntry> inputQueue,
            BlockingCollection<FileEntry> outputQueue,
            Func<string, string> computeHashMethod,
            int workerCount,
            CancellationToken ct
        )
        {
            var tasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
            {
                // internally uses atomic operations to ensure thread-safety
                // enforces blocking behavior 
                foreach (var metadata in inputQueue.GetConsumingEnumerable())
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("File hashing cancelled.");
                        return;
                    }
                    try
                    {
                        string hash = computeHashMethod(metadata.Path);
                        // TODO: create a new FileEntry or update existing one and add to output queue?
                        // will the latter approach cause issues with multiple threads updating the same object?
                        outputQueue.TryAdd(new FileEntry(metadata.Path, metadata.Size, hash));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error hashing file {metadata}: {ex.Message}");
                    }
                }
            })).ToArray();


            // creates a yield point. control goes back to the caller. when the task completes, the continuation resumes (output queue is marked as complete)
            await Task.WhenAll(tasks);
            outputQueue.CompleteAdding();
        }
    }
}