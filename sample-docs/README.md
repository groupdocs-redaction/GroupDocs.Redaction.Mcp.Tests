# Sample documents

Real fixture documents used by the integration tests. At test startup
`McpServerFixture` copies everything in this folder into the MCP server's
temporary storage path (see `Fixtures/SampleDocuments.cs`). The csproj copies
this folder to the test output via
`<None Include="..\..\sample-docs\**\*" CopyToOutputDirectory="PreserveNewest" />`.

GroupDocs.Redaction permits `Save()` in evaluation mode (output is watermarked,
not blocked), so these fixtures let every redaction tool be exercised
end-to-end without a license.

## Provenance

All four files come from the upstream
[GroupDocs.Redaction-for-.NET](https://github.com/groupdocs-redaction/GroupDocs.Redaction-for-.NET)
examples repo, under
`Examples/GroupDocs.Redaction.Examples.CSharp/Resources/SampleFiles/`:

| File | Upstream source | Exercised by |
|---|---|---|
| `sample.docx` | `Doc/sample.docx` | `redact_text`, `erase_metadata`, `get_document_info` |
| `sample.pdf` | `Pdf/sample.pdf` | `redact_text`, `redact_image_area`, `get_document_info` |
| `sample.xlsx` | `Xls/sample.xlsx` | `erase_metadata`, `get_document_info` |
| `annotated.xlsx` | `Xls/sample1.xlsx` (workbook with cell comments) | `redact_annotations` |

## Refreshing

```bash
SRC="<path>/GroupDocs.Redaction-for-.NET/Examples/GroupDocs.Redaction.Examples.CSharp/Resources/SampleFiles"
cp "$SRC/Doc/sample.docx"  sample.docx
cp "$SRC/Pdf/sample.pdf"   sample.pdf
cp "$SRC/Xls/sample.xlsx"  sample.xlsx
cp "$SRC/Xls/sample1.xlsx" annotated.xlsx
```

Keep these self-authored or permissively licensed; do not commit customer
documents or license files here.
