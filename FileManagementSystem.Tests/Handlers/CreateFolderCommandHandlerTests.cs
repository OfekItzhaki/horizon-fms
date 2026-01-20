using FluentAssertions;
using Moq;
using Xunit;
using FileManagementSystem.Application.Handlers;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Tests.Handlers;

public class CreateFolderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateFolderCommandHandler>> _loggerMock;
    private readonly CreateFolderCommandHandler _handler;

    public CreateFolderCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateFolderCommandHandler>>();
        
        _handler = new CreateFolderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateFolderCommand("");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNameIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateFolderCommand("   ");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFolderNameAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateFolderCommand("ExistingFolder");
        var existingFolder = new Folder { Id = Guid.NewGuid(), Name = "ExistingFolder", Path = "C:\\test\\ExistingFolder" };
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder> { existingFolder });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenParentFolderNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var command = new CreateFolderCommand("NewFolder", parentId);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValidRootFolder_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateFolderCommand("NewFolder");
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder folder, CancellationToken ct) => folder);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FolderId.Should().NotBeEmpty();
        result.Folder.Name.Should().Be("NewFolder");
        result.Folder.Path.Should().Be("NewFolder");
        mockFolderRepo.Verify(r => r.AddAsync(It.Is<Folder>(f => f.Name == "NewFolder" && f.Path == "NewFolder"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidSubFolder_ShouldCreateWithParentPath()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentFolder = new Folder { Id = parentId, Name = "Parent", Path = "C:\\storage\\Parent" };
        var command = new CreateFolderCommand("ChildFolder", parentId);
        
        var mockFolderRepo = new Mock<IFolderRepository>();
        _unitOfWorkMock.Setup(u => u.Folders).Returns(mockFolderRepo.Object);
        mockFolderRepo.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Folder>());
        mockFolderRepo.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);
        mockFolderRepo.Setup(r => r.AddAsync(It.IsAny<Folder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Folder folder, CancellationToken ct) => folder);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FolderId.Should().NotBeEmpty();
        result.Folder.Name.Should().Be("ChildFolder");
        result.Folder.ParentFolderId.Should().Be(parentId);
        mockFolderRepo.Verify(r => r.AddAsync(It.Is<Folder>(f => 
            f.Name == "ChildFolder" && 
            f.ParentFolderId == parentId &&
            f.Path.Contains("ChildFolder")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
