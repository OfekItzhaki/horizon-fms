using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Queries;
using Asp.Versioning;
using System.Collections.Generic;
using System.Linq;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IMediator mediator, ILogger<FilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get list of files with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SearchFilesResult>> GetFiles(
        [FromQuery] string? searchTerm = null,
        [FromQuery] List<string>? tags = null,
        [FromQuery] bool? isPhoto = null,
        [FromQuery] string? folderId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Parse folderId string to Guid if provided
        Guid? parsedFolderId = null;
        if (!string.IsNullOrEmpty(folderId) && Guid.TryParse(folderId, out var guid))
        {
            parsedFolderId = guid;
        }
        else if (!string.IsNullOrEmpty(folderId))
        {
            _logger.LogWarning("Invalid folderId format: {FolderId}", folderId);
        }
        
        _logger.LogInformation("GetFiles called with searchTerm={SearchTerm}, isPhoto={IsPhoto}, folderId={FolderId}, skip={Skip}, take={Take}",
            searchTerm, isPhoto, parsedFolderId, skip, take);
        
        try
        {
            var query = new SearchFilesQuery(
                SearchTerm: searchTerm,
                Tags: tags,
                IsPhoto: isPhoto,
                FolderId: parsedFolderId,
                Skip: skip,
                Take: take
            );

            var result = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("GetFiles returned {Count} files", result.TotalCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFiles");
            throw; // Let middleware handle it
        }
    }

    /// <summary>
    /// Get file by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetFile(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetFileQuery(id);
        var file = await _mediator.Send(query, cancellationToken);
        
        if (file == null)
        {
            return NotFound();
        }
        
        return Ok(file);
    }

    /// <summary>
    /// Download/Open a file by ID
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult> DownloadFile(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFileDownloadQuery(id), cancellationToken);
        
        if (result == null)
        {
            return NotFound("File not found on server.");
        }

        return File(result.Content, result.MimeType, result.FileName);
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadFileResult>> UploadFile(
        IFormFile file,
        [FromForm] string? destinationFolderId = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload attempt with no file or empty file");
            return BadRequest("No file provided or file is empty");
        }

        // Save uploaded file to temp location
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
        
        _logger.LogInformation("File upload started: {FileName}, Size={Size}, DestinationFolderId={DestinationFolderId}, TempPath={TempPath}",
            file.FileName, file.Length, destinationFolderId ?? "default", tempPath);
        
        try
        {
            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Verify temp file was created successfully
            if (!System.IO.File.Exists(tempPath))
            {
                _logger.LogError("Temp file was not created: {TempPath}", tempPath);
                return BadRequest("Failed to save uploaded file");
            }

            _logger.LogDebug("Temp file created successfully: {TempPath}, Size={Size}", 
                tempPath, new FileInfo(tempPath).Length);

            // Parse folder ID if provided
            Guid? folderId = null;
            if (!string.IsNullOrEmpty(destinationFolderId) && Guid.TryParse(destinationFolderId, out var parsedFolderId))
            {
                folderId = parsedFolderId;
            }

            var command = new UploadFileCommand(tempPath, file.FileName, folderId);
            var result = await _mediator.Send(command, cancellationToken);
            
            _logger.LogInformation("File upload completed: {FileId}, IsDuplicate={IsDuplicate}",
                result.FileId, result.IsDuplicate);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            throw; // Let middleware handle it
        }
        finally
        {
            // Clean up temp file if still exists
            if (System.IO.File.Exists(tempPath))
            {
                try 
                { 
                    System.IO.File.Delete(tempPath);
                    _logger.LogDebug("Cleaned up temp file: {TempPath}", tempPath);
                } 
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {TempPath}", tempPath);
                }
            }
        }
    }

    /// <summary>
    /// Delete a file by ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFile(Guid id, [FromQuery] bool moveToRecycleBin = true, CancellationToken cancellationToken = default)
    {
        var command = new DeleteFileCommand(id, moveToRecycleBin);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Rename a file
    /// </summary>
    [HttpPut("{id}/rename")]
    public async Task<ActionResult> RenameFile(Guid id, [FromBody] RenameFileRequest request, CancellationToken cancellationToken = default)
    {
        var command = new RenameFileCommand(id, request.NewName);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add tags to a file
    /// </summary>
    [HttpPost("{id}/tags")]
    public async Task<ActionResult> AddTags(
        Guid id,
        [FromBody] AddTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddTagsCommand(id, request.Tags);
        await _mediator.Send(command, cancellationToken);
        
        return NoContent();
    }
}

public record RenameFileRequest(string NewName);
public record AddTagsRequest(List<string> Tags);
