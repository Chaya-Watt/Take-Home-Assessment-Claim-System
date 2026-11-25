# AI working journal

บันทึกการทำงานร่วมกับ AI ตลอดโปรเจกต์ (24–25 พ.ย. 2026): ถาม/สั่ง AI อะไร, **รับ** อะไร, **แย้ง (challenge)** อะไร และ **ตัดสินใจแทน (override)** อะไร พร้อมเหตุผลสั้นๆ
ไม่ได้เกลาให้สวย เขียนตามลำดับที่เกิดขึ้นจริง

---

## 1. เครื่องมือและการตั้งค่า

| สิ่งที่ใช้ | ใช้ทำอะไร |
|---|---|
| **Claude Code** (extension ใน VS Code, model Claude Opus) | ผู้ช่วยหลัก: วางแผน, เขียนโค้ด, รัน build/test, อธิบายโค้ด |
| **Plan mode** ของ Claude Code | ให้ AI วางแผนอย่างเดียวโดยยังไม่แตะโค้ด ใช้ตอนวางแผน backend และ frontend แล้ว review / comment แผนก่อนอนุมัติ |
| **Microsoft Learn MCP** | ให้ AI ตรวจโครงสร้าง .NET เทียบเอกสารทางการของ Microsoft (Clean Architecture, Minimal APIs) และหาสาเหตุ error ของ test runner บน .NET 10 |
| **Angular CLI MCP** (`npx @angular/cli mcp` ตั้งใน `.mcp.json`) | ตั้งตามหน้า [angular.dev/ai/develop-with-ai](https://angular.dev/ai/develop-with-ai) แล้วให้ AI ใช้ `search_documentation` / `get_best_practices` เช็ค API ของ Angular 22 (Signal Forms, `httpResource`, `@Service`) และใช้ `run_target` รัน build/test |
| **`CLAUDE.md`** (root + `frontend/CLAUDE.md`) | "กฎของโปรเจกต์" ที่ AI อ่านทุกครั้ง: คำสั่ง build/test, โครงสร้าง, convention และข้อตกลงการทำงาน |
| **Angular best-practices rules file** | เอาไฟล์ best practices จาก angular.dev ใส่ใน `frontend/CLAUDE.md` ให้ AI ใช้เฉพาะตอนทำ frontend (Claude Code โหลด `CLAUDE.md` ในโฟลเดอร์ย่อยเมื่อทำงานกับไฟล์ในโฟลเดอร์นั้น) |
| **Memory ของ Claude Code** | จำการตัดสินใจและวิธีทำงานข้าม session เช่น "ทำทีละ phase", "แก้ไฟล์ผ่าน editor ให้เห็น diff" และ scope ที่ตัดออก |

ไม่ได้ใช้ skill พิเศษเพิ่มเติม เพราะใช้ `CLAUDE.md` + MCP เป็นตัวกำหนดแนวทางแทน

---

## 2. วิธีสั่งงาน (working agreement)

กติกาที่ตั้งให้ AI ตั้งแต่ต้น บางข้อเกิดจากการแย้งระหว่างทาง:

1. **ทำทีละ phase เล็กๆ** ทุก phase ต้องมี test และผ่าน gate (`dotnet build -warnaserror` + `dotnet test`, ฝั่ง frontend คือ `ng build` + `ng test`) จากนั้น **หยุดรอ review** ก่อนไป phase ถัดไป
2. **Commit เอง** ไม่ให้ AI commit แต่ให้ AI เสนอ commit message แทน เพื่อให้ commit history สะท้อนการ review ของคนจริงทีละ phase
3. **แก้ไฟล์ผ่าน editor tools** เท่านั้น เพื่อให้เห็น diff ใน IDE (ไม่ให้แก้ผ่าน shell หรือ `dotnet add`)
4. **Setup ให้น้อยที่สุด** เพิ่ม config/package เฉพาะใน phase ที่ได้ใช้จริง
5. **ถามก่อนตัดสินใจเรื่องที่เป็นของเรา** เช่น scope, ภาษา, วิธีเก็บไฟล์ ส่วนเรื่องที่มีค่าเริ่มต้นชัดเจนให้ AI ตัดสินใจเองแล้วบอก
6. **อธิบายก่อนเขียนสิ่งที่ไม่คุ้น** ถ้า AI จะเพิ่ม abstraction ใหม่ ต้องบอกว่าคืออะไรและทำไม
7. สื่อสารภาษาไทย ส่วนโค้ด, comment และ README เป็นภาษาอังกฤษสำหรับผู้ตรวจ

**รูปแบบ prompt ที่ใช้บ่อย**
- สั่งระดับเป้าหมาย: *"ช่วยแพลนงาน BE ก่อน ทำเป็น phase ย่อยๆ"*, *"commit แล้ว phase ถัดไปได้เลย"*
- ถามเพื่อทำความเข้าใจก่อนยอมรับ: *"Version ที่อยู่ใน claim คืออะไร"*, *"ClaimNumberCounter ใช้ทำอะไร"*, *"UseAuthentication กับ UseAuthorization ต่างกันยังไง"*, *"app-root ใช้ตอนไหน"*
- Reject การแก้ไฟล์กลางทางพร้อมเหตุผล เมื่อ AI ทำเกินหรือไม่ชัด: *"อันนี้กำลังทำอะไร?"*, *"หน้านี้ไว้ใช้ทำอะไร?"*
- Comment บนแผนใน plan mode ก่อนอนุมัติ

---

## 3. บันทึกตาม phase

ป้าย: ✅ รับตามที่ AI เสนอ · ❓ แย้ง/ถามจน AI ต้องอธิบายหรือปรับ · ✋ ตัดสินใจแทน AI

### วางแผน backend
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| AI ถามเรื่องสถาปัตยกรรมกลับมาก่อนวางแผน (monolith + Kafka, JWT ที่ออกเอง, role Staff + Supervisor) | ✅ เลือกตามตัวเลือก | ตรงโจทย์และขนาดงาน |
| "โครงสร้างดูตาม best practice หรือยัง" | ❓ ให้ AI ตรวจผ่าน Microsoft Learn MCP | อยากให้อ้างอิงเอกสารทางการ ไม่ใช่ความจำของ AI |
| `HolderId` / `ClaimantId` คืออะไร, IncidentType มีอะไรบ้าง | ❓ AI ต้องเพิ่มคำอธิบายและตาราง IncidentType ตาม ProductType ในแผน | แผนต้องชัดพอให้คนอ่านเข้าใจ |
| `ClaimNote` "ทำไมต้องมี" | ✋ ตัดออก | ผลประเมินกับเหตุผลการตัดสินมี field เก็บอยู่แล้ว |
| วงเงินอนุมัติ + ขั้นรอ Supervisor อนุมัติ | ✋ ตัดออก: Staff อนุมัติเองได้ทุกเคส ส่วน Supervisor ดู dashboard | ลดความซับซ้อน โจทย์ไม่ได้ขอ |

### Phase 0 — Scaffolding
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| AI รัน `dotnet sln add` / `add package` ทีละหลายคำสั่งผ่าน shell | ✋ Reject | มองไม่เห็นว่าแก้อะไรใน IDE จึงตั้งกติกาแก้ไฟล์ผ่าน editor |
| AI ใส่ Central Package Management, `.editorconfig` ยาว, analyzer เข้มๆ, global exception handler, architecture tests | ❓ "config/setup เยอะเกินไปไหม" แล้วตัดเหลือขั้นต่ำ | เป็นงานส่ง test ต้องอ่านง่าย ของพวกนี้ค่อยเพิ่มเมื่อมีเหตุผล |
| "ทำเป็น Minimal API หรือ Controllers" | ✅ Minimal API ต่อ | ตรงกับแนวทางปัจจุบันของ .NET |
| Error "VSTest no longer supported" บน .NET 10 | ✅ AI หาวิธีแก้จาก Microsoft Learn แล้วเปลี่ยนเป็น Microsoft.Testing.Platform | แก้ตามเอกสารทางการ |
| AI จะ commit เอง | ✋ Reject: commit เอง | ต้องการคุม commit history |

### Phase 1 — Domain + state machine
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| AI เริ่มเขียน `AggregateRoot` / `IDomainEvent` | ❓ "อันนี้กำลังทำอะไร" แล้วเลื่อนไปทำพร้อม Kafka | ยังไม่มีคนใช้ event ใน phase นี้ (สุดท้ายไม่ได้ทำ) |
| Test แบบ matrix (ทุก action × ทุกสถานะที่ห้าม) | ✅ | ครอบคลุม state machine โดยโค้ด test ไม่ยาว |

### Phase 2 — Persistence
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| AI ทำ test DB แยก (temp SQLite ต่อ test class) | ✋ ไม่ต้องมี ให้ test ใช้ `claims.db` เดียวกับตอนรัน | ลด setup (AI ปรับ test ให้รันซ้ำบน DB เดิมได้) |
| ถาม `Version`, เวลาเก็บเป็น UTC ไหม, `ClaimNumberCounter` | ❓ ให้อธิบายจนเข้าใจก่อนยอมรับ | ต้องตอบคำถามเหล่านี้ได้ตอน walkthrough |

### Phase 3 — Auth
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| JWT + PasswordHasher + role policies + error handler | ✅ | ตามแผน |
| AI ไม่ทำ `ICurrentUser` ตามแผน แต่ส่ง user id เป็นพารามิเตอร์แทน | ✅ | ง่ายกว่าและสอดคล้องกับ domain ที่รับ actor id อยู่แล้ว |

### Phase 4 — Claimant
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| AI จะคิด "วันนี้" ตามเวลากรุงเทพ (`BusinessCalendar`) | ❓ แย้งว่าอยากให้ส่ง UTC แล้ว FE แปลงเอง → AI อธิบายว่าเป็นเรื่องกฎ "วันเกิดเหตุห้ามเป็นอนาคต" ไม่ใช่การแสดงผล → เลือก Bangkok calendar | เวลาเก็บเป็น UTC แต่กฎที่อิง "วันนี้" ต้องใช้ปฏิทินของธุรกิจ |
| "document จะเก็บไว้ไหน" | ✅ ไฟล์บน disk + metadata ใน DB | — |
| **"ทำเยอะหรือซับซ้อนเกินความจำเป็นในการส่งเทสไหม"** | ❓ AI ประเมิน scope ให้ → ✋ **ตัดการแนบไฟล์ทิ้ง** และลด Kafka | โจทย์ไม่ได้ขอแนบไฟล์ เก็บแรงไว้กับสิ่งที่โจทย์ขอ |

### Phase 5–8 — Staff, ตัดสิน/จ่าย, dashboard, README
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| "approve / reject / pay แล้วเกิดอะไรขึ้นต่อ" | ❓ ให้ AI อธิบายก่อนทำ | เข้าใจ flow หลังตัดสินผล |
| Kafka notification | ✋ เลื่อนไปเป็น "ถ้ามีเวลา" และเขียนอธิบายใน README | อยากเริ่มทำ API/FE ให้ครบก่อน การแจ้งลูกค้าไม่ต้อง realtime |
| Reporting เชิงสถิติ | ✋ ตัดออก เหลือ dashboard: ใครดูเคลมไหน, เคลมไหนไม่มีคนรับ, ค้างที่สถานะไหน | ตรงกับที่ supervisor ต้องใช้จริง |
| เลขอ้างอิงการจ่าย | ✅ staff กรอกเอง (โอนนอกระบบ) | ไม่ปลอมเลขที่ไม่ผูกกับการโอนจริง |
| README | ❓ ขอให้ใส่วิธีรัน backend ให้ละเอียด แล้ว AI เพิ่ม prerequisite / troubleshooting และลองทำตามบน DB ใหม่ | ผู้ตรวจต้องรันตามได้จริง |

### วางแผน frontend
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| ต่อ Angular CLI MCP + เอา best practices ใส่ `frontend/CLAUDE.md` | ✅ ทำตามที่สั่ง | ให้ AI ใช้ Angular 22 ที่ถูกเวอร์ชัน ไม่ใช่ความจำเก่า |
| AI จะเขียน DTO ฝั่ง TS เอง | ❓ "generate จาก OpenAPI คุ้มกว่าไหม", "ต้องเขียน DTO เองหรือเช็คจาก BE ได้" → เปลี่ยนเป็น generate | ลดงานซ้ำ และพอ BE เปลี่ยน build จะชี้จุดที่ไม่ตรง |
| Supervisor ใช้หน้า staff ได้ไหม | ❓ ให้เขียนลงแผนให้ชัด | Supervisor ต้องทำได้ทุกอย่างเหมือน staff |
| Styling / test | ✅ Pico.css, Vitest unit tests (ไม่ทำ Playwright) | UI ไม่ต้องสวย เน้นต่อ API ได้จริง |

### Frontend F0–F5
| เรื่อง | ผล | เหตุผล |
|---|---|---|
| `openapi-typescript` ยังไม่รองรับ TypeScript 6 | ✅ AI เลือกรันผ่าน `npx` แบบล็อกเวอร์ชัน แทนการบังคับติดตั้ง | ไม่ทำ dependency ของโปรเจกต์พัง |
| AI เจอบั๊ก BE ตอนเช็ค proxy (JSON ผิดรูปได้ 500) | ✅ แก้เป็น 400 + test | — |
| ภาษา UI: เลือกอังกฤษ → กลาง F3 ขอเปลี่ยนเป็นไทย → AI เริ่มแปลข้อความใน backend | ✋ หยุด "ถ้าให้เป็นไทยแค่ FE ได้ไหม" แล้วตัดสินใจคงภาษาอังกฤษไว้ | ไม่อยากแก้ backend ตอนท้าย และข้อความ error จาก BE เป็นอังกฤษอยู่แล้ว |
| AI จะเขียนหน้า Work queue | ❓ "หน้านี้ไว้ใช้ทำอะไร" แล้วอนุมัติหลังได้คำอธิบาย | อยากรู้ว่าแต่ละหน้าตอบโจทย์อะไร |
| AI เสนอเพิ่ม axe-core ใน test | ✋ ไม่เพิ่ม ตรวจ accessibility ด้วยมือ (axe DevTools) | ไม่อยากเพิ่ม dependency |
| **ทดสอบด้วยมือเจอบั๊ก:** วันที่เกิดเหตุแสดงเลื่อนไป 1 วัน (API `2026-11-25` แต่จอ Nov 24) | ❓ ส่งภาพหน้าจอ + response ให้ AI → AI พบว่าตัวเองใส่ `'UTC'` ใน DatePipe ผิด แก้ 6 จุด + เพิ่ม test ที่เช็ควันที่บนจอ | unit test เดิมไม่เช็คข้อความวันที่ จึงต้องทดสอบด้วยมือด้วย |

---

## 4. ข้อผิดพลาดของ AI ที่จับได้

- **ทำเกินความจำเป็น:** config/analyzer/architecture tests ใน Phase 0, domain events ล่วงหน้า, การแนบไฟล์ และ reporting ทั้งหมดถูกตัด (AI มักเสนอแบบ "production-grade" ต้องคอยดึงกลับมาที่ scope ของโจทย์)
- **แก้ไฟล์แบบมองไม่เห็น:** ใช้ shell แก้หลายไฟล์ จึงต้องตั้งกติกาแก้ผ่าน editor
- **ไปหยุด dev server ของเรา:** ตอนเช็คพอร์ต AI สั่ง `pkill -f "ng serve"` ซึ่งปิด `npm start` ที่เราเปิดค้างไว้ AI แจ้งเองแล้วเปลี่ยนไปใช้พอร์ตอื่นและปิดเฉพาะ process ของตัวเอง (เขียนกฎนี้ลง `frontend/CLAUDE.md` แล้ว)
- **Bug จาก "ความรู้ที่มั่นใจแต่ผิด":** ใส่ `'UTC'` ใน DatePipe เพื่อกันวันเลื่อน แต่กลับทำให้เลื่อน จับได้จากการทดสอบด้วยมือ ไม่ใช่จาก test
- **ผิดเวอร์ชัน API:** เช่นใส่ `min` บน input ของ Signal Forms ไม่ได้ (NG8022) และ test ของ `httpResource` ค้าง แต่ build/test gate จับได้ทันทีและ AI แก้ตามเอกสาร Angular ผ่าน MCP

## 5. สิ่งที่ได้เรียนรู้

- **ทำทีละ phase + gate + หยุด review** ทำให้แย้งได้เร็วก่อนงานจะบานปลาย และ commit history อ่านเป็นเรื่องราวได้
- **ถาม "อันนี้คืออะไร / ทำไม"** ก่อนกดยอมรับ ช่วยกันไม่ให้มีโค้ดที่อธิบายเองไม่ได้ (ต้องใช้ใน walkthrough)
- **MCP ช่วยได้จริงกับของใหม่** (.NET 10, Angular 22) ที่ความจำของ AI อาจล้าสมัย
- **Test อัตโนมัติไม่พอ** ยังต้องลองใช้งานจริง บั๊กวันที่เป็นตัวอย่างชัดเจน
- AI ชอบเสนอของครบเกินโจทย์ ต้องเป็นคนตัดสินเรื่อง scope เอง
