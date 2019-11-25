Write-Host("Start to download store certificate from")
$url = "${env:PrivateStorage_BaseUri}${env:PrivateStorage_FileToDownload}"
$header = @{
    Authorization = "${env:PrivateStorage_AccessToken}"
}

Invoke-WebRequest -Uri "${downloadUri}" -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")