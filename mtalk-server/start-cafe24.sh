#!/usr/bin/env bash
# CAFE24 VPS — MTalk unified server (PostgreSQL 18.4 / JDK 21)
set -euo pipefail

APP_DIR="${APP_DIR:-/app/mtalk}"
JAR="${JAR:-$APP_DIR/mtalk-server-1.0.0.jar}"
PROFILE="${SPRING_PROFILES_ACTIVE:-cafe24}"

export SERVER_PORT="${SERVER_PORT:-8080}"
export MBENCH_DB_HOST="${MBENCH_DB_HOST:-localhost}"
export MBENCH_DB_PORT="${MBENCH_DB_PORT:-5432}"
export MBENCH_DB_NAME="${MBENCH_DB_NAME:-mbench}"
export MBODY_DB_HOST="${MBODY_DB_HOST:-localhost}"
export MBODY_DB_PORT="${MBODY_DB_PORT:-5432}"
export MBODY_DB_NAME="${MBODY_DB_NAME:-mbody_revised}"

if [[ -z "${MBENCH_DB_USER:-}" || -z "${MBENCH_DB_PASSWORD:-}" ]]; then
  echo "ERROR: set MBENCH_DB_USER and MBENCH_DB_PASSWORD" >&2
  exit 1
fi
export MBODY_DB_USER="${MBODY_DB_USER:-$MBENCH_DB_USER}"
export MBODY_DB_PASSWORD="${MBODY_DB_PASSWORD:-$MBENCH_DB_PASSWORD}"

exec java -Xms256m -Xmx512m -jar "$JAR" --spring.profiles.active="$PROFILE"
