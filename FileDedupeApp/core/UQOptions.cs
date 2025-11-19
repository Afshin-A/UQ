namespace FileDedupeApp.core
{
    public class UQOptions
    {
        public List<string> Root { get; set; } = [];

        //TODO: Uncomment
        // public int Workers { get; set; } = Environment.ProcessorCount;
        public int Workers { get; set; } = 4;
        public long MinSize { get; set; } = 1024; // 1 KB
        public bool Recursive { get; set; } = true;

    }
}