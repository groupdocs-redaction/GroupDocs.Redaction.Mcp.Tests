using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// RedactText calls Redactor.Save(), which GroupDocs.Redaction permits in
/// evaluation mode (the saved file is watermarked rather than blocked). So these
/// tests verify end-to-end output-file creation without requiring a license.
public class RedactTextTests : McpServerTestBase
{
    private readonly ITestOutputHelper _output;

    public RedactTextTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TextSamples() => new[]
    {
        new object[] { SampleDocuments.SampleDocx, "sample_redacted.docx" },
        new object[] { SampleDocuments.SamplePdf,  "sample_redacted.pdf" },
    };

    [Theory]
    [MemberData(nameof(TextSamples))]
    public async Task RedactText_RealSample_WritesRedactedOutput(string fileName, string expectedOutputName)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // Remove any redacted copy a sibling test may have produced (the shared
        // collection storage and the common `_redacted` suffix mean the same output
        // name is reused) so File.Exists below proves *this* call wrote the file.
        var outputPath = Path.Combine(_fixture.StoragePath, expectedOutputName);
        if (File.Exists(outputPath)) File.Delete(outputPath);

        // SSN-style pattern. Whether or not the document contains a match, the
        // redaction completes successfully and a redacted copy is written.
        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactText.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
                ["pattern"] = @"\d{3}-\d{2}-\d{4}",
                ["replacement"] = "[REDACTED]",
            });

        Assert.False(response.IsError ?? false,
            $"RedactText failed for '{fileName}': {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.True(File.Exists(outputPath),
            $"Expected redacted output at '{outputPath}'. Response body:\n{body}");
    }

    [Fact]
    public async Task RedactText_AcceptsPasswordParameter()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.SampleDocx)))
        {
            _output.WriteLine("sample.docx not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // Wrong/extra password on an unprotected file — the tool must accept the
        // schema and return a result (success or graceful error), not reject the call.
        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactText.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SampleDocx },
                ["pattern"] = "confidential",
                ["password"] = "",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);
        Assert.False(string.IsNullOrEmpty(body));
    }
}
