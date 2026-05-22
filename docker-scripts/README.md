# Docker Scripts — Integration Test Automation

Complete bash script suite for running GroupDocs.Redaction.Mcp integration tests in both local and Docker environments. **Works on Windows (Git Bash/WSL), Linux, and macOS.**

## 📋 Overview

This folder contains production-ready bash scripts for testing the GroupDocs.Redaction.Mcp NuGet package across all scenarios:

- **ToolDiscovery** — MCP server handshake and tool listing
- **RedactText** — DOCX/PDF text redaction by regex pattern
- **EraseMetadata** — Document metadata field erasure
- **RedactAnnotations** — Annotation redaction and delete-all
- **RedactImageArea** — Rectangular region covering with solid color
- **GetDocumentInfo** — Read-only file info (fileName, fileType, pageCount)
- **ErrorHandling** — Unknown files, corrupted bytes, password parameters

## 📁 Files

Numeric prefixes indicate the recommended running sequence.

| File | Purpose |
|------|---------|
| `00_quick-start.sh` | **Cheatsheet** — Copy-paste reference. Not meant to be executed directly. |
| `01_verify-setup.sh` | **Preflight** — Check Docker, .NET SDK, project structure, connectivity. |
| `02_test-all-scenarios.sh` | **Main runner** — Runs the integration suite locally against the published NuGet (via `dnx`). |
| `03_test-docker-compose.sh` | **SDK-container runner** — Runs the suite inside a `dotnet/sdk` container; still uses `dnx` to spawn the MCP server. |
| `04_run-server-with-samples.sh` | **Interactive Docker MCP server** — Launches the published `redaction-net-mcp` container with `sample-docs/` mounted, for ad-hoc smoke testing. |
| `helpers.sh` | Library — shared logging, Docker, .NET utilities (sourced, not run). |
| `README.md` | This file. |

## 🚀 Quick Start

### Prerequisites

**Windows:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (includes Docker CLI)
- [Git Bash](https://git-scm.com/download/win) or [WSL2](https://learn.microsoft.com/en-us/windows/wsl/install)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for local testing)

**Linux:**
```bash
sudo apt-get install docker.io dotnet-sdk-10.0 git
```

**macOS:**
```bash
brew install docker dotnet git
# Also install Docker Desktop or Docker for Mac
```

### Basic Usage

```bash
# Run all tests against the LATEST nuget.org release (fastest — local execution, default)
./02_test-all-scenarios.sh

# Run tests in Docker containers (also defaults to latest)
./03_test-docker-compose.sh

# Try the published Docker MCP image with the repo's sample-docs mounted
./04_run-server-with-samples.sh

# Run specific test scenario
./02_test-all-scenarios.sh --filter RedactText

# Pin to a specific package version (reproducible / CI runs)
./02_test-all-scenarios.sh --version 26.5.0

# Use license for watermark-free output
./02_test-all-scenarios.sh --license /path/to/GroupDocs.Total.lic
```

## 📖 Detailed Usage

### `02_test-all-scenarios.sh` — Local Test Runner

**Best for:** Local development, quick validation, CI/CD pipelines

```bash
Usage:
  ./02_test-all-scenarios.sh [OPTIONS]

Options:
  --version VERSION       Test specific package version (default: latest)
                          Use "latest" or omit to track nuget.org's most recent
                          stable release. Pin (e.g. "26.5.0") for reproducible
                          / shared / CI runs.
  --filter PATTERN        Run only tests matching pattern
  --no-build              Skip local .NET build, use pre-built
  --license PATH          Path to GroupDocs license file
  --help                  Show help message

Examples:
  # All tests against latest stable (default)
  ./02_test-all-scenarios.sh

  # Only RedactText tests
  ./02_test-all-scenarios.sh --filter RedactText

  # Pin to 26.5.0 with custom license
  ./02_test-all-scenarios.sh --version 26.5.0 --license /path/to/lic

  # Skip rebuild (use cached binaries)
  ./02_test-all-scenarios.sh --no-build --filter ToolDiscovery
```

**What it does:**
1. ✅ Validates .NET SDK and project structure
2. ✅ Builds the test project in Release mode
3. ✅ Runs all integration tests with specified filters
4. ✅ Reports results in console + JSON format

**Output:**
```
Passed  ToolDiscoveryTests.ServerInfo_AdvertisesGroupDocsRedactionMcp
Passed  RedactTextTests.RedactText_RealSample_WritesRedactedOutput
Passed  ErrorHandlingTests.GetDocumentInfo_UnknownFile_ReturnsErrorOrAvailableFilesHint
...
✓ All integration test scenarios completed!
```

---

### `03_test-docker-compose.sh` — Containerized Test Runner

**Best for:** CI/CD, isolated environments, cross-platform reproducibility

```bash
Usage:
  ./03_test-docker-compose.sh [OPTIONS]

Options:
  --version VERSION       Test specific package version (default: latest)
                          Use "latest" or omit to track nuget.org's most recent
                          stable release. Pin (e.g. "26.5.0") for reproducible
                          / shared / CI runs.
  --filter PATTERN        Run only tests matching pattern
  --license PATH          Path to GroupDocs license file
  --keep                  Keep containers running (for debugging)
  --help                  Show help message

Examples:
  # Run all tests in Docker against latest stable (default)
  ./03_test-docker-compose.sh

  # Pin to a specific version
  ./03_test-docker-compose.sh --version 26.5.0

  # Keep containers for inspection
  ./03_test-docker-compose.sh --keep
```

**What it does:**
1. ✅ Generates isolated docker-compose.yml
2. ✅ Mounts sample-docs volume (read-only)
3. ✅ Runs tests in .NET SDK 10.0 container
4. ✅ Auto-cleans resources (unless `--keep` is set)

**Generated docker-compose file structure:**
```yaml
services:
  test-runner:
    image: mcr.microsoft.com/dotnet/sdk:10.0
    volumes:
      - workspace-volume:/workspace
      - sample-docs-volume:/sample-docs:ro
```

---

## 📂 Sample Documents

The `sample-docs/` folder contains test fixtures:

```
sample-docs/
  ├── sample.docx          (DOCX for text redaction, metadata erasure)
  ├── sample.pdf           (PDF for text redaction, image area covering)
  ├── sample.xlsx          (XLSX for metadata erasure)
  └── annotated.xlsx       (XLSX with cell comments for annotation redaction)
```

**To add your own:**
```bash
cp my-document.pdf ../sample-docs/
./02_test-all-scenarios.sh
```

---

## 🔐 License Files

### Using a License

```bash
# Unlock watermark-free output
export GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic
./02_test-all-scenarios.sh

# Or inline
./02_test-all-scenarios.sh --license /path/to/GroupDocs.Total.lic
```

### Evaluation Mode (Default)

Without a license, all redaction tools **still pass** — GroupDocs.Redaction.Save()
succeeds in evaluation mode and produces watermarked output. Tests assert
output-file creation, which always succeeds regardless of license state.

License file must be readable by the process running the tests.

---

## 🎯 Test Scenarios & Filters

Run individual test suites with `--filter`:

| Filter | Tests | Command |
|--------|-------|---------|
| `ToolDiscovery` | 3 cases | `./02_test-all-scenarios.sh --filter ToolDiscovery` |
| `RedactText` | 3 cases | `./02_test-all-scenarios.sh --filter RedactText` |
| `EraseMetadata` | 3 cases | `./02_test-all-scenarios.sh --filter EraseMetadata` |
| `RedactAnnotations` | 2 cases | `./02_test-all-scenarios.sh --filter RedactAnnotations` |
| `RedactImageArea` | 2 cases | `./02_test-all-scenarios.sh --filter RedactImageArea` |
| `GetDocumentInfo` | 3 cases | `./02_test-all-scenarios.sh --filter GetDocumentInfo` |
| `ErrorHandling` | 3 cases | `./02_test-all-scenarios.sh --filter ErrorHandling` |
| (default) | 19 cases (16 methods) | `./02_test-all-scenarios.sh` |

**Example: Quick validation (only discovery)**
```bash
./02_test-all-scenarios.sh --filter ToolDiscovery  # ~2 seconds
```

---

## 🐳 Docker Configuration

### Using Latest Package

```bash
./03_test-docker-compose.sh --version latest
```

### Building Docker Images Locally

```bash
# Build from server source (requires main repo)
docker build -t redaction-net-mcp:local ../../../GroupDocs.Redaction.Mcp/

# Run tests against local image
./03_test-docker-compose.sh
```

### Debugging Docker Tests

```bash
# Keep containers running
./03_test-docker-compose.sh --keep

# View logs
docker logs <container-id>

# Inspect running containers
docker ps

# Clean up manually
docker compose -f docker-compose.test.yml down -v
```

---

## 🔧 Advanced Usage

### Test Multiple Versions

```bash
for version in 26.4.0 26.5.0 latest; do
  echo "Testing version $version..."
  ./02_test-all-scenarios.sh --version $version || exit 1
done
```

### Parallel Testing (Different Filters)

```bash
# Run in separate terminals
./02_test-all-scenarios.sh --filter ToolDiscovery &
./02_test-all-scenarios.sh --filter RedactText &
./02_test-all-scenarios.sh --filter ErrorHandling &
wait
```

### CI/CD Integration

**GitHub Actions:**
```yaml
- name: Run integration tests
  run: |
    cd docker-scripts
    ./02_test-all-scenarios.sh --version ${{ matrix.version }}
  env:
    GROUPDOCS_LICENSE_PATH: ${{ secrets.GROUPDOCS_LICENSE }}
```

**Azure Pipelines:**
```yaml
- script: |
    cd docker-scripts
    chmod +x 02_test-all-scenarios.sh
    ./02_test-all-scenarios.sh
  displayName: 'Run Integration Tests'
```

---

## 📋 Helper Functions

The `helpers.sh` file provides reusable bash functions for scripting:

```bash
#!/bin/bash
source "$(dirname "$0")/helpers.sh"

# Logging
log_info "Starting..."
log_success "Completed!"
log_error "Failed"
log_warning "Be careful"

# Checks
check_command docker
check_dotnet_sdk 10
check_file_exists /path/to/file

# Docker utilities
docker_pull_image "ghcr.io/groupdocs-redaction/redaction-net-mcp:latest"
check_docker_daemon

# .NET utilities
dotnet_restore "/path/to/project"
dotnet_build "/path/to/project" "Release"
```

---

## 🐛 Troubleshooting

### "Docker daemon is not running"
```bash
# Windows/macOS: Start Docker Desktop
# Linux
sudo systemctl start docker
```

### "Permission denied" on .sh files (Windows Git Bash)
```bash
git update-index --chmod=+x 02_test-all-scenarios.sh
./02_test-all-scenarios.sh
```

### "dotnet command not found"
```bash
# Install .NET 10 SDK
# https://dotnet.microsoft.com/download/dotnet/10.0

# Or skip build
./02_test-all-scenarios.sh --no-build
```

### Tests timeout in Docker
```bash
# Increase Docker Desktop memory: Preferences → Resources → Memory (8GB+)
# Or use local testing instead
./02_test-all-scenarios.sh
```

### Output files not created
```bash
# Verify path is absolute and the file exists in sample-docs
ls -la ../sample-docs/

# Confirm the server's storage path is set correctly
export GROUPDOCS_MCP_STORAGE_PATH=$(pwd)/../sample-docs
./02_test-all-scenarios.sh
```

---

## 📊 Expected Output

### All Tests Pass
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

✓ All integration test scenarios completed!
```

### Sample-Docs Detected
```
✓ Found 4 sample documents
  /workspace/sample-docs/sample.docx (48 KB)
  /workspace/sample-docs/sample.pdf (72 KB)
  /workspace/sample-docs/sample.xlsx (12 KB)
  /workspace/sample-docs/annotated.xlsx (14 KB)
```

---

## 📝 Notes

- Scripts are **cross-platform** (Windows/Linux/macOS)
- All paths are **automatically normalized** for your OS
- Docker volumes are **read-only** for safety
- Tests can run **locally or containerized**
- License path is **optional** (evaluation mode default — all tests pass either way)
- Results are logged in **JSON format** for CI integration

---

## 📚 Related Files

- [GroupDocs.Redaction.Mcp NuGet](https://www.nuget.org/packages/GroupDocs.Redaction.Mcp)
- [Integration Tests Guide](../how-to/06-run-integration-tests.md)
- [Docker Setup Guide](../how-to/02-run-via-docker.md)
- [Test Project](../src/GroupDocs.Redaction.Mcp.Tests/GroupDocs.Redaction.Mcp.Tests.csproj)

---

## 💡 Tips & Best Practices

1. **For quick validation:** Use `--filter ToolDiscovery` (2-5 seconds)
2. **For CI/CD:** Use `02_test-all-scenarios.sh` (local is faster than Docker)
3. **For debugging:** Use `03_test-docker-compose.sh --keep` to inspect containers
4. **For multiple versions:** Use a loop with `--version` flag
5. **For sample docs:** Drop files in `sample-docs/`, they're auto-mounted
6. **For licenses:** Store in a secure location, pass via env var in CI

---

**Last Updated:** 2026-05-22  
**Minimum Requirements:** Bash 4.0+, Docker 20.10+, .NET 10 SDK
