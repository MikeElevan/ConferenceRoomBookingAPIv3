# Migrations

This folder is intentionally empty. Generate the initial migration once, locally, with the
.NET SDK and the EF Core tools installed:

```bash
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add InitialCreate --project ConferenceRoomBookingAPIv3.csproj
```

This uses `BookingDbContextFactory` (in `Infrastructure/Persistence/`) to build the `DbContext`
at design time, so it works without a running host or a configured JWT authority/audience.

Commit the generated `Migrations/*.cs` and `*ModelSnapshot.cs` files — they are part of the
application's source of truth for the schema, not build output. From then on:

- `dotnet ef migrations add <Name>` whenever the EF Core model changes.
- `dotnet ef database update` to apply pending migrations to a target database by hand, or let
  `DatabaseInitializer.Initialize` call `dbContext.Database.Migrate()` on startup (already wired
  up in `Program.cs` for the SqlServer provider).

Do not hand-edit the generated files, and never call `EnsureCreated()` on this context — it
builds a one-shot schema from the current model with no upgrade path, and mixing it with
migrations on the same database will corrupt the `__EFMigrationsHistory` tracking table.
