#!/usr/bin/env python3
import re
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
AGENTS_DIR = REPO_ROOT / ".cursor" / "agents"
MANIFEST_GLOB = "manifest-*.md"
PATH_PATTERN = re.compile(r"`((?:Assets|\.cursor)/[^`]+)`")


def collect_manifest_paths(manifest_path: Path):
    text = manifest_path.read_text(encoding="utf-8")
    for raw in PATH_PATTERN.findall(text):
        yield raw


def main():
    missing = []
    manifests = sorted(AGENTS_DIR.glob(MANIFEST_GLOB))
    if not manifests:
        print("No manifest files found.")
        return 1

    for manifest in manifests:
        for rel in collect_manifest_paths(manifest):
            target = REPO_ROOT / rel
            if not target.exists():
                missing.append((manifest.relative_to(REPO_ROOT), rel))

    if missing:
        print("Missing paths in manifests:")
        for manifest, rel in missing:
            print(f"- {manifest}: `{rel}`")
        return 1

    print(f"OK: validated {len(manifests)} manifest files.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
