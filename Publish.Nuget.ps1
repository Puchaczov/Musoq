param([String] $nuget, [String]$project, [String]$apiKey)

$fileName=Get-ChildItem -Path $path -File | Select-Object -First 1 | Select -exp Name
$text = Invoke-Expression "$nuget list Puchaczov" | Select-String -Pattern $project | Out-String
$name,$version = $text.trim().split(" ")
$publishedFileName = "$name.$version.nupkg"

if ($fileName -ne $publishedFileName){
	echo "Publishing $fileName...";
	Invoke-Expression "$nuget push './$fileName' -Source https://api.nuget.org/v3/index.json -ApiKey $apiKey -skipDuplicate"
	echo "done."
}
else
{
	echo "File $publishedFileName has already been published. Skipping."
}
