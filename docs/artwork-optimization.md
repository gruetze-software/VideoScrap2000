# Artwork optimization

In **Settings → Artwork optimization**, choose a profile for newly selected artwork when saving:

| Profile | Poster limit | Fanart / landscape / episode-image limit | JPEG quality |
| --- | --- | --- | --- |
| Original (default) | Unchanged | Unchanged | Original bytes |
| Compact | 600 × 900 | 1280 × 720 | 85 |
| Balanced | 1000 × 1500 | 1920 × 1080 | 88 |

Banner and keyart also use the landscape bounds. Images retain their aspect ratio and are never enlarged. Optimization converts decoded pixels to sRGB, applies EXIF orientation, and writes a baseline JPEG without copying the original EXIF/XMP/IPTC metadata. Transparent artwork, clearlogos, clearart and discart remain unchanged. There is no additional image-processing package: the application uses its existing Skia library.

This is an optional compatibility workaround. A smaller, re-encoded poster displayed correctly in a user's Kodi/Fire-TV test, but that does not establish a universal cause or guarantee a fix on every device.

## Existing artwork

Choose **Optimize existing artwork …** in the same Settings section. Select a folder, choose Compact or Balanced, and create a preview. The tool lists recognized artwork under that folder and its subfolders, compares file sizes, and shows before/after previews. It ignores hidden backup folders, actor photos, unrelated images and symbolic-link directories.

Review the list, then explicitly choose **Save optimized images**. Cancellation stops further work; already completed images stay optimized and are reported. Files changed since the preview are rejected instead of silently overwritten. A metadata-only edit in the normal movie editor does not optimize or rewrite existing artwork.

Every distinct original is kept under `.artwork-originals` beside the artwork, named with its original stem and content hash. This includes an existing image replaced by a new download. Backups consume additional disk space and are not automatically removed. To restore one, copy the desired original back beside the video using its original artwork name and extension. Existing replacement `.bak` behavior also remains available.

Output is written completely to a unique temporary file in the destination folder before replacing the artwork. Filesystem and NAS rename guarantees vary. No Kodi database or cache is modified. Kodi may continue showing a cached image until it is selected or refreshed again; updating NFO metadata alone does not guarantee artwork cache invalidation.
