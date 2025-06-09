# SharePoint Banner Manager

A WPF application for managing red notification banners on SharePoint 2016 site collections using CSOM (Client Side Object Model) with app-only authentication.

## Features

- Apply red notification banners to SharePoint 2016 site collections
- Support for custom JavaScript code injection
- Multiple authentication methods:
  - App-only authentication with Client ID/Secret
  - High-Trust authentication with certificates
  - Windows authentication (default credentials or prompted)
- Automatic duplicate prevention
- Modern and classic SharePoint page support

## Prerequisites

- .NET 8.0 SDK
- SharePoint 2016 on-premises environment
- Appropriate permissions to manage site collection custom actions
- For app-only authentication: Registered SharePoint app with proper permissions

## Configuration

### 1. App-Only Authentication

Edit the `App.config` file and provide your SharePoint app credentials:

```xml
<appSettings>
    <add key="SharePointClientId" value="YOUR_CLIENT_ID_HERE" />
    <add key="SharePointClientSecret" value="YOUR_CLIENT_SECRET_HERE" />
</appSettings>
```

### 2. High-Trust Authentication (Certificate)

For High-Trust apps, configure the certificate settings:

```xml
<appSettings>
    <add key="SharePointCertificateThumbprint" value="YOUR_CERT_THUMBPRINT" />
    <add key="SharePointIssuerId" value="YOUR_ISSUER_ID" />
</appSettings>
```

### 3. Windows Authentication

If no app credentials are configured, the application will use Windows authentication (either default credentials or prompt for credentials).

## Usage

1. Run the application
2. Enter the SharePoint site collection URL (e.g., `https://company.sharepoint.com/sites/sitename`)
3. Enter the banner message (supports HTML)
4. Optionally add custom JavaScript code
5. Click "Apply Action" to deploy the banner

## Banner Behavior

The banner appears as a red notification bar at the top of SharePoint pages:
- **Classic Pages**: Uses SharePoint's `SP.UI.Status` API for native red status bar
- **Modern Pages**: Falls back to custom HTML banner with similar styling
- Includes a close button (X) that hides the banner for the session
- Automatically adjusts page padding to prevent content overlap

## Technical Details

### Custom Actions

The application creates two custom actions:
1. `SharePointBannerManager_Banner` - Contains the banner display logic
2. `SharePointBannerManager_CustomJS` - Contains any additional JavaScript code

### Authentication Flow

1. First attempts app-only authentication if credentials are configured
2. Falls back to certificate-based High-Trust authentication if configured
3. Finally falls back to Windows authentication (with credential prompt if needed)

### Red Banner Implementation

The banner uses SharePoint's native status bar for classic pages:
```javascript
SP.UI.Status.addStatus('Your message here');
SP.UI.Status.setStatusPriColor(statusId, 'red');
```

For modern pages or when SP.UI is unavailable, it creates a custom HTML banner with equivalent styling.

## Troubleshooting

### Authentication Issues

- **App-Only**: Ensure the app is properly registered and has site collection permissions
- **Certificate**: Verify the certificate is installed in the certificate store
- **Windows**: Ensure you have proper permissions on the SharePoint site

### Banner Not Appearing

1. Check if custom actions are enabled on the site collection
2. Verify JavaScript is not blocked by browser policies
3. Check browser console for any JavaScript errors

### OAuth Configuration for SharePoint 2016

For on-premises SharePoint 2016, app-only authentication requires:
1. Configured OAuth with your STS/ADFS
2. Properly registered SharePoint app
3. Granted app permissions at the site collection level

## Development

### Building from Source

```bash
git clone https://github.com/przemekbok/Redirection-Tool.git
cd Redirection-Tool
git checkout feature/implement-sharepoint-banner-csom
dotnet restore
dotnet build
```

### Required NuGet Packages

- Microsoft.SharePointOnline.CSOM (16.1.24723.12000)
- System.Configuration.ConfigurationManager (8.0.0)
- Microsoft.Identity.Client (4.61.3)
- System.IdentityModel.Tokens.Jwt (7.6.0)

## Security Considerations

- Store app credentials securely (consider using encrypted configuration)
- Limit app permissions to minimum required
- Be cautious with custom JavaScript injection
- Test thoroughly in non-production environments first

## License

This project is part of the Redirection-Tool repository.
