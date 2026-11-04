# VideoScrap 2000

<img src="MovieScrap2000/Assets/videoscrap-2000.png" alt="VideoScrap 2000" width="88" />

Schlanker Filmmanager von Grütze-Software für lokale Sammlungen und Netzlaufwerke. VideoScrap 2000 bearbeitet Metadaten und Artwork direkt im jeweiligen Filmordner. Die App nutzt .NET 10 und Avalonia und unterstützt Deutsch und Englisch sowie helles, dunkles und systemabhängiges Design.

## Erste Schritte

1. In **Einstellungen** den Filmordner auswählen. Lege jeden Film in einen eigenen Unterordner. Enthält ein Ordner mehrere unterstützte Videos, verwendet die App die größte Videodatei.
2. Oben **Einlesen** wählen. Vorhandene NFOs werden gelesen. Filme ohne Metadaten stehen als **Neu** oben. Die Liste lässt sich nach Name, Erscheinungsjahr oder Erstellungsdatum der NFO sortieren.
3. Über **Metadaten suchen** den passenden Film zuordnen. Bei der ersten Zuordnung oder ohne vorhandenes Artwork startet auch die Bildsuche.
4. Metadaten, Darsteller und Bilder prüfen und **Änderungen speichern**. Die App schreibt die NFO und ausgewählte Bilder in den Filmordner und sichert vorhandene Dateien als `.bak`.

Die Artwork-Suche zeigt Treffer in der bevorzugten Sprache zuerst. Mit **Alle** übernimmt sie den ersten passenden Treffer pro Typ. Optional erscheint Fanart dezent hinter den Filmdetails. Ein Doppelklick auf ein Bild öffnet es in der Standard-App. Nicht erreichbare Filme sind rot markiert und lassen sich per Rechtsklick aus dem lokalen Cache entfernen.

Die Serienverwaltung ist noch in Vorbereitung. Der Serienordner kann bereits gespeichert werden. Die App arbeitet mit lokalen NFOs und Bildern. Die Aktualisierung einer Kodi-Bibliothek erfolgt in Kodi.

## Online-Quellen

[<img src="docs/tmdb.svg" alt="TMDB" width="104" />](https://www.themoviedb.org/)

[TMDB](https://www.themoviedb.org/) liefert Filmmetadaten, Darsteller und Bilder. [Fanart.tv](https://fanart.tv/) ergänzt weitere Artwork-Typen. Für Fanart.tv einen [persönlichen API-Schlüssel](https://fanart.tv/get-an-api-key/) als Umgebungsvariable `FANART_TV_API_KEY` setzen. Starte die Anwendung danach neu.

> This product uses the TMDB API but is not endorsed or certified by TMDB.

Der ChatGPT-Button am Kurzinhalt kopiert einen Prompt in die Zwischenablage und öffnet ChatGPT im Browser. Du entscheidest dort, ob du den Text absendest.

## Entwickeln und veröffentlichen

Voraussetzung für den Build ist das .NET-10-SDK.

```powershell
dotnet run --project MovieScrap2000/MovieScrap2000.csproj
dotnet test VideoScrap2000.Tests/VideoScrap2000.Tests.csproj -m:1
./scripts/Publish.ps1 -Runtime win-x64
```

Weitere Release-Ziele sind `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64` und `osx-arm64`. Die Archive enthalten standardmäßig die passende .NET-Laufzeit und die nativen Bibliotheken des Zielsystems. `-FrameworkDependent` erstellt ein kleineres Paket für eine vorhandene .NET-10-Laufzeit. Das Skript prüft die Archive auf Debug-Symbole und Bibliotheken fremder Plattformen. Linux nutzt X11 oder XWayland. macOS-Archive enthalten ausführbare Verzeichnisse.

Die Oberfläche liegt in `MovieScrap2000`, der Anwendungskern in `VideoScrap2000.Core`. Die vorhandene SQLite-Datei im Anwendungsdatenordner `MovieScrap2000` wird weiterverwendet. Azure Pipelines und der GitHub-Workflow sind für Builds auf Windows, Linux und macOS vorbereitet.

[Icon-Vorlage für Paint.NET](docs/VideoScrap-2000-icon.png) · [Drittlizenzen](THIRD-PARTY-NOTICES.md) · [Grütze-Software](https://github.com/gruetze-software) · [Ko-fi](https://ko-fi.com/gruetzesoftware)
