using FluentAssertions;
using Moq;
using Xunit;
using FileManagementSystem.Application.Handlers;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Tests.Handlers;

public class RenameFolderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RenameFolderCommandHandler>> _loggerMock;
    private readonly RenameFolderCommandHandler _handler;

    public RenameFolderCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RenameFolderCommandHandler>>();
        
        _handler = new RenameFolderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNameIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var command = new RenameFolderCommand(Guid.NewGuid(), "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Folder name cannot be empty");
    }

    [Fact]
    public async Task Handle_WhenFolderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RenameFolderCommand(Guid.NewGuid(), "NewName");
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
    public async Task Handle_WhenSiblingFolderWithSameNameExists_ShouldReturnFailure()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "OldName", ParentFolderId = parentId, Path = "C:\\test\\OldName" };
        var siblingFolder = new Folder { Id = Guid.NewGuid(), Name = "NewName", ParentFolderId = parentId, Path = "C:\\test\\NewName" };
        var command = new RenameFolderCommand(folderId, "NewName");
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder> { folder, siblingFolder });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WhenValidRename_ShouldRenameSuccessfully()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "OldName", Path = "C:\\test\\OldName", ParentFolderId = null };
        var command = new RenameFolderCommand(folderId, "NewName");
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder> { folder });
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem>());
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Folder.Should().NotBeNull();
        result.Folder!.Name.Should().Be("NewName");
        mockFolderRepo.Verify(r => r.UpdateAsync(It.Is<Folder>(f => f.Name == "NewName"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRenameSubFolder_ShouldUpdatePathCorrectly()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var parentFolder = new Folder { Id = parentId, Name = "Parent", Path = "C:\\test\\Parent" };
        var folder = new Folder { Id = folderId, Name = "OldName", Path = "C:\\test\\Parent\\OldName", ParentFolderId = parentId };
        var command = new RenameFolderCommand(folderId, "NewName");
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        var mockFileRepo = new Mock<IFileRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        _unitOfWorkMock.Setup(u => u.Files).Returns(mockFileRepo.Object);
        
        mockFolderRepo.Setup(r => r.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        mockFolderRepo.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder> { folder });
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.UpdateAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockFileRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FileItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileItem>());
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Folder.Should().NotBeNull();
        result.Folder!.Name.Should().Be("NewName");
        result.Folder.Path.Should().Contain("NewName");
    }
}
