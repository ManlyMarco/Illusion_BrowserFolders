if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$array = @("KK", "AI")


$copy = $dir + "\copy\BepInEx" 


New-Item -ItemType Directory -Force -Path ($dir + "\out")  

foreach ($element in $array) {

Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "*.*") -Destination ($copy + "\plugins\" ) -Recurse -Force 

$ver = "v" + (Get-ChildItem -Path ($copy) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString()

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BrowserFolders_" + $ver + ".zip")

}

Remove-Item -Force -Path ($dir + "\copy") -Recurse