#!/usr/bin/env bash
# Hook E: SessionEnd — cleanup
# Removes the read-tracking temp file for this session.

INPUT=$(cat)
SESSION_ID=$(echo "$INPUT" | jq -r '.session_id')
rm -f "/tmp/claude-reads-${SESSION_ID}"
exit 0
