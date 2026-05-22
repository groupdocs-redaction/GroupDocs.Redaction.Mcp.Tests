using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// EraseMetadata calls Redactor.Save(), permitted (watermarked) in evaluation
/// mode, so these tests verify output-file creation without a license.
[Collection(McpServerCollection.Name)]
public class EraseMetadataTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public EraseMetadataTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public static IEnumerable<object[]> MetadataSamples() => new[]
    {
        new object[] { SampleDocuments.SampleDocx, "sample_redacted.docx" },
        new object[] { SampleDocuments.SampleXlsx, "sample_redacted.xlsx" },
    };

    [Theory]
    [MemberData(nameof(MetadataSamples))]
    public async Task EraseMetadata_AllFields_WritesCleanedOutput(string fileName, string expectedOutputName)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // The shared collection storage and common `_redacted` suffix mean a sibling
        // test may have written this output name already — delete it so File.Exists
        // below proves *this* call produced the file.
        var outputPath = Path.Combine(_fixture.StoragePath, expectedOutputName);
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var response = await _fixture.Client.CallToolAsync(
            catalog.EraseMetadata.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
                ["fields"] = "All",
            });

        Assert.False(response.IsError ?? false,
            $"EraseMetadata failed for '{fileName}': {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.True(File.Exists(outputPath),
            $"Expected cleaned output at '{outputPath}'. Response body:\n{body}");
    }

    [Fact]
    public async Task EraseMetadata_SpecificFields_Succeeds()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.SampleDocx)))
        {
            _output.WriteLine("sample.docx not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.EraseMetadata.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SampleDocx },
                ["fields"] = "Author,Company,CreatedTime",
            });

        Assert.False(response.IsError ?? false,
            $"EraseMetadata failed: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);
        Assert.False(string.IsNullOrEmpty(body));
    }
}
