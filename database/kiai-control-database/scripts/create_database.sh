#!/usr/bin/env bash
set -euo pipefail

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-dev}"
PGPASSWORD="${PGPASSWORD:-dev-local-only}"
DB_NAME="${DB_NAME:-dev}"
PG_CONTAINER_NAME="${PG_CONTAINER_NAME:-postgres-dev}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
MIGRATION_FILE="${ROOT_DIR}/migrations/001_initial_api_schema.sql"
SEED_FILE="${ROOT_DIR}/scripts/001_seed_admin_user.sql"

if [[ ! "${DB_NAME}" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
  echo "DB_NAME must contain only letters, numbers, and underscores, and cannot start with a number." >&2
  exit 1
fi

has_local_psql() {
  command -v psql >/dev/null 2>&1
}

check_db_exists_local() {
  local result
  result="$(PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '${DB_NAME}'" 2>/dev/null)" || return 1
  [[ "${result}" == "1" ]]
}

check_db_exists_docker() {
  local result
  result="$(docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '${DB_NAME}'" 2>/dev/null)" || return 1
  [[ "${result}" == "1" ]]
}

public_table_count_local() {
  PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${DB_NAME}" -tAc "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'"
}

public_table_count_docker() {
  docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d "${DB_NAME}" -tAc "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'"
}

create_db_local() {
  PGPASSWORD="${PGPASSWORD}" createdb -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" "${DB_NAME}"
}

create_db_docker() {
  docker exec -i "${PG_CONTAINER_NAME}" createdb -U "${PGUSER}" "${DB_NAME}"
}

run_sql_file_local() {
  local db_name="$1"
  local file_path="$2"
  PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${db_name}" -v ON_ERROR_STOP=1 -f "${file_path}"
}

run_sql_file_docker() {
  local db_name="$1"
  local file_path="$2"
  docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d "${db_name}" -v ON_ERROR_STOP=1 < "${file_path}"
}

ensure_migration_ledger_local() {
  PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${DB_NAME}" \
    -v ON_ERROR_STOP=1 -c "CREATE TABLE IF NOT EXISTS schema_migrations (migration_name varchar(255) PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())" >/dev/null
}

ensure_migration_ledger_docker() {
  docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d "${DB_NAME}" \
    -v ON_ERROR_STOP=1 -c "CREATE TABLE IF NOT EXISTS schema_migrations (migration_name varchar(255) PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())" >/dev/null
}

migration_applied_local() {
  local migration_name="$1"
  local result
  result="$(PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${DB_NAME}" -tAc \
    "SELECT 1 FROM schema_migrations WHERE migration_name = '${migration_name}'")"
  [[ "${result}" == "1" ]]
}

migration_applied_docker() {
  local migration_name="$1"
  local result
  result="$(docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d "${DB_NAME}" -tAc \
    "SELECT 1 FROM schema_migrations WHERE migration_name = '${migration_name}'")"
  [[ "${result}" == "1" ]]
}

mark_migration_local() {
  local migration_name="$1"
  PGPASSWORD="${PGPASSWORD}" psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${DB_NAME}" \
    -v ON_ERROR_STOP=1 -c "INSERT INTO schema_migrations (migration_name) VALUES ('${migration_name}') ON CONFLICT DO NOTHING" >/dev/null
}

mark_migration_docker() {
  local migration_name="$1"
  docker exec -i "${PG_CONTAINER_NAME}" psql -U "${PGUSER}" -d "${DB_NAME}" \
    -v ON_ERROR_STOP=1 -c "INSERT INTO schema_migrations (migration_name) VALUES ('${migration_name}') ON CONFLICT DO NOTHING" >/dev/null
}

apply_followup_migrations_local() {
  local migration_file
  for migration_file in "${ROOT_DIR}"/migrations/*.sql; do
    [[ "${migration_file}" == "${MIGRATION_FILE}" ]] && continue
    local migration_name
    migration_name="$(basename "${migration_file}")"
    if migration_applied_local "${migration_name}"; then
      echo "Skipping applied migration ${migration_name}."
      continue
    fi
    echo "Applying migration ${migration_name}..."
    run_sql_file_local "${DB_NAME}" "${migration_file}"
    mark_migration_local "${migration_name}"
  done
}

apply_followup_migrations_docker() {
  local migration_file
  for migration_file in "${ROOT_DIR}"/migrations/*.sql; do
    [[ "${migration_file}" == "${MIGRATION_FILE}" ]] && continue
    local migration_name
    migration_name="$(basename "${migration_file}")"
    if migration_applied_docker "${migration_name}"; then
      echo "Skipping applied migration ${migration_name}."
      continue
    fi
    echo "Applying migration ${migration_name}..."
    run_sql_file_docker "${DB_NAME}" "${migration_file}"
    mark_migration_docker "${migration_name}"
  done
}

if has_local_psql; then
  if ! check_db_exists_local; then
    echo "Creating database ${DB_NAME}..."
    create_db_local
  fi

  if [[ "$(public_table_count_local)" == "0" ]]; then
    echo "Applying database schema..."
    run_sql_file_local "${DB_NAME}" "${MIGRATION_FILE}"
  else
    echo "Schema already exists in ${DB_NAME}; skipping initial migration."
  fi

  ensure_migration_ledger_local
  mark_migration_local "$(basename "${MIGRATION_FILE}")"
  apply_followup_migrations_local

  echo "Applying admin bootstrap..."
  run_sql_file_local "${DB_NAME}" "${SEED_FILE}"
else
  if ! check_db_exists_docker; then
    echo "Creating database ${DB_NAME}..."
    create_db_docker
  fi

  if [[ "$(public_table_count_docker)" == "0" ]]; then
    echo "Applying database schema..."
    run_sql_file_docker "${DB_NAME}" "${MIGRATION_FILE}"
  else
    echo "Schema already exists in ${DB_NAME}; skipping initial migration."
  fi

  ensure_migration_ledger_docker
  mark_migration_docker "$(basename "${MIGRATION_FILE}")"
  apply_followup_migrations_docker

  echo "Applying admin bootstrap..."
  run_sql_file_docker "${DB_NAME}" "${SEED_FILE}"
fi

echo "Database ${DB_NAME} is ready."
