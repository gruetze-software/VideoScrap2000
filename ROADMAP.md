# VideoScrap2000 – Zielbild

## Technische Priorität

- .NET 10 und Avalonia 12 für Windows, Linux und macOS.
- Plattformunabhängiger Kern für Sammlung, Scraper, NFO und SQLite; Avalonia-Oberfläche mit MVVM.
- WinForms, System.Drawing und feste Windows-Pfade aus dem gemeinsamen Code entfernen.
- Bestehende Funktionen und lokale Metadaten bei der Migration erhalten.

## Gestaltung ab der ersten Avalonia-Version

Referenz ist das lokale Projekt `../BingWallE/BingWallE`, insbesondere
`Views/MainWindow.axaml`, `Views/AboutWindow.axaml` und `Styles/Buttons.axaml`.

- Gleiche Gestaltungsfamilie: großzügige Abstände, abgerundete Flächen, klare Typografie und türkise Akzente.
- Filmoberfläche mit Posterübersicht, Suche und Filtern sowie übersichtlichen Details und Artwork-Auswahl.
- Hell, Dunkel und Systemeinstellung; Auswahl dauerhaft speichern und unmittelbar anwenden.
- Zentrale Theme-Ressourcen für Hauptfenster, Dialoge, Menüs, Lade-, Fehler- und Auswahlzustände.
- Grütze-Software-Symbol aus BingWallE: `Assets/gruetze-software.png`.
- Eigenes Anwendungssymbol unabhängig vom BingWallE-Produktsymbol behandeln.
- Info-Dialog mit Produktname, tatsächlicher Anwendungsversion, Grütze-Software-Symbol,
  Copyright, Projektlink, Technologieangaben und Lizenzhinweisen.
- Ko-fi-Schaltfläche wie in BingWallE: https://ko-fi.com/gruetzesoftware.
- Herstellerlink: https://github.com/gruetze-software.
- Projektlink erst mit verifizierter Repository-Adresse belegen.
- Tastaturbedienung, sichtbare Fokuszustände, lesbare Kontraste und skalierbare Dialoge.
- Hauptfenster und Dialoge in beiden Themes sowie bei hoher Anzeigeskalierung visuell prüfen.

## Releases

- Getrennte Publish-Ausgaben je Betriebssystem und Architektur; keine Sammelpakete mit fremden nativen Bibliotheken.
- Release-Downloads ohne Debug-Symbole, Avalonia-Diagnosewerkzeuge, Tests und Entwicklungsdateien.
- Nur geprüfte Publish-Ausgaben verpacken; Lizenzhinweise benötigter Abhängigkeiten erhalten.
- Eigenständig lauffähige Ausgaben als vorläufiger Standard; kleinere Ausgaben mit vorausgesetzter .NET-Laufzeit separat bewerten.
- Trimming und Native AOT erst nach Prüfung von XML-/JSON-Serialisierung, SQLite und Avalonia aktivieren.
- Paketinhalt, Größe und Startfähigkeit pro Zielsystem prüfen und dokumentieren.

## Reihenfolge

1. Plattformunabhängigen Kern herauslösen und auf .NET 10 umstellen.
2. Avalonia-12-Grundgerüst einschließlich Themes, Branding, Info und Ko-fi erstellen.
3. Filmübersicht, Details, Scraping und Artwork schrittweise übertragen.
4. Zielgerichtete Releases und Plattformprüfungen absichern.
5. Bibliotheksverwaltung, Statusfilter und sichere Stapelverarbeitung erweitern.

## Umgesetzt in 0.2.0

- WinForms-Ansichten entfernt; Avalonia 12.1.4 und plattformunabhängiger .NET-10-Kern.
- BingWallE-Gestaltungsfamilie, gespeicherte Themes, Hersteller-Symbol, Info und Ko-fi.
- Bestehenden SQLite-Cache weiterverwenden, Ordner auswählen, Scan abbrechen, Sammlung durchsuchen.
- Metadaten bearbeiten und suchen; lokale und Online-Artwork-Auswahl.
- NFO-Sicherungen, UTF-8 und Erhaltung unbekannter NFO-Felder; Scan ohne Änderungen an Filmdateien.
- Zielabhängige Avalonia-Pakete, geprüfte Release-Archive, Tests und CI-Konfigurationen.

Native Starttests auf Linux/macOS, signierte Installationspakete und eine erweiterte
Stapelverarbeitung sind noch offen.

## Weitere umgesetzte Bedienung

- Klar hervorgehobene Reiter für Metadaten, Darsteller, Details und Artwork.
- Vollständige deutsche und englische Oberfläche, automatische Client-Sprachwahl;
  getrennte optionale Sprachvorgaben für Metadaten und Artwork unter Erweitert.
- Titel- und Jahreserkennung aus Release-Dateinamen; Jahr als anpassbarer Suchfilter.
- Neue Filme grün und zuerst; normale Einträge ohne zusätzliche NFO-Statuszeile.
- Sofortige Online-Artwork-Suche mit gewähltem Typ, automatische Übernahme einzelner
  Treffer und Alle-Suche mit erstem Treffer je Typ aus TMDb und Fanart.tv.
- Einfacher Klick markiert Artwork und setzt den Suchtyp; Doppelklick öffnet das Original.
- Erweiterte Metadaten und Darstellerbearbeitung; technische Daten aus vorhandenen NFOs.
- Keine Kodi-Datenbankanbindung und keine YouTube-Trailer.
