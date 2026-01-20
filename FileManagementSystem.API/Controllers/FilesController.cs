using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Queries;
using System.Collections.Generic;
using System.Linq;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var query = new GetFileQuery(id);
        var file = await _mediator.Send(query, cancellationToken);
        
        if (file == null)
        {
            _logger.LogWarning("Download attempt for non-existent file ID: {FileId}", id);
            return NotFound();
        }

        try
        {
            var storageService = HttpContext.RequestServices.GetRequiredService<FileManagementSystem.Application.Interfaces.IStorageService>();
            
            // The database stores the display path (without .gz), but the file on disk has .gz if compressed
            var storedPath = file.Path;
            _logger.LogInformation("Download request: FileId={FileId}, StoredPath={StoredPath}, IsCompressed={IsCompressed}", 
                id, storedPath, file.IsCompressed);
            
            string actualFilePath = null;
            var triedPaths = new List<string>();
            
            // Build list of paths to try
            // Always try both compressed and uncompressed versions to handle old files
            var pathsToTry = new List<string>();
            
            // Helper to add both compressed and uncompressed versions
            void AddPathVariations(string basePath)
            {
                if (string.IsNullOrEmpty(basePath)) return;
                
                // Add uncompressed version (original path)
                pathsToTry.Add(basePath);
                
                // Add compressed version (with .gz) if it doesn't already have .gz
                if (!basePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    pathsToTry.Add(basePath + ".gz");
                }
            }
            
            // 1. The stored path as-is (try both compressed and uncompressed)
            AddPathVariations(storedPath);
            
            // 2. If stored path is relative, try resolving it
            if (!Path.IsPathRooted(storedPath))
            {
                // Try relative to storage directory
                var storageBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FileManagementSystem",
                    "Storage"
                );
                var resolvedPath = Path.GetFullPath(Path.Combine(storageBasePath, storedPath.TrimStart('\\', '/')));
                AddPathVariations(resolvedPath);
                
                // Try relative to current working directory
                try
                {
                    var workingDirPath = Path.GetFullPath(storedPath);
                    if (workingDirPath != storedPath)
                    {
                        AddPathVariations(workingDirPath);
                    }
                }
                catch
                {
                    // Ignore if path is invalid
                }
            }
            
            // Try each path until we find the file
            foreach (var pathToTry in pathsToTry.Distinct())
            {
                triedPaths.Add(pathToTry);
                var exists = System.IO.File.Exists(pathToTry);
                _logger.LogInformation("Checking path: {Path} | Exists: {Exists}", pathToTry, exists);
                if (exists)
                {
                    actualFilePath = pathToTry;
                    _logger.LogInformation("Found file at: {FilePath}", actualFilePath);
                    break;
                }
            }
            
            // If still not found and path is absolute, try to list the directory to see what files are there
            if (actualFilePath == null && Path.IsPathRooted(storedPath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(storedPath);
                    if (!string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory))
                    {
                        var filesInDir = System.IO.Directory.GetFiles(directory);
                        _logger.LogWarning("Directory exists but file not found. Files in directory: {Files}", string.Join(", ", filesInDir.Select(Path.GetFileName)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not list directory contents");
                }
            }

            if (actualFilePath == null)
            {
                _logger.LogError("File not found on disk for ID: {FileId}", id);
                _logger.LogError("Stored path in DB: {StoredPath}", storedPath);
                _logger.LogError("IsCompressed flag: {IsCompressed}", file.IsCompressed);
                _logger.LogError("Tried {Count} paths:", triedPaths.Count);
                foreach (var triedPath in triedPaths)
                {
                    var exists = System.IO.File.Exists(triedPath);
                    _logger.LogError("  Path: {Path} | Exists: {Exists}", triedPath, exists);
                }
                return NotFound("File not found on server. Please check the API logs for details. The file may need to be re-uploaded.");
            }

            // Read file (automatically decompresses if needed)
            var isActuallyCompressed = actualFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
            var fileData = await storageService.ReadFileAsync(actualFilePath, isActuallyCompressed, cancellationToken);
            
            // Use the original filename from database (FileName property) if available
            // This ensures the downloaded file has the correct original name the user uploaded
            var downloadFileName = !string.IsNullOrEmpty(file.FileName) 
                ? file.FileName 
                : fileData.OriginalFileName;
            
            _logger.LogDebug("File read for download: ID={FileId}, DatabaseFileName={DatabaseFileName}, ExtractedFileName={ExtractedFileName}, WasCompressed={WasCompressed}, Size={Size}", 
                id, file.FileName, fileData.OriginalFileName, fileData.WasCompressed, fileData.Content.Length);
            
            return File(fileData.Content, file.MimeType, downloadFileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found for download: ID={FileId}, Path={FilePath}", id, file.Path);
            return NotFound("File not found on server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file for download: ID={FileId}, Path={FilePath}", id, file.Path);
            return StatusCode(500, "Error reading file");
        }
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
    /// Delete a file
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFile(
        Guid id,
        [FromQuery] bool moveToRecycleBin = true,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteFileCommand(id, moveToRecycleBin);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.Success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    /// <summary>
    /// Rename a file
    /// </summary>
    [HttpPut("{id}/rename")]
    public async Task<ActionResult<RenameFileResult>> RenameFile(
        Guid id,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RenameFileCommand(id, request.NewName);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.Success)
        {
            return NotFound();
        }
        
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
