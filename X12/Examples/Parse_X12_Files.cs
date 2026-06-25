namespace X12
{
    internal partial class Examples
    {
        /// <summary>
        /// Parses an X12 file
        /// Configure a model map that reloves the JSON models for each X12 message type
        /// All models are local files
        /// For parsing only - the ParseMode is set to Json (default)
        /// </summary>
        public static void Parse_to_json_with_local_models()
        {
            X12Client.SetMap(mapFile);
            var result = X12Client.Parse(edi837P, ParseMode.Json);
            Console.WriteLine($"         X12 parsed ✓");
        }

        /// <summary>
        /// Parses an X12 file
        /// Configure a model map that reloves the JSON models for each X12 message type
        /// All models are retrieved over the Internet from the EdiNation Cloud API
        /// For parsing only - the ParseMode is set to Json (default)
        /// </summary>
        public static void Parse_to_json_with_online_models()
        {
            X12Client.SetMap(mapFileDefault);
            var result = X12Client.Parse(edi837P, ParseMode.Json);
            Console.WriteLine($"         X12 parsed ✓");
        }

        /// <summary>
        /// Parse a large X12 file by splitting it into chunks
        /// Configure a splitter that requires a repeating loop where each chunk is a separate instance of the repeating loop
        /// The last chunk  carries the envelope-level JSON (and optional errors/ACK).
        /// </summary>
        public static void Parse_to_json_with_splitting()
        {
            X12Client.SetMap(mapFile);
            var results = X12Client.Split(edi837P, splitConfig, ParseMode.Json).ToList();

            if (results.Count != 5)
                throw new Exception($"Expected 5 split results, got {results.Count}");

            // 5th result is the envelope-level summary.
            Console.WriteLine($"         X12 parsed with splitting ✓");
        }
    }
}