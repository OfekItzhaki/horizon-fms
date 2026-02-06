# 🛡️ Guardian AI: Product Strategy & Technical Blueprint

This document captures the distilled vision for **Guardian AI**—a revolutionary "Architecture-as-Code" governance product. It is designed to be the "Master Context" for the next AI agent you hire to build this.

## 🚀 The Million-Dollar Prompt (v2.0)
*Copy and paste this into an AI Agent (like Cursor, ChatGPT, or Antigravity) to begin construction.*

> "I want to build a SaaS platform called **'Guardian AI'**. Its mission is to solve 'AI-Generated Spaghetti Code' by enforcing strict, elite architectural patterns on human and AI developers.
>
> ### 🏗️ Core Stack (Non-Negotiable)
> - **Backend**: .NET 8 Web API, MediatR (CQRS), Entity Framework Core.
> - **Observability**: Serilog with **Seq** integration for beautiful structured logging.
> - **Frontend**: React (Vite), TypeScript, **TanStack Query** (Caching/State).
> - **Safety**: Global Exception Middleware (Zero `try-catch` in business logic).
> - **Database**: PostgreSQL (Dockerized).
> - **Contract**: Auto-generated NSwag clients (Single Source of Truth).
>
> ### 🛡️ The Guardian Feature Set
> 1. **The Auditor**: A service that consumes a project's `ARCHITECTURE.md` and uses an LLM to scan pull requests for architectural violations (e.g., 'Fat Controllers', 'Missing Interfaces').
> 2. **The Mentor Panel**: A UI that doesn't just show 'Errors' but provides a 1-minute summary explaining *why* a pattern is being enforced (teaching the dev).
> 3. **The Auto-Fixer**: A sub-agent that suggests a 'Refactored version' of the violating code, following the blueprint perfectly.
>
> ### 📜 Execution Rules
> - Read the provided `GUARDIAN_TECH_STANDARDS.md` (based on the Horizon project).
> - Use the **Clean Architecture** pattern (Domain -> Application -> Infrastructure -> API).
> - Build the system to be **Recursive**: Guardian AI must use its own engine to audit its own code during development.
>
> Start by creating the solution structure and a roadmap for the 'Auditor' engine."

---

## 💎 How to make this "Product Worthy"

To turn this into something people will pay for, we need to move beyond simple "linting":

### 1. The "Human-in-the-Loop" Mentorship
Instead of an error message like "Don't use ViewBag," Guardian AI should show a side-by-side comparison:
- **Your Code**: `ViewBag.Title = "Home";` (Unchecked, Error-prone)
- **Guardian Way**: `public class HomeVm { public string Title { get; set; } }` (Typed, Safe)
*Value: You are selling an automated Senior Architect.*

### 2. The "Agentic Guardrail" API
Build an API endpoint that *other AI agents* can call. 
- *Scenario*: A coding agent wants to submit code. It calls `Guardian.Validate(codeBlock)`.
- *Guardian Response*: `{"status": "fail", "reason": "Rule #3: Component too long. Split into sub-components."}`
*Value: You become the "Quality Control" layer for the entire AI coding industry.*

### 3. Integrated Observability (Seq)
Integrate **Seq** directly into the dash. When Guardian sees a crash in production (via the Global Middleware), it should automatically link the log from Seq to the exact line of code and suggest a fix.

### 4. Dynamic Blueprints (Learning Mode)
The Guardian shouldn't just be a static cop. It should observe your 'best' code. If you implement a clever performance fix, Guardian should ask: *"This is a great pattern. Should I add it to the ARCHITECTURE.md so I can enforce it elsewhere?"*

### 5. Multi-Agent Consensus
When auditing code, Guardian can use two different LLMs (e.g., Claude and Gemini) to 'debate' the architecture. If both agree there's a violation, it's a high-confidence error. This prevents 'dumb' AI mistakes.

---

## 🛠️ The "v1 Template" Standards
*These are the rules we established in the Horizon project. Use these as the first 'Law' for Guardian.*

1. **Thin Controllers**: No logic. Only `return await _mediator.Send(command);`.
2. **Standardized Responses**: Everything returns `ProblemDetails` on error.
3. **No Inline Styles**: 100% CSS/Tailwind extraction.
4. **Type-Safety Chain**: C# DTO -> NSwag -> TypeScript Interface.

## 🏁 Next Steps
When you open your other computer:
1. Create a new repository named `GuardianAI`.
2. Paste the **Master Prompt** into your AI assistant.
3. Clip this file and the `ARCHITECTURE.md` from Horizon as the starter context.

**You aren't just building a project; you're building the 'Building Inspector' for the AI Age.**
