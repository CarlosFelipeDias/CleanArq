param([string]$ContainerName = 'keycloak_local')
$ErrorActionPreference = 'Stop'
$themePath = Join-Path $PSScriptRoot 'themes/cleanarq'
docker cp $themePath "${ContainerName}:/opt/keycloak/themes/cleanarq"
if ($LASTEXITCODE -ne 0) { throw 'Unable to copy the theme to Keycloak.' }
Write-Host 'Theme installed. Select cleanarq under Realm settings > Themes > Login theme.'
Write-Host 'Re-run this script after recreating the container, or mount ./themes at /opt/keycloak/themes in your own compose file.'
