#!/usr/bin/env bash
# Hook B: PreToolUse on Write/Edit — directory-based shard gate
# Uses DIRECTORY to determine required shards, not file extension.
# Future-proof — any file type in a source directory is gated.

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')
SESSION_ID=$(echo "$INPUT" | jq -r '.session_id')
READS_FILE="/tmp/claude-reads-${SESSION_ID}"

# Determine required shards based on directory
REQUIRED=()

# Team workflow process doc — required before ANY source file write
if echo "$FILE_PATH" | grep -qE '(api/src|api/tests|web/src)/'; then
  REQUIRED+=("team-workflow.md")
  REQUIRED+=("architecture.md")
fi

# Backend source or tests
if echo "$FILE_PATH" | grep -qE 'api/(src|tests)/'; then
  REQUIRED+=("patterns-backend.md")
  REQUIRED+=("api-patterns.md")
fi

# Backend tests specifically
if echo "$FILE_PATH" | grep -qE 'api/tests/'; then
  REQUIRED+=("testing-standards.md")
fi

# Frontend source
if echo "$FILE_PATH" | grep -qE 'web/src/'; then
  REQUIRED+=("patterns-frontend.md")
  REQUIRED+=("frontend-architecture.md")
fi

# Frontend tests (test/spec in path or filename)
if echo "$FILE_PATH" | grep -qE 'web/src/.*(test|spec|__tests__)'; then
  REQUIRED+=("testing-standards.md")
fi

# No requirements for non-source files
if [ ${#REQUIRED[@]} -eq 0 ]; then
  exit 0
fi

# Check read log
if [ ! -f "$READS_FILE" ]; then
  MISSING="${REQUIRED[*]}"
  echo "BLOCKED: Read these docs before writing source code: $MISSING" >&2
  echo "Start with: _bmad-output/planning-artifacts/architecture.md (routing table)" >&2
  echo "Process: .claude/process/team-workflow.md" >&2
  exit 2
fi

MISSING=()
for SHARD in "${REQUIRED[@]}"; do
  if ! grep -q "$SHARD" "$READS_FILE" 2>/dev/null; then
    MISSING+=("$SHARD")
  fi
done

if [ ${#MISSING[@]} -gt 0 ]; then
  echo "BLOCKED: Read these docs before writing to this directory: ${MISSING[*]}" >&2
  exit 2
fi

exit 0
