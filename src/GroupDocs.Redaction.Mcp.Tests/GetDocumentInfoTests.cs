using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// GetDocumentInfo is read-only — it never calls Save(), so it works identically
/// in evaluation and licensed mode. Returns raw JSON describing the document.
[Collection(McpServerCollection.Name)]
public class GetDocumentInfoTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GetDocumentInfoTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Theory]
    [InlineData(SampleDocuments.SamplePdf)]
    [InlineData(SampleDocuments.SampleDocx)]
    public async Task GetDocumentInfo_RealSample_ReturnsFileTypeAndPageCount(string fileName)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.GetDocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error for '{fileName}': {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal(fileName, json.GetProperty("fileName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("fileType").GetString()),
            "Expected a non-empty fileType.");
        Assert.True(json.GetProperty("pageCount").GetInt32() >= 0,
            "Expected a non-negative pageCount.");
    }

    [Fact]
    public async Task GetDocumentInfo_Pdf_ReportsPageDimensions()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.SamplePdf)))
        {
            _output.WriteLine("sample.pdf not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.GetDocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SamplePdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        // The pages array is optional in the schema, but a PDF should report at least one page.
        Assert.True(json.TryGetProperty("pages", out var pages),
            "Expected a 'pages' property in the response.");
        Assert.Equal(System.Text.Json.JsonValueKind.Array, pages.ValueKind);
        Assert.True(pages.GetArrayLength() >= 1, "Expected at least one page entry.");

        var first = pages[0];
        Assert.True(first.GetProperty("width").GetInt32() > 0, "Expected positive page width.");
        Assert.True(first.GetProperty("height").GetInt32() > 0, "Expected positive page height.");
    }
}
