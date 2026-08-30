# EF Core migrations

This folder contains the versioned EF Core schema history for `BookingDbContext`.
Apply it with `dotnet ef database update` (or let the application's SQL Server startup path run `Database.Migrate()`).

Create future migrations from the project root with:

```powershell
dotnet ef migrations add <DescriptiveName>
```

Review generated migrations before committing them, especially changes that drop or alter populated tables.
