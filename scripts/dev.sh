#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

cleanup() {
  echo "Shutting down..."
  kill $API_PID $WEB_PID 2>/dev/null || true
  wait $API_PID $WEB_PID 2>/dev/null || true
}

trap cleanup EXIT INT TERM

echo "==> Starting API (dotnet run)..."
cd "$REPO_ROOT/api"
dotnet run --project src/Web/Web.csproj &
API_PID=$!

echo "==> Starting frontend (vite dev)..."
cd "$REPO_ROOT/web"
npm run dev &
WEB_PID=$!

echo ""
echo "API:      http://localhost:5000"
echo "Frontend: http://localhost:5173"
echo "Press Ctrl+C to stop both servers."
echo ""

wait
