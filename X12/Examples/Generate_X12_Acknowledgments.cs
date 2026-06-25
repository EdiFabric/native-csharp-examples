namespace X12
{
    internal partial class Examples
    {
        /// <summary>
        /// Parses, validates and generates a X12 999 ack for a valid X12 file
        /// Set ParseMode to Ack - this parses and validates the X12 data, AND generates an acknowledgment 
        /// Pass Ack configuration in parseConfig
        /// </summary>
        public static void Ack_valid_x12()
        {
            X12Client.SetMap(mapFile);
            var result = X12Client.Parse(edi837P, ParseMode.Ack, parseConfig);          
            Console.WriteLine($"         ACK generated ✓");
        }

        /// <summary>
        /// Parses, validates and generates a X12 999 ack for an invalid X12 file
        /// Set ParseMode to Ack - this parses and validates the X12 data, AND generates an acknowledgment 
        /// Pass Ack configuration in parseConfig
        /// </summary>
        public static void Ack_invalid_x12()
        {
            X12Client.SetMap(mapFile);
            var result = X12Client.Parse(edi837PError, ParseMode.Ack, parseConfig);
            Console.WriteLine($"         ACK with errors generated ✓");
        }     
    }
}
