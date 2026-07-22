# SunyaSuite — Localhost IIS Deployment Guide

## Prerequisites

- Windows 10/11 or Windows Server with IIS enabled
- .NET 10 Hosting Bundle
- PostgreSQL 18 (Docker or native)
- IIS URL Rewrite Module ([download](https://www.iis.net/downloads/microsoft/url-rewrite))

---

## Step 1: Database

**Option A — Docker (recommended):**

```powershell
docker run -d --name sunya-db `
  -e POSTGRES_PASSWORD=postgres `
  -p 5433:5432 `
  -v sunya_pgdata:/var/lib/postgresql/data `
  postgres:18
```

**Option B — Native PostgreSQL installer:**

Create two databases:

```sql
CREATE DATABASE "SunyaSuite";
CREATE DATABASE "SunyaSuite_Config";
```

> The API runs migrations and seeds data automatically on first startup — no manual `CREATE DATABASE` needed if `Database.MigrateAsync()` handles it, but create them upfront to be safe.

---

## Step 2: Publish the Web API to Desktop

From the repo root:

```powershell
dotnet publish src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output "$env:USERPROFILE\Desktop\SunyaSuiteApi"
```

---

## Step 3: Configure API — `appsettings.Production.json`

Create `"$env:USERPROFILE\Desktop\SunyaSuiteApi\appsettings.Production.json"`:

```json
{
  "ConnectionStrings": {
    "TemplateConnection": "Host=localhost;Port=5433;Database=SunyaSuite;Username=postgres;Password=postgres",
    "ConfigConnection": "Host=localhost;Port=5433;Database=SunyaSuite_Config;Username=postgres;Password=postgres"
  },
  "ClientUrl": "http://localhost:8080",
  "ClientUrls": [ "http://localhost:8080" ],
  "Jwt": {
    "Secret": "<replace-with-random-64-char-string>",
    "Issuer": "SunyaSuite",
    "Audience": "SunyaSuiteClient",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 0,
    "Username": "",
    "Password": "",
    "FromAddress": "",
    "FromName": ""
  },
  "SeedData": {
    "AdminEmail": "admin@sunya.local",
    "AdminPassword": "Admin@123"
  },
  "DatabaseReset": {
    "ResetOnStartup": false,
    "SeedOnStartUp": true
  },
  "AllowedHosts": "*",
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "C:\\logs\\SunyaSuite\\api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14
        }
      }
    ]
  }
}
```

> Adjust `Port` in connection strings if PostgreSQL is on default 5432.

---

## Step 4: Create API `web.config`

Create `"$env:USERPROFILE\Desktop\SunyaSuiteApi\web.config"`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*"
             modules="AspNetCoreModuleV2"
             resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\SunyaSuite.Web.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpProtocol>
        <customHeaders>
          <remove name="X-Powered-By" />
        </customHeaders>
      </httpProtocol>
    </system.webServer>
  </location>
</configuration>
```

---

## Step 5: Publish the Blazor WASM Client to Desktop

```powershell
dotnet publish src/SunyaSuite.Web.Client/SunyaSuite.Web.Client.csproj `
  --configuration Release `
  --output "$env:USERPROFILE\Desktop\SunyaSuiteClient"
```

---

## Step 6: Configure Client `appsettings.json`

Edit `"$env:USERPROFILE\Desktop\SunyaSuiteClient\wwwroot\appsettings.json"`:

```json
{
  "ApiUrl": "http://localhost:5000"
}
```

---

## Step 7: Create Client `web.config`

Create `"$env:USERPROFILE\Desktop\SunyaSuiteClient\web.config"`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <staticContent>
        <remove fileExtension=".br" />
        <mimeMap fileExtension=".br" mimeType="application/brotli" />
      </staticContent>
      <rewrite>
        <rules>
          <rule name="SPA fallback" stopProcessing="true">
            <match url=".*" />
            <conditions logicalGrouping="MatchAll">
              <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
              <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
            </conditions>
            <action type="Rewrite" url="/" />
          </rule>
        </rules>
      </rewrite>
      <httpProtocol>
        <customHeaders>
          <remove name="X-Powered-By" />
        </customHeaders>
      </httpProtocol>
    </system.webServer>
  </location>
</configuration>
```

---

## Step 8: Copy Build Files to IIS Folders

After Steps 2-7, your Desktop has everything ready. Copy to `C:\inetpub\`:

```powershell
# Create target folders
New-Item -ItemType Directory -Path "C:\inetpub\SunyaSuiteApi" -Force
New-Item -ItemType Directory -Path "C:\inetpub\SunyaSuiteClient" -Force

# Copy API
Copy-Item -Path "$env:USERPROFILE\Desktop\SunyaSuiteApi\*" `
  -Destination "C:\inetpub\SunyaSuiteApi" -Recurse -Force

# Copy Client
Copy-Item -Path "$env:USERPROFILE\Desktop\SunyaSuiteClient\*" `
  -Destination "C:\inetpub\SunyaSuiteClient" -Recurse -Force
```

---

## Step 9: Create IIS Application Pools

```powershell
New-WebAppPool -Name "SunyaSuiteApi"
Set-ItemProperty -Path "IIS:\AppPools\SunyaSuiteApi" -Name managedRuntimeVersion -Value ""

New-WebAppPool -Name "SunyaSuiteClient"
Set-ItemProperty -Path "IIS:\AppPools\SunyaSuiteClient" -Name managedRuntimeVersion -Value ""
```

> ASP.NET Core apps must use **"No Managed Code"** (`managedRuntimeVersion ""`).

---

## Step 10: Create IIS Sites

```powershell
New-WebSite -Name "SunyaSuiteApi" `
  -PhysicalPath "C:\inetpub\SunyaSuiteApi" `
  -ApplicationPool "SunyaSuiteApi" `
  -Port 5000

New-WebSite -Name "SunyaSuiteClient" `
  -PhysicalPath "C:\inetpub\SunyaSuiteClient" `
  -ApplicationPool "SunyaSuiteClient" `
  -Port 8080
```

### Alternative: Single site

```powershell
New-WebSite -Name "SunyaSuite" `
  -PhysicalPath "C:\inetpub\SunyaSuiteClient" `
  -Port 8080

New-WebApplication -Name "api" `
  -Site "SunyaSuite" `
  -PhysicalPath "C:\inetpub\SunyaSuiteApi" `
  -ApplicationPool "SunyaSuiteApi"
```

If using this approach:

- Client `appsettings.json` → `"ApiUrl": "http://localhost:8080/api"`
- Client `web.config` → add condition to SPA fallback rule:
  ```xml
  <add input="{REQUEST_URI}" pattern="^/api/" negate="true" />
  ```

---

## Step 11: Firewall (optional — for LAN access)

```powershell
New-NetFirewallRule -DisplayName "SunyaSuite API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
New-NetFirewallRule -DisplayName "SunyaSuite Client" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow
```

---

## Step 12: Start & Verify

```powershell
Start-WebSite -Name "SunyaSuiteApi"
Start-WebSite -Name "SunyaSuiteClient"
```

Open `http://localhost:8080` in a browser. Login with `admin@sunya.local` / `Admin@123`.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|------|
| **502.5 / 500.30** | App can't start | Check Event Viewer. Run `dotnet SunyaSuite.Web.dll` directly from the publish folder. |
| **500.19** | Invalid `web.config` | Validate XML syntax. |
| **Can't connect to DB** | PostgreSQL not running or wrong port | `docker ps` or `psql -h localhost -p 5433 -U postgres` |
| **Client blank/console errors** | CORS or wrong `ApiUrl` | Check DevTools (F12) → Console for blocked requests. |
| **404 on direct URL** | Missing SPA rewrite rule | Verify URL Rewrite Module is installed and the rule exists in `web.config`. |
| **403 — Access denied** | App pool identity can't read files | Grant `IIS AppPool\SunyaSuiteApi` read permission on the publish folder. |

---

## Files Created (on Desktop before copy)

| File | Location |
|------|----------|
| Published API | `Desktop\SunyaSuiteApi\` |
| `appsettings.Production.json` | `Desktop\SunyaSuiteApi\` |
| `web.config` (API) | `Desktop\SunyaSuiteApi\` |
| Published Client | `Desktop\SunyaSuiteClient\` |
| `web.config` (Client) | `Desktop\SunyaSuiteClient\` |
| `appsettings.json` (Client) | `Desktop\SunyaSuiteClient\wwwroot\` |
