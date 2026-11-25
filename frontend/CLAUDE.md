# Frontend (Angular 22)

Angular app for the claim system. It talks to the backend API at `http://localhost:5106` (CORS allows `http://localhost:4200`); see the root `README.md` for the API overview and demo accounts.

- Use the **Angular CLI MCP server** (`angular-cli` in the repo's `.mcp.json`): `search_documentation` / `get_best_practices` when unsure about an Angular API, `run_target` for build/test, `devserver_start` / `devserver_wait_for_build` to check the app compiles.
- API amounts are integers in satang (1 THB = 100); timestamps are UTC — convert to the user's local time for display.
- Follow the working agreement in the root `CLAUDE.md` (one phase at a time, tests + build before review, visible file edits). Plan and phase status: `docs/frontend-plan.md`.

## Commands (run from `frontend/`)
```bash
npm start          # ng serve on http://localhost:4200; /api is proxied to the backend (proxy.conf.json)
npm run build      # must pass
npm test           # Vitest unit tests, must pass
npm run gen:api    # regenerate src/app/core/api/schema.d.ts from the running backend's /openapi/v1.json
```
- Never edit `src/app/core/api/schema.d.ts` by hand; regenerate it after backend DTO changes and fix what `ng build` reports.
- `openapi-typescript` runs via `npx` (pinned) because it does not support TypeScript 6 as a dependency yet.
- Styling is Pico.css (classless): use semantic HTML (`form`, `label`, `table`, `nav`, `article`) instead of custom classes.
- When checking the dev server yourself, use another port (e.g. `--port 4300`) and stop only that process: the user may have their own `npm start` on 4200.

## Project conventions
- UI text is **English** (matches the backend's error messages).
- API types: import from `core/api/models.ts` (aliases over the generated `schema.d.ts`).
- Errors: show `errorMessage(error)` from `core/api/problem.ts` in `<app-error-alert>`; `errorCode(error)` gives the backend `code`.
- Money: `thb` pipe for display, `bahtToSatang()` before sending (`core/ui/money.ts`). Status: `<app-status-badge>`.
- Auth: `AuthStore` (signals: `user`, `role`, `homeUrl`); routes use `authGuard` + `roleGuard(...)` mirroring the backend policies.
- Every page starts with `<h1 tabindex="-1">`: the shell moves focus to it after each navigation.
- Forms: Signal Forms with `[formRoot]` + `submission.action`; return `{ kind: 'server', message: errorMessage(e) }` for API errors and show `form().errors()` in `<app-error-alert>`. Each input gets a `<label for>`, `[attr.aria-invalid]="ariaInvalid(field())"`, `aria-describedby` and an `<app-field-error>` below it.
- Route params / query params / route data reach components as `input()`s (`withComponentInputBinding`).

---

You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## TypeScript Best Practices

- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Do NOT set `changeDetection: ChangeDetectionStrategy.OnPush` explicitly. `OnPush` is the default in Angular v22+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Accessibility Requirements

- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

## Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `model()` for two-way bound properties with `[(prop)]` syntax instead of pairing `input()` with `output()`
- Use `computed()` for derived state
- Use `linkedSignal()` for state derived from multiple reactive sources that must stay synchronized
- Prefer inline templates for small components
- Prefer Signal Forms (`@angular/forms/signals`) for new forms. They are stable in Angular v22+ and provide signal-based state, type-safe field access, and schema-based validation
- When not using Signal Forms, prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- Do NOT import `CommonModule`, import only the directives and pipes the template uses, such as `AsyncPipe` or `DatePipe`
- When using external templates/styles, use paths relative to the component TS file.

## State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.

## Services

- Design services around a single responsibility
- Prefer the `@Service` decorator over `@Injectable({providedIn: 'root'})` for new singleton services (Angular v22+)
- Use the `inject()` function instead of constructor injection
