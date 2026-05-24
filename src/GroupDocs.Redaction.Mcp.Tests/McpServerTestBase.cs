using GroupDocs.Redaction.Mcp.IntegrationTests.Fixtures;
using Xunit;

namespace GroupDocs.Redaction.Mcp.IntegrationTests;

/// Base class for every integration test class. xUnit instantiates a test class
/// once PER TEST METHOD, so each test method gets its own fresh MCP server
/// process via the IAsyncLifetime hooks below.
///
/// Why per-test and NOT a shared `ICollectionFixture` server:
/// GroupDocs.Redaction's evaluation mode caps document opens at ONE per process
/// ("Trial mode allows only 1 document to open"). A single server shared across
/// the whole suite throws TrialLimitationsException on the second tool call. A
/// fresh process per test resets the counter; no single test opens more than one
/// document, so the cap is never reached. The suite runs fully UNLICENSED.
public abstract class McpServerTestBase : IAsyncLifetime
{
    protected McpServerFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new McpServerFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fixture is not null)
            await _fixture.DisposeAsync();
    }
}
