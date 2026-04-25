# Build the HSK RN Weapon Router DLL.
# Output: ../../1.5/Assemblies/HSKRNRouter.dll
#
# Usage:  .\_build.ps1
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build -c Release --nologo
} finally {
    Pop-Location
}
