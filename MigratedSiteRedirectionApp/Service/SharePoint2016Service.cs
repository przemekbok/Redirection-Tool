using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using System.Configuration;
using System.Net;

namespace MigratedSiteRedirectionApp.Service
{
    public class SharePoint2016Service
    {
        private readonly string _clientId;
        private readonly string _clientSecret;

        public SharePoint2016Service()
        {
            // Load configuration from app settings
            _clientId = ConfigurationManager.AppSettings["SharePointClientId"];
            _clientSecret = ConfigurationManager.AppSettings["SharePointClientSecret"];
        }

        public async Task<ServiceResult> ApplyBannerAndCustomAction(string siteUrl, string bannerMessage, string jsCode)
        {
            try
            {
                using (var context = GetClientContext(siteUrl))
                {
                    var site = context.Site;
                    var userCustomActions = site.UserCustomActions;
                    
                    context.Load(userCustomActions);
                    await Task.Run(() => context.ExecuteQuery());
                    
                    // Remove existing banner custom actions to avoid duplicates
                    await RemoveExistingCustomActions(context, userCustomActions);
                    
                    // Create banner custom action
                    var bannerAction = userCustomActions.Add();
                    bannerAction.Name = "SharePointBannerManager_Banner";
                    bannerAction.Location = "ScriptLink";
                    bannerAction.Sequence = 1000;
                    bannerAction.ScriptBlock = GenerateRedBannerScript(bannerMessage);
                    bannerAction.Update();
                    
                    // Create JS custom action if JS code is provided
                    if (!string.IsNullOrWhiteSpace(jsCode))
                    {
                        var jsAction = userCustomActions.Add();
                        jsAction.Name = "SharePointBannerManager_CustomJS";
                        jsAction.Location = "ScriptLink";
                        jsAction.Sequence = 1001;
                        jsAction.ScriptBlock = jsCode;
                        jsAction.Update();
                    }
                    
                    await Task.Run(() => context.ExecuteQuery());
                    
                    return new ServiceResult
                    {
                        IsSuccess = true,
                        Message = "Banner and custom action have been successfully applied to the site collection."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = $"Failed to apply banner and custom action: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<ServiceResult> RemoveBannerAndCustomAction(string siteUrl)
        {
            try
            {
                using (var context = GetClientContext(siteUrl))
                {
                    var site = context.Site;
                    var userCustomActions = site.UserCustomActions;
                    
                    context.Load(userCustomActions);
                    await Task.Run(() => context.ExecuteQuery());
                    
                    await RemoveExistingCustomActions(context, userCustomActions);
                    
                    return new ServiceResult
                    {
                        IsSuccess = true,
                        Message = "Banner and custom action have been successfully removed from the site collection."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = $"Failed to remove banner and custom action: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private ClientContext GetClientContext(string siteUrl)
        {
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new InvalidOperationException("SharePoint Client ID and Client Secret must be configured in app.config");
            }

            var context = new ClientContext(siteUrl);
            
            // App-only authentication for SharePoint 2016
            var realm = GetRealmFromTargetUrl(new Uri(siteUrl));
            var accessToken = GetAppOnlyAccessToken(_clientId, _clientSecret, siteUrl, realm);
            
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return context;
        }

        private string GetRealmFromTargetUrl(Uri targetApplicationUri)
        {
            WebRequest request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
            request.Headers.Add("Authorization: Bearer ");
            request.Method = "GET";

            try
            {
                using (var response = request.GetResponse())
                {
                    // This will fail but the header will contain the realm
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    return null;

                var bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
                if (string.IsNullOrEmpty(bearerResponseHeader))
                    return null;

                const string bearer = "Bearer realm=\"";
                var bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);
                if (bearerIndex < 0)
                    return null;

                var realmIndex = bearerIndex + bearer.Length;
                if (bearerResponseHeader.Length < realmIndex + 36)
                    return null;

                return bearerResponseHeader.Substring(realmIndex, 36);
            }

            return null;
        }

        private string GetAppOnlyAccessToken(string clientId, string clientSecret, string siteUrl, string realm)
        {
            // For SharePoint 2016 on-premises, you might need to adjust this based on your STS configuration
            // This is a simplified version - in production, use proper OAuth implementation
            
            // Note: For on-premises SharePoint 2016, you might need to use:
            // - High Trust Provider-Hosted Apps with certificate authentication
            // - Or configure OAuth with your ADFS/STS
            
            throw new NotImplementedException(
                "App-only authentication implementation depends on your SharePoint 2016 configuration. " +
                "Please implement based on your authentication setup (High Trust Apps or OAuth with ADFS).");
        }

        private async Task RemoveExistingCustomActions(ClientContext context, UserCustomActionCollection userCustomActions)
        {
            var actionsToRemove = new System.Collections.Generic.List<UserCustomAction>();
            
            foreach (var action in userCustomActions)
            {
                if (action.Name == "SharePointBannerManager_Banner" || 
                    action.Name == "SharePointBannerManager_CustomJS")
                {
                    actionsToRemove.Add(action);
                }
            }
            
            foreach (var action in actionsToRemove)
            {
                action.DeleteObject();
            }
            
            if (actionsToRemove.Count > 0)
            {
                await Task.Run(() => context.ExecuteQuery());
            }
        }

        private string GenerateRedBannerScript(string bannerMessage)
        {
            // Escape the banner message for JavaScript
            var escapedMessage = bannerMessage
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            // Generate JavaScript to show RED banner using SharePoint's notification framework
            return $@"
(function() {{
    'use strict';
    
    function showRedBanner() {{
        // For classic SharePoint pages using SP.UI.Notify
        if (typeof SP !== 'undefined' && SP.UI && SP.UI.Notify) {{
            SP.SOD.executeFunc('sp.js', 'SP.ClientContext', function() {{
                // Create a custom notification with red background
                var notificationHtml = '<div style=""background-color: #dc3545; color: white; padding: 10px; margin: -10px; font-weight: bold;"">' + 
                                      '{escapedMessage}' + 
                                      '</div>';
                var notificationId = SP.UI.Notify.addNotification(notificationHtml, false);
                
                // Make the notification persistent (don't auto-hide)
                window.SharePointBannerNotificationId = notificationId;
            }});
        }} else {{
            // Fallback for modern pages or when SP.UI is not available
            if (!document.getElementById('sharepoint-banner-manager-banner')) {{
                var banner = document.createElement('div');
                banner.id = 'sharepoint-banner-manager-banner';
                banner.style.cssText = 'background-color: #dc3545; color: white; padding: 15px 20px; border-bottom: 2px solid #c82333; font-size: 14px; font-weight: bold; position: fixed; top: 0; left: 0; right: 0; z-index: 10000; box-shadow: 0 2px 4px rgba(0,0,0,0.2);';
                banner.innerHTML = '{escapedMessage}';
                
                // Add close button
                var closeBtn = document.createElement('span');
                closeBtn.style.cssText = 'float: right; cursor: pointer; font-size: 20px; line-height: 1; margin-left: 15px;';
                closeBtn.innerHTML = '&times;';
                closeBtn.onclick = function() {{
                    banner.style.display = 'none';
                    document.body.style.paddingTop = '0';
                }};
                banner.insertBefore(closeBtn, banner.firstChild);
                
                document.body.insertBefore(banner, document.body.firstChild);
                document.body.style.paddingTop = (banner.offsetHeight + 'px');
            }}
        }}
    }}
    
    // Execute when DOM is ready
    if (document.readyState === 'loading') {{
        document.addEventListener('DOMContentLoaded', showRedBanner);
    }} else {{
        showRedBanner();
    }}
    
    // Also execute on SP page load for classic pages
    if (typeof _spBodyOnLoadFunctionNames !== 'undefined') {{
        _spBodyOnLoadFunctionNames.push('showRedBanner');
        window.showRedBanner = showRedBanner;
    }}
}})();
";
        }
    }

    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
