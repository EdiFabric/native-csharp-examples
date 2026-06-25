namespace X12
{
    internal partial class Examples
    {
        /// <summary>
        /// Parses and validates a valid X12 file
        /// Set ParseMode to Validate - this parses AND validates the X12 data 
        /// Pass validation configuration in parseConfig
        /// </summary>
        public static void Validate_valid_x12()
        {
            X12Client.SetMap(mapFile);
            var result = X12Client.Parse(edi837P, ParseMode.Validate, parseConfig);
            Console.WriteLine($"         errors_count = 0 ✓");
        }

        /// <summary>
        /// Parses and validates an invalid X12 file
        /// Set ParseMode to Validate - this parses AND validates the X12 data 
        /// Pass validation configuration in parseConfig
        /// </summary>
        public static void Validate_invalid_x12()
        {
            X12Client.SetMap(mapFile);
            var result = X12Client.Parse(edi837PError, ParseMode.Validate, parseConfig);
            Console.WriteLine($"         Detected {result.Errors.Count()} validation errors ✓");
        }

        /// <summary>
        /// Parse a large X12 file by splitting it into chunks
        /// Validate the X12 data at the same time
        /// Configure a splitter that requires a repeating loop where each chunk is a separate instance of the repeating loop
        /// The last chunk  carries the envelope-level JSON (and optional errors/ACK).
        /// </summary>
        public static void Validate_with_splitting()
        {
            X12Client.SetMap(mapFile);
            var results = X12Client.Split(edi837PError, splitConfig, ParseMode.Validate).ToList();

            if (results.Count != 5)
                throw new Exception($"Expected 5 split results, got {results.Count}");

            var last = results[^1];
            if (last.Errors is null)
                throw new Exception("Last result should carry error JSON");
            Console.WriteLine($"         Last split result carries validation errors ✓");
        }
    }
}
