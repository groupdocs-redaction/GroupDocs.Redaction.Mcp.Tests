---
id: 001
date: 2026-05-22
package-under-test: 26.5.0
type: feature
---

# Initial integration test suite for GroupDocs.Redaction.Mcp

## What changed

- xUnit test project targeting `net10.0`, referencing only the published
  `ModelContextProtocol` 1.1.0 NuGet — no project reference to the server source.
- `McpServerFixture` launches the published `GroupDocs.Redaction.Mcp@26.5.0`
  package via `dnx` as a child process, wires an MCP stdio client, and seeds a
  temporary storage folder with sample documents from `sample-docs/`.
- A **fresh server process per test** (`McpServerTestBase`) plus
  `[assembly: CollectionBehavior(DisableTestParallelization = true)]`:
  GroupDocs.Redaction's evaluation mode allows only **one document open per
  process** ("Trial mode allows only 1 document to open"), so a shared server
  would throw `TrialLimitationsException` on the second tool call. Each test
  opens at most one document; serial execution warms the package cache once.
- `SampleDocuments` / `McpServerFixture` copies `sample.docx`, `sample.pdf`,
  `sample.xlsx`, and `annotated.xlsx` from `sample-docs/` into the server's
  writable storage path at test startup.
- Seven test classes, 16 test methods (19 cases counting theory data):
  - `ToolDiscoveryTests` — server info, `tools/list`, input schema validation
    for all five tools.
  - `RedactTextTests` — DOCX + PDF text redaction by regex, output-file
    creation assertion, password-parameter acceptance.
  - `EraseMetadataTests` — metadata field erasure on DOCX + XLSX (all fields and
    a specific field list), output-file creation assertion.
  - `RedactAnnotationsTests` — annotation redaction (replace) and delete-all on
    `annotated.xlsx`.
  - `RedactImageAreaTests` — rectangular region covering on PDF pages (exercises
    the System.Drawing / libgdiplus native path), named + hex colors.
  - `GetDocumentInfoTests` — read-only info (fileName, fileType, pageCount,
    size, per-page dimensions) for PDF + DOCX.
  - `ErrorHandlingTests` — unknown file, corrupted bytes, password parameter.
- GitHub Actions workflow `.github/workflows/integration.yml`:
  - Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.
  - Triggers: push, PR, nightly cron, `workflow_dispatch` (with `package_version`
    input), `repository_dispatch` (`nuget-published` event for release smoke).
  - Optional `GROUPDOCS_LICENSE` repo secret auto-decoded into `$RUNNER_TEMP` and
    exported as `GROUPDOCS_LICENSE_PATH` to produce watermark-free output.
- `examples/` — ready-to-use `claude-desktop.json`, `vscode-mcp.json`,
  `docker-compose.yml` copy-paste configs.
- `AGENTS.md` + `llms.txt` for AI coding agent orientation.
- `how-to/` guides covering every deployment channel (NuGet via dnx / dotnet
  tool, Docker, MCP registry, Claude Desktop, VS Code / GitHub Copilot, plus
  running this test suite).

## Why

Closes the release-validation gap: the main repo's unit tests mock internal
interfaces and validate tool logic, but nothing previously exercised the
**shipped** NuGet end-to-end. Every release now has a cross-platform smoke
check against live nuget.org before users hit it.

Unlike some GroupDocs products, GroupDocs.Redaction.Save() succeeds in
evaluation mode — output is simply watermarked. All tests therefore pass
without a license, making this suite immediately usable in any CI environment.

## Migration / impact

First release of this repository — no migration. To wire the release-smoke
trigger, add a `gh api repos/.../dispatches -f event_type=nuget-published -f
'client_payload[package_version]=…'` step to the main repo's publish workflow
after `dotnet nuget push` succeeds. See `how-to/06-run-integration-tests.md`.
