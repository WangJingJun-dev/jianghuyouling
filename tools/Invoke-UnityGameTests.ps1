param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [string]$GameRoot = $env:TAIWU_GAME_DIR,
    [string]$OutputRoot = '',
    [ValidateSet('windowed', 'headless')][string]$Mode = 'windowed',
    [int]$ReadyTimeoutSeconds = 90,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path $RepoRoot '.e2e-results' }
if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $localProps = Join-Path $RepoRoot 'Directory.Build.local.props'
    if (Test-Path -LiteralPath $localProps -PathType Leaf) {
        [xml]$props = Get-Content -LiteralPath $localProps -Raw
        $GameRoot = [string]$props.Project.PropertyGroup.TaiwuGameDir
    }
}
if ([string]::IsNullOrWhiteSpace($GameRoot)) { throw 'GameRoot or TAIWU_GAME_DIR is required' }
$shared = Join-Path (Split-Path -Parent $RepoRoot) 'UnityGameAutomation\tools\Invoke-UnityGameTests.ps1'
$safety = Join-Path $RepoRoot 'tools\e2e\protect-real-game-data.ps1'
& $shared -ManifestPath $ManifestPath -GameRoot $GameRoot -OutputRoot $OutputRoot -SafetyScript $safety -Mode $Mode -ReadyTimeoutSeconds $ReadyTimeoutSeconds -SkipBuild:$SkipBuild -UseSteamLaunch
exit $LASTEXITCODE
