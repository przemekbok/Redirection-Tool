using System;
using System.Configuration;
using System.Net;
using System.Security;
using Microsoft.SharePoint.Client;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Linq;

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
            // Try different authentication methods based on configuration
            
            // Method 1: If certificate thumbprint is provided, use High-Trust authentication
            if (!string.IsNullOrEmpty(_certificateThumbprint))
            {
                return GetHighTrustContext(siteUrl);
            }
            
            // Method 2: If client secret is provided, use app-only authentication
            if (!string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret))
            {
                return GetAppOnlyContext(siteUrl);
            }
            
            // Method 3: Fall back to current user credentials (Windows authentication)
            return GetUserContext(siteUrl);
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

        private ClientContext GetAppOnlyContext(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            
            try
            {
                // For SharePoint Online, use standard app-only authentication
                // For SharePoint 2016 on-premises, this might need adjustment based on your OAuth configuration
                
                var siteUri = new Uri(siteUrl);
                var realm = GetRealmFromTargetUrl(siteUri);
                
                // Simple implementation for SharePoint app-only auth
                var authManager = new OfficeDevPnP.Core.AuthenticationManager();
                context = authManager.GetAppOnlyAuthenticatedContext(siteUrl, _clientId, _clientSecret);
            }
            catch (Exception ex)
            {
                // If app-only fails, provide guidance
                throw new InvalidOperationException(
                    "App-only authentication failed. For SharePoint 2016 on-premises, ensure:\n" +
                    "1. The app is properly registered in SharePoint\n" +
                    "2. App permissions are granted\n" +
                    "3. OAuth is configured with your STS/ADFS\n" +
                    "Original error: " + ex.Message, ex);
            }
            
            return context;
        }

        private ClientContext GetUserContext(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            
            // Use current Windows credentials
            context.Credentials = CredentialCache.DefaultNetworkCredentials;
            
            // Alternatively, prompt for credentials
            // context.Credentials = GetUserCredentials();
            
            return context;
        }

        private NetworkCredential GetUserCredentials()
        {
            // In a real application, you might want to:
            // 1. Show a credential dialog
            // 2. Use stored credentials
            // 3. Use Windows Credential Manager
            
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            
            Console.Write("Enter password: ");
            var password = GetSecurePassword();
            
            Console.Write("Enter domain (optional): ");
            var domain = Console.ReadLine();
            
            return new NetworkCredential(username, password, domain);
        }

        private SecureString GetSecurePassword()
        {
            var securePassword = new SecureString();
            ConsoleKeyInfo key;
            
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    securePassword.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && securePassword.Length > 0)
                {
                    securePassword.RemoveAt(securePassword.Length - 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);
            
            Console.WriteLine();
            return securePassword;
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
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("aud", $"{clientId}/{new Uri(siteUrl).Host}@{realm}"),
                new System.Security.Claims.Claim("iss", $"{issuerId}@{realm}"),
                new System.Security.Claims.Claim("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new System.Security.Claims.Claim("exp", DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString()),
                new System.Security.Claims.Claim("nameid", $"{clientId}@{realm}"),
                new System.Security.Claims.Claim("trustedfordelegation", "true")
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
