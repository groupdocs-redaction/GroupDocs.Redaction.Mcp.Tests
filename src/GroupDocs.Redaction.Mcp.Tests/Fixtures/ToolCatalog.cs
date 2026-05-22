using ModelContextProtocol.Client;

namespace GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;

/// Resolves tool names by keyword. The server-side attribute [McpServerTool] today
/// exposes snake_case names (redact_text, erase_metadata, redact_annotations,
/// redact_image_area, get_document_info), but keyword-based resolution keeps tests
/// robust against future renames / casing convention changes.
internal sealed class ToolCatalog
{
    private readonly IReadOnlyList<McpClientTool> _tools;

    private ToolCatalog(IReadOnlyList<McpClientTool> tools) => _tools = tools;

    public static async Task<ToolCatalog> LoadAsync(McpClient client, CancellationToken ct = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return new ToolCatalog(tools.ToList());
    }

    public IReadOnlyList<McpClientTool> All => _tools;

    public McpClientTool RedactText => Resolve("text");
    public McpClientTool EraseMetadata => Resolve("erase");
    public McpClientTool RedactAnnotations => Resolve("annotation");
    public McpClientTool RedactImageArea => Resolve("image");
    public McpClientTool GetDocumentInfo => Resolve("info");

    private McpClientTool Resolve(string keyword) =>
        _tools.FirstOrDefault(t => t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No tool with name containing '{keyword}'. Found: {string.Join(", ", _tools.Select(t => t.Name))}");
}
