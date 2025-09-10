#!/usr/bin/env bash
set -euo pipefail

# Usage: bash scripts/verify-backend.sh [HOST] [PORT]
# Example: bash scripts/verify-backend.sh 192.168.68.103 8080

HOST="${1:-localhost}"
PORT="${2:-8080}"

echo "==> Rebuilding and restarting backend API on port ${PORT}"
(
  cd backend
  docker compose up -d --build mytrader_api
)

BASE="http://${HOST}:${PORT}"
echo "==> Waiting for API to become healthy at ${BASE}"

deadline=$((SECONDS+60))
ok=false
while [ $SECONDS -lt $deadline ]; do
  code=$(curl -s -o /dev/null -w "%{http_code}" "${BASE}/health" || true)
  if [ "$code" = "200" ]; then
    ok=true
    break
  fi
  sleep 2
done

if [ "$ok" != true ]; then
  echo "!! API did not become healthy within timeout" >&2
  exit 1
fi

echo "==> Health check"
curl -i "${BASE}/health" || true

echo "==> Root endpoint"
curl -i "${BASE}/" || true

echo "==> SignalR negotiate"
curl -i -X POST "${BASE}/hubs/trading/negotiate?negotiateVersion=1" -H 'Content-Type: application/json' --data '{}' || true

echo "==> Done"

