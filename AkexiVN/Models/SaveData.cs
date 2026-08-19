using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class SaveData
    {
        public string CurrentNodeId { get; set; } = string.Empty;

        public string Background { get; set; } = string.Empty;

        public string Bgm { get; set; } = string.Empty;

        public Dictionary<string, SceneCharacter> Characters { get; set; } = new();

        public DateTime SaveTime { get; set; }
    }
}
