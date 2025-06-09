using System;
using System.Configuration;
using System.Net;
using System.Security;
using Microsoft.SharePoint.Client;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MigratedSiteRedirectionApp.Service
{
    public class SharePointAuthenticationHelper
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _certificateThumbprint;
        private readonly string _issuerId;

        public SharePointAuthenticationHelper()
        {
            _clientId = ConfigurationManager.AppSettings["SharePointClientId"];
            _clientSecret = ConfigurationManager.AppSettings["SharePointClientSecret"];
            _certificateThumbprint = ConfigurationManager.AppSettings["SharePointCertificateThumbprint"];
            _issuerId = ConfigurationManager.AppSettings["SharePointIssuerId"];
        }

        public ClientContext GetAuthenticatedContext(string siteUrl)
        {
            try
            {
                // Try different authentication methods based on configuration
                
                // Method 1: If certificate thumbprint is provided, use High-Trust authentication
                if (!string.IsNullOrEmpty(_certificateThumbprint))
                {
                    return GetHighTrustContext(siteUrl);
                }
                
                // Method 2: If client secret is provided, use SharePoint app authentication
                if (!string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret))
                {
                    return GetAppAuthContext(siteUrl);
                }
                
                // Method 3: Fall back to user credentials
                return GetUserCredentialContext(siteUrl);
            }
            catch (Exception ex)
            {
                // If all authentication methods fail, show error dialog
                MessageBox.Show(
                    $"Authentication failed. Please check your configuration.\n\nError: {ex.Message}",
                    "SharePoint Authentication Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }
        }

        private ClientContext GetHighTrustContext(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            
            try
            {
                // Load certificate from store
                var certificate = GetCertificateFromStore(_certificateThumbprint);
                if (certificate == null)
                {
                    throw new InvalidOperationException($"Certificate with thumbprint {_certificateThumbprint} not found in certificate store.");
                }

                // Create access token using certificate
                var realm = GetRealmFromTargetUrl(new Uri(siteUrl));
                var accessToken = CreateHighTrustAccessToken(_clientId, _issuerId, realm, siteUrl, certificate);
                
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to authenticate using High-Trust certificate.", ex);
            }
            
            return context;
        }

        private ClientContext GetAppAuthContext(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            
            try
            {
                // For SharePoint 2016 on-premises with registered app
                var siteUri = new Uri(siteUrl);
                var realm = GetRealmFromTargetUrl(siteUri);
                
                // Use SharePoint app-only authentication token
                var accessToken = GetAppOnlyAccessToken(siteUri, realm);
                
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "App authentication failed. Ensure the app is registered and has proper permissions.", ex);
            }
            
            return context;
        }

        private ClientContext GetUserCredentialContext(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            
            // First try default network credentials (for domain-joined machines)
            try
            {
                context.Credentials = CredentialCache.DefaultNetworkCredentials;
                
                // Test the connection
                context.Load(context.Web, w => w.Title);
                context.ExecuteQuery();
                
                return context;
            }
            catch
            {
                // If default credentials fail, prompt for credentials
                var credentials = PromptForCredentials(siteUrl);
                if (credentials != null)
                {
                    context.Credentials = credentials;
                    return context;
                }
                
                throw new InvalidOperationException("Authentication cancelled by user.");
            }
        }

        private NetworkCredential PromptForCredentials(string siteUrl)
        {
            // Create a simple credential dialog
            var dialog = new Window
            {
                Title = "SharePoint Authentication",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var lblInfo = new System.Windows.Controls.TextBlock
            {
                Text = $"Enter credentials for:\n{siteUrl}",
                Margin = new Thickness(10),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            System.Windows.Controls.Grid.SetRow(lblInfo, 0);
            grid.Children.Add(lblInfo);

            var lblUsername = new System.Windows.Controls.Label { Content = "Username:", Margin = new Thickness(10, 5, 10, 0) };
            System.Windows.Controls.Grid.SetRow(lblUsername, 1);
            grid.Children.Add(lblUsername);

            var txtUsername = new System.Windows.Controls.TextBox { Margin = new Thickness(10, 0, 10, 5) };
            System.Windows.Controls.Grid.SetRow(txtUsername, 2);
            grid.Children.Add(txtUsername);

            var lblPassword = new System.Windows.Controls.Label { Content = "Password:", Margin = new Thickness(10, 5, 10, 0) };
            System.Windows.Controls.Grid.SetRow(lblPassword, 3);
            grid.Children.Add(lblPassword);

            var txtPassword = new System.Windows.Controls.PasswordBox { Margin = new Thickness(10, 0, 10, 5) };
            System.Windows.Controls.Grid.SetRow(txtPassword, 4);
            grid.Children.Add(txtPassword);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(5),
                IsDefault = true
            };

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 75,
                Margin = new Thickness(5),
                IsCancel = true
            };

            NetworkCredential result = null;

            btnOk.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtUsername.Text))
                {
                    var username = txtUsername.Text;
                    var password = txtPassword.SecurePassword;
                    
                    // Parse domain from username if provided
                    string domain = "";
                    if (username.Contains("\\"))
                    {
                        var parts = username.Split('\\');
                        domain = parts[0];
                        username = parts[1];
                    }
                    
                    result = new NetworkCredential(username, password, domain);
                    dialog.DialogResult = true;
                }
            };

            btnCancel.Click += (s, e) =>
            {
                dialog.DialogResult = false;
            };

            buttonPanel.Children.Add(btnOk);
            buttonPanel.Children.Add(btnCancel);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 5);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            
            return dialog.ShowDialog() == true ? result : null;
        }

        private string GetAppOnlyAccessToken(Uri siteUri, string realm)
        {
            // For SharePoint 2016 on-premises, you need to implement based on your OAuth configuration
            // This is a placeholder that shows the structure
            
            var resource = $"{_clientId}/{siteUri.Host}@{realm}";
            var clientId = $"{_clientId}@{realm}";
            
            // In production, implement proper OAuth token request based on your STS configuration
            // This might involve:
            // 1. Getting token from ADFS
            // 2. Using ACS (Access Control Service) if configured
            // 3. Using custom STS implementation
            
            throw new NotImplementedException(
                "App-only token acquisition needs to be implemented based on your SharePoint 2016 OAuth configuration.\n" +
                "Please implement the token acquisition logic for your specific environment.");
        }

        private X509Certificate2 GetCertificateFromStore(string thumbprint)
        {
            var stores = new[] { StoreName.My, StoreName.Root };
            var locations = new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine };
            
            foreach (var location in locations)
            {
                foreach (var storeName in stores)
                {
                    using (var store = new X509Store(storeName, location))
                    {
                        store.Open(OpenFlags.ReadOnly);
                        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                        if (certificates.Count > 0)
                        {
                            return certificates[0];
                        }
                    }
                }
            }
            
            return null;
        }

        private string CreateHighTrustAccessToken(string clientId, string issuerId, string realm, string siteUrl, X509Certificate2 certificate)
        {
            // Create JWT token for High-Trust authentication
            var claims = new Dictionary<string, object>
            {
                { "aud", $"{clientId}/{new Uri(siteUrl).Host}@{realm}" },
                { "iss", $"{issuerId}@{realm}" },
                { "nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "exp", DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() },
                { "nameid", $"{clientId}@{realm}" },
                { "trustedfordelegation", "true" }
            };

            var header = new JwtHeader(
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.X509SecurityKey(certificate),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256));

            var payload = new JwtPayload(claims);
            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            
            return handler.WriteToken(token);
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
                    return null;
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
        }
    }
}
