# Script to find hard-coded strings in WPF XAML files that should be moved to resource files.

$searchPath = "./AccountingSystem.WPF"
Write-Host "Searching for hard-coded strings in XAML files under '$searchPath'..."

# Regex to find attributes (like Content, Header, Text, Title) with hard-coded strings.
# It avoids strings that are bindings or static resources.
$regex = '(Content|Header|Title|Text)\s*=\s*"([^"{}]*[^"{}\s]+[^"{}]*)"'

# Get all XAML files, excluding the designer cache.
$xamlFiles = Get-ChildItem -Path $searchPath -Filter "*.xaml" -Recurse | Where-Object { $_.FullName -notlike "*\obj\*" }

$foundSomething = $false

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $lines = $content.Split([Environment]::NewLine)

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $matches = [regex]::Matches($line, $regex)

        if ($matches.Count -gt 0) {
            foreach ($match in $matches) {
                $foundSomething = $true
                $hardcodedString = $match.Groups[2].Value

                # Report the finding
                Write-Host "--------------------------------------------------"
                Write-Host "File: $($file.FullName)" -ForegroundColor Yellow
                Write-Host "Line $($i + 1): $line"
                Write-Host "Found hard-coded string: '$hardcodedString'" -ForegroundColor Red
            }
        }
    }
}

if (-not $foundSomething) {
    Write-Host "No hard-coded strings found in common attributes. The project looks clean!" -ForegroundColor Green
} else {
    Write-Host "--------------------------------------------------"
    Write-Host "Search complete. Use the 'add-resource.ps1' script to move these strings to the resource file."
}