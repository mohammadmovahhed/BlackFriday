param(
    [Parameter(Mandatory=$true)]
    [string]$SaleEventId,

    [int]$Concurrency = 200,
    [int]$Capacity = 50,
    [string]$BaseUrl = "https://localhost:7061",
    [string]$ApiKey = "demo-tenant"
)

Write-Host "Firing $Concurrency concurrent reservations at capacity=$Capacity"
Write-Host "SaleEvent: $SaleEventId"
Write-Host "----------------------------------------"

$sw = [System.Diagnostics.Stopwatch]::StartNew()

$jobs = 1..$Concurrency | ForEach-Object {
    $i = $_
    Start-ThreadJob -ScriptBlock {
        param($url, $apiKey, $saleEventId, $i)

        $body = @{
            saleEventId = $saleEventId
            sku = "TV-TEST-CONCURRENCY"
            quantity = 1
            customerId = "cust-$i"
        } | ConvertTo-Json

        $headers = @{
            "X-Api-Key" = $apiKey
            "Content-Type" = "application/json"
        }

        try {
            $response = Invoke-WebRequest -Uri "$url/v1/reservations" `
                -Method POST -Headers $headers -Body $body `
                -SkipCertificateCheck -ErrorAction Stop
            return @{ Status = [int]$response.StatusCode }
        }
        catch {
            $code = 0
            if ($_.Exception.Response) {
                $code = [int]$_.Exception.Response.StatusCode.value__
            }
            return @{ Status = $code }
        }
    } -ArgumentList $BaseUrl, $ApiKey, $SaleEventId, $i
}

$results = $jobs | Receive-Job -Wait -AutoRemoveJob
$sw.Stop()

$success = ($results | Where-Object { $_.Status -eq 200 }).Count
$soldOut = ($results | Where-Object { $_.Status -eq 409 }).Count
$errors  = ($results | Where-Object { $_.Status -ne 200 -and $_.Status -ne 409 }).Count

Write-Host ""
Write-Host "----------------------------------------"
Write-Host "200 OK (reserved):   $success"
Write-Host "409 Sold Out:        $soldOut"
Write-Host "Other errors:        $errors"
Write-Host "Expected reserved:   $Capacity"
Write-Host "Elapsed:             $($sw.ElapsedMilliseconds) ms"
Write-Host "----------------------------------------"

if ($success -eq $Capacity) {
    Write-Host "PASS - exactly $Capacity reserved, no oversell"
    exit 0
} else {
    Write-Host "FAIL - expected $Capacity, got $success"
    exit 1
}