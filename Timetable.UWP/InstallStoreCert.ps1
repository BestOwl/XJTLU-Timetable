Write-Host("Start to download store certificate from") 
$header = @{
    Authorization = "${env:PrivateStorage_AccessToken}"
}

Invoke-WebRequest -Uri "${env:PrivateStorage_BaseUri}${env:PrivateStorage_FileToDownload}" -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")
