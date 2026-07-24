using X12;

// ═══════════════════════════════════════════════════════════════════════════════
//  edifabric-x12-tools — Console wrapper/examples
//
//  Prerequisites
//  ─────────────
//  1. Download edifabric-x12-tools.dll from edifabric
//
//  2. Copy edifabric-x12-tools.dll to the Debug\net10.0 and Release\net10.0 folders
//
//  3. Follow the steps in the README file.
// ═══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Parse X12 files");
Console.WriteLine(new string('─', 60));
// ─────────────────────────────────────────────────────────────────────────────

Run("Parse X12 with local models", Examples.Parse_to_json_with_local_models);
Run("Parse X12 with online models", Examples.Parse_to_json_with_online_models);
Run("Parse large X12 with splitting", Examples.Parse_to_json_with_splitting);

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Validate X12 files");
Console.WriteLine(new string('─', 60));
// ─────────────────────────────────────────────────────────────────────────────

Run("Validate X12", Examples.Validate_valid_x12);
Run("Validate invalid X12", Examples.Validate_invalid_x12);
Run("Validate large X12 with splitting", Examples.Validate_with_splitting);

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Generate X12 TA1 999 or 997 acknowledgmnets");
Console.WriteLine(new string('─', 60));
// ─────────────────────────────────────────────────────────────────────────────

Run("Generate X12 ack", Examples.Ack_valid_x12);
Run("Generate X12 ack with validation errors", Examples.Ack_invalid_x12);

// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Create X12 files");
Console.WriteLine(new string('─', 60));
// ─────────────────────────────────────────────────────────────────────────────

Run("Create X12 file from JSON", Examples.Create_x12_without_postfix);
Run("Create X12 file from JSON with a new line after each segment", Examples.Create_x12_with_postfix);
Run("Create X12 file from JSON, segment by segment", Examples.Create_x12_by_merging_segments);

// ── Helper utilities ──────────────────────────────────────────────────────────

static void Ok(string label)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("  OK ");
    Console.ResetColor();
    Console.WriteLine(label);
}

static void Fail(string label, string reason)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write("  ERROR ");
    Console.ResetColor();
    Console.WriteLine($"{label}: {reason}");
}

void Run(string label, Action test)
{
    try
    {
        X12Client.SetSerial(Examples.serialKey);
        test();
        Ok(label);
    }
    catch (Exception ex)
    {
        Fail(label, ex.Message);
    }
    finally
    {
        // Always reset native state between runs so they are independent.
        try { X12Client.ClearCache(); } catch { /* swallow — already failed */ }
    }
}