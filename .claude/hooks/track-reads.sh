#!/usr/bin/env bash
# Hook A: PostToolUse on Read — track reads
# Appends every Read file path to /tmp/claude-reads-{session_id}.
# Pure logging, no blocking.

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty' 2>/dev/null)
SESSION_ID=$(echo "$INPUT" | jq -r '.session_id' 2>/dev/null)
READS_FILE="/tmp/claude-reads-${SESSION_ID}"
echo "$FILE_PATH" >> "$READS_FILE"
exit 0
