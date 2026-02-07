# Horizon FMS - File Management System

**A production-ready, enterprise-grade file management application built with Clean Architecture and industrial-strength observability.**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]() [![Tests](https://img.shields.io/badge/tests-33%2F33-success)]() [![.NET](https://img.shields.io/badge/.NET-8.0-blue)]() [![React](https://img.shields.io/badge/React-19-blue)]()

> 🛡️ **This project serves as the reference implementation for [Horizon Guardian](HORIZON_GUARDIAN.md)** - an AI-powered architecture enforcement engine.

---

## 🚀 Features

### Core Functionality
- **📁 Folder Management**: Create, rename, delete folders with hierarchical tree navigation
- **📄 File Operations**: Upload, rename, delete files with drag-and-drop support
- **🔍 Advanced Search**: Full-text search with tag-based filtering
- **🖼️ Photo Metadata**: EXIF extraction (date, GPS, camera info) via SixLabors.ImageSharp
- **🎯 Smart Deduplication**: SHA256-based file deduplication
- **📸 Thumbnails**: Async thumbnail generation for images

### Industrial-Grade Features
- **🔍 Seq Integration**: Beautiful structured logging with visual query interface
- **❤️ Health Checks**: `/health` endpoint for orchestration and monitoring
- **🔄 API Versioning**: URI-based versioning (`/api/v1/...`)
- **🛡️ Security Headers**: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- **⚡ Rate Limiting**: IP-based rate limiting (100 req/min)
- **💾 Redis Caching**: Distributed caching for horizontal scaling
- **🐳 Container-First**: Full Docker Compose mesh (API, Web, Postgres, Seq, Redis)

---

## 🏗️ Architecture

This project follows **Clean Architecture** with 8 architectural pillars (see [ARCHITECTURE.md](ARCHITECTURE.md)):

```
FileManagementSystem/
├── Domain/              # Entities, domain exceptions
├── Application/         # Commands/Queries (MediatR), Handlers, DTOs
├── Infrastructure/      # EF Core, Repositories, Services
├── API/                 # ASP.NET Core Web API, Controllers, Middleware
├── Web/                 # React + TypeScript frontend
└── Tests/              # xUnit tests (33 tests, 100% pass rate)
```

### Architectural Pillars
1. **Single Source of Truth API** (NSwag auto-generated TypeScript client)
2. **Standardized Error Handling** (Global Middleware + ProblemDetails)
3. **Container-First** (Docker Compose for all services)
4. **Background Jobs** (Offloading for slow operations)
5. **Resilient Session Management** (401 interceptor + auto-refresh)
6. **Universal State & Caching** (TanStack Query)
7. **Real-time Communication** (Socket.IO ready)
8. **Observability & Health Monitoring** (Seq + Health Checks)

---

## 🛠️ Technology Stack

### Backend
- **.NET 8.0** - Modern C# with file-scoped namespaces
- **ASP.NET Core Web API** - RESTful API with API Versioning
- **PostgreSQL** - Production-grade relational database
- **Entity Framework Core 8.0** - ORM with migrations
- **MediatR 12.2.0** - CQRS pattern
- **FluentValidation 11.9.0** - Command validation
- **Serilog + Seq** - Structured logging with visual query UI
- **Redis** - Distributed caching
- **NSwag** - OpenAPI client generation

### Frontend
- **React 19** - Latest React with concurrent features
- **TypeScript** - Full type safety (zero `any` types)
- **Vite** - Lightning-fast build tool
- **TanStack Query** - Data fetching, caching, and state management
- **React Router** - Client-side routing
- **Auto-generated API Client** - Type-safe API calls via NSwag

### DevOps
- **Docker & Docker Compose** - Containerized deployment
- **GitHub Actions** - CI/CD pipeline (build, test, lint)
- **Health Checks** - Kubernetes-ready health endpoints

---

## 📦 Quick Start

### Prerequisites
- **Docker Desktop** (recommended) OR
- **.NET 8.0 SDK** + **Node.js 20+** + **PostgreSQL**

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/OfekItzhaki/horizon-fms.git
cd horizon-fms

# Start all services
docker-compose up -d

# Access the application
# Web UI: http://localhost:3000
# API: http://localhost:5000
# Seq Logs: http://localhost:5341
# Swagger: http://localhost:5000/swagger
```

**Services Started:**
- `horizon-fms-api`: ASP.NET Core API
- `horizon-fms-web`: React frontend
- `postgres`: PostgreSQL database
- `seq`: Structured logging UI
- `redis`: Distributed cache

### Option 2: Development Mode

```bash
# Terminal 1: Start API
cd FileManagementSystem.API
dotnet run

# Terminal 2: Start Web
cd FileManagementSystem.Web
npm install
npm run dev
```

---

## 🧪 Testing

### Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test FileManagementSystem.Tests/FileManagementSystem.Tests.csproj
```

### Test Coverage

**33 unit tests** covering all Command Handlers:
- ✅ `CreateFolderCommandHandlerTests` (6 tests)
- ✅ `DeleteFolderCommandHandlerTests` (6 tests)
- ✅ `RenameFolderCommandHandlerTests` (5 tests)
- ✅ `UploadFileCommandHandlerTests` (6 tests)
- ✅ `DeleteFileCommandHandlerTests` (4 tests)
- ✅ `RenameFileCommandHandlerTests` (6 tests)

Tests use **xUnit**, **Moq**, and **FluentAssertions** for readable, maintainable test code.

---

## 🔧 Configuration

### Environment Variables

Create a `.env` file in the root directory:

```env
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_DB=filemanagement

# API
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=postgres;Database=filemanagement;Username=postgres;Password=your_password

# Redis
REDIS_CONNECTION=redis:6379

# Seq
SEQ_URL=http://seq:5341
```

### API Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=filemanagement;Username=postgres;Password=your_password"
  },
  "Seq": {
    "ServerUrl": "http://localhost:5341"
  },
  "Redis": {
    "Configuration": "localhost:6379"
  },
  "ThumbnailSettings": {
    "MaxWidth": 200,
    "MaxHeight": 200
  }
}
```

---

## 📖 API Documentation

### Swagger UI
Access interactive API documentation at: `http://localhost:5000/swagger`

### Health Check
```bash
curl http://localhost:5000/health
```

**Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "storage": "Healthy"
  }
}
```

### API Versioning
All endpoints are versioned: `/api/v1/files`, `/api/v1/folders`

---

## 🛡️ Security Features

### Security Headers
- **CSP (Content-Security-Policy)**: Prevents XSS attacks
- **X-Frame-Options: DENY**: Prevents clickjacking
- **X-Content-Type-Options: nosniff**: Prevents MIME-sniffing attacks
- **Referrer-Policy: no-referrer**: Protects sensitive URLs

### Rate Limiting
- **100 requests/minute per IP** to prevent abuse and DDoS

### Data Protection
- **SHA256 hashing** for file deduplication
- **Path sanitization** to prevent directory traversal
- **File type validation** with MIME type checking

---

## 📊 Observability

### Seq Structured Logging
Access Seq at `http://localhost:5341` to:
- Search logs with queries: `Level == "Error" && UserId == "123"`
- Filter by timestamp, user, request ID
- Visualize error trends and performance metrics

### Logging Best Practices
All logs are structured with contextual properties:
```csharp
_logger.LogInformation("File uploaded: {FileName} by {UserId}", fileName, userId);
```

---

## 🏗️ Project Structure

### Domain Layer (`FileManagementSystem.Domain`)
- `Entities/`: FileItem, Folder, User
- `Exceptions/`: Domain-specific exceptions

### Application Layer (`FileManagementSystem.Application`)
- `Commands/`: MediatR commands (CreateFolder, UploadFile, etc.)
- `Queries/`: MediatR queries (GetFiles, SearchFiles, etc.)
- `Handlers/`: Command and query handlers
- `DTOs/`: Data transfer objects
- `Validators/`: FluentValidation validators
- `Interfaces/`: Application contracts

### Infrastructure Layer (`FileManagementSystem.Infrastructure`)
- `Data/`: AppDbContext, EF Core configuration
- `Repositories/`: Repository implementations
- `Services/`: MetadataService, StorageService, etc.

### API Layer (`FileManagementSystem.API`)
- `Controllers/`: FilesController, FoldersController
- `Middleware/`: GlobalExceptionHandlerMiddleware
- `Program.cs`: Dependency injection, middleware pipeline

### Web Layer (`FileManagementSystem.Web`)
- `src/components/`: React components (Dashboard, FileList, FolderTree)
- `src/services/`: Auto-generated API client (NSwag)
- `src/types/`: TypeScript type definitions

---

## ✅ Golden Rules Compliance

This project strictly follows the [ARCHITECTURE.md](ARCHITECTURE.md) golden rules:

- ✅ **Thin Controllers**: No business logic, only `return await _mediator.Send(command);`
- ✅ **Global Error Handling**: No `try-catch` in Controllers; middleware handles all exceptions
- ✅ **Type Safety**: Zero `any` types in TypeScript; proper Error types everywhere
- ✅ **Component Modularity**: All components under 200 lines
- ✅ **Single Source of Truth**: Auto-generated NSwag client for API calls
- ✅ **Container-First**: All services in `docker-compose.yml`
- ✅ **Observability**: Seq for structured logging, `/health` endpoint
- ✅ **Security Headers**: CSP, X-Frame-Options, X-Content-Type-Options, Rate Limiting

---

## 🚀 Deployment

### Docker Production Build

```bash
# Build production images
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build

# Deploy to production
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes (Coming Soon)
Health checks are Kubernetes-ready for liveness and readiness probes.

---

## 📚 Additional Resources

- **[ARCHITECTURE.md](ARCHITECTURE.md)**: Complete architectural standards and golden rules
- **[HORIZON_GUARDIAN.md](HORIZON_GUARDIAN.md)**: Product blueprint for AI-powered architecture enforcement
- **[Walkthrough](https://github.com/OfekItzhaki/horizon-fms/wiki)**: Detailed implementation walkthrough

---

## 🤝 Contributing

This project serves as a reference implementation of elite fullstack standards. Contributions should:
1. Follow the [ARCHITECTURE.md](ARCHITECTURE.md) golden rules
2. Include unit tests (maintain 100% test pass rate)
3. Use conventional commits (`feat:`, `fix:`, `chore:`, etc.)
4. Pass all CI/CD checks (build, test, lint)

---

## 📄 License

This project is provided as-is for demonstration and educational purposes.

---

## 🎯 Roadmap

- [ ] Real-time collaboration via Socket.IO
- [ ] Background job processing with BullMQ
- [ ] Cloud storage integration (Azure Blob, AWS S3)
- [ ] Advanced search with Elasticsearch
- [ ] Image editing capabilities
- [ ] Mobile app (React Native)
- [ ] Plugin system for extensibility

---

**Built with ❤️ using Clean Architecture and Industrial-Grade Standards**

*This project powers the [Horizon Guardian](HORIZON_GUARDIAN.md) enforcement engine.*
