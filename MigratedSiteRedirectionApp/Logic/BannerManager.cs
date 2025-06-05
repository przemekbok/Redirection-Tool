using System;
using System.Threading.Tasks;
using MigratedSiteRedirectionApp.Service;

namespace MigratedSiteRedirectionApp.Logic
{
    public class BannerManager
    {
        private readonly SharePointUrlValidator _urlValidator;
        private readonly SharePoint2016Service _sharePointService;

        public BannerManager()
        {
            _urlValidator = new SharePointUrlValidator();
            _sharePointService = new SharePoint2016Service();
        }

        public async Task<BannerOperationResult> ApplyBannerAsync(string siteUrl, string bannerMessage, string jsCode)
        {
            var result = new BannerOperationResult();

            try
            {
                // Step 1: Validate inputs
                if (string.IsNullOrWhiteSpace(bannerMessage))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Banner message cannot be empty.";
                    return result;
                }

                // Step 2: Validate URL
                var urlValidation = _urlValidator.ValidateUrl(siteUrl);
                if (!urlValidation.IsValid)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = urlValidation.ErrorMessage;
                    return result;
                }

                // Step 3: Extract site collection URL
                var siteCollectionUrl = _urlValidator.ExtractSiteCollectionUrl(siteUrl);
                if (string.IsNullOrEmpty(siteCollectionUrl))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Could not extract valid site collection URL.";
                    return result;
                }

                // Step 4: Apply banner and custom action
                result.ProcessedSiteUrl = siteCollectionUrl;
                var serviceResult = await _sharePointService.ApplyBannerAndCustomAction(
                    siteCollectionUrl, 
                    bannerMessage, 
                    jsCode);

                result.IsSuccess = serviceResult.IsSuccess;
                result.Message = serviceResult.Message;
                result.ErrorMessage = serviceResult.IsSuccess ? null : serviceResult.Message;

                if (serviceResult.Exception != null)
                {
                    result.Exception = serviceResult.Exception;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        public async Task<BannerOperationResult> RemoveBannerAsync(string siteUrl)
        {
            var result = new BannerOperationResult();

            try
            {
                // Validate URL
                var urlValidation = _urlValidator.ValidateUrl(siteUrl);
                if (!urlValidation.IsValid)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = urlValidation.ErrorMessage;
                    return result;
                }

                // Extract site collection URL
                var siteCollectionUrl = _urlValidator.ExtractSiteCollectionUrl(siteUrl);
                if (string.IsNullOrEmpty(siteCollectionUrl))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Could not extract valid site collection URL.";
                    return result;
                }

                // Remove banner and custom action
                result.ProcessedSiteUrl = siteCollectionUrl;
                var serviceResult = await _sharePointService.RemoveBannerAndCustomAction(siteCollectionUrl);

                result.IsSuccess = serviceResult.IsSuccess;
                result.Message = serviceResult.Message;
                result.ErrorMessage = serviceResult.IsSuccess ? null : serviceResult.Message;

                if (serviceResult.Exception != null)
                {
                    result.Exception = serviceResult.Exception;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }
    }

    public class BannerOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ProcessedSiteUrl { get; set; }
        public Exception Exception { get; set; }
    }
}