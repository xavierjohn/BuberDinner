# Requests

VS Code / Visual Studio REST Client `.http` files for the BuberDinner API,
plus a PowerShell driver that replays all of them as an end-to-end smoke test.

## Layout

Each subfolder mirrors a controller area:

| Folder | Contents |
|---|---|
| `Authentication/` | Register, Login, Login-InvalidCredentials |
| `Hosts/` | CreateHost |
| `Menus/` | Create / Get / Update / List + ETag-flow failure modes |
| `Dinners/` | Schedule / Get / List (with cursor pagination edge cases) / Start / End / Cancel + lifecycle failure modes |
| `Reservations/` | Create (idempotent) / Cancel / ListMine + failure modes |
| `MenuReviews/` | Submit / Update / List + FluentValidation failure modes |

`httpenv.json` defines the `{{host}}` variable for the dev profile
(`https://localhost:7059`, matching `Api/src/Properties/launchSettings.json`).

## Running individual files (REST Client)

Authenticate first:

1. Open `Authentication/Register.http`, run the request, copy the `token` from the response.
2. In every other `.http` file, replace `@token = yourtoken` with the value you copied.
3. Each file documents which other `@variable` values (hostId, menuId, dinnerId, etc.)
   you need to substitute in, and which prior file produces them.

## Running everything (`smoke.ps1`)

`smoke.ps1` replays every `.http` file against a running API instance, threading
IDs / ETags / Idempotency-Keys between calls. Use it to verify the suite end-to-end
after any controller or contract change.

```pwsh
# Terminal 1: start the API.
dotnet run --project Api/src --launch-profile InMemory

# Terminal 2: run the smoke driver.
pwsh Requests/smoke.ps1
```

Options:

```pwsh
# Run against a non-default host.
pwsh Requests/smoke.ps1 -ApiHost https://localhost:5001
```

The script:

- Generates unique `userId`s per run (suffixed with epoch seconds) so reruns
  against the same in-memory instance don't trip on 409 / fingerprint mismatches.
- Asserts the HTTP status code (and a few derived fields like `wasCapped`); does
  not validate full response bodies — it's a smoke driver, not a contract test.
- Exits with `0` when every call passes; otherwise exits with the count of failures.
