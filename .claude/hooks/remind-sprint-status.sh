#!/usr/bin/env bash
# Hook D: PostToolUse on Bash — sprint-status reminder
# Non-blocking reminder after git commit if sprint-status wasn't updated.

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if ! echo "$COMMAND" | grep -q "git commit"; then
  exit 0
fi

# Check if sprint-status.yaml was in the commit
if git diff --name-only HEAD~1 HEAD 2>/dev/null | grep -q "sprint-status"; then
  exit 0
fi

cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "PostToolUse",
    "additionalContext": "REMINDER: If this commit completes a story, update sprint-status.yaml (story status -> done)."
  }
}
EOF
exit 0
