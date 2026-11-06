# Linux Mint test package

The self-contained AMD64 DEB package includes .NET 10 and adds **VideoScrap 2000** to the application menu. No separate .NET installation is needed. It targets Linux Mint 21/22 on Intel/AMD 64-bit PCs. The build checks installation and startup on their Ubuntu 22.04/24.04 base systems; testing on Linux Mint itself is still needed.

Download the **VideoScrap2000-Linux-Mint-DEB-amd64** artifact from the **Linux Mint DEB test package** GitHub Actions run. Extract the downloaded ZIP, then double-click the `.deb` file and install it with Mint's package installer. Start **VideoScrap 2000** from the application menu under **Sound & Video**.

Alternatively, open a terminal in the folder containing the DEB:

```sh
sudo apt install ./videoscrap2000_1.0.0_amd64.deb
videoscrap2000
```

Settings and library files remain in the normal user data folders. Removing the package does not remove your settings, movie/TV files, NFOs or artwork:

```sh
sudo apt remove videoscrap2000
```

The manual **Linux Mint DEB test package** workflow builds and validates the installer without creating a release or tag. Flatpak and Snap packages are separate future work.
