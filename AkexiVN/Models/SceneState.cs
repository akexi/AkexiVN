using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class SceneState
    {
        public string Background { get; set; } = string.Empty;

        public Dictionary<string, SceneCharacter> Characters { get; set; } = new();

        public string Bgm { get; set; } = string.Empty;
    }
}
