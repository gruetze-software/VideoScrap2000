param(
    [Parameter(Mandatory)][string]$Archive,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$template = Join-Path $repository 'packaging/windows-store'
[xml]$project = Get-Content -LiteralPath (Join-Path $repository 'MovieScrap2000/MovieScrap2000.csproj') -Raw
$version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
$source = Get-Item -LiteralPath $Archive
if ($source.Name -notmatch ('^VideoScrap2000-' + [regex]::Escape($version) + '-win-(x64|arm64)\.zip$')) {
    throw "Expected a self-contained Windows $version archive, got $($source.Name)."
}
$architecture = $Matches[1]
$sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$makeAppx = Get-ChildItem -LiteralPath $sdkRoot -Directory |
    Where-Object { $_.Name -as [version] } | Sort-Object { [version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x64/makeappx.exe' } |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (!$makeAppx) { throw 'MakeAppx from the Windows SDK is required.' }

$layoutRoot = New-Item -ItemType Directory -Path (Join-Path $repository 'artifacts/msix-layouts') -Force
$layout = New-Item -ItemType Directory -Path (Join-Path $layoutRoot.FullName ([guid]::NewGuid().ToString('N')))
try {
    Expand-Archive -LiteralPath $source.FullName -DestinationPath $layout.FullName
    Copy-Item -LiteralPath (Join-Path $template 'Assets') -Destination $layout.FullName -Recurse
    [xml]$manifest = Get-Content -LiteralPath (Join-Path $template 'AppxManifest.xml') -Raw
    $manifest.Package.Identity.Version = "$version.0"
    $manifest.Package.Identity.ProcessorArchitecture = $architecture
    $manifest.Save((Join-Path $layout.FullName 'AppxManifest.xml'))

    $output = New-Item -ItemType Directory -Path $OutputDirectory -Force
    $package = Join-Path $output.FullName "VideoScrap2000-$version-$architecture-store.msix"
    & $makeAppx pack /d $layout.FullName /p $package /o
    if ($LASTEXITCODE -ne 0) { throw "Store MSIX packaging failed for $architecture." }
}
finally {
    if ((Resolve-Path -LiteralPath $layout.FullName).Path -eq $layout.FullName -and
        (Split-Path -Parent $layout.FullName) -eq $layoutRoot.FullName) {
        Remove-Item -LiteralPath $layout.FullName -Recurse -Force
    }
}
