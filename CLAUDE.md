# CLAUDE.md — Таверна Аферистов / TTRPGHub

Этот файл содержит контекст, правила и соглашения проекта для Claude Code.
Читай его при каждой сессии перед любыми изменениями.

---

## Идентификация проекта

- **Бренд (UI):** Таверна Аферистов
- **Код и неймспейсы:** `TTRPGHub` (без исключений — Таверна Аферистов только в UI)
- **Репозиторий:** https://github.com/F3T1W/TTRPGHub.git
- **Формат solution:** `.slnx` только (не `.sln`)
- **Target framework:** `net10.0` (LTS)

---

## Архитектура

Clean Architecture. Зависимости идут строго внутрь:

```
Web/API → Application → Domain
Infrastructure/Persistence → Application → Domain
```

**Никогда не нарушай направление зависимостей.**

### Проекты
| Проект | Назначение |
|---|---|
| `TTRPGHub.Domain` | Сущности, Value Objects, Domain Events, интерфейсы репозиториев |
| `TTRPGHub.Application` | CQRS handlers (MediatR 12), Pipeline Behaviours, интерфейсы сервисов |
| `TTRPGHub.Infrastructure` | JWT, BCrypt, Redis cache, CurrentUser |
| `TTRPGHub.Persistence` | EF Core 10, AppDbContext, репозитории, UnitOfWork, миграции |
| `TTRPGHub.API` | Minimal API Endpoints, Program.cs, Scalar, Health Checks |
| `TTRPGHub.Web` | Blazor WASM, Refit клиент, Auth state, страницы |
| `TTRPGHub.Contracts` | Общие DTOs (пока минимальный) |
| `TTRPGHub.ServiceDefaults` | Заготовка под .NET Aspire (Phase 1) |

---

## Ключевые паттерны — как их применять

### CQRS + MediatR 12
- **Commands** меняют состояние, возвращают `Result<T>` или `Result`
- **Queries** только читают, кешируемые реализуют `ICacheableQuery`
- Каждый feature = отдельная папка: `Features/Auth/Commands/Register/`
- В папке: `*Command.cs`, `*CommandHandler.cs`, `*CommandValidator.cs`

### Result<T>
```csharp
// Всегда используй Result<T> в Application/Domain, не бросай исключения
return Error.NotFound(nameof(User));   // не throw
return Result<T>.Success(value);
```

### Strongly Typed IDs
```csharp
// Всегда используй типизированные ID, не Guid напрямую
public UserId Id { get; }   // не Guid Id
```

### Domain Events
```csharp
// Поднимай события в агрегатах через RaiseDomainEvent()
// AppDbContext диспатчит их через MediatR после SaveChanges
RaiseDomainEvent(new UserCreatedEvent(Id));
```

### Pipeline Behaviours (порядок важен)
```
LoggingBehaviour → ValidationBehaviour → CachingBehaviour → Handler
```

### EF конфигурации
- Используй `IEntityTypeConfiguration<T>` в отдельных файлах `Configurations/`
- Имена таблиц и колонок — `snake_case` (PostgreSQL-конвенция)
- Owned types для Value Objects (`Email`, `UserProfile`)

---

## Кодстайл

- **Язык UI (Razor, сообщения):** русский
- **Язык кода (классы, методы, переменные):** английский
- **Комментарии в коде:** только когда WHY неочевиден
- **Async:** всегда с `CancellationToken ct` параметром
- **DI:** primary constructors везде где возможно (`class Foo(IBar bar)`)
- **Модификаторы:** `internal sealed` для handlers/validators/repos, `public` для DI extensions и интерфейсов

---

## Пакеты — Central Package Management

**Все версии только в `Directory.Packages.props`.**
Никогда не добавляй `Version=` в `<PackageReference>` в csproj.

При добавлении нового пакета:
```bash
dotnet add <csproj> package <PackageName>
# CPM сам добавит в Directory.Packages.props
```

---

## Database

- **СУБД:** PostgreSQL 16 (Docker)
- **ORM:** Entity Framework Core 10 + Npgsql
- **Миграции:** хранятся в `TTRPGHub.Persistence/Migrations/`
- **Новая миграция:**
  ```bash
  dotnet ef migrations add <Name> \
    --project src/Infrastructure/TTRPGHub.Persistence \
    --startup-project src/Presentation/TTRPGHub.API \
    --output-dir Migrations
  ```
- **Автомиграция в dev:** включена в `Program.cs` (только `Development`)

### Строки подключения (dev)
```
Postgres: Host=localhost;Port=5432;Database=taverna_db;Username=taverna;Password=taverna_dev_pass
Redis:    localhost:6379
```

---

## Blazor Web

- **Тип:** WebAssembly (WASM), не Server
- **Auth:** JWT в `localStorage` через `TokenStorage`, `AppAuthStateProvider`
- **API клиент:** Refit (`IApiClient`) с `AuthHeaderHandler` DelegatingHandler
- **Конфиг API:** `wwwroot/appsettings.json` → `ApiBaseUrl`
- **Стейт-менеджмент:** Fluxor (подключён, используется в Phase 1)

---

## Цветовая тема "Тёмная магия"

Только CSS-переменные, Bootstrap overrides. Никаких inline стилей цветов.

```css
--ta-accent:       #7c3aed   /* основной фиолетовый */
--ta-accent-light: #a78bfa   /* светлый фиолетовый */
--ta-gold:         #f59e0b   /* золото для акцентов */
--ta-bg:           #0d0d1a   /* фон */
--ta-surface:      #13132a   /* поверхности/карточки */
```

Классы: `btn-ta-primary`, `ta-card`, `ta-nav-link`, `ta-navbar-brand`, `ta-auth-card`, `ta-level-badge`.

---

## Docker Compose (dev-инфраструктура)

```bash
docker compose up -d --wait    # поднять
docker compose down            # остановить
docker compose logs -f         # логи
```

Сервисы: `taverna_postgres` (5432), `taverna_redis` (6379), `taverna_seq` (8080/5341).

---

## Запуск проекта

```bash
# API (порт 5014)
dotnet run --project src/Presentation/TTRPGHub.API --environment Development

# Web (порт 5141)
dotnet run --project src/Presentation/TTRPGHub.Web
```

---

## Проверка сборки

После любых изменений:
```bash
dotnet build TTRPGHub.slnx
# Ожидаем: 0 Warning(s), 0 Error(s)
```

`TreatWarningsAsErrors=true` — ни одного warning.

---

## Правила для Claude

1. **Никогда** не используй `TavernaAferistov` или `Taverna` в названиях классов/неймспейсах — только `TTRPGHub`
2. **Никогда** не добавляй `.sln` файл — только `.slnx`
3. **Не добавляй** `Version=` в `<PackageReference>` — CPM управляет версиями
4. **Не бросай** исключения в бизнес-логике — используй `Result<T>` и `Error`
5. **Не пиши** избыточные комментарии (summary, param, returns) — код самодокументируется
6. **Всегда** проверяй сборку после изменений: `dotnet build TTRPGHub.slnx`
7. **Всегда** добавляй `CancellationToken ct` в async методы
8. **Сохраняй** snake_case для имён таблиц и колонок в EF конфигурациях
9. При добавлении страниц — определяй `<PageTitle>` и `@page` директивы
10. **Blazor компоненты** в `Components/`, **страницы** в `Pages/`
