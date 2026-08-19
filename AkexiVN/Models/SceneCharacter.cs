using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class SceneCharacter
    {
        public string Name { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string Position { get; set; } = "center";

        public string Effect { get; set; } = "none";

        public double Opacity { get; set; } = 1;
    }
}
