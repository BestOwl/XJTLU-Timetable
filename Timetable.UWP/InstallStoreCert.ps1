Write-Host("Start to download store certificate from")
$url = "${env:PrivateStorage_BaseUri}${env:PrivateStorage_FileToDownload}"
$header = @{
    Authorization = "${env:PrivateStorage_AccessToken}"
}
Write-Host($url)
Invoke-WebRequest -Uri $url -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")