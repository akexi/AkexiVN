using System.Collections.Generic;

namespace AkexiVN.Models
{
    public class GameConfig
    {
        public string GameTitle { get; set; } = "AkexiVN";

        public string StartChapter { get; set; } = string.Empty;

        public string Language { get; set; } = "zh-CN";

        public List<string> ChapterOrder { get; set; } = new();
    }
}
