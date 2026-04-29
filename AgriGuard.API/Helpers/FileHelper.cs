using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.Helpers
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 2 * 1024 * 1024; // 2MB

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

        public static string GenerateSafeFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            return $"{Guid.NewGuid()}{extension}";
        }
    }
}