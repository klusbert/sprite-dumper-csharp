using System.ComponentModel;

namespace SpriteDumper
{
    public class Catalog
    {
        public Catalog()
        {
            SpriteType = int.MinValue;
            FirstSpriteid = int.MinValue;
            LastSpriteid = int.MinValue;
            Area = int.MinValue;
            Version = int.MinValue;
        }

        public string Type { get; set; }
        public string File { get; set; }
        [DefaultValue(int.MinValue)] public int SpriteType { get; set; }
        [DefaultValue(int.MinValue)] public int FirstSpriteid { get; set; }
        [DefaultValue(int.MinValue)] public int LastSpriteid { get; set; }
        [DefaultValue(int.MinValue)] public int Area { get; set; }
        [DefaultValue(int.MinValue)] public int Version { get; set; }
    }
}