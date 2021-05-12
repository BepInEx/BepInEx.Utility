$dir = $PSScriptRoot + "/bin/"
$copy = $dir + "/copy/BepInEx"

Remove-Item -Force -Path ($dir + "/copy") -Recurse -ErrorAction SilentlyContinue
Remove-Item -Force -Path ($dir + "/out") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "/plugins")

foreach ($filepath in [System.IO.Directory]::EnumerateFiles($dir + "/BepInEx/plugins","*.dll"))
{
	$filename = Split-Path $filepath -leaf

	Remove-Item -Force -Path ($copy) -Recurse
	New-Item -ItemType Directory -Force -Path ($copy + "/plugins/")
	Copy-Item -Path ($filepath) -Destination ($copy + "/plugins/") -Force

	$version = "v" + (Get-ChildItem -Path ($filepath) -Filter "*.dll" -Force)[0].VersionInfo.FileVersion.ToString()
	$zipfilename = $filename.Replace(".dll", " " + $version + ".zip")

	"Creating archive: " + $zipfilename
	Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + $zipfilename)
}

foreach ($filepath in [System.IO.Directory]::EnumerateFiles($dir + "/BepInEx/patchers","*.dll"))
{
	$filename = Split-Path $filepath -leaf

	Remove-Item -Force -Path ($copy) -Recurse
	New-Item -ItemType Directory -Force -Path ($copy + "/patchers/")
	Copy-Item -Path ($filepath) -Destination ($copy + "/patchers/") -Force

	$version = "v" + (Get-ChildItem -Path ($filepath) -Filter "*.dll" -Force)[0].VersionInfo.FileVersion.ToString()
	$zipfilename = $filename.Replace(".dll", " " + $version + ".zip")

	"Creating archive: " + $zipfilename
	Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + $zipfilename)
}

Remove-Item -Force -Path ($dir + "/copy") -Recurse
