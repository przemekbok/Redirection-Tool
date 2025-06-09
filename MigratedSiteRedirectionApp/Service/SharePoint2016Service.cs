using System;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using System.Linq;

namespace MigratedSiteRedirectionApp.Service
{
    public class SharePoint2016Service
    {
        private readonly SharePointAuthenticationHelper _authHelper;

        public SharePoint2016Service()
        {
            _authHelper = new SharePointAuthenticationHelper();
        }

        public async Task<ServiceResult> ApplyBannerAndCustomAction(string siteUrl, string bannerMessage, string jsCode)
        {
            try
            {
                using (var context = _authHelper.GetAuthenticatedContext(siteUrl))
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
                        Message = "Red banner and custom action have been successfully applied to the site collection."
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
                using (var context = _authHelper.GetAuthenticatedContext(siteUrl))
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

        private async Task RemoveExistingCustomActions(ClientContext context, UserCustomActionCollection userCustomActions)
        {
            var actionsToRemove = userCustomActions
                .Where(action => action.Name == "SharePointBannerManager_Banner" || 
                                action.Name == "SharePointBannerManager_CustomJS")
                .ToList();
            
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
        // For classic SharePoint pages using SP.UI.Status (red status bar)
        if (typeof SP !== 'undefined' && SP.UI && SP.UI.Status) {{
            SP.SOD.executeFunc('sp.js', 'SP.ClientContext', function() {{
                // Create a red status bar (more prominent than notification)
                var statusId = SP.UI.Status.addStatus('{escapedMessage}');
                SP.UI.Status.setStatusPriColor(statusId, 'red');
                
                // Store the status ID for potential removal
                window.SharePointBannerStatusId = statusId;
            }});
        }} else {{
            // Fallback for modern pages or when SP.UI is not available
            createFallbackBanner();
        }}
    }}
    
    function createFallbackBanner() {{
        if (!document.getElementById('sharepoint-banner-manager-banner')) {{
            var banner = document.createElement('div');
            banner.id = 'sharepoint-banner-manager-banner';
            banner.style.cssText = 'background-color: #dc3545; color: white; padding: 15px 20px; border-bottom: 2px solid #c82333; font-size: 14px; font-weight: bold; position: fixed; top: 0; left: 0; right: 0; z-index: 10000; box-shadow: 0 2px 4px rgba(0,0,0,0.2); text-align: center;';
            
            // Create message container
            var messageContainer = document.createElement('div');
            messageContainer.style.cssText = 'display: inline-block; max-width: 90%; margin: 0 auto;';
            messageContainer.innerHTML = '{escapedMessage}';
            
            // Add close button
            var closeBtn = document.createElement('span');
            closeBtn.style.cssText = 'position: absolute; right: 20px; top: 50%; transform: translateY(-50%); cursor: pointer; font-size: 20px; line-height: 1; opacity: 0.8;';
            closeBtn.innerHTML = '&times;';
            closeBtn.title = 'Close banner';
            closeBtn.onmouseover = function() {{ closeBtn.style.opacity = '1'; }};
            closeBtn.onmouseout = function() {{ closeBtn.style.opacity = '0.8'; }};
            closeBtn.onclick = function() {{
                banner.style.display = 'none';
                document.body.style.paddingTop = '0';
                // Store preference in session storage
                if (typeof sessionStorage !== 'undefined') {{
                    sessionStorage.setItem('sharepoint-banner-hidden', 'true');
                }}
            }};
            
            banner.appendChild(messageContainer);
            banner.appendChild(closeBtn);
            
            // Check if banner was previously closed in this session
            if (typeof sessionStorage !== 'undefined' && sessionStorage.getItem('sharepoint-banner-hidden') === 'true') {{
                return;
            }}
            
            document.body.insertBefore(banner, document.body.firstChild);
            document.body.style.paddingTop = (banner.offsetHeight + 'px');
            
            // Adjust padding on window resize
            window.addEventListener('resize', function() {{
                if (banner.style.display !== 'none') {{
                    document.body.style.paddingTop = (banner.offsetHeight + 'px');
                }}
            }});
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
    
    // For modern pages, also try on window load
    if (window.addEventListener) {{
        window.addEventListener('load', function() {{
            setTimeout(showRedBanner, 100);
        }});
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
