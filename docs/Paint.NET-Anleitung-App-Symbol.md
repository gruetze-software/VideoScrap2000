# VideoScrap 2000 in Paint.NET nachzeichnen

Die Anleitung baut das aktuelle App-Symbol mit getrennten Ebenen nach. Als Arbeitsgröße verwenden wir **1024 × 1024 Pixel**. Die Formen und Farben orientieren sich an der vorhandenen Vorlage. Die Maße sind eine nachzeichnbare Konstruktion und leicht gerundet.

Die Vorlage enthält eine wolkige Textur. Für die neue Fassung ist ein glatter petrolfarbener Hintergrund mit einem dezenten Verlauf vorgesehen. Das lässt sich sauber bearbeiten und funktioniert auch als kleines App-Symbol.

![Vorhandene Vorlage](VideoScrap-2000-icon.png)

## 1. Arbeitsdatei vorbereiten

1. In Paint.NET ein neues Bild mit **1024 × 1024 Pixeln** anlegen.
2. Die weiße Hintergrundebene vollständig auswählen und ihren Inhalt mit **Entf** löschen. Das Schachbrett zeigt den transparenten Hintergrund.
3. Die Ebene **Grundfläche** nennen.
4. Darüber zwei leere Ebenen anlegen, zuerst **Filmrahmen**, dann **Play**.
5. Als **VideoScrap-2000-icon-redraw.pdn** speichern.

Die Ebenen von oben nach unten sind damit Play, Filmrahmen und Grundfläche. Über die Ebeneneigenschaften lässt sich später auch ihre Deckkraft einstellen. [Paint.NET-Dokumentation zu Ebenen](https://paint.net/doc/latest/WorkingWithLayers.html)

## 2. Farben einstellen

Diese Farben wurden aus der aktuellen PNG-Vorlage abgeleitet. Für die gefüllten Formen jeweils volle Deckkraft verwenden.

| Bestandteil | Hex-Farbe |
| --- | --- |
| Petrol-Grundfarbe | `#1A6662` |
| Heller Verlaufspunkt, optional | `#237F79` |
| Dunkler Verlaufspunkt, optional | `#12504D` |
| Filmrahmen, warmes Weiß | `#FDFBF7` |
| Play-Dreieck, Mint | `#69F7AF` |

Im Farbenfenster die erweiterte Ansicht öffnen und den jeweiligen Hex-Wert eingeben. Die Transparenz der Farben auf **255** setzen. [Paint.NET-Farbenfenster](https://paint.net/doc/latest/ColorsWindow.html)

## 3. Petrolfarbene Grundfläche zeichnen

**Maße genau eingeben**

Die numerischen Felder für Breite und Höhe befinden sich beim Werkzeug **Rechteckige Auswahl**. In dessen oberer Werkzeugleiste den Größenmodus auf **Feste Größe** stellen und als Einheit **Pixel** wählen. Dann beispielsweise Breite **864** und Höhe **864** eingeben. Diese Auswahl kann anschließend positioniert werden. Für rechteckige Ausschnitte wie das mittlere Fenster und die Film-Löcher ist das die direkte Methode.

Die Größenfelder gehören zur Auswahl. Beim Formenwerkzeug werden Formen über ihre Kontrollpunkte angepasst. Für das abgerundete Rechteck deshalb eine maßhaltige Auswahl als Begrenzung verwenden und die Form innerhalb dieses Bereichs ausrichten. Die Auswahlposition lässt sich in der Statusleiste ablesen. [Paint.NET-Dokumentation zur festen Auswahlgröße](https://paint.net/doc/latest/RectangleSelectionTool.html)

1. Die Ebene **Grundfläche** auswählen.
2. Im Formenwerkzeug **Abgerundetes Rechteck** wählen.
3. Als Zeichenmodus **Gefüllte Form** einstellen und die Kantenglättung einschalten.
4. Mit der Petrol-Grundfarbe ein Rechteck ab Position **X 80 / Y 80** zeichnen. Zielgröße **864 × 864 Pixel**, Eckenradius ungefähr **160 Pixel**.
5. Die Form mit **Enter** abschließen.

Das Formenwerkzeug bietet Füllung, Umriss, Kantenglättung und einen einstellbaren Eckenradius. Während der Bearbeitung kann die Form verschoben und skaliert werden. [Paint.NET-Formenwerkzeug](https://paint.net/doc/latest/ShapeTools.html)

Für einen dezenten Verlauf die gefüllte Fläche mit dem Zauberstab auswählen. Primärfarbe auf das hellere Petrol und Sekundärfarbe auf das dunklere Petrol setzen. Mit dem Verlaufswerkzeug diagonal von links oben nach rechts unten ziehen. Die Auswahl danach aufheben. [Paint.NET-Verlaufswerkzeug](https://paint.net/doc/latest/GradientTool.html)

## 4. Weißen Filmrahmen aufbauen

Die Ebene **Filmrahmen** auswählen. Zunächst ein gefülltes, weißes, abgerundetes Rechteck zeichnen.

| Eigenschaft | Zielwert |
| --- | --- |
| Linke obere Ecke | X 164 / Y 244 |
| Breite | 696 Pixel |
| Höhe | 544 Pixel |
| Eckenradius | etwa 24 Pixel |
| Farbe | `#FDFBF7` |

Danach die Mitte und die acht Löcher aus dieser Ebene ausschneiden. So scheint die Grundfläche einschließlich ihres Verlaufs hindurch.

**Mittleres Fenster**

Mit der rechteckigen Auswahl ein Fenster ab **X 296 / Y 292** mit **432 × 444 Pixeln** markieren. **Entf** drücken und anschließend die Auswahl aufheben.

**Seitliche Löcher**

Für jedes Loch eine rechteckige Auswahl von **64 × 64 Pixeln** erstellen und mit **Entf** ausschneiden.

| Loch | Linke Seite, X / Y | Rechte Seite, X / Y |
| --- | --- | --- |
| 1 | 196 / 300 | 764 / 300 |
| 2 | 196 / 424 | 764 / 424 |
| 3 | 196 / 548 | 764 / 548 |
| 4 | 196 / 672 | 764 / 672 |

Im Werkzeug für rechteckige Auswahl kann eine feste Auswahlgröße eingestellt werden. Für die Löcher einmal **64 × 64** wählen und die Auswahl jeweils an die Zielposition setzen. Die Statusleiste hilft beim Prüfen von Position und Größe. [Paint.NET-Werkzeugleiste](https://paint.net/doc/latest/Toolbar.html)

Beim Ausschneiden bleibt ausschließlich die Ebene **Filmrahmen** aktiv.

## 5. Das Play-Dreieck zeichnen

1. Die Ebene **Play** auswählen und die Primärfarbe auf Mint **#69F7AF** setzen.
2. Im **Formenwerkzeug** die fertige Form **Dreieck** auswählen. Den Zeichenmodus auf **Gefüllte Form** stellen und die Kantenglättung einschalten.
3. Ein Dreieck ungefähr in der Mitte aufziehen. Die Form zunächst im Bearbeitungsmodus lassen.
4. Mit gedrückter **rechter Maustaste** drehen. Dabei zusätzlich **Umschalt** halten, damit die Drehung in 15-Grad-Schritten einrastet. Die Spitze nach rechts drehen, entsprechend einer Vierteldrehung bei einem zunächst nach oben zeigenden Dreieck.
5. Über die Kontrollpunkte die Größe anpassen. Nach der Drehung ist die Zielgröße ungefähr **228 Pixel breit und 272 Pixel hoch**.
6. Mit gedrückter linker Maustaste im Inneren der Form verschieben. Die senkrechte linke Kante liegt ungefähr bei **X 416**, die obere Ecke bei **Y 388**. Die Spitze liegt dann etwa bei **X 644 / Y 524**.
7. Erst wenn Größe und Position passen, die Form mit **Enter** abschließen.

Für das Positionieren der noch aktiven Form helfen die Pfeiltasten. Sie verschieben um einen Pixel, zusammen mit Strg um zehn Pixel. Das Dreieck sitzt leicht rechts von der Mitte des Filmfensters. [Paint.NET-Formenwerkzeug](https://paint.net/doc/latest/ShapeTools.html)

## 6. Prüfen und exportieren

Die PDN-Datei mit ihren drei Ebenen speichern. Anschließend PNG-Kopien exportieren.

| Datei | Zweck |
| --- | --- |
| `VideoScrap-2000-icon-redraw.pdn` | Bearbeitbare Originaldatei mit Ebenen |
| `VideoScrap-2000-icon-redraw.png`, 1024 × 1024 | Hochauflösende Vorlage für die Release-Grafiken |
| `videoscrap-2000.png`, 256 × 256 | App-Symbol in der aktuell verwendeten Größe |

Beim Export die transparenten Außenbereiche erhalten. Nach dem Verkleinern zusätzlich bei **64 × 64**, **32 × 32** und **16 × 16 Pixeln** prüfen, ob Filmrahmen und Play-Dreieck noch erkennbar sind. Die Vorschau jeweils in tatsächlicher Größe auf hellem und dunklem Hintergrund ansehen.

Für das Windows-Release wird aus der fertigen PNG eine **ICO-Datei mit mehreren Auflösungen** erzeugt, einschließlich 16, 32, 48 und 256 Pixeln.

## Dateien im Projekt

| Vorhandene Datei | Verwendung |
| --- | --- |
| [VideoScrap-2000-icon.png](VideoScrap-2000-icon.png) | Fertige hochauflösende Zeichnung, 1024 × 1024 Pixel |
| [VideoScrap-2000-icon.pdn](VideoScrap-2000-icon.pdn) | Bearbeitbare Zeichnung mit Paint.NET-Ebenen |
| [videoscrap-2000.png](../MovieScrap2000/Assets/videoscrap-2000.png) | Aktuell eingebundenes App-Symbol, 256 × 256 Pixel |
| [videoscrap-2000.ico](../MovieScrap2000/Assets/videoscrap-2000.ico) | Aktuell eingebundenes Windows-Symbol |

Die neue Zeichnung zuerst unter dem Zusatz **redraw** ablegen. Die finale PNG und die daraus erzeugte ICO-Datei werden anschließend für das Release in die Anwendung übernommen.
