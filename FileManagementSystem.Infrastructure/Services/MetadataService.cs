using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Infrastructure.Services;

public class MetadataService : IMetadataService
{
    private static readonly string[] PhotoExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", 
        ".webp", ".heic", ".heif", ".raw", ".cr2", ".nef", ".orf", ".sr2"
    };
    
    public async Task<bool> IsPhotoFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return PhotoExtensions.Contains(extension);
    }
    
    public async Task<PhotoMetadata?> ExtractPhotoMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await IsPhotoFileAsync(filePath, cancellationToken))
        {
            return null;
        }
        
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            var imageInfo = await Image.IdentifyAsync(fileStream, cancellationToken);
            
            if (imageInfo?.Metadata?.ExifProfile == null)
            {
                return null;
            }
            
            var exif = imageInfo.Metadata.ExifProfile;
            
            // Extract date taken
            DateTime? dateTaken = null;
            if (exif.TryGetValue(ExifTag.DateTimeOriginal, out var dateTimeOriginal))
            {
                if (DateTime.TryParse(dateTimeOriginal.Value, out var parsedDate))
                {
                    dateTaken = parsedDate;
                }
            }
            else if (exif.TryGetValue(ExifTag.DateTime, out var dateTime))
            {
                if (DateTime.TryParse(dateTime.Value, out var parsedDate))
                {
                    dateTaken = parsedDate;
                }
            }
            
            // Extract camera make and model
            string? cameraMake = null;
            string? cameraModel = null;
            
            if (exif.TryGetValue(ExifTag.Make, out var make))
            {
                cameraMake = make.Value?.Trim();
            }
            
            if (exif.TryGetValue(ExifTag.Model, out var model))
            {
                cameraModel = model.Value?.Trim();
            }
            
            // Extract GPS coordinates
            double? latitude = null;
            double? longitude = null;
            
            if (exif.TryGetValue(ExifTag.GPSLatitude, out var gpsLat) &&
                exif.TryGetValue(ExifTag.GPSLatitudeRef, out var gpsLatRef) &&
                exif.TryGetValue(ExifTag.GPSLongitude, out var gpsLong) &&
                exif.TryGetValue(ExifTag.GPSLongitudeRef, out var gpsLongRef))
            {
                if (gpsLat.Value != null && gpsLong.Value != null)
                {
                    latitude = ConvertGpsCoordinate(gpsLat.Value, gpsLatRef.Value?.ToString());
                    longitude = ConvertGpsCoordinate(gpsLong.Value, gpsLongRef.Value?.ToString());
                }
            }
            
            return new PhotoMetadata
            {
                DateTaken = dateTaken,
                CameraMake = cameraMake,
                CameraModel = cameraModel,
                Latitude = latitude,
                Longitude = longitude
            };
        }
        catch
        {
            return null;
        }
    }
    
    private static double? ConvertGpsCoordinate(Rational[] rationals, string? refValue)
    {
        if (rationals == null || rationals.Length < 3)
        {
            return null;
        }
        
        var degrees = rationals[0].ToDouble();
        var minutes = rationals[1].ToDouble();
        var seconds = rationals[2].ToDouble();
        
        var decimalValue = degrees + minutes / 60.0 + seconds / 3600.0;
        
        if (refValue?.ToUpperInvariant() == "S" || refValue?.ToUpperInvariant() == "W")
        {
            decimalValue = -decimalValue;
        }
        
        return decimalValue;
    }
}
