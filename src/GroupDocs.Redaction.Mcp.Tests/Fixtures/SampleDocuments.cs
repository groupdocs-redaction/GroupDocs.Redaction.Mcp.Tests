namespace GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;

/// Copies the real sample documents committed under the repo's `sample-docs/`
/// folder into the test storage directory so tests exercise real formats.
/// GroupDocs.Redaction permits Save() in evaluation mode (the output is just
/// watermarked), so — unlike metadata removal — these redaction tests can verify
/// output-file creation end-to-end without a license.
internal static class SampleDocuments
{
    // Real samples committed under sample-docs/ — copied from the source folder
    // (env var or csproj-staged copy under bin/) into the test storage directory.
    public const string SampleDocx = "sample.docx";   // text + document metadata
    public const string SamplePdf = "sample.pdf";     // text + page geometry for image-area redaction
    public const string SampleXlsx = "sample.xlsx";   // spreadsheet metadata
    public const string AnnotatedXlsx = "annotated.xlsx"; // workbook carrying cell comments (annotations)

    public static IReadOnlyList<string> RealSamples { get; } = new[]
    {
        SampleDocx, SamplePdf, SampleXlsx, AnnotatedXlsx,
    };

    /// Copies real sample files (those in RealSamples) from the resolved source
    /// directory into the test storage directory. Files not present in the source
    /// are skipped — the corresponding tests detect absence and skip themselves.
    public static void CopyRealSamples(string targetDirectory, string? sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(targetDirectory);
        foreach (var name in RealSamples)
        {
            var src = Path.Combine(sourceDirectory, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(targetDirectory, name), overwrite: true);
        }
    }

    /// Resolves the source folder containing real sample files. Order:
    ///   1. GROUPDOCS_MCP_SAMPLE_DOCS env var (set by docker-compose mount).
    ///   2. `sample-docs/` next to the test assembly — populated by the csproj
    ///      `<None Include="..\..\sample-docs\**\*">` copy item.
    ///   3. Walk up from the assembly to find the repo's `sample-docs/`.
    public static string? ResolveSourceSampleDocs()
    {
        var env = Environment.GetEnvironmentVariable("GROUPDOCS_MCP_SAMPLE_DOCS");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
            return env;

        var staged = Path.Combine(AppContext.BaseDirectory, "sample-docs");
        if (Directory.Exists(staged))
            return staged;

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "sample-docs");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
