#!/usr/bin/env bash
set -euo pipefail

# LocalProxy build script (macOS / Linux)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

CONFIGURATION="${1:-Release}"
RID="${2:-}"

echo "=== LocalProxy Build ==="
echo "Configuration: $CONFIGURATION"

# Restore
echo ""
echo "[1/3] Restoring packages..."
dotnet restore

# Build
echo ""
echo "[2/3] Building..."
dotnet build -c "$CONFIGURATION" --no-restore

# Publish CLI (AOT)
echo ""
echo "[3/3] Publishing CLI (AOT)..."
if [ -n "$RID" ]; then
    dotnet publish src/LocalProxy.Cli/LocalProxy.Cli.csproj \
        -c "$CONFIGURATION" \
        -r "$RID" \
        --self-contained \
        -o artifacts/cli/"$RID"
    echo "CLI published to artifacts/cli/$RID/"
else
    dotnet publish src/LocalProxy.Cli/LocalProxy.Cli.csproj \
        -c "$CONFIGURATION" \
        -o artifacts/cli/
    echo "CLI published to artifacts/cli/"
fi

echo ""
echo "=== Build complete ==="
