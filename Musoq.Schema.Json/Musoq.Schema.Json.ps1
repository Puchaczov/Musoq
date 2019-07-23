if (Test-Path ".\bin\Published\*") 
{
	Write-Host 'Removing .\bin\Published\*';
	Remove-Item -Recurse -Force (Resolve-Path ".\bin\Published\*"); 
}

$apiUrl = "https://soupinf.net"
$project = "Musoq.Schema.Json";

dotnet publish ".\$project.csproj" --configuration "Release" --output ".\bin\Published\Application";

$pathToAssembly = Resolve-Path ".\bin\Published\Application\$project.dll";
$assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($pathToAssembly);
$version = $assemblyName.Version.ToString();

$publishedDir = (Resolve-Path ".\bin\Published\Application");
$installerPath = [System.IO.Path]::Combine((Resolve-Path ".\bin\Published"), "$project.zip");

Compress-Archive -Path "$publishedDir\*" -DestinationPath $installerPath -CompressionLevel "Optimal";

$continueAnyway = Read-Host -Prompt "You are about to publish program in version $version (y/n)";

if($continueAnyway -ne 'Y' -or $continueAnyway -ne 'y'){
	exit;
}

$publishType = Read-Host -Prompt "Select publish type. 2 - Full version, 3 - Diff version, 4 - Separate version";

if($publishType -ne '2' -and $publishType -ne '3' -and $publishType -ne '4'){
	exit;
}

$account = $env:PACKER_ACCOUNT_NAME;
$password = $env:PACKER_ACCOUNT_PASSWORD;
$packageId = '09f009ac-5077-48d8-962d-06cf1e4566fa';

switch($publishType){
	"2"
	{
		dotnet "$env:PACKER_LOCATION\Sui.Repository.Packer.dll" --packType $publishType --destinationVersionDirPath "$publishedDir" --installerPath "$installerPath" --entryPoint "$project.dll" --outputPath "E:\Temp" --apiUrl "$apiUrl" --login $account --password $password --packageId $packageId;
		break;
	}
	"3"
	{
		dotnet "$env:PACKER_LOCATION\Sui.Repository.Packer.dll" --packType $publishType --destinationVersionDirPath "$publishedDir" --entryPoint "$project.dll" --outputPath "E:\Temp" --apiUrl "$apiUrl" --login $account --password $password --packageId $packageId;
		break;
	}
	"4"
	{
		dotnet "$env:PACKER_LOCATION\Sui.Repository.Packer.dll" --packType $publishType --destinationVersionDirPath "$publishedDir" --installerPath "$installerPath" --entryPoint "$project.dll" --outputPath "E:\Temp" --apiUrl "$apiUrl" --login $account --password $password --packageId $packageId;
		break;
	}
}