$version = dotnet-gitversion | ConvertFrom-Json
# Write-Host $version
$currentPath = Split-Path $MyInvocation.MyCommand.Path
$path = "$currentPath/src/LinqInfer/LinqInfer.csproj"
$xml = [XML](Get-Content $path)
$nodes = $xml.Project.PropertyGroup.ChildNodes

foreach($node in $nodes) 
{
	if($node.Name -eq 'Version') 
	{
		Write-Host "Set" $node.Name "to" $version.NuGetVersionV2
		$node.InnerText = $version.NuGetVersionV2
	}
}
$xml.Save($path)
