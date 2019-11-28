param([String]$project, [String]$apiKey)

$fileName=Get-ChildItem -Path $path -File | Select-Object -First 1 | Select -exp Name
$text = Nuget.exe list Puchaczov | Select-String -Pattern $project | Out-String
$name,$version = $text.trim().split(" ")
$publishedFileName = "$name.$version.nupkg"

if ($fileName -ne $publishedFileName){
	echo $fileName, $publishedFileName
	dotnet push "./$fileName" -Source https://api.nuget.org/v3/index.json -ApiKey $apiKey
}
