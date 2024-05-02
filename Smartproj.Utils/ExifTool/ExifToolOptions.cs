namespace Smartproj.Utils
{
    public class ExifToolOptions
    {
        public string ExifToolPath { get; set; } = "exiftool";
        public bool IncludeBinaryTags { get; set; }
        public bool EscapeTagValues { get; set; }
        public bool ExtractICCProfile { get; set; }
    }
}
