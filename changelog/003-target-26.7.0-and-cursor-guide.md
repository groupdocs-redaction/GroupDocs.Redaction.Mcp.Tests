---
id: 003
date: 2026-07-15
package-under-test: 26.7.0
type: change
---

# Target package 26.7.0 + add Cursor how-to and example config

## What changed
- **Bumped the package under test** 26.5.1 → **26.7.0** everywhere:
  `Directory.Build.props` `<McpPackageVersion>`, the `integration.yml` default
  and `||` fallback, and all pinned `@26.7.0` / `:26.7.0` examples across
  `how-to/*`, `examples/*`, `docker-scripts/*`, `README.md`, and `AGENTS.md`.
- **Added `how-to/07-use-with-cursor.md`** — Cursor uses the `mcpServers` key
  (like Claude Desktop, not VS Code's `servers`). Documents the dnx route, the
  Windows `dotnet.exe` + cached-DLL SSL/timeout workaround, the Docker route,
  and Redaction's evaluation-mode notes (watermarked output, one-document cap,
  libgdiplus for `redact_image_area`).
- **Added `examples/cursor-mcp.json`** — copy-paste starter for the Cursor config.
- Linked the new guide from `how-to/README.md`.

## Why
The server was released as 26.7.0 (engine upgraded to GroupDocs.Redaction 26.6.0);
the integration suite must exercise that published version. Cursor is a common MCP
client and warranted its own guide alongside Claude Desktop and VS Code.

## Migration / impact
- Tool surface is unchanged — still five tools; `ToolDiscoveryTests` still asserts
  a count of 5.
- Integration tests only pass once 26.7.0 is live on nuget.org (they resolve the
  package via `dnx` at runtime).
