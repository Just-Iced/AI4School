# Deploy to Azure App Service

This project uses interactive-server Blazor, so it needs an ASP.NET Core process. The Static Web Apps workflow only uploaded `wwwroot` assets; it did not publish or run the application.

## Create the App Service

In the Azure portal, create a Web App with:

- Publish: **Code**
- Operating system: **Linux**
- Runtime stack: **.NET 11**
- A plan that supports WebSockets; enable WebSockets in the App Service configuration for Blazor Server.

Keep the app at one instance while using SQLite. The workflow below does not create Azure resources.

## Configure application settings

In the Web App, open **Settings → Environment variables** and add these app settings:

| Name | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | `Data Source=/home/school-db.sqlite;Cache=Shared;` |
| `GitHubToken` | A newly issued GitHub token |

The database file is placed under the Linux App Service persistent home directory. SQLite is appropriate here only for a small, single-instance demo; use a managed database before scaling out or storing production school data.

Do not reuse the token previously placed in local configuration. Revoke it, then store its replacement in App Service settings or use a Key Vault reference.

## Connect GitHub Actions

In the GitHub repository, open **Settings → Secrets and variables → Actions**:

1. Add the App Service name as a repository variable named `AZURE_WEBAPP_NAME`.
2. Download the publish profile from the Web App's **Overview** page and add its contents as a repository secret named `AZURE_WEBAPP_PUBLISH_PROFILE`.

The workflow at `.github/workflows/azure-app-service.yml` publishes the project and deploys it to the named Web App on pushes to `main`. You can also start it from the repository's **Actions** tab using **Run workflow**.
