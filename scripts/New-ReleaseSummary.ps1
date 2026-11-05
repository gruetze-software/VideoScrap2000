param(
    [Parameter(Mandatory)][string]$PackageRoot,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Repository,
    [string]$NotesPath = 'release-notes.md',
    [string]$ChecksumPath = 'SHA256SUMS.txt'
)
$ErrorActionPreference = 'Stop'
$packages = @(Get-ChildItem -LiteralPath $PackageRoot -Filter '*.manifest.json' -File -Recurse |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json } | Sort-Object archive)
$runtimes = @('win-x64', 'win-arm64', 'osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64')
if ($packages.Count -ne 12) { throw 'Expected twelve package manifests.' }
foreach ($runtime in $runtimes) {
    $variants = @($packages | Where-Object runtime -eq $runtime)
    if ($variants.Count -ne 2 -or @($variants | Where-Object selfContained).Count -ne 1) { throw "Missing package variant: $runtime" }
}
foreach ($package in $packages) {
    $extension = if ($package.runtime.StartsWith('win-')) { '.zip' } else { '.tar.gz' }
    $flavor = if ($package.selfContained) { '' } else { '-framework-dependent' }
    if ($package.version -ne $Version -or $package.sha256 -notmatch '^[a-f0-9]{64}$' -or
        $package.archive -ne "VideoScrap2000-$Version-$($package.runtime)$flavor$extension") { throw 'Invalid package manifest.' }
}
$baseUrl = "https://github.com/$Repository/releases/download/v$Version"
function Get-DownloadLink([string]$Runtime, [bool]$Bundled, [string]$Label) {
    $package = $packages | Where-Object { $_.runtime -eq $Runtime -and $_.selfContained -eq $Bundled } | Select-Object -First 1
    $size = ($package.downloadBytes / 1MB).ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
    return "[$Label · $size MiB]($baseUrl/$($package.archive))"
}
function Get-DownloadTable([bool]$Bundled) {
    @(
        '| System | Intel / AMD 64-bit | ARM64 / Apple Silicon |'
        '| --- | --- | --- |'
        "| Windows | $(Get-DownloadLink 'win-x64' $Bundled 'Windows 64-bit') | $(Get-DownloadLink 'win-arm64' $Bundled 'Windows ARM64') |"
        "| macOS | $(Get-DownloadLink 'osx-x64' $Bundled 'Intel Mac') | $(Get-DownloadLink 'osx-arm64' $Bundled 'Apple Silicon Mac') |"
        "| Linux | $(Get-DownloadLink 'linux-x64' $Bundled 'Linux 64-bit') | $(Get-DownloadLink 'linux-arm64' $Bundled 'Linux ARM64') |"
    ) -join "`n"
}
$downloads = @(
    '## Downloads'
    ''
    '**Start here:** choose your system and processor below. These downloads include .NET 10; no separate .NET installation is needed.'
    ''
    (Get-DownloadTable $true)
    ''
    '<details>'
    '<summary>Smaller downloads — only if .NET 10 Runtime is already installed</summary>'
    ''
    'These framework-dependent packages require the .NET 10 Runtime for the selected processor.'
    ''
    (Get-DownloadTable $false)
    ''
    '</details>'
    ''
    "Optional: [SHA-256 checksums]($baseUrl/SHA256SUMS.txt). GitHub's **Source code** files are for developers, not for running the app."
    ''
) -join "`n"
$source = Join-Path $PSScriptRoot "../docs/releases/v$Version.md"
$body = if (Test-Path -LiteralPath $source) { Get-Content -LiteralPath $source -Raw } else { "# VideoScrap 2000 $Version`n`n<!-- RELEASE_DOWNLOADS -->`n" }
if ($body.Contains('<!-- RELEASE_DOWNLOADS -->')) { $body = $body.Replace('<!-- RELEASE_DOWNLOADS -->', $downloads) }
else { $body = $downloads + "`n" + $body }
$body += "`n`n<details>`n<summary>Package sizes after extraction</summary>`n`n| Platform | Package | Extracted |`n| --- | --- | --- |`n"
foreach ($package in $packages) {
    $flavor = if ($package.selfContained) { '.NET included' } else { '.NET 10 required' }
    $size = ($package.installedBytes / 1MB).ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
    $body += "| $($package.runtime) | $flavor | $size MiB |`n"
}
$body += "`n</details>`n"
[IO.File]::WriteAllText([IO.Path]::GetFullPath($NotesPath), $body)
[IO.File]::WriteAllText([IO.Path]::GetFullPath($ChecksumPath), (($packages | ForEach-Object { "$($_.sha256)  $($_.archive)" }) -join "`n") + "`n")
Write-Host "Prepared download tables and one checksum file for all twelve packages."
