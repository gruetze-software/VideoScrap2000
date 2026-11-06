# Linux Mint test package

The self-contained DEB packages include .NET 10 and add **VideoScrap 2000** to the application menu. No separate .NET installation is needed. Use **AMD64** for Linux Mint 21/22 on Intel/AMD 64-bit PCs; **ARM64** is available for Debian-based ARM64 systems. Both architectures are checked on Ubuntu 22.04/24.04, the bases of Mint 21/22. This is not a claim that every Debian-derived distribution has been tested.

Download the DEB directly from **GitHub Releases**, or download the **VideoScrap2000-Linux-Mint-DEB-amd64** test artifact from GitHub Actions and extract its ZIP. Double-click the `.deb` file and install it with Mint's package installer. Start **VideoScrap 2000** from the application menu under **Sound & Video**. When replacing a test package with the same version, choose reinstall.

Alternatively, open a terminal in the folder containing the DEB:

```sh
sudo apt install ./videoscrap2000_1.0.0_amd64.deb
videoscrap2000
```

Settings and library files remain in the normal user data folders. Removing the package does not remove your settings, movie/TV files, NFOs or artwork:

```sh
sudo apt remove videoscrap2000
```

The manual **Linux Mint DEB test package** workflow builds and validates the installers without creating a release or tag. Release builds also use this workflow and attach both DEBs to the release. [Flatpak and Snap workflows](linux-stores.md) are separate from DEB packaging and Store registration.
