#!/usr/bin/env bash
# Hook E: SessionEnd — cleanup
# Removes the temporary reads tracking file.

INPUT=$(cat)
SESSION_ID=$(echo "$INPUT" | jq -r '.session_id' 2>/dev/null)
rm -f "/tmp/claude-reads-${SESSION_ID}"
exit 0
