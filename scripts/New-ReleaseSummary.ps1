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
    $extension = if ($package.runtime.StartsWith('win-') -or $package.runtime.StartsWith('osx-')) { '.zip' } else { '.tar.gz' }
    $flavor = if ($package.selfContained) { '' } else { '-framework-dependent' }
    if ($package.version -ne $Version -or $package.sha256 -notmatch '^[a-f0-9]{64}$' -or
        $package.archive -ne "VideoScrap2000-$Version-$($package.runtime)$flavor$extension") { throw 'Invalid package manifest.' }
}
$baseUrl = "https://github.com/$Repository/releases/download/v$Version"
$installers = @(Get-ChildItem -LiteralPath $PackageRoot -Filter '*.deb-info.json' -File -Recurse |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json } | Sort-Object architecture)
if ($installers.Count -gt 0) {
    if ($installers.Count -ne 2 -or @($installers.architecture | Sort-Object -Unique).Count -ne 2) { throw 'Expected two DEB installer manifests.' }
    foreach ($installer in $installers) {
        if ($installer.architecture -notin @('amd64','arm64') -or $installer.version -ne $Version -or
            $installer.archive -ne "videoscrap2000_${Version}_$($installer.architecture).deb" -or
            $installer.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid DEB installer manifest.' }
        $files = @(Get-ChildItem -LiteralPath $PackageRoot -Filter $installer.archive -File -Recurse)
        if ($files.Count -ne 1 -or $files[0].Length -ne $installer.downloadBytes -or
            (Get-FileHash -LiteralPath $files[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $installer.sha256) { throw 'DEB installer integrity check failed.' }
    }
}
function Get-InstallerLink([string]$Architecture, [string]$Label) {
    $installer = $installers | Where-Object architecture -eq $Architecture
    $size = ($installer.downloadBytes / 1MB).ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
    return "[$Label · $size MiB]($baseUrl/$($installer.archive))"
}
function Get-DownloadLink([string]$Runtime, [bool]$Bundled, [string]$Label) {
    $package = $packages | Where-Object { $_.runtime -eq $Runtime -and $_.selfContained -eq $Bundled } | Select-Object -First 1
    $size = ($package.downloadBytes / 1MB).ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
    return "[$Label · $size MiB]($baseUrl/$($package.archive))"
}
$diskImages = @(Get-ChildItem -LiteralPath $PackageRoot -Filter '*.dmg-info.json' -File -Recurse |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
if ($diskImages.Count -gt 0) {
    if ($diskImages.Count -ne 2 -or @($diskImages.runtime | Sort-Object -Unique).Count -ne 2) { throw 'Expected Intel and Apple Silicon DMG manifests.' }
    foreach ($disk in $diskImages) {
        if ($disk.runtime -notin @('osx-x64','osx-arm64') -or !$disk.signed -or !$disk.notarized -or
            $disk.version -ne $Version -or $disk.archive -ne "VideoScrap2000-$Version-$($disk.runtime).dmg") { throw 'Invalid signed DMG manifest.' }
        $image = @(Get-ChildItem -LiteralPath $PackageRoot -Filter $disk.archive -File -Recurse)
        if ($image.Count -ne 1 -or $image[0].Length -ne $disk.downloadBytes -or
            (Get-FileHash -LiteralPath $image[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $disk.sha256) { throw 'DMG integrity check failed.' }
    }
}
function Get-MacLink([string]$Runtime, [bool]$Bundled, [string]$Label) {
    $disk = $diskImages | Where-Object runtime -eq $Runtime
    if (!$Bundled -or !$disk) { return Get-DownloadLink $Runtime $Bundled $Label }
    $size = ($disk.downloadBytes / 1MB).ToString('F1', [Globalization.CultureInfo]::InvariantCulture)
    return "[$Label · DMG · $size MiB]($baseUrl/$($disk.archive))"
}
function Get-DownloadTable([bool]$Bundled) {
    @(
        '| System | Intel / AMD 64-bit | ARM64 / Apple Silicon |'
        '| --- | --- | --- |'
        "| Windows | $(Get-DownloadLink 'win-x64' $Bundled 'Windows 64-bit') | $(Get-DownloadLink 'win-arm64' $Bundled 'Windows ARM64') |"
        "| macOS | $(Get-MacLink 'osx-x64' $Bundled 'Intel Mac') | $(Get-MacLink 'osx-arm64' $Bundled 'Apple Silicon Mac') |"
        $(if ($Bundled -and $installers.Count -gt 0) {
            "| Linux Mint / Ubuntu / Debian | $(Get-InstallerLink 'amd64' 'DEB · 64-bit') | $(Get-InstallerLink 'arm64' 'DEB · ARM64') |"
        } else {
            "| Linux archive | $(Get-DownloadLink 'linux-x64' $Bundled 'Linux 64-bit') | $(Get-DownloadLink 'linux-arm64' $Bundled 'Linux ARM64') |"
        })
    ) -join "`n"
}
$downloads = @(
    '## Downloads'
    ''
    '**Start here:** choose your system and processor below. These downloads include .NET 10; no separate .NET installation is needed.'
    ''
    (Get-DownloadTable $true)
    ''
    $(if ($diskImages.Count -gt 0) {
        "**macOS:** open the DMG and drag **VideoScrap 2000.app** into **Applications**. Developer ID signed and notarized by Apple. No separate .NET installation required.`n`n<details>`n<summary>macOS ZIP alternatives</summary>`n`n$(Get-DownloadLink 'osx-x64' $true 'Intel Mac ZIP') · $(Get-DownloadLink 'osx-arm64' $true 'Apple Silicon Mac ZIP')`n`n</details>`n"
    })
    $(if ($installers.Count -gt 0) {
        "**Linux DEB:** install the downloaded file, then start **VideoScrap 2000** from the application menu. Includes .NET 10. Tested on Ubuntu 22.04/24.04 (Linux Mint 21/22 bases).`n`n<details>`n<summary>Linux archives for other distributions or portable use</summary>`n`n$(Get-DownloadLink 'linux-x64' $true 'Linux 64-bit') · $(Get-DownloadLink 'linux-arm64' $true 'Linux ARM64')`n`n</details>`n"
    })
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
[IO.File]::WriteAllText([IO.Path]::GetFullPath($ChecksumPath), ((@($packages) + @($installers) + @($diskImages) | ForEach-Object { "$($_.sha256)  $($_.archive)" }) -join "`n") + "`n")
Write-Host "Prepared download tables and one checksum file for $($packages.Count + $installers.Count + $diskImages.Count) packages."
