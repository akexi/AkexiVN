using System.Collections.Generic;

namespace AkexiVN.Models
{
    public class Chapter
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string File { get; set; } = string.Empty;

        public string? NextChapter { get; set; }

        public bool Unlock { get; set; } = true;

        public string Summary { get; set; } = string.Empty;

        public List<string> Rewards { get; set; } = new();

        public List<string> Unlocks { get; set; } = new();
    }
}
