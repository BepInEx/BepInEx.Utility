$dir = $PSScriptRoot + "/bin/"
$copy = $dir + "/copy/BepInEx"

Remove-Item -Force -Path ($dir + "/copy") -Recurse -ErrorAction SilentlyContinue
Remove-Item -Force -Path ($dir + "/out") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "/plugins")
New-Item -ItemType Directory -Force -Path ($dir + "/out")

foreach ($filepath in [System.IO.Directory]::EnumerateFiles($dir,"*.dll"))
{
	$filename = $filepath.Replace($dir, "")

	Remove-Item -Force -Path ($copy) -Recurse
	New-Item -ItemType Directory -Force -Path ($copy + "/plugins/")
	Copy-Item -Path ($filepath) -Destination ($copy + "/plugins/") -Force

	$version = "v" + (Get-ChildItem -Path ($filepath) -Filter "*.dll" -Force)[0].VersionInfo.FileVersion.ToString()
	$zipfilename = $filename.Replace(".dll", " " + $version + ".zip")

	"Creating archive: " + $zipfilename
	Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out/" + $zipfilename)
}

Remove-Item -Force -Path ($dir + "/copy") -Recurse
