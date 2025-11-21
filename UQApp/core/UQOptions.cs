namespace UQApp.core
{
    public sealed class UQOptions
    {
        public List<string> Roots { get; set; } = [];
        // public required string Roots { get; set; }

        //TODO: Uncomment
        public int Workers { get; set; } = 3 * Environment.ProcessorCount / 4;
        // public int Workers { get; set; } = 4;
        public long MinSize { get; set; } = 1024; // 1 KB
        public bool Recursive { get; set; } = true;
        public string Find { get; set; } = "*";
        public string? Version { get; set; } = "1.0.0";
    }
}