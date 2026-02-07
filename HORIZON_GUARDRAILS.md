# 🛡️ Horizon Guardrails: The Architecture Enforcement Engine

**Tagline**: *"The Guardrails for AI-Generated Code"*

This document is the **Master Blueprint** for building **Horizon Guardrails**—a revolutionary product that automates and enforces elite architectural standards in software projects. It solves the "AI-Generated Spaghetti Code" problem by teaching, auditing, and auto-fixing violations in real-time.

---

## 🎯 The Problem We're Solving

### The AI Fatigue Crisis
- **Senior Devs**: Tired of correcting AI-generated "spaghetti code"
- **Junior Devs**: Don't know *why* we use MediatR, PostgreSQL, or Clean Architecture
- **The Agentic Future**: In 2 years, most code will be written by AI agents—they need **constraints**

**Horizon Guardrails** is the "Building Inspector" for the AI Age.

---

## 🚀 The Product: Three Pillars

### 1. **The Scanner** 📊
A CLI tool that "grades" a repository against architectural patterns (not just syntax).

**What it checks**:
- ❌ "Violation: Business logic found in Controller; should be in a Command Handler"
- ❌ "Violation: Component `Dashboard.tsx` is 400 lines; max is 200 (Rule #7)"
- ❌ "Violation: Using `any` type in error handler; violates type-safety rule"

**How it works**:
- Reads your project's `ARCHITECTURE.md`
- Uses an LLM to analyze code structure (not just regex)
- Outputs a "Quality Score" with specific violations

**Integration**: GitHub Action, CLI, or API endpoint

---

### 2. **The Fixer** 🔧
When a violation is found, Guardian triggers an AI sub-agent to auto-refactor and create a Pull Request.

**Example Flow**:
1. Scanner detects: "Fat Controller with business logic"
2. Fixer analyzes the code
3. Fixer creates:
   - New `CreateUserCommand.cs` (Handler)
   - Refactored `UsersController.cs` (thin)
   - Pull Request with explanation

**The Killer Feature**: The PR description includes a **"Why I Did This"** section (see below).

---

### 3. **The AI Prompt Forge** 🎨
Generates custom "System Prompts" for AI tools (Cursor, Copilot, ChatGPT) based on your `ARCHITECTURE.md`.

**Example Output**:
```markdown
You are a senior architect working on the Horizon FMS project.

CRITICAL RULES:
1. Controllers must be thin. Use MediatR for all business logic.
2. Never use `any` types. Use proper Error or unknown types.
3. Components must be under 200 lines. Extract sub-components if needed.
4. All API calls must use the auto-generated NSwag client (src/services/api-client.ts).

When you write code, follow these patterns exactly. If you violate a rule, stop and refactor immediately.
```

**Value**: Every AI agent working on your project becomes an "expert" in your architecture.

---

## 💎 The "Summary/Learning" Panel (The Mentorship Feature)

**This is what makes Horizon Guardrails a MENTOR, not just a linter.**

### Traditional Linter:
```
❌ Error: Don't use ViewBag (line 42)
```

### Horizon Guardrails:
```
🛡️ Violation Detected: ViewBag Usage (Rule #9)

📖 Why This Matters:
ViewBag is a dynamic object with no compile-time safety. If you rename a property
in the view, the code will compile but crash at runtime.

✅ The Guardian Way:
public class HomeViewModel {
    public string Title { get; set; }
}

🎓 What You'll Learn:
This pattern (strongly-typed ViewModels) is used in 847 other files in this project.
It prevents 90% of "NullReferenceException" bugs in production.

🔧 Auto-Fix Available: [Apply Fix] [Ignore Once] [Update Rule]
```

**Impact**: Developers feel like they're **learning**, not being replaced. Trust builds over time.

---

## 🌍 Why This is Realistic (Market Validation)

### 1. **Existing Market**: Boilerplate Starter Kits
- Products like *ShipFast* make $100k+/month selling Next.js boilerplates
- **Your Angle**: You're not selling a starter kit; you're selling **ongoing enforcement**

### 2. **The "Linter Gap"**
- ESLint checks syntax; Guardian checks **architecture**
- No existing tool can say: "This logic belongs in a Handler, not a Controller"

### 3. **The Enterprise Pain Point**
- Companies are **scared** of AI-generated code creating technical debt
- Horizon Guardrails = "Safe AI Adoption"

---

## 🎙️ The "Million Dollar" Prompt (Recursive Self-Enforcement)

**Use this prompt to build Horizon Guardrails itself:**

> "I want to build a platform called **'Horizon Guardrails'**. Its purpose is to automate and enforce 'Elite Fullstack Standards' in software projects.
>
> ### Step 1: The Design
> You are a Lead Software Architect. Create a project using the **Clean Architecture** pattern.
> - **Backend**: .NET 8, MediatR (CQRS), Serilog (Structured Logging), Global Exception Middleware
> - **Frontend**: React (Vite), TypeScript, TanStack Query, modular components (max 150 lines per file)
> - **Database**: PostgreSQL (Dockerized)
> - **Interface**: All communication via auto-generated NSwag API Client
>
> ### Step 2: The Core Feature
> The service must have a **'Blueprint Processor'** that:
> - Reads an `ARCHITECTURE.md` file
> - Uses an LLM (via API) to analyze a separate codebase for violations
> - Returns a structured report with violation details and suggested fixes
>
> ### Step 3: Self-Correction (CRITICAL)
> Write an initial `ARCHITECTURE.md` for **Horizon Guardrails itself**.
> Every line of code you write for Guardian must strictly follow its own 'Elite Standards.'
> If you find yourself writing a 'Fat Controller' or inline styles, **stop and refactor immediately**.
>
> ### Step 4: The CLI
> Build a simple CLI tool that can be used in a GitHub Action to report violations back to the Guardian API.
>
> Start by defining the solution structure and the initial ARCHITECTURE.md for Horizon Guardrails itself."

---

## 🛠️ The "v1 Template" (Horizon FMS Standards)

Use the `ARCHITECTURE.md` from the Horizon FMS project as the **first ruleset** for Horizon Guardrails.

**Key Rules**:
1. **Thin Controllers**: No logic. Only `return await _mediator.Send(command);`
2. **Global Error Handling**: No `try-catch` in Controllers; use middleware
3. **Type Safety**: No `any` types; use proper Error or unknown
4. **Component Modularity**: Max 200 lines per file
5. **Single Source of Truth API**: Auto-generated NSwag client
6. **Container-First**: All services in `docker-compose.yml`
7. **Observability**: Seq for structured logging, `/health` endpoint

---

## 🏁 Next Steps

### Phase 1: Build the Scanner (MVP)
1. Create a new repo: `guardian-guardrails`
2. Build a CLI that reads `ARCHITECTURE.md` and scans a single file
3. Use an LLM API (OpenAI, Anthropic) to detect violations
4. Output a simple report

### Phase 2: Add the Mentorship Panel
1. Build a React dashboard
2. Show violations with "Why This Matters" explanations
3. Add "Auto-Fix" button that generates refactored code

### Phase 3: The Fixer (Auto-PR)
1. Integrate with GitHub API
2. Auto-create PRs with refactored code
3. Include "Summary/Learning" in PR description

### Phase 4: The Prompt Forge
1. Generate custom system prompts from `ARCHITECTURE.md`
2. Provide integration guides for Cursor, Copilot, ChatGPT

---

## 📖 Architecture Decision Records (Horizon Guardrails)

- **ADR 001: Name**: Chose "Horizon Guardrails" for alliteration, memorability, and clear value proposition
- **ADR 002: LLM Strategy**: Use multi-agent consensus (2+ LLMs) to reduce false positives
- **ADR 003: Mentorship Over Enforcement**: Focus on teaching "why" to build trust with developers
- **ADR 004: Recursive Self-Enforcement**: Horizon Guardrails must use its own engine to audit itself during development

---

**You aren't just building a linter. You're building the "Senior Architect" that every team wishes they had.**

*Created February 2026 | Powered by the Horizon FMS Architecture*
