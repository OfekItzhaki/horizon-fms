using Microsoft.AspNetCore.Mvc;
using MediatR;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Commands;
using Asp.Versioning;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class FoldersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FoldersController> _logger;

    public FoldersController(IMediator mediator, ILogger<FoldersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all folders (tree structure)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GetFoldersResult>> GetFolders(
        [FromQuery] Guid? parentFolderId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFoldersQuery(parentFolderId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new folder
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CreateFolderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateFolderResult>> CreateFolder(
        [FromBody] CreateFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Folder name is required");
        }
        
        var command = new CreateFolderCommand(request.Name, request.ParentFolderId);
        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Folder created successfully: {FolderId}", result.FolderId);
        return Ok(result);
    }

    /// <summary>
    /// Rename a folder
    /// </summary>
    [HttpPut("{id}/rename")]
    public async Task<ActionResult<RenameFolderResult>> RenameFolder(
        Guid id,
        [FromBody] RenameFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            return BadRequest("New folder name is required");
        }

        var command = new RenameFolderCommand(id, request.NewName);
        var result = await _mediator.Send(command, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Delete a folder
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<DeleteFolderResult>> DeleteFolder(
        Guid id,
        [FromQuery] bool deleteFiles = false,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteFolderCommand(id, deleteFiles);
        var result = await _mediator.Send(command, cancellationToken);
        
        return Ok(result);
    }
}

public record CreateFolderRequest(string Name, Guid? ParentFolderId = null);
public record RenameFolderRequest(string NewName);
