# 🏆 Fullstack Excellence Blueprint

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
  - `feat/feature-name` for new work.
  - `fix/bug-name` for hotfixes.
  - `chore/task-name` for maintenance (e.g., updating dependencies).
- **Commit Messages**: Use **Conventional Commits** (e.g., `feat: add folder renaming`).
- **PR Strategy**:
  - Always squash-merge to keep the `main` branch history clean.
  - Mandatory code review for any logic changes.

## 🛡️ Security & Performance
- **Secrets**: Never commit `.env` files. Use `.env.example` as a template.
- **CORS**: Strictly define allowed origins; never use `*` in production.
- **Data Fetching**: Use `@tanstack/react-query` for caching and loading states to minimize redundant API calls.

## 🧪 Testing Strategy
- **Unit Tests**: Focus on Application `Handlers` and Domain `Entities`.
- **Integration Tests**: Verify API endpoints against a real (test) database.
- **Linting**: Enforce strict TypeScript/C# linting rules to catch errors before they reach CI.

## ⚛️ Frontend Best Practices
- **Component Anatomy**:
  - Keep components under 150 lines.
  - Extract complex sub-logic into separate files (e.g., `FolderTree` -> `FolderItem`).
  - **No Inline Styles**: Use CSS files or a CSS-in-JS library. Inline styles clutter the logic.
- **Data Fetching**: Use `@tanstack/react-query` for caching, loading states, and automatic revalidation.

## 🛠️ Backend Best Practices
- **Mediator Pattern**: Use **MediatR** to keep controllers thin. Controllers should only validate input and dispatch commands/queries.
- **Clean Architecture**: 
  - `Domain`: Enterprise logic & Entities.
  - `Application`: Use Cases (Command/Query Handlers).
  - `Infrastructure`: Data access, external services.
  - `API`: Entry points & Middleware.
- **Logging**: Use structured logging (e.g., **Serilog**) with meaningful properties, not just strings.

## 🤖 Future Agent Instructions
1. **Read the Blueprint**: Always check this file before implementing new features.
2. **Modularize First**: If a file exceeds 200 lines, search for extraction points before adding more code.
3. **Audit the "Chain"**: When adding a field to the DB, update the Entity -> DTO -> Handler/Query -> API -> Generated Client.

---
*Created during the Architectural Overhaul of February 2026.*
