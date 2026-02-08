---
inclusion: always
---

# Horizon Coding Standards & Architecture Rules

## 🏗️ Core Architectural Principles

### API-First Development
- **ALWAYS** use NSwag/OpenAPI to auto-generate TypeScript clients
- **NEVER** manually define Response Models or Request Structures in frontend
- When backend DTOs change, re-run the client generator
- This eliminates type mismatch bugs

### Error Handling
- **Backend**: Use global `ExceptionMiddleware` - no try-catch in controllers unless specific logic requires it
- **Responses**: Always return `ProblemDetails` (RFC 7807)
- **Frontend**: Use central notification system (react-hot-toast) for errors

### Container-First Development
- Every core dependency (API, Web, DB, Cache) MUST be in `docker-compose.yml`
- Use `.env` files for configuration
- Local dev must be "Plug & Play" with `dev.ps1` script

### State Management & Caching
- **ALWAYS** use `@tanstack/react-query` for data fetching and caching
- Ensures snappy UI with optimistic updates
- All frontends must adopt the same caching logic

### Observability
- **Structured Logging**: All logs must be JSON format with context
- **Health Checks**: Implement `/health` endpoints
- Use **Seq** for log visualization

### Storage Abstraction
- Interact with storage via interfaces (e.g., `IStorageService`)
- Support multiple providers (Local, S3, Cloudinary) via configuration
- Use centralized path resolver

## 🚀 Git & Workflow Standards

### Commit Strategy
- **Atomic Commits**: Each commit = one logical change
- **Conventional Commits**: Use `type(scope): description` format
  - Types: feat, fix, chore, refactor, docs, style, test
  - Example: `feat(ui): add dark mode support`
- Keep commits small and focused
- Related/dependent changes should be committed together

### After Making Changes
1. Check git status
2. Stage changes: `git add .`
3. Commit with descriptive message
4. Push to remote: `git push`

### Versioning
- Follow **Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`
- Use automation tools for version management
- Never manually edit automated `CHANGELOG.md`

## 🛡️ Security Standards

Every project MUST implement:
- **CSP** (Content-Security-Policy)
- **X-Frame-Options: DENY**
- **X-Content-Type-Options: nosniff**
- **Referrer-Policy: no-referrer**
- **Rate Limiting** (IP-based)

## 📜 Naming Conventions

### C# / .NET
- **Namespaces**: Use file-scoped namespaces
- **Interfaces**: Start with `I` (e.g., `IStorageService`)
- **Async Methods**: End with `Async` (e.g., `GetFileAsync`)

### TypeScript / React
- **Components**: PascalCase (e.g., `FileList`, `Dashboard`)
- **Files**: Match component name (e.g., `FileList.tsx`)
- **Hooks**: Start with `use` (e.g., `useTheme`)
- **Types/Interfaces**: PascalCase (e.g., `FileItemDto`)

### General
- Use descriptive, meaningful names
- Avoid abbreviations unless widely understood
- Be consistent across the codebase

## ✅ Code Quality Standards

### Frontend
- **Zero `any` types** - always use proper TypeScript types
- Vite/Build must succeed without errors
- Use ESLint and Prettier for formatting
- Components should be under 200 lines - extract logic if larger

### Backend
- Controllers should be thin - business logic in handlers
- Use dependency injection
- Implement proper validation (FluentValidation)
- Follow CQRS pattern (Commands/Queries)

### General
- **Modularize**: If a file exceeds 200 lines, extract logic
- **Audit the Chain**: Ensure changes propagate: Entity → DTO → Handler → API → Client
- Write clean, readable code with clear intent
- Add comments only when necessary to explain "why", not "what"

## 🤖 AI Agent Instructions

1. **Read this file first** before making changes
2. **Follow the architecture** - don't deviate without good reason
3. **Commit after changes** - always suggest git workflow
4. **Use proper types** - no shortcuts with `any`
5. **Test your changes** - ensure builds succeed
6. **Be consistent** - follow existing patterns in the codebase

## 🎯 Definition of Done

A feature is only "Done" when:
- ✅ Code builds without errors
- ✅ Follows naming conventions
- ✅ Uses proper TypeScript types (no `any`)
- ✅ Implements error handling
- ✅ Has been committed and pushed
- ✅ Passes health checks in Docker
- ✅ Follows security standards

---

*Based on Horizon Universal Architecture & Excellence Blueprint*
*Last Updated: February 2026*
