# Shared by every testsm proof (dot-source it). RigVM's host side is pwsh 7.5 only (P111): its entry scripts run in a
# pwsh child, never powershell.exe, so an entry's `exit` can't end the proof.
function Invoke-RigEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Entry,
        [Parameter(Mandatory)] [hashtable] $Params,
        [Parameter(Mandatory)] [string] $WorkDir
    )
    New-Item -ItemType Directory -Force $WorkDir | Out-Null
    $pf = Join-Path $WorkDir "rigentry-$([guid]::NewGuid().ToString('N').Substring(0, 8)).clixml"
    $Params | Export-Clixml -Path $pf
    try {
        $cmd = "`$p = Import-Clixml -LiteralPath '$pf'; & '$Entry' @p; exit `$LASTEXITCODE"
        & pwsh -NoProfile -Command $cmd | Out-Host
        return [int] $LASTEXITCODE
    } finally {
        Remove-Item -Force $pf -ErrorAction SilentlyContinue
    }
}

function Get-SiblingRepo {
    param([Parameter(Mandatory)] [string] $Name, [string] $From = $PSScriptRoot)
    $common = (git -C $From rev-parse --path-format=absolute --git-common-dir 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $common) { throw "Get-SiblingRepo: $From is not in a git checkout" }
    $own = Split-Path -Parent (Resolve-Path $common).Path
    $path = Join-Path (Split-Path -Parent $own) $Name
    if (-not (Test-Path $path)) { throw "Get-SiblingRepo: no sibling repo '$Name' at $path" }
    $path
}

# A folder root named by an environment variable; fails clearly when it is not set.
function Get-EnvRoot {
    param([Parameter(Mandatory)] [string] $Var, [string] $What)
    $v = [Environment]::GetEnvironmentVariable($Var)
    if (-not $v -or -not (Test-Path $v)) { throw "Set $Var to $What (it is unset or the folder is missing)." }
    $v
}
