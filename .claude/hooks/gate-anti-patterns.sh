#!/usr/bin/env bash
# Hook C: PreToolUse on Write/Edit — anti-pattern content gate
# Checks content against BOTH anti-pattern files (permanent + pending).
# Uses file glob matching for targeted checks.

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty' 2>/dev/null)

# Get content to check (Write uses .content, Edit uses .new_string)
CONTENT=$(echo "$INPUT" | jq -r '.tool_input.content // .tool_input.new_string // empty' 2>/dev/null)

if [ -z "$CONTENT" ]; then
  exit 0
fi

# Use CLAUDE_PROJECT_DIR for project-relative paths (hooks may run from any cwd)
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-.}"

# Check both anti-pattern files
for AP_FILE in "$PROJECT_DIR/.claude/hooks/anti-patterns.txt" "$PROJECT_DIR/.claude/hooks/anti-patterns-pending.txt"; do
  [ -f "$AP_FILE" ] || continue

  while IFS='|' read -r PATTERN GLOB MESSAGE; do
    # Skip comments and empty lines
    [[ "$PATTERN" =~ ^#.*$ || -z "$PATTERN" ]] && continue

    # Check if file matches glob pattern
    case "$FILE_PATH" in
      $GLOB) ;;  # Matches
      *) continue ;;  # Doesn't match, skip
    esac

    # Check content for anti-pattern
    if echo "$CONTENT" | grep -qE "$PATTERN"; then
      echo "BLOCKED: Anti-pattern detected — $MESSAGE" >&2
      exit 2
    fi
  done < "$AP_FILE"
done

exit 0
