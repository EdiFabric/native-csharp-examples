# ediFabric Native C# Examples

**ediFabric Native** is a self-contained, high-performance X12 EDI engine compiled
ahead-of-time to a native shared library. It converts X12 EDI to JSON (and back),
validates transaction sets, and generates acknowledgments — callable from **any
language with a C foreign-function interface** (C, C++, Rust, Go, Python, Node.js,
Java/JNA, .NET, …). No .NET runtime, JVM, or other dependency is required on the
target machine.

- **Fast** — native code, zero-copy UTF‑8 buffers, streaming split/merge.
- **Portable** — a single native library per platform, no runtime install.
- **Simple ABI** — a handful of C entry points returning integer status codes.

Copyright © EdiFabric 2026. Use of this library requires a valid license
(see [Licensing](#licensing)).

---

## Contents

- [Package](#package)
- [Requirements](#requirements)
- [Quick start](#quick-start)
- [Licensing](#licensing)
- [API reference](#api-reference)
  - [Model](#model)
  - [Parse](#parse)
  - [Split (streaming)](#split-streaming)
  - [Build](#build)
  - [Merge (streaming)](#merge-streaming)
  - [Results & errors](#results--errors)
  - [Logging & lifecycle](#logging--lifecycle)
- [Operation modes](#operation-modes)
- [Configuration JSON](#configuration-json)
- [Error codes](#error-codes)
- [Threading model](#threading-model)
- [Language bindings](#language-bindings)
- [Support](#support)

---

## Package

The distribution contains one native library per platform:

| Platform | File                          |
|----------|-------------------------------|
| Windows  | `edifabric-x12-tools.dll`     |
| Linux    | `edifabric-x12-tools.so`      |
| macOS    | `edifabric-x12-tools.dylib`   |

Plus your **model files** (per transaction set) and a **map file** that tells the
engine where to find them.

---

## Requirements

- A supported 64-bit OS (Windows / Linux / macOS).
- Internet access **only** for one-time license installation / token retrieval.
- No .NET runtime — the library is fully self-contained.

---

## Quick start

1. **Load the library** with your language's FFI.
2. **Authorize** with a token or serial (see [Licensing](#licensing)).
3. **Load the model map** once with `set_map`.
4. **Parse** EDI to JSON with `parse` (or stream with `start_split` / `split`).

```text
set_token(token)          // or set_serial(serial)
set_map(mapJson)          // once
parse(edi, mode=1, …)     // EDI → JSON
```

All strings and payloads cross the boundary as **UTF‑8 byte buffers**
(`pointer + length`). Every function returns `0` on success or a non-zero
[error code](#error-codes).

---

## Licensing

A license is validated during parse/build operations. Choose one of two models:

### Token (recommended — offline, high throughput, NOT available for free or developer license types)

```text
get_token(serial)  → token      // one-time, requires internet; store the token
set_token(token)                // per process start; validated cryptographically
```

Tokens expire — query `get_token_expiration` and refresh with `get_token` as needed.

### Serial (online — validates against the license server per operation)

```text
install_license(serial)         // one-time per machine, requires internet
set_serial(serial)              // per process start
```

Use **token** mode for containers, air-gapped, or high-volume scenarios;
**serial** mode is simplest for low-volume, always-online use.

| Entry point            | Signature                                                                 | Purpose |
|------------------------|--------------------------------------------------------------------------|---------|
| `install_license`      | `int install_license(byte* serial, int len)`                             | Register this machine (once, online). |
| `get_token`            | `int get_token(byte* serial, int len, byte* out, int cap, int* outLen)`  | Fetch a signed token (grow-and-retry). |
| `set_token`            | `int set_token(byte* token, int len)`                                    | Cache a token for this process. |
| `validate_token`       | `int validate_token(byte* token, int len)`                               | Validate a token without caching. |
| `get_token_expiration` | `int get_token_expiration(long* expUtc)`                                 | Token expiry (UTC ticks; `0` = none). |
| `set_serial`           | `int set_serial(byte* serial, int len)`                                  | Cache a serial for runtime auth. |
| `get_app_version`      | `int get_app_version(int* version)`                                      | Library application version. |

---

## API reference

Conventions used by every function:

- Returns `int`: `0` = success, non-zero = [error code](#error-codes).
- Inputs are UTF‑8 `byte*` + `int length`.
- Output functions use **grow-and-retry**: if the buffer is too small the call
  returns `1` (`InsufficientCapacity`) and writes the required size into the
  length out-parameter; reallocate and call again.
- Exceptions never cross the boundary.

### Model

```c
int set_map(byte* map, int mapLength);
```
Loads the [template map](#template-map-set_map) that resolves EDI transaction sets
to model files. **Call once before any parse/split.** Cached until replaced.

### Parse

```c
int parse(byte* input, int inputLength,
          int mode,
          byte* config, int configLength,
          byte* output, int outputCapacity,
          int* outputLength, int* outputOffset);
```
Parses a whole interchange in one call.

- `mode` — see [operation modes](#operation-modes).
- `config` — optional [ParseConfig](#parseconfig-parse--start_split) JSON (`null` allowed).
- Output = transaction-set JSON, followed by validation/ACK JSON when `mode ≥ 2`:
  - `*outputOffset` = start of the errors/ACK section (`0` when `mode == 1`).
  - `*outputLength` = total bytes written.

### Split (streaming)

```c
int start_split(byte* input, int inputLength, int mode, byte* config, int configLength);
int split(int* resultSize, int* resultOffset, byte* last);
```
Streams one transaction set at a time (low, flat memory use for large files).

- `config` **must** include a [`split`](#parseconfig-parse--start_split) section with a `segment_id`.
- Call `split` repeatedly; for each step with `*resultSize > 0`, fetch the payload
  with [`get_result`](#results--errors). `*last == 1` marks the final result.

### Build

```c
int build(byte* input, int inputLength,
          void* postfix,
          byte* output, int outputCapacity, int* outputLength);
```
Builds an X12 EDI string from transaction-set JSON. `postfix` is an optional
null-terminated string appended after each segment terminator (e.g. `"\r\n"`);
pass `null` for compact output. Grow-and-retry as with `parse`.

### Merge (streaming)

```c
int start_merge(byte* input, int inputLength);
int merge(int* resultSize);
```
Streams X12 segments from a full interchange JSON document. Each `merge` returns
one segment (fetch with `get_result`); `*resultSize == 0` ends the stream.

### Results & errors

```c
int    get_result(byte* buffer, int bufferSize);   // copy the last split/merge result
void*  get_error(int errorCode);                    // null-terminated message string
```
- `get_result` — `bufferSize` **must equal** the `resultSize` returned by the
  preceding `split`/`merge` call. The internal buffer is consumed on success.
- `get_error` — returns a human-readable message. **The caller must free the
  returned string** (`free` / `Marshal.FreeHGlobal`).

### Logging & lifecycle

```c
int init_logger(byte* pathUtf8, int pathLen, int minLevel);  // 0=Trace..4=Error
int shutdown_logger();
int clear_cache();   // reset model, split/merge state, results, license, logger
```

---

## Operation modes

| `mode` | Output                                            |
|-------:|---------------------------------------------------|
| `1`    | JSON only                                          |
| `2`    | JSON + validation error report                     |
| `3`    | JSON + validation report + acknowledgment (999/997/TA1) |

Any other value returns `IncorrectMode` (`616`).

---

## Configuration JSON

All JSON uses **`snake_case`** keys and is case-insensitive.

### Template map (`set_map`)

Map keys are `message:version`. `type: 1` loads the model
from a local file at `location/name`. Set `default` to fall back to the online
service for unmapped transaction sets.

```json
{
  "default": null,
  "maps": {
    "837:005010X222A1": { "type": 1, "name": "837P.json", "location": "/opt/models" },
    "834:005010X220A1": { "type": 1, "name": "834.json",  "location": "/opt/models" }
  }
}
```
### Models

All X12 transactions, such as 837P, 834, 850, etc. are represented as proprietary JSON. 
Download a standard model from [EdiNation Spec Library](https://edination.edifabric.com/edi-spec-library.html), 
or a custom model from [EdiNation Spec Builder](https://edination.edifabric.com/edi-spec-builder.html). 
Create/modify models in OpenEDI format, upload them in EdiNation Spec Builder and download them as JSON for use in ediFabric Native.

To download a model in either EdiNation Spec Library or EdiNation Spec Builder, 
select the model first, then in the JSON view select the Download button in the top right corner:
![Test](https://github.com/EdiFabric/native-csharp-examples/blob/main/model.png)

### ParseConfig (`parse` / `start_split`)

```json
{
  "split":    { "segment_id": "ST", "segment_depth": 0, "loop_id": null },
  "validate": { "regex": null, "date_format": null, "time_format": null,
                "skip_seq_count": false, "skip_hl_seq": false,
                "snip_level": 0, "max_errors": 0 },
  "ack":      { "supress_ta1": false, "ak901p": false,
                "gen_for_valid": false, "gen997": false }
}
```

- `split` — required for `start_split`; `segment_id` is the boundary (usually `ST`).
- `validate` — applied when `mode ≥ 2`; `snip_level` is `1`–`4`.
- `ack` — applied when `mode == 3`.

All sections are optional for `parse`.

---

## Error codes

`0` = success. Envelope/segment/element codes originate from the validation
engine; the library-level codes are:

| Code | Meaning |
|-----:|---------|
| `1`  | Output buffer too small — retry with the returned required size. |
| `611`| Incorrect/empty input. |
| `612`| Logger initialization error. |
| `613`| Map JSON deserialization failed. |
| `614`| Incorrect (negative) output capacity. |
| `615`| Model map not set (call `set_map` first). |
| `616`| Incorrect mode (must be 1–3). |
| `617`| No JSON produced. |
| `618`| Validation result unavailable. |
| `619`| Validation report serialization failed. |
| `620`| Incorrect token. |
| `621`| Config JSON deserialization failed. |
| `622`| Split `segment_id` missing/empty. |
| `623`| `split` called before `start_split`. |
| `624`| No result available for `get_result`. |
| `625`| `get_result` buffer size mismatch. |
| `626`| `merge` called before `start_merge`. |
| `627`| Incorrect/null output pointer. |
| `628`| Incorrect serial. |
| `629`| License not installed (run `install_license`). |
| `630`| Application maximum version exceeded. |
| `631`| Token expired. |
| `632`| Token missing. |
| `633`| Maximum licenses exceeded. |
| `634`| License snapshot not found. |
| `635`| License not set (call `set_token` or `set_serial`). |

Call `get_error(code)` at runtime for the descriptive message.

---

## Threading model

The library holds **process-global** state (loaded map, active split reader,
active merge writer, last result, license) protected by an internal lock.

- `parse` and `build` are one-shot and independent per call.
- `start_split`/`split`/`get_result` and `start_merge`/`merge`/`get_result` are
  **stateful sequences**: run each sequence to completion without interleaving it
  with another split/merge on another thread. Serialize these sequences in your
  application.

---

## Language bindings

### C

```c
extern int  set_map(const unsigned char* map, int len);
extern int  parse(const unsigned char* in, int inLen, int mode,
                  const unsigned char* cfg, int cfgLen,
                  unsigned char* out, int cap, int* outLen, int* outOff);
extern void* get_error(int code);
```

### Python (ctypes)

```python
import ctypes, json

lib = ctypes.CDLL("./edifabric-x12-tools.so")     # .dll / .dylib on other OSes
lib.parse.restype = ctypes.c_int

def set_map(map_json: str):
    b = map_json.encode("utf-8")
    if lib.set_map(b, len(b)) != 0:
        raise RuntimeError("set_map failed")

def parse(edi: bytes, mode: int = 1) -> str:
    cap = len(edi) * 12
    out_len = ctypes.c_int(0); out_off = ctypes.c_int(0)
    while True:
        out = ctypes.create_string_buffer(cap)
        rc = lib.parse(edi, len(edi), mode, None, 0, out, cap,
                       ctypes.byref(out_len), ctypes.byref(out_off))
        if rc == 1:                     # InsufficientCapacity
            cap = out_len.value; continue
        if rc != 0:
            raise RuntimeError(f"parse failed: {rc}")
        return out.raw[:out_len.value].decode("utf-8")
```

### .NET (P/Invoke)

```csharp
[DllImport("edifabric-x12-tools", EntryPoint = "parse")]
static extern unsafe int Parse(byte* input, int inputLen, int mode,
                               byte* config, int configLen,
                               byte* output, int outputCap,
                               int* outputLen, int* outputOffset);
```

Follow the same UTF‑8 buffer + grow-and-retry + `get_result` contracts from any
other FFI-capable language.

---

## Support

Questions, licensing, and model files: **support@edifabric.com**

Copyright © EdiFabric 2026. All rights reserved.
