# แผนงาน Backend — ระบบ Claim ประกัน (.NET 10 + SQLite + Kafka)

## สถานะ
| Phase | หัวข้อ | สถานะ |
|---|---|---|
| 0 | Scaffolding | ✅ เสร็จ |
| 1 | Domain model + state machine | ✅ เสร็จ |
| 2 | Persistence (EF Core + SQLite) | ✅ เสร็จ |
| 3 | Authentication / Authorization | ✅ เสร็จ |
| 4 | Claimant: แจ้งเหตุและติดตามสถานะ | ⏳ ถัดไป |
| 5 | Staff: รับเคลมและประเมิน | — |
| 6 | ตัดสินผลและจ่ายค่าสินไหม | — |
| 7 | Kafka: Outbox + consumers | — |
| 8 | Reporting | — |
| 9 | Hardening + เอกสาร | — |

## ข้อตกลงหลัก
- **Modular monolith** (Web API ตัวเดียว) + **Kafka เป็น event bus** สำหรับ domain events (transactional outbox)
- **Minimal APIs**: แยกไฟล์ endpoint ตาม feature และจัดกลุ่มด้วย `MapGroup`
- **JWT ที่ระบบออกเอง** แยก role ได้แก่ `Claimant`, `Staff`, `Supervisor`
- **Staff อนุมัติหรือปฏิเสธเคลมได้เองทุกเคส** (ไม่มีวงเงินอนุมัติ และไม่มีขั้นรอ Supervisor อนุมัติ)
- **Supervisor** ทำได้ทุกอย่างเหมือน Staff และดู dashboard workload/performance ของ**ทั้งทีม**ได้ ส่วน Staff ทั่วไปดูได้เฉพาะของตัวเอง
- **Setup ให้น้อยที่สุด**: config, package และ tooling จะเพิ่มเฉพาะใน phase ที่ได้ใช้จริง

## โครงสร้าง (Clean Architecture ตามแนวทางของ Microsoft)
```
backend/
  ClaimSystem.slnx, Directory.Build.props, global.json (เปิด MTP test runner)
  src/ClaimSystem.Domain          entities, state machine, domain events (ไม่มี dependency)
  src/ClaimSystem.Application     use cases แยกตาม feature + interfaces (ports)
  src/ClaimSystem.Infrastructure  EF Core SQLite, JWT, Kafka, file storage
  src/ClaimSystem.Api             composition root + Minimal API endpoints
  tests/ClaimSystem.UnitTests          (สร้างใน Phase 1)
  tests/ClaimSystem.IntegrationTests   WebApplicationFactory
frontend/                          Angular 22 (ทำหลัง BE)
```

## Domain หลัก
- **User**: Id, Email, PasswordHash, FullName, Role
- **Policy** (seed data): PolicyNumber, `HolderId` (→ User.Id ของผู้ถือกรมธรรม์), ProductType (`Motor` | `Health` | `Property`), SumInsured, StartDate/EndDate
- **Claim**: ClaimNumber (`CLM-2026-000001`), `ClaimantId` (→ User.Id), PolicyId, IncidentType, IncidentDate, Location, Description, ClaimedAmount, AssessedAmount, AssessmentSummary, ApprovedAmount, Status, `AssignedStaffId` (→ User.Id), SubmittedAt/AssignedAt/DecidedAt/PaidAt, DecisionReason, PaymentReference, `Version` (concurrency token — เพิ่มใน Phase 2 เป็น shadow property ของ EF)
  - กฎ: `Policy.HolderId == ClaimantId` คือแจ้งเคลมได้เฉพาะกรมธรรม์ของตัวเอง
- **ClaimDocument**: ไฟล์แนบ (เก็บไฟล์บน disk และเก็บ metadata ใน DB)
- **InformationRequest**: Message, RequestedBy, DueDate, Status (Open/Responded), ResponseText, RespondedAt
- **ClaimStatusHistory**: audit trail ของทุก transition ใช้ทำ timeline และคำนวณ performance
- **Notification**, **OutboxMessage**, **ProcessedMessage** (Phase 7)
- ไม่มี entity `Payment` แยก เพราะหนึ่งเคลมจ่ายครั้งเดียว จึงเก็บ `PaymentReference` + `PaidAt` ไว้บน Claim

จำนวนเงินเก็บเป็น `long` หน่วยสตางค์ (SQLite ทำ SUM/ORDER BY กับ `decimal` ฝั่ง DB ไม่ได้)

### IncidentType ตาม ProductType
| ProductType | IncidentType |
|---|---|
| Motor | `Collision`, `Theft`, `Flood`, `Glass` |
| Health | `Illness`, `Accident`, `Hospitalization` |
| Property | `Fire`, `Flood`, `Burglary`, `WaterDamage` |

### Claim state machine
```
Submitted ──staff assign──▶ UnderReview ──requestInfo──▶ PendingInformation
                                ▲                               │
                                └──────claimant responds────────┘
UnderReview ──assess (AssessedAmount ≤ ClaimedAmount)──▶ Assessed
Assessed ──approve (ApprovedAmount ≤ AssessedAmount)──▶ Approved ──pay──▶ Paid
UnderReview / Assessed ──reject (ต้องระบุเหตุผล)──▶ Rejected
Submitted ──claimant withdraw──▶ Withdrawn
```
(ตอนแจ้งเหตุเช็ค `ClaimedAmount ≤ SumInsured` ไว้แล้ว จึงไม่มียอดไหนเกินทุนประกัน)

Transition ทั้งหมดอยู่ใน `Claim` aggregate ([Claim.cs](../backend/src/ClaimSystem.Domain/Claims/Claim.cs)) แต่ละเมธอดเช็คตามลำดับ **สถานะ → สิทธิ์ (เจ้าของ / staff ที่รับงาน) → ข้อมูล** แล้วบันทึก `ClaimStatusHistory`
ถ้าผิดกฎจะ throw `DomainException` พร้อม `Code`:

| Code | ความหมาย | HTTP |
|---|---|---|
| `claim.invalid_transition` | ทำ action นี้ในสถานะปัจจุบันไม่ได้ | 409 |
| `validation` | ข้อมูลผิดกฎ (จำนวนเงิน, วันที่, ประเภทเหตุ, ข้อความว่าง) | 400 |
| `not_allowed` | ไม่ใช่เจ้าของเคลม / ไม่ใช่ staff ที่รับงาน | 403 |
| `not_found` | ไม่พบ information request ที่อ้างถึง | 404 |
| `conflict` | ข้อมูลซ้ำ (เช่น email ที่สมัครไว้แล้ว) | 409 |

`DomainExceptionHandler` (Api) แปลง code เป็น ProblemDetails พร้อม field `code` เพื่อให้ frontend แยกกรณีได้

---

## กติกาการทำงานทุก phase
1. ทำทีละ phase และให้ผู้ใช้ review ก่อนเริ่ม phase ถัดไป
2. ทุก phase ต้องมี test ของตัวเอง
3. Gate ก่อนปิด phase (รันใน `backend/`): `dotnet build -warnaserror` และ `dotnet test` ต้องผ่านทั้งหมด
4. อัปเดตตารางสถานะด้านบน แล้ว commit แยกต่อ phase

## รายละเอียดแต่ละ Phase

### Phase 0 — Scaffolding ✅
- Solution `.slnx`, 4 src projects + integration test project, project references ตาม Clean Architecture
- Api: OpenAPI + Scalar (`/scalar`), `/health`, ProblemDetails, CORS สำหรับ `http://localhost:4200`
- `global.json` เปิด Microsoft.Testing.Platform (บังคับบน .NET 10 SDK สำหรับ xUnit v3)
- Tests: `GET /health` ได้ 200 และ route ที่ไม่มีอยู่ได้ 404 เป็น ProblemDetails
- *ปรับจากแผนเดิม*: ย้าย unit test project ไปสร้างใน Phase 1 และย้าย `docker-compose.yml` ไป Phase 7

### Phase 1 — Domain model และ state machine ✅
- `User`, `Policy`, `Claim` (+ `ClaimStatusHistory`, `InformationRequest`), mapping `IncidentTypes`, `DomainException`
- เวลาส่งเข้ามาเป็นพารามิเตอร์ `now` (UTC) เพื่อให้ test กำหนดเวลาได้ ส่วน Id ใช้ `Guid.CreateVersion7()` ซึ่งเรียงตามเวลาและ index ได้ดี
- Tests (`tests/ClaimSystem.UnitTests`): กฎตอนแจ้งเหตุ, flow ที่ถูกต้องทุกเส้น, matrix ของทุก action × ทุกสถานะที่ไม่อนุญาต, สิทธิ์, จำนวนเงิน และ mapping
- *ปรับจากแผนเดิม*: เลื่อน domain events (`AggregateRoot`/`IDomainEvent`) ไปทำใน Phase 7 พร้อม Kafka และตัด entity `Payment` ออก

### Phase 2 — Persistence (EF Core + SQLite) ✅
- `ClaimDbContext` + `IEntityTypeConfiguration` แยกไฟล์ต่อ entity (`Infrastructure/Persistence/Configurations`)
  - enum เก็บเป็นข้อความ, DateTime อ่านกลับเป็น UTC, Id ทุกตัวเป็น `ValueGeneratedNever` (สร้างใน domain)
  - `Version` เป็น shadow property ของ Claim: `SaveChanges` สุ่มค่าใหม่ทุกครั้ง ถ้าคนอื่น save ก่อนจะได้ `DbUpdateConcurrencyException`
- Migration `InitialCreate` (`Persistence/Migrations`) และตอน Development แอป migrate + seed เอง (`DatabaseInitializer`)
  - Seed: `claimant1` (Motor + Property), `claimant2` (Health), `staff1`, `staff2`, `supervisor` (`@example.com`) ยังไม่มีรหัสผ่าน (เพิ่มใน Phase 3)
- `IClaimNumberGenerator` → `CLM-{ปี}-{000001}` ใช้ตาราง `ClaimNumberCounters` (1 แถวต่อปี) เพิ่มค่าด้วย SQL statement เดียว จึงไม่ได้เลขซ้ำแม้เรียกพร้อมกัน
- DB อยู่ที่ `src/ClaimSystem.Api/claims.db` (path แบบ relative อ้างจากโฟลเดอร์ Api) และ integration tests ใช้ไฟล์เดียวกันนี้ (ไม่มี test DB แยก)
  - test ทุก class ใช้แอป instance เดียวกัน (`ApiCollection`) และแต่ละ test สร้างเคลมใหม่ของตัวเอง จึงรันซ้ำได้
  - ลบ `claims.db*` ได้ทุกเมื่อ แอปจะสร้างใหม่ตอนรันครั้งถัดไป
- Tests (`IntegrationTests/Persistence`): migration ครบ + model ตรงกับ migration, seed และ seed ซ้ำ, save/load aggregate พร้อม children, concurrency conflict, ClaimNumber ไม่ซ้ำ/แยกตามปี

### Phase 3 — Authentication และ Authorization ✅
- `POST /api/auth/register` (สร้าง Claimant เสมอ ส่ง `role` มาก็ไม่มีผล), `POST /api/auth/login`, `GET /api/auth/me`
- Application: `AuthService` + ports `IPasswordHasher`, `IAccessTokenIssuer`, `IUserRepository`, `IUnitOfWork` (Application ไม่อ้าง EF ตรงๆ)
- Infrastructure: `PasswordHasher<User>` ของ Identity (PBKDF2), JWT HS256 ผ่าน `JsonWebTokenHandler` + `AddJwtBearer`, claims `sub` / `role` / `name` / `email`
  - Config `Jwt` (Issuer, Audience, ExpiryMinutes ใน `appsettings.json` และ `SigningKey` ใน `appsettings.Development.json`) ตรวจตอน start ว่า key ยาวพอ (≥ 32 bytes) โดย production ต้องตั้ง `Jwt__SigningKey` ผ่าน env
- Api: `AuthorizationPolicies` (`ClaimantOnly`, `StaffOrSupervisor`, `SupervisorOnly`), `DomainExceptionHandler`, JSON enum เป็นข้อความ
- Seed accounts ใช้รหัสผ่าน `Password123!`
- Tests: register (สำเร็จ, แอบส่ง role, email ซ้ำแบบตัวพิมพ์ต่าง, input ผิด), login (seed ทุก role, หลัง register, รหัสผิด/ไม่มี user), `/me` (401 ไม่มี token, token ถูกแก้), policy × role ครบ 9 กรณี
- *ปรับจากแผนเดิม*: ไม่ทำ `ICurrentUser` แต่ endpoint อ่าน user id จาก `ClaimsPrincipal.GetUserId()` แล้วส่งเป็นพารามิเตอร์ให้ service เหมือนที่ domain รับ actor id อยู่แล้ว ส่วน 403 ผ่าน HTTP จะ test ใน Phase 4/5 เมื่อมี endpoint ที่จำกัด role

### Phase 4 — Claimant: แจ้งเหตุและติดตามสถานะ
- `GET /api/policies/mine`
- `POST /api/claims`: validate ความเป็นเจ้าของ policy, ช่วงคุ้มครอง, IncidentType และยอดไม่เกินทุนประกัน
  - ต้องแก้: "วันนี้" ที่ใช้เช็ควันเกิดเหตุ/DueDate ตอนนี้คิดจาก UTC ควรคิดตาม business timezone `Asia/Bangkok` (เวลาใน DB ยังเป็น UTC)
- `GET /api/claims/mine` (filter + pagination), `GET /api/claims/{id}` (รายละเอียด + timeline + ผลพิจารณา)
- `POST /api/claims/{id}/documents`, `GET .../documents/{docId}`, `POST /api/claims/{id}/withdraw`
- Tests: flow แจ้งเหตุ → ดูรายการ → ดูรายละเอียด และ ownership check

### Phase 5 — Staff: รับเคลมและตรวจสอบ/ประเมิน
- `GET /api/staff/claims` (queue: unassigned / assignedToMe, filter + pagination)
- `POST /api/staff/claims/{id}/assign` (ถ้ามีคนรับไปแล้วได้ 409), `GET /api/staff/claims/{id}`
- ขอข้อมูลเพิ่ม: `POST /api/staff/claims/{id}/information-requests` และ claimant ตอบผ่าน `POST /api/claims/{id}/information-requests/{reqId}/response`
- `POST /api/staff/claims/{id}/assessment`
- Tests: flow รับงาน → ขอข้อมูล → claimant ตอบ → ประเมิน

### Phase 6 — ตัดสินผลและจ่ายค่าสินไหม
- `POST /api/staff/claims/{id}/approve`, `.../reject` (ต้องมีเหตุผล), `.../pay` (สร้าง reference แบบ mock)
- Tests: happy path จนถึง Paid และ rejection path

### Phase 7 — Kafka: Outbox + Event consumers
- `docker-compose.yml`: Kafka (KRaft) + kafka-ui
- Domain events: เพิ่ม `AggregateRoot` (เก็บรายการ event) ให้ `Claim` และ raise `ClaimSubmitted`, `ClaimAssigned`, `InformationRequested`, `InformationProvided`, `ClaimAssessed`, `ClaimApproved`, `ClaimRejected`, `ClaimPaid`, `ClaimWithdrawn` ในแต่ละ transition
- SaveChanges interceptor เขียน `OutboxMessage`, `OutboxPublisher` publish ไป `claims.events` (key = ClaimId) ด้วย `Confluent.Kafka`
- `NotificationConsumer` (idempotent ด้วย `ProcessedMessage`), `GET /api/notifications`, `POST /api/notifications/{id}/read`
- `Kafka:Enabled=false` ใช้ in-process bus (ใช้ใน tests)
- Tests: outbox, idempotency, อนุมัติแล้ว claimant ได้ notification

### Phase 8 — Reporting
- `GET /api/reports/me` (Staff), `GET /api/reports/team/workload` และ `GET /api/reports/team/performance?from=&to=` (Supervisor)
- Tests: ตัวเลขจาก seed scenario ถูกต้อง

### Phase 9 — Hardening และเอกสาร
- Request validation, logging, ตรวจ error response ให้รูปแบบเดียวกัน
- `.http` file ครบทุก endpoint, README (วิธีรัน, seed accounts, event catalog)

## Verification (end-to-end)
1. `cd backend && dotnet build -warnaserror && dotnet test`
2. `docker compose up -d` แล้ว `dotnet run --project src/ClaimSystem.Api` และเปิด `/scalar`
3. ไล่ flow: แจ้งเหตุ → รับงาน → ขอข้อมูล → ตอบ → ประเมิน → อนุมัติ → จ่าย → claimant เห็น Paid และได้ notification → supervisor ดู dashboard
4. เช็ค kafka-ui ว่าใน topic `claims.events` มี event ครบตามลำดับ
