# NextHorizon

ASP.NET Core MVC app with pinned SDK/tool/library versions for reproducible setup across collaborators.

## Pinned Versions

- .NET SDK: `10.0.103` (see `global.json`, patch roll-forward only)
- Target framework: `net10.0`
- LibMan CLI: `3.0.71` (see `dotnet-tools.json`)
- Frontend libraries (see `NextHorizon/libman.json`):
  - `jquery@3.7.1`
  - `bootstrap@5.3.3`
  - `jquery-validation@1.21.0`
  - `jquery-validation-unobtrusive@4.0.0`
- NuGet lock file: `NextHorizon/packages.lock.json`

## Prerequisites

1. Install .NET SDK `10.0.103` (or latest `10.0.10x` patch).
2. Confirm SDK:

   ```powershell
   dotnet --list-sdks
   ```

## Step-by-Step Setup

Run from repository root (`C:\dev\NextHorizon`):

1. Restore local tools (LibMan is pinned via manifest):

   ```powershell
   dotnet tool restore
   ```

2. Restore frontend static libraries from `libman.json`:

   ```powershell
   Push-Location .\NextHorizon
   dotnet tool run libman restore
   Pop-Location
   ```

3. Restore NuGet packages using lock file:

   ```powershell
   dotnet restore .\NextHorizon\NextHorizon.csproj --locked-mode
   ```

4. Build:

   ```powershell
   dotnet build .\NextHorizon.slnx -c Debug --no-restore
   ```

5. Run:

   ```powershell
   dotnet run --project .\NextHorizon\NextHorizon.csproj
   ```

6. Open:
   - `http://localhost:5000`
   - `https://localhost:5001`

## Collaborator Consistency Rules

1. Keep `global.json`, `dotnet-tools.json`, `NextHorizon/libman.json`, and `NextHorizon/packages.lock.json` committed.
2. Use `--locked-mode` for normal restores to avoid accidental version drift.
3. If intentionally updating dependencies, regenerate lock artifacts and commit them in the same PR.
