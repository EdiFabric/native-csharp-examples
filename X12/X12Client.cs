using System.Text;

namespace X12
{
    /// <summary>
    /// Parse mode passed to <see cref="X12Client.Parse"/> and <see cref="X12Client.Split"/>.
    /// </summary>
    public enum ParseMode
    {
        /// <summary>X12 → JSON only.</summary>
        Json = 1,
        /// <summary>X12 → JSON + validation error report.</summary>
        Validate = 2,
        /// <summary>X12 → JSON + validation error report + 999/997 ACK.</summary>
        Ack = 3,
    }

    /// <summary>
    /// Result of a parse or split call when <see cref="ParseMode.Validate"/> or
    /// <see cref="ParseMode.Ack"/> is requested.
    /// </summary>
    public sealed class ParseResult
    {
        /// <summary>The parsed X12 transaction set as JSON.</summary>
        public string Json { get; init; } = string.Empty;

        /// <summary>
        /// Validation error report as JSON, or <see langword="null"/> when
        /// <see cref="ParseMode.Json"/> was used.
        /// </summary>
        public string? Errors { get; init; }
    }

    /// <summary>
    /// Thread-safe, high-level managed wrapper around the edifabric-x12-tools native AOT DLL.
    ///
    /// Responsibilities:
    ///  - Hides all unsafe / pointer code behind safe managed methods.
    ///  - Automatically grows output buffers when <c>InsufficientCapacity</c> (code 1)
    ///    is returned by the native side.
    ///  - Converts UTF-8 byte buffers to/from <see cref="string"/>.
    ///  - Translates non-zero native error codes into <see cref="X12NativeException"/>.
    /// </summary>
    public sealed class X12Client
    {
        // InsufficientCapacity = 1; the native side sets *outputLength to the required size.
        private const int InsufficientCapacity = 1;

        private static readonly object _splitLock = new();

        // ── Lifecycle ────────────────────────────────────────────────────────

        /// <summary>Clears all cached state inside the native library (reader, writer, model, license).</summary>
        public static void ClearCache() => ThrowIfError(X12NativeImport.ClearCache());

        // ── Logging ──────────────────────────────────────────────────────────

        /// <summary>
        /// Enables file logging inside the native library.
        /// </summary>
        /// <param name="path">Absolute path to the log file.</param>
        /// <param name="minLevel">Minimum level to log: 0=Trace, 1=Debug, 2=Info, 3=Warn, 4=Error.</param>
        public static void InitLogger(string path, int minLevel = 2)
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(path);
            unsafe
            {
                fixed (byte* p = pathBytes)
                    ThrowIfError(X12NativeImport.InitLogger(p, pathBytes.Length, minLevel));
            }
        }

        /// <summary>Flushes and closes the log file opened by <see cref="InitLogger"/>.</summary>
        public static void ShutdownLogger() => ThrowIfError(X12NativeImport.ShutdownLogger());

        // ── Licensing ────────────────────────────────────────────────────────

        /// <summary>Returns the application version integer reported by the native library.</summary>
        public static int GetAppVersion()
        {
            unsafe
            {
                int v = 0;
                ThrowIfError(X12NativeImport.GetAppVersion(&v));
                return v;
            }
        }

        /// <summary>
        /// Installs a new license for this machine using the provided serial key.
        /// Requires internet access.
        /// </summary>
        public static void InstallLicense(string serialKey)
        {
            byte[] input = Encoding.UTF8.GetBytes(serialKey);
            unsafe
            {
                fixed (byte* p = input)
                    ThrowIfError(X12NativeImport.InstallLicense(p, input.Length));
            }
        }

        /// <summary>
        /// Retrieves a signed token for the given serial key from the license server.
        /// The returned token can be stored and later passed to <see cref="SetToken"/>.
        /// </summary>
        public static string GetToken(string serialKey)
        {
            byte[] input = Encoding.UTF8.GetBytes(serialKey);
            unsafe
            {
                fixed (byte* inPtr = input)
                {
                    int needed = 0;
                    // First call: probe required size.
                    int rc = X12NativeImport.GetToken(inPtr, input.Length, null, 0, &needed);
                    if (rc != InsufficientCapacity && rc != 0)
                        ThrowIfError(rc);

                    byte[] outBuf = new byte[needed];
                    int written = 0;
                    fixed (byte* outPtr = outBuf)
                        ThrowIfError(X12NativeImport.GetToken(inPtr, input.Length, outPtr, outBuf.Length, &written));

                    return Encoding.UTF8.GetString(outBuf, 0, written);
                }
            }
        }

        /// <summary>
        /// Sets a previously obtained token so that subsequent operations are authorized.
        /// The token is validated cryptographically before being cached.
        /// </summary>
        public static void SetToken(string token)
        {
            byte[] input = Encoding.UTF8.GetBytes(token);
            unsafe
            {
                fixed (byte* p = input)
                    ThrowIfError(X12NativeImport.SetToken(p, input.Length));
            }
        }

        /// <summary>Validates a token without caching it. Throws on any validation failure.</summary>
        public static void ValidateToken(string token)
        {
            byte[] input = Encoding.UTF8.GetBytes(token);
            unsafe
            {
                fixed (byte* p = input)
                    ThrowIfError(X12NativeImport.ValidateToken(p, input.Length));
            }
        }

        /// <summary>Returns the UTC expiration tick count of the currently cached token, or 0 if none is set.</summary>
        public static DateTime? GetTokenExpiration()
        {
            unsafe
            {
                long ticks = 0;
                ThrowIfError(X12NativeImport.GetTokenExpiration(&ticks));
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Sets a serial key for runtime authentication (calls the license server on each operation).
        /// Use <see cref="SetToken"/> instead for offline / high-throughput scenarios.
        /// </summary>
        public static void SetSerial(string serialKey)
        {
            byte[] input = Encoding.UTF8.GetBytes(serialKey);
            unsafe
            {
                fixed (byte* p = input)
                    ThrowIfError(X12NativeImport.SetSerial(p, input.Length));
            }
        }

        // ── Model ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the template map that tells the parser how to resolve X12 transaction sets.
        /// Must be called before any parse, or split operation.
        /// </summary>
        /// <param name="mapJson">Contents of the map JSON configuration file.</param>
        public static void SetMap(string mapJson)
        {
            byte[] input = Encoding.UTF8.GetBytes(mapJson);
            unsafe
            {
                fixed (byte* p = input)
                    ThrowIfError(X12NativeImport.SetMap(p, input.Length));
            }
        }

        /// <summary>Overload that accepts a pre-encoded map buffer.</summary>
        public static void SetMap(byte[] mapBytes)
        {
            unsafe
            {
                fixed (byte* p = mapBytes)
                    ThrowIfError(X12NativeImport.SetMap(p, mapBytes.Length));
            }
        }

        // ── Parse ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses an X12 document. With <see cref="ParseMode.Json"/>, returns a
        /// <see cref="ParseResult"/> whose <see cref="ParseResult.Errors"/> is
        /// <see langword="null"/>. With <see cref="ParseMode.Validate"/> or
        /// <see cref="ParseMode.Ack"/>, <see cref="ParseResult.Errors"/> contains
        /// the validation/ACK JSON.
        /// </summary>
        /// <param name="edi">Raw X12 bytes.</param>
        /// <param name="mode">Requested operation mode.</param>
        /// <param name="configJson">Optional parse/validate/ack configuration JSON.</param>
        public static ParseResult Parse(byte[] edi, ParseMode mode = ParseMode.Json, string? configJson = null)
        {
            byte[]? configBytes = configJson is null ? null : Encoding.UTF8.GetBytes(configJson);
            int capacity = edi.Length * 12;

            unsafe
            {
                fixed (byte* inPtr = edi)
                {
                    byte[]? cfgPin = configBytes;
                    while (true)
                    {
                        byte[] output = GC.AllocateUninitializedArray<byte>(capacity);
                        int resultSize = 0, resultOffset = 0;

                        fixed (byte* outPtr = output)
                        fixed (byte* cfgPtr = cfgPin)
                        {
                            int rc = X12NativeImport.Parse(
                                inPtr, edi.Length,
                                (int)mode,
                                cfgPtr, configBytes?.Length ?? 0,
                                outPtr, capacity,
                                &resultSize, &resultOffset);

                            if (rc == InsufficientCapacity)
                            {
                                capacity = resultSize;
                                continue;
                            }

                            ThrowIfError(rc);

                            string json = Encoding.UTF8.GetString(output, 0,
                                mode == ParseMode.Json ? resultSize : resultOffset);

                            string? errors = mode == ParseMode.Json
                                ? null
                                : Encoding.UTF8.GetString(output, resultOffset, resultSize - resultOffset);

                            return new ParseResult { Json = json, Errors = errors };
                        }
                    }
                }
            }
        }

        // ── Split ────────────────────────────────────────────────────────────

        /// <summary>
        /// Splits an X12 document into individual transaction-set results or loops in a repeating loop, streaming one at
        /// a time. Each element is a <see cref="ParseResult"/>; the last element also carries
        /// the envelope-level JSON (and optional errors/ACK).
        ///
        /// This method serialises concurrent callers via an internal lock because the native
        /// split state is global.
        /// </summary>
        /// <param name="edi">Raw X12 bytes.</param>
        /// <param name="configJson">Split + optional validate/ack configuration JSON (required).</param>
        /// <param name="mode">Requested operation mode.</param>
        public static IEnumerable<ParseResult> Split(byte[] edi, string configJson, ParseMode mode = ParseMode.Json)
        {
            byte[] configBytes = Encoding.UTF8.GetBytes(configJson);
            var results = new List<ParseResult>();

            lock (_splitLock)
            {
                unsafe
                {
                    fixed (byte* inPtr = edi)
                    fixed (byte* cfgPtr = configBytes)
                    {
                        ThrowIfError(X12NativeImport.StartSplit(inPtr, edi.Length, (int)mode, cfgPtr, configBytes.Length));
                    }

                    bool isDone = false;
                    while (!isDone)
                    {
                        int resultSize = 0, resultOffset = 0;
                        byte last = 0;

                        ThrowIfError(X12NativeImport.Split(&resultSize, &resultOffset, &last));
                        isDone = last != 0;

                        if (resultSize == 0)
                            break;

                        if (!isDone || mode == ParseMode.Json)
                        {
                            // Intermediate segment result — JSON only
                            results.Add(new ParseResult { Json = FetchResult(resultSize) });
                        }
                        else
                        {
                            // Final result — may contain errors/ACK appended after JSON
                            if (resultOffset == 0)
                            {
                                results.Add(new ParseResult { Json = FetchResult(resultSize) });
                            }
                            else
                            {
                                var (j, e) = FetchResult(resultSize, resultOffset);
                                results.Add(new ParseResult { Json = j, Errors = e });
                            }
                        }
                    }
                }
            }

            return results;
        }

        // ── Build ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an X12 string from a transaction-set JSON document.
        /// </summary>
        /// <param name="json">Transaction set JSON bytes.</param>
        /// <param name="segmentPostfix">
        /// Optional string appended after each segment terminator (e.g. <c>"\r\n"</c>
        /// for line-separated output). Pass <see langword="null"/> for compact output.
        /// </param>
        /// <returns>X12 string (UTF-8 encoded in the native layer).</returns>
        public static string Build(byte[] json, string? segmentPostfix = null)
        {
            int capacity = json.Length;

            unsafe
            {
                fixed (byte* inPtr = json)
                {
                    while (true)
                    {
                        byte[] output = GC.AllocateUninitializedArray<byte>(capacity);
                        int resultSize = 0;

                        fixed (byte* outPtr = output)
                        {
                            int rc = X12NativeImport.Build(inPtr, json.Length, segmentPostfix, outPtr, capacity, &resultSize);

                            if (rc == InsufficientCapacity)
                            {
                                capacity = resultSize;
                                continue;
                            }

                            ThrowIfError(rc);
                            return Encoding.UTF8.GetString(output, 0, resultSize);
                        }
                    }
                }
            }
        }

        // ── Merge ────────────────────────────────────────────────────────────

        /// <summary>
        /// Merges a JSON document back into X12, returning
        /// all produced segments concatenated with <c>"\r\n"</c> line endings.
        /// </summary>
        /// <param name="json">Full X12 interchange JSON bytes (as produced by Parse/Split).</param>
        /// <param name="postfix">Optinal postfix inserted between segments in the output.</param>
        public static string Merge(byte[] json, string postfix = "\r\n")
        {
            var sb = new StringBuilder();

            lock (_splitLock)
            {
                unsafe
                {
                    fixed (byte* inPtr = json)
                        ThrowIfError(X12NativeImport.StartMerge(inPtr, json.Length));

                    while (true)
                    {
                        int resultSize = 0;
                        ThrowIfError(X12NativeImport.Merge(&resultSize));

                        if (resultSize == 0)
                            break;

                        sb.Append(FetchResult(resultSize));
                        sb.Append(postfix);
                    }
                }
            }

            return sb.ToString();
        }

        // ── Error descriptions ───────────────────────────────────────────────

        /// <summary>Returns the human-readable description for a native error code.</summary>
        public static string GetError(int errorCode) => X12NativeImport.GetError(errorCode);

        // ── Internal helpers ─────────────────────────────────────────────────

        private static void ThrowIfError(int rc)
        {
            if (rc == 0) return;
            string message = X12NativeImport.GetError(rc);
            throw new X12NativeException(rc, message);
        }

        private static unsafe string FetchResult(int resultSize)
        {
            byte[] buffer = new byte[resultSize];
            fixed (byte* p = buffer)
                ThrowIfError(X12NativeImport.GetResult(p, buffer.Length));
            return Encoding.UTF8.GetString(buffer);
        }

        private static unsafe (string json, string errors) FetchResult(int resultSize, int resultOffset)
        {
            byte[] buffer = new byte[resultSize];
            fixed (byte* p = buffer)
                ThrowIfError(X12NativeImport.GetResult(p, buffer.Length));

            string json   = Encoding.UTF8.GetString(buffer, 0, resultOffset);
            string errors = Encoding.UTF8.GetString(buffer, resultOffset, resultSize - resultOffset);
            return (json, errors);
        }
    }

    /// <summary>
    /// Exception thrown when the native AOT library returns a non-zero error code.
    /// </summary>
    public sealed class X12NativeException : Exception
    {
        /// <summary>The raw integer error code returned by the native function.</summary>
        public int ErrorCode { get; }

        public X12NativeException(int errorCode, string message)
            : base($"[{errorCode}] {message}")
        {
            ErrorCode = errorCode;
        }
    }
}
