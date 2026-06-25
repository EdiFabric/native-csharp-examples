namespace X12
{
    internal partial class Examples
    {
        /// <summary>
        /// Creates an X12 string from a JSON document.
        /// All X12 separators are configured inside the JSON
        /// No postfix is applied at the end of each segment
        /// </summary>
        public static void Create_x12_without_postfix()
        {
            string x12 = X12Client.Build(json837P);
            Console.WriteLine($"         X12 length = {x12.Length} chars ✓");
        }

        /// <summary>
        /// Creates an X12 string from a JSON document.
        /// All X12 separators are configured inside the JSON
        /// A postfix is applied at the end of each segment
        /// </summary>
        public static void Create_x12_with_postfix()
        {
            string x12 = X12Client.Build(json837P, "\r\n");
            Console.WriteLine($"         X12 with line endings length = {x12.Length} chars ✓");
        }

        /// <summary>
        /// Creates an X12 string from a JSON document.
        /// Breaks down the large JSON into individual segments, and merges them into the X12 string
        /// Useful for creating large X12 files
        /// </summary>
        public static void Create_x12_by_merging_segments()
        {
            string x12 = X12Client.Merge(json837P);
            Console.WriteLine($"         Merged X12 length = {x12.Length} chars ✓");
        }
    }
}
