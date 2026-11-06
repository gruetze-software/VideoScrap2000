param([Parameter(Mandatory)][string]$PublishDirectory, [Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../artifacts/snap-payload'
if (Test-Path -LiteralPath $root) { throw 'Use a clean checkout to prepare the Snap payload.' }
$app = Join-Path $root 'opt/videoscrap2000'
New-Item -ItemType Directory -Path $app, "$root/bin", "$root/usr/share/applications", "$root/usr/share/icons/hicolor/256x256/apps" -Force | Out-Null
Copy-Item -Path "$PublishDirectory/*" -Destination $app -Recurse
[IO.File]::WriteAllText((Join-Path $app 'VERSION'), "$Version`n")
$launcher = @'
#!/bin/sh
export LD_LIBRARY_PATH="$SNAP/opt/videoscrap2000:$SNAP/usr/lib/x86_64-linux-gnu${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec "$SNAP/opt/videoscrap2000/VideoScrap2000" "$@"
'@
[IO.File]::WriteAllText((Join-Path $root 'bin/videoscrap2000'), $launcher + "`n")
Copy-Item -LiteralPath "$PSScriptRoot/../branding/linux/de.gruetze_software.VideoScrap2000.desktop" -Destination "$root/usr/share/applications"
Copy-Item -LiteralPath "$PSScriptRoot/../MovieScrap2000/Assets/videoscrap-2000.png" -Destination "$root/usr/share/icons/hicolor/256x256/apps/de.gruetze_software.VideoScrap2000.png"
& chmod 755 "$root/bin/videoscrap2000" "$app/VideoScrap2000"
if ($LASTEXITCODE -ne 0) { throw 'Cannot set executable permissions.' }
