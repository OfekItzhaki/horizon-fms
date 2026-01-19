# File Management System

A production-ready file management application built with **Clean Architecture**, featuring a **React + TypeScript** frontend and **ASP.NET Core Web API** backend.

## 🚀 Features

- **Directory Scanning**: Async directory scanning with progress reporting
- **File Upload/Organization**: Automatic file deduplication by SHA256 hash
- **Photo Metadata Extraction**: EXIF data extraction (date, GPS, camera info) via SixLabors.ImageSharp
- **Tagging & Search**: Full-text search with EF Core, tag-based filtering
- **Thumbnails**: Async thumbnail generation for photos
- **Folder Hierarchy**: Tree navigation with hierarchical folder structure
- **Drag & Drop**: File upload via drag-and-drop (React)
- **Delete/Rename**: File operations with undo capability (planned)
- **Modern Stack**: .NET 8, React 19, TypeScript, Clean Architecture

## 🏗️ Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
FileManagementSystem/
├── Domain/              # Entities and domain exceptions
├── Application/         # DTOs, Commands/Queries (MediatR), Validators (FluentValidation)
├── Infrastructure/      # EF Core DbContext, Repositories, Services
├── API/                 # ASP.NET Core Web API (REST endpoints)
├── Web/                 # React + TypeScript frontend
└── Tests/              # xUnit unit and integration tests
```

## 🛠️ Technology Stack

### Backend
- **.NET 8.0** - Target framework
- **ASP.NET Core Web API** - RESTful API
- **Entity Framework Core 8.0** - ORM with SQLite provider
- **MediatR 12.2.0** - CQRS pattern implementation
- **FluentValidation 11.9.0** - Command validation
- **Serilog** - Structured logging (file and console sinks)
- **SixLabors.ImageSharp 3.1.2** - Image processing and EXIF extraction

### Frontend
- **React 19** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **React Query (TanStack Query)** - Data fetching and caching
- **React Router** - Navigation
- **Axios** - HTTP client

## 📦 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Node.js 18+ and npm
- Visual Studio 2022 or VS Code

### Running the Application

#### 1. Start the API Backend

```bash
cd FileManagementSystem.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5295`
- HTTPS: `https://localhost:7136`
- Swagger UI: `https://localhost:7136/swagger`

#### 2. Start the React Frontend

```bash
cd FileManagementSystem.Web
npm install
npm run dev
```

The React app will be available at `http://localhost:5173` (or the port Vite assigns)

### Building the Solution

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Build React app
cd FileManagementSystem.Web
npm run build
```

### Database Setup

The application uses SQLite and will automatically create the database on first run. The database file (`filemanager.db`) will be created in the API project's output directory.

## 🔧 Configuration

### API Configuration (`FileManagementSystem.API/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=filemanager.db"
  },
  "ThumbnailSettings": {
    "MaxWidth": 200,
    "MaxHeight": 200
  }
}
```

### Frontend Configuration

The React app uses a Vite proxy to connect to the API. The proxy is configured in `FileManagementSystem.Web/vite.config.ts`.

## 📖 Usage

### Scanning a Directory

1. Use the API endpoint: `POST /api/files/scan` (to be implemented)
2. Or use the file upload feature

### Searching Files

- Use the search bar in the React UI
- Filter by photos only
- Search by tags, filename, or metadata

### Managing Files

- **Upload**: Drag and drop files or use the upload button
- **Rename**: Click on a file and use the rename action
- **Delete**: Select a file and delete (moves to recycle bin)

## 🧪 Testing

Run tests with:

```bash
dotnet test
```

## 📝 Project Structure

### Domain Layer
- `Entities/`: FileItem, Folder, User entities
- `Exceptions/`: Domain-specific exceptions

### Application Layer
- `Commands/`: MediatR commands (ScanDirectoryCommand, UploadFileCommand, etc.)
- `Queries/`: MediatR queries (SearchFilesQuery, GetFoldersQuery, etc.)
- `DTOs/`: Data transfer objects
- `Handlers/`: Command and query handlers
- `Validators/`: FluentValidation validators
- `Behaviors/`: MediatR pipeline behaviors (logging, validation, authorization)

### Infrastructure Layer
- `Data/`: AppDbContext and EF Core configuration
- `Repositories/`: Repository implementations
- `Services/`: MetadataService, StorageService, AuthenticationService, etc.

### API Layer
- `Controllers/`: REST API endpoints (FilesController, FoldersController)

### Web Layer (React)
- `src/components/`: React components (Dashboard, FileList, FolderTree, etc.)
- `src/services/`: API client and services
- `src/types/`: TypeScript type definitions

## ✅ Best Practices Implemented

- ✅ **Clean Architecture**: Clear separation of concerns
- ✅ **CQRS**: Commands and Queries separation with MediatR
- ✅ **Async/Await**: All I/O operations are async
- ✅ **Dependency Injection**: Microsoft.Extensions.DependencyInjection throughout
- ✅ **Validation**: FluentValidation on all commands
- ✅ **Logging**: Serilog with structured logging
- ✅ **Error Handling**: Custom exceptions and try-catch with user-friendly messages
- ✅ **Security**: Path sanitization, file size/type validation, SHA256 hashing
- ✅ **Performance**: Background tasks, EF AsNoTracking for reads, React Query caching
- ✅ **Type Safety**: TypeScript throughout the frontend

## 📄 License

This project is provided as-is for demonstration purposes.

## 🤝 Contributing

Feel free to extend this application with additional features:
- Cloud storage integration (Azure Blob, AWS S3)
- Batch operations
- Advanced search filters
- Image editing capabilities
- Export functionality
- Plugin system
