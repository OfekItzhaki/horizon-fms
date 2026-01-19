namespace FileManagementSystem.Application.DTOs;

public record ProgressReportDto
{
    public int ProcessedItems { get; init; }
    public int TotalItems { get; init; }
    public double Percentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
    public string CurrentItem { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}
