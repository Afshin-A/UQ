namespace FileDedupeApp.core 
{
    public class FileEntry
    {
        public string FilePath { get; }
        public long FileSize { get; }
        public string FileHash { get; }

        public FileEntry(string filePath, long fileSize, string fileHash)
        {
            FilePath = filePath;
            FileSize = fileSize;
            FileHash = fileHash;
        }
    }
}