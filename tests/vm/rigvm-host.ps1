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
