using System;
using System.Collections.Generic;
using System.Collections.Generic;

namespace AkexiVN.Models
{
    public class Choice
    {
        public string Text { get; set; } = string.Empty;

        public string Next { get; set; } = string.Empty;

        public Dictionary<string, object> Effects { get; set; } = new();
    }
}
