# แผนงาน Frontend — ระบบ Claim ประกัน (Angular 22)

## สถานะ
| Phase | หัวข้อ | สถานะ |
|---|---|---|
| F0 | Scaffold + types จาก OpenAPI | ✅ เสร็จ |
| F1 | Auth + layout + ของที่ใช้ร่วมกัน | ⏳ ถัดไป |
| F2 | Claimant | — |
| F3 | Staff (Supervisor ใช้ร่วม) | — |
| F4 | Supervisor dashboard | — |
| F5 | ตรวจรอบสุดท้าย + เอกสาร | — |

## เป้าหมาย
หน้าเว็บที่ **ใช้งาน API ได้จริงครบทุก role** ไม่เน้นความสวย
- **Pico.css**: HTML ธรรมดาก็ดูเรียบร้อยและอ่านง่าย (classless)
- **Vitest unit tests**: runner ค่าเริ่มต้นของ Angular 22
- ส่วน end-to-end ทดสอบเองกับ backend จริง

กฎการเขียน Angular อยู่ใน `frontend/CLAUDE.md` และใช้ Angular CLI MCP (`.mcp.json`) ค้นเอกสารและรัน build/test

## หลักการ
- **Component/state:** standalone (ค่าเริ่มต้น), zoneless, signals / `computed()`, `input()` / `output()`, control flow `@if` / `@for`, ไม่ใช้ `CommonModule`
- **อ่านข้อมูล:** ใช้ `httpResource` (ได้ signal พร้อม loading/error)
- **เขียนข้อมูล:** ใช้ `HttpClient` ผ่าน service ที่ประกาศด้วย `@Service()` + `inject()`
- **ฟอร์ม:** **Signal Forms** (`@angular/forms/signals`)
- **Route:** lazy load แยกตาม role, guard แบบ functional (`CanActivateFn`), interceptor แบบ functional (`withInterceptors`)
- **เรียก API:** ใช้ URL แบบ relative `/api/...` ผ่าน dev-server proxy (`proxy.conf.json`) ไปที่ `http://localhost:5106` ไม่ต้องมีไฟล์ environment และไม่ติด CORS
- **Types:** generate จาก OpenAPI ของ backend ด้วย `openapi-typescript`
  - `npm run gen:api` (ต้องรัน backend อยู่) → `src/app/core/api/schema.d.ts` (commit ไว้ ห้ามแก้มือ)
  - `core/api/models.ts` ตั้งชื่อสั้นให้ เช่น `type ClaimDetail = Schemas['ClaimDetailResponse']`
  - เมื่อ DTO ฝั่ง backend เปลี่ยน ให้ gen ใหม่ แล้ว `ng build` จะชี้จุดที่ไม่ตรง
- **เงิน:** API เป็นสตางค์
  - แสดงผลผ่าน pipe `thb` (`4500000` → `฿45,000.00`)
  - ฟอร์มรับเป็นบาทแล้วแปลงเป็นสตางค์ก่อนส่ง
- **เวลา:** API ส่ง UTC (`...Z`) แล้วใช้ `DatePipe` แสดงเป็นเวลาเครื่องผู้ใช้อัตโนมัติ
- **Error:** อ่าน ProblemDetails (`detail`) แล้วแสดงในกล่อง error ที่มี `role="alert"`
- **Accessibility:**
  - ทุก input มี `<label>`
  - ใช้ HTML ที่มีความหมาย (`nav`, `main`, `table`)
  - ย้าย focus ไปที่หัวข้อหน้าเมื่อเปลี่ยน route
  - ปุ่มที่ทำไม่ได้ซ่อนหรือ disable พร้อมบอกเหตุผล
- **Gate ทุก phase:** `ng build` และ `ng test` ผ่าน (MCP `run_target` หรือ `npm run build` / `npm test` ใน `frontend/`) จากนั้นหยุดให้ review

## โครงสร้าง
```
frontend/
  proxy.conf.json                /api → http://localhost:5106
  src/styles.css                 ปรับเล็กน้อย (Pico โหลดผ่าน angular.json)
  src/app/
    app.config.ts, app.routes.ts
    core/
      api/schema.d.ts            generate จาก /openapi/v1.json (ห้ามแก้มือ)
      api/models.ts              ชื่อสั้นของ types ที่ใช้บ่อย
      auth/                      auth-store (token + user ใน signal + localStorage), auth.interceptor, guards
      ui/                        thb pipe, status-badge, error-alert, problem-details helper
    layout/shell.ts              header + nav ตาม role + logout
    auth/                        login, register
    claimant/                    my-claims, new-claim, claim-detail
    staff/                       queue, staff-claim-detail (+ action panels)
    supervisor/                  dashboard
```

## Phases

### F0 — Scaffold ✅
- **สร้างโปรเจกต์:** `ng new claim-web --directory frontend --routing --style=css --ssr=false --zoneless --skip-git --ai-config=none` (Angular 22.3, TypeScript 6, Vitest)
- **ลบไฟล์ที่ generate มาแต่ไม่ใช้:**
  - `frontend/README.md`: วิธีรันอยู่ใน README หลัก
  - `frontend/.vscode/`: VS Code อ่าน settings จาก root
  - `app.html` / `app.css`: หน้า placeholder ซึ่งแทนด้วย shell แบบ inline template
- **Pico.css:** `@picocss/pico` โหลดผ่าน `angular.json` → `styles`
- **Proxy:** `proxy.conf.json` + `serve.options.proxyConfig`
- **Types จาก OpenAPI** (`npm run gen:api`):
  - ได้ types ที่ใช้ได้ทันที: ฟิลด์ required ถูก, nullable เป็น `null | T`, enum เป็น string union, จำนวนเงินเป็น `number`
  - `openapi-typescript` ยังประกาศ peer เป็น TypeScript 5 แต่ Angular 22 ใช้ TypeScript 6 จึง**ไม่ติดตั้งเป็น dependency** และรันผ่าน `npx -y openapi-typescript@7.13.0` (ล็อกเวอร์ชัน แยก cache ไม่กระทบโปรเจกต์)
- **แก้ backend:**
  - `ConfigureHttpJsonOptions` → `NumberHandling = Strict`: API รับเฉพาะตัวเลขจริง และ OpenAPI ประกาศเป็น `integer` ไม่ใช่ `integer | string`
  - **แก้บั๊ก**: body ที่ JSON ผิดรูป/ผิดชนิดเคยได้ **500** ใน Development (Minimal APIs throw `BadHttpRequestException`) ตอนนี้ได้ **400** ProblemDetails ผ่าน `ExceptionHandlerOptions.StatusCodeSelector`
  - เพิ่ม integration test 3 ตัว (ผ่าน 181/181)
- ✅ `ng build` ผ่าน, `ng test` ผ่าน, dev server เรียก `/api/auth/login` ผ่าน proxy ได้ 200

### F1 — Auth + layout + ของที่ใช้ร่วมกัน
- **Types:** `core/api/models.ts` ตั้งชื่อสั้นให้ types ที่ generate มา (AuthResponse, Claim*, Staff*, Dashboard, PagedResult, ClaimStatus ฯลฯ)
- **AuthStore:** login / register / logout, เก็บ token + user, มี `isLoggedIn` / `role` เป็น `computed`, และเคลียร์ token เมื่อหมดอายุ (`expiresAt`)
- **Interceptor:** แนบ `Authorization: Bearer` และเมื่อได้ 401 ให้ logout แล้วพาไปหน้า login
- **Guards:** `authGuard` และ `roleGuard(roles)`
- **หลัง login:** พาไปหน้าตาม role (Claimant → `/claims`, Staff → `/staff/claims`, Supervisor → `/supervisor`)
- **สิทธิ์ตาม route:**
  - `/claims/**` → Claimant
  - `/staff/**` → Staff **และ Supervisor** (ตรงกับ policy `StaffOrSupervisor` ของ backend)
  - `/supervisor` → Supervisor
- **Nav ตาม role:**
  - Claimant: "เคลมของฉัน", "แจ้งเหตุ"
  - Staff: "คิวงาน"
  - Supervisor: "Dashboard" + "คิวงาน" (ทำงานกับเคลมได้ทุกอย่างเหมือน staff)
- **หน้า:** login, register (Signal Forms + validation) และ shell ที่มี nav, ชื่อผู้ใช้ และปุ่ม logout
- **ของใช้ร่วม:** `thb` pipe, `StatusBadge`, `ErrorAlert`, helper อ่าน ProblemDetails
- **Tests:** AuthStore (HttpTestingController), interceptor, guards, thb pipe

### F2 — Claimant
- **`/claims` (เคลมของฉัน):** ตาราง + filter สถานะ + แบ่งหน้า ใช้ `httpResource` ตาม signal ของ filter/page
- **`/claims/new` (แจ้งเหตุ):**
  - เลือก policy จาก `GET /api/policies/mine` แล้ว dropdown IncidentType เปลี่ยนตาม policy (`linkedSignal`)
  - วันที่เกิดเหตุ, สถานที่, รายละเอียด และยอดเป็นบาท
  - error จาก backend (400/403) แสดงในกล่อง error
- **`/claims/:id` (รายละเอียด):** ข้อมูลเคลม, timeline, ผลพิจารณาและการจ่ายเงิน, คำขอข้อมูลที่ยังเปิดอยู่พร้อมฟอร์มตอบ และปุ่มยกเลิกเคลม (เฉพาะตอน Submitted)
- **Tests:** ClaimantApi service, ฟอร์มแจ้งเหตุ (แปลงบาท → สตางค์, incident types เปลี่ยนตาม policy)

### F3 — Staff (Supervisor ใช้หน้าเดียวกันและทำได้ทุก action)
Supervisor เข้าคิวงาน รับงาน ขอข้อมูล ประเมิน อนุมัติ ปฏิเสธ และจ่ายได้เหมือน staff ทุกอย่าง โดย backend อนุญาตอยู่แล้ว ฝั่ง frontend แค่ให้ guard และ nav รองรับ
- **`/staff/claims` (คิวงาน):** แท็บ ยังไม่มีคนรับ / ของฉัน / ทั้งหมด + filter สถานะ + แบ่งหน้า
- **`/staff/claims/:id` (รายละเอียด):** ข้อมูล claimant, policy, ผลประเมิน และ timeline พร้อม **แผง action ตามสถานะ**:
  - Submitted → ปุ่ม "รับงาน"
  - UnderReview (เป็นคนรับงาน) → ขอข้อมูลเพิ่ม (ข้อความ + due date), ประเมิน (ยอด + สรุป), ปฏิเสธ
  - Assessed → อนุมัติ (ยอด ≤ ยอดประเมิน + หมายเหตุ), ปฏิเสธ
  - Approved → บันทึกการจ่ายเงิน (เลขอ้างอิง)
  - ถ้าไม่ใช่คนรับงาน ให้แสดงว่าใครถืองานอยู่และไม่แสดงปุ่ม action
  - 409 (คนอื่นรับไปก่อน) แสดงข้อความแล้วโหลดข้อมูลใหม่
- **การเลือก action** อยู่ใน `computed()` ที่ test ได้แยกจาก template
- **Tests:** StaffApi service, ฟังก์ชันเลือก action ตามสถานะ/คนรับงาน, `roleGuard` ให้ Supervisor เข้า `/staff/**` ได้

### F4 — Supervisor dashboard
- **`/supervisor`:**
  - การ์ดจำนวนเคลมตามสถานะ
  - ตาราง "ยังไม่มีคนรับ (รอนานสุด)"
  - ตาราง "ค้างในสถานะนานสุด"
  - workload ของ staff แต่ละคน (จำนวนงานแยกสถานะ + รายการเคลม)
  - ทุกเลขเคลมกดไปหน้า `/staff/claims/:id` ได้
  - ปุ่ม refresh
- **Tests:** dashboard render จากข้อมูลจำลอง

### F5 — ตรวจรอบสุดท้าย + เอกสาร
- ไล่ flow ทั้งหมดกับ backend จริง และตรวจ a11y พื้นฐาน (label, focus, contrast ของ Pico)
- README: วิธีรัน frontend + backend พร้อมกัน, หน้าจอแต่ละ role
- อัปเดตตารางสถานะในไฟล์นี้

## Verification (end-to-end)
1. ทุก phase: `ng build` และ `ng test` ผ่าน
2. รัน backend (`dotnet run --project backend/src/ClaimSystem.Api`) และ frontend (`npm start` ใน `frontend/`) แล้วเปิด `http://localhost:4200`
3. ไล่ flow:
   1. claimant1 แจ้งเหตุ
   2. staff1 รับงานและขอข้อมูล
   3. claimant1 ตอบ
   4. staff1 ประเมิน, อนุมัติ และจ่าย
   5. claimant1 เห็น Paid
   6. supervisor เห็นตัวเลขบน dashboard แล้วกดเข้าเคลมจาก dashboard รับงานและอนุมัติเองได้
4. กรณี error: login ผิด, role ผิด URL (guard), อนุมัติเกินยอด (400 แสดงข้อความ), token หมดอายุ (กลับไปหน้า login)
