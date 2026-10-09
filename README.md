# PixelVault

PixelVault is a fictional online video game shop, built as a system under test for SDET training. It has a React
frontend and six ASP.NET Core services. Each service uses controller → service → repository layering, and the
service layers contain business rules written to be unit tested, mocked and mutation tested.

No real money, cards or orders are involved.

## Getting started

- [ ] **Install the [.NET 10 SDK](https://dotnet.microsoft.com/download)** (10.0.100 or newer). Check with `dotnet --version`.
- [ ] **Install [Node.js 22 LTS](https://nodejs.org/)** (22.13 or newer). Check with `node --version`.
- [ ] **Make sure these ports are free:** 5101-5106 (services), 5173 (shop), 15888, 16175 and 17037 (Aspire).
- [ ] **On a corporate network:** make sure nuget.org and the npm registry can be reached, directly or through your proxy.
- [ ] **Start everything** from the repository root:

  ```bash
  dotnet run --project src/PixelVault.AppHost
  ```

  This restores the NuGet packages, builds the solution, runs `npm install` for the React app and starts all six
  services through [Aspire](https://aspire.dev). The first run takes a few minutes; after that it starts in seconds.
- [ ] **Open the shop** at http://localhost:5173. The Aspire dashboard (logs, traces and health of every service)
  is at http://localhost:15888.
- [ ] **Optional, for VS Code:** install the recommended extensions (C# Dev Kit) when VS Code offers them.

Press `Ctrl+C` to stop everything.

You don't need Docker, a database server, Aspire templates or workloads, or an HTTPS certificate. Every service
stores its data in its own SQLite file.

## Other ways to run the app

### From VS Code

Install the recommended extensions (C# Dev Kit), then pick a configuration in the *Run and Debug* view:

- **PixelVault (Aspire)** runs the AppHost, the same as the command above.
- **All services (no Aspire)** starts the six APIs and the React app directly, with the debugger attached to every API.
- Each API also has its own configuration, for debugging one service at a time. These don't build first: run
  the `build` task (`Ctrl+Shift+B`) after changing code.

### Without Aspire

Every service also runs on its own with `dotnet run` from its folder. The React app runs with `npm install` and
`npm run dev` in `src/web`. The ports are fixed, so the services find each other either way.

## Services

The browser calls each service directly, so you can see in the browser's Network tab which service handles each call.
The calls between services (such as Orders calling Payment) happen on the server. They show up as traces in
the Aspire dashboard.

| Service | URL | Swagger UI | Does |
|---|---|---|---|
| Catalog | http://localhost:5101 | http://localhost:5101/swagger | Games, filtering, sale prices, stock, stock reservations |
| Basket | http://localhost:5102 | http://localhost:5102/swagger | Baskets, quantity limits, bundle deal, promo codes |
| Orders | http://localhost:5103 | http://localhost:5103/swagger | Checkout, sales tax, age check, order lifecycle, admin |
| Payment | http://localhost:5104 | http://localhost:5104/swagger | Fake payment provider: card checks, fraud score, refunds |
| Shipping | http://localhost:5105 | http://localhost:5105/swagger | Address validation, shipping cost, delivery dates |
| Identity | http://localhost:5106 | http://localhost:5106/swagger | Staff login (JWT), account lockout |

```
                        React app (:5173)
                               │  (direct calls from the browser)
     ┌──────────┬──────────┬───┴──────┬──────────┬──────────┐
  Catalog    Basket     Orders     Payment   Shipping   Identity
     ▲          │         │ │ │ │
     └──────────┘         │ │ │ └──► Shipping  (check the quote)
     (prices, stock)      │ │ └────► Payment   (charge, refund)
                          │ └──────► Catalog   (reserve, commit, release stock)
                          └────────► Basket    (get, close)
```

Every service follows the same structure:

```
Controllers/     HTTP only: binding, status codes. No business logic.
Services/        Business logic. One class per rule set, each behind an interface.
Repositories/    Data access with EF Core, behind interfaces.
Clients/         Typed HTTP clients for other services, behind interfaces.
Domain/          Entities and enums.
Dtos/            Request and response types.
Infrastructure/  DbContext and seed data.
```

Business rules throw exceptions such as `ConflictException("OUT_OF_STOCK", ...)`. These become
[problem details](https://www.rfc-editor.org/rfc/rfc9457) responses with a machine-readable `code`:

```json
{ "status": 409, "title": "Conflict", "detail": "'Starfall Tactics' is out of stock.", "code": "OUT_OF_STOCK" }
```

Validation errors are a `400` with an `errors` object per field.

## Business rules

Every class below documents its rules in its XML doc comment.

| Service | Class | Rules |
|---|---|---|
| Catalog | `PricingCalculator` | Sale window (inclusive start, exclusive end), 1-90% discount, rounding, $4.99 price floor |
| Catalog | `StockStatusResolver` | Pre-order / out of stock / low stock (1-5) / in stock |
| Catalog | `GameCatalogService` | Filter validation, category/platform/price/sale/stock filters, search, sort, paging |
| Catalog | `StockService` | Stock adjustments: reasons and directions, notes, 0-9999 limits, audit log |
| Catalog | `ReservationService` | All-or-nothing reservations, pre-orders, idempotency, commit and release |
| Basket | `BasketRules` | 5 per game, 2 per pre-order, 10 per basket, stock limits, 7 day expiry |
| Basket | `BundleDiscountCalculator` | Buy 3 in one category, cheapest 50% off (per 3 units, pre-orders excluded) |
| Basket | `PromoCodeEvaluator` | Active, date window, usage limit, category restriction, minimum spend |
| Basket | `BasketPricingCalculator` | Best of bundle or promo (no stacking), price change and stock issue flags |
| Shipping | `AddressValidator` | US ZIP and Canadian postal code formats, ZIP/postal code must match state/province |
| Shipping | `ShippingCostCalculator` | Zones, methods, extra items surcharge, free shipping at $50 |
| Shipping | `DeliveryDateEstimator` | Business days, 2 PM Eastern cutoff, pre-order release dates |
| Shipping | `UsFederalHolidayCalendar` | 11 federal holidays with observed-date rules |
| Payment | `CardValidator` | Luhn check, brand detection, CVV length per brand, expiry, cardholder name |
| Payment | `FraudScorer` | Weighted fraud rules, declined at 70 points |
| Payment | `PaymentService` | Idempotency keys, amount limits, declines, refund limits |
| Orders | `CheckoutService` | Checkout orchestration across four services, with compensation when payment fails |
| Orders | `SalesTaxCalculator` | Rates per state, shipping taxable in some states, no tax for Canada |
| Orders | `AgeVerificationPolicy` | 17+ for M and 18+ for AO, age calculation including February 29 birthdays |
| Orders | `CustomerDetailsValidator` | Name, email, North American phone numbers |
| Orders | `OrderStateMachine` | Allowed status changes |
| Orders | `CancellationPolicy` | Customer cancellation within 24 hours, refunds within 30 days of delivery |
| Orders | `AdminOrderService` | Tracking numbers, notes, no shipping before a pre-order is released |
| Identity | `AuthService` | Lockout after 5 failed attempts for 15 minutes, disabled accounts |

Time always comes from an injected `TimeProvider`, randomness (order numbers) from `IOrderNumberGenerator`, and
other services from client interfaces. Tests can control all of them.

## Test data

The databases are recreated with seed data every time a service starts. Dates (sales, releases, orders) are relative
to the current date, so the same situations exist whenever you start the app.

### Games

20 fictional games. Some were picked for specific situations:

| Game | Situation |
|---|---|
| Starfall Tactics, Rally Kings: Gravel | Out of stock |
| Farmstead Valley (1), Hollow Lantern: Uncut (3), Lumen Garden (exactly 5) | Low stock |
| Whispering Pines (6) | Just above the low stock threshold |
| Echoes of the Void | Pre-order, released 45 days from now |
| Neon Drift 2088, Deepwater Station | On sale |
| Sky Courier | Sale ends at midnight UTC tonight |
| Kingdoms of Brass | Sale starts in 3 days |
| Goalrush 26 | Sale ended yesterday |
| Mechanica Arena | 60% sale, stopped by the $4.99 price floor |
| Hollow Lantern: Uncut | Rated AO (18+) |
| Ashes of Valdren, Deepwater Station, Shadow Protocol, Echoes of the Void | Rated M (17+) |

### Promo codes

| Code | Rule |
|---|---|
| `WELCOME10` | 10% off |
| `SAVE5` | $5 off, minimum spend $30 |
| `BIGSPENDER` | $20 off, minimum spend $150 |
| `RPGFAN` | 20% off RPG games only |
| `ONEUSE` | 15% off, can be used for one order only |
| `SPRING25` | Expired |
| `HOLIDAY30` | Not valid yet (starts in 30 days) |
| `STAFFONLY` | Deactivated |

### Test cards

Use any future expiry date and any cardholder name. The CVV is 3 digits, or 4 for Amex.

| Card number | Result |
|---|---|
| `4242 4242 4242 4242` | Approved (Visa) |
| `5555 5555 5555 4444` | Approved (Mastercard) |
| `3782 822463 10005` | Approved (Amex) |
| `6011 1111 1111 1117` | Approved (Discover) |
| `4000 0000 0000 0002` | Declined: `CARD_DECLINED` |
| `4000 0000 0000 9995` | Declined: `INSUFFICIENT_FUNDS` |
| `4000 0000 0000 0069` | Declined: `EXPIRED_CARD` |
| `4000 0000 0000 0127` | Declined: `INCORRECT_CVC` |
| `4100 0000 0000 0019` | Declined: `FRAUD_SUSPECTED` |
| `4000 0000 0000 0119` | Processor error (502), no money taken |
| `4000 0000 0000 0259` | Approved after a 5 second delay |

### Staff accounts

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin123!` | Admin: full access |
| `support` | `Support123!` | Support: read-only |
| `former.employee` | `Former123!` | Disabled account |

Get a token with `POST http://localhost:5106/auth/login` and send it as `Authorization: Bearer <token>`. The Swagger
pages have an *Authorize* button.

### Orders

15 historical orders with matching payments, covering every status. Among them:

- `...-P5L8N` was paid a few hours ago, so the customer can still cancel it (email `noah.miller@example.com`).
- `...-G3H6K` was paid just over 24 hours ago, so the customer can no longer cancel it.
- `...-F6G1H` contains a pre-order, so it cannot be shipped yet.
- `...-J8K3L` was delivered 25 days ago and can still be refunded. `...-K7M2Q` was delivered 38 days ago and can't.

The date in the order number is the order date, which depends on when you start the app. Search for the last five
characters on the admin orders page.

## Test support

These endpoints exist in the Development environment only (`TestSupport:Enabled`):

| Endpoint | Does |
|---|---|
| `POST /test-support/reset` (every service) | Deletes all data and restores the seed data |
| `GET/PUT http://localhost:5104/test-support/faults` | Makes the Payment service slow or unreliable, e.g. `{"failureRate": 0.5, "extraLatencyMilliseconds": 2000}` |

Set `Database:ResetOnStartup` to `false` in a service's `appsettings.json` to keep its data between restarts.

## Frontend

React 19, TypeScript and Vite, in `src/web`. For UI tests:

- Pages use semantic HTML, labels and ARIA roles, so role- and label-based locators work.
- Key elements have a `data-testid` attribute (`game-card`, `add-to-basket`, `basket-total`, `place-order`, ...).
- Form fields show errors in an element with `data-testid="<field id>-error"`.
- Error messages from the services have `data-testid` and `data-error-code` attributes (e.g. `PAYMENT_DECLINED`).
- Catalog filters are kept in the URL, e.g. http://localhost:5173/?category=Rpg&maxPrice=50&sort=price-asc.
- The basket id is stored in `localStorage` (`pv.basketId`). The admin token is stored in `sessionStorage`.

## Repository layout

```
PixelVault.slnx
src/
  PixelVault.AppHost/         Aspire: starts everything
  PixelVault.ServiceDefaults/ Aspire defaults: health checks, telemetry, service discovery, resilience
  PixelVault.Shared/          Error handling, JWT auth, CORS, OpenAPI, test support, shared seed orders
  PixelVault.Catalog.Api/
  PixelVault.Basket.Api/
  PixelVault.Orders.Api/
  PixelVault.Payment.Api/
  PixelVault.Shipping.Api/
  PixelVault.Identity.Api/
  web/                        React app
```
