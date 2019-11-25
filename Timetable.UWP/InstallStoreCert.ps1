param($uri)
Write-Host("Start to download store certificate from") 
$header = @{
    Authorization = "${env:PrivateStorage_AccessToken}"
}

Invoke-WebRequest -Uri "https://goodtimestudio.visualstudio.com/PrivateStorage/_apis/git/repositories/PrivateStorage/items?api-version=1.0&scopePath=Timetable_UWP_Store.pfx" -UserAgent VSTS-Get -ContentType "application/json" -Method Get -Headers $header -OutFile "./StoreCert.pfx"
Write-Host("Download completed")
