using Microsoft.AspNetCore.Http;

using System.Buffers.Binary;

namespace MemberTracker.Models;

public static class UploadValidationRules
{
    public const long MaxProofSizeBytes = 5 * 1024 * 1024;

    public const decimal MaxDistanceKm = 300;

    public const decimal KmPerMile = 1.609344m;

    public const int MinMovingTimeSec = 60;

    public const int MaxMovingTimeSec = 86400;

    public const int MinSteps = 1;

    public const int MaxSteps = 100000;

    public static decimal MaxDistanceMi => MaxDistanceKm / KmPerMile;

    public static readonly HashSet<string> AllowedActivities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Run",
        "Walk",
        "Ride",
        "Training",
    };

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    public static bool BeAllowedActivity(string activityName)
        => AllowedActivities.Contains(activityName.Trim());

    public static bool BeNotMoreThanOneDayAhead(DateTime activityDate)
        => activityDate.Date <= DateTime.UtcNow.Date.AddDays(1);

    public static bool HaveAllowedExtension(IFormFile proof)
    {
        var extension = Path.GetExtension(proof.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public static bool HaveAllowedContentType(IFormFile proof)
    {
        var contentType = NormalizeContentType(proof.ContentType);
        return !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType);
    }

    public static bool HaveMatchingExtensionAndContentType(IFormFile proof)
    {
        var extension = NormalizeExtension(proof.FileName);
        var contentType = NormalizeContentType(proof.ContentType);

        if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return ExtensionToContentType.TryGetValue(extension, out var expectedContentType) &&
               string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HaveMatchingSignature(IFormFile proof)
    {
        var extension = NormalizeExtension(proof.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        using var stream = proof.OpenReadStream();

        return extension switch
        {
            ".jpg" or ".jpeg" => IsJpegSignature(stream),
            ".png" => IsPngSignature(stream),
            ".webp" => IsWebpSignature(stream),
            _ => false,
        };
    }

    public static bool HaveValidImageStructure(IFormFile proof)
    {
        var extension = NormalizeExtension(proof.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        using var stream = proof.OpenReadStream();

        return extension switch
        {
            ".jpg" or ".jpeg" => HasValidJpegStructure(stream),
            ".png" => HasValidPngStructure(stream),
            ".webp" => HasValidWebpStructure(stream),
            _ => false,
        };
    }

    public static bool HaveConsistentDistanceAndTime(decimal distanceKm, int movingTimeSec)
        => distanceKm > 0 && movingTimeSec > 0;

    public static decimal? ResolveDistanceKm(decimal? distanceKm, decimal? distanceMi)
    {
        var hasKm = distanceKm.HasValue;
        var hasMi = distanceMi.HasValue;
        if (hasKm == hasMi)
        {
            return null;
        }

        if (hasKm)
        {
            return distanceKm.GetValueOrDefault();
        }

        return ConvertMilesToKm(distanceMi.GetValueOrDefault());
    }

    public static decimal ConvertMilesToKm(decimal miles)
        => miles * KmPerMile;

    public static decimal ConvertKmToMiles(decimal kilometers)
        => kilometers / KmPerMile;

    private static string NormalizeExtension(string fileName)
        => Path.GetExtension(fileName).Trim().ToLowerInvariant();

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var separatorIndex = contentType.IndexOf(';');
        var rawType = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        return rawType.Trim().ToLowerInvariant();
    }

    private static bool IsJpegSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[3];
        return TryReadAtStart(stream, header) &&
               header[0] == 0xFF &&
               header[1] == 0xD8 &&
               header[2] == 0xFF;
    }

    private static bool IsPngSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[8];
        return TryReadAtStart(stream, header) &&
               header[0] == 0x89 &&
               header[1] == 0x50 &&
               header[2] == 0x4E &&
               header[3] == 0x47 &&
               header[4] == 0x0D &&
               header[5] == 0x0A &&
               header[6] == 0x1A &&
               header[7] == 0x0A;
    }

    private static bool IsWebpSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        return TryReadAtStart(stream, header) &&
               header[..4].SequenceEqual("RIFF"u8) &&
               header[8..12].SequenceEqual("WEBP"u8);
    }

    private static bool HasValidJpegStructure(Stream stream)
    {
        if (!IsJpegSignature(stream) || stream.Length < 4 || !stream.CanSeek)
        {
            return false;
        }

        stream.Seek(-2, SeekOrigin.End);
        Span<byte> trailer = stackalloc byte[2];
        return TryReadFully(stream, trailer) && trailer[0] == 0xFF && trailer[1] == 0xD9;
    }

    private static bool HasValidPngStructure(Stream stream)
    {
        if (!IsPngSignature(stream))
        {
            return false;
        }

        Span<byte> headerBlock = stackalloc byte[24];
        if (!TryReadAtStart(stream, headerBlock))
        {
            return false;
        }

        var ihdrLength = BinaryPrimitives.ReadUInt32BigEndian(headerBlock[8..12]);
        var hasIhdrChunk = headerBlock[12..16].SequenceEqual("IHDR"u8);
        var width = BinaryPrimitives.ReadUInt32BigEndian(headerBlock[16..20]);
        var height = BinaryPrimitives.ReadUInt32BigEndian(headerBlock[20..24]);

        return ihdrLength == 13 && hasIhdrChunk && width > 0 && height > 0;
    }

    private static bool HasValidWebpStructure(Stream stream)
    {
        Span<byte> header = stackalloc byte[16];
        if (!TryReadAtStart(stream, header))
        {
            return false;
        }

        if (!header[..4].SequenceEqual("RIFF"u8) || !header[8..12].SequenceEqual("WEBP"u8))
        {
            return false;
        }

        return header[12..16].SequenceEqual("VP8 "u8) ||
               header[12..16].SequenceEqual("VP8L"u8) ||
               header[12..16].SequenceEqual("VP8X"u8);
    }

    private static bool TryReadAtStart(Stream stream, Span<byte> buffer)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        return TryReadFully(stream, buffer);
    }

    private static bool TryReadFully(Stream stream, Span<byte> buffer)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = stream.Read(buffer[totalRead..]);
            if (bytesRead == 0)
            {
                return false;
            }

            totalRead += bytesRead;
        }

        return true;
    }
}
