# ediFabric Native Examples for X12

## 1. Overview
ediFabric Native is a true native blazing-fast cross-platform library (Windows, Linux, macOS) with bindings for .NET, Java, Python, C++, and more. It allows you to parse, split, validate, acknowledge, build, and merge X12 files.

## 2. Installation
- To run the examples you need Visual Studio 2026+. [Download Visual Studio](https://visualstudio.microsoft.com/downloads/).
- Build EdiFabric.Native solution
- Download edifabric-x12-tools.dll from edifabric.com and copy it to the Debug\net10.0 and Release\net10.0 folders.
- Run X12.exe in command prompt

## 3. Authentication
The examples use the serial key for the free plan. You don't need to aquire a separate serial key to run the examples.

## 4. Quick Start
To parse an X12 file in C# do:
```C#
//  Authenticate
var token = X12Client.GetToken(Examples.serialKey);
X12Client.SetToken(token);

//  Configure X12 model resolution (local map or online)
var mapFile = File.ReadAllBytes(@"..\YourFolder\X12ModelsMap.json");
X12Client.SetMap(mapFile);

//  Parse an X12 file
var x12File = File.ReadAllBytes(@"..\YourFolder\YourX12File.txt");
var result = X12Client.Parse(x12File, ParseMode.Json);
```

## 5. Examples Section
- How to read, parse or translate X12 files, go to [Parse_X12_Files](./X12/Examples/Parse_X12_Files.cs).
- How to validate X12 transactions and envelopes, go to [Validate_X12_Files](./X12/Examples/Validate_X12_Files.cs).
- How to generate X12 acknowledgments such as TA1, 999, and 997, go to [Generate_X12_Acknowledgments](./X12/Examples/Generate_X12_Acknowledgments.cs).
- How to create or build X12 files, go to [Create_X12_Files](./X12/Examples/Create_X12_Files.cs).

## 6. API Reference
For full method/API reference go to [X12Client](./X12/X12Client.cs).

## 7. EDI Models
Download X12 models as JSON from [EdiNation]([https://visualstudio.microsoft.com/downloads/](https://edination.edifabric.com/edi-spec-library.html)). 

## 8. Warranty
*The source code in these example projects is strictly for demonstrational purposes and is provided "AS IS" without warranty of any kind, whether expressed or implied, including but not limited to the implied warranties of merchantability and/or fitness for a particular purpose.*

## 9. Additional information

[Support](https://support.edifabric.com/hc/en-us/requests/new)

### 2026 © EdiFabric
