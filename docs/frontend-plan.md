# แผนงาน Frontend — ระบบ Claim ประกัน (Angular 22)

## สถานะ
| Phase | หัวข้อ | สถานะ |
|---|---|---|
| F0 | Scaffold + types จาก OpenAPI | ✅ เสร็จ |
| F1 | Auth + layout + ของที่ใช้ร่วมกัน | ✅ เสร็จ |
| F2 | Claimant | ✅ เสร็จ |
| F3 | Staff (Supervisor ใช้ร่วม) | ✅ เสร็จ |
| F4 | Supervisor dashboard | ✅ เสร็จ |
| F5 | ตรวจรอบสุดท้าย + เอกสาร | ✅ เสร็จ (รอผลทดสอบด้วยมือตาม checklist) |

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

### F1 — Auth + layout + ของที่ใช้ร่วมกัน ✅
**สรุปสิ่งที่ทำ**
- ข้อความบนหน้าจอเป็น **ภาษาอังกฤษ** (ผู้ใช้เลือก) เพื่อให้ตรงกับข้อความ error จาก backend
- **Shell (`app.ts`):**
  - nav ตาม role พร้อมชื่อผู้ใช้และปุ่ม Sign out
  - skip link (`Skip to main content`)
  - ย้าย focus ไปที่ `<h1>` ของหน้าใหม่หลังเปลี่ยน route
  - เมนูปัจจุบันมี `aria-current="page"`
- **Login / Register:** ใช้ Signal Forms (`form` + `[formRoot]` + `[formField]`)
  - validation ฝั่ง client ตรงกับ backend (email, รหัสผ่าน ≥ 8 ตัว)
  - error จาก backend แสดงใน `role="alert"`
  - login กลับไปหน้า `returnUrl` ได้ (เฉพาะ path ภายในแอป กัน open redirect) และบอกเมื่อ session หมดอายุ (`?expired=true`)
- **ของใช้ร่วม:** `AuthStore`, `authInterceptor`, `authGuard` / `roleGuard`, `ThbPipe` + `bahtToSatang`, `StatusBadge`, `ErrorAlert`, `FieldError` + `ariaInvalid`, `errorMessage` / `errorCode`
- Route ของ F2–F4 ใช้หน้า `ComingSoon` ชั่วคราว (มี guard ตาม role แล้ว)
- **Tests (Vitest 38 ตัว):**
  - money, problem
  - AuthStore (login/register/restore/หมดอายุ/logout)
  - interceptor (แนบ token, 401 → logout)
  - guards (ทุก role × ทุกกลุ่มหน้า)
  - หน้า Login (validation, สำเร็จ, error จาก server, returnUrl, กัน URL ภายนอก)
  - shell (nav ตาม role)
- ตรวจกับ backend จริง: dev server เปิด deep link ได้, proxy ส่ง login ได้ 200 และรหัสผิดได้ 401 พร้อม `detail`

**แผนเดิม**
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

### F2 — Claimant ✅
**สรุปสิ่งที่ทำ** (route lazy ใต้ `/claims` ใน `claimant/claimant.routes.ts`)
- **`/claims`:** ตาราง (เลขเคลมกดเข้าได้, ประเภทเหตุ+วันที่, ยอด, สถานะ, เวลาส่ง), filter สถานะ, ก่อนหน้า/ถัดไป, ข้อความเมื่อยังไม่มีเคลม
- **`/claims/new`:**
  - dropdown ประเภทเหตุถูกปิดจนกว่าจะเลือก policy แล้วแสดงเฉพาะประเภทที่ policy นั้นคุ้มครอง (เปลี่ยน policy จะล้างค่าที่เลือก)
  - เช็คฝั่ง client: วันที่ไม่อยู่ในอนาคต, ยอด > 0 และไม่เกินทุนประกัน
  - ส่งเป็นสตางค์ แล้วพาไปหน้ารายละเอียดเคลมใหม่
- **`/claims/:claimId`:**
  - กล่อง "We need more information" พร้อมฟอร์มตอบ (`AnswerRequest`) และคำตอบที่เคยให้ไป
  - ผลพิจารณา (ยอดอนุมัติ, เหตุผล/หมายเหตุ, เลขอ้างอิงการจ่าย), รายละเอียด, timeline
  - ปุ่ม withdraw แบบต้องยืนยัน (เฉพาะ Submitted) ถ้าล้มเหลวจะโหลดสถานะล่าสุดใหม่
- **ใช้ร่วมกับ F3:** `ClaimTimeline`, `IncidentTypePipe`, `todayIsoDate`
- **ไม่ได้ใช้ `linkedSignal` ตามแผน:** model ของ Signal Forms เป็น signal เดียว จึงใช้ `computed` (รายการประเภทเหตุ) + ล้างค่าเมื่อเปลี่ยน policy ซึ่งง่ายกว่า
- **Tests (รวม 58):**
  - ClaimantApi
  - labels
  - NewClaim: ประเภทตาม policy, ส่งเป็นสตางค์ + ไปหน้าเคลม, กันยอดเกินทุนประกัน, error จาก backend
  - MyClaims: แสดงแถว, filter กลับไปหน้า 1, ว่าง
  - ClaimDetail: แสดงข้อมูล, withdraw, ไม่มีปุ่ม withdraw หลังมีคนรับ, ตอบคำขอข้อมูล, ผลพิจารณา + การจ่าย

**แผนเดิม**
- **`/claims` (เคลมของฉัน):** ตาราง + filter สถานะ + แบ่งหน้า ใช้ `httpResource` ตาม signal ของ filter/page
- **`/claims/new` (แจ้งเหตุ):**
  - เลือก policy จาก `GET /api/policies/mine` แล้ว dropdown IncidentType เปลี่ยนตาม policy (`linkedSignal`)
  - วันที่เกิดเหตุ, สถานที่, รายละเอียด และยอดเป็นบาท
  - error จาก backend (400/403) แสดงในกล่อง error
- **`/claims/:id` (รายละเอียด):** ข้อมูลเคลม, timeline, ผลพิจารณาและการจ่ายเงิน, คำขอข้อมูลที่ยังเปิดอยู่พร้อมฟอร์มตอบ และปุ่มยกเลิกเคลม (เฉพาะตอน Submitted)
- **Tests:** ClaimantApi service, ฟอร์มแจ้งเหตุ (แปลงบาท → สตางค์, incident types เปลี่ยนตาม policy)

### F3 — Staff (Supervisor ใช้หน้าเดียวกันและทำได้ทุก action) ✅
**สรุปสิ่งที่ทำ** (route lazy ใต้ `/staff/claims` ใน `staff/staff.routes.ts`)
- **`/staff/claims` (Work queue):**
  - ปุ่มกลุ่ม Waiting to be picked up (ค่าเริ่มต้น) / My claims / All claims ใช้ `aria-pressed`
  - filter สถานะ + แบ่งหน้า
  - คอลัมน์ "Handled by" แสดง You / Another officer / Nobody yet
- **`/staff/claims/:claimId`:**
  - แผง Actions ตาม `availableActions(status, assignee, me)` ซึ่งเป็นฟังก์ชันล้วนที่ test ได้
    - ปุ่ม "Pick up this claim"
    - ฟอร์มใน `<details>`: ขอข้อมูล / ประเมิน / อนุมัติ / ปฏิเสธ / บันทึกการจ่าย (`staff/actions/*`)
  - ถ้าไม่มี action แสดงเหตุผล (ปิดแล้ว / คนอื่นถืองาน / รอ claimant)
  - ข้อมูล claimant, policy, เหตุการณ์, ผลประเมินและผลพิจารณา, คำขอข้อมูลทั้งหมด, timeline
- **ฟอร์มประเมินและอนุมัติ:**
  - เติมยอดเริ่มต้นจากยอดเคลม/ยอดประเมินด้วย `linkedSignal`
  - กันยอดเกินฝั่ง client
  - ส่งเป็นสตางค์
- **รับงานไม่สำเร็จ** (เช่นคนอื่นรับไปก่อน 409): แสดงข้อความจาก backend แล้วโหลดเคลมใหม่ให้เห็นว่าใครถืองาน
- **`Pager`** แยกเป็น component ใช้ร่วมกับ My claims (`model()` แบบ two-way)
- **Type ที่ generate มา:** `StaffClaimQueue` มี `null` (query parameter เป็น optional) จึงใช้ `NonNullable<>` สำหรับแท็บ
- **ข้อจำกัด:** API รายละเอียดเคลมส่งมาแค่ `assignedStaffId` ไม่มีชื่อ จึงแสดงเป็น "You" / "Another claims officer"
- **Tests (รวม 85):**
  - `availableActions` ทุกสถานะ
  - StaffApi ทั้ง 6 action
  - WorkQueue: แท็บส่ง queue ถูก, "You" / "Another officer"
  - StaffClaimDetail:
    - รับงานแล้ว action เปลี่ยน
    - 409 แล้วแสดงข้อความ + โหลดใหม่
    - อนุมัติส่งสตางค์ + ยอดตั้งต้น
    - กันอนุมัติเกินยอดประเมิน
    - supervisor ทำ action ได้
    - เคลมปิดแล้วไม่มี action

**แผนเดิม**
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

### F4 — Supervisor dashboard ✅
**สรุปสิ่งที่ทำ** (`supervisor/dashboard.ts`, route `/supervisor` เฉพาะ Supervisor)
- **Claims by status:** ตารางจำนวนเคลมทั้ง 8 สถานะ
- **Waiting to be picked up (N):** เคลมที่รอนานสุด พร้อม "รอมาแล้วกี่วัน/ชั่วโมง"
- **Open claims stuck longest:** สถานะ, คนถืองาน (ชื่อ) และค้างในสถานะนี้มานานเท่าไร
- **Team workload:** `<details>` ต่อคน (เปิดไว้เมื่อมีงาน) แสดงจำนวนงานแยกตามสถานะ + รายการเคลม
- **การใช้งาน:**
  - เลขเคลมทุกจุดลิงก์ไป `/staff/claims/:id` ให้ supervisor ทำ action ต่อได้
  - ปุ่ม Refresh และเวลา "Updated …"
  - ระยะเวลาคำนวณจาก `generatedAt` ของ server (`core/ui/duration.ts`) ไม่ใช้นาฬิกาเครื่อง
- ลบ `ComingSoon` (ไม่มีหน้าไหนใช้แล้ว)
- **Tests (รวม 98):** `timeSince`, Dashboard (จำนวนตามสถานะ, unassigned + ลิงก์, คนถืองาน, workload ต่อคน, Refresh โหลดใหม่)

**แผนเดิม**
- **`/supervisor`:**
  - การ์ดจำนวนเคลมตามสถานะ
  - ตาราง "ยังไม่มีคนรับ (รอนานสุด)"
  - ตาราง "ค้างในสถานะนานสุด"
  - workload ของ staff แต่ละคน (จำนวนงานแยกสถานะ + รายการเคลม)
  - ทุกเลขเคลมกดไปหน้า `/staff/claims/:id` ได้
  - ปุ่ม refresh
- **Tests:** dashboard render จากข้อมูลจำลอง

### F5 — ตรวจรอบสุดท้าย + เอกสาร ✅
**สิ่งที่ตรวจอัตโนมัติแล้ว**
- Gate: backend 181 tests และ frontend 98 tests ผ่าน, `ng build` ผ่าน
- **E2E ผ่าน dev-server proxy (27/27 ผ่าน):** ยิง request ลำดับเดียวกับที่หน้าจอเรียก ผ่าน `http://localhost:4200/api` → backend จริง
  - deep link ทุกหน้าเปิดแอปได้
  - รหัสผิดได้ 401 พร้อมข้อความ, claimant เรียก API ของ staff ได้ 403
  - แจ้งเหตุ → อยู่ในคิว "Waiting to be picked up" → staff1 รับงาน (staff2 รับซ้ำได้ 409) → ขอข้อมูล → claimant ตอบ
  - ประเมิน → อนุมัติเกินยอดได้ 400 → อนุมัติ → จ่าย
  - claimant เห็น Paid + ยอด + หมายเหตุ + เลขอ้างอิง + timeline ครบ 7 ขั้น, เวลาเป็น UTC
  - dashboard: Paid +1 และเคลมที่จ่ายแล้วหายจาก workload
- **ไม่ได้เพิ่ม axe-core** (ผู้ใช้เลือกตรวจ accessibility ด้วยมือ)

**บั๊กที่เจอจากการทดสอบด้วยมือ**
1. **วันที่เกิดเหตุแสดงเลื่อนไป 1 วัน** (API ส่ง `2026-11-25` แต่จอแสดง Nov 24)
   - สาเหตุ: ใช้ `date: 'mediumDate' : 'UTC'` แต่ DatePipe parse string แบบวันที่อย่างเดียวเป็นเที่ยงคืน*เวลาเครื่อง* พอแสดงเป็น UTC ในโซน UTC+7 จึงถอยไปวันก่อน
   - แก้: เอา `'UTC'` ออกทั้ง 6 จุด (incidentDate, dueDate) และเพิ่ม test ที่เช็ควันที่บนจอ 4 หน้า (รวม 99 tests)

**เอกสาร:** README เพิ่ม Quick start (รันสองฝั่ง), ตาราง Screens ต่อ role, flow ทดลองผ่านหน้าเว็บ, โครงสร้าง frontend และ design decisions ฝั่ง frontend

**Checklist ทดสอบด้วยมือ** (รัน backend + `npm start` แล้วเปิด http://localhost:4200, รหัสผ่านทุกบัญชี `Password123!`)

*Claimant (`claimant1@example.com`)*
- [ ] Login แล้วไปหน้า My claims และเมนูมี "My claims" + "Report an incident"
- [ ] Report an incident:
  - ประเภทเหตุถูกปิดจนกว่าจะเลือก policy
  - Motor แสดง Collision/Theft/Flood/Glass, เปลี่ยนเป็น Property แล้วรายการเปลี่ยน
- [ ] กดส่งตอนฟอร์มว่าง: ทุกช่องขึ้นข้อความแดงใต้ช่อง
- [ ] ใส่ยอดเกินทุนประกัน (เช่น 600000 กับ Motor) แล้วขึ้นข้อความ ไม่ส่ง
- [ ] ส่งสำเร็จแล้วไปหน้ารายละเอียด: ยอดแสดงเป็น ฿, เวลาเป็นเวลาเครื่อง, timeline 1 แถว
- [ ] My claims: เคลมใหม่อยู่บนสุด, filter สถานะ "Withdrawn" แล้วรายการเปลี่ยน
- [ ] ยกเลิกเคลม (Withdraw → ยืนยัน) แล้วสถานะเปลี่ยนเป็น Withdrawn และปุ่มหายไป
- [ ] Register บัญชีใหม่ (รหัสผ่านสั้นกว่า 8 ต้องไม่ผ่าน, email ซ้ำได้ข้อความจาก server)

*Staff (`staff1@example.com`, `staff2@example.com`)*
- [ ] Work queue เปิดที่แท็บ "Waiting to be picked up", สลับแท็บ/filter แล้วรายการเปลี่ยน
- [ ] เปิดเคลมใหม่ → "Pick up this claim" แล้ว Handled by เป็น You และมีฟอร์ม 3 อัน
- [ ] staff2 เปิดเคลมเดียวกัน: ไม่มีปุ่ม action และบอกว่าคนอื่นถืองาน
- [ ] (ทดสอบชนกัน) เปิดเคลม Submitted ใบเดียวกันใน 2 แท็บด้วย staff1 และ staff2 แล้วกดรับงานทีละแท็บ: แท็บหลังขึ้นข้อความ error และหน้าโหลดใหม่
- [ ] ขอข้อมูล → claimant เห็นกล่อง "We need more information" แล้วตอบ → staff เห็นคำตอบ
- [ ] ประเมิน (ยอดตั้งต้น = ยอดเคลม) → อนุมัติ (ลองยอดเกินยอดประเมินต้องไม่ผ่าน) → บันทึกการจ่าย
- [ ] claimant เห็นสถานะ Paid, ยอดอนุมัติ, หมายเหตุ, เลขอ้างอิง
- [ ] ลองปฏิเสธเคลมอีกใบ (ต้องใส่เหตุผล) แล้ว claimant เห็นเหตุผล

*Supervisor (`supervisor@example.com`)*
- [ ] Login แล้วไป Dashboard และเมนูมี Dashboard + Work queue
- [ ] ตัวเลขตามสถานะ, รายการรอรับงาน (มี "รอมาแล้ว …"), workload ต่อคน
- [ ] กดเลขเคลมจาก dashboard → รับงาน/อนุมัติได้เหมือน staff → กลับมากด Refresh แล้วตัวเลขเปลี่ยน

*สิทธิ์และ session*
- [ ] พิมพ์ URL ข้าม role (claimant เปิด `/supervisor`, staff เปิด `/claims`) แล้วถูกพากลับหน้าของตัวเอง
- [ ] ยังไม่ login แล้วเปิด `/claims/new` จะไปหน้า login แล้ว login เสร็จกลับมาหน้าเดิม
- [ ] Sign out แล้วกด Back ของเบราว์เซอร์: เข้าไม่ได้ ต้อง login ใหม่
- [ ] (session หมดอายุ) ใน DevTools → Application → Local Storage แก้ `expiresAt` ใน `claim-system.session` เป็นเวลาในอดีต แล้ว reload: กลับไปหน้า login

*Accessibility*
- [ ] ใช้ keyboard อย่างเดียว (Tab / Shift+Tab / Enter / Space) ทำ flow แจ้งเหตุจนจบได้ และเห็น focus ชัดทุกจุด
- [ ] กด Tab ครั้งแรกในหน้าจะเห็นลิงก์ "Skip to main content"
- [ ] เปลี่ยนหน้าแล้ว focus อยู่ที่หัวข้อของหน้าใหม่
- [ ] extension **axe DevTools** ไม่มี violation ในหน้า: login, register, My claims, Report an incident, claim detail, Work queue, staff claim detail, Dashboard
- [ ] ลอง dark mode ของระบบ: ตัวหนังสือยังอ่านออก (Pico เปลี่ยนธีมตาม OS)

**แผนเดิม**
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
