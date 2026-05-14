using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.Helpers
{
    // Helper methods for image validation and file handling
    public static class FileHelper
    {
        // Supported image extensions
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        // Maximum allowed image size (2MB)
        private const long MaxFileSize = 2 * 1024 * 1024;

        // Validate uploaded image file
        public static bool IsValidImage(IFormFile file, out string errorMessage)
        {
            errorMessage = "";

            if (file == null || file.Length == 0)
            {
                errorMessage = "File is empty";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!AllowedExtensions.Contains(extension))
            {
                errorMessage = "Only JPG, JPEG, PNG are allowed";
                return false;
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                errorMessage = "Invalid file type";
                return false;
            }

            if (file.Length > MaxFileSize)
            {
                errorMessage = "File size must be less than 2MB";
                return false;
            }

            return true;
        }

        // Generate unique file name to avoid conflicts
        public static string GenerateSafeFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            return $"{Guid.NewGuid()}{extension}";
        }
    }
}