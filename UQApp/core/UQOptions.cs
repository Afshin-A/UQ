namespace UQApp.core
{
    public sealed class UQOptions
    {
        public List<string> Roots { get; set; } = [];
        // public required string Root { get; set; }

        //TODO: Uncomment
        public int Workers { get; set; } = Environment.ProcessorCount / 2;
        // public int Workers { get; set; } = 4;
        public long MinSize { get; set; } = 1024; // 1 KB
        public bool Recursive { get; set; } = true;

    }
}