# PowerShell Script to Find Hardcoded Arabic Strings in XAML Files

# --- Configuration ---
# Target directory to scan for XAML files.
$targetDirectory = ".\AccountingSystem.WPF"
# Regular expression to find hardcoded Arabic strings in XAML attributes.
# This looks for attributes like `Content="some arabic text"` or `Header="some arabic text"`
$regex = '((Content|Header|Title|Text|Tag|ToolTip|Message|Header)="([^"]*[\u0600-\u06FF]+[^"]*)"'

# --- Script ---
# Get all XAML files recursively from the target directory.
Get-ChildItem -Path $targetDirectory -Recurse -Filter *.xaml | ForEach-Object {
    $file = $_
    # Read the content of the file.
    $content = Get-Content $file.FullName -Raw

    # Find all matches for the regex in the file content.
    $matches = $content | Select-String -Pattern $regex -AllMatches

    # If any matches are found, print the file path and the matched strings.
    if ($matches) {
        Write-Host "File: $($file.FullName)"
        $matches.Matches | ForEach-Object {
            Write-Host "  - $($_.Value)"
        }
    }
}