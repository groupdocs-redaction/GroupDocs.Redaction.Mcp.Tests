using Xunit;

// Run the whole suite serially. Each test method spawns its OWN dnx-launched MCP
// server process (see McpServerTestBase — required to dodge the eval-mode 1-open
// document cap). With xUnit's default cross-collection parallelism, many dnx
// processes would start at once: on a cold CI cache they would race to restore
// the (large) package, and concurrent launches strain the runner. Disabling
// parallelization ensures the first launch warms the cache for the rest and only
// one server runs at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
