param(
    [string]$ImagePath = "C:\Users\ASUS\.gemini\antigravity\scratch\VehicleVisionOCR\apps\frontend-react\sample-image.jpg",
    [int]$Iterations = 5
)

$uri = "http://localhost:5000/api/MobileScanner/upload"
$historyUri = "http://localhost:5000/api/vehicles/history"
$expectedVin = "ME4MC56FGTA009533"
$expectedColor = "ATHLETIC BLUE METALLIC"

Write-Host "Starting OCR Test Script. Iterations: $Iterations" -ForegroundColor Cyan

$successCount = 0
$failCount = 0

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "`n--- Iteration $i ---"
    
    $fileBytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $boundary = [System.Guid]::NewGuid().ToString() 
    $LF = "`r`n"
    
    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"image`"; filename=`"sample-image.jpg`"",
        "Content-Type: image/jpeg",
        "",
        ""
    )
    $bodyString = $bodyLines -join $LF
    $bodyBytes = [System.Text.Encoding]::ASCII.GetBytes($bodyString)
    
    $footerLines = (
        "",
        "--$boundary--",
        ""
    )
    $footerString = $footerLines -join $LF
    $footerBytes = [System.Text.Encoding]::ASCII.GetBytes($footerString)
    
    $payload = New-Object byte[] ($bodyBytes.Length + $fileBytes.Length + $footerBytes.Length)
    [System.Array]::Copy($bodyBytes, 0, $payload, 0, $bodyBytes.Length)
    [System.Array]::Copy($fileBytes, 0, $payload, $bodyBytes.Length, $fileBytes.Length)
    [System.Array]::Copy($footerBytes, 0, $payload, $bodyBytes.Length + $fileBytes.Length, $footerBytes.Length)
    
    try {
        $response = Invoke-RestMethod -Uri $uri -Method Post -ContentType "multipart/form-data; boundary=$boundary" -Body $payload
        
        Write-Host "Waiting for backend OCR processing (3s)..."
        Start-Sleep -Seconds 5
        
        $history = Invoke-RestMethod -Uri $historyUri -Method Get
        $latest = $history[0]
        
        $vin = $latest.registrationNumber
        if (-not $vin) { $vin = $latest.vin }
        $color = $latest.color

        $vinMatch = $vin -eq $expectedVin
        $colorMatch = $color -eq $expectedColor

        Write-Host "Extracted VIN:   $vin" -ForegroundColor $(if ($vinMatch) { "Green" } else { "Red" })
        Write-Host "Extracted Color: $color" -ForegroundColor $(if ($colorMatch) { "Green" } else { "Red" })

        if ($vinMatch -and $colorMatch) {
            Write-Host "Result: PASS" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "Result: FAIL" -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host "API Request Failed: $_" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`n========================="
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "Total Passes: $successCount / $Iterations" -ForegroundColor Green
Write-Host "Total Fails:  $failCount / $Iterations" -ForegroundColor Red
Write-Host "========================="

if ($failCount -gt 0) { exit 1 }
exit 0
