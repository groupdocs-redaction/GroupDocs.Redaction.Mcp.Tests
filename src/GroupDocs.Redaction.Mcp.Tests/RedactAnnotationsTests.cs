using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// RedactAnnotations targets comments / sticky notes. annotated.xlsx ships with
/// cell comments. Save() is permitted (watermarked) in evaluation mode.
public class RedactAnnotationsTests : McpServerTestBase
{
    private readonly ITestOutputHelper _output;

    public RedactAnnotationsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RedactAnnotations_ReplaceMatching_WritesRedactedOutput()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.AnnotatedXlsx)))
        {
            _output.WriteLine("annotated.xlsx not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var outputPath = Path.Combine(_fixture.StoragePath, "annotated_redacted.xlsx");
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactAnnotations.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AnnotatedXlsx },
                ["pattern"] = "(?im:john)",
                ["replacement"] = "[REDACTED]",
            });

        Assert.False(response.IsError ?? false,
            $"RedactAnnotations failed: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.True(File.Exists(outputPath),
            $"Expected redacted output at '{outputPath}'. Response body:\n{body}");
    }

    [Fact]
    public async Task RedactAnnotations_DeleteAll_WritesRedactedOutput()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.AnnotatedXlsx)))
        {
            _output.WriteLine("annotated.xlsx not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var outputPath = Path.Combine(_fixture.StoragePath, "annotated_redacted.xlsx");
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactAnnotations.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AnnotatedXlsx },
                ["deleteAll"] = true,
            });

        Assert.False(response.IsError ?? false,
            $"RedactAnnotations (deleteAll) failed: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.True(File.Exists(outputPath),
            $"Expected redacted output at '{outputPath}'. Response body:\n{body}");
    }
}
