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

        public string Text { get; set; } = string.Empty;

        public string? Next { get; set; }

        public List<Choice> Choices { get; set; } = new();

        public List<SceneCharacter> Characters { get; set; } = new();

        public string Bgm { get; set; } = string.Empty;

        public string Se { get; set; } = string.Empty;
    }
}
