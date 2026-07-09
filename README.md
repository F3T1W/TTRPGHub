# Таверна Аферистов

> Русскоязычный некоммерческий хаб для настольных ролевых игр (TTRPG).
> Кодовое имя репозитория: **TTRPGHub**.

[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Веб-платформа для игроков и мастеров: листы персонажей, справочник правил, поиск группы, сообщество и виртуальный стол с realtime-синхронизацией (карты, туман войны, инициатива, макросы). Основной фокус разработки — **Pathfinder 2e**, цель — полноценная замена Foundry VTT для русскоязычных групп.

---

## Быстрый старт

### Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Локальный запуск

```bash
git clone https://github.com/F3T1W/TTRPGHub.git
cd TTRPGHub

cp .env.example .env   # при необходимости отредактируй

docker compose up -d --wait

dotnet run --project src/Presentation/TTRPGHub.API --environment Development
dotnet run --project src/Presentation/TTRPGHub.Web   # в отдельном терминале
```

| Сервис | URL |
|---|---|
| Web UI | http://localhost:5141 |
| API | http://localhost:5014 |
| API Docs (Scalar) | http://localhost:5014/scalar/v1 |
| Health Check | http://localhost:5014/health |
| Seq (логи) | http://localhost:8080 |

Миграции в `Development` применяются автоматически при старте API. Вручную:

```bash
dotnet ef database update \
  --project src/Infrastructure/TTRPGHub.Persistence \
  --startup-project src/Presentation/TTRPGHub.API
```

Проверка сборки:

```bash
dotnet build TTRPGHub.slnx
```

---

## Технологии

ASP.NET Core 10 · Minimal API · MediatR (CQRS) · EF Core 10 · PostgreSQL 16 · Redis · JWT · Blazor WebAssembly · Bootstrap 5 · SignalR · Docker Compose

Архитектура — **Clean Architecture** (Domain → Application → Infrastructure/Persistence → API/Web).

```
src/
├── Core/           Domain, Application
├── Infrastructure/ Persistence, Infrastructure
├── Presentation/   API, Web (Blazor WASM)
└── Shared/         Contracts
tests/              Domain, Application, Infrastructure, API
```

---

## Документация

| Файл | Содержание |
|---|---|
| [ROADMAP.md](ROADMAP.md) | Текущее состояние, планы и приоритеты |
| [PROJECT.md](PROJECT.md) | Исходное видение проекта |
| [CLAUDE.md](CLAUDE.md) | Конвенции кода, паттерны, правила для разработки |

Полный список эндпоинтов — в Scalar после запуска API.

---

## Вклад в проект

1. Fork репозитория
2. Ветка: `git checkout -b feature/моя-фича`
3. Коммит: `git commit -m 'feat: описание изменения'`
4. Push и Pull Request

Перед PR: `dotnet build TTRPGHub.slnx` без warnings и errors.

---

## Лицензия

[MIT](LICENSE) © 2025–2026 F3T1W
