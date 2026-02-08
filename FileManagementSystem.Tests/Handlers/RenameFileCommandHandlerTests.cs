using FluentAssertions;
using Moq;
using Xunit;
using FileManagementSystem.Application.Handlers;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Tests.Handlers;

public class RenameFileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RenameFileCommandHandler>> _loggerMock;
    private readonly RenameFileCommandHandler _handler;

    public RenameFileCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RenameFileCommandHandler>>();
        
        _handler = new RenameFileCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RenameFileCommand(Guid.NewGuid(), "NewName.txt");
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileItem?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.NewPath.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNewNameIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = new FileItem { Id = fileId, Path = "C:\\test\\old.txt" };
        var command = new RenameFileCommand(fileId, "");
        
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNewNameContainsInvalidChars_ShouldThrowArgumentException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = new FileItem { Id = fileId, Path = "C:\\test\\old.txt" };
        // Use null character which is invalid on all platforms
        var command = new RenameFileCommand(fileId, "new\0file.txt");
        
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNewPathContainsPathTraversal_ShouldThrowArgumentException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = new FileItem { Id = fileId, Path = "C:\\test\\old.txt" };
        var command = new RenameFileCommand(fileId, "../new.txt");
        
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        mockFileRepo.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act & Assert
        // The handler validates invalid filename chars first, which throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNewFileAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, "old.txt");
        var newPath = Path.Combine(tempDir, "existing.txt");
        
        // Create the files
        File.WriteAllText(tempFile, "old content");
        File.WriteAllText(newPath, "exists");
        
        try
        {
            var file = new FileItem { Id = fileId, Path = tempFile, FileName = "old.txt" };
            var command = new RenameFileCommand(fileId, "existing");
            
            var mockFileRepo = new Mock<IFileRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            mockFileRepo.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);
            mockFileRepo.Setup(r => r.UpdateAsync(file, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(newPath)) File.Delete(newPath);
        }
    }

    [Fact]
    public async Task Handle_WhenValidRename_ShouldRenameSuccessfully()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "old.txt");
        File.WriteAllText(tempFile, "test content");
        
        try
        {
            var file = new FileItem { Id = fileId, Path = tempFile, FileName = "old.txt" };
            var newFileName = $"new_{Guid.NewGuid():N}"; // Use unique name to avoid conflicts
            var command = new RenameFileCommand(fileId, newFileName);
            
            var mockFileRepo = new Mock<IFileRepository>();
            _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
            mockFileRepo.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);
            mockFileRepo.Setup(r => r.UpdateAsync(file, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.NewPath.Should().Contain(newFileName);
            mockFileRepo.Verify(r => r.UpdateAsync(It.Is<FileItem>(f => f.Path == result.NewPath), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
