# 1. Configuration - USE 7103 (HTTPS) OR 5210 (HTTP)
$BaseUrl = "https://127.0.0.1:7103" 
$Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzYjkwYmE5Yi01NDIzLTQyNTItOGQ1Yy1iZWVjN2RiMjE3NjIiLCJlbWFpbCI6ImFkbWluQGxvY2FsIiwicm9sZSI6IkFkbWluIiwibmJmIjoxNzY4MTU2MjQ5LCJleHAiOjE3NjgxODUwNDksImlhdCI6MTc2ODE1NjI0OSwiaXNzIjoiQUREUGVyZm9ybWFuY2UiLCJhdWQiOiJBRERQZXJmb3JtYW5jZUF1ZGllbmNlIn0.s4JC5EEJp7WEBZmnw__19VeUfD796n7OyoMbFV2plSc"


# 2. Force Security Protocols for Windows PowerShell 5.1
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# 3. Create Headers
$Headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type"  = "application/json"
}

function Test-Controller($controllerName, $testPayload) {
    # DEFINE THE ENDPOINT INSIDE THE FUNCTION
    $endpoint = "$BaseUrl/api/$controllerName"
    Write-Host "`n--- Testing $controllerName (/api/$controllerName) ---" -ForegroundColor Cyan

    try {
        # --- TEST 1: GET ALL ---
        Write-Host "GET $endpoint... " -NoNewline
        # We use -DisableKeepAlive to prevent "Connection Closed Unexpectedly"
        $getRes = Invoke-RestMethod -Uri $endpoint -Headers $Headers -Method Get -DisableKeepAlive
        Write-Host "OK (Found $($getRes.Count) records)" -ForegroundColor Green

        # --- TEST 2: CREATE (POST) ---
        Write-Host "POST $endpoint (Create)... " -NoNewline
        $jsonBody = $testPayload | ConvertTo-Json
        $postRes = Invoke-RestMethod -Uri $endpoint -Headers $Headers -Method Post -Body $jsonBody -DisableKeepAlive
        $newId = $postRes.id
        Write-Host "CREATED (ID: $newId)" -ForegroundColor Green

        # --- TEST 3: DELETE ---
        Write-Host "DELETE $endpoint/$newId... " -NoNewline
        Invoke-RestMethod -Uri "$endpoint/$newId" -Headers $Headers -Method Delete -DisableKeepAlive
        Write-Host "OK" -ForegroundColor Green

    } catch {
        Write-Host "FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

# --- TEST DATA ---
$revUsdData = @{ date = "2026-01-01"; cy_usd = 100; ly_usd = 80; target_usd = 90; month = "JAN"; year = 2026 }
$onlineSalesData = @{ date = "2026-01-01"; cyPercent = 70; lyPercent = 60; targetPercent = 65; month = "JAN"; year = 2026 }

# --- EXECUTE ---
Test-Controller "RevUsd" $revUsdData
Test-Controller "OnlineSales" $onlineSalesData

Write-Host "`nTest Suite Completed."