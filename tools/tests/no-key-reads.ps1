#Requires -Version 7.5
# P112: a mod key must never fire while the player types in a game text field. SES reads no keys at all, so this scan
# fails on any key read in the mod's source; adding one means adding a text-focus guard first (and changing this scan to
# allow reads only through that guard).
param([string] $Path = (Join-Path $PSScriptRoot '..\..\src'))

$patterns = @(
    'Input\.(GetKey|GetKeyDown|GetKeyUp|GetButton|GetButtonDown|GetButtonUp|anyKey|anyKeyDown|inputString)\b',
    '\bKeyCode\.',
    '\bKeyboardShortcut\b',
    '\bKeyManager\b',
    '\bKeyboard\.current\b',
    '\bImGui\.(IsKey|GetKey)'
)
$hits = Get-ChildItem -Path $Path -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
    Select-String -Pattern $patterns
if ($hits) {
    $hits | ForEach-Object { Write-Host "key read: $($_.Path | Resolve-Path -Relative):$($_.LineNumber): $($_.Line.Trim())" }
    Write-Host "no-key-reads: $(@($hits).Count) key read(s); route them through a text-focus guard (P112)."
    exit 1
}
Write-Host 'no-key-reads: clean (the mod reads no keys)'
exit 0
