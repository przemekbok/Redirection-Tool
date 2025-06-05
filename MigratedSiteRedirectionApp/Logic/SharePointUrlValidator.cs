using System;
using System.Text.RegularExpressions;

namespace MigratedSiteRedirectionApp.Logic
{
    public class SharePointUrlValidator
    {
        private static readonly Regex SharePointUrlPattern = new Regex(
            @"^https?://[\w\-._~:/?#[\]@!$&'()*+,;=]+\.sharepoint\.com(/.+)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ValidationResult ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "URL cannot be empty."
                };
            }

            // Check if it's a valid URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "The provided URL is not a valid URL format."
                };
            }

            // Check if it's HTTPS (recommended for SharePoint)
            if (uri.Scheme != "https" && uri.Scheme != "http")
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "URL must use HTTP or HTTPS protocol."
                };
            }

            // Check if it matches SharePoint URL pattern
            if (!SharePointUrlPattern.IsMatch(url))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "URL must be a valid SharePoint site collection URL (e.g., https://company.sharepoint.com/sites/sitename)."
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                ErrorMessage = null,
                ParsedUri = uri
            };
        }

        public string ExtractSiteCollectionUrl(string url)
        {
            var validationResult = ValidateUrl(url);
            if (!validationResult.IsValid || validationResult.ParsedUri == null)
            {
                return null;
            }

            var uri = validationResult.ParsedUri;
            
            // Extract site collection URL (typically up to /sites/sitename or /teams/teamname)
            var path = uri.AbsolutePath.ToLowerInvariant();
            
            if (path.StartsWith("/sites/") || path.StartsWith("/teams/"))
            {
                var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2)
                {
                    return $"{uri.Scheme}://{uri.Host}/{segments[0]}/{segments[1]}";
                }
            }
            
            // If no sites/teams pattern, return base URL
            return $"{uri.Scheme}://{uri.Host}";
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public Uri ParsedUri { get; set; }
    }
}