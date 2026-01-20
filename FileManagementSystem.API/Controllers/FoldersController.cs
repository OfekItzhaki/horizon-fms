using Microsoft.AspNetCore.Mvc;
using MediatR;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        _logger.LogInformation("CreateFolder endpoint called with Name: {Name}, ParentFolderId: {ParentFolderId}", 
            request?.Name, request?.ParentFolderId);
        
        if (request == null)
        {
            _logger.LogWarning("CreateFolder called with null request");
            return BadRequest("Request body is required");
        }
        
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Folder name is required");
        }

        try
        {
            var command = new CreateFolderCommand(request.Name, request.ParentFolderId);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Folder created successfully: {FolderId}", result.FolderId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for folder creation");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operation error during folder creation");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder");
            return StatusCode(500, "An error occurred while creating the folder");
        }
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

        try
        {
            var command = new RenameFolderCommand(id, request.NewName);
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming folder {FolderId}", id);
            return StatusCode(500, "An error occurred while renaming the folder");
        }
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
        try
        {
            var command = new DeleteFolderCommand(id, deleteFiles);
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderId}", id);
            return StatusCode(500, "An error occurred while deleting the folder");
        }
    }
}

public record CreateFolderRequest(string Name, Guid? ParentFolderId = null);
public record RenameFolderRequest(string NewName);
