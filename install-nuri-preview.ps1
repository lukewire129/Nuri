[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = $PSScriptRoot
$previewProjectPath = Join-Path $repositoryRoot "src\Nuri.VisualStudioPreview\Nuri.VisualStudioPreview.csproj"
$vsixPath = Join-Path $repositoryRoot "src\Nuri.VisualStudioPreview\bin\$Configuration\net472\Nuri.VisualStudioPreview.vsix"
$vsixInstallerPath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VsixInstaller\VSIXInstaller.exe"
$vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

function Assert-ProcessIsNotRunning {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Message
    )

    if (Get-Process -Name $Name -ErrorAction SilentlyContinue) {
        throw $Message
    }
}

function Get-VisualStudioInstances {
    if (-not (Test-Path -LiteralPath $vswherePath)) {
        throw "vswhere.exe was not found: $vswherePath"
    }

    $json = & $vswherePath -all -prerelease -products * -format json -utf8
    if ($LASTEXITCODE -ne 0) {
        throw "vswhere.exe failed with exit code $LASTEXITCODE."
    }

    @($json | ConvertFrom-Json) |
        ForEach-Object {
            $devenvPath = Join-Path $_.installationPath "Common7\IDE\devenv.exe"
            if (Test-Path -LiteralPath $devenvPath) {
                [pscustomobject]@{
                    DisplayName = $_.displayName
                    DevenvPath = $devenvPath
                    InstanceId = $_.instanceId
                    ConfigurationVersion = "$(([version]$_.installationVersion).Major).0"
                }
            }
        }
}

function Test-NuriPreviewInstalled {
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$Instance
    )

    $instanceRoot = Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\$($Instance.ConfigurationVersion)_$($Instance.InstanceId)\Extensions"
    if (-not (Test-Path -LiteralPath $instanceRoot)) {
        return $false
    }

    foreach ($manifestPath in Get-ChildItem -LiteralPath $instanceRoot -Recurse -Filter "extension.vsixmanifest" -ErrorAction SilentlyContinue) {
        try {
            [xml]$manifest = Get-Content -LiteralPath $manifestPath.FullName -Raw
            $identity = $manifest.SelectSingleNode("//*[local-name()='Identity']")
            if ($null -ne $identity -and $identity.GetAttribute("Id") -eq "Nuri.VisualStudioPreview") {
                return $true
            }
        }
        catch {
            Write-Verbose "Could not inspect $($manifestPath.FullName): $_"
        }
    }

    return $false
}

Assert-ProcessIsNotRunning -Name "devenv" -Message "Close every Visual Studio window before installing Nuri Preview."
Assert-ProcessIsNotRunning -Name "VSIXInstaller" -Message "Another VSIX Installer is running. Close it before retrying."

$instances = @(Get-VisualStudioInstances)
if ($instances.Count -eq 0) {
    throw "No Visual Studio instance with devenv.exe was found."
}

Write-Host "[1/3] Building Nuri Preview ($Configuration)..." -ForegroundColor Cyan
Push-Location $repositoryRoot
try {
    & dotnet build $previewProjectPath -c $Configuration -p:RestoreBuildInParallel=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $vsixPath)) {
    throw "The build completed, but the VSIX was not found: $vsixPath"
}

if (-not (Test-Path -LiteralPath $vsixInstallerPath)) {
    throw "VSIXInstaller.exe was not found: $vsixInstallerPath"
}

Write-Host "[2/3] Removing an existing Nuri Preview installation when present..." -ForegroundColor Cyan
foreach ($instance in $instances) {
    if (-not (Test-NuriPreviewInstalled -Instance $instance)) {
        continue
    }

    Write-Host "  $($instance.DisplayName)"
    $uninstallArguments = @(
        "/quiet"
        "/instanceIds:$($instance.InstanceId)"
        "/u:Nuri.VisualStudioPreview"
    )
    $uninstaller = Start-Process -FilePath $vsixInstallerPath -ArgumentList $uninstallArguments -WindowStyle Hidden -Wait -PassThru
    if ($uninstaller.ExitCode -ne 0) {
        throw "Nuri Preview uninstall failed for $($instance.DisplayName). Exit code: $($uninstaller.ExitCode)"
    }
}

Assert-ProcessIsNotRunning -Name "VSIXInstaller" -Message "The previous VSIX uninstall has not finished. Wait for it to exit and retry."

Write-Host "[3/3] Installing the new VSIX..." -ForegroundColor Cyan
foreach ($instance in $instances) {
    Write-Host "  $($instance.DisplayName)"
    $installArguments = @(
        "/quiet"
        "/instanceIds:$($instance.InstanceId)"
        "`"$vsixPath`""
    )
    $installer = Start-Process -FilePath $vsixInstallerPath -ArgumentList $installArguments -WindowStyle Hidden -Wait -PassThru
    if ($installer.ExitCode -ne 0) {
        throw "Nuri Preview installation failed for $($instance.DisplayName). Exit code: $($installer.ExitCode)"
    }
}

Write-Host "Nuri Preview installation completed. Start Visual Studio to load the updated extension." -ForegroundColor Green
