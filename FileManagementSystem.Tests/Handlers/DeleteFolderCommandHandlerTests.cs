using FluentAssertions;
using Moq;
using Xunit;
using FileManagementSystem.Application.Handlers;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Tests.Handlers;

public class DeleteFolderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DeleteFolderCommandHandler>> _loggerMock;
    private readonly DeleteFolderCommandHandler _handler;

    public DeleteFolderCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteFolderCommandHandler>>();
        
        _handler = new DeleteFolderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFolderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteFolderCommand(Guid.NewGuid());
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Folder not found");
    }

    [Fact]
    public async Task Handle_WhenDefaultFolder_ShouldReturnFailure()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var defaultFolder = new Folder { Id = folderId, Name = "Default", Path = "C:\\storage\\Default" };
        var command = new DeleteFolderCommand(folderId);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultFolder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot delete the Default folder");
    }

    [Fact]
    public async Task Handle_WhenFolderHasSubfoldersAndDeleteFilesFalse_ShouldReturnFailure()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "TestFolder", Path = "C:\\test" };
        var subFolder = new Folder { Id = Guid.NewGuid(), Name = "SubFolder", ParentFolderId = folderId, Path = "C:\\test\\SubFolder" };
        var command = new DeleteFolderCommand(folderId, DeleteFiles: false);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder> { subFolder });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("contains subfolders");
    }

    [Fact]
    public async Task Handle_WhenFolderHasFilesAndDeleteFilesFalse_ShouldReturnFailure()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "TestFolder", Path = "C:\\test" };
        var file = new FileItem { Id = Guid.NewGuid(), FolderId = folderId, Path = "C:\\test\\file.txt" };
        var command = new DeleteFolderCommand(folderId, DeleteFiles: false);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem> { file });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("contains");
    }

    [Fact]
    public async Task Handle_WhenValidEmptyFolder_ShouldDeleteSuccessfully()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "TestFolder", Path = "C:\\test" };
        var command = new DeleteFolderCommand(folderId);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.DeleteAsync(folder, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem>());
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        mockFolderRepo.Verify(r => r.DeleteAsync(folder, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeleteFilesTrue_ShouldDeleteFolderAndFiles()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "TestFolder", Path = "C:\\test" };
        var file = new FileItem { Id = Guid.NewGuid(), FolderId = folderId, Path = "C:\\test\\file.txt" };
        var command = new DeleteFolderCommand(folderId, DeleteFiles: true);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.DeleteAsync(folder, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem> { file });
        mockFileRepo.Setup(r => r.DeleteAsync(file, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        mockFileRepo.Verify(r => r.DeleteAsync(file, It.IsAny<CancellationToken>()), Times.Once);
        mockFolderRepo.Verify(r => r.DeleteAsync(folder, It.IsAny<CancellationToken>()), Times.Once);
    }
}
