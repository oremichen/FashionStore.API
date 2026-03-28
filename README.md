To run migration
PM> dotnet ef migrations add InitialIdentitySetup --project FashionStore.Infrastructure --startup-project FashionStore.API
PM> dotnet ef database update InitialIdentitySetup --project FashionStore.Infrastructure --startup-project FashionStore.API
