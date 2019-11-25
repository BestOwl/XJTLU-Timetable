param($uri)
Write-Host("Start to download store certificate from") 
$header = @{
    Authorization = "${env:PrivateStorage_AccessToken}"
}

Invoke-WebRequest -Uri "$uri" -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")
