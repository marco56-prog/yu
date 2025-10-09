param(
    [string]$Key,
    [string]$Value
)

if (-not $Key -or -not $Value) {
    Write-Host "Usage: .\add-resource.ps1 -Key <resource_key> -Value <resource_value>"
    exit 1
}

$filePath = ".\AccountingSystem.WPF\Properties\Strings.resx"

$xml = [xml](Get-Content $filePath)

$dataNode = $xml.CreateElement("data")
$dataNode.SetAttribute("name", $Key)
$dataNode.SetAttribute("xml:space", "preserve")

$valueNode = $xml.CreateElement("value")
$valueNode.InnerText = $Value

$dataNode.AppendChild($valueNode)

$xml.root.AppendChild($dataNode)

$xml.Save($filePath)

Write-Host "Resource '$Key' added to $filePath"