# ConferenceRoomBookingAPIv3

REST API для бронирования конференц-залов с поддержкой динамического ценоуправления, управления залами и построения отчётов.

## Содержание

- [Архитектура](#архитектура)
- [Технологии](#технологии)
- [Начало работы](#начало-работы)
- [API Endpoints](#api-endpoints)
- [Модель ценообразования](#модель-ценообразования)
- [Аутентификация и авторизация](#аутентификация-и-авторизация)
- [Обработка ошибок](#обработка-ошибок)
- [Тестирование](#тестирование)
- [База данных](#база-данных)

## Архитектура

Проект следует принципам Clean Architecture с разделением на слои:

```
┌─────────────────────────────────────────┐
│           Controllers (API)             │
│   ConferenceRooms, Bookings, Reports    │
├─────────────────────────────────────────┤
│         Application Services            │
│   BookingService, PricingService,      │
│   ReportService                         │
├─────────────────────────────────────────┤
│              Contracts                  │
│   Request/Response Models (DTO)         │
├─────────────────────────────────────────┤
│               Domain                    │
│   ConferenceRoom, Booking, RoomService  │
├─────────────────────────────────────────┤
│            Infrastructure              │
│   Persistence, Security, Middleware     │
└─────────────────────────────────────────┘
```

### Ключевые решения

- **Optimistic Concurrency**: SQL Server `rowversion` предотвращает lost update при одновременном PATCH одной комнаты (второй запрос получает 409 Conflict)
- **Pricing Windows**: Динамическое ценообразование по временным окнам с учётом часового пояса и DST
- **Snapshot Pattern**: Детали услуг копируются в бронирование на момент создания (`BookingServiceSnapshot`)
- **CancellationToken**: Пробрасывается через все слои для корректной отмены долгих операций

## Технологии

- **.NET 10** / C# 13
- **ASP.NET Core** — Web API
- **Entity Framework Core** — ORM
- **SQL Server** (опционально) — persistent storage
- **JWT Bearer** — аутентификация в production
- **Swagger/OpenAPI** — документация API (Development only)
- **xUnit** — тестирование
- **Rate Limiting** — 100 req/min per IP

## Начало работы

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (опционально, для persistent storage)

### Сборка и запуск

```bash
# Клонирование репозитория
git clone https://github.com/MikeElevan/ConferenceRoomBookingAPIv3.git
cd ConferenceRoomBookingAPIv3

# Сборка
dotnet build

# Запуск с InMemory хранилищем (по умолчанию)
dotnet run --project ConferenceRoomBookingAPIv3.csproj

# Запуск с SQL Server
# В appsettings.json установите "Persistence:Provider": "SqlServer"
# Убедитесь что строка подключения настроена и БД существует
```

API будет доступен по умолчанию на `https://localhost:5001` и `http://localhost:5000`.

Swagger UI: `https://localhost:5001/swagger`

### Конфигурация

Основные секции `appsettings.json`:

| Секция | Описание |
|--------|----------|
| `Persistence` | Провайдер хранилища (`InMemory` или `SqlServer`) и retry-стратегия |
| `Pricing` | Часовой пояс, границы окон и множители цен |
| `Security` | JWT Authority и Audience (для production) |
| `HttpLogging` | Логирование HTTP-запросов/ответов |
| `ConnectionStrings` | Строка подключения к SQL Server |

```json
{
  "Persistence": {
    "Provider": "InMemory",
    "ConnectionStringName": "DefaultConnection",
    "Retry": {
      "MaxRetryCount": 3,
      "MaxRetryDelaySeconds": 5
    }
  },
  "Pricing": {
    "TimeZoneId": "Europe/Kyiv",
    "MorningDiscountStartHour": 6,
    "StandardStartHour": 9,
    "PeakStartHour": 12,
    "PeakEndHour": 14,
    "StandardEndHour": 18,
    "EveningDiscountEndHour": 23,
    "MorningDiscountMultiplier": 0.90,
    "StandardMultiplier": 1.00,
    "PeakMultiplier": 1.15,
    "EveningDiscountMultiplier": 0.80,
    "NightMultiplier": 1.00
  }
}
```

## API Endpoints

Базовый URL: `/api/v1`

### Комнаты (Rooms)

| Метод | Путь | Описание | Роль |
|-------|------|----------|------|
| GET | `/rooms` | Список всех комнат | Any authenticated |
| GET | `/rooms/{id}` | Получить комнату по ID | Any authenticated |
| POST | `/rooms` | Создать новую комнату | Administrator |
| PATCH | `/rooms/{id}` | Частичное обновление комнаты | Administrator |
| DELETE | `/rooms/{id}` | Удалить комнату (нет активных бронирований) | Administrator |
| GET | `/rooms/available` | Поиск доступных комнат | Any authenticated |

**Примеры запросов:**

```bash
# Создание комнаты
POST /api/v1/rooms
{
  "name": "Зал А",
  "capacity": 20,
  "baseHourlyRate": 1500,
  "services": [
    { "name": "Проектор", "price": 500 },
    { "name": "Wi-Fi", "price": 100 }
  ]
}

# Частичное обновление (только указанные поля)
PATCH /api/v1/rooms/{id}
{
  "name": "Зал А Премиум",
  "capacity": 25
}

# Поиск доступных комнат
GET /api/v1/rooms/available?startsAt=2024-09-01T10:00:00Z&endsAt=2024-09-01T12:00:00Z&capacity=15
```

### Бронирования (Bookings)

| Метод | Путь | Описание | Роль |
|-------|------|----------|------|
| POST | `/bookings` | Создать бронирование | Any authenticated |

```bash
POST /api/v1/bookings
{
  "roomId": "guid",
  "startsAt": "2024-09-01T10:00:00Z",
  "durationMinutes": 120,
  "serviceIds": ["guid1", "guid2"]
}
```

### Отчёты (Reports)

| Метод | Путь | Описание | Роль |
|-------|------|----------|------|
| GET | `/reports/bookings` | Отчёт по бронированиям за период | Administrator, Manager |

```bash
GET /api/v1/reports/bookings?from=2024-09-01T00:00:00Z&to=2024-09-30T23:59:59Z
```

**Ответ содержит:**
- Общее количество бронирований и выручку
- Процент утилизации (utilization)
- Детализацию по залам
- Детализацию по услугам (сортировка по убыванию выручки)

## Модель ценообразования

Стоимость бронирования рассчитывается по часам с учётом временных окон:

| Окно | Часы (по умолчанию) | Множитель |
|------|---------------------|-----------|
| Morning Discount | 06:00 - 09:00 | 0.90 (-10%) |
| Standard | 09:00 - 12:00 | 1.00 |
| Peak | 12:00 - 14:00 | 1.15 (+15%) |
| Standard | 14:00 - 18:00 | 1.00 |
| Evening Discount | 18:00 - 23:00 | 0.80 (-20%) |
| Night | 23:00 - 06:00 | 1.00 |

**Пример расчёта:** Бронирование 11:00-13:00 при ставке 2000/час:
- 11:00-12:00 = 2000 × 1.00 = 2000
- 12:00-13:00 = 2000 × 1.15 = 2300
- **Итого: 4300**

Особенности:
- Учитывает переход на летнее/зимнее время (DST)
- Часовой пояс задаётся сервером (параметр `TimeZoneId`), не клиентом
- Границы окон настраиваются в `appsettings.json`

## Аутентификация и авторизация

| Окружение | Способ |
|-----------|--------|
| Development | Фейковый handler из `appsettings.Development.json` (роли настраиваются) |
| Testing | Тестовый handler с ролями Administrator, Manager |
| Production | JWT Bearer (Authority + Audience из конфигурации) |

**Политики авторизации:**

| Политика | Требуемые роли |
|----------|----------------|
| `Administrator` | Administrator |
| `Reporting` | Administrator или Manager |

## Обработка ошибок

API возвращает стандартизированные ошибки в формате:

```json
{
  "code": "ErrorCode",
  "message": "Human readable message"
}
```

| HTTP | Code | Описание |
|------|------|----------|
| 400 | `InvalidRequest` | Ошибка валидации входных данных |
| 401 | - | Не аутентифицирован |
| 403 | - | Недостаточно прав |
| 404 | `RoomNotFound` | Комната не найдена |
| 404 | `ServiceNotFound` | Услуга не найдена |
| 409 | `BookingConflict` | Конфликт бронирования (занято) |
| 409 | `ConcurrencyConflict` | Конкурентное изменение (lost update) |
| 409 | `RoomHasBookings` | Нельзя удалить комнату с бронированиями |
| 429 | - | Превышен лимит запросов |

## Тестирование

```bash
# Unit-тесты (бизнес-логика, без зависимостей)
dotnet test UnitTests/ConferenceRoomBookingAPIv3.UnitTests.csproj

# Интеграционные тесты (полный HTTP pipeline)
dotnet test Tests/ConferenceRoomBookingAPIv3.IntegrationTests.csproj

# Все тесты
dotnet test
```

**Покрытие:**
- `BookingServiceTests` — корректность создания бронирований и снимков услуг
- `PricingServiceTests` — расчёт стоимости, DST-переходы, разные оффсеты
- `ConferenceRoomsApiTests` — полный CRUD, валидация, бронирования
- `BookingsApiTests` — создание бронирований, конфликты

## База данных

### InMemory

По умолчанию. Данные хранятся в памяти и сбрасываются при перезапуске. Подходит для:
- Локальной разработки
- Интеграционных тестов

### SQL Server

Включение:
```json
{
  "Persistence": {
    "Provider": "SqlServer"
  }
}
```

Строка подключения:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ConferenceRoomBooking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Миграции

```bash
# Создать миграцию
dotnet ef migrations add MigrationName --project ConferenceRoomBookingAPIv3.csproj

# Применить миграции
dotnet ef database update --project ConferenceRoomBookingAPIv3.csproj
```

**Реализованные миграции:**
- `InitialCreate` — начальная схема
- `AddConferenceRoomRowVersion` — rowversion для optimistic concurrency