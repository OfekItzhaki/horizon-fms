using System;
using System.Reactive;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Interfaces;

/// <summary>
/// Provides an observable stream for progress reporting
/// </summary>
public interface IProgressObservable
{
    /// <summary>
    /// Observable stream of progress reports
    /// </summary>
    IObservable<ProgressReportDto> Progress { get; }
    
    /// <summary>
    /// Report progress
    /// </summary>
    void Report(ProgressReportDto progress);
    
    /// <summary>
    /// Complete the progress stream
    /// </summary>
    void Complete();
    
    /// <summary>
    /// Report an error
    /// </summary>
    void ReportError(Exception error);
}
