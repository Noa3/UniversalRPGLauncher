# Projektaudit und Windows-Build

Stand: 2026-10-04. Auditbasis: `bda191f`, anschließend lokale, nicht committed Änderungen.

## Nachtrag 2026-10-05: Spielansicht und der Magenta-Fehler

**Der gemeldete pinke Bildschirm hatte eine gemessene Ursache.** `Rm2kBmp`
las jedes 8-Bit-BMP vertikal gespiegelt: `vonUnten = rohHoehe < 0`, während
der Windows-Bitmap-Vertrag das Gegenteil sagt (positive Höhe = erste
Datenzeile ist die unterste). Lisas zehn Chipsets und 25 CharSets sind
`.bmp`, also traf jeder Slot die gespiegelte Zelle: Der Boden-Slot 54 landete
auf einer Magenta-Marker-Fläche und der obere Slot 0 (99 % der Ebene, gedacht
als transparent) auf einer pinken Zelle, die das ganze Feld überdeckte. Mit
den echten Spieldaten reproduziert: untere Ebene `0x13BE`, obere `0x2710`;
bei korrekter Lesart ist Slot 0 Index-0-transparent. Fix plus
`tests/core/test_rm2k_bmp_orientation.cs`; die Vollsuite blieb mit
**2672/2672** grün. Der BMP-Pfad hatte vorher keinen einzigen Test.

**Die Spielansicht** (`app/ui/Rm2kGameScreen.cs`): Während eine RM2K-Laufzeit
läuft, verschwindet das Launcher-Layout und die Karte füllt das Fenster im
Letterbox-Verhältnis (`ComputeGameView`, getestet: Kamera-Offsets aus
`Rm2kMapCamera`, Fenster-/240-Skalierung, Integer-Scale-Option, kleine Karten).
Pro Frame wird nur der sichtbare 320×240-Ausschnitt kopiert und hochgeladen.
Nachrichten, Auswahlzeilen und Eingabe erscheinen im Spielbild; **F4** öffnet
das Pause-Menü mit Fortsetzen, Optionen, Cheats, Laufzeit beenden und Programm
schließen; Escape setzt fort, die Pause hält auch die Laufzeit an, der
Fenstertitel trägt während des Spiels `- F4: Pause`, und ein fehlendes
Kartenbild nennt die Renderdiagnose statt schwarz zu bleiben. Der
**Stop-Button ist aus dem Launcher entfernt**; beendet wird über das
Pause-Menü. Neue Locale-Schlüssel in `en.po`/`de.po`; UI-Suite auf 13 Tests
erweitert.

## Ergebnis

Ein lauffähiger Windows-x64-Entwicklungsbuild liegt unter
`build/windows/UniversalRPG.exe`. Das ist der UniversalRPG-Launcher mit dem
aktuellen nativen Backend, **kein fertiger, vollständig kompatibler Player für
beliebige RPG-Maker-Spiele**. Das gesamte Verzeichnis muss zusammenbleiben:
EXE, `UniversalRPG.pck` und `data_UniversalRPG_windows_x86_64/`.
Spiele und RTP-Archive wurden nicht mitgeliefert.

## Tatsächlich verifiziert

| Prüfung | Ergebnis | Grenze der Aussage |
| --- | --- | --- |
| `bash scripts/build_windows.sh` | Exit 0 | Führt kanonische Validierung, Releaseexport und Windows-Headless-Start aus. |
| Kanonische Godot-.NET-Suite | **2670/2670 Tests bestanden** | Kein Beweis eines vollständig durchspielbaren fremden Spiels. |
| Frischer C#-Rebuild | Exit 0, **0 Fehler, 135 Warnungen** | Warnungen wurden nicht deaktiviert oder als vollständig behoben dargestellt. |
| Windows-Releaseexport | Exit 0; EXE/PCK/.NET-Assembly vorhanden | Im Exportlog stehen zusätzlich Godot-`EditorSettings`-Diagnosen; der Export und die folgenden Starts waren erfolgreich. |
| Exportierte EXE, `--headless --quit-after 60` | Exit 0 | Beweist Start/Shutdown, keine visuelle Spielkompatibilität. |
| Exportierte EXE, `--write-movie ...png --quit-after 3` | Exit 0; gerenderte PNGs geprüft | Sichtbare Launcheroberfläche, Spieleliste, scrollbare Details und Start-/Stop-Schaltflächen. |
| Interaktive exportierte EXE | Native Ordnerauswahl geöffnet und bedient | `E:/RPGMakerGames` anschließend in `library.cfg` zurückgelesen; 11 Spiele auch im gerenderten Launcher sichtbar. |
| Daten-/Runtime-Audit von `E:/RPGMakerGames` | 11 Spiele erfasst | Ausschließlich eigener Code, keine fremden EXE/DLL/JS/Ruby-Dateien ausgeführt. |
| `git diff --check`, `bash -n scripts/build_windows.sh` | Exit 0 | Änderungen sind lokal; kein neuer Audit-Commit/Push. |

Die normale Hintergrund-Fensteraufnahme des Desktop-Treibers zeigte beim
OpenGL-Fenster nur die Client-Hintergrundfarbe. Das wurde **nicht** als Beweis
für eine leere/defekte UI gewertet: Die EXE erzeugte selbst echte gerenderte
Frames, und der Ordnerknopf öffnete nach Hintergrundklick nachweislich den
nativen Dialog. Visuelle Evidenz: `build/verification/windows-library00000002.png`.
Ein vollständiger interaktiver Gameplay-Durchlauf wurde nicht durchgeführt.

## Was funktioniert – und was nicht

| Bereich | Aktueller belegter Stand |
| --- | --- |
| Lokale Spielbibliothek | Ordnerscan, Enginehinweise, gespeicherte Metadaten, Supportentscheidung und explizite Enginewahl. |
| RM2000/2003 | LCF-Datenparser, begrenzter nativer Runtimepfad, Karten-/Event-/Simulationsbausteine und Präsentation. Start/180 Frames an drei echten Spielen geprüft. Keine vollständige Gameplay-Parität zugesagt. |
| RM2000/2003-Auswahl | Gemeinsame LCF-Signaturen bleiben ohne zusätzliche Evidenz mehrdeutig. Der Benutzer kann einen **bereits erkannten, unterstützten** Kandidaten auswählen; die Wahl wird gespeichert. Das verifiziert nicht die tatsächliche Generation eines Spiels. |
| Spielstände | Eigener begrenzter JSON-Savecodec und Hostslot-Roundtrips sind regressionstestgestützt. Kein Beleg für vollständige Original-`RPG_RT`-Savekompatibilität oder ein fertiges spielinternes Speichermenü. |
| XP / VX / VX Ace | Erkennung und Daten-/Parserbausteine; Live-Katalog bleibt `DetectionOnly`. Keine freigegebene vollständige RGSS/Ruby-Spielruntime. |
| MV / MZ | Erkennung, Metadaten und begrenzte native Daten-/Befehlsbausteine. Live-Katalog bleibt `DetectionOnly`; keine vollständige JavaScript-/Plugin-Spielruntime. |
| RTP | Vorhandener Prüf-/Consent-/Cacheablauf; ZIP-Entpackung abgesichert. Cachepfad für `System.IO` globalisiert, Fortschritt auf Hauptthread umgesetzt. In diesem Audit kein konkreter RTP-Download-End-to-End-Nachweis. |
| Fremde Plugins/Installer | Nicht ausgeführt. Keine EXE-/DLL-/Ruby-/JavaScript-Kompatibilität durch ungeprüfte Ausführung vorgetäuscht. |
| Windows-Auslieferung | Export samt .NET-Abhängigkeiten vorhanden und lokal gestartet. Kein Installer, keine Prüfung auf einem frischen zweiten Windows-System. |

## Echte Spiele

Ohne explizite Wahl: **3 mehrdeutige LCF-Spiele, 8 Detection-only-Spiele**.
Mit der jeweiligen ausdrücklichen RM2000- bzw. RM2003-Auswahl:

| Spiel | RM2000-Backend | RM2003-Backend |
| --- | --- | --- |
| Lisa, im Spiel als „Diary“ bezeichnet | Start erfolgreich, 180 Frames, kein Updatefehler | Start erfolgreich, 180 Frames, kein Updatefehler |
| Dragon Destiny | Start erfolgreich, 180 Frames, kein Updatefehler | Start erfolgreich, 180 Frames, kein Updatefehler |
| Pom Gets Wi-Fi | Start erfolgreich, 180 Frames, kein Updatefehler | Start erfolgreich, 180 Frames, kein Updatefehler |

Die Wahl beider Backends ist ein Start-/Robustheitstest des gemeinsamen
begrenzten Pfads, **kein Nachweis**, dass die Spiele gleichermaßen zu beiden
Generationen gehören oder alle Karten, Kämpfe, Dialoge und Assets korrekt laufen.

Weiterhin nicht startbar:
- MZ: Camillia's Coronation Report.
- MV: Fatal Fantasy, LegalTruck_v1.1.
- XP: Heartache 101 v2.5, MicroQuest: Beneath Brimestone.
- VX: Random Dungeon, The Princess and the Rose Knight.
- VX Ace: Dreaming Mary.

Rohberichte: `build/verification/real-games.json`, `real-games-rm2k.json`,
`real-games-rm2k3.json`. Der Auditrunner kopiert nur LCF-/INI-Daten in ein eigenes
temporäres Verzeichnis und begrenzt jeden Runtimeversuch auf 180 Frames.
Die Originalspiele und ihre Spielstände wurden dabei nicht verändert.

## Behobene Probleme

1. **ZIP-Pfadflucht:** Auch Verzeichniseinträge werden vor Erstellung geprüft;
   gleichnamige Pfadpräfix-Geschwister gelten nicht mehr als Inhalt des Zielordners.
2. **Archivüberschreiben/Links/Größen:** Bestehende Dateien und doppelte Einträge
   ersetzen keine ersten Dateien; Unix-Symlinks und Reparse-Point-Pfade werden
   verweigert. Grenzen: 10000 Einträge, 256 MiB je Datei, 2 GiB insgesamt.
3. **Windows-Pfadaliases:** Gerätebezeichnungen und Segmente mit abschließendem
   Punkt/Leerzeichen werden nicht als normale Dateien akzeptiert.
4. **UI-Thread:** RTP-Worker schreiben Fortschritt in eine threadsichere Queue;
   Godot-Controls werden nur im Hauptthread aktualisiert.
5. **Cachepfad:** `user://` wird vor `System.IO`-Zugriff in einen absoluten Pfad
   aufgelöst.
6. **Nachrichtenfortsetzung:** „Weiter“ bestätigt auch den Runtime-Wartezustand,
   statt nur den sichtbaren Text zu entfernen.
7. **Lebenszyklus:** Szenenaustritt beendet den aktiven Host; Start-Reentry und
   Schließen während eines RTP-Ablaufs sind abgesichert.
8. **Save-I/O:** Ein ungültiges/nicht beschreibbares Saveziel liefert einen
   Fehler über den Codec statt einer vorher ungeschützten Ausnahme.
9. **Supportprüfung:** Verwendet den tatsächlichen DetectionReport statt allein
   eines Legacy-Engineenums; Detection-only wird dadurch nicht startbar.
10. **LCF-Ambiguität:** Sichere, persistente Auswahl eines unterstützten erkannten
    Kandidaten in Library und Launcher; unbekannte/defekte Engines werden nicht
    über einen Override legitimiert.
11. **Layout:** Lange Details scrollen; Start/Stop bleiben außerhalb des
    Scrollbereichs. Inaktive Karten-/MZ-Vorschauen belegen keinen Leerraum.
12. **Exportziel:** Rootverzeichnis `build/windows` statt `project/build/windows`;
    reproduzierbares Buildskript ergänzt.

Neue/geänderte Regressionen: `test_rtp_archive_safety.cs`,
`test_launcher_ui_safety.cs`, `TestGameLibraryIntegration.cs`. Die relevanten
Fehler wurden vor den Fixes mit fehlschlagenden Tests reproduziert.

## Offen / Risiken

- Keine Zusage vollständiger Spielbarkeit, vollständiger Event-/Kampf-/Renderer-
  Parität oder aller Asset-/Audio-/Plugin-/Speicherpfade für reale Spiele.
- 135 bestehende Compilerwarnungen, überwiegend Nullable-/Testdiagnosen.
- Die Vollsuite meldet weiterhin 3 geleakte CanvasItem-RIDs und 6 ObjectDB-
  Instanzen beim Beenden. Bereits die Baseline meldete 3 RIDs und 7 Instanzen.
  Der getestete exportierte Launcherstart/-shutdown meldete keine solchen Fehler.
- RTP-Netzwerkdownload/-Installation wurde nicht als vollständiger erfolgreicher
  Ende-zu-Ende-Ablauf bestätigt; keine stille Beschaffung.
- Native Screenshotaufnahme ist kein zuverlässiger Pixelnachweis für dieses
  Hintergrund-OpenGL-Fenster; direkt gerenderte Exportframes dienen als Evidenz.
- `qa_patches/` wurde nicht verändert. Keine fremden Installer oder Spielprogramme
  wurden ausgeführt. Keine Versionsgeschichte umgeschrieben, kein Commit/Push.

## Benutzung und Wiederholung

1. `build/windows/UniversalRPG.exe` starten; alle Nachbardateien beibehalten.
2. Spieleordner wählen, zum Beispiel `E:\RPGMakerGames`.
3. Spiel auswählen. Bei mehrdeutigem RM2000/2003-Eintrag die Engineauswahl im
   scrollbaren Detailbereich auf die bekannte Generation setzen.
4. Detection-only-Spiele bleiben gesperrt. Ein erfolgreicher Start ist kein
   Versprechen vollständiger Kompatibilität.

Aus Git Bash im Repository: `bash scripts/build_windows.sh`.
Voraussetzungen: .NET-SDK, Godot 4.7.2 .NET und dessen passende Mono-
Exporttemplates. `GODOT_BIN` kann den Editorpfad explizit vorgeben.
Logs liegen anschließend unter `build/verification/`.
