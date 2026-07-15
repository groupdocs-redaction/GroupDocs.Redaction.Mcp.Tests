# Use with Cursor

Connect the MCP server to [Cursor](https://cursor.com) so you can ask its Agent
to redact text, erase metadata, redact annotations, cover image areas, or inspect
documents.

## Prerequisites

- Cursor installed and updated (MCP support is in **Settings → Tools & MCP**).
- One of:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the `dnx` route — recommended), or
  - [Docker](https://www.docker.com/products/docker-desktop) (for the container route).
- On Linux/macOS with the `dnx` or global-tool route, install `libgdiplus` first —
  `redact_image_area` needs it (see Troubleshooting). Docker already bundles it.

## Config file location

Cursor uses the **`mcpServers`** key (like Claude Desktop) — **not** `servers`
as in VS Code. Two scopes:

| Scope | Path |
|---|---|
| Global (all projects) | `~/.cursor/mcp.json` (macOS/Linux) · `%USERPROFILE%\.cursor\mcp.json` (Windows) |
| Project-only | `.cursor/mcp.json` in the workspace root |

Create the file if it doesn't exist.

## Option A — dnx (recommended)

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "command": "dnx",
      "args": ["GroupDocs.Redaction.Mcp@26.7.0", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

- Replace the storage path with an **absolute path** to the folder Cursor should
  operate on. On Windows use `"C:\\Users\\you\\Documents"` (double-escaped) or
  forward slashes.
- Omit `@26.7.0` to always pull the latest stable.
- Add `"GROUPDOCS_LICENSE_PATH": "…/GroupDocs.Total.lic"` to `env` to remove the
  evaluation watermark from redacted output. All five tools still run without a
  license — only the output is watermarked (and evaluation mode limits the process
  to one document at a time).

Copy-paste starter: [examples/cursor-mcp.json](../examples/cursor-mcp.json).

## Option B — Windows: full path to `dotnet.exe` (SSL / timeout workaround)

On Windows, Cursor launching `dnx` can fail with an **SSL / ~30 s timeout** on
the first package probe. Bypass `dnx` by running the already-cached tool DLL
directly with `dotnet.exe`:

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "command": "C:\\Program Files\\dotnet\\dotnet.exe",
      "args": [
        "C:\\Users\\you\\.nuget\\packages\\groupdocs.redaction.mcp\\26.7.0\\tools\\net10.0\\any\\GroupDocs.Redaction.Mcp.dll"
      ],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "C:\\Users\\you\\Documents"
      }
    }
  }
}
```

Populate the cache first by running `dnx GroupDocs.Redaction.Mcp@26.7.0 --yes` once
in a terminal, then point `args[0]` at the resulting
`…\.nuget\packages\groupdocs.redaction.mcp\<version>\tools\net10.0\any\GroupDocs.Redaction.Mcp.dll`.

## Option C — Docker

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "/Users/you/Documents:/data",
        "ghcr.io/groupdocs-redaction/redaction-net-mcp:26.7.0"
      ]
    }
  }
}
```

## Reload and verify

1. Save `mcp.json`.
2. **Settings → Tools & MCP** → find `groupdocs-redaction` → toggle it on (or hit
   the reload icon). A green dot means it connected.
3. Expand it — you should see `redact_text`, `erase_metadata`, `redact_annotations`,
   `redact_image_area`, and `get_document_info`.

## Example prompts (Agent mode)

```
Redact every SSN (\d{3}-\d{2}-\d{4}) in payroll.pdf.

Erase the author and company metadata from report.docx before I share it.

Delete all comments and annotations from review.docx.

Cover the signature at (100,200) size 300×80 on page 1 of agreement.pdf with a black box.

How many pages does confidential.pdf have, and what are their dimensions?
```

The Agent will call `redact_text` / `erase_metadata` / `redact_annotations` /
`redact_image_area` / `get_document_info` and compose its answer from the results.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Server greyed out / won't start on Windows | `dnx` SSL/timeout — use **Option B** (full `dotnet.exe` path + cached DLL). |
| Server not listed | JSON typo — Cursor silently drops unparseable entries. Validate with `jq . mcp.json`. Confirm the key is `mcpServers`, not `servers`. |
| Redacted output has an evaluation watermark | Expected in evaluation mode. Add `GROUPDOCS_LICENSE_PATH` to remove it. |
| Only the first redaction works, later calls fail | Evaluation mode allows one open document per process. Add a license, or restart the server between documents. |
| `DllNotFoundException: libgdiplus` (macOS/Linux) on `redact_image_area` | Install native deps — `brew install mono-libgdiplus` (macOS) / `apt-get install libgdiplus libfontconfig1` (Linux), or use the Docker option. |

## Next steps

- [04 — Use with Claude Desktop](04-use-with-claude-desktop.md)
- [05 — Use with VS Code / Copilot](05-use-with-vscode-copilot.md)
