# File Management System - Features & Technology Stack

## 🚀 Features

### Core File Management
1. **File Upload & Organization**
   - Drag-and-drop file upload via React frontend
   - Automatic file deduplication using SHA256 hashing
   - Organized storage with folder hierarchy support
   - Support for all file types with MIME type detection

2. **Directory Scanning**
   - Async directory scanning with progress reporting
   - Recursive folder traversal
   - Background processing for large directories

3. **Photo Management**
   - Automatic photo detection and metadata extraction
   - EXIF data extraction (date taken, GPS coordinates, camera make/model)
   - Async thumbnail generation for fast previews
   - Photo-specific search and filtering

4. **Search & Discovery**
   - Full-text search across filenames and metadata
   - Tag-based filtering and organization
   - Filter by file type (photos only)
   - Folder-based navigation and filtering
   - Pagination support for large result sets

5. **File Operations**
   - Rename files
   - Delete files (with optional recycle bin support)
   - Add/remove tags
   - Folder management (create, navigate, organize)

6. **User Interface**
   - Modern React 19 frontend with TypeScript
   - Responsive design
   - Real-time file listing
   - Drag-and-drop upload interface
   - Folder tree navigation

## 🛠️ Technology Stack & Rationale

### Backend Technologies

#### **.NET 8.0**
- **Why**: Latest LTS version with excellent performance, cross-platform support, and modern language features
- **Benefits**: High performance, async/await support, strong typing, extensive ecosystem

#### **ASP.NET Core Web API**
- **Why**: Industry-standard REST API framework for .NET
- **Benefits**: Built-in routing, model binding, middleware pipeline, Swagger integration, CORS support

#### **Entity Framework Core 8.0**
- **Why**: Modern ORM that simplifies database operations
- **Benefits**: 
  - LINQ queries for type-safe database access
  - Automatic migrations
  - Change tracking
  - SQLite provider for easy deployment (no separate database server needed)

#### **SQLite**
- **Why**: Embedded database, perfect for file management systems
- **Benefits**: 
  - No separate database server required
  - Single file database (easy backup/portability)
  - ACID compliant
  - Excellent for development and small-to-medium deployments
  - Can be upgraded to SQL Server/PostgreSQL later if needed

#### **MediatR 12.2.0**
- **Why**: Implements CQRS (Command Query Responsibility Segregation) pattern
- **Benefits**:
  - Clean separation between commands (write) and queries (read)
  - Decouples controllers from business logic
  - Pipeline behaviors for cross-cutting concerns (logging, validation, authorization)
  - Easy to test and maintain

#### **FluentValidation 11.9.0**
- **Why**: Fluent, readable validation rules
- **Benefits**:
  - Strongly-typed validation
  - Reusable validation rules
  - Clear error messages
  - Integrates seamlessly with MediatR pipeline

#### **Castle Windsor 5.1.0**
- **Why**: Advanced IoC container with powerful features
- **Benefits**:
  - Advanced dependency injection capabilities
  - Lifestyle management (Singleton, Scoped, Transient)
  - Interceptors for AOP (Aspect-Oriented Programming)
  - Factory method support
  - Open generic registration (for ILogger<>)
  - Better suited for complex enterprise scenarios than built-in DI

#### **Serilog**
- **Why**: Structured logging framework
- **Benefits**:
  - Structured logging (JSON format)
  - Multiple sinks (file, console, can extend to cloud)
  - Performance optimized
  - Easy to query and analyze logs
  - Better than default logging for production systems

#### **SixLabors.ImageSharp 3.1.2**
- **Why**: Pure .NET image processing library
- **Benefits**:
  - No native dependencies (pure C#)
  - Cross-platform
  - EXIF metadata extraction
  - Image manipulation (thumbnail generation, resizing)
  - Better performance than System.Drawing

### Frontend Technologies

#### **React 19**
- **Why**: Most popular and mature UI library
- **Benefits**:
  - Component-based architecture
  - Large ecosystem
  - Excellent developer experience
  - Strong community support
  - Latest version with improved performance

#### **TypeScript**
- **Why**: Type safety for JavaScript
- **Benefits**:
  - Catch errors at compile time
  - Better IDE support and autocomplete
  - Self-documenting code
  - Easier refactoring
  - Reduces runtime errors

#### **Vite**
- **Why**: Next-generation frontend build tool
- **Benefits**:
  - Extremely fast development server (HMR)
  - Fast builds
  - Modern ES modules
  - Better than Webpack for modern projects

#### **React Query (TanStack Query)**
- **Why**: Powerful data fetching and caching library
- **Benefits**:
  - Automatic caching and background refetching
  - Request deduplication
  - Optimistic updates
  - Loading/error states management
  - Reduces boilerplate code

#### **React Router**
- **Why**: Standard routing solution for React
- **Benefits**:
  - Declarative routing
  - Nested routes
  - URL-based navigation
  - Browser history integration

#### **Axios**
- **Why**: Popular HTTP client
- **Benefits**:
  - Promise-based API
  - Request/response interceptors
  - Automatic JSON transformation
  - Better error handling than fetch

### Architecture Patterns

#### **Clean Architecture**
- **Why**: Maintainable, testable, and scalable codebase
- **Benefits**:
  - Clear separation of concerns
  - Domain layer independent of infrastructure
  - Easy to test (business logic isolated)
  - Easy to swap implementations (e.g., change database)

#### **CQRS (Command Query Responsibility Segregation)**
- **Why**: Separate read and write operations
- **Benefits**:
  - Optimize reads and writes independently
  - Clear intent (command = change state, query = read data)
  - Easier to scale
  - Better performance (can use different data stores)

#### **Repository Pattern**
- **Why**: Abstract data access
- **Benefits**:
  - Testable (can mock repositories)
  - Easy to swap data sources
  - Centralized data access logic

#### **Dependency Injection**
- **Why**: Loose coupling and testability
- **Benefits**:
  - Easy to test (inject mocks)
  - Flexible (swap implementations)
  - Better code organization
  - Castle Windsor provides advanced DI features

### Why Castle Windsor Instead of Built-in DI?

While ASP.NET Core has built-in dependency injection, Castle Windsor was chosen for:

1. **Advanced Features**: 
   - Interceptors for AOP
   - More sophisticated lifestyle management
   - Factory method support with complex scenarios

2. **Enterprise Patterns**: 
   - Better support for complex dependency graphs
   - Open generic registration (ILogger<>)
   - Collection resolution (IEnumerable<T>)

3. **Legacy Integration**: 
   - If migrating from existing Castle Windsor codebase
   - Consistent DI across multiple applications

4. **Future-Proofing**: 
   - Easier to add cross-cutting concerns (logging, caching, transactions)
   - Better support for decorator pattern

**Note**: MediatR and FluentValidation are registered with ASP.NET Core DI for better compatibility, while business services use Castle Windsor.

## 📊 Architecture Layers

```
┌─────────────────────────────────────┐
│         API (Controllers)           │  ← REST endpoints
├─────────────────────────────────────┤
│      Application (CQRS)             │  ← Commands/Queries, Validators
├─────────────────────────────────────┤
│      Domain (Entities)              │  ← Business entities
├─────────────────────────────────────┤
│   Infrastructure (Data/Services)    │  ← EF Core, Repositories, Services
└─────────────────────────────────────┘
```

## 🔄 Data Flow

1. **Request** → API Controller
2. **Controller** → MediatR Command/Query
3. **MediatR** → Pipeline Behaviors (Logging → Authorization → Validation → Exception Handling)
4. **Handler** → Application Logic
5. **Repository** → Database (via EF Core)
6. **Response** ← DTO ← Entity

## 🎯 Key Design Decisions

1. **SQLite for Development**: Easy setup, no external dependencies
2. **MediatR for CQRS**: Industry standard, well-documented
3. **Castle Windsor for DI**: Advanced features for complex scenarios
4. **React Query for State**: Eliminates need for Redux for data fetching
5. **TypeScript Everywhere**: Type safety reduces bugs
6. **Clean Architecture**: Long-term maintainability

## 🚀 Future Enhancements

- Cloud storage integration (Azure Blob, AWS S3)
- Batch operations
- Advanced search filters
- Image editing capabilities
- Export functionality
- Plugin system
- Real-time notifications
- Multi-user support with permissions
