# Student workflow and operations reference

These are reference examples, not an automatically installed solution. Your application, Dockerfiles, GitHub repository, and workflow are your work. Keep supplied credential files out of source control. Use fictional data.

## Receive and connect your workspace

1. Sign in to the supplied Azure tenant with your school identity and select WebProgramming1.
2. Create your GitHub repository; give the instructor access and its owner/name plus deployment branch.
3. Instructor runs `bind` to establish repository-specific OIDC trust. This does not write your workflow or repository settings.
4. In GitHub repository Settings → Secrets and variables → Actions, create variables from your `sXX-github-variables.json`. These IDs and endpoints are not secrets. Add `SQL_CONNECTION_STRING` from your own `sXX-app-connection.txt` as an Actions secret.
5. Adapt `deploy-reference.yml` into `.github/workflows/deploy.yml`, `deploy-app.py` into `scripts/deploy-app.py`, and `compose.yml` to your actual project layout. Each service needs a Dockerfile building your .NET project, listening on port 8080. Pin supported dependencies and review security advisories.
6. Push your repository default branch and inspect every workflow step. Add tests that prove your frontend, API, authentication, SQL, and uploads work; an Azure ready revision does not prove application correctness.

Default branch-based federation requires the workflow job to run on the bound branch. Adding a GitHub Environment changes the token subject and requires instructor review/rebinding. Do not print OIDC tokens or credentials into workflow logs.

## Application configuration

The packaged workflow deploys only from the current default branch. Azure binding must match that branch.

The example deploys independent frontend and API apps, with public HTTPS for browser clients. Configure API authentication and CORS to match your frontend's actual URL. Do not use wildcard credentialed CORS. The API gets the SQL connection secret; the frontend does not. Adjust this if your architecture is a server-rendered monolith, but keep credentials server-side.

Both apps use the assigned runtime identity for registry pulls and their private uploads container. It has no deployment permissions. Read BlobServiceUri, BlobContainer, and AZURE_CLIENT_ID from application configuration. Locally, `DefaultAzureCredential` can use your Visual Studio/Azure CLI sign-in; choose the supplied lab tenant and omit managed identity client ID locally if unnecessary.

`UploadStore.cs` demonstrates private Blob upload/download. Add current Azure.Identity and Azure.Storage.Blobs packages. Enforce authentication, owner checks, actual request size, permitted file types, and safe download headers in the endpoint. Store blob keys and authenticated owner IDs in SQL. Files do not live permanently in the application container filesystem. Configure persistent ASP.NET Core Data Protection keys if you require login cookies to survive restarts and deployments; use a separate private Blob path and the runtime identity, rather than ephemeral files.

The student SQL user owns only your database. Use it for local tools and migrations with TLS verification. The app credential is reader/writer only. Run migrations using the student credential through a protected development/migration process; do not put that credential in the public repository or log it. When using SSMS, explicitly select your assigned database instead of master.

## Supported limits

Two fixed app names per student (frontend/API); one container per app; 0.25 CPU with 0.5Gi memory by default, maximum 0.5 CPU / 1Gi; min replicas 0 and max 1; Single revision mode. Use the Consumption workload profile. Extra VMs, AKS clusters, additional SQL servers, premium storage, extra environments and arbitrarily named apps are outside this lab. Policies can take time to become effective. If an allowed operation is denied, send the policy error to your instructor.

## Operations in Cloud Shell Bash

Set values from your access file first. Do not substitute another student's resources.

```bash
az account set --subscription 53e48d05-17eb-429a-8c7e-62fc0bff29f2
RG='YOUR_RESOURCE_GROUP'
APP='YOUR_FRONTEND_OR_API_APP'
az containerapp show -g "$RG" -n "$APP" --query '{State:properties.provisioningState,URL:properties.configuration.ingress.fqdn}' -o table
az containerapp logs show -g "$RG" -n "$APP" --type console --tail 50
az containerapp revision list -g "$RG" -n "$APP" -o table
```

For restart, copy a revision name from the list:

```bash
REVISION='EXACT_REVISION_NAME'
az containerapp revision restart -g "$RG" -n "$APP" --revision "$REVISION"
```

For rollback, deploy a known-good immutable image tag (a previous commit SHA):

```bash
IMAGE='YOUR_REGISTRY.azurecr.io/wp1/sXX/api:PREVIOUS_COMMIT_SHA'
az containerapp update -g "$RG" -n "$APP" --image "$IMAGE" -o none
```

Keep previous image tags needed for rollback. Historical console/system logs are in your Log Analytics workspace; live streaming is available in Container Apps. Logging has 30-day retention and a 0.1-GB daily ingestion cap; the cap can interrupt retained logs and is not a precise billing limit. Avoid logging credentials, personal data, or complete request bodies.

Export SQL data/schema and download uploads before December 21, 2026, Central time. Your source repository survives; Azure resources do not.
