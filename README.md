# Таверна Аферистов

> Первый русскоязычный хаб для настольных ролевых игр (TTRPG).
> Некоммерческий проект сообщества.

[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://img.shields.io/badge/build-passing-brightgreen)]()

---

## О проекте

**Таверна Аферистов** — это веб-платформа, которая объединяет всё необходимое для игры в D&D и другие TTRPG:

- Создание и хранение листов персонажей
- Поиск групп и сессий (LFG)
- Справочник правил на русском языке
- Форум и сообщество игроков

Проект задуман как некоммерческий, сделанный для сообщества и силами сообщества.

---

## Технологический стек

| Слой | Технологии |
|---|---|
| **Backend** | ASP.NET Core 10 · Minimal API · MediatR 12 (CQRS) · FluentValidation |
| **База данных** | PostgreSQL 16 · Entity Framework Core 10 · Npgsql |
| **Кэш** | Redis 7 · IDistributedCache |
| **Аутентификация** | JWT Bearer · BCrypt |
| **Frontend** | Blazor WebAssembly · Bootstrap 5 · Blazor.Bootstrap 3 |
| **Логирование** | Serilog · Seq |
| **Инфраструктура** | Docker Compose |

---

## Архитектура

Проект построен по принципам **Clean Architecture** с разделением на слои:

```
src/
├── Core/
│   ├── TTRPGHub.Domain          # Сущности, Value Objects, Domain Events
│   └── TTRPGHub.Application     # CQRS handlers, Pipeline Behaviours, Interfaces
├── Infrastructure/
│   ├── TTRPGHub.Infrastructure  # JWT, BCrypt, Redis cache, ICurrentUser
│   └── TTRPGHub.Persistence     # EF Core, Repositories, UnitOfWork, Migrations
├── Presentation/
│   ├── TTRPGHub.API             # Minimal API Endpoints, Scalar UI, Health Checks
│   └── TTRPGHub.Web             # Blazor WASM, Auth state, Refit API client
└── Shared/
    └── TTRPGHub.Contracts       # Общие DTOs между API и Web
tests/
├── TTRPGHub.Domain.Tests        # Юнит-тесты домена
├── TTRPGHub.Application.Tests   # Юнит-тесты use cases
├── TTRPGHub.Infrastructure.Tests
└── TTRPGHub.API.Tests           # Интеграционные тесты (Testcontainers)
```

### Ключевые паттерны

- **CQRS + MediatR** — каждый use case = отдельный handler
- **Pipeline Behaviours** — Logging → Validation → Caching → Handler
- **Result\<T\>** — явная обработка ошибок без исключений в бизнес-логике
- **Strongly Typed IDs** — `UserId`, `CharacterId` как `readonly record struct`
- **Domain Events** — через `IDomainEvent : INotification`, dispatch после SaveChanges
- **Central Package Management** — все версии в `Directory.Packages.props`

---

## Быстрый старт

### Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Локальный запуск

```bash
# 1. Клонируй репозиторий
git clone https://github.com/F3T1W/TTRPGHub.git
cd TTRPGHub

# 2. Скопируй файл окружения
cp .env.example .env
# Отредактируй .env при необходимости

# 3. Подними инфраструктуру (Postgres + Redis + Seq)
docker compose up -d --wait

# 4. Запусти API (миграции применятся автоматически в dev-режиме)
dotnet run --project src/Presentation/TTRPGHub.API --environment Development

# 5. В отдельном терминале запусти Web
dotnet run --project src/Presentation/TTRPGHub.Web
```

После запуска:
| Сервис | URL |
|---|---|
| Web UI | http://localhost:5141 |
| API | http://localhost:5014 |
| Scalar API Docs | http://localhost:5014/scalar/v1 |
| Health Check | http://localhost:5014/health |
| Seq (логи) | http://localhost:8080 |

### Ручное применение миграций

```bash
dotnet ef database update \
  --project src/Infrastructure/TTRPGHub.Persistence \
  --startup-project src/Presentation/TTRPGHub.API
```

---

## Структура API

### Аутентификация
```
POST /api/auth/register   — регистрация
POST /api/auth/login      — вход (возвращает JWT)
```

### Персонажи (требуют JWT)
```
GET  /api/characters/me   — мои персонажи
POST /api/characters      — создать персонажа
GET  /api/characters/{id} — персонаж по ID
```

---

## Цветовая схема — «Тёмная магия»

| Роль | Цвет |
|---|---|
| Акцент (primary) | `#7c3aed` — фиолетовый |
| Акцент светлый | `#a78bfa` |
| Золото | `#f59e0b` |
| Фон | `#0d0d1a` |
| Поверхность | `#13132a` |

---

## Роадмап

### MVP (текущий этап)
- [x] Clean Architecture + CQRS
- [x] Аутентификация (JWT)
- [x] Управление персонажами
- [x] Blazor Web UI с тёмной темой
- [x] Docker Compose окружение

### Phase 1
- [ ] Полный лист персонажа D&D 5e
- [ ] Загрузка аватара (S3-compatible)
- [ ] Система сессий (создание, запись)
- [ ] LFG (поиск группы)
- [ ] .NET Aspire

### Phase 2
- [ ] Справочник правил и заклинаний
- [ ] Форум и обсуждения
- [ ] Трекер инициативы
- [ ] Онлайн-дайс

### Phase 3
- [ ] Мобильное PWA
- [ ] Виртуальный стол
- [ ] API для сторонних разработчиков

---

## Вклад в проект

Проект открыт для участия! Если хочешь помочь:

1. Fork репозитория
2. Создай ветку: `git checkout -b feature/моя-фича`
3. Коммить изменения: `git commit -m 'feat: добавил крутую фичу'`
4. Push: `git push origin feature/моя-фича`
5. Открой Pull Request

---

## Лицензия

[MIT](LICENSE) © 2025 F3T1W

---

> *«Все великие приключения начинаются в таверне.»*
