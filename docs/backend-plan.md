# แผนงาน Backend — ระบบ Claim ประกัน (.NET 10 + SQLite)

## สถานะ
| Phase | หัวข้อ | สถานะ |
|---|---|---|
| 0 | Scaffolding | ✅ เสร็จ |
| 1 | Domain model + state machine | ✅ เสร็จ |
| 2 | Persistence (EF Core + SQLite) | ✅ เสร็จ |
| 3 | Authentication / Authorization | ✅ เสร็จ |
| 4 | Claimant: แจ้งเหตุและติดตามสถานะ | ✅ เสร็จ |
| 5 | Staff: รับเคลมและประเมิน | ✅ เสร็จ |
| 6 | ตัดสินผลและจ่ายค่าสินไหม | ✅ เสร็จ |
| 7 | Supervisor dashboard | ✅ เสร็จ |
| 8 | เอกสาร (README) | ✅ เสร็จ |
| — | Kafka notification | ถ้ามีเวลา (ดูท้ายเอกสาร) |

## ข้อตกลงหลัก
- **Modular monolith** (Web API ตัวเดียว) ส่วน Kafka ทำเป็นงานเสริมถ้ามีเวลา (ดู "ถ้ามีเวลา")
- **Scope ให้พอดีกับงานส่ง test**: ทำเฉพาะที่โจทย์ขอ ตัดการแนบไฟล์ออก (claimant ตอบคำขอข้อมูลเป็นข้อความ)
- **เวลา**: timestamp เก็บและส่งเป็น UTC ให้ FE แปลงเป็นเวลาเครื่องเอง ส่วนกฎที่เกี่ยวกับ "วันนี้" ใช้ปฏิทิน `Asia/Bangkok` (`BusinessCalendar`)
- **Minimal APIs**: แยกไฟล์ endpoint ตาม feature และจัดกลุ่มด้วย `MapGroup`
- **JWT ที่ระบบออกเอง** แยก role ได้แก่ `Claimant`, `Staff`, `Supervisor`
- **Staff อนุมัติหรือปฏิเสธเคลมได้เองทุกเคส** (ไม่มีวงเงินอนุมัติ และไม่มีขั้นรอ Supervisor อนุมัติ)
- **Supervisor** ทำได้ทุกอย่างเหมือน Staff และดู dashboard ของทีมได้ว่าใครดูเคลมไหนอยู่ เคลมไหนยังไม่มีคนรับ และเคลมค้างที่สถานะไหน (ไม่ทำ report เชิงสถิติ/performance)
- **Setup ให้น้อยที่สุด**: config, package และ tooling จะเพิ่มเฉพาะใน phase ที่ได้ใช้จริง

## โครงสร้าง (Clean Architecture ตามแนวทางของ Microsoft)
```
backend/
  ClaimSystem.slnx, Directory.Build.props, global.json (เปิด MTP test runner)
  src/ClaimSystem.Domain          entities, state machine, domain events (ไม่มี dependency)
  src/ClaimSystem.Application     use cases แยกตาม feature + interfaces (ports)
  src/ClaimSystem.Infrastructure  EF Core SQLite, JWT, Kafka
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
- **InformationRequest**: Message, RequestedBy, DueDate, Status (Open/Responded), ResponseText, RespondedAt
- **ClaimStatusHistory**: audit trail ของทุก transition ใช้ทำ timeline และคำนวณ performance
- **Notification** (Phase 7)
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

### Phase 4 — Claimant: แจ้งเหตุและติดตามสถานะ ✅
- `GET /api/policies/mine`: policy ของฉันพร้อม IncidentType ที่คุ้มครอง (ให้ FE ทำ dropdown)
- `POST /api/claims` (201): policy ไม่มีได้ 404, ไม่ใช่ของตัวเองได้ 403, กฎอื่นผิดได้ 400
- `GET /api/claims/mine?status=&page=&pageSize=` (ใหม่สุดก่อน, pageSize สูงสุด 100), `GET /api/claims/{id}` (รายละเอียด + timeline + คำขอข้อมูล + ผลพิจารณา ไม่รวมบันทึกประเมินภายใน)
- `POST /api/claims/{id}/withdraw` (ได้เฉพาะตอน Submitted ไม่งั้นได้ 409)
- เคลมของคนอื่นตอบ **404** (ไม่ใช่ 403) เพื่อไม่บอกว่ามีเคลมนี้อยู่
- `BusinessCalendar`: "วันนี้" สำหรับกฎวันเกิดเหตุ/DueDate และปีของเลขเคลมใช้ปฏิทิน `Asia/Bangkok`
- Application: `ClaimantClaimService`, `PolicyService` + ports `IClaimRepository`, `IPolicyRepository`, `PagedResult<T>`
- Tests: unit (กฎ timezone) และ integration (policies, flow แจ้งเหตุ → รายการ → รายละเอียด, 403/400/404 ตอนแจ้ง, ownership 404, filter + paging, withdraw ซ้ำ 409, Staff เรียกได้ 403, ไม่มี token ได้ 401)
- *ปรับจากแผนเดิม*: ตัดการแนบไฟล์ (`ClaimDocument`, upload/download) ออก เพราะโจทย์ไม่ได้ขอ

### Phase 5 — Staff: รับเคลมและตรวจสอบ/ประเมิน ✅
- Endpoints `/api/staff/claims` (`StaffOrSupervisor`):
  - `GET /?queue=Unassigned|Mine&status=&page=&pageSize=` (ไม่ใส่ queue = ทั้งหมด, ใหม่สุดก่อน)
  - `GET /{id}`: รายละเอียดเต็มรวมข้อมูล claimant, policy และผลประเมิน
  - `POST /{id}/assign` (รับงานเอง), `POST /{id}/information-requests`, `POST /{id}/assessment`
  - ทุก action คืนรายละเอียดเคลมที่อัปเดตแล้ว
- Claimant ตอบเป็นข้อความผ่าน `POST /api/claims/{id}/information-requests/{reqId}/response` แล้วเคลมกลับเป็น UnderReview
- Staff ทุกคนดูเคลมได้ทุกใบ แต่ทำ action ได้เฉพาะคนที่รับงาน (domain ตอบ 403)
- `ConcurrencyExceptionHandler`: save ชนกัน (`DbUpdateConcurrencyException`) ได้ **409** `concurrency_conflict`
- Tests: flow รับงาน → ขอข้อมูล → claimant ตอบ → ประเมิน, queue Unassigned/Mine, supervisor ทำงานได้, รับงานซ้ำ 409, รับงานพร้อมกัน 2 คน (ได้ 200 + 409), staff ที่ไม่ได้รับงาน 403, ยอดเกิน 400, due date ย้อนหลัง 400, claimant อื่นตอบ 404, claimant เรียก staff endpoint 403

### Phase 6 — ตัดสินผลและจ่ายค่าสินไหม ✅
- `POST /api/staff/claims/{id}/approve` (`approvedAmount` ≤ ยอดประเมิน, `note` ไม่บังคับ) → Approved
- `POST /api/staff/claims/{id}/reject` (`reason` บังคับ, จาก UnderReview/Assessed) → Rejected
- `POST /api/staff/claims/{id}/pay` (`paymentReference` ที่ staff กรอก เพราะการโอนเงินเกิดนอกระบบ) → Paid
- Rejected และ Paid เป็นสถานะจบ ทำอะไรต่อไม่ได้ (409) และ claimant เห็นยอดอนุมัติ เหตุผล และเลขอ้างอิงการจ่ายใน `GET /api/claims/{id}`
- Tests: approve → pay แล้ว claimant เห็นผลครบ, reject แล้ว claimant เห็นเหตุผลและทำต่อไม่ได้, อนุมัติเกินยอดประเมิน 400, reject ไม่มีเหตุผล 400, pay ก่อนอนุมัติ 409, staff ที่ไม่ได้รับงาน 403

### Phase 7 — Supervisor dashboard ✅
`GET /api/supervisor/dashboard` (`SupervisorOnly`) คำนวณสดจากตาราง Claims + ClaimStatusHistory ไม่มีตารางเพิ่ม:
- `claimsByStatus`: จำนวนเคลมทุกสถานะ (สถานะที่ไม่มีเคลมได้ 0) ตอบว่าเคลมค้างอยู่ที่สถานะไหน
- `unassignedCount` + `oldestUnassigned` (10 ใบที่รอนานสุด) ตอบว่าเคลมไหนยังไม่มีคนรับ
- `staff[]`: staff/supervisor แต่ละคนมีงานที่ยังไม่จบกี่ใบ (`openClaims`, `openClaimsByStatus`) และเป็นใบไหน (`claims`) ตอบว่าใครดูเคลมไหนอยู่
- `longestInCurrentStatus`: 10 ใบที่ค้างในสถานะปัจจุบันนานที่สุด โดย `inStatusSince` = เวลาของ history ล่าสุด
- เคลมที่ "ยังไม่จบ" = ไม่ใช่ Paid/Rejected/Withdrawn (`ClaimStatus.IsFinal()`)
- ดึงเคลมที่ยังไม่จบมาจัดกลุ่มในหน่วยความจำ (`SupervisorDashboardService`) ซึ่งพอสำหรับขนาดทีม ถ้าข้อมูลโตมากค่อยย้ายไป SQL aggregate
- ดูรายละเอียดเคลมต่อได้ด้วย `GET /api/staff/claims/{id}` ที่มีอยู่แล้ว
- Tests: unit (`IsFinal` ตรงกับ state machine) และ integration (ตัวเลขเปลี่ยนตาม submit → assign → reject, ลำดับ list, มีเฉพาะเคลมที่ยังไม่จบ, Staff/Claimant ได้ 403)

### Phase 8 — เอกสาร ✅
- `README.md` (ภาษาอังกฤษสำหรับผู้ตรวจ): วิธีรัน, demo accounts, flow ทดลอง, state machine + กฎ, API overview, error codes, architecture, design decisions, สิ่งที่ไม่ทำ และ "ถ้ามีเวลา: Kafka"
- `ClaimSystem.Api.http` มี request ครบทุก endpoint (ทำมาตั้งแต่ Phase 3–7)

## ถ้ามีเวลา: แจ้งผลให้ลูกค้าผ่าน Kafka
ตอนนี้ claimant ต้องเปิดดูสถานะเอง (`GET /api/claims/{id}`) ถ้ามีเวลาจะทำการแจ้งเตือนแบบ async:
- ทุกครั้งที่เคลมเปลี่ยนสถานะ publish event `ClaimStatusChanged` ไป Kafka topic `claims.events` (key = ClaimId เพื่อรักษาลำดับต่อเคลม)
- Consumer สร้าง notification ให้ลูกค้า เช่น ถูกขอข้อมูลเพิ่ม, ผลอนุมัติ/ปฏิเสธ, **ชำระเงินแล้ว (พร้อมเลขอ้างอิง)** และ `GET /api/notifications`
- ไม่ต้อง realtime เพราะลูกค้าได้รับช้าไปไม่กี่วินาทีก็ไม่เป็นไร การแยกเป็น event ทำให้ API ไม่ต้องรอระบบแจ้งเตือน (email/SMS/push) และเพิ่มช่องทางใหม่ได้โดยไม่แก้ flow เคลม
- `docker-compose.yml` (Kafka KRaft + kafka-ui) และ fake publisher สำหรับ tests
- ถ้าจะให้ event ไม่หายเมื่อ Kafka ล่ม ให้ใช้ transactional outbox (เขียน event ลง DB ใน transaction เดียวกับเคลม แล้วค่อย publish)

## Verification (end-to-end)
1. `cd backend && dotnet build -warnaserror && dotnet test`
2. `dotnet run --project src/ClaimSystem.Api` แล้วเปิด `/scalar` หรือใช้ `ClaimSystem.Api.http`
3. ไล่ flow: แจ้งเหตุ → รับงาน → ขอข้อมูล → ตอบ → ประเมิน → อนุมัติ → จ่าย → claimant เห็น Paid → supervisor ดู dashboard
