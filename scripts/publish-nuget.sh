#!/usr/bin/env bash
set -euo pipefail

# Usage: scripts/publish-nuget.sh <version> [api_key]
# - version: required, e.g., 1.2.3
# - api_key: optional; if absent, use $NUGET_API_KEY env var

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJ="$ROOT_DIR/src/MultiDb.Extensions/MultiDb.Extensions.csproj"
OUT_DIR="$ROOT_DIR/out"

VERSION="${1:-}"
API_KEY="${2:-${NUGET_API_KEY:-}}"

if [[ -z "$VERSION" ]]; then
  echo "Version is required. Example: scripts/publish-nuget.sh 1.0.0" >&2
  exit 1
fi

if [[ -z "${API_KEY}" ]]; then
  echo "NuGet API key is required via arg2 or NUGET_API_KEY env var." >&2
  exit 2
fi

mkdir -p "$OUT_DIR"

echo "Packing version $VERSION..."
dotnet pack "$PROJ" -c Release -o "$OUT_DIR" /p:PackageVersion="$VERSION"

echo "Pushing packages..."
dotnet nuget push "$OUT_DIR"/*.nupkg --source https://api.nuget.org/v3/index.json --api-key "$API_KEY" --skip-duplicate

echo "Done."
