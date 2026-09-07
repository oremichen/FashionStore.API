Run with Docker only
docker compose up --build

API URL
http://localhost:8080/swagger

Local PostgreSQL connection string
Server=localhost;Port=5432;Database=FashionStoreDb;User Id=postgres;Password=1234;Include Error Detail=true
## Bootstrap administrator

Database seeding requires `SeedData__SuperAdmin__Email` in the environment.
If that account does not exist, also supply `SeedData__SuperAdmin__Password`
through your deployment secret manager or local .NET user secrets. The password
must satisfy the configured ASP.NET Identity password policy. Missing or blank
required values stop seeding; there are no built-in administrator credentials.
Once the account exists, the bootstrap password can be removed from configuration;
keep the email configured. Existing accounts are not modified by seeding.
Vercel startup skips database seeding.

# Redis response cache

The API caches successful JSON `GET` responses for 24 hours. Configure the Aiven
TLS connection string outside source control:

```powershell
$env:ConnectionStrings__Redis = 'rediss://default:<password>@<host>:<port>'
dotnet run --project FashionStore.API
```

For containers, provide the equivalent `ConnectionStrings__Redis` environment
variable through the deployment platform's secret manager. Successful `POST`,
`PUT`, `PATCH`, and `DELETE` responses invalidate the related cache group.
