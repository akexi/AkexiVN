using System;
using System.Collections.Generic;

namespace AkexiVN.Models
{
    public class SaveData
    {
        public string CurrentChapterId { get; set; } = string.Empty;

        public string CurrentNodeId { get; set; } = string.Empty;

        public string GameState { get; set; } = string.Empty;

        public string Background { get; set; } = string.Empty;

        public string Bgm { get; set; } = string.Empty;

        public string CurrentCharacterName { get; set; } = string.Empty;

        public string CurrentText { get; set; } = string.Empty;

        public Dictionary<string, SceneCharacter> Characters { get; set; } = new();

        public DateTime SaveTime { get; set; }
    }
}
