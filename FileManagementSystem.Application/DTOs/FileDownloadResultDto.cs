using System;

namespace FileManagementSystem.Application.DTOs;

public record FileDownloadResultDto(
    byte[] Content,
    string MimeType,
    string FileName,
    bool WasCompressed
);
