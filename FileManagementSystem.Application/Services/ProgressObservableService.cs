using System;
using System.Reactive;
using System.Reactive.Subjects;
using FileManagementSystem.Application.DTOs;
using FileManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Application.Services;

/// <summary>
/// Reactive progress reporting service using Rx.NET
/// </summary>
public class ProgressObservableService : IProgressObservable, IDisposable
{
    private readonly Subject<ProgressReportDto> _progressSubject;
    private readonly ILogger<ProgressObservableService> _logger;
    private bool _disposed;

    public ProgressObservableService(ILogger<ProgressObservableService> logger)
    {
        _logger = logger;
        _progressSubject = new Subject<ProgressReportDto>();
    }

    public IObservable<ProgressReportDto> Progress => _progressSubject.AsObservable();

    public void Report(ProgressReportDto progress)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to report progress on disposed service");
            return;
        }

        try
        {
            _progressSubject.OnNext(progress);
            _logger.LogDebug("Progress reported: {Processed}/{Total} - {CurrentItem}", 
                progress.ProcessedItems, progress.TotalItems, progress.CurrentItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress");
        }
    }

    public void Complete()
    {
        if (_disposed) return;

        try
        {
            _progressSubject.OnCompleted();
            _logger.LogDebug("Progress stream completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing progress stream");
        }
    }

    public void ReportError(Exception error)
    {
        if (_disposed) return;

        try
        {
            _progressSubject.OnError(error);
            _logger.LogError(error, "Progress stream error reported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress stream error");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _progressSubject?.OnCompleted();
            _progressSubject?.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing progress service");
        }
    }
}
