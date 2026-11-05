param(
    [Parameter(Mandatory)][ValidateSet('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')][string]$Runtime,
    [switch]$FrameworkDependent
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$run = Join-Path $repository ('artifacts/' + $Runtime + '/' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $run 'VideoScrap2000'
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
$project = Join-Path $repository 'MovieScrap2000/MovieScrap2000.csproj'
[xml]$projectMetadata = Get-Content -LiteralPath $project -Raw
$version = @($projectMetadata.Project.PropertyGroup.Version | Where-Object { $_ })[0]
$flavor = if ($FrameworkDependent) { '-framework-dependent' } else { '' }
$packageName = "VideoScrap2000-$version-$Runtime$flavor"
dotnet publish $project -c Release -r $Runtime --self-contained $selfContained -o $output -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:UseSharedCompilation=false -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen.' }

if ($Runtime.StartsWith('linux-') -and $IsLinux) {
    $triplet = if ($Runtime.EndsWith('arm64')) { 'aarch64-linux-gnu' } else { 'x86_64-linux-gnu' }
    $mms = "/usr/lib/$triplet/libmms.so.0"
    if (!(Test-Path -LiteralPath $mms)) { throw 'Install libmms0 before packaging Linux releases.' }
    Copy-Item -LiteralPath $mms -Destination (Join-Path $output 'libmms.so.0')
}

# Universal macOS libraries otherwise contain a second, unused CPU architecture.
if ($Runtime.StartsWith('osx-') -and $IsMacOS) {
    $architecture = if ($Runtime.EndsWith('arm64')) { 'arm64' } else { 'x86_64' }
    foreach ($library in Get-ChildItem -LiteralPath $output -Filter '*.dylib' -File -Recurse) {
        $architectures = (& lipo -archs $library.FullName).Trim().Split(' ', [StringSplitOptions]::RemoveEmptyEntries)
        if ($LASTEXITCODE -ne 0 -or $architecture -notin $architectures) { throw "Falsche Architektur in $($library.Name)." }
        if ($architectures.Count -gt 1) {
            $thin = $library.FullName + '.thin'
            & lipo $library.FullName -thin $architecture -output $thin
            if ($LASTEXITCODE -ne 0) { throw "Architektur konnte nicht reduziert werden: $($library.Name)." }
            Move-Item -LiteralPath $thin -Destination $library.FullName -Force
            & codesign --force --sign - $library.FullName
            if ($LASTEXITCODE -ne 0) { throw "Ad-hoc-Signatur fehlgeschlagen: $($library.Name)." }
        }
    }
}
Copy-Item -LiteralPath (Join-Path $repository 'THIRD-PARTY-NOTICES.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $repository 'licenses') -Destination $output -Recurse
# Preserve notices supplied by the exact restored dependency versions as well.
$assets = Get-Content (Join-Path $repository 'MovieScrap2000/obj/project.assets.json') -Raw | ConvertFrom-Json
foreach ($library in $assets.libraries.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { continue }
    foreach ($packageRoot in $assets.packageFolders.PSObject.Properties.Name) {
        $package = Join-Path $packageRoot $library.Value.path
        if (!(Test-Path -LiteralPath $package)) { continue }
        Get-ChildItem -LiteralPath $package -File | Where-Object Name -Match '^(LICENSE|LICENCE|COPYING|THIRD.PARTY.NOTICES)' | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $output ('licenses/' + $library.Name.Replace('/', '-') + '-' + $_.Name))
        }
    }
}

$files = Get-ChildItem -LiteralPath $output -File -Recurse
$forbidden = $files | Where-Object {
    $_.Extension -in @('.pdb','.dbg','.debug','.mdb','.lib','.exp','.a','.pdn','.nupkg','.nuspec','.cs','.csproj','.sln','.slnx') -or
    $_.FullName -match '\.dSYM[\\/]' -or $_.Name -match 'Avalonia\.Diagnostics|testhost|xunit|Microsoft\.VisualStudio\.TestPlatform' -or
    ($Runtime.StartsWith('win-') -and ($_.Name -match '\.so($|\.)|\.dylib$|Avalonia\.(X11|Native|FreeDesktop)' )) -or
    ($Runtime.StartsWith('linux-') -and ($_.Name -match '\.dylib$|Avalonia\.(Win32|Native)|^av_libglesv2\.dll$|^libSkiaSharp\.dll$|^e_sqlite3\.dll$')) -or
    ($Runtime.StartsWith('osx-') -and ($_.Name -match '\.so($|\.)|Avalonia\.(Win32|X11|FreeDesktop)|^av_libglesv2\.dll$|^libSkiaSharp\.dll$|^e_sqlite3\.dll$')) -or
    ($_.FullName -match '[\\/]runtimes[\\/](?<rid>[^\\/]+)' -and $Matches.rid -ne $Runtime)
}
if ($forbidden) { throw ('Unerwartete Release-Dateien: ' + ($forbidden.Name -join ', ')) }
$bytes = ($files | Measure-Object Length -Sum).Sum
if ($Runtime.StartsWith('win-')) {
    $archive = Join-Path $run "$packageName.zip"
    [IO.Compression.ZipFile]::CreateFromDirectory($output, $archive, [IO.Compression.CompressionLevel]::SmallestSize, $true)
} else {
    $archive = Join-Path $run "$packageName.tar.gz"
    $archiveStream = [IO.File]::Create($archive)
    $gzip = [IO.Compression.GZipStream]::new($archiveStream, [IO.Compression.CompressionLevel]::SmallestSize)
    try { [System.Formats.Tar.TarFile]::CreateFromDirectory($output, $gzip, $true) }
    finally { $gzip.Dispose(); $archiveStream.Dispose() }
}
$downloadBytes = (Get-Item -LiteralPath $archive).Length
$checksum = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($archive + '.sha256', "$checksum  $(Split-Path -Leaf $archive)" + [Environment]::NewLine)
$manifest = [ordered]@{
    version = $version
    runtime = $Runtime
    selfContained = !$FrameworkDependent
    files = $files.Count
    installedBytes = $bytes
    downloadBytes = $downloadBytes
    archive = Split-Path -Leaf $archive
    sha256 = $checksum
}
$manifest | ConvertTo-Json | Set-Content (Join-Path $run "$packageName.manifest.json")
Write-Host ("Geprüft: {0}, {1:N1} MiB entpackt, {2:N1} MiB Download" -f $Runtime, ($bytes / 1MB), ((Get-Item $archive).Length / 1MB))
Write-Host $archive
