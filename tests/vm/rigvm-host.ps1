# Shared by every tests\vm proof (dot-source it). RigVM's entry scripts are `#Requires -PSEdition Desktop`, so a proof running under
# pwsh 7 starts them as a Windows PowerShell 5.1 child; there is no in-process path.
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
    $savedModulePath = $env:PSModulePath
    try {
        $cmd = "`$p = Import-Clixml -LiteralPath '$pf'; & '$Entry' @p; exit `$LASTEXITCODE"
        # Exactly the Windows PowerShell module folders: pwsh 7's PSModulePath breaks 5.1's core modules.
        $env:PSModulePath = (@(
            (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'WindowsPowerShell\Modules'),
            (Join-Path $env:ProgramFiles 'WindowsPowerShell\Modules'),
            (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\Modules')
        )) -join ';'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $cmd | Out-Host
        return [int] $LASTEXITCODE
    } finally {
        $env:PSModulePath = $savedModulePath
        Remove-Item -Force $pf -ErrorAction SilentlyContinue
    }
}

# A repo that sits beside this one (RigVM, Aegis, StationGodMCP): found through git's common dir, never by ..\ (a worktree is nested).
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
