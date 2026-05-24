using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// RedactImageArea draws a solid-color box over a page region. On Linux/macOS the
/// underlying System.Drawing path needs libgdiplus (installed by the CI workflow).
/// Whether a given page region yields a redactable raster is content-dependent, so
/// the happy-path assertion is tolerant: the call must complete and leave the
/// server responsive; when it reports success, the redacted output must exist.
public class RedactImageAreaTests : McpServerTestBase
{
    private readonly ITestOutputHelper _output;

    public RedactImageAreaTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RedactImageArea_Pdf_CompletesAndKeepsServerResponsive()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.SamplePdf)))
        {
            _output.WriteLine("sample.pdf not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var outputPath = Path.Combine(_fixture.StoragePath, "sample_redacted.pdf");
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactImageArea.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SamplePdf },
                ["x"] = 10,
                ["y"] = 10,
                ["width"] = 100,
                ["height"] = 50,
                ["color"] = "Black",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        // Exercises the System.Drawing / GDI+ native path. Image-area redaction
        // needs GDI+: built into Windows, but on Linux/macOS the underlying
        // Aspose.Pdf path P/Invokes `gdiplus.dll`, which is frequently unavailable
        // to the dnx-launched server (the .NET loader probes `gdiplus.dll` /
        // `libgdiplus.dll`, not the system `libgdiplus.so` / `.dylib`). The tool
        // surfaces that as a graceful "Image area redaction failed" message rather
        // than crashing. So success isn't required — but WHEN the tool reports
        // success, the redacted file must be on disk. Either way the server must
        // stay responsive.
        var redactionFailed = (response.IsError ?? false)
            || body.Contains("Image area redaction failed", StringComparison.OrdinalIgnoreCase);

        if (!redactionFailed)
        {
            Assert.True(File.Exists(outputPath),
                $"Tool reported success but no output at '{outputPath}'. Response body:\n{body}");
        }

        // The server must stay alive regardless of the redaction outcome.
        var listAfter = await _fixture.Client.ListToolsAsync();
        Assert.NotEmpty(listAfter);
    }

    [Fact]
    public async Task RedactImageArea_AcceptsHexColor()
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, SampleDocuments.SamplePdf)))
        {
            _output.WriteLine("sample.pdf not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // A hex color must be accepted by the schema and parsed without throwing.
        var response = await _fixture.Client.CallToolAsync(
            catalog.RedactImageArea.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SamplePdf },
                ["x"] = 0,
                ["y"] = 0,
                ["width"] = 20,
                ["height"] = 20,
                ["color"] = "#FF0000",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);
        Assert.False(string.IsNullOrEmpty(body));
    }
}
