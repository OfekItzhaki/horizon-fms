# 🏆 The Horizon Standard: Universal Architecture & Excellence Blueprint

This document defines **The Horizon Standard** - a universal set of architectural principles and coding standards for **all software projects**, regardless of platform or technology. It is intended for both human developers and AI agents to ensure consistency, scalability, and high code quality across any codebase.

**The Horizon Standard applies to:**
- Web applications (React, Vue, Angular, etc.)
- Backend services (.NET, Node.js, Python, etc.)
- Mobile applications (React Native, Flutter, etc.)
- Desktop applications
- Any software project that values quality and maintainability

---

## 🏗️ Architectural Pillars

These principles apply to **any project** adopting The Horizon Standard:

### 1. The "Single Source of Truth" API
- **Tooling**: Use **NSwag**, **OpenAPI**, or equivalent code generation tools for your stack.
- **Models & Structure**: Auto-generate client code from API definitions. Never manually define these in the frontend.
- **Rule**: Whenever the backend contracts change, re-run the client generator.
- **Benefit**: Zero "type mismatch" bugs.
- **Applies to**: REST APIs, GraphQL, gRPC, or any API-first architecture.

### 2. Standardized Error Handling
- **Backend**: Use a global error handling middleware. No scattered `try-catch` blocks unless for specific logic.
- **Responses**: Return standardized error formats (e.g., `ProblemDetails` RFC 7807, or equivalent for your stack).
- **Frontend**: Use a central notification system to display errors consistently.
- **Applies to**: Any client-server architecture.

### 3. Container-First & Infrastructure-as-Code
- **Docker**: Every core dependency (API, Web, DB, Cache) should be containerized.
- **Environment**: Use `.env` files for configuration. Use infrastructure-as-code tools (Docker Compose, Kubernetes, Terraform, etc.).
- **Rules**: Local dev must be "Plug & Play" with a single helper script that handles setup and service startup.
- **Applies to**: Any project with multiple services or dependencies.

### 3.1 Environment Variables & Configuration Management
- **Never Hardcode**: No production URLs, API keys, or environment-specific values in code.
- **Environment Detection**: Applications must detect their environment (development, staging, production) and fail fast if required configuration is missing.
- **Validation Pattern**: Validate environment variables at application startup:
```typescript
// Example: TypeScript/Node.js
function getConfig() {
  const apiUrl = import.meta.env.VITE_API_URL || process.env.API_URL;
  
  if (import.meta.env.PROD && !apiUrl) {
    throw new Error('Production environment detected but API_URL not configured');
  }
  
  return { apiUrl };
}
```
- **`.env.example` Files**: Every project must include `.env.example` with:
  - All required environment variables
  - Example values for local development
  - Comments explaining where to get production values
  - Clear separation between local/staging/production configs
- **Deployment Platforms**: Configure environment variables in platform dashboards:
  - Vercel: Project Settings → Environment Variables
  - Netlify: Site Settings → Environment Variables
  - Render: Service → Environment
  - AWS: Parameter Store, Secrets Manager
  - Azure: App Configuration, Key Vault
- **Applies to**: Any application with environment-specific configuration.

### 4. Background Job & Multi-Channel Delivery
- **Offloading**: Never perform slow operations (Email, external API sync, heavy processing) in the request-response cycle.
- **Tools**: Use job queues appropriate for your stack (BullMQ for Node.js, Hangfire for .NET, Celery for Python, etc.).
- **Reliability**: Jobs should be retriable and traceable.
- **Fallback**: Implement multi-channel defaults (e.g., WebSocket for real-time, Email for fallback).
- **Applies to**: Any application with async operations or background processing.

### 5. Resilient Session Management
- **UX Requirement**: Users should never be kicked out due to expired short-lived tokens.
- **Pattern**: Implement a 401 Interceptor that triggers an automatic renewal (refresh token) and retries the original request seamlessly.

### 6. Universal State & Caching
- **Standard**: Always use a robust data-fetching library with built-in caching.
- **Examples**: `@tanstack/react-query` (React), SWR (React), Apollo Client (GraphQL), RTK Query (Redux), etc.
- **Benefit**: Ensures a "snappy" UI with built-in optimistic updates and automated background refetching.
- **Parity**: All frontends (Web, Mobile) MUST adopt the same caching logic.
- **Applies to**: Any application with API data fetching.

### 7. Real-time Communication & Presence
- **Technology**: Use appropriate real-time tech for your stack (Socket.IO, SignalR, WebSockets, Server-Sent Events, etc.).
- **Pattern**: Implement room-based/hub-based communication for scoped updates.
- **Mobile Integration**: Use singleton instances for persistent connections across screens.
- **Fallback**: Always ensure REST APIs are available as fallback for critical operations.
- **Applies to**: Any application requiring real-time updates.

### 8. Observability & Health Monitoring
- **Structured Logging**: All logs must be structured (JSON format with contextual properties).
- **Log Aggregation**: Use centralized logging (Seq, ELK Stack, Datadog, CloudWatch, etc.).
- **Health Checks**: Implement `/health` endpoints for orchestration and monitoring.
- **Transient Fault Handling**: Implement retries and circuit breakers for infrastructure dependencies.
- **Persistence Strategy**: All infrastructure data (DB, Cache, Logs) must persist across restarts.
- **Applies to**: Any production application.

### 8.1 Logging Standards & Sensitive Data Protection
- **Structured Format**: Use JSON logging with contextual properties:
```json
{
  "timestamp": "2026-02-09T10:30:00Z",
  "level": "info",
  "message": "User login successful",
  "userId": "user-123",
  "ip": "192.168.1.1",
  "userAgent": "Mozilla/5.0..."
}
```
- **CRITICAL - Never Log Sensitive Data**:
  - ❌ **NEVER log**: Passwords, tokens, API keys, session IDs, credit card numbers, SSNs, OTPs, security answers
  - ❌ **NEVER log**: Full email addresses in production (use hashed or masked versions)
  - ❌ **NEVER log**: Personal Identifiable Information (PII) without explicit consent and legal basis
  - ✅ **Safe to log**: User IDs (non-sensitive identifiers), timestamps, IP addresses (with privacy policy), error codes, request paths
- **Masking Pattern**: If you must log potentially sensitive data for debugging:
```typescript
// Bad
logger.info('User login', { email: user.email, password: password });

// Good
logger.info('User login', { 
  userId: user.id, 
  emailDomain: user.email.split('@')[1] // Only log domain, not full email
});
```
- **Code Review Checklist**: Before committing, search codebase for:
  - `console.log` statements (remove or replace with proper logger)
  - Logging of request bodies without sanitization
  - Debug logs that expose tokens or credentials
- **Compliance**: Logging sensitive data may violate GDPR, CCPA, HIPAA, and other privacy regulations.
- **Applies to**: Any application that handles user data.

### 9. Pluggable Storage Abstraction
- **Abstraction**: Applications must interact with storage via an interface (e.g., `IStorageService`, `StorageAdapter`) rather than direct filesystem/cloud calls.
- **Hybrid Support**: The architecture should support multiple providers (Local Disk, S3, Azure Blob, Cloudinary, etc.) switchable via configuration.
- **Path Resolution**: Use a centralized resolver to handle transitions between relative local paths and absolute production URLs.
- **Applies to**: Any application handling file storage.

### 10. Implementation Excellence & Patterns
- **Standardized Onboarding**: Complex flows must be broken into discrete, verifiable steps.
- **Tooling Automation**: Repetitive developer tasks (setup, verification, seeding) MUST be scripted.
- **Dynamic Infrastructure**: Setup scripts should handle environmental conflicts automatically (port allocation, service health checks, etc.).
- **Self-Healing**: Scripts should clear zombie services/orphans before launch.
- **Applies to**: Any project with complex setup or multiple services.

---

## 🚀 DevOps Workflow Patterns

### Northern Workflow (Build & Test)
- **Goal**: Code quality, formatting, and logical correctness.
- **Tools**: IDE, Linters (ESLint, Prettier, etc.), CI/CD (GitHub Actions, GitLab CI, Jenkins, etc.).
- **Rule**: Never merge if formatting checks, linting, or tests fail.
- **Pre-Commit Checklist**:
```bash
# Frontend (TypeScript/React/Vue/etc.)
npm run lint          # Check for linting errors
npm run lint -- --fix # Auto-fix linting errors
npm run build         # Ensure build succeeds

# Backend (.NET)
dotnet build          # Ensure build succeeds
dotnet test           # Run all tests

# Backend (Node.js)
npm run lint
npm run build
npm test

# Backend (Python)
flake8 .              # Linting
pytest                # Tests
```

### Southern Workflow (Docker & Deploy)
- **Goal**: Environment parity and deployment reliability.
- **Rules**: A feature is only "Done" when it passes health checks in the container mesh. All infrastructure MUST be ephemeral-ready.
- **Applies to**: Any containerized application.

### Automated Pre-Commit Testing
- **Goal**: Catch errors before they reach CI/CD, ensuring code quality at commit time.
- **Implementation**: Use Git hooks (Husky) with lint-staged to automatically run tests on changed files.
- **What Runs Automatically**:
  - **Linting**: Auto-fix code style issues
  - **Formatting**: Apply Prettier formatting
  - **Testing**: Run tests for modified files only (`--findRelatedTests`)
- **Benefits**:
  - Prevents broken code from being committed
  - Catches test failures immediately
  - Faster feedback loop than waiting for CI/CD
  - Reduces CI/CD failures and build times
- **Configuration Example** (package.json):
```json
"lint-staged": {
  "src/**/*.{ts,tsx}": [
    "prettier --write",
    "eslint --fix",
    "npm test -- --bail --findRelatedTests"
  ]
}
```
- **Applies to**: Any project with automated testing

---

## 🌿 Git & Collaboration

### Git Tagging & Semantic Versioning
- **Versioning Standard**: Follow **Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`.
  - **MAJOR**: Breaking changes, incompatible API changes
  - **MINOR**: New features, backward-compatible
  - **PATCH**: Bug fixes, backward-compatible
- **Automated Management**: Use automation tools to manage versions and changelogs:
  - **Node.js**: semantic-release, standard-version
  - **.NET**: GitVersion, Nerdbank.GitVersioning
  - **Universal**: Google's Release Please, conventional-changelog
- **Rule**: Never manually edit `CHANGELOG.md` files managed by automation.

### Commit & PR Strategy
- **Atomic Commits**: Each commit should represent a single logical change. Commits should be kept short and broken into smaller commits by features. If changes are related or dependent, they should be committed together.
- **Conventional Commits**: Use the `type(scope): description` format
  - **Types**:
    - `feat`: New feature
    - `fix`: Bug fix
    - `chore`: Maintenance tasks (dependencies, config)
    - `refactor`: Code restructuring without behavior change
    - `docs`: Documentation changes
    - `style`: Formatting, whitespace (no code change)
    - `test`: Adding or updating tests
    - `perf`: Performance improvements
    - `ci`: CI/CD changes
    - `build`: Build system changes
  - **Examples**:
    - `feat(ui): add dark mode support`
    - `fix(api): resolve file upload timeout issue`
    - `chore(deps): update dependencies`
    - `refactor(auth): simplify token validation logic`
    - `docs(readme): add installation instructions`
- **Squash Merge**: Default for feature branches to keep history clean.
- **Pre-Commit Requirements**:
  1. Run linting and auto-fix: `npm run lint -- --fix` (Frontend)
  2. Ensure builds succeed: `npm run build` or `dotnet build`
  3. Run tests: `npm test` or `dotnet test`
  4. Stage changes: `git add .`
  5. Commit with conventional format: `git commit -m "type(scope): description"`
  6. Push to remote: `git push`

### Code Review Standards
- **Review Checklist**:
  - ✅ Code follows project conventions and standards
  - ✅ No hardcoded secrets or sensitive data
  - ✅ Proper error handling implemented
  - ✅ Tests included for new functionality
  - ✅ Documentation updated if needed
  - ✅ No unnecessary complexity
  - ✅ Performance considerations addressed
  - ✅ Security best practices followed
- **Review Etiquette**:
  - Be constructive and respectful
  - Explain the "why" behind suggestions
  - Distinguish between blocking issues and suggestions
  - Approve when standards are met, even if you'd do it differently

### Branch Strategy
- **Main/Master**: Production-ready code only
- **Develop**: Integration branch for features
- **Feature Branches**: `feature/description` or `feat/description`
- **Bugfix Branches**: `fix/description` or `bugfix/description`
- **Hotfix Branches**: `hotfix/description` for urgent production fixes
- **Release Branches**: `release/version` for release preparation

### Branch Protection Rules
Protect your main branches from accidental or unauthorized changes:

**Main/Master Branch Protection**:
- ✅ Require pull request reviews before merging (minimum 1 approval)
- ✅ Require status checks to pass (CI/CD, tests, linting)
- ✅ Require branches to be up to date before merging
- ✅ Require conversation resolution before merging
- ✅ Restrict who can push to matching branches (admins only)
- ✅ Require signed commits (optional but recommended)
- ❌ Do not allow force pushes
- ❌ Do not allow deletions

**Setup Instructions**:
- **GitHub**: Repository Settings → Branches → Add branch protection rule
- **GitLab**: Repository Settings → Protected Branches
- **Bitbucket**: Repository Settings → Branch Permissions
- **Azure DevOps**: Project Settings → Repositories → Policies

**Recommended Rules**:
```yaml
# Example: GitHub branch protection
branches:
  main:
    protection:
      required_pull_request_reviews:
        required_approving_review_count: 1
        dismiss_stale_reviews: true
      required_status_checks:
        strict: true
        contexts:
          - "build"
          - "test"
          - "lint"
      enforce_admins: true
      restrictions: null
```

### Git Branch Management

Comprehensive guide for managing branches locally and remotely.

#### Viewing Branches
```bash
# List local branches
git branch

# List remote branches
git branch -r

# List all branches (local + remote)
git branch -a

# Show current branch
git branch --show-current

# Show branches with last commit
git branch -v

# Show merged branches
git branch --merged

# Show unmerged branches
git branch --no-merged
```

#### Creating & Switching Branches
```bash
# Create new branch
git branch feature/new-feature

# Create and switch to new branch
git checkout -b feature/new-feature

# Modern syntax (Git 2.23+)
git switch -c feature/new-feature

# Create branch from specific commit
git branch feature/new-feature abc123

# Switch to existing branch
git checkout feature/new-feature
git switch feature/new-feature  # Modern syntax
```

#### Renaming Branches
```bash
# Rename current branch
git branch -m new-branch-name

# Rename specific branch
git branch -m old-name new-name

# Rename and update remote
git branch -m old-name new-name
git push origin :old-name new-name
git push origin -u new-name
```

#### Deleting Branches

**Local Branches**:
```bash
# Delete merged branch (safe)
git branch -d feature/completed

# Force delete unmerged branch (careful!)
git branch -D feature/abandoned

# Delete multiple branches
git branch -d feature/one feature/two feature/three
```

**Remote Branches**:
```bash
# Delete remote branch
git push origin --delete feature/old-feature

# Alternative syntax
git push origin :feature/old-feature

# Delete local tracking branch after remote deletion
git fetch --prune
git fetch -p  # Short form
```

#### Cleaning Up Branches
```bash
# Remove local branches that no longer exist on remote
git fetch --prune

# Delete all merged branches except main/master
git branch --merged | grep -v "\*\|main\|master" | xargs -n 1 git branch -d

# Windows PowerShell version
git branch --merged | Select-String -NotMatch "\*|main|master" | ForEach-Object { git branch -d $_.ToString().Trim() }

# Interactive cleanup (shows what will be deleted)
git branch --merged | grep -v "\*\|main\|master"
```

#### Working with Remote Branches
```bash
# Fetch all remote branches
git fetch --all

# Track remote branch
git checkout --track origin/feature/remote-feature

# Create local branch from remote
git checkout -b feature/local-name origin/feature/remote-name

# Push local branch to remote
git push -u origin feature/new-feature

# Update remote tracking
git branch -u origin/feature/name

# Show remote tracking branches
git branch -vv
```

#### Rollback Strategies

**Revert (Safe - Creates New Commit)**:
```bash
# Revert last commit (keeps history)
git revert HEAD

# Revert specific commit
git revert abc123

# Revert multiple commits
git revert abc123..def456

# Revert without committing (stage changes)
git revert -n HEAD
```

**Reset (Destructive - Rewrites History)**:
```bash
# Soft reset (keep changes staged)
git reset --soft HEAD~1

# Mixed reset (keep changes unstaged) - DEFAULT
git reset HEAD~1
git reset --mixed HEAD~1

# Hard reset (discard all changes) - CAREFUL!
git reset --hard HEAD~1

# Reset to specific commit
git reset --hard abc123

# Reset to remote state
git reset --hard origin/main
```

**When to Use Each**:
- **Revert**: When commits are already pushed to shared branches (preserves history)
- **Reset --soft**: When you want to recommit with different message/changes
- **Reset --mixed**: When you want to unstage changes but keep them
- **Reset --hard**: When you want to completely discard changes (use with caution!)

#### Main Branch Protection & Backup

**Before Risky Operations**:
```bash
# Create backup branch
git branch backup/main-$(date +%Y%m%d)

# Or with timestamp
git branch backup/main-20260209-1030

# Tag important states
git tag -a v1.0.0-backup -m "Backup before major refactor"
```

**Recovering from Mistakes**:
```bash
# View reflog (history of HEAD movements)
git reflog

# Restore to previous state
git reset --hard HEAD@{2}

# Recover deleted branch
git checkout -b recovered-branch HEAD@{5}

# Restore specific file from history
git checkout abc123 -- path/to/file.txt
```

#### Recommended Workflows

**Feature Development Workflow**:
```bash
# 1. Start from updated main
git checkout main
git pull origin main

# 2. Create feature branch
git checkout -b feature/new-feature

# 3. Make changes and commit
git add .
git commit -m "feat(scope): add new feature"

# 4. Push to remote
git push -u origin feature/new-feature

# 5. Create pull request (via GitHub/GitLab UI)

# 6. After merge, cleanup
git checkout main
git pull origin main
git branch -d feature/new-feature
git push origin --delete feature/new-feature
```

**Hotfix Workflow**:
```bash
# 1. Create hotfix from main
git checkout main
git pull origin main
git checkout -b hotfix/critical-bug

# 2. Fix and commit
git add .
git commit -m "fix(critical): resolve security issue"

# 3. Push and create PR
git push -u origin hotfix/critical-bug

# 4. After merge to main, also merge to develop
git checkout develop
git merge main
git push origin develop

# 5. Cleanup
git branch -d hotfix/critical-bug
git push origin --delete hotfix/critical-bug
```

**Syncing Fork with Upstream**:
```bash
# 1. Add upstream remote (once)
git remote add upstream https://github.com/original/repo.git

# 2. Fetch upstream changes
git fetch upstream

# 3. Merge upstream into your main
git checkout main
git merge upstream/main

# 4. Push to your fork
git push origin main
```

#### Best Practices

**Branch Naming**:
- Use descriptive names: `feature/user-authentication` not `feature/fix`
- Include ticket numbers: `feature/JIRA-123-add-login`
- Use consistent prefixes: `feature/`, `fix/`, `hotfix/`, `refactor/`, `docs/`
- Use kebab-case: `feature/add-user-profile` not `feature/AddUserProfile`

**Branch Lifecycle**:
- Keep branches short-lived (days, not weeks)
- Merge or delete stale branches regularly
- Rebase feature branches on main frequently to avoid conflicts
- Delete branches after merging (both local and remote)

**Collaboration**:
- Never force push to shared branches
- Communicate before deleting shared branches
- Use pull requests for code review
- Keep commits atomic and well-described

#### Useful Git Aliases

Add these to your `~/.gitconfig`:
```ini
[alias]
  # Branch management
  br = branch
  co = checkout
  sw = switch
  cb = checkout -b
  
  # Branch cleanup
  cleanup = "!git branch --merged | grep -v '\\*\\|main\\|master' | xargs -n 1 git branch -d"
  prune-all = fetch --prune --all
  
  # Branch info
  branches = branch -a
  current = branch --show-current
  recent = branch --sort=-committerdate
  
  # Rollback helpers
  undo = reset --soft HEAD~1
  unstage = reset HEAD
  discard = checkout --
  
  # History
  hist = log --pretty=format:'%h %ad | %s%d [%an]' --graph --date=short
  last = log -1 HEAD --stat
  
  # Status shortcuts
  st = status -sb
  s = status
```

**Usage**:
```bash
git cb feature/new-feature  # Create and checkout
git recent                  # Show recent branches
git cleanup                 # Delete merged branches
git undo                    # Undo last commit (keep changes)
```

---

## 🛡️ Security & Performance Standards

### Security Headers
Every project must implement:
- **CSP (Content-Security-Policy)**: Prevent XSS attacks
- **X-Frame-Options: DENY**: Prevent clickjacking
- **X-Content-Type-Options: nosniff**: Prevent MIME sniffing
- **Referrer-Policy: no-referrer**: Protect user privacy
- **Strict-Transport-Security**: Enforce HTTPS
- **Rate Limiting**: IP-based rate limiting to prevent abuse

### Security Best Practices
- **Authentication**: Use industry-standard auth (OAuth 2.0, JWT, etc.)
- **Authorization**: Implement role-based or attribute-based access control
- **Input Validation**: Validate and sanitize all user inputs
- **SQL Injection**: Use parameterized queries or ORMs
- **XSS Prevention**: Escape output, use Content Security Policy
- **CSRF Protection**: Implement CSRF tokens for state-changing operations
- **Secrets Management**: Never commit secrets, use environment variables or secret managers
- **Dependencies**: Regularly update dependencies and scan for vulnerabilities
- **Logging**: Never log sensitive data (passwords, tokens, PII)

### CAPTCHA & Bot Protection
- **Purpose**: Prevent automated abuse, spam, and bot attacks on public endpoints.
- **Recommended Solution**: Cloudflare Turnstile (privacy-friendly, no visual puzzles for most users).
- **Implementation Pattern**:
  - **Frontend**: Include CAPTCHA widget on sensitive forms (login, registration, password reset).
  - **Backend**: Verify CAPTCHA token on server-side before processing requests.
  - **Graceful Degradation**: If CAPTCHA service is unavailable, log errors but don't block legitimate users (or implement fallback rate limiting).
- **Development vs Production**:
  - **Development**: Use test keys that always pass validation (e.g., Cloudflare's `1x00000000000000000000AA`).
  - **Production**: Use real keys from CAPTCHA provider dashboard.
- **Configuration**:
  - Frontend: `VITE_TURNSTILE_SITE_KEY` or equivalent (public key, safe to expose).
  - Backend: `TURNSTILE_SECRET_KEY` or equivalent (private key, never expose).
- **Best Practices**:
  - Only apply CAPTCHA to high-risk endpoints (login, registration, contact forms).
  - Don't apply CAPTCHA to every API call (degrades UX).
  - Combine with rate limiting for defense in depth.
  - Monitor CAPTCHA solve rates to detect issues.
- **Applies to**: Any application with public-facing forms or authentication endpoints.

### Performance Standards
- **Response Times**:
  - API endpoints: < 200ms for simple queries, < 1s for complex operations
  - Page load: < 3s for initial load, < 1s for subsequent navigation
- **Caching**: Implement appropriate caching strategies
- **Database**: Use indexes, optimize queries, implement connection pooling
- **Assets**: Minify and compress static assets, use CDN
- **Lazy Loading**: Load resources on-demand when appropriate
- **Monitoring**: Track performance metrics and set up alerts

---

## 📚 Documentation Standards

### README.md Requirements
Every project must have a comprehensive README with:
1. **Project Overview**: What the project does and why it exists
2. **Prerequisites**: Required tools, versions, and dependencies
3. **Installation**: Step-by-step setup instructions
4. **Configuration**: Environment variables and configuration options
5. **Usage**: How to run the project locally
6. **Testing**: How to run tests
7. **Deployment**: How to deploy to production
8. **Contributing**: Guidelines for contributors
9. **License**: Project license information

### Code Documentation
- **Functions/Methods**: Document complex logic, parameters, and return values
  - TypeScript: Use JSDoc comments
  - C#: Use XML documentation comments
  - Python: Use docstrings
  - Java: Use Javadoc
- **Classes**: Document purpose and responsibilities
- **APIs**: Use OpenAPI/Swagger for API documentation
- **Architecture**: Maintain architecture diagrams and ADRs

### Documentation Tools
- **API Docs**: Swagger/OpenAPI, Postman, Insomnia
- **Code Docs**: JSDoc, TSDoc, Sphinx, Doxygen
- **Diagrams**: Mermaid, PlantUML, Draw.io, Lucidchart
- **Wiki**: GitHub Wiki, Confluence, Notion

---

## 📜 Naming Conventions & Style

### TypeScript / React
- **Components**: PascalCase (e.g., `FileList`, `Dashboard`)
- **Files**: Match component name (e.g., `FileList.tsx`)
- **Hooks**: Start with `use` (e.g., `useTheme`, `useAuth`)
- **Types/Interfaces**: PascalCase (e.g., `FileItemDto`, `UserProfile`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `API_BASE_URL`, `MAX_FILE_SIZE`)
- **Functions/Variables**: camelCase (e.g., `handleSubmit`, `isLoading`)

### C# / .NET
- **Namespaces**: Use file-scoped namespaces (C#) and consistent directory structures.
- **Interfaces**: Start with `I` (e.g., `IStorageService`, `IRepository`).
- **Async**: Methods must end in `Async` (e.g., `GetFileAsync`, `SaveAsync`).
- **Classes**: PascalCase (e.g., `FileRepository`, `UserService`)
- **Private Fields**: _camelCase with underscore prefix (e.g., `_dbContext`, `_logger`)
- **Properties**: PascalCase (e.g., `FileName`, `CreatedDate`)

### General Rules
- **No `any` types**: Always use proper TypeScript types
- **Descriptive names**: Use meaningful, self-documenting names
- **Avoid abbreviations**: Unless widely understood (e.g., `id`, `url`, `api`)
- **Consistency**: Follow existing patterns in the codebase

---

## 📖 Architecture Decision Records (ADR)

Projects should document project-specific ADRs separately. **The Horizon Standard** provides recommendations for common architectural decisions:

### Database Selection
- **Relational**: PostgreSQL (preferred), MySQL, SQL Server, Oracle
  - Use for: Transactional data, complex relationships, ACID requirements
- **Document**: MongoDB, CouchDB, DynamoDB
  - Use for: Flexible schemas, hierarchical data, high write throughput
- **Key-Value**: Redis, Memcached
  - Use for: Caching, session storage, real-time data
- **Time-Series**: InfluxDB, TimescaleDB
  - Use for: Metrics, logs, IoT data
- **Graph**: Neo4j, ArangoDB
  - Use for: Social networks, recommendation engines, knowledge graphs

### Observability Stack
- **Logging**:
  - Structured logging (JSON format with context)
  - Tools: Seq, ELK Stack (Elasticsearch, Logstash, Kibana), Datadog, Splunk, CloudWatch
- **Metrics**: Prometheus, Grafana, Application Insights, New Relic
- **Tracing**: Jaeger, Zipkin, OpenTelemetry
- **Health Checks**: Implement `/health` and `/ready` endpoints

### Caching Strategy
- **Distributed**: Redis (preferred), Memcached
  - Use for: Horizontally scalable services, shared cache
- **In-Memory**: Built-in caching (MemoryCache, etc.)
  - Use for: Single-instance applications, simple caching needs
- **CDN**: CloudFlare, AWS CloudFront, Azure CDN
  - Use for: Static assets, global distribution

### Message Queues & Event Streaming
- **Message Queues**: RabbitMQ, AWS SQS, Azure Service Bus
  - Use for: Task queues, async processing, decoupling services
- **Event Streaming**: Apache Kafka, AWS Kinesis, Azure Event Hubs
  - Use for: Event sourcing, real-time analytics, high-throughput events
- **Pub/Sub**: Redis Pub/Sub, Google Pub/Sub, MQTT
  - Use for: Real-time notifications, lightweight messaging

### API Architecture
- **REST**: Standard HTTP APIs with JSON
  - Use for: CRUD operations, public APIs, simple integrations
- **GraphQL**: Query language for APIs
  - Use for: Complex data requirements, mobile apps, flexible queries
- **gRPC**: High-performance RPC framework
  - Use for: Microservices communication, high-performance needs
- **WebSockets/SignalR**: Real-time bidirectional communication
  - Use for: Chat, notifications, live updates

### Authentication & Authorization
- **JWT**: JSON Web Tokens for stateless auth
- **OAuth 2.0**: For third-party integrations
- **OpenID Connect**: For identity layer on top of OAuth 2.0
- **Session-based**: For traditional web applications
- **API Keys**: For service-to-service communication

### Storage Solutions
- **Object Storage**: AWS S3, Azure Blob Storage, Google Cloud Storage, Cloudinary
  - Use for: Files, images, videos, backups
- **File System**: Local disk, NFS, SMB
  - Use for: Development, small-scale deployments
- **CDN**: For global content delivery

### Background Jobs
- **Node.js**: BullMQ, Agenda, Bee-Queue
- **.NET**: Hangfire, Quartz.NET
- **Python**: Celery, RQ (Redis Queue)
- **Java**: Quartz, Spring Batch

---

## 🧪 Testing Standards

### Unit Testing
- **Coverage**: Aim for 80%+ code coverage on business logic
- **Naming**: Test names should describe what is being tested and expected outcome
  - Example: `test_user_login_with_invalid_credentials_returns_401`
  - Example: `should_throw_error_when_file_not_found`
- **Arrange-Act-Assert**: Follow AAA pattern for test structure
- **Isolation**: Tests should be independent and not rely on execution order

### Integration Testing
- **API Tests**: Test all API endpoints with various scenarios
- **Database Tests**: Use test databases or in-memory databases
- **External Services**: Mock external dependencies appropriately

### End-to-End Testing
- **Critical Paths**: Test main user journeys
- **Tools**: Use appropriate E2E tools (Playwright, Cypress, Selenium, etc.)
- **CI Integration**: E2E tests should run in CI pipeline

### Testing Tools by Stack
- **JavaScript/TypeScript**: Jest, Vitest, Mocha, Chai, Testing Library
- **.NET**: xUnit, NUnit, MSTest, Moq, FluentAssertions
- **Python**: pytest, unittest, mock
- **Java**: JUnit, Mockito, TestNG

---

## ✅ Gold Standard Verification

### Frontend (TypeScript/React/Vue/Angular)
1. **Zero-Error Build**: Build command succeeds without errors
   - React/Vue: `npm run build`
   - Angular: `ng build`
2. **Zero `any` Types**: No `any` types in codebase (except auto-generated files)
3. **Linting Passes**: Linting returns 0 errors
   - ESLint: `npm run lint`
   - TSLint: `tslint --project .`
4. **Formatted Code**: Code formatted with Prettier or equivalent
   - Auto-fix: `npm run lint -- --fix` or `prettier --write .`
5. **Type Safety**: All props, state, and API responses properly typed
6. **Tests Pass**: All unit and integration tests passing
   - Jest/Vitest: `npm test`
   - Karma: `ng test`

### Backend (.NET)
1. **Zero-Error Build**: `dotnet build` succeeds without errors
2. **Tests Pass**: `dotnet test` all tests passing
3. **No Warnings**: Build produces no warnings (or only acceptable warnings)
4. **Proper DI**: All dependencies injected via constructor
5. **CQRS Pattern**: Commands and Queries properly separated (if applicable)
6. **Validation**: FluentValidation or Data Annotations properly implemented

### Backend (Node.js)
1. **Zero-Error Build**: `npm run build` or `tsc` succeeds without errors
2. **Tests Pass**: `npm test` all tests passing
3. **Linting Passes**: `npm run lint` returns 0 errors
4. **Type Safety**: Proper TypeScript types throughout
5. **Error Handling**: Centralized error middleware implemented

### Backend (Python)
1. **Zero-Error Build**: No syntax errors, imports resolve
2. **Tests Pass**: `pytest` all tests passing
3. **Linting Passes**: `flake8 .` or `pylint` returns 0 errors
4. **Type Hints**: Type hints used throughout (checked with `mypy`)
5. **Formatting**: Code formatted with `black` or `autopep8`

### Infrastructure
1. **Stable Infrastructure**: Health checks green for all services
2. **Container Orchestration**: All services start successfully
   - Docker Compose: `docker-compose up`
   - Kubernetes: `kubectl get pods` all running
3. **Environment Parity**: Local dev matches production architecture
4. **Logs Structured**: All logs in JSON format with context
5. **Secrets Management**: No hardcoded secrets, using environment variables or secret managers

### General
1. **Standardized Formatting**: Ruleset-compliant project-wide
2. **Conventional Commits**: All commits follow `type(scope): description` format
3. **Documentation**: README and architecture docs up to date
4. **Security**: Security headers and best practices implemented
5. **Performance**: No obvious performance bottlenecks or anti-patterns

---

## 🤖 AI Agent Instructions

### Before Starting Work
1. **Read the Blueprint**: Always check this file first to understand the standards
2. **Check Steering Files**: Review `.kiro/steering/` or equivalent for project-specific rules
3. **Understand the Stack**: Identify the technology stack (Frontend framework, Backend language, Database, etc.)
4. **Review Existing Code**: Look at existing patterns and conventions in the codebase
5. **Check Dependencies**: Understand what libraries and tools are already in use

### During Development
1. **Follow Patterns**: Use existing code patterns as reference - consistency is key
2. **Type Everything**:
   - TypeScript: No `any` types - use proper types
   - Python: Use type hints
   - C#: Use strong typing
   - Java: Use generics appropriately
3. **Modularize First**: If a file exceeds 200 lines, extract logic into smaller modules
4. **Audit the Chain**: Ensure changes propagate correctly:
   - Backend: Entity → DTO → Handler/Service → API → Client
   - Frontend: API Client → State Management → Component → UI
5. **Test Locally**: Run builds and tests before committing
6. **Error Handling**: Implement proper error handling at every layer
7. **Security First**: Never commit secrets, always validate inputs, follow security best practices

### Before Committing

**CRITICAL**: Always run these checks before committing:

#### Frontend (JavaScript/TypeScript)
```bash
# React/Vue/Angular
npm run lint -- --fix  # Auto-fix linting errors
npm run build          # Ensure build succeeds
npm test               # Run tests (if applicable)
```

#### Backend (.NET)
```bash
dotnet build           # Ensure build succeeds
dotnet test            # Run all tests
```

#### Backend (Node.js)
```bash
npm run lint -- --fix  # Auto-fix linting errors
npm run build          # Compile TypeScript
npm test               # Run tests
```

#### Backend (Python)
```bash
flake8 .               # Check linting
black .                # Format code
pytest                 # Run tests
mypy .                 # Type checking
```

#### Git Workflow
```bash
git add .
git commit -m "type(scope): description"
git push
```

### Code Quality Checklist
Before marking any task as complete, verify:
- ✅ **No type shortcuts**: No `any` types (TS), proper type hints (Python), strong typing (C#/Java)
- ✅ **Proper error handling**: Centralized error handling, no silent failures
- ✅ **Follows naming conventions**: Consistent with project standards
- ✅ **Uses dependency injection**: Services properly injected, not instantiated
- ✅ **Implements validation**: Input validation at API boundaries
- ✅ **Passes linting**: Zero linting errors
- ✅ **Builds successfully**: No build errors or warnings
- ✅ **Tests pass**: All unit and integration tests passing
- ✅ **Committed properly**: Conventional commit format used
- ✅ **Security checked**: No secrets, proper authentication/authorization
- ✅ **Performance considered**: No obvious bottlenecks or anti-patterns

### Common Pitfalls to Avoid
1. **Don't skip linting**: Always run and fix linting errors before committing
2. **Don't use `any`**: Take the time to define proper types
3. **Don't hardcode values**: Use configuration files and environment variables
4. **Don't ignore errors**: Implement proper error handling and logging
5. **Don't break patterns**: Follow existing architectural patterns in the codebase
6. **Don't commit secrets**: Use environment variables or secret managers
7. **Don't skip tests**: Write tests for new functionality
8. **Don't create god classes**: Keep classes and functions focused and small

### When Stuck
1. **Review similar code**: Look for similar functionality already implemented
2. **Check documentation**: Review API docs, library docs, and project docs
3. **Ask for clarification**: If requirements are unclear, ask the user
4. **Start simple**: Implement the simplest solution first, then refactor
5. **Test incrementally**: Test each piece as you build it

---

## 🎯 Summary: Definition of Done

A feature or task is only considered "Done" when ALL of the following are met:

### Code Quality
- ✅ Code builds without errors
- ✅ All tests pass
- ✅ Linting passes with 0 errors
- ✅ Code formatted according to project standards
- ✅ No type shortcuts (no `any` in TypeScript, proper type hints in Python, etc.)
- ✅ Follows naming conventions
- ✅ Proper error handling implemented
- ✅ Security best practices followed

### Testing
- ✅ Unit tests written for new functionality
- ✅ Integration tests for API endpoints
- ✅ Edge cases covered
- ✅ All tests passing

### Documentation
- ✅ Code comments for complex logic
- ✅ API documentation updated (if applicable)
- ✅ README updated (if applicable)
- ✅ Architecture docs updated (if applicable)

### Git & Deployment
- ✅ Committed with conventional commit format
- ✅ Pushed to remote repository
- ✅ Passes CI/CD pipeline
- ✅ Health checks pass in containerized environment (if applicable)

### Review
- ✅ Code reviewed by at least one other developer (if team project)
- ✅ All review comments addressed
- ✅ Approved for merge

---

*The Horizon Standard - Universal Architecture & Excellence Blueprint*  
*Created February 2026*  
*Version 1.0*

**This document is designed to be copied and adapted for any software project.**  
**Customize the technology-specific sections while maintaining the core principles.**
