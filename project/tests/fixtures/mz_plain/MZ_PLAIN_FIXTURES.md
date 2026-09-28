# MZ-Fixture `mz_plain` — ein Spiel ohne Plugins

**Herkunft.** `CamelliaCoronation-Win`, aus `E:/RPGMakerGames`. Ein
frei verfügbares RPG Maker MZ-Spiel, vom Nutzer zum Arbeiten bereitgestellt.
Engine: **RPG Maker MZ 1.9.1** — gemessen, nicht angenommen: die
`commandNNN`-Methoden in `js/rmmz_objects.js` sind **114**, und es sind
**genau dieselben 114** wie in der Stranded-with-You-Engine, ohne ein einziges
davon nur hier oder nur dort.

**Warum diese Fixture existiert.** Die erste MZ-Fixture
(`mz/`, *Stranded with You*) trägt **52 aktive Plugins**, und **elf ihrer
Befehle sind Pluginaufrufe** — `PluginManager.callCommand`. Damit prüft eine
Fixture vor allem die Verweigerungsregeln und kaum den Eventcode selbst. Diese
Fixture ist das Gegenteil: **ein Plugin**, das eine leere Parameterliste hat
und in keinem einzigen Befehl vorkommt, und **kein `355` und kein `357` auf
irgendeiner der 20 Karten**.

**Was gemessen wurde, nicht behauptet:**

- **2 432 Befehle** über 20 Karten, **1 208 davon** mit einer Zahl ungleich 0.
- **Kein `355` und kein `357` irgendwo.** Das ist der Punkt der Karte, und es
  ist am Dateibestand abgezählt, nicht an einer Erwartung.
- **Kein Switch, keine Common-Event-Aufrufe, keine Actor-Referenzen** in den
  Eventlisten.
- **15 Variablen** (höchste Nummer 15), **8 Items** (höchste Nummer 8), **eine
  Klasse, eine Animation, null Actors** — alles so klein, dass ein Leser den
  ganzen Datenbestand kennen kann, statt einen Teil davon zu raten.
- **`CommonEvents.json` ist 376 Byte** und steht damit als echte Datei da. Bei
  der ersten Fixture fehlte sie, weil das Original 4,5 MB groß war; die Lücke
  zwang den Runner, verweigerte Common Events zu melden. Hier ist sie
  vorhanden, und die Regel wird gegen Daten geprüft statt gegen eine Lücke.
- Die Codes **0, 401, 404, 405, 412 und 505** sind **echte MZ-Sonderbefehle**
  (Blockende, Text, Wahl, Ende, Zweigende, Wegliste) und keine Plugins. Sie
  haben keine eigene `commandNNN`-Methode, und genau darum führt der Leser sie
  gesondert.

**Was diese Fixture nicht enthält** und warum:

- **Kein `js/`-Verzeichnis, keine `.exe`, keine `.dll`, kein `nwjs`.** Die
  Engine ist 1.9.1 und steht in der ersten Fixture nachgewiesen; eine zweite
  Kopie ausführbaren Codes im Repository wäre Ballast.
- **Keine Bilder, keine Audiodateien, keine Tilesets** (`Tilesets.json` ist im
  Original 247 KB reines Base64). Der Leser lädt keine Textur, also ist eine
  Textur in der Fixture eine Behauptung über etwas, das nichts liest.
- **`Skills.json` ist von 104 525 auf 1 181 Byte reduziert** — 236 leere
  Einträge statt 236 Skilltexte. **Kein einziger Befehl der 20 Karten
  referenziert eine Skill, und der Leser liest keine.** Das ist die einzige
  Datei, die nicht byteweise das Original ist, und sie ist es mit Absicht und
  mit der Begründung hier. Wer sie später braucht, holt sie sich wieder —
  und das ist richtig, denn dann ist es eine Aussage mit Beleg statt einer
  Behauptung.

**Alles andere ist byteweise identisch.** Die SHA-256-Werte unten sind über
die Dateien in `data/` dieses Verzeichnisses und lassen sich gegen die Quelle
prüfen.

| Datei | Bytes im Original | Bytes in der Fixture | gleich |
|---|---:|---:|:---:|
| `Actors.json` | 2,031 | 2,031 | ja |
| `Armors.json` | 23,745 | 23,745 | ja |
| `Classes.json` | 30,473 | 30,473 | ja |
| `CommonEvents.json` | 376 | 376 | ja |
| `Enemies.json` | 5,202 | 5,202 | ja |
| `Items.json` | 11,492 | 11,492 | ja |
| `Map001.json` | 11,359 | 11,359 | ja |
| `Map002.json` | 13,300 | 13,300 | ja |
| `Map003.json` | 49,696 | 49,696 | ja |
| `Map004.json` | 29,790 | 29,790 | ja |
| `Map005.json` | 29,423 | 29,423 | ja |
| `Map006.json` | 28,707 | 28,707 | ja |
| `Map007.json` | 32,531 | 32,531 | ja |
| `Map008.json` | 6,162 | 6,162 | ja |
| `Map009.json` | 24,164 | 24,164 | ja |
| `Map010.json` | 26,196 | 26,196 | ja |
| `Map011.json` | 41,203 | 41,203 | ja |
| `Map012.json` | 18,926 | 18,926 | ja |
| `Map013.json` | 6,867 | 6,867 | ja |
| `Map014.json` | 505 | 505 | ja |
| `Map015.json` | 25,731 | 25,731 | ja |
| `Map016.json` | 25,604 | 25,604 | ja |
| `Map017.json` | 37,436 | 37,436 | ja |
| `Map018.json` | 7,458 | 7,458 | ja |
| `Map019.json` | 11,390 | 11,390 | ja |
| `MapInfos.json` | 2,098 | 2,098 | ja |
| `Skills.json` | 104,525 | 1,181 | **nein** |
| `States.json` | 13,812 | 13,812 | ja |
| `System.json` | 6,797 | 6,797 | ja |
| `Weapons.json` | 13,849 | 13,849 | ja |
