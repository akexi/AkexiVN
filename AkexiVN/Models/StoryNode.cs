using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class StoryNode
    {
        public string Id { get; set; } = string.Empty;

        public string Background { get; set; } = string.Empty;

        public string Character { get; set; } = string.Empty;

        public string CharacterImage { get; set; } = string.Empty;

        public string Position { get; set; } = "center";

        public string Effect { get; set; } = "none";

        public string Text { get; set; } = string.Empty;

        public string? Next { get; set; }

        public List<Choice> Choices { get; set; } = new();
    }
}
