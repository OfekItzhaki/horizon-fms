using FluentAssertions;
using Moq;
using Xunit;
using FileManagementSystem.Application.Handlers;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Services;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using PhotoMetadata = FileManagementSystem.Application.Interfaces.PhotoMetadata;

namespace FileManagementSystem.Tests.Handlers;

public class UploadFileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IMetadataService> _metadataServiceMock;
    private readonly Mock<ILogger<UploadDestinationResolver>> _destinationResolverLoggerMock;
    private readonly Mock<ILogger<UploadFileCommandHandler>> _loggerMock;
    private readonly UploadDestinationResolver _destinationResolver;
    private readonly UploadFileCommandHandler _handler;

    public UploadFileCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _storageServiceMock = new Mock<IStorageService>();
        _metadataServiceMock = new Mock<IMetadataService>();
        _destinationResolverLoggerMock = new Mock<ILogger<UploadDestinationResolver>>();
        _loggerMock = new Mock<ILogger<UploadFileCommandHandler>>();
        
        _destinationResolver = new UploadDestinationResolver(
            _unitOfWorkMock.Object,
            _destinationResolverLoggerMock.Object);
        
        _handler = new UploadFileCommandHandler(
            _unitOfWorkMock.Object,
            _storageServiceMock.Object,
            _metadataServiceMock.Object,
            _destinationResolver,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var command = new UploadFileCommand("nonexistent.txt", "test.txt");
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem>());

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenPathContainsPathTraversal_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var command = new UploadFileCommand("../test.txt", "test.txt");
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem>());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFileAlreadyExists_ShouldReturnExistingFile()
    {
        // Arrange
        var existingFile = new FileItem { Id = Guid.NewGuid(), Path = "C:\\test\\test.txt" };
        var command = new UploadFileCommand("C:\\test\\test.txt", "test.txt");
        
        // Create a temporary file for the test
        var tempFile = Path.GetTempFileName();
        try
        {
            var mockFileRepo = new Mock<IFileRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileItem> { existingFile });

            // Act
            var result = await _handler.Handle(new UploadFileCommand(tempFile, "test.txt"), CancellationToken.None);

            // Assert
            result.IsDuplicate.Should().BeTrue();
            result.FileId.Should().Be(existingFile.Id);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Handle_WhenDuplicateHashExists_ShouldThrowFileDuplicateException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var hash = new byte[] { 1, 2, 3, 4 };
            var duplicateFile = new FileItem { Id = Guid.NewGuid(), Path = "existing.txt", Hash = hash };
            var command = new UploadFileCommand(tempFile, "test.txt");
            
            var mockFileRepo = new Mock<IFileRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileItem>());
            mockFileRepo.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(duplicateFile);
            _storageServiceMock.Setup(s => s.ComputeHashAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hash);
            
            var targetFolder = new Folder { Id = Guid.NewGuid(), Path = "C:\\storage\\default", Name = "Default" };
            var mockFolderRepo = new Mock<IFolderRepository>();
            _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
            mockFolderRepo.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Folder?)null);
            mockFolderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());
            mockFolderRepo.Setup(r => r.GetOrCreateByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetFolder);

            // Act & Assert
            await Assert.ThrowsAsync<FileDuplicateException>(() => _handler.Handle(command, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Handle_WhenValidFile_ShouldUploadSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content");
            var hash = new byte[] { 1, 2, 3, 4 };
            var command = new UploadFileCommand(tempFile, "test.txt");
            
            var mockFileRepo = new Mock<IFileRepository>();
            var mockFolderRepo = new Mock<IFolderRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
            
            mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileItem>());
            mockFileRepo.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileItem?)null);
            mockFileRepo.Setup(r => r.AddAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileItem item, CancellationToken ct) => item);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            _storageServiceMock.Setup(s => s.ComputeHashAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hash);
            
            // Create the destination directory and file that SaveFileAsync would create
            var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllText(destinationPath, "compressed content");
            
            _storageServiceMock.Setup(s => s.SaveFileAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(destinationPath);
            
            _metadataServiceMock.Setup(m => m.IsPhotoFileAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            var targetFolder = new Folder { Id = Guid.NewGuid(), Path = Path.GetDirectoryName(destinationPath)!, Name = "Default" };
            mockFolderRepo.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Folder?)null);
            mockFolderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());
            mockFolderRepo.Setup(r => r.GetOrCreateByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetFolder);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsDuplicate.Should().BeFalse();
            result.FileId.Should().NotBeEmpty();
            mockFileRepo.Verify(r => r.AddAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            // Cleanup
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            if (Directory.Exists(Path.GetDirectoryName(destinationPath)!)) 
                Directory.Delete(Path.GetDirectoryName(destinationPath)!, true);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Handle_WhenPhotoFile_ShouldExtractMetadata()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content");
            var hash = new byte[] { 1, 2, 3, 4 };
            var command = new UploadFileCommand(tempFile, "photo.jpg");
            var photoMetadata = new PhotoMetadata
            {
                DateTaken = DateTime.UtcNow,
                CameraMake = "Canon",
                CameraModel = "EOS 5D"
            };
            
            var mockFileRepo = new Mock<IFileRepository>();
            var mockFolderRepo = new Mock<IFolderRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
            
            mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileItem>());
            mockFileRepo.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileItem?)null);
            mockFileRepo.Setup(r => r.AddAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FileItem item, CancellationToken ct) => item);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            _storageServiceMock.Setup(s => s.ComputeHashAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hash);
            
            // Create the destination directory and file that SaveFileAsync would create
            var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "photo.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllText(destinationPath, "compressed photo content");
            
            _storageServiceMock.Setup(s => s.SaveFileAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(destinationPath);
            
            _metadataServiceMock.Setup(m => m.IsPhotoFileAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _metadataServiceMock.Setup(m => m.ExtractPhotoMetadataAsync(tempFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photoMetadata);
            
            var targetFolder = new Folder { Id = Guid.NewGuid(), Path = Path.GetDirectoryName(destinationPath)!, Name = "Default" };
            mockFolderRepo.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Folder?)null);
            mockFolderRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());
            mockFolderRepo.Setup(r => r.GetOrCreateByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetFolder);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsDuplicate.Should().BeFalse();
            _metadataServiceMock.Verify(m => m.ExtractPhotoMetadataAsync(tempFile, It.IsAny<CancellationToken>()), Times.Once);
            
            // Cleanup
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            if (Directory.Exists(Path.GetDirectoryName(destinationPath)!)) 
                Directory.Delete(Path.GetDirectoryName(destinationPath)!, true);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
