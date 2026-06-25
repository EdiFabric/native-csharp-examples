namespace X12
{
    internal partial class Examples
    {
        public const string serialKey = "bd96a836feca45cb91c86ee65d281f52";

        const string ediDir = "../../../EDI/";
        const string mapDir = "../../../MAP/";
        const string jsonDir = "../../../JSON/";

        static byte[] edi837P = File.ReadAllBytes(Path.Combine(ediDir, "837P.txt"));
        static byte[] edi837PError = File.ReadAllBytes(Path.Combine(ediDir, "837P_CodeError.txt"));
        static byte[] mapFile = File.ReadAllBytes(Path.Combine(mapDir, "Map.json"));
        static byte[] mapFileDefault = File.ReadAllBytes(Path.Combine(mapDir, "MapDefault.json"));
        static byte[] json837P = File.ReadAllBytes(Path.Combine(jsonDir, "837P.json"));

        static string splitConfig = File.ReadAllText(Path.Combine(jsonDir, "SplitConfig.json"));
        static string parseConfig = File.ReadAllText(Path.Combine(jsonDir, "ParseConfig.json"));
    }
}
