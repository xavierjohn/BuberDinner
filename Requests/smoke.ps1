<#
.SYNOPSIS
    End-to-end smoke test for every .http file in this folder.

.DESCRIPTION
    Replays the request from each .http file against a running BuberDinner.Api
    instance, threading IDs / ETags / Idempotency-Keys between calls so dependent
    requests (e.g. UpdateMenu after GetMenu, SubmitReview after a reserved-and-
    ended dinner) see realistic state.

    Each test reports the file name, expected HTTP status, and the actual status.
    Process exit code = number of failures.

    This is a smoke driver, not an exhaustive contract test:
      - It asserts only the HTTP status code (and a handful of derived fields
        like `wasCapped`) — not the full response body.
      - It uses unique userIds per run (suffixed with epoch seconds) so reruns
        against the same in-memory instance don't trip on 409 / fingerprint
        mismatches from prior runs.

.PARAMETER ApiHost
    Base URL of the running API. Defaults to https://localhost:7059 (matches
    the dev launchSettings profile).

.EXAMPLE
    # 1. Start the API in another terminal:
    dotnet run --project Api/src --launch-profile InMemory

    # 2. Run the smoke driver:
    pwsh Requests/smoke.ps1

.EXAMPLE
    # Run against a non-default host:
    pwsh Requests/smoke.ps1 -ApiHost https://localhost:5001
#>
[CmdletBinding()]
param(
    [string]$ApiHost = 'https://localhost:7059'
)

$ErrorActionPreference = 'Stop'
$results = New-Object System.Collections.Generic.List[object]

function Send {
    param(
        [string]$file,
        [string]$method,
        [string]$path,
        [hashtable]$headers,
        $body,
        [int[]]$expect
    )
    if ($null -eq $headers) { $headers = @{} }
    if ($body -is [hashtable]) { $body = ($body | ConvertTo-Json -Depth 10 -Compress) }
    $req = @{
        Uri                  = "$ApiHost$path"
        Method               = $method
        Headers              = $headers
        SkipCertificateCheck = $true
        SkipHttpErrorCheck   = $true
        UseBasicParsing      = $true
        TimeoutSec           = 30
    }
    if ($body) { $req.Body = $body; $req.ContentType = 'application/json' }
    try {
        $r = Invoke-WebRequest @req
        $status = [int]$r.StatusCode
        $pass = $expect -contains $status
        $bodyText = $r.Content
        $etag = if ($r.Headers.ContainsKey('ETag')) { ($r.Headers.ETag | Select-Object -First 1).ToString() } else { $null }
    } catch {
        $status = -1
        $pass = $false
        $bodyText = $_.Exception.Message
        $etag = $null
    }
    $row = [PSCustomObject]@{
        File     = $file
        Method   = $method
        Path     = $path
        Expected = ($expect -join ',')
        Actual   = $status
        Pass     = $pass
        Etag     = $etag
        Body     = $bodyText
    }
    $script:results.Add($row)
    $tick = if ($pass) { 'PASS' } else { 'FAIL' }
    $color = if ($pass) { 'Green' } else { 'Red' }
    Write-Host ("{0,-4} {1,-3} {2,-55} expect={3,-10} actual={4}" -f $tick, $method, $file, $row.Expected, $status) -ForegroundColor $color
    if (-not $pass -and $bodyText) {
        $snippet = ($bodyText -replace '\s+', ' ').Substring(0, [Math]::Min(220, $bodyText.Length))
        Write-Host "     -> $snippet" -ForegroundColor Yellow
    }
    return $row
}

function FromJson($text) { try { $text | ConvertFrom-Json } catch { $null } }

Write-Host "Smoke testing $ApiHost" -ForegroundColor Cyan

# ---------- Phase 1: Auth + Hosts ----------
# Unique userIds per run so reruns don't 409.
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
$user1 = "smoke1_$stamp"
$user2 = "smoke2_$stamp"

$r = Send 'Authentication/Register.http (user1)' 'POST' '/authentication/register' @{} @{
    userId = $user1; firstName = 'Smoke'; lastName = 'One'; email = 's1@example.com'; password = 'Smoke1234!'
} @(200, 201)
$token1 = (FromJson $r.Body).token

$r = Send 'Authentication/Register.http (user2)' 'POST' '/authentication/register' @{} @{
    userId = $user2; firstName = 'Smoke'; lastName = 'Two'; email = 's2@example.com'; password = 'Smoke1234!'
} @(200, 201)
$token2 = (FromJson $r.Body).token

if (-not $token1 -or -not $token2) {
    Write-Host 'No tokens captured - aborting.' -ForegroundColor Red
    exit 1
}

Send 'Authentication/Login.http' 'POST' '/authentication/login' @{} @{ userId = $user1; password = 'Smoke1234!' } @(200) | Out-Null
Send 'Authentication/Login-InvalidCredentials.http' 'POST' '/authentication/login' @{} @{ userId = $user1; password = 'WrongPassword!' } @(401) | Out-Null

$auth1 = @{ Authorization = "Bearer $token1" }
$auth2 = @{ Authorization = "Bearer $token2" }

$r = Send 'Hosts/CreateHost.http (host1)' 'POST' '/hosts?api-version=2022-10-01' $auth1 @{ displayName = "Smoke's Kitchen" } @(201)
$hostId1 = (FromJson $r.Body).id
$r = Send 'Hosts/CreateHost.http (host2 for cross-owner tests)' 'POST' '/hosts?api-version=2022-10-01' $auth2 @{ displayName = "Other Kitchen" } @(201)
$hostId2 = (FromJson $r.Body).id

# ---------- Phase 2: Menus ----------
$createMenuBody = @{
    name        = 'Muffins R Us'
    description = 'Menu for Muffins R Us'
    sections    = @(
        @{ name = 'Muffins'; description = 'The Muffins section'; items = @(
                @{ name = 'Blueberry'; description = 'A Blueberry Muffin' },
                @{ name = 'Chocolate'; description = 'A Chocolate Muffin' }) },
        @{ name = 'Not Muffins'; description = 'Anything that is not a muffin'; items = @(
                @{ name = 'Cookie'; description = 'Probably oatmeal raisin' }) }
    )
}
$r = Send 'Menus/CreateMenu.http' 'POST' "/hosts/$hostId1/menus/create?api-version=2022-10-01" $auth1 $createMenuBody @(201)
$menuId1 = (FromJson $r.Body).id

$badNested = @{
    name        = 'Valid Menu'; description = 'Valid Description'
    sections    = @(@{ name = ''; description = 'Section description'; items = @(@{ name = 'Item name'; description = '' }) })
}
Send 'Menus/CreateMenu-InvalidNested.http' 'POST' "/hosts/$hostId1/menus/create?api-version=2022-10-01" $auth1 $badNested @(422) | Out-Null

$r = Send 'Menus/GetMenu.http' 'GET' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" $auth1 $null @(200)
$etagMenu1 = $r.Etag

Send 'Menus/GetMenu-NotModified.http' 'GET' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" `
    ($auth1 + @{ 'If-None-Match' = $etagMenu1 }) $null @(304) | Out-Null

$r = Send 'Menus/UpdateMenu.http' 'PUT' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" `
    ($auth1 + @{ 'If-Match' = $etagMenu1 }) @{ name = 'Brunch v2'; description = 'Updated brunch menu' } @(200)
$etagMenu1 = $r.Etag

# UpdateMenu-IfMatchMismatch.http: 3 scenarios in one file
Send 'Menus/UpdateMenu-IfMatchMismatch.http (no If-Match -> 428)' 'PUT' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" $auth1 `
    @{ name = 'X'; description = 'Y' } @(428) | Out-Null
Send 'Menus/UpdateMenu-IfMatchMismatch.http (stale If-Match -> 412)' 'PUT' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" `
    ($auth1 + @{ 'If-Match' = '"deadbeefdeadbeefdeadbeefdeadbeef"' }) @{ name = 'X'; description = 'Y' } @(412) | Out-Null
Send 'Menus/UpdateMenu-IfMatchMismatch.http (cross-owner -> 403)' 'PUT' "/hosts/$hostId1/menus/$menuId1`?api-version=2022-10-01" `
    ($auth2 + @{ 'If-Match' = $etagMenu1 }) @{ name = 'Pwned'; description = 'By a different user' } @(403, 404) | Out-Null

Send 'Menus/ListMenus.http' 'GET' "/hosts/$hostId1/menus?api-version=2022-10-01&limit=5" $auth1 $null @(200) | Out-Null

# ---------- Phase 3: Dinners ----------
$scheduleBody = @{
    name = 'Brunch with friends'; description = 'Casual Sunday brunch'
    menuId = $menuId1; startDateTime = '2026-07-01T18:00:00Z'; endDateTime = '2026-07-01T21:00:00Z'
}
$r = Send 'Dinners/ScheduleDinner.http' 'POST' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $scheduleBody @(201)
$dinnerId1 = (FromJson $r.Body).id

Send 'Dinners/GetDinner.http' 'GET' "/hosts/$hostId1/dinners/$dinnerId1`?api-version=2022-10-01" $auth1 $null @(200) | Out-Null
Send 'Dinners/ListDinners.http' 'GET' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $null @(200) | Out-Null

$r = Send 'Dinners/ListDinners-OversizedLimit.http' 'GET' "/hosts/$hostId1/dinners?api-version=2022-10-01&limit=500" $auth1 $null @(200)
$wasCapped = (FromJson $r.Body).wasCapped
if (-not $wasCapped) {
    Write-Host '     warning: expected wasCapped=true on oversized limit' -ForegroundColor Yellow
}

Send 'Dinners/ListDinners-MalformedCursor.http' 'GET' "/hosts/$hostId1/dinners?api-version=2022-10-01&cursor=NOT-A-VALID-CURSOR-!!!" $auth1 $null @(422) | Out-Null

# Create a few more dinners so we can capture a real `next.cursor` for the With-Cursor test.
foreach ($i in 2..6) {
    $body = @{
        name = "Dinner $i"; description = "d$i"; menuId = $menuId1
        startDateTime = '2026-07-01T18:00:00Z'; endDateTime = '2026-07-01T21:00:00Z'
    }
    Send "Dinners/Schedule extra #$i" 'POST' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $body @(201) | Out-Null
}
$r = Send 'Dinners ListPage1 (capture cursor)' 'GET' "/hosts/$hostId1/dinners?api-version=2022-10-01&limit=3" $auth1 $null @(200)
$cursor = (FromJson $r.Body).next.cursor
if ($cursor) {
    Send 'Dinners/ListDinners-WithCursor.http' 'GET' "/hosts/$hostId1/dinners?api-version=2022-10-01&limit=3&cursor=$cursor" $auth1 $null @(200) | Out-Null
} else {
    Write-Host 'SKIP Dinners/ListDinners-WithCursor.http (no next cursor)' -ForegroundColor DarkGray
}

Send 'Dinners/StartDinner.http' 'POST' "/hosts/$hostId1/dinners/$dinnerId1/start?api-version=2022-10-01" $auth1 $null @(200) | Out-Null
Send 'Dinners/Lifecycle-InvalidTransition.http (cancel after start -> 422)' 'POST' "/hosts/$hostId1/dinners/$dinnerId1/cancel?api-version=2022-10-01" $auth1 @{ reason = 'Too late' } @(422) | Out-Null
Send 'Dinners/Lifecycle-InvalidTransition.http (cross-host start -> 403)' 'POST' "/hosts/$hostId1/dinners/$dinnerId1/start?api-version=2022-10-01" $auth2 $null @(403, 404) | Out-Null
Send 'Dinners/EndDinner.http' 'POST' "/hosts/$hostId1/dinners/$dinnerId1/end?api-version=2022-10-01" $auth1 $null @(200) | Out-Null

# CancelDinner.http requires a fresh Upcoming dinner.
$body = @{
    name = 'Sunday brunch v2'; description = 'Will be cancelled'; menuId = $menuId1
    startDateTime = '2026-07-01T18:00:00Z'; endDateTime = '2026-07-01T21:00:00Z'
}
$r = Send 'Dinners Schedule for cancel' 'POST' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $body @(201)
$dinnerToCancel = (FromJson $r.Body).id
Send 'Dinners/CancelDinner.http' 'POST' "/hosts/$hostId1/dinners/$dinnerToCancel/cancel?api-version=2022-10-01" $auth1 @{ reason = 'Host illness' } @(200) | Out-Null

# ---------- Phase 4: Reservations ----------
$body = @{
    name = 'Brunch for reservations'; description = 'Reservation testing'; menuId = $menuId1
    startDateTime = '2026-07-01T18:00:00Z'; endDateTime = '2026-07-01T21:00:00Z'
}
$r = Send 'Dinners Schedule for reservations' 'POST' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $body @(201)
$dinnerRes = (FromJson $r.Body).id

$key = [Guid]::NewGuid().ToString()
$r = Send 'Reservations/CreateReservation.http' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $key }) @{ dinnerId = $dinnerRes; guestCount = 2 } @(201)
$resId1 = (FromJson $r.Body).id

Send 'Reservations/CreateReservation.http (replay)' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $key }) @{ dinnerId = $dinnerRes; guestCount = 2 } @(201) | Out-Null

Send 'Reservations/CancelReservation.http' 'POST' "/reservations/$resId1/cancel?api-version=2022-10-01" $auth2 @{ reason = 'Schedule conflict' } @(200) | Out-Null

$key2 = [Guid]::NewGuid().ToString()
Send 'Reservations CreateReservation (for list)' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $key2 }) @{ dinnerId = $dinnerRes; guestCount = 1 } @(201) | Out-Null

Send 'Reservations/ListMyReservations.http' 'GET' '/reservations/mine?api-version=2022-10-01&limit=10' $auth2 $null @(200) | Out-Null

Send 'Reservations/CreateReservation-FailureModes.http (no Idempotency-Key -> 400)' 'POST' '/reservations?api-version=2022-10-01' $auth2 `
    @{ dinnerId = $dinnerRes; guestCount = 1 } @(400) | Out-Null
Send 'Reservations/CreateReservation-FailureModes.http (key+different body -> 422)' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $key2 }) @{ dinnerId = $dinnerRes; guestCount = 99 } @(422, 409) | Out-Null
$phantom = '00000000-1111-2222-3333-444444444444'
$keyPhantom = [Guid]::NewGuid().ToString()
Send 'Reservations/CreateReservation-FailureModes.http (phantom dinner -> 404)' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $keyPhantom }) @{ dinnerId = $phantom; guestCount = 1 } @(404) | Out-Null

Send 'Dinners/ListReservationsForDinner.http' 'GET' "/hosts/$hostId1/dinners/$dinnerRes/reservations?api-version=2022-10-01&limit=10" $auth1 $null @(200) | Out-Null

# ---------- Phase 5: MenuReviews ----------
# Reviews require: caller reserved the dinner AND dinner is Ended.
$body = @{
    name = 'Brunch for reviews'; description = 'Review testing'; menuId = $menuId1
    startDateTime = '2026-07-01T18:00:00Z'; endDateTime = '2026-07-01T21:00:00Z'
}
$r = Send 'Dinners Schedule for review' 'POST' "/hosts/$hostId1/dinners?api-version=2022-10-01" $auth1 $body @(201)
$dinnerRev = (FromJson $r.Body).id
$keyRev = [Guid]::NewGuid().ToString()
Send 'Reservations CreateReservation (for review)' 'POST' '/reservations?api-version=2022-10-01' `
    ($auth2 + @{ 'Idempotency-Key' = $keyRev }) @{ dinnerId = $dinnerRev; guestCount = 1 } @(201) | Out-Null
Send 'Dinners Start (for review)' 'POST' "/hosts/$hostId1/dinners/$dinnerRev/start?api-version=2022-10-01" $auth1 $null @(200) | Out-Null
Send 'Dinners End (for review)' 'POST' "/hosts/$hostId1/dinners/$dinnerRev/end?api-version=2022-10-01" $auth1 $null @(200) | Out-Null

$r = Send 'MenuReviews/SubmitReview.http' 'POST' '/menu-reviews?api-version=2022-10-01' $auth2 `
    @{ menuId = $menuId1; dinnerId = $dinnerRev; rating = 4; comment = 'Loved the brunch.' } @(201)
$reviewId1 = (FromJson $r.Body).id

Send 'MenuReviews/UpdateReview.http' 'PUT' "/menu-reviews/$reviewId1`?api-version=2022-10-01" $auth2 `
    @{ rating = 5; comment = 'Even better on second visit' } @(200) | Out-Null

Send 'MenuReviews/ListReviewsForMenu.http' 'GET' "/menu-reviews/for-menu/$menuId1`?api-version=2022-10-01&limit=10" $auth2 $null @(200) | Out-Null

Send 'MenuReviews/SubmitReview-ValidationFailure.http (rating=99 -> 422)' 'POST' '/menu-reviews?api-version=2022-10-01' $auth2 `
    @{ menuId = $menuId1; dinnerId = $dinnerRev; rating = 99; comment = 'anything' } @(422) | Out-Null
Send 'MenuReviews/SubmitReview-ValidationFailure.http (empty comment -> 422)' 'POST' '/menu-reviews?api-version=2022-10-01' $auth2 `
    @{ menuId = $menuId1; dinnerId = $dinnerRev; rating = 3; comment = '' } @(422) | Out-Null
Send 'MenuReviews/SubmitReview-ValidationFailure.http (both wrong -> 422)' 'POST' '/menu-reviews?api-version=2022-10-01' $auth2 `
    @{ menuId = $menuId1; dinnerId = $dinnerRev; rating = 0; comment = '' } @(422) | Out-Null

# ---------- Summary ----------
$pass = ($results | Where-Object Pass).Count
$fail = ($results | Where-Object { -not $_.Pass }).Count
Write-Host ''
Write-Host '================== SUMMARY ==================' -ForegroundColor Cyan
Write-Host ("Total: {0}  Pass: {1}  Fail: {2}" -f $results.Count, $pass, $fail)
if ($fail -gt 0) {
    Write-Host ''
    Write-Host 'FAILURES:' -ForegroundColor Red
    foreach ($f in ($results | Where-Object { -not $_.Pass })) {
        Write-Host ("  [{0}] {1} {2} expect={3} actual={4}" -f $f.File, $f.Method, $f.Path, $f.Expected, $f.Actual)
        if ($f.Body) {
            $snippet = ($f.Body -replace '\s+', ' ').Substring(0, [Math]::Min(220, $f.Body.Length))
            Write-Host "    -> $snippet" -ForegroundColor Yellow
        }
    }
}
exit $fail
