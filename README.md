# CodeMetrics Backend

ASP.NET Core API для сбора метрик из Gitea, анализа через Ollama и интеграции с SonarQube.

## Состав docker-compose

| Сервис | Порт | Назначение |
|--------|------|------------|
| `backend` | 8080 | REST API CodeMetrics |
| `gitea` | 3000 | Git-сервер |
| `sonarqube` | 9000 | Анализ качества кода |
| `ollama` | 11434 | LLM для AI-анализа |
| `runner` | — | Gitea Actions runner |

Docker-сеть: **`codemetrics-dev-network`** (фиксированное имя — используйте его для фронтенда).

## Быстрый старт

```bash
cd CodeMetricsBackend
cp .env.example .env
# заполните DATABASE_CONNECTION_STRING

docker compose up -d
docker ps   # все сервисы должны быть Up (healthy где есть healthcheck)
```

Проверка сервисов:

```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
curl http://localhost:8080/api/config
curl http://localhost:9000/api/system/status
curl http://localhost:3000/api/v1/version
```

## Инициализация Gitea и SonarQube

После того как контейнеры поднялись, выполните bash-скрипт (нужны `curl`, `bash`):

```bash
chmod +x scripts/init-services.sh
./scripts/init-services.sh
```

Скрипт через HTTP:

1. Дожидается Gitea и SonarQube
2. Устанавливает Gitea (если первый запуск)
3. Создаёт API-токен Gitea
4. **Импортирует** репозиторий [FolderEncryptor](https://github.com/renam4113/FolderEncryptor.git)
5. Создаёт проект SonarQube **`renamshina`** (Other CI, ключ из `appsettings.json`)
6. Генерирует user token SonarQube
7. Выводит переменные для `.env`

### Прокидывание SonarQube token в backend

Добавьте в `.env`:

```env
SONAR_TOKEN=sqp_xxxxxxxx
GITEA_ACCESS_TOKEN=xxxxxxxx
```

Перезапустите backend:

```bash
docker compose --env-file .env up -d backend
```

Backend ожидает токен через `IOptionsMonitor`: hosted-сервис `SonarQubeTokenReadinessHostedService` логирует предупреждение, если `SonarQube__Token` пуст, и в течение 10 минут проверяет его появление (после `docker compose restart` с новым `.env` токен подхватится без пересборки).

## Gitea Actions: ci.yml и runner

1. Убедитесь, что runner зарегистрирован:

```bash
docker logs gitea-runner
docker compose --env-file .env up -d runner
```

2. В Gitea включите Actions для репозитория и добавьте secrets:
   - `SONAR_TOKEN` — из вывода `init-services.sh`
   - `GITEA_TOKEN` — токен Gitea для clone в CI

3. Создайте `.gitea/workflows/ci.yml` (или используйте файл из репозитория):

```yaml
on:
  push:
    branches:
      - develop
  pull_request:
    types: [opened, synchronize, reopened]

name: SonarQube Scan
jobs:
  sonarqube:
    name: SonarQube Scan
    runs-on: ubuntu-latest
    container: mcr.microsoft.com/dotnet/sdk:8.0

    steps:
      - name: Wait for 10 seconds
        run: sleep 10

      - name: Clone repository
        run: |
          git clone http://x-access-token:${{ secrets.GITEA_TOKEN }}@gitea:3000/admin/CodeMetricsBackend.git .
          git checkout ${{ github.sha }}

      - name: Install SonarScanner for .NET
        run: |
          dotnet tool install --global dotnet-sonarscanner

      - name: Begin SonarScanner
        run: |
          export PATH="$PATH:/root/.dotnet/tools"
          dotnet sonarscanner begin \
            /k:"renamshina" \
            /d:sonar.token="${{ secrets.SONAR_TOKEN }}" \
            /d:sonar.host.url="http://sonarqube:9000"

      - name: Build project
        run: |
          export PATH="$PATH:/root/.dotnet/tools"
          dotnet build

      - name: End SonarScanner (upload results)
        run: |
          export PATH="$PATH:/root/.dotnet/tools"
          dotnet sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN }}"
```

4. В настройках workflow выберите runner **`codemetrics-runner`** (label `ubuntu-latest`) после его успешной регистрации.

## API SonarQube (для фронтенда)

| Метод | Путь |
|-------|------|
| GET | `/api/SonarQube/scan-summary/{projectKey}?branch=` |
| GET | `/api/SonarQube/measures/{projectKey}?branch=&metrics=` |
| GET | `/api/SonarQube/issues/{projectKey}?branch=` |
| GET | `/api/SonarQube/metrics` |
| GET | `/api/config` |

## Локальная разработка без Docker

```bash
cd CodeMetrics
dotnet run
```

Swagger: http://localhost:8080/swagger
