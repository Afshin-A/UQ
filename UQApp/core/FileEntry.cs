namespace UQApp.core
{
    // [Obsolete("This class is deprecated, use FileMetaData instead.")]
    public class FileEntry(string path, long size, string? hash = null)
    {
        // no setter for Path and Size makes them read-only after initialization
        public string Path { get; } = path;
        // Same effect as above. Size is immutable after object creation
        public readonly long Size = size;
        public string? Hash { get; set; } = hash;
    }
}