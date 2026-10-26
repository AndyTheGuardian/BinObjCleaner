<#
    Startet BinObj Cleaner immer mit dem aktuellen Stand.

    Warum diese Datei? Laeuft die App noch, kann MSBuild die EXE nicht
    ersetzen (MSB3026) - Visual Studio startet dann eine alte Datei.
    Dieses Skript beendet zuerst alle laufenden Instanzen, baut neu und
    startet die frisch erzeugte EXE.

    Aufruf:  powershell -ExecutionPolicy Bypass -File start.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\BinObjCleaner\BinObjCleaner.csproj'
$exe = Join-Path $root "src\BinObjCleaner\bin\$Configuration\net10.0\BinObjCleaner.exe"

Write-Host '[1/3] Beende laufende Instanzen ...' -ForegroundColor DarkGray
Get-Process BinObjCleaner -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

Write-Host '[2/3] Baue ...' -ForegroundColor DarkGray
& dotnet build $project -c $Configuration -v q --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host '    Build fehlgeschlagen - siehe Ausgabe oben.' -ForegroundColor Red
    Write-Host ''
    Read-Host '    Taste druecken zum Beenden'
    exit 1
}

if (-not (Test-Path $exe)) {
    Write-Host "    Startdatei fehlt: $exe" -ForegroundColor Red
    Read-Host '    Taste druecken zum Beenden'
    exit 1
}

Write-Host '[3/3] Starte die neue Version ...' -ForegroundColor DarkGray
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
