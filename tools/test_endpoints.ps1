param(
    [string]$BaseUrl = "https://localhost:5210"
)

# Run from PowerShell 7+. If using self-signed dev certs, trust them: dotnet dev-certs https --trust
Write-Host "Testing endpoints against $BaseUrl`n"

function Invoke-ApiGet($path) {
    try {
        $url = "$BaseUrl$path"
        $resp = Invoke-RestMethod -Uri $url -Method Get -SkipCertificateCheck -ErrorAction Stop
        return @{ ok = $true; status = 200; body = $resp }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $status = $_.Exception.Response.StatusCode.Value__
            return @{ ok = $false; status = $status; error = $_.Exception.Message }
        }
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

function Invoke-ApiPostFile($path, $filePath, $extraFields = @{}) {
    try {
        $url = "$BaseUrl$path"
        $form = @{}
        foreach ($k in $extraFields.Keys) { $form[$k] = $extraFields[$k] }
        $form['file'] = Get-Item $filePath
        $resp = Invoke-RestMethod -Uri $url -Method Post -Form $form -SkipCertificateCheck -ErrorAction Stop
        return @{ ok = $true; status = 200; body = $resp }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $status = $_.Exception.Response.StatusCode.Value__
            return @{ ok = $false; status = $status; error = $_.Exception.Message }
        }
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

function Invoke-ApiPostJson($path, $jsonObj) {
    try {
        $url = "$BaseUrl$path"
        $resp = Invoke-RestMethod -Uri $url -Method Post -Body ($jsonObj | ConvertTo-Json -Depth 6) -ContentType 'application/json' -SkipCertificateCheck -ErrorAction Stop
        return @{ ok = $true; status = 200; body = $resp }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $status = $_.Exception.Response.StatusCode.Value__
            return @{ ok = $false; status = $status; error = $_.Exception.Message }
        }
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

function Invoke-ApiPutJson($path, $jsonObj) {
    try {
        $url = "$BaseUrl$path"
        $resp = Invoke-RestMethod -Uri $url -Method Put -Body ($jsonObj | ConvertTo-Json -Depth 6) -ContentType 'application/json' -SkipCertificateCheck -ErrorAction Stop
        return @{ ok = $true; status = 204; body = $resp }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $status = $_.Exception.Response.StatusCode.Value__
            return @{ ok = $false; status = $status; error = $_.Exception.Message }
        }
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

function Invoke-ApiDelete($path) {
    try {
        $url = "$BaseUrl$path"
        $resp = Invoke-RestMethod -Uri $url -Method Delete -SkipCertificateCheck -ErrorAction Stop
        return @{ ok = $true; status = 204; body = $resp }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $status = $_.Exception.Response.StatusCode.Value__
            return @{ ok = $false; status = $status; error = $_.Exception.Message }
        }
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

# Create small CSV test file
$testCsv = "Date,CY,LY,Target`nJAN-2024,100,90,95"
$tmpDir = [System.IO.Path]::GetTempPath()
$csvPath = Join-Path $tmpDir "test_upload.csv"
Set-Content -Path $csvPath -Value $testCsv -Encoding UTF8
Write-Host "Created test CSV: $csvPath`n"

$controllers = @(
    @{ name='AddCk'; path='/api/AddCk'; upload='/api/AddCk/upload'; supportsUpload=$true },
    @{ name='RevUsd'; path='/api/RevUsd'; upload='/api/RevUsd/upload'; supportsUpload=$true },
    @{ name='CorporateSales'; path='/api/CorporateSales'; upload='/api/CorporateSales/upload'; supportsUpload=$true; put=true; delete=true },
    @{ name='OnlineSales'; path='/api/OnlineSales'; upload='/api/OnlineSales/upload'; supportsUpload=$true },
    @{ name='ByTourCodes'; path='/api/ByTourCodes'; createJson = @{ Date = "2024-01-01T00:00:00Z"; TourCode = "TST"; CORP_TYPE = "CORP"; CY = 10; LY = 9; Target = 8 } },
    @{ name='Destinations'; path='/api/Destinations'; upload='/api/Destinations/upload'; supportsUpload=$true },
    @{ name='DateMasters'; path='/api/DateMasters' }
)

$report = @()

# Check swagger
$s = Invoke-ApiGet '/swagger/v1/swagger.json'
if ($s.ok) { Write-Host "Swagger JSON OK" } else { Write-Host "Swagger JSON fetch failed: $($s.status) $($s.error)" }

foreach ($c in $controllers) {
    $name = $c.name
    Write-Host "\n--- Controller: $name ---"
    $getAll = Invoke-ApiGet $c.path
    Write-Host "GET $($c.path) => $($getAll.status)"
    $firstId = $null
    if ($getAll.ok -and $getAll.body -is [System.Collections.IEnumerable]) {
        $arr = @($getAll.body)
        if ($arr.Count -gt 0) {
            $first = $arr[0]
            if ($first.PSObject.Properties['Id']) { $firstId = $first.Id }
            elseif ($first.PSObject.Properties['id']) { $firstId = $first.id }
            elseif ($first.PSObject.Properties['ID']) { $firstId = $first.ID }
        }
    }
    if ($firstId) { Write-Host "Found item id: $firstId" }

    if ($c.supportsUpload) {
        Write-Host "POST $($c.upload) (file)"
        $post = Invoke-ApiPostFile $c.upload $csvPath @{}
        Write-Host "=> $($post.status)"
        $report += @{ controller=$name; getAll=$getAll.status; upload=$post.status }
    } elseif ($c.createJson) {
        Write-Host "POST $($c.path) (create JSON)"
        $post = Invoke-ApiPostJson $c.path $c.createJson
        Write-Host "=> $($post.status)"
        $report += @{ controller=$name; getAll=$getAll.status; create=$post.status }
    } else {
        $report += @{ controller=$name; getAll=$getAll.status }
    }

    if ($firstId -and $c.put) {
        Write-Host "PUT $($c.path)/$firstId"
        $putBody = @{ CY = 123.45; LY = 120; Target = 100 }
        $put = Invoke-ApiPutJson "$($c.path)/$firstId" $putBody
        Write-Host "=> $($put.status)"
        $report[-1].put = $put.status

        Write-Host "DELETE $($c.path)/$firstId"
        $del = Invoke-ApiDelete "$($c.path)/$firstId"
        Write-Host "=> $($del.status)"
        $report[-1].delete = $del.status
    }
}

# Output summary table
Write-Host "`n=== Summary ===`n"
$report | Format-Table -AutoSize

Write-Host "`nNotes:`n- If you see 401/403 for some endpoints, they require authentication (Identity).`n- Compare these results with README: modules listed in README should have endpoints above. The README also states uploads and deletions are protected by roles; if endpoints returned 200/201 anonymously then role restrictions are not enforced at controller level.`n"

Write-Host "Test script finished. Remove $csvPath if desired."