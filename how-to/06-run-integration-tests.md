# Run the integration tests

This repo's test suite validates the **published** `GroupDocs.Redaction.Mcp`
NuGet package end-to-end — it spawns the server via `dnx`, connects as an MCP
client, and exercises every advertised tool. Useful when you want to confirm a
release is healthy before promoting it, or gate CI on live smoke checks.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to nuget.org (the first run downloads the package)
- Optional: a GroupDocs license file to produce clean (non-watermarked) output

## Run locally

```bash
# All tests against the default pinned version (26.7.0)
dotnet test -c Release
```

## Run against a different published version

```bash
# Via MSBuild property
dotnet test -c Release -p:McpPackageVersion=26.7.0

# Or via env var
MCP_PACKAGE_VERSION=26.7.0 dotnet test -c Release
```

Version resolution order (highest wins):

1. `MCP_PACKAGE_VERSION` environment variable
2. `McpPackageVersion` MSBuild property → baked into assembly metadata
3. Default: `26.7.0`

## License (optional — removes output watermarks)

GroupDocs.Redaction succeeds in evaluation mode — all redaction tests pass
without a license, and output files are produced. The license only removes the
evaluation watermark from output.

```bash
export GROUPDOCS_LICENSE_PATH=/absolute/path/to/GroupDocs.Total.lic
dotnet test -c Release
```

The license path is forwarded into the server child process by
`McpServerFixture`.

## Run a subset

```bash
# Only discovery (fastest — no tool invocations after handshake)
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"

# Only redact-text tests
dotnet test -c Release --filter "FullyQualifiedName~RedactText"

# Only erase-metadata tests
dotnet test -c Release --filter "FullyQualifiedName~EraseMetadata"

# Only error-handling tests
dotnet test -c Release --filter "FullyQualifiedName~ErrorHandling"
```

## Expected output

```
Passed  ToolDiscoveryTests.ServerInfo_AdvertisesGroupDocsRedactionMcp
Passed  ToolDiscoveryTests.ListTools_ExposesAllFiveRedactionTools
Passed  ToolDiscoveryTests.AllTools_HaveNonEmptyDescriptionAndInputSchema
Passed  RedactTextTests.RedactText_RealSample_WritesRedactedOutput (sample.docx)
Passed  RedactTextTests.RedactText_RealSample_WritesRedactedOutput (sample.pdf)
Passed  RedactTextTests.RedactText_AcceptsPasswordParameter
Passed  EraseMetadataTests.EraseMetadata_AllFields_WritesCleanedOutput (sample.docx)
Passed  EraseMetadataTests.EraseMetadata_AllFields_WritesCleanedOutput (sample.xlsx)
Passed  EraseMetadataTests.EraseMetadata_SpecificFields_Succeeds
Passed  RedactAnnotationsTests.RedactAnnotations_ReplaceMatching_WritesRedactedOutput
Passed  RedactAnnotationsTests.RedactAnnotations_DeleteAll_WritesRedactedOutput
Passed  RedactImageAreaTests.RedactImageArea_Pdf_CompletesAndKeepsServerResponsive
Passed  RedactImageAreaTests.RedactImageArea_AcceptsHexColor
Passed  GetDocumentInfoTests.GetDocumentInfo_RealSample_ReturnsFileTypeAndPageCount (sample.pdf)
Passed  GetDocumentInfoTests.GetDocumentInfo_RealSample_ReturnsFileTypeAndPageCount (sample.docx)
Passed  GetDocumentInfoTests.GetDocumentInfo_Pdf_ReportsPageDimensions
Passed  ErrorHandlingTests.GetDocumentInfo_UnknownFile_ReturnsErrorOrAvailableFilesHint
Passed  ErrorHandlingTests.RedactText_CorruptedFile_DoesNotCrashServer
Passed  ErrorHandlingTests.GetDocumentInfo_PasswordParameter_IsAccepted

Total: 19, Passed: 19, Time: ~13s
```

The first test run is slower (~60s) because `dnx` downloads the package into
the NuGet cache.

## Add real-world fixtures

Sample documents (`sample.docx`, `sample.pdf`, `sample.xlsx`, `annotated.xlsx`)
live in [sample-docs/](../sample-docs/). The csproj's
`<None Include="..\..\sample-docs\**\*" CopyToOutputDirectory="PreserveNewest" />`
glob copies them to the test output, which `McpServerFixture` seeds into the
server's storage path.

To add a custom fixture:

1. Drop the file into `sample-docs/`.
2. Add a test referencing it by filename:

```csharp
var response = await _fixture.Client.CallToolAsync(
    catalog.RedactText.Name,
    new Dictionary<string, object?>
    {
        ["file"] = new Dictionary<string, object?> { ["filePath"] = "contract.docx" },
        ["pattern"] = @"\d{3}-\d{2}-\d{4}",
    });
```

3. Ensure the file is license-clean (self-authored or CC0 / Apache-2.0) before
   committing.

## Use in CI

The workflow at [.github/workflows/integration.yml](../.github/workflows/integration.yml)
runs on four triggers:

- **`push` + `pull_request`** — validates repo changes.
- **Nightly cron** (`0 6 * * *` UTC) — catches regressions in nuget.org, `dnx`,
  or the .NET runtime.
- **`workflow_dispatch`** with a `package_version` input — smoke-test any
  published version manually.
- **`repository_dispatch`** (`nuget-published` event) — fires from the main
  repo's publish pipeline after `dotnet nuget push`. Payload:
  `{ "package_version": "x.y.z" }`.

Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.

### Wire the release-smoke hook in the server repo

Add this step to the server repo's publish workflow, right after the push step:

```yaml
- name: Dispatch integration tests
  env:
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  run: |
    gh api \
      repos/groupdocs-redaction/GroupDocs.Redaction.Mcp.Tests/dispatches \
      -f event_type=nuget-published \
      -f 'client_payload[package_version]=${{ steps.version.outputs.version }}'
```

The `GITHUB_TOKEN` scope is enough if both repos are in the same org.
Otherwise use a fine-grained PAT with `Contents: write` on the test repo.

### License secret in CI

Store a base64-encoded `.lic` file as the repo secret `GROUPDOCS_LICENSE`.
The workflow decodes it into `$RUNNER_TEMP` and exports `GROUPDOCS_LICENSE_PATH`
— output will then be watermark-free.

```bash
# Locally: base64-encode and set the secret
base64 -w0 GroupDocs.Total.lic | gh secret set GROUPDOCS_LICENSE \
  --repo groupdocs-redaction/GroupDocs.Redaction.Mcp.Tests
```

## Debugging failures

### Inspect server stderr

`McpServerFixture` doesn't currently capture the child process's stderr — if a
test fails with a cryptic error message, reproduce the call manually:

```bash
mkdir -p /tmp/gd && cp sample.pdf /tmp/gd/
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"p","version":"1"}}}'
  echo '{"jsonrpc":"2.0","method":"notifications/initialized"}'
  echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"redact_text","arguments":{"file":{"filePath":"sample.pdf"},"pattern":"\\d{3}-\\d{2}-\\d{4}"}}}'
  sleep 5
) | GROUPDOCS_MCP_STORAGE_PATH=/tmp/gd dnx GroupDocs.Redaction.Mcp@26.7.0 --yes \
    > stdout.log 2> stderr.log
tail -50 stderr.log
```

The server logs full exception stacks to stderr.

### Verbose test output

```bash
dotnet test -c Release --logger "console;verbosity=detailed"
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| `dnx: command not found` during test | Ensure .NET 10 SDK is installed. On Windows, `CommandResolver` looks for `dnx.cmd`; check it exists at `C:\Program Files\dotnet\dnx.cmd`. |
| Output file not created | Check the server's stderr log. The most common cause is a bad `filePath` or a file not present in the storage path. |
| First run takes minutes | NuGet download. Subsequent runs hit the cache. |
| Cross-OS flakes in CI | Different line endings in sample-docs fixtures. Commit with `text=auto` or binary mode in `.gitattributes`. |

## Next steps

- [03 — MCP registry](03-verify-mcp-registry.md) — cross-check registry state
- [01 — NuGet install](01-install-from-nuget.md) — manual smoke the same way users do
