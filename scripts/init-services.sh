#!/usr/bin/env bash
# Инициализация Gitea (импорт репозитория) и SonarQube (проект + токен) после docker compose up.
set -euo pipefail

GITEA_URL="${GITEA_URL:-http://localhost:3000}"
SONAR_URL="${SONAR_URL:-http://localhost:9000}"
GITEA_ADMIN_USER="${GITEA_ADMIN_USER:-admin}"
GITEA_ADMIN_PASSWORD="${GITEA_ADMIN_PASSWORD:-admin123}"
GITEA_ADMIN_EMAIL="${GITEA_ADMIN_EMAIL:-admin@example.com}"
SONAR_ADMIN_USER="${SONAR_ADMIN_USER:-admin}"
SONAR_ADMIN_PASSWORD="${SONAR_ADMIN_PASSWORD:-admin}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-renamshina}"
IMPORT_REPO_URL="${IMPORT_REPO_URL:-https://github.com/renam4113/FolderEncryptor.git}"
IMPORT_REPO_NAME="${IMPORT_REPO_NAME:-FolderEncryptor}"

log() { printf '[init-services] %s\n' "$*"; }

wait_for_http() {
  local name="$1"
  local url="$2"
  local attempts="${3:-60}"
  local delay="${4:-5}"

  log "Ожидание $name ($url)..."
  for ((i = 1; i <= attempts; i++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      log "$name доступен"
      return 0
    fi
    sleep "$delay"
  done

  log "ОШИБКА: $name не ответил за $((attempts * delay)) секунд"
  return 1
}

gitea_install_if_needed() {
  local status
  status="$(curl -s -o /dev/null -w '%{http_code}' "$GITEA_URL/api/v1/version" || true)"

  if [[ "$status" == "200" ]]; then
    log "Gitea уже установлена"
    return 0
  fi

  log "Первичная установка Gitea через API..."
  curl -fsS -X POST "$GITEA_URL/api/v1/install" \
    -H 'Content-Type: application/json' \
    -d "{
      \"db_type\": \"postgres\",
      \"db_host\": \"gitea-db:5432\",
      \"db_user\": \"gitea\",
      \"db_passwd\": \"gitea\",
      \"db_name\": \"gitea\",
      \"ssl_mode\": \"disable\",
      \"charset\": \"utf8\",
      \"admin_name\": \"$GITEA_ADMIN_USER\",
      \"admin_passwd\": \"$GITEA_ADMIN_PASSWORD\",
      \"admin_confirm_passwd\": \"$GITEA_ADMIN_PASSWORD\",
      \"admin_email\": \"$GITEA_ADMIN_EMAIL\",
      \"repo_root_path\": \"/data/git/repositories\",
      \"lfs_root_path\": \"/data/lfs\",
      \"run_user\": \"git\",
      \"domain\": \"localhost\",
      \"ssh_port\": 2222,
      \"http_port\": 3000,
      \"app_name\": \"Gitea\",
      \"repo_script_type\": \"bash\"
    }"
  log "Gitea установлена"
}

gitea_create_token() {
  log "Создание API-токена Gitea..."
  local response
  response="$(curl -fsS -u "$GITEA_ADMIN_USER:$GITEA_ADMIN_PASSWORD" \
    -X POST "$GITEA_URL/api/v1/users/$GITEA_ADMIN_USER/tokens" \
    -H 'Content-Type: application/json' \
    -d '{"name":"codemetrics-init","scopes":["all"]}')"

  GITEA_TOKEN="$(echo "$response" | sed -n 's/.*"sha1"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
  if [[ -z "$GITEA_TOKEN" ]]; then
    log "Не удалось создать токен Gitea. Ответ: $response"
    exit 1
  fi
  log "Gitea token: $GITEA_TOKEN"
}

gitea_import_repo() {
  log "Импорт репозитория $IMPORT_REPO_URL в Gitea..."
  local exists
  exists="$(curl -fsS -H "Authorization: token $GITEA_TOKEN" \
    "$GITEA_URL/api/v1/repos/$GITEA_ADMIN_USER/$IMPORT_REPO_NAME" -o /dev/null -w '%{http_code}' || true)"

  if [[ "$exists" == "200" ]]; then
    log "Репозиторий $IMPORT_REPO_NAME уже существует"
    return 0
  fi

  curl -fsS -X POST "$GITEA_URL/api/v1/repos/migrate" \
    -H "Authorization: token $GITEA_TOKEN" \
    -H 'Content-Type: application/json' \
    -d "{
      \"clone_addr\": \"$IMPORT_REPO_URL\",
      \"repo_name\": \"$IMPORT_REPO_NAME\",
      \"repo_owner\": \"$GITEA_ADMIN_USER\",
      \"mirror\": false,
      \"private\": false,
      \"description\": \"Imported from GitHub\",
      \"issues\": false,
      \"labels\": false,
      \"milestones\": false,
      \"pull_requests\": false,
      \"releases\": false
    }"
  log "Репозиторий $IMPORT_REPO_NAME импортирован"
}

gitea_runner_token() {
  log "Получение registration token для act_runner..."
  RUNNER_TOKEN="$(curl -fsS -u "$GITEA_ADMIN_USER:$GITEA_ADMIN_PASSWORD" \
    -X POST "$GITEA_URL/api/v1/admin/actions/runners/registration-token" | \
    sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"

  if [[ -n "$RUNNER_TOKEN" ]]; then
    log "Runner registration token: $RUNNER_TOKEN"
  else
    log "Не удалось получить runner token (Actions могут быть отключены)"
  fi
}

sonar_change_password_if_needed() {
  local auth_check
  auth_check="$(curl -s -o /dev/null -w '%{http_code}' -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
    "$SONAR_URL/api/authentication/validate" || true)"

  if [[ "$auth_check" != "200" ]]; then
    log "SonarQube: проверьте учётные данные admin"
    return 1
  fi
}

sonar_create_project() {
  log "Создание проекта SonarQube ($SONAR_PROJECT_KEY) с CI = Other..."
  local exists
  exists="$(curl -s -o /dev/null -w '%{http_code}' -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
    "$SONAR_URL/api/projects/search?projects=$SONAR_PROJECT_KEY" || true)"

  if [[ "$exists" == "200" ]]; then
    local count
    count="$(curl -fsS -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
      "$SONAR_URL/api/projects/search?projects=$SONAR_PROJECT_KEY" | \
      sed -n 's/.*"total"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/p')"
    if [[ "${count:-0}" != "0" ]]; then
      log "Проект $SONAR_PROJECT_KEY уже существует"
      return 0
    fi
  fi

  curl -fsS -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
    -X POST "$SONAR_URL/api/projects/create?project=$SONAR_PROJECT_KEY&name=$SONAR_PROJECT_KEY&mainBranch=main"
  log "Проект $SONAR_PROJECT_KEY создан (Other CI — без привязки к DevOps-платформе)"
}

sonar_create_token() {
  log "Генерация user token SonarQube..."
  local response
  response="$(curl -fsS -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
    -X POST "$SONAR_URL/api/user_tokens/generate?name=codemetrics-backend")"

  SONAR_TOKEN="$(echo "$response" | sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
  if [[ -z "$SONAR_TOKEN" ]]; then
    log "Не удалось создать SonarQube token. Ответ: $response"
    exit 1
  fi
  log "SonarQube token: $SONAR_TOKEN"
}

main() {
  wait_for_http "Gitea" "$GITEA_URL/api/v1/version" 60 5
  wait_for_http "SonarQube" "$SONAR_URL/api/system/status" 90 10

  gitea_install_if_needed
  gitea_create_token
  gitea_import_repo
  gitea_runner_token

  sonar_change_password_if_needed
  sonar_create_project
  sonar_create_token

  cat <<EOF

================================================================================
Инициализация завершена. Добавьте в .env и перезапустите backend:

GITEA_ACCESS_TOKEN=$GITEA_TOKEN
GITEA_OWNER=$GITEA_ADMIN_USER
SONAR_TOKEN=$SONAR_TOKEN
SONAR_PROJECT_KEY=$SONAR_PROJECT_KEY
GITEA_RUNNER_TOKEN=${RUNNER_TOKEN:-REPLACE_ME}

Затем:
  docker compose --env-file .env up -d backend runner

Для Gitea Actions добавьте secrets в репозитории:
  SONAR_TOKEN=$SONAR_TOKEN
  GITEA_TOKEN=$GITEA_TOKEN

Импортированный репозиторий:
  $GITEA_URL/$GITEA_ADMIN_USER/$IMPORT_REPO_NAME
================================================================================
EOF
}

main "$@"
