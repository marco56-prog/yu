param(
    [Parameter(Mandatory=$true)]
    [string]$Key,

    [Parameter(Mandatory=$true)]
    [string]$Value
)

$resxPath = "./AccountingSystem.WPF/Resources/Strings.resx"

# Load the XML file
try {
    $xml = [xml](Get-Content $resxPath)
}
catch {
    Write-Error "Failed to load or parse the ResX file at '$resxPath'. Error: $_"
    exit 1
}

# Find the root element
$root = $xml.root

# Check if the key already exists
$existingNode = $root.SelectSingleNode("//data[@name='$Key']")

if ($existingNode) {
    Write-Warning "Resource key '$Key' already exists. No changes were made."
    exit
}

# Create the new 'data' element
$newData = $xml.CreateElement("data")
$newData.SetAttribute("name", $Key)
$newData.SetAttribute("xml:space", "preserve")

# Create the 'value' element and set its content
$newValue = $xml.CreateElement("value")
$newValue.InnerText = $Value

# Append the new elements
$newData.AppendChild($newValue)
$root.AppendChild($newData)

# Save the modified XML file
try {
    $xml.Save($resxPath)
    Write-Host "Successfully added resource '$Key' to '$resxPath'."
}
catch {
    Write-Error "Failed to save the ResX file. Error: $_"
    exit 1
}