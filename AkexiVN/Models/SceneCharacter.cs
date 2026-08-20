using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkexiVN.Models
{
    public class SceneCharacter
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string Expression { get; set; } = "normal";

        public string Position { get; set; } = "center";

        public double Scale { get; set; } = 1.0;

        public double OffsetX { get; set; } = 0;

        public double OffsetY { get; set; } = 0;

        public string Effect { get; set; } = "none";

        public double Opacity { get; set; } = 1;
    }
}
