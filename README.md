Run with Docker only
docker compose up --build

API URL
http://localhost:8080/swagger

Local PostgreSQL connection string
Server=localhost;Port=5432;Database=FashionStoreDb;User Id=postgres;Password=1234;Include Error Detail=true
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
