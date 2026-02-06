# 🏆 Architecture & Excellence Blueprint

This document defines the "Golden Rules" and architectural standards for the Horizon FMS project. It is intended for both human developers and AI agents to ensure consistency, scalability, and high code quality.

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

### 3. Container-First Development
- **Docker**: Every service (API, Web, DB) must be in `docker-compose.yml`.
- **Environment**: Use `.env` files for secrets and environment-specific URLs.
- **Database**: Use a real containerized DB (Postgres) even in dev, not the "easy" SQLite path.

## 🌿 Git & Collaboration
- **Branch Naming**: 
  - `feat/feature-name` (new work)
  - `fix/bug-name` (hotfixes)
  - `chore/task-name` (maintenance)
- **Commit Messages**: Use **Conventional Commits** (e.g., `feat: add folder renaming`).
- **PR Strategy**: Always squash-merge to keep the `main` branch history clean.

## 🛡️ Security & Performance
- **Secrets**: Never commit `.env` files. Use `.env.example` as a template.
- **CORS**: Strictly define allowed origins; never use `*` in production.
- **Data Fetching**: Use `@tanstack/react-query` for caching and loading states to minimize redundant API calls.

## 📜 Naming Conventions & Style
- **C#**:
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

## 🤖 Future Agent Instructions
1. **Read the Blueprint**: Always check this file before implementing new features.
2. **Modularize First**: If a file exceeds 200 lines, search for extraction points before adding more code.
3. **Audit the "Chain"**: When adding a field to the DB, update the Entity -> DTO -> Handler/Query -> API -> Generated Client.

---
*Created during the Architectural Overhaul of February 2026.*
