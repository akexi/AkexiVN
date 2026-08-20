namespace AkexiVN.Models
{
    public class CharacterProfile
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public double DefaultScale { get; set; } = 1.0;

        public double DefaultOffsetX { get; set; } = 0;

        public double DefaultOffsetY { get; set; } = 0;
    }
}
