using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class StoryData
    {
        public List<StoryNode> Nodes { get; set; } = new();
    }

    public class StoryChapter
    {
        public string Chapter { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Start { get; set; } = string.Empty;

        public List<StoryNode> Nodes { get; set; } = new();
    }
}
