# Таверна Аферистов — Project Document v1.3

> Русскоязычный некоммерческий хаб для НРИ-сообщества.  
> Всё в одном месте: правила, персонажи, поиск группы, кампании.
>
> Репозиторий / папка проекта: `TTRPGHub`

---

## 1. Миссия и цели

**Миссия:** создать лучший бесплатный русскоязычный ресурс для игроков и мастеров настольных ролевых игр — от первого знакомства с D&D до ведения многолетней кампании.

**Принципы:**
- Некоммерческий, без пейволла и рекламы
- Сделано игроками для игроков
- Открытый и расширяемый

**Целевая аудитория:**
- Новички, которые не знают с чего начать
- Опытные игроки, которым нужны удобные инструменты
- Мастера подземелий (DM/GM)
- Русскоязычное сообщество в первую очередь; в перспективе — мультиязычность

---

## 2. Продуктовые модули

### MVP (фаза 0 — личное использование)
| Модуль | Описание |
|--------|----------|
| **Rules Reference** | Справочник правил D&D 5e 2024 на русском. Поиск, фильтры, удобная навигация. |
| **Character Builder** | Конструктор персонажа: раса, класс, характеристики, навыки, снаряжение. Лист персонажа онлайн. |
| **My Characters** | Личный кабинет: список персонажей, быстрый просмотр. |
| **Auth** | Регистрация / вход. JWT + Refresh Tokens. |

### Фаза 1 — публичный запуск
| Модуль | Описание |
|--------|----------|
| **LFG (Looking for Group)** | Поиск группы: объявления с фильтрами (город, онлайн/офлайн, система, расписание). |
| **User Profiles** | Публичные профили: опыт, предпочтения, персонажи, отзывы. |
| **Spellbook** | Справочник заклинаний с фильтрами по классу, уровню, школе. |
| **Bestiary** | Справочник монстров с фильтрами. |

### Фаза 2 — инструменты мастера
| Модуль | Описание |
|--------|----------|
| **Campaign Manager** | Создание и ведение кампании: сессии, NPC, локации, заметки. |
| **Session Notes** | Заметки по сессиям с привязкой к кампании и персонажам. |
| **Encounter Builder** | Конструктор столкновений с оценкой сложности. |
| **Initiative Tracker** | Трекер инициативы на сессии (real-time через SignalR). |

### Фаза 3 — сообщество
| Модуль | Описание |
|--------|----------|
| **Forum / Discussions** | Тематические обсуждения по системам, городам, механикам. |
| **Homebrew Hub** | Публикация и рейтинг хоумбрю: расы, классы, заклинания, приключения. |
| **Reviews** | Отзывы на DM-ов и группы. |
| **Events** | Анонсы конвентов, open table, демо-игр по городам. |

---

## 3. Архитектура

### Общий подход
**Clean Architecture** с явным разделением слоёв и зависимостями строго внутрь:

```
Presentation → Application → Domain
Infrastructure → Application → Domain
```

Домен не знает ни об инфраструктуре, ни о фреймворках. Всё взаимодействие через интерфейсы и абстракции.

### CQRS + MediatR
Все операции — через Commands (изменение состояния) и Queries (чтение). Никаких "сервисов с 20 методами". Каждый хэндлер — одна ответственность (SRP).

### Паттерны
- **Repository** — абстракция доступа к данным в Domain
- **Unit of Work** — транзакционность через EF Core DbContext
- **Result<T>** — явная обработка ошибок без исключений в логике
- **Domain Events** — слабая связь между агрегатами
- **Specification** — переиспользуемые запросы к БД
- **Factory** — сложная инициализация агрегатов

---

## 4. Структура решения

```
TTRPGHub.sln
│
├── src/
│   ├── Core/
│   │   ├── TTRPGHub.Domain/          # Сущности, Value Objects, интерфейсы репозиториев, Domain Events
│   │   └── TTRPGHub.Application/     # Use Cases (CQRS), DTO, валидация, маппинг, интерфейсы сервисов
│   │
│   ├── Infrastructure/
│   │   ├── TTRPGHub.Infrastructure/  # EF Core, репозитории, внешние сервисы, кеш, email
│   │   └── TTRPGHub.Persistence/     # Миграции, конфигурации EF, seed data
│   │
│   ├── Presentation/
│   │   ├── TTRPGHub.API/             # ASP.NET Core Web API, контроллеры/minimal api, middleware, DI
│   │   └── TTRPGHub.Web/             # Blazor WebAssembly, компоненты, страницы, CSS
│   │
│   └── Shared/
│       └── TTRPGHub.Contracts/       # Shared DTOs, API contracts, константы (между API и Web)
│
├── aspire/
│   ├── TTRPGHub.AppHost/             # [Фаза 1] Aspire оркестратор — описывает все сервисы и ресурсы
│   └── TTRPGHub.ServiceDefaults/     # [MVP] Общий OpenTelemetry, health checks, resilience extensions
│
├── tests/
│   ├── TTRPGHub.Domain.Tests/        # Unit тесты доменной логики
│   ├── TTRPGHub.Application.Tests/   # Unit тесты хэндлеров (с моками)
│   ├── TTRPGHub.Infrastructure.Tests/# Интеграционные тесты (реальная БД, Testcontainers)
│   └── TTRPGHub.API.Tests/           # E2E тесты API (WebApplicationFactory)
│
└── docs/
    ├── PROJECT.md                    # Этот файл
    ├── architecture/                 # ADR (Architecture Decision Records)
    └── api/                          # OpenAPI спецификации
```

### Правило зависимостей (строго)
```
Domain       — зависит от: ничего
Application  — зависит от: Domain
Infrastructure — зависит от: Application, Domain
Persistence  — зависит от: Application, Domain
API          — зависит от: Application, Infrastructure, Persistence
Web          — зависит от: Contracts
Contracts    — зависит от: ничего
```

---

## 5. Технологический стек

### Backend
| Категория | Технология | Версия |
|-----------|-----------|--------|
| Платформа | .NET | 10 (LTS) |
| Язык | C# | 14 |
| Web API | ASP.NET Core Minimal API | 10 |
| ORM | Entity Framework Core | 10 |
| CQRS | MediatR | 12+ |
| Валидация | FluentValidation | 11+ |
| Маппинг | Mapster | 7+ |
| Логирование | Serilog + Seq (dev) | — |
| Observability | OpenTelemetry | — |
| Real-time | SignalR | встроен |
| Auth | ASP.NET Core Identity + JWT Bearer | — |
| Документация API | Scalar / Swashbuckle | — |
| Health Checks | AspNetCore.HealthChecks | — |

### Frontend
| Категория | Технология | Обоснование |
|-----------|-----------|-------------|
| Фреймворк | Blazor WebAssembly (.NET 10) | Полный .NET стек, C# на фронте |
| UI компоненты | Blazor.Bootstrap 3.x | Bootstrap 5 компоненты для Blazor, минимум кастомного CSS |
| CSS фреймворк | Bootstrap 5 | Индустриальный стандарт, responsive grid, утилиты |
| HTTP клиент | HttpClient + Refit | Типизированные API-клиенты из интерфейсов |
| State management | Fluxor | Flux/Redux для Blazor, предсказуемый стейт |

> Bootstrap 5 берёт на себя всю тяжёлую работу по вёрстке: сетка, responsive breakpoints, spacing, типографика.
> Кастомный CSS — только точечные переопределения темы через CSS-переменные Bootstrap (`--bs-primary`, `--bs-body-bg` и т.д.).

### База данных и инфраструктура
| Категория | Технология | Назначение |
|-----------|-----------|------------|
| БД | PostgreSQL 16 | Основное хранилище |
| Кеш | Redis | Кеш запросов, сессии, rate limiting |
| Поиск | PostgreSQL FTS (MVP) → Meilisearch | Полнотекстовый поиск |
| Хранилище файлов | MinIO (self-hosted S3) | Аватары, изображения |
| Очередь | Hangfire → RabbitMQ | Фоновые задачи, email |

### DevOps (целевое)
| Категория | Технология | Фаза |
|-----------|-----------|------|
| Контейнеризация | Docker + Docker Compose | MVP |
| Оркестрация | Docker Compose → Kubernetes | Фаза 1+ |
| Dev-оркестрация | .NET Aspire AppHost | Фаза 1 (когда 2+ сервиса) |
| CI/CD | GitHub Actions | MVP |
| Реверс-прокси | nginx | MVP |
| Мониторинг | Grafana + Prometheus | Фаза 1 |
| Трейсинг | Jaeger / Tempo | Фаза 1 |

### .NET Aspire — решение

**MVP:** не используем. Docker Compose проще и прозрачнее для одного API + Postgres + Redis.

**Фаза 1 (публичный запуск):** добавляем Aspire, когда появляется второй процесс (Worker для email/фоновых задач).
Что получаем: единый dev-dashboard с логами/трейсами всех сервисов, zero-config OpenTelemetry, 
автоматический запуск контейнеров (Postgres, Redis, MinIO) без Docker Compose YAML.

**Заготовка уже сейчас:** проект `TTRPGHub.ServiceDefaults` создаём сразу — он содержит
OpenTelemetry конфиг и health check extensions. Стоит 0 усилий сейчас, подключается к Aspire в фазе 1 одной строкой.

```
TTRPGHub.sln (добавить к структуре из раздела 4)
├── src/
│   └── Aspire/
│       ├── TTRPGHub.AppHost/          # Фаза 1: точка входа Aspire, описывает все сервисы
│       └── TTRPGHub.ServiceDefaults/  # MVP: общий OpenTelemetry, health checks, resilience
```

---

## 6. Дизайн-система и цветовая схема

### Выбранная схема: «Тёмная магия»
Пурпурно-фиолетовая палитра. Мистическая атмосфера, современный вид, выделяется на фоне всех существующих D&D ресурсов.
Тёмная тема — основная. Светлая — опциональная, переключается пользователем.

### Bootstrap 5 CSS переменные — переопределения темы

```css
/* _variables.scss — поверх Bootstrap 5 */

/* ── Тёмная тема (основная) ───────────────────────── */
[data-bs-theme="dark"] {
  --bs-body-bg:          #0E0A1A;   /* фон страницы */
  --bs-body-bg-surface:  #1A1230;   /* карточки, панели */
  --bs-border-color:     #2D1F52;   /* границы */

  --bs-body-color:       #EDE8FF;   /* основной текст */
  --bs-secondary-color:  #9B7FD4;   /* второстепенный текст, подписи */

  --bs-primary:          #8B6FD0;   /* акцент — кнопки, ссылки, активные эл-ты */
  --bs-primary-rgb:      139, 111, 208;
}

/* ── Светлая тема ─────────────────────────────────── */
[data-bs-theme="light"] {
  --bs-body-bg:          #F5F2FB;
  --bs-body-bg-surface:  #EAE4F7;
  --bs-border-color:     #C8B8EE;

  --bs-body-color:       #1C1030;
  --bs-secondary-color:  #5B3FA0;

  --bs-primary:          #6B4FA0;
  --bs-primary-rgb:      107, 79, 160;
}
```

### Токены (для использования в компонентах)

| Токен | Тёмная | Светлая | Назначение |
|-------|--------|---------|------------|
| `--bs-body-bg` | `#0E0A1A` | `#F5F2FB` | Фон страницы |
| `--bs-body-bg-surface` | `#1A1230` | `#EAE4F7` | Карточки, сайдбары, модалки |
| `--bs-border-color` | `#2D1F52` | `#C8B8EE` | Все границы |
| `--bs-body-color` | `#EDE8FF` | `#1C1030` | Основной текст |
| `--bs-secondary-color` | `#9B7FD4` | `#5B3FA0` | Подписи, метки, вторичный текст |
| `--bs-primary` | `#8B6FD0` | `#6B4FA0` | Акцент: кнопки, ссылки, badges |

### Правила применения
- Кастомный CSS — минимум. Всё через Bootstrap-классы (`bg-body`, `text-body-secondary`, `btn-primary` и т.д.)
- Цвет напрямую в разметке не пишем — только через CSS-переменные или Bootstrap утилиты
- Переключение темы: `document.documentElement.setAttribute('data-bs-theme', 'dark'|'light')`
- Preference по умолчанию берём из `prefers-color-scheme` медиазапроса

---

## 7. Доменная модель (MVP)

### Агрегаты

```
User (агрегат)
├── Id: UserId
├── Username: string
├── Email: Email (Value Object)
├── PasswordHash: string
├── CreatedAt: DateTime
├── Profile: UserProfile
│   ├── DisplayName: string
│   ├── AvatarUrl: string?
│   ├── Bio: string?
│   ├── City: string?
│   └── ExperienceLevel: ExperienceLevel (enum)
└── Characters: IReadOnlyList<CharacterId>

Character (агрегат)
├── Id: CharacterId
├── OwnerId: UserId
├── Name: string
├── Race: Race (Value Object)
├── Class: CharacterClass (Value Object)
├── Level: Level (Value Object, 1-20)
├── AbilityScores: AbilityScores (Value Object)
│   ├── Strength, Dexterity, Constitution
│   ├── Intelligence, Wisdom, Charisma
├── HitPoints: HitPoints (Value Object)
├── Background: Background (Value Object)
├── Skills: IReadOnlyList<SkillProficiency>
├── Equipment: IReadOnlyList<EquipmentItem>
├── SpellSlots: SpellSlots? (если каст. класс)
├── Notes: string?
└── IsPublic: bool

RuleEntry (агрегат — справочник)
├── Id: RuleEntryId
├── Slug: string (URL-friendly)
├── Category: RuleCategory (enum)
├── Title: string
├── Content: string (Markdown)
├── Tags: IReadOnlyList<string>
└── UpdatedAt: DateTime
```

### Value Objects
- `Email` — валидация формата
- `Level` — 1..20, бизнес-правила повышения
- `AbilityScores` — 6 характеристик, вычисляемые модификаторы
- `HitPoints` — текущие/максимум/временные
- `UserId`, `CharacterId` — strongly typed IDs (record struct)

---

## 8. API Design

**Стиль:** RESTful Minimal API с версионированием (`/api/v1/`)

### Endpoints MVP

```
Auth
  POST   /api/v1/auth/register
  POST   /api/v1/auth/login
  POST   /api/v1/auth/refresh
  DELETE /api/v1/auth/logout

Characters
  GET    /api/v1/characters              — мои персонажи
  POST   /api/v1/characters              — создать
  GET    /api/v1/characters/{id}         — просмотр
  PUT    /api/v1/characters/{id}         — обновить
  DELETE /api/v1/characters/{id}         — удалить
  GET    /api/v1/characters/{id}/sheet   — лист персонажа (PDF/HTML)

Rules
  GET    /api/v1/rules                   — список (с фильтрами + пагинация)
  GET    /api/v1/rules/{slug}            — конкретная запись

Users
  GET    /api/v1/users/me                — мой профиль
  PUT    /api/v1/users/me                — обновить профиль
```

### Стандарт ответов
```json
// Успех
{
  "data": { ... },
  "meta": { "page": 1, "pageSize": 20, "total": 150 }
}

// Ошибка
{
  "error": {
    "code": "CHARACTER_NOT_FOUND",
    "message": "Персонаж не найден",
    "details": []
  }
}
```

### Result<T> паттерн
```csharp
public sealed class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);
}
```

---

## 9. Ключевые технические решения

### Strongly Typed IDs
```csharp
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);
}
```
Исключает путаницу между `userId` и `characterId` на этапе компиляции.

### CQRS — пример команды
```csharp
// Command
public record CreateCharacterCommand(
    string Name,
    string Race,
    string Class,
    int Level,
    AbilityScoresDto AbilityScores
) : IRequest<Result<CharacterId>>;

// Handler
public class CreateCharacterHandler(
    ICharacterRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateCharacterCommand, Result<CharacterId>>
{
    public async Task<Result<CharacterId>> Handle(
        CreateCharacterCommand command,
        CancellationToken ct)
    {
        var character = Character.Create(
            currentUser.Id,
            command.Name,
            command.Race,
            command.Class,
            command.Level,
            command.AbilityScores.ToDomain());

        if (character.IsFailure)
            return Result<CharacterId>.Failure(character.Error!);

        await repository.AddAsync(character.Value!, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CharacterId>.Success(character.Value!.Id);
    }
}
```

### Specification pattern
```csharp
public class CharactersByOwnerSpec : Specification<Character>
{
    public CharactersByOwnerSpec(UserId ownerId) =>
        Query.Where(c => c.OwnerId == ownerId)
             .OrderByDescending(c => c.UpdatedAt);
}
```

### Pipeline Behaviours (MediatR)
Порядок выполнения для каждого запроса:
```
LoggingBehaviour → ValidationBehaviour → CachingBehaviour → Handler
```

### Кеширование
- Queries помечаются `ICacheableQuery` с TTL
- `CachingBehaviour` автоматически кеширует/инвалидирует через Redis
- Commands после выполнения инвалидируют связанные ключи

---

## 10. UI/UX принципы

### Дизайн-система
- **MudBlazor** как базовая компонентная библиотека
- Кастомная тема поверх MudBlazor (цвета, типографика, скругления)
- Тёмная тема как основная, светлая опционально
- Fantasy-атмосфера без китча: благородный, современный дизайн вдохновлённый D&D

### Responsive
- Mobile-first подход в CSS
- Breakpoints: 360px / 768px / 1024px / 1440px
- Touch-friendly элементы (min 44px tap targets)
- Лист персонажа на мобиле — вертикальный stack; на десктопе — двухколоночный layout

### Производительность
- Blazor WASM lazy loading по модулям
- Виртуализация длинных списков (`Virtualize` компонент)
- Debounce на поисковых полях
- Skeleton loaders вместо спиннеров
- PWA-манифест для установки на телефон

---

## 11. Масштабируемость (заложено с MVP)

| Аспект | Решение |
|--------|---------|
| БД | PostgreSQL с connection pooling (Npgsql + PgBouncer), read replicas в будущем |
| Кеш | Redis Cluster-ready конфигурация |
| Stateless API | JWT без серверных сессий → горизонтальное масштабирование |
| Фоновые задачи | Отдельный worker process (Hangfire / Hosted Service) |
| Поиск | Сначала PG FTS, потом Meilisearch без изменения API |
| Файлы | S3-совместимый MinIO → любой облачный S3 |
| Очередь | Сначала In-Memory / Hangfire, потом RabbitMQ |
| Multi-tenancy | Не нужна, но UserId везде — изоляция данных гарантирована |

---

## 12. Тестирование

### Стратегия
```
Unit Tests        — доменная логика, хэндлеры (с моками)
Integration Tests — репозитории, EF (Testcontainers + реальный Postgres)
API Tests         — E2E через WebApplicationFactory
```

### Правила
- Тест одного хэндлера = один файл
- Arrange / Act / Assert — явное разделение
- Названия: `Method_Condition_ExpectedResult`
- Никаких тестов на controllers напрямую — тестируем хэндлеры
- Minimum coverage цель: Domain 95%, Application 80%

---

## 13. Безопасность

- **HTTPS everywhere** — даже в dev (dev cert)
- **JWT** с коротким TTL (15 мин) + Refresh Token rotation
- **Rate limiting** на auth endpoints (ASP.NET Core + Redis)
- **FluentValidation** на всех командах — никакого raw input в домен
- **Strongly Typed IDs** — нет IDOR через угадывание числовых ID
- **CORS** — белый список origins
- **Content Security Policy** заголовки
- Никаких sensitive данных в логах (маскирование через Serilog Destructuring)

---

## 14. Дорожная карта

### Фаза 0 — MVP (личное использование)
- [ ] Solution structure + базовая инфраструктура
- [ ] Domain модели: User, Character, RuleEntry
- [ ] Auth: регистрация, вход, JWT
- [ ] Rules Reference: импорт SRD данных, просмотр, поиск
- [ ] Character Builder: создание и редактирование персонажа
- [ ] Базовый UI (MudBlazor тема, layout, навигация)
- [ ] Docker Compose для локалки (API + Web + Postgres + Redis)

### Фаза 1 — публичный запуск
- [ ] LFG: объявления, поиск, фильтры по городу/онлайн/системе
- [ ] Публичные профили пользователей
- [ ] Справочник заклинаний
- [ ] Справочник монстров
- [ ] Email уведомления (подтверждение регистрации)
- [ ] PWA
- [ ] Деплой (VPS + Docker + nginx)

### Фаза 2 — инструменты мастера
- [ ] Campaign Manager
- [ ] Session Notes
- [ ] Encounter Builder
- [ ] Initiative Tracker (SignalR, real-time)

### Фаза 3 — сообщество
- [ ] Forum / Discussions
- [ ] Homebrew Hub
- [ ] Reviews
- [ ] Events calendar

---

## 15. Следующий шаг (прямо сейчас)

1. Создать `TTRPGHub.sln` со структурой проектов из раздела 4
2. Настроить `Directory.Build.props` и `Directory.Packages.props` (Central Package Management)
3. Реализовать `TTRPGHub.Domain` — сущности MVP, Value Objects, интерфейсы
4. Реализовать `TTRPGHub.Application` — CQRS для Auth и Characters
5. Реализовать `TTRPGHub.Infrastructure` — EF Core, PostgreSQL, Identity
6. Реализовать `TTRPGHub.API` — Minimal API endpoints
7. Docker Compose для локальной разработки
8. `TTRPGHub.Web` — базовый Blazor WASM, тема MudBlazor, Auth flow

---

*Документ живёт вместе с кодом и обновляется при изменении решений.*  
*Версия: 1.3 | Дата: 2026-06-28*
