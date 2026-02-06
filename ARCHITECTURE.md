# 🏆 Architecture & Excellence Blueprint

This document defines the "Golden Rules" and the architectural standards. It is intended for both human developers and AI agents to ensure consistency, scalability, and high code quality.

## 🏗️ Architectural Pillars

### 1. The "Single Source of Truth" API
- **Tooling**: Use **NSwag** or **OpenAPI** to auto-generate TypeScript clients.
- **Models & Structure**: The auto-generated client should include all **Response Models** and **Request Structures**. Never manually define these in the frontend.
- **Rule**: Whenever the backend DTOs change, re-run the client generator.
- **Benefit**: Zero "type mismatch" bugs.

### 2. Standardized Error Handling
- **Backend**: Use a global `ExceptionMiddleware`. No `try-catch` blocks in controllers unless for very specific logic.
- **Responses**: Always return `ProblemDetails` (RFC 7807).
- **Frontend**: Use a central notification system (e.g., `react-hot-toast`) to display these errors.

### 3. Container-First & Infrastructure-as-Code
- **Docker**: Every core dependency (API, Web, DB, **Redis**) must be in `docker-compose.yml`.
- **Environment**: Use `.env` files for secrets. Use `render.yaml` or similar for cloud infrastructure definition.
- **Rules**: Local dev must be "Plug & Play" with a single `docker-compose up`.

### 4. Background Job & Multi-Channel Delivery
- **Offloading**: Never perform slow operations (Email, external API sync) in the request-response cycle. Use **BullMQ** or similar.
- **Reliability**: Jobs should be retriable and traceable.
- **Fallback**: Implement multi-channel defaults (e.g., WebSocket for real-time, Email for fallback).

### 5. Resilient Session Management
- **UX Requirement**: Users should never be kicked out due to expired short-lived tokens.
- **Pattern**: Implement a 401 Interceptor that triggers an automatic `/auth/refresh` and retries the original request seamlessly.

### 6. Universal State & Caching
- **Standard**: Always use `@tanstack/react-query` for data fetching, caching, and state synchronization.
- **Benefit**: Ensures a "snappy" UI with built-in optimistic updates and automated background refetching.
- **Parity**: All frontends (Web, Mobile, etc.) MUST adopt this to ensure consistent behavior.

### 7. Real-time Communication & Presence
- **Technology**: Use **Socket.IO** for bidirectional, real-time communication.
- **Pattern**: Implement room-based communication for scoped updates (e.g., `enter-list`, `leave-list` events).
- **Use Cases**:
  - Collaborative presence (show active users in a list)
  - Live task updates across clients
  - Instant notifications for shared list changes
- **Mobile Integration**: Use a singleton socket instance to manage persistent connections across screens.
- **Fallback**: Always ensure REST APIs are available as fallback for critical operations.

## 🌿 Git & Collaboration
- **Branch Naming**: 
  - `feat/feature-name` (new work)
  - `fix/bug-name` (hotfixes)
  - `chore/task-name` (maintenance)
- **Commit Messages**: Use **Conventional Commits** (e.g., `feat: add folder renaming`).
- **PR Strategy**: Always squash-merge to keep the `main` branch history clean.

### Git Tagging & Semantic Versioning
- **Versioning Standard**: Follow **Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`
  - **MAJOR**: Breaking changes (e.g., API contract changes, removed features)
  - **MINOR**: New features, backward-compatible additions
  - **PATCH**: Bug fixes, performance improvements
- **Tag Format**: `v{version}` (e.g., `v1.2.3`)
- **Release Process**:
  1. Update version in relevant `package.json` or version files
  2. Create annotated tag: `git tag -a v1.2.3 -m "Release v1.2.3: Feature parity achieved"`
  3. Push tags: `git push origin v1.2.3`
- **Pre-release Tags**: Use `-alpha`, `-beta`, `-rc` suffixes (e.g., `v2.0.0-beta.1`)
- **Changelog**: Maintain `CHANGELOG.md` with version history using conventional commit groupings

## 🛡️ Security & Performance
- **Secrets**: Never commit `.env` files. Use `.env.example` as a template.
- **CORS**: Strictly define allowed origins; never use `*` in production.

## 📜 Naming Conventions & Style
- **NestJS / TypeScript**:
  - DTOs end in `Dto` (`CreateTaskDto.ts`).
  - Handlers follow the `Command/Handler` or `Query/Handler` pattern.
  - Services use `camelCase` for methods and avoid `any` at all costs.
- **C# / .NET (Alternative Standard)**:
  - Interfaces start with `I` (e.g., `IStorageService`).
  - Async methods must end in `Async` (e.g., `SaveFileAsync`).
  - Use file-scoped namespaces.
- **TypeScript/React**:
  - Components use PascalCase (`FolderTree.tsx`).
  - Filenames should match the exported component.
  - Constants use UPPER_SNAKE_CASE.

## 🚫 Anti-Patterns (What NOT to do)
- **Lazy API Calls**: Don't use raw `axios` or `fetch` in components; use the generated client.
- **Fat Controllers**: Controllers should not contain business logic. Logic lives in `Handlers`.
- **ViewBag/ViewData**: In ASP.NET, never use dynamic objects like `ViewBag`. Use strongly-typed ViewModels or MediatR results for compile-time safety.
- **Inline Styles**: Avoid `style={{...}}` in React. Use CSS files or Tailwind.

## 📖 Architecture Decision Records (ADR)
- **ADR 001: Database**: Chose Postgres over SQLite to ensure production/development parity and support complex indexing.
- **ADR 002: Error Handling**: Chose Global Middleware + ProblemDetails to provide a consistent mobile/web-friendly error format.
- **ADR 003: Background Jobs**: Chose BullMQ for task offloading to ensure API responsiveness and job reliability.

## 🤖 Future Agent Instructions
1. **Read the Blueprint**: Always check this file before implementing new features.
2. **Modularize First**: If a file exceeds 200 lines, search for extraction points before adding more code.
3. **Audit the "Chain"**: When adding a field to the DB, update the Entity -> DTO -> Handler/Query -> API -> Generated Client.

---
*Created during the Architectural Overhaul of February 2026.*
