param(
    [string]$BaseUrl =  "https://localhost:7103",
    [string]$AdminEmail = "admin@local",
    [string]$AdminPassword = "P@ssword1!",
    [string]$CsvFile = ""
)

# Detect PowerShell version
$IsPwsh = $PSVersionTable.PSVersion.Major -ge 7
if (-not $IsPwsh) {
    Write-Host "Warning: Running under Windows PowerShell (<7). Falling back to HttpClient for requests and relaxing certificate checks." -ForegroundColor Yellow

    # Relax certificate validation for dev (only do this in dev)
    try {
        Add-Type -AssemblyName System.Net.Http
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.ServerCertificateCustomValidationCallback = { param($sender,$cert,$chain,$sslPolicyErrors) return $true }
        $global:TestHttpClient = New-Object System.Net.Http.HttpClient($handler)
    } catch {
        Write-Host "Failed to create HttpClient fallback: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
} else {
    Write-Host "Running under PowerShell 7+ (pwsh)." -ForegroundColor Green
}

# Create a small CSV if none provided
if ([string]::IsNullOrEmpty($CsvFile)) {
    $tmp = [System.IO.Path]::GetTempPath()
    $CsvFile = Join-Path $tmp "test_upload.csv"
    $csvContent = "Date,CY,LY,Target`nJAN-2024,100,90,95"
    Set-Content -Path $CsvFile -Value $csvContent -Encoding UTF8
    Write-Host "Created temporary CSV: $CsvFile"
}

function Invoke-Api($method, $path, $body=$null, $headers=@{}) {
    $url = "$BaseUrl$path"
    try {
        if ($IsPwsh) {
            # Use Invoke-RestMethod with -SkipCertificateCheck supported in pwsh
            if ($method -in @("GET","DELETE")) {
                $resp = Invoke-RestMethod -Uri $url -Method $method -Headers $headers -SkipCertificateCheck -ErrorAction Stop
                return @{ ok=$true; status=200; body=$resp }
            } elseif ($method -eq "POST" -and $body -is [System.IO.FileInfo]) {
                $form = @{}
                $form['file'] = $body
                $resp = Invoke-RestMethod -Uri $url -Method Post -Form $form -Headers $headers -SkipCertificateCheck -ErrorAction Stop
                return @{ ok=$true; status=200; body=$resp }
            } else {
                $json = $body | ConvertTo-Json -Depth 10
                $resp = Invoke-RestMethod -Uri $url -Method $method -Body $json -ContentType 'application/json' -Headers $headers -SkipCertificateCheck -ErrorAction Stop
                return @{ ok=$true; status=200; body=$resp }
            }
        } else {
            # Fallback for Windows PowerShell: use HttpClient created earlier
            $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($method), $url)

            foreach ($k in $headers.Keys) {
                $null = $request.Headers.TryAddWithoutValidation($k, $headers[$k])
            }

            if ($method -in @("GET","DELETE")) {
                $respMsg = $global:TestHttpClient.SendAsync($request).GetAwaiter().GetResult()
                $content = $respMsg.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($respMsg.IsSuccessStatusCode) {
                    $bodyObj = $null
                    if ($content -and $content.Trim() -ne "") {
                        try { $bodyObj = $content | ConvertFrom-Json } catch { $bodyObj = $content }
                    }
                    return @{ ok=$true; status=[int]$respMsg.StatusCode; body=$bodyObj }
                } else {
                    return @{ ok=$false; status=[int]$respMsg.StatusCode; error=$content }
                }
            } elseif ($method -eq "POST" -and $body -is [System.IO.FileInfo]) {
                $multipart = New-Object System.Net.Http.MultipartFormDataContent
                $fileStream = $body.OpenRead()
                $streamContent = New-Object System.Net.Http.StreamContent($fileStream)
                $streamContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")
                $multipart.Add($streamContent, "file", $body.Name)
                $request.Content = $multipart

                $respMsg = $global:TestHttpClient.SendAsync($request).GetAwaiter().GetResult()
                $content = $respMsg.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($respMsg.IsSuccessStatusCode) {
                    $bodyObj = $null
                    if ($content -and $content.Trim() -ne "") {
                        try { $bodyObj = $content | ConvertFrom-Json } catch { $bodyObj = $content }
                    }
                    return @{ ok=$true; status=[int]$respMsg.StatusCode; body=$bodyObj }
                } else {
                    return @{ ok=$false; status=[int]$respMsg.StatusCode; error=$content }
                }
            } else {
                $json = $body | ConvertTo-Json -Depth 10
                $request.Content = New-Object System.Net.Http.StringContent($json, [System.Text.Encoding]::UTF8, "application/json")
                $respMsg = $global:TestHttpClient.SendAsync($request).GetAwaiter().GetResult()
                $content = $respMsg.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($respMsg.IsSuccessStatusCode) {
                    $bodyObj = $null
                    if ($content -and $content.Trim() -ne "") {
                        try { $bodyObj = $content | ConvertFrom-Json } catch { $bodyObj = $content }
                    }
                    return @{ ok=$true; status=[int]$respMsg.StatusCode; body=$bodyObj }
                } else {
                    return @{ ok=$false; status=[int]$respMsg.StatusCode; error=$content }
                }
            }
        }
    } catch {
        $status = 0
        if ($_.Exception.Response -ne $null) {
            try { $status = $_.Exception.Response.StatusCode.Value__ } catch {}
        }
        return @{ ok=$false; status=$status; error=$_.Exception.Message }
    }
}

Write-Host "`n== Login to obtain JWT ==" -ForegroundColor Cyan
# $body = @{ Email = 'admin@local'; Password = 'P@ssword1!' } | ConvertTo-Json
# $loginResult = Invoke-RestMethod -Uri 'https://localhost:7103/api/auth/login' -Method Post -Body $body -ContentType 'application/json'
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzYjkwYmE5Yi01NDIzLTQyNTItOGQ1Yy1iZWVjN2RiMjE3NjIiLCJlbWFpbCI6ImFkbWluQGxvY2FsIiwicm9sZSI6IkFkbWluIiwibmJmIjoxNzY4MTU2MjQ5LCJleHAiOjE3NjgxODUwNDksImlhdCI6MTc2ODE1NjI0OSwiaXNzIjoiQUREUGVyZm9ybWFuY2UiLCJhdWQiOiJBRERQZXJmb3JtYW5jZUF1ZGllbmNlIn0.s4JC5EEJp7WEBZmnw__19VeUfD796n7OyoMbFV2plSc"

$authHeader = @{ Authorization = "Bearer $token" }
Write-Host "Using manual JWT token from Swagger." -ForegroundColor Green
if (-not $token) {
    Write-Host "Login returned no token." -ForegroundColor Red
    exit 1
}
$authHeader = @{ Authorization = "Bearer $token" }
Write-Host "Obtained JWT token." -ForegroundColor Green

# Controllers to test
$controllers = @(
    @{ name='AddCk'; path='/api/AddCk'; upload=$true; createJson=$null; put=$false; delete=$true },
    @{ name='RevUsd'; path='/api/RevUsd'; upload=$true; createJson=$null; put=$false; delete=$false },
    @{ name='CorporateSales'; path='/api/CorporateSales'; upload=$true; createJson=$null; put=$true; delete=$true },
    @{ name='OnlineSales'; path='/api/OnlineSales'; upload=$true; createJson=$null; put=$false; delete=$false },
    @{ name='ByTourCodes'; path='/api/ByTourCodes'; upload=$false; createJson=@{ Date = (Get-Date -Year 2024 -Month 1 -Day 1).ToString("o"); TourCode = "TST"; CORP_TYPE = "CORP"; CORPORATE_NAME = "Test Corp"; MonthylyAmount = 100.0; Target = 90.0 }; put=$true; delete=$true },
    @{ name='Destinations'; path='/api/Destinations'; upload=$false; createJson=@{ Destination="DST"; Origin="ORG"; Month = (Get-Date -Year 2024 -Month 1 -Day 1).ToString("o"); paxCount = 10; MonthNum = 1; Year = 2024 }; put=$false; delete=$false },
    @{ name='DateMasters'; path='/api/DateMasters'; upload=$false; createJson=@{ Year = 2024; Month = 1 }; put=$false; delete=$false }
)

$report = @()

foreach ($c in $controllers) {
    Write-Host "`n--- Testing $($c.name) ($($c.path)) ---" -ForegroundColor Cyan

    # GET all
    $getAll = Invoke-Api -method GET -path $c.path -body $null -headers $authHeader
    Write-Host "GET $($c.path) => $($getAll.status) (ok=$($getAll.ok))"
    $firstId = $null
    if ($getAll.ok -and $getAll.body) {
        $arr = @($getAll.body)
        if ($arr.Count -gt 0) {
            $first = $arr[0]
            if ($first.PSObject.Properties['Id']) { $firstId = $first.Id }
            elseif ($first.PSObject.Properties['id']) { $firstId = $first.id }
        }
    }

    $entry = @{ controller=$c.name; getAll=$getAll.status }

    # Upload test
    if ($c.upload) {
        Write-Host "POST $($c.path)/upload (file)" -ForegroundColor Yellow
        $fileInfo = Get-Item $CsvFile
        $upload = Invoke-Api -method POST -path "$($c.path)/upload" -body $fileInfo -headers $authHeader
        Write-Host "=> $($upload.status) ok=$($upload.ok)"
        $entry.upload = $upload.status
    }

    # Create JSON
    if ($c.createJson) {
        Write-Host "POST $($c.path) (create JSON)" -ForegroundColor Yellow
        $post = Invoke-Api -method POST -path $c.path -body $c.createJson -headers $authHeader
        Write-Host "=> $($post.status) ok=$($post.ok)"
        $entry.create = $post.status
        if ($post.ok -and $post.body) {
            if ($post.body.PSObject.Properties['Id']) { $firstId = $post.body.Id }
            elseif ($post.body.PSObject.Properties['id']) { $firstId = $post.body.id }
        }
    }

    # PUT (update)
    if ($c.put -and $firstId) {
        Write-Host "PUT $($c.path)/$firstId" -ForegroundColor Yellow
        if ($c.name -eq 'CorporateSales') {
            $putBody = @{ CY = 123.45; LY = 120.0; Target = 100.0 }
        } else {
            $putBody = @{ }
        }
        $put = Invoke-Api -method PUT -path "$($c.path)/$firstId" -body $putBody -headers $authHeader
        Write-Host "=> $($put.status) ok=$($put.ok)"
        $entry.put = $put.status
    }

    # DELETE (soft)
    if ($c.delete -and $firstId) {
        Write-Host "DELETE $($c.path)/$firstId" -ForegroundColor Yellow
        $del = Invoke-Api -method DELETE -path "$($c.path)/$firstId" -headers $authHeader
        Write-Host "=> $($del.status) ok=$($del.ok)"
        $entry.delete = $del.status
    }

    $report += $entry
}

Write-Host "`n=== Summary ===`n" -ForegroundColor Cyan
$report | Format-Table -AutoSize

Write-Host "`nNotes:` - If any request returned 401/403, ensure admin user exists and credentials are correct."
Write-Host "If a controller is not present, GET will return 404. Some MVC controllers (non-API) may not expose /api endpoints."
Write-Host "`nTest script completed."