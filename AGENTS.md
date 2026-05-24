# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

**Integration tests** for the [`GroupDocs.Redaction.Mcp`](https://www.nuget.org/packages/GroupDocs.Redaction.Mcp) NuGet package — an MCP server that exposes GroupDocs.Redaction for .NET as AI-callable tools.

This repo is **not** the server itself. The server lives at [groupdocs-redaction/GroupDocs.Redaction.Mcp](https://github.com/groupdocs-redaction/GroupDocs.Redaction.Mcp). This repo:

1. Consumes only the **published** NuGet artifact (no project references).
2. Launches the server via `dnx`, connects as an MCP stdio client, and exercises every advertised tool.
3. Doubles as a copy-pasteable set of example configs and how-to guides for all deployment channels (NuGet, Docker, MCP registry, Claude Desktop, VS Code).

## Folder layout

```
src/GroupDocs.Redaction.Mcp.Tests/
  Fixtures/
    McpServerFixture.cs          ← launches dnx child process, wires stdio MCP client
    SampleDocuments.cs           ← writes sample-docs/ files to the server storage path at runtime
    ToolCatalog.cs               ← keyword-based tool name resolution (redact/erase/annotate/image/info)
    ToolResponse.cs              ← CallToolResult text/JSON extraction
    CommandResolver.cs           ← cross-platform dnx.cmd resolution on Windows
    PackageVersion.cs            ← pulls version from env / assembly metadata / default
  McpServerTestBase.cs           ← per-test base: spawns a FRESH server per test (1-doc trial cap)
  AssemblyInfo.cs                ← [assembly: CollectionBehavior(DisableTestParallelization = true)]
  ToolDiscoveryTests.cs          ← handshake, tools/list, schema validation
  RedactTextTests.cs             ← DOCX + PDF happy-path, output file assertions
  EraseMetadataTests.cs          ← metadata field erasure, output file round-trip
  RedactAnnotationsTests.cs      ← annotation redaction / delete-all on annotated.xlsx
  RedactImageAreaTests.cs        ← image area covering on PDF pages
  GetDocumentInfoTests.cs        ← read-only info: fileName, fileType, pageCount, size
  ErrorHandlingTests.cs          ← unknown file, corrupted bytes, password parameter
  GroupDocs.Redaction.Mcp.Tests.csproj
.github/workflows/integration.yml  ← matrix × 3 OS, nightly cron, release-smoke dispatch
changelog/                         ← one MD file per change (NNN-slug.md)
how-to/                            ← user-facing guides for every deployment channel
examples/                          ← claude-desktop.json, vscode-mcp.json, docker-compose.yml
sample-docs/                       ← sample.docx, sample.pdf, sample.xlsx, annotated.xlsx
Directory.Build.props              ← McpPackageVersion property (overridable)
global.json                        ← pinned to .NET 10.0.100
```

## What gets tested

| Area | Covered by |
|---|---|
| Package installs and starts via `dnx` | `McpServerFixture` |
| MCP handshake, server info, version | `ToolDiscoveryTests` |
| `redact_text` — DOCX + PDF, output file created | `RedactTextTests` |
| `erase_metadata` — output file + field removal | `EraseMetadataTests` |
| `redact_annotations` — comment redaction / delete-all | `RedactAnnotationsTests` |
| `redact_image_area` — rectangular region covered | `RedactImageAreaTests` |
| `get_document_info` — JSON file info (read-only) | `GetDocumentInfoTests` |
| Unknown / corrupted files, password parameter | `ErrorHandlingTests` |

## Commands you can run

```bash
# Restore + build
dotnet restore
dotnet build -c Release

# Run all tests against the default package version (26.5.1)
dotnet test -c Release

# Run against a specific published version
dotnet test -c Release -p:McpPackageVersion=26.5.1
# or
MCP_PACKAGE_VERSION=26.5.1 dotnet test -c Release

# Unlock licensed-mode tests (removes watermark from output — does NOT change pass/fail)
GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic dotnet test -c Release

# Run just the discovery suite (fastest — no tool invocations)
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"
```

## Key design decisions

1. **Keyword-based tool resolution.** `ToolCatalog.Resolve("redact_text")` picks the tool whose name contains "redact_text" (case-insensitive). The MCP C# SDK converts `[McpServerTool]` method names to `snake_case` — so the actual wire names are `redact_text`, `erase_metadata`, `redact_annotations`, `redact_image_area`, and `get_document_info`. Tests stay robust if that convention changes.

2. **Real sample documents.** `sample-docs/` contains `sample.docx`, `sample.pdf`, `sample.xlsx`, and `annotated.xlsx`. The csproj auto-copies everything in `sample-docs/` to the test output. `McpServerFixture` seeds the server's storage path from that output folder.

3. **Evaluation-mode behavior + per-test servers.** `GroupDocs.Redaction.Save()` **succeeds** in evaluation mode — it writes a watermarked copy — so tests assert output-file creation and a `GROUPDOCS_LICENSE_PATH` only removes the watermark. **However, evaluation mode caps document opens at ONE per process** ("Trial mode allows only 1 document to open"). So each test runs against its **own fresh `dnx` server** (`McpServerTestBase` news up `McpServerFixture` per test method) and opens at most one document; a shared server would throw `TrialLimitationsException` on the second tool call. `[assembly: CollectionBehavior(DisableTestParallelization = true)]` keeps launches serial so the first warms the package cache for the rest.

4. **Output naming convention.** Every redaction tool writes `{name}_redacted{ext}` — e.g. `sample_redacted.pdf`. Tests resolve the expected output path from the input filename.

5. **No project references to the server.** The csproj only references `ModelContextProtocol` 1.1.0. If the server source breaks in the sibling repo, these tests still pass — they validate the shipped NuGet artifact.

## House rules

1. **Changelog entries required** — any PR that changes behaviour adds `changelog/NNN-slug.md` (schema in `changelog/README.md`).
2. **How-to guides track deployment reality** — if the main repo publishes a new channel (e.g. new Docker registry), add a guide under `how-to/` *and* update `README.md`.
3. **Version bumps flow through `Directory.Build.props`** — `<McpPackageVersion>` is the single source of truth for "what version are we testing." CI overrides it via env var / workflow input.
4. **Tests must not require the main repo's source.** If a test needs a server-side change, file an issue there — don't work around it here.
5. **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.

## Release smoke hook

The main repo's `publish_prod.yml` should fire a `repository_dispatch` with `event_type=nuget-published` after `dotnet nuget push` succeeds. The workflow in `.github/workflows/integration.yml` consumes `client_payload.package_version` and runs the matrix against the just-published version. This closes the loop: publish → smoke-test live nuget.org → fail loud if broken.

## What NOT to change

- Do not add a `ProjectReference` to the main repo's `GroupDocs.Redaction.Mcp.csproj`. This repo exists to test the shipped NuGet, not the source.
- Do not hardcode tool names as string literals (`"redact_text"`). Use `ToolCatalog.RedactText.Name` etc.
- Do not commit real license files or binary fixtures with unclear provenance. License goes through the `GROUPDOCS_LICENSE` CI secret; fixtures in `sample-docs/` must be self-authored or CC0/Apache-2.0.
