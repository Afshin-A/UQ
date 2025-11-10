using System.Collections.Concurrent;

namespace FileDedupeApp.core
{
    // this is called a primary constructor in C#. 
    public class DuplicateFinder(BlockingCollection<FileEntry> inputQueue)
    {
        private readonly BlockingCollection<FileEntry> _inputQueue = inputQueue;
        private readonly ConcurrentDictionary<string, List<FileEntry>> _hashGroups = new();

        public void Process()
        {
            foreach (var entry in _inputQueue.GetConsumingEnumerable())
            {
                _hashGroups.AddOrUpdate(
                    // key
                    entry.FileHash,
                    // if the key doesn't exist, create a new list with the current entry
                    [entry],
                    // if they key exists, update the value using this lambda.
                    // ensure thread-safety when updating the list by locking the bucket. This way, if multiple threads try to add to the same list, they do so one at a time.
                    (_, list) => {
                        lock (list)
                        {
                            list.Add(entry);
                        } 
                        return list; 
                    }
                );
            }
        }
        public IEnumerable<List<FileEntry>> GetDuplicates()
        {
            return _hashGroups.Values.Where(list => list.Count > 1);
        }
    }
}