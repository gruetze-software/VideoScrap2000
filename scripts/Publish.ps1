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
dotnet publish $project -c Release -r $Runtime --self-contained $selfContained -o $output -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:UseSharedCompilation=false -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen.' }
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
    $_.Extension -in @('.pdb','.dbg','.debug','.mdb','.lib','.exp','.a') -or
    $_.FullName -match '\.dSYM[\\/]' -or $_.Name -match 'Avalonia\.Diagnostics|testhost|xunit|Microsoft\.VisualStudio\.TestPlatform' -or
    ($Runtime.StartsWith('win-') -and ($_.Name -match '\.so($|\.)|\.dylib$|Avalonia\.(X11|Native|FreeDesktop)' )) -or
    ($Runtime.StartsWith('linux-') -and ($_.Name -match '\.dylib$|Avalonia\.(Win32|Native)|^av_libglesv2\.dll$|^libSkiaSharp\.dll$|^e_sqlite3\.dll$')) -or
    ($Runtime.StartsWith('osx-') -and ($_.Name -match '\.so($|\.)|Avalonia\.(Win32|X11|FreeDesktop)|^av_libglesv2\.dll$|^libSkiaSharp\.dll$|^e_sqlite3\.dll$')) -or
    ($_.FullName -match '[\\/]runtimes[\\/](?<rid>[^\\/]+)' -and $Matches.rid -ne $Runtime)
}
if ($forbidden) { throw ('Unerwartete Release-Dateien: ' + ($forbidden.Name -join ', ')) }
$bytes = ($files | Measure-Object Length -Sum).Sum
$manifest = [ordered]@{ runtime = $Runtime; selfContained = !$FrameworkDependent; files = $files.Count; bytes = $bytes }
$manifest | ConvertTo-Json | Set-Content (Join-Path $run 'manifest.json')
if ($Runtime.StartsWith('win-')) {
    $archive = Join-Path $run "VideoScrap2000-$Runtime.zip"
    Compress-Archive -Path $output -DestinationPath $archive
} else {
    $archive = Join-Path $run "VideoScrap2000-$Runtime.tar.gz"
    tar -czf $archive -C $run VideoScrap2000
    if ($LASTEXITCODE -ne 0) { throw 'Archivierung fehlgeschlagen.' }
}
Write-Host ("Geprüft: {0}, {1:N1} MiB entpackt, {2:N1} MiB Download" -f $Runtime, ($bytes / 1MB), ((Get-Item $archive).Length / 1MB))
Write-Host $archive
