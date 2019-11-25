Write-Host("Start to download store certificate from")
$url = "${env:_PrivateStorage_AccountUrl}/${env:_PrivateStorage_Project}/_apis/git/repositories/${env:_PrivateStorage_Repo}/items?api-version=1.0&scopePath=${env:_PrivateStorage_FileToDownload}"
$header = @{
    Authorization = "${env:INPUT_PrivateStorage_AccessToken}"
}

Invoke-WebRequest -Uri $url -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")