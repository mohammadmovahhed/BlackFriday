# Black Friday Sale Reservation Service

موتور رزرو فروش بلک‌فرایدی با معماری Multi-Tenant. جلوگیری از Overselling با استفاده از Redis Lua Script اتمیک.

---

## 🚀 اجرا (Quick Start)

### پیش‌نیاز
- Docker Desktop (حداقل ۴GB RAM)
- .NET 10 SDK (فقط برای اجرای تست)

### دستور اجرا

```bash
docker-compose up --build
```

حدود ۶۰ ثانیه صبر کن تا SQL Server بالا بیاد. بعد:

| سرویس | آدرس |
|---|---|
| Swagger UI | https://localhost:7061/swagger |
| API (HTTP) | http://localhost:5000 |

### چی خودکار اتفاق میفته؟

1. Migration اجرا میشه → دیتابیس `BlackFriday` با ۴ جدول ساخته میشه
2. **Seed Data** خودکار اضافه میشه (idempotent — هر بار restart، دوباره اضافه نمیشه):
   - **۲ تا Tenant:** `tenant-electronics` و `tenant-fashion`
   - **۱۰ تا SKU** با قیمت‌های واقعی
   - **۳۰ تا Sale Event** (ترکیبی از Active, Future, Expired)
   - **۱۰۰ عدد موجودی** برای هر SKU
3. Redis counterها برای همه‌ی Sale Eventهای Active مقداردهی میشن

---

## 🧪 تست API

### ۱. گرفتن یه Sale Event فعال

اول از Swagger یا با curl یه ID بگیر. توی Seed، Sale Eventها با SKU `TV-65-4K` معمولاً فعالن.

### ۲. ساخت یه رزرو

```bash
curl -k -X POST https://localhost:7061/v1/reservations \
  -H "X-Api-Key: tenant-electronics" \
  -H "Content-Type: application/json" \
  -d '{
    "saleEventId": "<SALE_EVENT_ID>",
    "sku": "TV-65-4K",
    "quantity": 1,
    "customerId": "cust-1"
  }'
```

**موفق (200):**
```json
{
  "reservation_token": "abc123...",
  "status": "PENDING",
  "expires_at": "2026-10-14T15:30:00Z",
  "remaining_stock": 99
}
```

**تمام شده (409):**
```json
{ "error": "SOLD_OUT", "remaining_stock": 0 }
```

### ۳. اثبات Overselling نمی‌شه (مهم‌ترین بخش)

```bash
cd tools/ConcurrencyTest
dotnet run -- "https://localhost:7061" "<SALE_EVENT_ID>" 200 100
```

این دستور **۲۰۰ درخواست همزمان** می‌فرسته به یه Sale Event با **ظرفیت ۱۰۰**.

**خروجی مورد انتظار:**
```
Firing 200 concurrent reservations at capacity=100
--------------------------------------------------

--------------------------------------------------
200 OK (reserved):   100
409 Sold Out:        100
Other errors:        0
Expected reserved:   100
Elapsed:             850 ms
--------------------------------------------------
PASS - exactly 100 reserved, no oversell
```

اگه تعداد موفق **دقیقاً ۱۰۰** باشه → Overselling غیرممکنه ✅

---

## 🏗️ معماری

```
Client → API (.NET 10) → Redis (Lua atomic) → SQL Server
                              ↓
                        پاسخ سریع (200ms)
```

### چرا Redis برای شمارش موجودی؟

**مهم‌ترین تصمیم این پروژه.**

| روش | مشکل |
|---|---|
| SQL `SELECT ... FOR UPDATE` | قفل ردیف روی SKU داغ → throughput می‌خوره زمین |
| Mutex توی اپلیکیشن | تک‌نقطه‌ی شکست، scale نمی‌شه |
| **Redis Lua Script (انتخاب شده)** | اجرای single-threaded اتمیک → Race Condition غیرممکن |

هر کلید Redis با `t:{tenant_id}:promo:{promo_id}:sku:{sku}:remaining` نام‌گذاری میشه → **ایزولاسیون رایگان multi-tenant**.

---

## 📊 تریدآف‌های Consistency

| داده | Consistency | کهنگی مجاز | مکانیزم |
|---|---|---|---|
| **شمارنده موجودی** | Strong | صفر | Redis atomic Lua |
| **رکورد رزرو** | Strong در Redis، Eventual در SQL | < 1s | Write-after-decrement |
| **متادیتای Sale Event** | Eventual | ≤ 30s | Cache-aside (برنامه‌ریزی شده) |
| **وضعیت سفارش** | Eventual | ≤ 5s | Pipeline async (برنامه‌ریزی شده) |

**قانون:** Consistency فقط جایی مهمه که پول درگیره. کاهش موجودی باید Strong باشه. داده‌ی نمایشی می‌تونه کهنه باشه.

---

## 🔐 ایزولاسیون Multi-Tenant

| لایه | مکانیزم |
|---|---|
| **کلیدهای Redis** | پیشوند `t:{tenant_id}:...` |
| **کوئری‌های SQL** | `WHERE TenantId = @tenantId` (توی همه‌ی کوئری‌ها) |
| **احراز هویت API** | هدر `X-Api-Key` |
| **Rate Limiting** | برنامه‌ریزی شده: Token Bucket per tenant |

باگ توی یه Tenant **نمی‌تونه** روی Tenant دیگه اثر بذاره چون کلیدها فیزیکی جدا هستن.

---

## 📁 ساختار پروژه

```
BlackFriday/
├── BlackFriday.Domain/          # Entities, Enums, Tenant Context
├── BlackFriday.Infrastructure/  # EF Core, Redis, Repositories
├── BlackFriday.Api/             # REST API + Swagger + Seed
├── BlackFriday.Worker/          # Background pipeline (اسکلت)
├── BlackFriday.Scheduler/       # TTL expiry (اسکلت)
├── tools/ConcurrencyTest/       # تست همزمانی
├── docs/                        # ADR, Architecture
├── docker-compose.yml
└── README.md
```

---

## 🛠️ Tech Stack

| کامپوننت | انتخاب | توجیه |
|---|---|---|
| زبان | .NET 10 / C# | الزام پروژه |
| دیتابیس | SQL Server 2025 | الزام، ACID |
| کش/شمارنده | Redis 7 | Lua اتمیک، Latency زیر میلی‌ثانیه |
| ORM | EF Core | Migration + Type Safety |
| Container | Docker Compose | Deploy با یک دستور |

---

## 📋 فرضیات (Assumptions)

1. **TTL ۱۵ دقیقه‌ای** برای رزرو — استاندارد صنعت
2. هر Tenant **موجودی مستقل** داره
3. ظرفیت Sale Event **موقع ساخت ثابت** میشه
4. `HTTP 409` پاسخ استاندارد "Sold Out" هست
5. موجودی **per (sale_event, sku)** هست، نه global per SKU
6. هدر `X-Api-Key` شناسه‌ی Tenant رو حمل می‌کنه (احراز هویت واقعی خارج از scope)

---

## ⚠️ محدودیت‌ها و کارهای آینده

این تحویل، **الزام اصلی (جلوگیری از overselling)** رو در اولویت قرار داده.

### ✅ پیاده‌سازی شده
- رزرو اتمیک با Redis Lua (oversell-proof)
- Multi-tenant namespacing
- ساخت و استعلام Sale Event
- Persistence رزرو
- Seed Data خودکار
- Swagger/OpenAPI
- تست همزمانی (Concurrency Test)

### 🚧 طراحی شده ولی پیاده نشده
- **Pipeline پس‌زمینه:** ساخت سفارش، سینک انبار، ارسال اعلان (اسکلت Worker هست)
- **Retry + DLQ:** جاب‌های شکست‌خورده نباید گم بشن
- **Scheduler TTL:** آزادسازی خودکار رزروهای منقضی
- **Idempotency Keys:** جلوگیری از درخواست تکراری
- **Rate Limiting per Tenant**
- **کش متادیتای Sale Event**
- **OpenTelemetry + Prometheus**

### 🎯 اگه وقت بیشتری داشتم
1. Pipeline کامل Outbox + Redis Streams (بزرگ‌ترین gap)
2. Integration Test با `WebApplicationFactory`
3. Chaos Test (Redis failover، Worker crash)
4. ابزار DLQ Replay

---