# Enables loadFromRemoteSources in AutoCAD acad.exe.config (required for NETLOAD from Downloads etc.)
# Run as Administrator: .\EnableLoadFromRemoteSources.ps1 -AutoCadYear 2023

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('2022', '2023')]
    [string]$AutoCadYear
)

$ErrorActionPreference = 'Stop'
$configPath = Join-Path $env:ProgramFiles "Autodesk\AutoCAD $AutoCadYear\acad.exe.config"

if (-not (Test-Path $configPath)) {
    Write-Error "Not found: $configPath"
}

[xml]$xml = Get-Content -Path $configPath -Encoding UTF8
$configuration = $xml.configuration
if ($null -eq $configuration) {
    Write-Error "Invalid config: missing <configuration>"
}

$runtime = $configuration.runtime
if ($null -eq $runtime) {
    $runtime = $xml.CreateElement('runtime')
    [void]$configuration.AppendChild($runtime)
}

$existing = $runtime.loadFromRemoteSources
if ($null -eq $existing) {
    $node = $xml.CreateElement('loadFromRemoteSources')
    $node.SetAttribute('enabled', 'true')
    [void]$runtime.AppendChild($node)
}
else {
    $existing.SetAttribute('enabled', 'true')
}

$backup = "$configPath.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item -Path $configPath -Destination $backup -Force
$xml.Save($configPath)

Write-Host "Updated: $configPath"
Write-Host "Backup:  $backup"
Write-Host "Restart AutoCAD $AutoCadYear for changes to take effect."
