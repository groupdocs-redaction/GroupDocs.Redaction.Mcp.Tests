# Use with Claude Desktop

Connect the MCP server to Claude Desktop (macOS / Windows) so you can ask
Claude to redact text, erase metadata, redact annotations, or cover image areas
in your documents.

## Prerequisites

- [Claude Desktop](https://claude.ai/download) installed and logged in.
- One of:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the `dnx` route — recommended), or
  - [Docker](https://www.docker.com/products/docker-desktop) (for the container route).

## Config file location

| OS | Path |
|---|---|
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |

Create the file if it doesn't exist.

## Option A — dnx (recommended)

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Redaction.Mcp@26.5.1", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

- Replace `/Users/you/Documents` with an **absolute path** to the folder
  containing documents you want Claude to operate on.
- On Windows use `"C:\\Users\\you\\Documents"` (double-escaped backslashes) or
  forward slashes: `"C:/Users/you/Documents"`.

Full example: [examples/claude-desktop.json](../examples/claude-desktop.json).

### If Claude can't find `dnx`

Claude Desktop launches child processes with a minimal PATH — `dnx` may not be
found on macOS even though it works in your shell. Use the absolute path:

```json
"command": "/usr/local/share/dotnet/dnx"
```

On Windows:

```json
"command": "C:\\Program Files\\dotnet\\dnx.cmd"
```

Find the correct path with:

```bash
which dnx            # macOS / Linux
where dnx.cmd        # Windows (from cmd)
```

## Option B — Docker

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "/Users/you/Documents:/data",
        "ghcr.io/groupdocs-redaction/redaction-net-mcp:26.5.1"
      ]
    }
  }
}
```

This works even if you don't have the .NET SDK installed. The first invocation
pulls the image; subsequent launches are fast.

## Option C — Global dotnet tool

```json
{
  "mcpServers": {
    "groupdocs-redaction": {
      "type": "stdio",
      "command": "groupdocs-redaction-mcp",
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

Requires you've already run `dotnet tool install -g GroupDocs.Redaction.Mcp`
(see [01 — NuGet install](01-install-from-nuget.md)).

## Restart Claude Desktop

After editing the config, fully quit and reopen Claude Desktop. On macOS,
`Cmd+Q` — closing the window isn't enough.

## Verify the connection

1. Open a new conversation.
2. Click the **🔨 tools** icon in the composer — you should see
   `redact_text`, `erase_metadata`, `redact_annotations`, `redact_image_area`,
   and `get_document_info` listed under `groupdocs-redaction`.
3. If the icon shows an error badge, hover for the details. The most common
   issue is a bad `command` path or invalid `GROUPDOCS_MCP_STORAGE_PATH`.

## Example prompts

```
Redact all social security numbers (pattern \d{3}-\d{2}-\d{4}) from contract.pdf.

Erase all metadata from report.docx before sharing it externally.

Delete all comments from annotated.xlsx.

Cover the signature area (x=50, y=700, width=200, height=50) on page 1 of agreement.pdf.

What file type and page count does proposal.pdf have?
```

Claude will call the appropriate tool (`redact_text`, `erase_metadata`,
`redact_annotations`, `redact_image_area`, or `get_document_info`) and
compose its answer from the tool results.

## License note

All tools work without a license — output files are produced in evaluation mode
with a watermark overlay. To produce clean output, add the license path to your
config:

```json
"env": {
  "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents",
  "GROUPDOCS_LICENSE_PATH": "/Users/you/.secrets/GroupDocs.Total.lic"
}
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| Server not listed in tools icon | Config JSON has a typo — Claude silently drops unparseable entries. Run it through `jq . claude_desktop_config.json`. |
| Server listed but greyed out | Claude couldn't launch the process. Check `~/Library/Logs/Claude/mcp*.log` on macOS or `%APPDATA%\Claude\logs\mcp*.log` on Windows for stderr from the server. |
| "No license configured" warnings | Expected in evaluation mode. All tools still work — output will be watermarked. |

## Next steps

- [05 — Use with VS Code / Copilot](05-use-with-vscode-copilot.md)
- [03 — MCP registry](03-verify-mcp-registry.md) — confirm the snippet matches what's on nuget.org
