using System.Runtime.InteropServices;

namespace X12
{
    /// <summary>
    /// Raw P/Invoke bindings for the edifabric-x12-tools native AOT DLL.
    /// All methods map 1-to-1 to their [UnmanagedCallersOnly] entry points.
    /// Use <see cref="X12Client"/> for a higher-level managed API instead.
    /// </summary>
    internal static partial class X12NativeImport
    {
        private const string Lib = "edifabric-x12-tools";

        // ── Lifecycle ────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "clear_cache")]
        public static partial int ClearCache();

        // ── Logging ──────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "init_logger")]
        public static unsafe partial int InitLogger(byte* pathUtf8, int pathLen, int minLevel);

        [LibraryImport(Lib, EntryPoint = "shutdown_logger")]
        public static partial int ShutdownLogger();

        // ── Licensing ────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "install_license")]
        public static unsafe partial int InstallLicense(byte* input, int inputLength);

        [LibraryImport(Lib, EntryPoint = "get_app_version")]
        public static unsafe partial int GetAppVersion(int* appVersion);

        [LibraryImport(Lib, EntryPoint = "get_token")]
        public static unsafe partial int GetToken(byte* input, int inputLength, byte* output, int outputCapacity, int* outputLength);

        [LibraryImport(Lib, EntryPoint = "validate_token")]
        public static unsafe partial int ValidateToken(byte* input, int inputLength);

        [LibraryImport(Lib, EntryPoint = "set_token")]
        public static unsafe partial int SetToken(byte* input, int inputLength);

        [LibraryImport(Lib, EntryPoint = "get_token_expiration")]
        public static unsafe partial int GetTokenExpiration(long* expirationUtc);

        [LibraryImport(Lib, EntryPoint = "set_serial")]
        public static unsafe partial int SetSerial(byte* input, int inputLength);

        // ── Model ────────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "set_map")]
        public static unsafe partial int SetMap(byte* map, int mapLength);

        // ── Parse ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses X12 into JSON (and optionally validation errors + ACK).
        /// Returns 0 on success, 1 (InsufficientCapacity) when the output buffer is too small
        /// — in that case <paramref name="outputLength"/> holds the required size — or another
        /// non-zero error code on failure.
        /// </summary>
        [LibraryImport(Lib, EntryPoint = "parse")]
        public static unsafe partial int Parse(
            byte* input, int inputLength,
            int mode,
            byte* config, int configLength,
            byte* output, int outputCapacity,
            int* outputLength, int* outputOffset);

        // ── Split ────────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "start_split")]
        public static unsafe partial int StartSplit(byte* input, int inputLength, int mode, byte* config, int configLength);

        /// <summary>
        /// Advances to the next split result.
        /// <paramref name="last"/> is set to 1 (true) on the final call that returns the
        /// envelope-level result; after that, further calls return a non-zero error code.
        /// </summary>
        [LibraryImport(Lib, EntryPoint = "split")]
        public static unsafe partial int Split(int* resultSize, int* resultOffset, byte* last);

        // ── Build ────────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "build", StringMarshalling = StringMarshalling.Utf8)]
        public static unsafe partial int Build(byte* input, int inputLength, string? postfix, byte* output, int outputCapacity, int* outputLength);

        // ── Merge ────────────────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "start_merge")]
        public static unsafe partial int StartMerge(byte* input, int inputLength);

        [LibraryImport(Lib, EntryPoint = "merge")]
        public static unsafe partial int Merge(int* resultSize);

        // ── Result retrieval ─────────────────────────────────────────────────

        [LibraryImport(Lib, EntryPoint = "get_result")]
        public static unsafe partial int GetResult(byte* buffer, int bufferSize);

        // ── Error reporting ──────────────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable error message for <paramref name="errorCode"/>.
        /// The native side allocates the string from the global heap; the managed
        /// [LibraryImport] with <see cref="StringMarshalling.Utf8"/> reads and immediately
        /// frees it via CoTaskMemFree — which works because the native side uses
        /// <c>Marshal.StringToHGlobalAnsi</c> (HeapAlloc on the same heap).
        /// </summary>
        [LibraryImport(Lib, EntryPoint = "get_error", StringMarshalling = StringMarshalling.Utf8)]
        public static partial string GetError(int errorCode);
    }
}
