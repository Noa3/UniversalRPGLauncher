## Active

**Und die Regel "Exception faengt alles" war ueberfluessig, und ich habe sie
gelo

**Und `sub` und `gsub` gab es als einen Satz, und sie sind zwei.** Gemessen
vorher: `"aXbXc".sub("X", "-")` antwortete `'a-b-c'` --
**und ein Leser, der `Replace` fuer beides nahm, macht aus einem Spiel, das
eine Marke aus einem Namen streicht, eines, das alle streicht.**
**Und `Replace` mit leerem Text tut in neueren Laufzeiten nichts mehr**,
**und darum hat die leere Ersetzung einen eigenen Weg mit `Remove`.**

**Und der Block war an keiner Stelle.** `gsub("X") { |t| t * 2 }` antwortete
`'abc'` -- **und nicht `'aXXbXXc'`** -- **und die leere Ersetzung war
nicht geraten, sie war gemessen.** `BrauchtBlock` nannte `sub` und `gsub`
nicht,
**und ein Block wird nur dann an den Aufruf gehaengt, wenn diese Liste ihn
nennt** -- **und `gsub` lief also mit leerem Ersatz und strich jedes X**,
**und ein Spiel, das seinen Gegaennamen in Grossbuchstaben schreibt,
haette leere Zeichen daraus gemacht und der Name waere weg.**

**Und drei der Fehler in diesem Batch waren Testfehler, und keiner davon
sah aus wie ein Testfehler:**

1. `"#{$1}"` ist eine andere Ruby-Sache als `$1`, **und der Leser hat sie
   nicht** -- **und mein Test behauptete, er habe sie.**
2. `"\1"` ist **der Oktalwert 1**, **gemessen: genau ein Byte `01`** --
   **und der Gruppenrueckverweis steht in `"\\1"`.** *Ein Backslash im
   Ruby-Text ist eine Oktalzahl und kein Backslash.*
3. `["a", "b"]` in einem Test prueft **eine Sammlung**, **und nicht zwei
   Umbenennungen** -- **und der Test lief in einen
   `IndexOutOfRangeException`, die aussah wie ein Leserfehler.**

**Und die Gruppenliste faengt bei 0 an, und die Ziffer im Ersatzerzeugnis
bei 1.** `"anna bob".gsub(/(\w+) (\w+)/, "\\2 \\1")` antwortete
`' bob'` -- **und das sieht wie ein Zeichenfehler aus und ist einer**,
**denn beide Gruppen waren vertauscht.**

**Und Muster aus Skripten laufen jetzt in derselben Schranke wie `=~`,
nachdem sie vorher alle abgelehnt wurden.** Die alte Regel
`Test_APatternFromAScriptIsNotRunAndItSaysSo` **war damals richtig und
ist es nicht mehr** -- **und ein Test, der eine Regel festschreibt, die
der Leser abschafft, muss mit der Regel gehen und nicht gegen sie.**
4097 Bytes werden abgelehnt, **und die Meldung nennt jetzt das Muster und
nicht nur die Laenge** -- **denn bei hundert Mustern in einem Spiel weiss
man dann nicht, welches zu gross war.**

        if (pMethode == "instance_variables")\n        {\n            var alleNamen = new List Gemessen: `M.instance_methods` sagte *M has no method
'instance_methods' on this host*, **und `Object.ancestors` sagte erst *the
constant Object is not defined by this host* und dann *nil has no method
'ancestors'*** -- **und beide Haelften waren falsch:** `Object` ist Teil
der Sprache, **und der Leser ist der, der kein `ancestors` hat.**
`instance_methods`, `include?`, `included_modules`, `ancestors`, `name`,
`superclass`, `to_s` und `is_a?` auf einem Modul **stehen jetzt da.**

**Und `include` hat die Methoden kopiert und das Modul vergessen.** Genau
das ist der Kernfehler, **und `include?` kann ohne Liste nicht antworten** --
**und `include?` ist die erste Zeile von fast jedem VX-Plugin.**

**Und `instance_methods(false)` und `instance_methods` waren vertauscht.**
Gemessen: `A.instance_methods` gab `[eigenes, geerbt]`? Nein: **`[geerbt]`
und `[eigenes]`** -- **die eigenen Namen standen nur in der Liste der
gesehenen und nie in der Antwort**, **und die Kette lief in den Zweig, der
nur die eigenen zurueckgibt.** `true` heisst die ganze Kette und `false`
nur dieser Typ, **und nil steht fuer `true`.**

**Und die Typen der Sprache sind jetzt echte Typen, und keine Namen.**
`Object`, `String`, `Module`, `Class`, `BasicObject`, `Kernel`, `Comparable`,
`Enumerable` und die Fehlerklassen **stehen im Konstruktor, bevor ein
Skript laeuft** -- **denn `class Held` sitzt unter `Object`, ob ein Host das
sagt oder nicht**, **und ein Leser, der erst beim ersten `include` nachsaehe,
haette `Object.ancestors` in einer Kette, die vorher nicht existierte.**
`DefinedTypes` **zaehlt nur noch, was ein Skript hinzugefuegt hat**,
**denn ein Host, der fragt was ein Skript definiert, will keine vierzig
Namen, die nicht aus dem Skript kommen.**

**Und `String.include?(Comparable)` ist wahr, weil Ruby es so macht.**
`String`, `Integer`, `Float`, `Numeric`, `Symbol` nehmen `Comparable`,
`Array`, `Hash` und `Range` nehmen `Enumerable`, **und jedes Objekt nimmt
`Kernel`** -- **und `modul` ist ein Schluesselwort in C#, der erste
Versuch hiess so und der Compiler sagte *the name 'module' does not exist
in the current context***, **und das sieht nach einem Tippfehler aus und ist
ein Schluesselwort.**

**Und der dritte Testfehler dieser Form: eine Liste, die bei jedem Lauf
geleert wird.** `Test_AMethodThisHostDoesNotHaveIsARefusal` macht drei
`Run`-Aufrufe **und liest am Ende eine gemeinsame Liste** -- **und `Run`
leert die Diagnosen, also zaehlt der Test genau den letzten.** Er behauptete
2 und es sind 4. **Nach `nil` statt eines Wertes und `printf` ist das die
dritte Form: *ein Test, der mehrere Dinge misst, braucht eine Liste, die
waechst.*** **Und der vierte: `Object.superclass` ist `BasicObject` und
erst dessen ist nil** -- **ich hatte `nil` behauptet, um die Kette zu
beenden, und damit `ancestors` um ein Glied gekuerzt.**

**Und die Fragen, die ein Objekt an sich selbst stellt, fehlten neun von
zehn, und eine davon schwieg.** `A.new.send(:gruessen)` sagte *A has no
method 'send' on this host* -- **und `__method__` antwortete `nil` ohne ein
Wort** -- **und das ist die schlimmste Form einer Ablehnung**, denn der
Aufrufer kann es nicht von einer Methode unterscheiden, die nichts zurueckgibt.

**Und `send` laeuft ueber dieselbe Suche wie ein geschriebener Name** -- **und
ein Leser, der den Namen selbst aufgeloest haette, waere an `super` und am
Empfaenger vorbeigelaufen**, **und ein Plugin, das eine Methode einer
Unterklasse durch `send` ruft, wuerde die der Basisklasse ausfuehren.**

**Und `1.send(:+, 2)` ist `1 + 2`**, **und ein Operator ist hier keine Methode
unter einem Namen** -- **und `OperatorName` ist eine Liste und keine
Ratswende**, **denn ein Leser, der jeden Namen als Operator probierte, wuerde
einem Spiel eine Zahl liefern, die es nie gerechnet hat.**

**Und `__method__` ohne Klammern ist ein Bezeichner und kein Aufruf.** Der
Leser sah nur in der Skripttabelle nach,
**und ein Leser, der das tat, fand eine lokale Variable namens
`__method__`** -- **und die ist nil.** *Genau an der Stelle, an der eine
Methode sagen muss, welche sie ist.*

**Und `object_id` wird gezaehlt, und nicht aus der Adresse gebildet** --
**eine Sammlung zwischen zwei Aufrufen wuerde dieselbe Zahl noch einmal
ausgeben**, **und `list.uniq` haette zwei verschiedene Helden in einem
Eintrag.**

**Und `instance_variables` nimmt kein Argument, und es ist die einzige dieser
fuenf Fragen, die keine Namen nimmt.** Der Leser pruefte bei allen fuenf
dasselbe Argument,
**und genau hier sagte er `nil` -- und genau hier steht die Frage in jedem
Plugin, das `@ivars` durchsucht.**

**Und vierter Testfehler dieser Form: mein Test behauptete `get(:mp)` sei
`10`.** Gemessen: **es ist `20`**, denn `:mp` ohne `@` bedeutet das Feld
`@mp` -- **und ein Test, der eine Erweiterung behauptet, muss auch die
richtige Zahl nennen.**

**Und `String * Integer` ist der Trennstrich zwischen zwei Fenstern.****Und `String * Integer` ist der Trennstrich zwischen zwei Fenstern.** `"-" *
30`,
**und ohne das antwortete der Leser *undefined operator '*' for a String
and a Integer*** -- **und die Meldung waere wieder ueber einen Operator
gewesen, den es gibt.**
escht, statt sie zu dokumentieren.** `RuntimeError -> StandardError ->
Exception` -- **die Elternkette enthaelt `Exception` von selbst**,
**und der Sonderzweig daneben hat nichts beigetragen.** Ein Sonderzweig,
der neben der allgemeinen Regel steht und dasselbe sagt,
**ist eine zweite Antwort auf dieselbe Frage**, **und die zweite ist
immer die, die man pflegt und die erste nicht.**

**Und `else` und `ensure` sind keine Arme, und das ist gemessen, und es ist
der Grund fuer den Sprung:** `begin; raise "x"; rescue; 5; else; 99; end`
gibt **5** zurueck und nicht 99 -- **weil `else` in derselben Knotenliste
steht wie die Arme, und die Arm-Schleife diese Liste ablaeuft.**
Ohne den Sprung waere es 99, **und `Test_ElseIsNotAnAnswerAndEnsureIsNotOneEither`
schliesst genau diese Luecke.**

**Und der Sprung selbst bleibt unbewiesen, und das ist gemessen und nicht
behauptet:** mit dem Sprung gibt `begin; raise "x"; rescue; 5; else; 99;
end` **5** zurueck, **und ohne den Sprung ebenfalls 5.**
`Test_ElseIsNotAnAnswerAndEnsureIsNotOneEither` deckt also den *Wert* ab,
**nicht den Sprung** -- **und die Mutation lebt aus genau diesem Grund
und wird hier als nicht-toetbar ausgewiesen statt als erledigt
gemeldet.**

**Der Unterschied waere nur sichtbar, wenn `else` selbst einen Wert
zurueckgaebe**, **und das tut es nicht**: `else` ist ein Weg, keine Antwort.
**Wer diese Regel in einem anderen Leser nachbaut, muss den Sprung also aus
der Bedeutung ableiten und nicht aus einem Test, der ihn trotzte.**

**Und `require` gab es nicht, und `Kernel#require` ist keine
Bequemlichkeit.** Jedes VX- und VX-Ace-Spiel verteilt seine Skripte auf
hundert Dateien und laedt sie der Reihe nach,
**und der Leser sagte in Zeile eins jedes Plugins *„self has no method
'require' on this host"*.**

Und das braucht **drei** Dinge, und **nur der Interpreter hat zwei davon**:
den Lexer, den Parser und sich selbst -- **und nur der Host hat Dateien.**

- **Und der Host liest, und nicht der Interpreter.** **Ein Leser, der
  selbst Dateien oeffnet, waere ein Programm, das ein Spiel laeuft und
  auch noch herumsuchen kann** -- **und das ist genau die Form, die dieses
  Projekt ueberall ablehnt.**
- **Und `ReadScript` ist eine Vorgabemethode, und keine Pflicht.** Sonst
  haette jede neue Faehigkeit jeden Host gebrochen, **und dann schreibt
  jeder Host eine leere Methode, die niemand liest** -- **gemessen: fuenf
  Hosts, von denen vier keine Dateien haben.**
- **Und `require` zweimal laedt einmal, und `load` immer.** Das ist der
  ganze Unterschied zwischen den beiden Woertern --
  **ein Leser, der die Datei immer laeuft, haette jede Klasse eines Spiels
  zweimal definiert**, und die zweite Definition naehme die Methoden mit,
  **und eine danach geschriebene Unterklasse erbte von einer anderen
  Klasse.**
- **Und die geladene Datei laeuft in DIESEM Interpreter.** Sonst haette
  jede Klasse eines Spiels eine Welt fuer sich,
  **und `class Neu < Aussen` in der geladenen Datei haette kein
  `Aussen`.**
- **Und CP932, und nicht UTF-8.** Ruby 1.8 kennt keine Kodierungsangabe
  in der Datei, und jedes Spiel aus dieser Zeit ist Shift_JIS --
  **ein Leser, der UTF-8 annimmt, macht aus jedem Kanji zwei Zeichen.**
- **Und ein Syntaxfehler in der geladenen Datei nennt die Datei.** Eine
  Zeilennummer ohne Datei ist eine Zeilennummer in dreihundert Skripten.

**Und `String#%`, `sprintf` und `printf` gab es nicht, und die Grammatik
kam aus `sprintf.c` von Ruby 1.8.1 und nicht aus meinem Kopf.** Flags,
Breite, Praezision, Wand, `*` aus einem Wert, `%%`. Gemessen: `"%05.2f" %
3.14159` war *„undefined operator '%' for a String and a Float"* --
**und die Meldung war ueber einen Operator, den es gibt.**

- **Und `0` ist ein Flag und nicht die Breite.** `%05.2f` ist Breite fuenf
  und Praezision zwei, **und ein Leser, der `0` als Breite las, wuerde
  `%5.2f` daraus machen** -- **und die fuehrende Luecke einer Uhr waere
  weg.**
- **Und die Luecke wird bei einer Zahl mit Nullen und bei einem Text mit
  Leerzeichen gefuellt.** `%05d` und `%5s` benutzen dieselbe Breite,
  **und `%.2s` schneidet einen Text ab, ohne zu runden.**
- **Und `printf` gibt nil zurueck, weil es schreibt**, **und `sprintf`
  gibt den Text zurueck.**
**Und `sub` und `gsub` gab es als einen Satz, und sie sind zwei.** Gemessen
vorher: `"aXbXc".sub("X", "-")` antwortete `'a-b-c'` --
**und ein Leser, der `Replace` fuer beides nimmt, macht aus einem Spiel, das
eine Marke aus einem Namen streicht, eines, das alle streicht.**
**Und `Replace` mit leerem Text tut in neueren Laufzeiten nichts mehr**,
**und darum hat die leere Ersetzung einen eigenen Weg.**

**Und der Block war an keiner Stelle.** `gsub("X") { |t| t * 2 }` antwortete
`'abc'` -- **und nicht `'aXXbXXc'`** -- **und die leere Ersetzung war nicht
geraten, sie war gemessen.** `BrauchtBlock` nannte `sub` und `gsub` nicht,
**und ein Block wird nur dann an den Aufruf gehaengt, wenn diese Liste ihn
nennt** -- **und `gsub` lief also mit leerem Ersatz und strich jedes X**,
**und ein Spiel, das seinen Gegaennamen in Grossbuchstaben schreibt, haette
leere Zeichen daraus gemacht und der Name waere weg.**

**Und die achte Mutation war toter Code, und das ist die dritte Form des
Problems in diesem Batch.** Zwei Ueberlebende waren *dieselbe* Luecke --
**kein Test hatte `sub` mit einem Muster** -- **und `sub` mit einem Muster
ist ein eigener Weg** (`Matches` statt `IndexOf`),
**und `Test_SubWithAPatternStopsAfterTheFirstOne` haelt jetzt beide Wege
fest.** *Ein Ueberleben ist eine Aussage ueber den Test und keine ueber den
Code.*

**Und die dritte Ueberlebende war ein Anker, der nichts aendert.** Im
Musterweg stand **zwei** `break` fuer `sub` -- **einer am Anfang der
Schleife, einer am Ende** -- **und der am Ende erreichte das `break` immer
zuerst**, **und ein Anker auf dem am Anfang war damit ein No-op**, **und
ein No-op sieht in der Mutationsliste aus wie ein Test, der zu schwach ist.
Also: **der tote Zweig ist weg**, **und die Regel sitzt jetzt auf dem
Abbruch, der tatsaechlich entscheidet.**

**Und `break` und `next` wurden nie ausgewertet, und der Wert kam nie an.**
`RubyNodeKind.Break` existierte, **und der Parser machte den Knoten, und der
Interpreter nicht** -- **und die Meldung war *this interpreter does not
evaluate a Break node, and the node is in the tree***, **einmal pro Lauf,
und ein Spiel, das `break` schreibt, hat seine Diagnosen mit einem Satz über
den Quelltext des Lesers gefüllt.**

**Und `while true; break 7; end` lief 2000000 Schritte**, **und die Meldung
war *this script ran 2000000 steps without finishing*** -- **und ein Spiel,
das auf eine Bedingung wartet, hätte zwei Millionen Schritte gehängt und
danach gestoppt, und der Stopp ist das einzige, was der Spieler sieht.**

**Und der Wert fehlte auch im Parser.** `break 7` wurde zu `break` und die 7
blieb als nächster Ausdruck stehen, **und `next if x == 2` wurde zu einem
`if`, dessen Rolle `Body` heißt, während `EvaluateIf` nach `WhenTrue`
fragte** -- **und `PartsOf` gibt leer zurück, wenn die Rollen da sind und
der Name fehlt.** Gemessen: `each { |x| next if x == 2; r = r + x }`
addierte alle drei, **und `each { |x| if x == 2; next; end; r = r + x }`
addierte vier.** *Zwei Schreibweisen desselben Satzes, und die mit einem Wort
dazwischen tat nichts.*

**Und ein Steuerwort, das im Rumpf steht, muss den Rumpf beenden.** Ohne das
lief `each { |x| next if x == 2; r = r + x }` weiter, **und der Wert des
Blocks ging in die Antwort statt in die Schleife** -- **und der Leser hat
den Wert nie angesehen, weil eine Schleife ihren Block aufruft und dessen
Ergebnis weglegt.**

**Und `dup` gab denselben Wert noch einmal zurück.**

**Und fuenf Fragen, die der erste Satz eines Ruby-Plugins stellt, fehlten
alle.** `method_defined?`, `private_method_defined?`,
`public_method_defined?`, `protected_method_defined?` und `module_function`
**-- und `require_relative`**, **das jedes VX-Plugin in einem Ordner
braucht.** Gemessen vorher, jede einzelne:

| Satz | Gemessen | Meldung |
| --- | --- | --- |
| `M.method_defined?(:x)` | `nil` | *M has no method 'method_defined?' on this host* |
| `A.method_defined?(:gibtsnicht)` | `nil` | *A has no method ...* |
| `M.module_function` | `nil` | *M has no method 'module_function' ...* |
| `M.x` nach `module_function` | `nil` | **keine Meldung, einfach nichts** |
| `require_relative "util"` | `nil` | *self has no method 'require_relative' ...* |
| `undef_method :x` | `nil` | *self has no method 'undef_method' ...* |

*Und keine davon hat einen Namen im Code. **Die Liste der abgelehnten
Methoden ist die Liste der Dinge, die der Leser nicht kann**, und sie
stand nirgends.*

**Und `method_defined?` war die schlimmste, weil sie `true` sagte.**
`A.method_defined?(:gibtsnicht)` **und** `A.method_defined?(:update)`
waren beide `true`, **weil der Leser fragte, ob der Name ein `self.`-Name
ist, und nicht, ob es die Methode gibt** -- **und `if !A.method_defined?
(:update)` haette nie ausgeloest, und genau das ist der Zweck des Satzes.**

**Und `undef_method` ist ein Aufruf auf dem Modul, und kein
Schluesselwort.** `undef` ist das Schluesselwort und verlangt einen
Bezeichner, **und `Module#undef_method` nimmt Symbole** -- **und der Leser
kannte nur `undef`**, **und die Meldung sprach von einem Host, der nie
gefragt wurde.**

**Und `A.new.respond_to?(:zeichne)` war `false`, und
`A.method_defined?(:zeichne)` war `true`.** Der Empfänger eines Objekts
trägt seinen Klassennamen bei sich, **und der Leser hielt den Namen des
Empfängers für den Klassennamen und fragte die Klasse, in der die Frage
geschrieben wurde** -- **und die beiden Zeilen stehen zwei Zeilen
auseinander in jedem Plugin.**

**Und `respond_to?` lief nur die Basisklassen, und nicht die Module.**
`HatMethode` ging `Superclass` hoch, **und `include` steht in
`Eingebunden`** -- **und `include` ist der Satz, mit dem ein VX-Grundsystem
seine Zeichenmethoden an eine Fensterklasse gibt**, **also war
`respond_to?(:draw)` `false` fuer genau die Methoden, die es gibt.**

**Und `module_function` ist eine Reihenfolge, und keine Frage.** `module M;
module_function; def x; end; end` **heisst, dass `x` auf M selbst
gerufen werden kann**, **und ein Leser, der das Wort als Frage las, sagte
`true` und liess `M.x` undefiniert** -- **und `M.x` ist der Satz, mit dem
ein VX-Plugin seine eigenen Hilfsmethoden aufruft.** Rubys `rb_mod_modfunc`
schreibt die Methode **ein zweites Mal** auf den Singleton, **und der Name
allein wird gemerkt** -- **ein Leser, der sie verschob statt kopierte,
haette jedem Fenster die Zeichenmethode genommen.**

**Und `A.x` fand die Klassenmethode der Basis nicht.** `A.respond_to?(:x)`
sagte `true` und `A.x` sagte `nil`, **und die Wache und der Aufruf stehen
zwei Zeilen auseinander** -- **und der Leser sah nur die Wache und glaubte
sie.**

**Und `require_relative` braucht den Ordner des Aufrufers, und der Leser
weiss ihn nicht.** `lib/a.rb` schreibt `require_relative "util"` und meint
`lib/util`, **und `lib/tief/b.rb` meint `lib/tief/util`** -- **und ein Leser,
der den Namen durchliess, laedt `util` von oben fuer beide, und das Spiel
bekommt einen seiner beiden Helfer und kein Wort ueber den anderen.**

**Und die Liste der geladenen Namen traegt den aufgeloesten Namen.**
Zwei Dateien, die `"util"` aus demselben Ordner anfordern, sind eine Datei,
**und ein Leser, der den geschriebenen Namen notierte, laedt denselben
Klassenrumpf zweimal** -- **und so bekommt ein Spiel zwei
`Window_Base`-Definitionen, und eine davon ist nicht die, die ihr eigenes
`super` findet.**

**Und die Liste der abgelehnten Methoden ist die Liste der Dinge, die der
Leser nicht kann, und sie stand nirgends.** Jede dieser sechs Fehlstellen
war ein `nil` mit der Meldung *has no method '...' on this host; the
interpreter does not guess* -- **und `M.x` nach `module_function` hatte
sogar keine Meldung, und das ist das Schlimmste an der ganzen Liste, weil
ein stilles `nil` wie ein `false` aussieht und der Spieler den Unterschied
erst sieht, wenn das Menue nicht aufgeht.**

*Und vier der sechs kamen in **gemessenen** Werten zusammen: `M.x` war
`nil` mit leerer Diagnose, waehrend `M.method_defined?(:x)` `true` war --
**und die beiden stehen drei Zeilen auseinander in jedem Plugin, das sich
in einem Ordner aufteilt.**

**Und der Stapel, und nicht ein Name.** Eine Datei laedt eine zweite, und
die eine dritte, **und ein Leser mit einem einzigen Namen wuerde alle drei
relativ zur aeussersten fragen** -- **und ein Helfer in einem Unterordner
wuerde einen Ordner zu hoch suchen.**

**Und vier Mutationen leben, und drei davon sind toter Code.**
Die beiden `foreach`-Schleifen ueber `Eingebunden` in `HatMethode` und
`TypHatMethode` **sagten dasselbe wie die Tabelle direkt darueber**,
**weil `include` die Modulmethoden nach `Methods` kopiert** --
`Eingemischt` macht genau das. Sie sind entfernt, **und ein Leser, der
beides haelt, kann auseinanderlaufen**: ein Modul, das nach dem
`include` noch eine Methode bekommt, steht in der einen Liste und nicht
in der anderen.

*Und ein zweiter Weg ueber dieselbe Frage ist nicht "harmlos", auch wenn
er heute dasselbe sagt.*

**Und der vierte war kein Widerspruch, und der Beleg lag die ganze Zeit
in `class.c` aus Ruby 1.8.1.**
`rb_make_metaclass` gibt dem Singleton `RBASIC(super)->klass` als
Basis (Zeile 158), `rb_singleton_class(obj)` ruft es mit
`RBASIC(obj)->klass` (Zeile 727), **und `rb_module_new` setzt
`mdl->super = 0` (Zeile 273)**. **Die Kette eines Aufrufs auf `M` ist
`Singleton(M) -> Singleton(Module) -> Class -> Module -> Object` --
und `M.m_tbl` kommt darin nicht vor.** `eval.c` Zeile 3076 ist fuer
`A.m`, `A.m()` und `A.m(1)` derselbe Opcode: `rb_call(CLASS_OF(recv),
recv, ...)`. **Es gibt keinen Unterschied, und es gab nie einen.**

**Und 52 Stellen im eigenen Testbestand hatten die falsche Fassung** --
`A.m`, `A.aussen`, `A.alt`, `Erbe.gruss`: **ein Aufruf auf eine
Instanzmethode durch den Klassennamen.** Das war meine Schreibweise und
kein Ruby-Fehler, **und alle 52 sind jetzt `A.new.m` und so weiter.**

**Und ein seit Monaten falsch gepushter Test:** `Erbe.antwort` war 42,
wobei `antwort` in `Basis` eine Instanzmethode ist. **Er prueft jetzt
`Erbe.new.antwort` (42) und `Erbe.selbst_antwort` (43)**, weil das zwei
verschiedene Aufrufe sind.

**Und der gemessene Fehler ist behoben, und vier Mutationen leben.**

**`M.x` gibt jetzt `nil` mit der Diagnose, die den Empfaenger nennt, und
`def self.x` gibt 7.** `EigeneMethode` nimmt fuer ein Symbol-Empfaenger
`M.Methods["x"]` -- **die Instanzmethode** -- **und genau dort lief der
Aufruf vorbei**, **denn der Typ-Zweig stand dahinter**. Er steht jetzt
davor, **und `EigeneMethode` bekommt fuer ein Symbol, das ein Typ ist,
gar nichts**, **weil ein Typ der Typ ist und nicht eine Instanz von ihm.**

**Und dieselbe Regel stand an zwei Orten, und ist jetzt an einem.**

**Die fuenf Feldfragen beantworteten `WertMethode` (Zeile 1703) und
`TypBefragt` (Zeile 9891) — mit demselben Code und demselben
Kommentar.** `Call` ruft `WertMethode` zuerst, **und die Kopie war
nie der Weg** — **und gemessen: mit `return null` dort bleiben alle
sechs Saetze identisch und die Suite bei `All 2069 tests passed`.**

**Und dieselbe Frage an zwei Orten ist eine Frage mit zwei Antworten,
und zwei Antworten laufen auseinander — **und die Drift ist unsichtbar,
bis ein Test den Zweig erreicht, den man nicht liest.**

**Und `FeldFrage` haelt die Bruecke zurueck und aendert nichts**
(gemessen: `All 2068 tests passed` mit und ohne den Guard) — **und
der Grund fuer ihre Existenz ist die entfernte Kopie, und der ist
gemessen.**

**Und `M.include?(N)` und `A.instance_methods` brauchen den
eingebundenen Modul-Walk**, **und als die Bruecke ganz entfernt war
antwortete dieser Ort `false`** (32 Fehler, gemessen) — **und die
Bruecke bleibt fuer die Fragen, die ihn brauchen, und nicht fuer
die fuenf.**

**Und ein `def` auf oberster Ebene ist jetzt ein `Object`, und nicht
langer eine Weigerung.**

**Gemessen vorher: `def lauf; 7; end; lauf` gab `nil` mit der Meldung
*method lauf is defined outside a class*, und `self.lauf` gab 7** —
**und `lauf` war ein `Identifier`, und `Name()` suchte in
`FindMethod(null, name)`, und null ist kein Name, und der Satz endete
als `Local("lauf")`**.

**Belegt an der Quelle, und nicht entschieden:** `eval.c` Zeile 1233
setzt beim Start `ruby_class = rb_cObject`, Zeile 1234 setzt
`ruby_frame->self = ruby_top_self`, **und Zeile 3516 gibt
`TypeError: no class/module to add method`, wenn `ruby_class` 0 ist** —
**und 0 ist genau das, was ein Leser hat, der den Satz auslaesst.**

**Und `Object` ist die Sprache, und nicht der Host:** der Leser legt
es selbst an, **und er legt es an, weil `Object.superclass ==
BasicObject` und `BasicObject.superclass == nil` gemessen sind** —
**das ist der Satz, der beweist, dass der Leser die Kette selbst
baut.** Kein Spiel-Host muss etwas bereitstellen.

**Und das ist der Satz, an dem jede RPG-Maker-Datei endet:**
`def setup` auf oberster Ebene steht in der zweiten Datei jedes VX-,
VX-Ace- und XP-Projekts.

**Und VX/VX Ace: der Ruby-Leser ist implementiert, und die Engine
ruft ihn an -- seit dieser Sitzung an einer Stelle.**

**Und ich habe die Luecke zuerst falsch benannt: ich habe sie eine
Entscheidung genannt, und sie war eine halbe Stunde Arbeit und ein
Fehler.**

**Gemessen, war fehlte:** `SkriptLaden` (Zeile 8669) kann alles -- Bytes
lesen, CP932 dekodieren, parsen, auswerten, die Kette fuehren -- **und
es war `private`,** **und oeffentlich gab es nur
`RunProgram(IReadOnlyList<RubyNode>)`,** **und also musste jeder
Aufrufer selbst lexen und parsen.** **Die Haelfte war da, und die Tuer
fehlte.**

**Und `RunScripts(IReadOnlyList<string>)` ist jetzt diese Tuer:**
die Namen kommen vom Aufrufer, die Bytes vom Host, **und die Reihenfolge
ist die des Projekts** -- `Scripts.list`, letzte Datei zuerst.

**Und der Befund, der diese Tuer aufgemacht hat, ist ein Sprachfehler
und keine Verdrahtung:** `@n = @n || 0` ist der Zaehler, den man schreibt,
wenn man keinen hat, **und `||` gab `true` zurueck statt des Operanden**
-- **gemessen fuer `nil`, `false`, `1` und `0` gleichermassen**
(`RubyInterpreter.cs` Zeile 892, vorher), **und `(nil || 0) + 1` warf
`undefined operator '+' for a Boolean and a Integer`.**

**Und das heisst: jedes Skript eines VX-Projekts aus dieser Zeit, das
so einen Zaehler hat, ist an seiner ersten Zeile gescheitert** -- **und die
Liste lief weiter, und das Spiel hatte keine Klassen und keinen Fehler
ueber der ersten Zeile.**

**Und `||` gibt jetzt den Operanden zurueck:** `nil || 0` ist 0,
`false || 0` ist 0, `1 || 0` ist 1, `0 || 0` ist 0.

**Und `&&` gab auch `false` statt des Operanden zurueck**, **und das
habe ich in derselben Stunde gemessen, in der ich es als offen
markiert hatte** -- **und mein Satz *nil und false sind in einer
Bedingung dasselbe* war falsch**, **denn `&&` gibt den Operanden
zurueck und nicht die Wahrheit.**

**Der Beleg ist Ruby 1.8.1 `eval.c` Zeile 2946, und nicht meine
Erinnerung:**

```
case NODE_AND:
    result = rb_eval(self, node->nd_1st);
    if (!RTEST(result)) break;
    node = node->nd_2nd;
    goto again;

case NODE_OR:
    result = rb_eval(self, node->nd_1st);
    if (RTEST(result)) break;
    node = node->nd_2nd;
    goto again;
```

**Der `break` verlaesst die Schleife mit `result` als Wert des
Ausdrucks, und `result` ist der linke Operand.**

**Und gemessen jetzt:** `nil && 7` ist nil, `false && 7` ist false,
`1 && 7` ist 7, `0 && 7` ist 7, `(nil && 7) || 3` ist 3,
`(1 && 7) || 3` ist 7.

**Und der Unterschied ist nicht kosmetisch:** `x.nil?` ist fuer nil
`true` und fuer false `false` -- **und ein Spiel, das
`return a && b` aus einer Methode zurueckgibt, gibt nil zurueck, wenn
`a` nil war, und ein Leser, der false zurueckgibt, gibt dem Spiel mit
`if result.nil?` einen anderen Weg.**

**Und vier weitere Regeln, und vier davon haben die Tests getoetet und
eine lebt, und die ist ehrlich nicht testbar** -- **denn
`SkriptLaden` setzt den Dateinamen in die Ausnahme selbst
(`in 'kaputt.rb': ...`),** **und der Aufrufer setzt ihn noch einmal
davor,** **und ein Test kann nicht unterscheiden, welcher der beiden der
Grund ist.** **Also habe ich die tote Regel durch die Regel ersetzt,
die den `||`-Fehler toetet,** **und das ist 5/5 durch Tests.**

*Ein Ueberleben, das man messen kann, ist eine Angabe und kein**Und der Weg von der Skriptliste zum Quelltext ist gemessen, und er
ist eine Grenze, und keine Arbeit.**

**Auf dieser Maschine liegt ein fertiges XP-Spiel:**
`E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0`, mit 109 kB
`Data/Scripts.rxdata`. **Und `MarshalReader` liest es als Daten: 90
Eintraege, jeder aus drei Feldern** -- **[0] eine Zahl, [1] der Name
(`Game_Temp`, `Game_System`, ... `Main`), [2] der Quelltext.**

**Und Feld [2] ist kein Ruby-Text, sondern Chiffre.** Gemessen: 1104
Bytes, die als `x<Bytes>WA<Bytes>6/...` ankommen. **XP und VX
verschluesseln ihr Skript mit einer aus dem Archive abgeleiteten
Kennung**, **und es gibt keinen Ordner mit `.rb`-Dateien neben
`Data/`** -- **es gibt nur `Game.exe`, `Game.ini`, `Game.rxproj` und
`RGSS104E.dll`.**

**Und damit ist die Verdrahtung, die ich als offen markiert habe, nicht
offen, sondern gedeckt** -- **und zwar durch drei unabhaengige
Entscheidungen, die vor mir getroffen wurden:**

- `AGENTS.md` Zeile 26: *Imported games are untrusted input. Never
  execute game EXEs, DLLs, Ruby, JavaScript, shell commands, or native
  plugins during detection/parsing tests.*
- `BuiltInEnginePlugins.cs` Zeile 424: `*.rgssad archive (not decrypted
  or executed)`
- `WolfDataReader.cs` Zeile 14: *deliberately not an archive decryptor*

**Und `RunScripts` bleibt damit richtig, wie es ist: der Leser kann
Skripte laden, wenn ein Host die Bytes gibt, und bei einem RGSS-Spiel
gibt der Host sie nicht, weil das Entschluesseln genau der Weg ist, der
fremden Ruby-Code ausfuehrt.**

**Und was fehlt, ist damit nicht die Engine, sondern eine Grenze, die
ein Mensch ziehen muss:** **entweder der Nutzer erlaubt das
Entschluesseln von Spielarchiven ausdruecklich, oder XP/VX/VX Ace
bleiben bei *der Geraet erkannt, der Quelltext gelesen*.**

**Und derselbe Weg hat einen zweiten Fund ergeben, und der ist ein
Datenfehler und kein Verdrahtungsfehler.**

**Auf derselben Maschine, gegen dieselbe Datei:**
`MarshalReader` las CP932 als UTF-8 (`ReadString`, `Encoding.UTF8.
GetString`). **Gemessen an den Bytes `83 65 58` -- *te* und *X* in
CP932: drei Zeichen zurueck, mit 65533 an der Stelle des ersten.**

**Und `Encoding.UTF8.GetString` wirft nicht, es ersetzt** -- **gemessen:
es kam `65533` zurueck und keine Ausnahme, und ein Leser, der auf die
Ausnahme wartet, wartet ewig.**

**Jetzt: CP932, mit UTF-8 als erstem Versuch**, **denn ein gueltiger
UTF-8-Strom hat kein U+FFFD, und eine Datei, die wirklich UTF-8 ist,
wird nicht zu Mojibake** -- **und die Rohbytes bleiben, denn die
Chiffre eines Skripts ist genau das, was CP932 nicht ist.**

**Und die vierte Mutationsregel lebte, und ehrlich:** `RegisterProvider
wird in `legacy_text_decoder.cs` Zeile 98 idempotent registriert, **und
eine zweite Registrierung im Marshal-Leser ist eine Zeile, die man
abhaengig von der Reihenfolge braucht** -- **also habe ich sie
geloescht, statt eine tote Regel zu dokumentieren.** 3/3.

**Und RM2K laeuft jetzt gegen ein fertiges Spiel, und zwei Fehler
dabei gefunden.**

**Auf dieser Maschine liegt `Dragon Destiny`: 743 Karten, 416 kB
Datenbank, echte Chipsets. Und `TestRealRm2kGameData` beweist, dass
Karten, Datenbank und Chipsets gelesen werden.**

**Und jetzt laeuft es: `TestRealRm2kRuntimeRun` startet das fertige
Spiel ueber `EnginePluginHost`, tickt 100 Bilder ohne eine einzige
Verweigerung und zeichnet eine Karte mit Inhalt.**

**Fehler 1: die Runtime nahm die erste Datei alphabetisch.**

**Zeile 89 war `Directory.EnumerateFiles(...).FirstOrDefault()`**
**und `Map0001.lmu` eines Spiels von 2002 ist die leere
Startkarte des Editors: 1227 Bytes vom 30. April 2002, ein Chip in
allen 300 Feldern.**

**Gemessen: diese Karte rendert eine Farbe, und `Map0002.lmu` --
478 kB desselben Spiels -- rendert 64.**

**Und die Startkarte steht in `RPG_RT.lmt`: `party_map_id = 742`.**
**Und 742 ist ebenfalls eine leere Editor-Karte (2266 Bytes)**
**-- das Spiel wurde also unfertig exportiert, und das ist eine
Eigenschaft des Spiels und keine des Lesers.**

**Also nimmt `PickStartMap` jetzt die Karte, die der MapTree nennt,
mit Rueckfall auf die erste** -- **und der Rueckfall bleibt, weil
ein MapTree aus einem gespeicherten Projekt eine geloeschte Karte
nennen kann, und eine Runtime, die sich daran verweigert, zeigt
statt des Spiels eine Ablehnung.**

**Fehler 2: ein leerer Frame sagte nichts.** 76800 mal
`0x00000000` und eine leere Diagnose -- **und ein Spieler sieht
einen schwarzen Bildschirm und hat nichts, was er melden kann.**
**Bei einer leeren Editor-Karte ist der schwarze Frame richtig, und
richtig ohne ein Wort ist er ein Fehler, der sich als Erfolg
verkleidet.**

**Und zwei Mutationen ueberlebten zuerst, weil mein Test die
*Entscheidung* nicht geprueft hat:** `Map0001` und `Map0002` sind
beide 20x15, **und "die Runtime laeuft" sagt nichts ueber die Karte,
die sie gewaehlt hat. Also prueft der Test jetzt
`Simulation.MapId == 742`.** 5/5 durch Tests.

**Und die Events des echten Spiels laufen, und das ist die Messung
fuer Kriterium 1.**

**`Map0002.lmu` traegt 873 Events, 1049 Seiten und 3262 Befehle in
35 verschiedenen Codes** -- Text, Verzweigungen, Variablen,
Teleporte, Bilder. **Und `Map0033` hat 4005, `Map0700` weniger.**

**Und gemessen: alle 3262 kommen beim Scheduler an, und nach 60
Bildern ist keine einzige Diagnose ueber einen Befehl.**

**Und die 100 Diagnosen sind alle von einer Art und alle richtig:**
26 mal *charset People1.png is missing from CharSet* und aehnliche.
**Das Spiel verweist auf 26 Charsets, die es nie mitgeliefert hat,**
**weil es sie nie benutzt hat** -- **und 37 Charsets liegen da.**

**Und das ist die Antwort, die ein Test aus Fixture nie geben
konnte: der Interpreter hat 3262 echte Befehle eines Spiels von 2002
gesehen, ohne an einem zu scheitern.**

**Und `Map0002` hat 478 Action-Seiten (Trigger 0) und nur 3
AutoStart-Seiten (Trigger 3)** -- **und nur die drei laufen von
selbst, und genau so soll es sein.**

**Und die Mutation *der Scheduler bekommt kein Event* wird jetzt vom
Test getoetet, und *keine automatische Seite startet* auch** --
**und die zweite Regel von vorher (die Seite wird nie gewaehlt) ist
genauso tot, weil ein Spiel mit drei AutoStart-Seiten ohne sie still
steht.** 5/5.

**Und die Befehle des Spiels aendern den Zustand, und das ist die
zweite Haelfte von Kriterium 1.**

**"Die Events sind angekommen" ist nicht "die Events haben etwas
getan."** Und ein Test, der nur Ankuenfte zaehlt, waere von einem
Leser befriedigt, der jeden Befehl liest und keinen ausfuehrt.

**Gemessen, auf `Map0002`:** die drei AutoStart-Seiten tragen 308, 89
und 8 Befehle, darunter `10210 Control switches` und `10220 Control
variables` -- **und alle Bedingungen jeder Seite sind `false`, also
unbedingt, also muessen sie laufen.**

**Und nach 30 Bildern ist `Switches.Count` von 0 auf 1847
1847. Und 1847 ist eine Schalter-Id, die die Datei des Spiels selbst
nennt** (`10210 [0,1847,1847,1]`).

**Und der Test fragt genau diese Zahl, und nicht nur "grosse als
1000":** **ein Leser, der die Liste auf eine Zahl eigener Wahl
waechst, besteht die erste Behauptung und faellt an der zweiten.**

**Und meine erste Sonde an dieser Stelle sagte `Switches.Count == 0`**
**nach 120 Bildern** -- **und sie hatte die falsche Datei gemessen**
**und die falsche Property gelesen**, **und die Sonde lief 120
Bilder in einer Runtime, die keine Befehle bekommen hat.** 5/5.

**Und Kriterium 2 war kaputt, und die Messung hat es gefunden.**

**`PresentationState.ShowPicture` verweigerte einen Befehl, dessen
Werte ausserhalb der eigenen Grenzen lagen.** Und der Befehl des
fertigen Spiels traegt `11110 [1,0,160,220,0,0,100,0,0,100,100,100,
100,0,60]` -- **also `parameters[12] = 100`, und 100 ist keine
Effektart, denn die sind 0 bis 3.**

**Gemessen vor dem Fix:** `Pictures.Count == 0` nach 40 Bildern.
**Ein Spiel, das seine Titelseite zeigt, zeigte sie nicht.**

**Und die Reparatur ist die der Quelle, und nicht meine Regel.**
EasyRPG `game_interpreter.cpp` Zeile 2949 ist der ganze
Sanitize-Block fuer `CommandShowPicture`:

```cpp
params.magnify_width = std::max(0, std::min(params.magnify_width, 2000));
params.magnify_height = std::max(0, std::min(params.magnify_height, 2000));
params.top_trans     = std::max(0, std::min(params.top_trans, 100));
params.bottom_trans  = std::max(0, std::min(params.bottom_trans, 100));
```

**Drei Clamps, und sonst nichts** -- **kein Kanal, keine Saettigung,
kein Effektmodus, kein Effektgrad, und kein Name** (und genau deswegen
hat EasyRPG ein eigenes Ticket *ShowPicture: Support empty names*).

**Und ein zu kurzer Befehl bleibt abgelehnt**, weil
`CmdSetup<&CommandShowPicture, 14>` vierzehn Parameter verlangt **und
eine abgeschnittene Datei ist kein Bild mit Vorgabewerten.**

**Und die erste Mutationsregel fuer den Clamp lebte, und das war mein
Messfehler:** ich hatte `magnify` geprueft, **und der Spielbefehl
traegt `magnify = 0`, und 0 ist schon in der Schranke.** **Also
pruefe ich jetzt die obere Transparenz mit 250, und die kommt als 100
zurueck.** 4/4.

**Und Kriterium 7 ist gemessen, und die Zahl ist schlechter als
angenommen.**

**Auf dieser Maschine liegt ein fertiges MZ-Projekt:**
`CamelliaCoronation-Win`, 20 Karten, echte Tilesets,
`System.json` mit `screenWidth: 816`.

**Und der Interpreter fuehrt 2436 Eventbefehle dieser 20 Karten in
1548 aus, und 888 nicht.**

**Und die 19 Befehle, die nicht laufen, sind keine Randfaelle:**
`123 Control Self Switch` (42x), `213 Show Balloon Icon` (36x),
`129 Change Party Member` (25x), `250 Play SE` (18x),
`221 Fadeout Screen` (16x), `402 When [**]` (16x),
`222 Fadein Screen` (14x), `203 Set Event Location` (10x),
`301 Battle Processing` (7x), `322 Change Actor Images` (6x),
`105 Show Scrolling Text` (4x), `241 Play BGM` (4x),
`225 Shake Screen` (2x), `314 Recover All` (1x).

**Ein Spiel, dessen Musik nie beginnt und dessen Kaempfe nie
beginnen, laeuft und ist nicht das Spiel.**

**Und meine erste Zahl war 3509, und die war falsch:** ich hatte
mit einem regulaeren Ausdruck gezählt **und Codes 0, 1, 2, 3, 29 und
505 gefunden** -- **und die sind gar keine Befehle**, **sondern liegen
in den Parametern von `205 Set Movement Route`**, **das ist eine
Bewegungsliste fuer sich.** **Ein Ausdruck kann einen Befehl nicht
von einem Parameter unterscheiden, und eine Zahl, die Parameter
zaehlt, ist eine Zahl ueber nichts.** **Also zaehlt der Test jetzt
die JSON-Struktur.**

**Und die Tabelle hat 114 Namen, 27 davon sind C#-Felder, und 22
werden dispatcht** -- **und 93 der 114 tun nichts.**

**Und die erste Loesung fuer Kriterium 7: sieben Audio-Befehle.**

**Die Form ist die Ueberraschung, und nicht der Befehl.** Gemessen an
`CamelliaCoronation`:

```
241 [{"name":"Scene8","volume":40,"pitch":80,"pan":0}]
250 [{"name":"Thunder4","volume":60,"pitch":120,"pan":0}]
```

**Ein Objekt als erster Parameter, und nicht vier Zahlen** -- **und XP
schreibt denselben Befehl als 11510 mit vier Bitfeldern.**

**Und `MzCommandEntry.From` gibt ein Objekt als Text zurueck**
(`MzJson.Write`, Zeile 58), **und `MzJson.TryParse` ist derselbe
Parser** -- **also wird er benutzt, und nicht ein zweiter, der sich
vom ersten unterscheiden koennte.** **Ohne das hatte jeder Kanal den
Namen `{"name"` und die Lautstaerke 0.**

**Und vier Kanaele:** BGM (241/242), BGS (245/246), ME (249) und SE
(250/251). **Und 251 traegt keinen Parameter**, **und ein Ausblenden
von null Bildern ist ein Stopp** -- **denn das Feld ist leer, wenn
niemand es angefasst hat, und ein Leser, der null als "noch nicht"
las, haette Musik fuer immer laufen lassen.**

**Und ein Spiel loescht ein laufendes Ausblenden** -- **ein zweites
241 hintereinander ist das zweite Stueck, und nicht das erste
zweimal.**

**Neu gemessen: 1570 von 2436 ausfuehrbar, 866 nicht** -- **vorher
waren es 1548 und 888.**

*Ein Ueberleben, das man messen kann, ist eine Angabe und keinag. **Und ein Ueberleben, das man nicht messen kann, ist ein
Test, den man schreiben muss** -- **und das ist der Unterschied
zwischen einer Zahl und einem Satz.**

**Und `self` im Klassenrumpf ist der Typ.** `class A; @n = 0; end`
schreibt `@n` an das Klassenobjekt, **und jedes `A.new` faengt leer
an** -- gemessen vorher: `A.instance_variables` war `[]` und
`A.instance_variable_get(:@n)` war `nil`, **weil der Rumpf ohne `self`
lief und der Wert in keinen Speicher kam.** `RubyType` hat jetzt einen
eigenen `Felder`-Speicher, **und `A.instance_variables` gibt `[@n]`
und `A.new.instance_variables` gibt `[]`** -- **und genau das ist der
Unterschied, den ein Plugin bemerkt.**

**Und vier Tests hielten eine Abweichung fest, und die Abweichung tat nichts.**
`Local` und `SetLocal` fingen bei der Blockebene an, **und der Kommentar,
der das begründete, nannte `3.times { |i| g.push(i) }` als den Fall, für den
sie nötig sei.** Gemessen: **`g = []; 3.times { |i| g.push(i) }; g.length`
war 0** -- **und genau dieser Satz baut jedes Menü und jedes Fenster eines
Spiels.**

**Verifiziert in `parse.y` aus Ruby 1.8.1:** `local_push` schreibt
`local->prev = lvtbl`, **`lvtbl = local`, und die Kette bleibt offen** --
**und nur `ruby_dyna_vars` wird in `opt_block_var` gespeichert und
wiederhergestellt.** Der Block sieht also die Variablen der Methode, **und
`lambda { x = 1 }` schreibt in ihre 99 hinein.**

**Und vier Tests behaupteten das Gegenteil, ausführlich begründet, und sie
lagen falsch.** `Test_ABlockHasItsOwnVariables` erwartete 99,
`Test_ABlockThatOnlyReadsSeesNothing` erwartete nil,
`Test_DefinedInABlockSeesTheBlockAndNotTheMethod` erwartete nil, **und
`Test_TwoBlocksDeepAndTheInnerOneSeesNothing` erwartete `[99, 1, 1]`, gemessen
ist `[99, 2, 2]`.** Sie sind auf die gemessene Wahrheit umgestellt.

*Ein Kommentar, der den Fall nennt, an dem die Regel scheitert, ist eine
Behauptung und kein Beleg -- und der Fall war der häufigste, den ein Spiel
schreibt.* **Und ein Test, der eine Abweichung festhält, prüft die
Abweichung und nicht Ruby.**
 `a.dup.n = 2` ließ
`a.n` auch auf 2, **und ein Spiel, das zwei Figuren aus einer Vorlage macht,
hätte eine Figur zweimal, und jede Änderung an der einen wäre an der
anderen sichtbar.** Die Kopie trägt jetzt den Klassennamen bei sich --
**und ohne das sagte `b.n = 2` *a value has no method 'n=' on this host*,
und die Meldung sprach von einem Host, der nie gefragt wurde.**

**Und `def hp=(v)` war ein Syntaxfehler, und `attr_writer` in derselben Datei
ging.** Die Fehlermeldung war *A member name was expected at offset 44, but
'end' is there* -- **und der Name war richtig: der zweite Lesevorgang hatte
ihn aufgegessen.** Und `def ==(other)` war ein Schreiber mit dem Namen `==`,
**weil `Is("=")` auf `Current.Text` sieht und dort `==` steht.**

*Ein Anker, der gebaut ist und nichts aendert, sieht wie ein ueberlebender
Test aus und ist ein Messfehler.* **Also gehoert in jede Mutationsliste
zwei Pruefungen: der Anker kommt genau einmal vor, **und der Ersatz ist
nicht identisch mit dem Anker.** Die zweite habe ich bis heute nicht
gemacht, **und deshalb habe ich eine tote Regel zwei Laeufe lang als
Befund gemeldet.**

**Und drei der Fehler in diesem Batch waren Testfehler, und keiner davon sah
aus wie ein Testfehler:**

1. `"#{$1}"` ist eine andere Ruby-Sache als `$1`, **und der Leser hat sie
   nicht** -- **und mein Test behauptete, er habe sie.**
   *Ein Test, der eine Form behauptet, ist manchmal genau die Form, die
   fehlt.*
2. `"\1"` ist **der Oktalwert 1** -- **gemessen: genau ein Byte `01`** --
   **und der Gruppenrueckverweis steht in `"\\1"`.**
   *Ein Backslash im Ruby-Text ist eine Oktalzahl und kein Backslash, und das
   weiss man erst, wenn man die Bytes ansieht.*
3. `["a", "b"]` in einem Test prueft **eine Sammlung**, **und nicht zwei
   Umbenennungen** -- **und der Test lief in einen
   `IndexOutOfRangeException`, der aussah wie ein Leserfehler.**

**Und die Gruppenliste faengt bei 0 an, und die Ziffer im Ersatzerzeugnis
bei 1.** `"anna bob".gsub(/(\w+) (\w+)/, "\\2 \\1")` antwortete
`' bob'` -- **und das sieht wie ein Zeichenfehler aus und ist einer**,
**denn beide Gruppen waren vertauscht.**

**Und Muster aus Skripten laufen jetzt in derselben Schranke wie `=~`, nachdem
sie vorher alle abgelehnt wurden.** Die alte Regel
`Test_APatternFromAScriptIsNotRunAndItSaysSo` **war damals richtig und ist es
nicht mehr** -- **und ein Test, der eine Regel festschreibt, muss mit der
Regel gehen und nicht gegen sie.** 4097 Bytes werden abgelehnt, **und die
Meldung nennt jetzt das Muster und nicht nur die Laenge** -- **denn bei
hundert Mustern in einem Spiel weiss man sonst nicht, welches zu gross war.**

**Und `String * Integer` ist der Trennstrich zwischen zwei Fenstern.** `"-" *
30`,
**und ohne das antwortete der Leser *undefined operator '*' for a String and
a Integer*** -- **und die Meldung waere wieder ueber einen Operator gewesen,
den es gibt.**

**Und die siebte Mutation lebte, und gemessen war sie ein Testfehler und
kein Codefehler:** `printf("%d", 5)` antwortet `Nil`, **und
`sprintf("%d", 5)` antwortet `'5'`.** Die Mutation liess `printf` den Text
zurueckgeben,
**und kein Test sah es, weil `printf` bis dahin nur auf seine *Art*
gesehen wurde** -- **und die Ablehnung antwortet auch mit `Nil`.**
**Ein Test, der nur `nil` sieht, besteht auf einem Leser, der `printf`
gar nicht hat.** `Test_SprintfReturnsTheTextAndPrintfReturnsNothing`
prueft jetzt **die Art *und* die Diagnose**, **und die leere Diagnose
schliesst den Null-Host als Kandidaten aus.**


## Und der Fund, der mehr wert ist als die Formatierung

**Ein Aufruf ohne geschriebenen Empfaenger hat bei diesem Leser einen
Empfaenger bekommen: seinen eigenen Namen.** `sprintf("%d", 5)` wurde zu
einem `Call`, dessen erstes Kind der Name `sprintf` war,
**und `Call` wertet sein erstes Kind als Empfaenger aus**,
**und ein Name, den niemand gesetzt hat, ist nil** --
**und die Meldung lautete *„nil has no method 'sprintf' on this host"***,
**also ueber einen Empfaenger, den der Leser selbst erfunden hatte.**

**Das ist jetzt ein `SelfCall`, und die Argumente sind seine Kinder und
tragen ihre Rolle.** `Test_ACallWithoutAReceiverIsACallOnSelf` haelt es
fest. **Und `draw(x)` in einer Klasse ist derselbe Satz** -- **was sich
aendert, ist der Empfaenger, den die Skriptmethode sucht: `self` und
nicht der Name.**

**Und das kostete zwei Fehlschlaege, die beide Messungen waren:**
`rollen=0` nach einer Aenderung, die im Code *stand* --
**weil sie im anderen der beiden Zweige stand** (`Keyword` bei 1211,
`Identifier` bei 1273), **und ein Anker mit drei Zeilen passt in beide**.


**Und der CP932-Test hat drei Fehler gehabt, und alle drei waren meine, nicht
des Lesers:**

- **Und das Byte-Array hatte ein Leerzeichen vor dem Kanji** -- **also war
  `Kanji` ein Name und `日` ein zweiter**, und der Lexer war berechtigt,
  zwei Namen zu sehen. **Der Lexer hat nie etwas falsch gemacht:**
  **gemessen: `Constant 'Kanji日'` ohne das Leerzeichen, und
  `Constant 'Kanji'` plus `Identifier '日'` mit.**
- **Und der Test suchte den blossen Kanji-Namen, nicht
  `Kanji<kanji>`.** **Ein Test, der nach dem reparierten Namen sucht,
  waere auf einem Leser durchgegangen, der jedes Kanji ersetzt** -- **weil
  beide nach einem Namen suchen, den die Datei nicht deklariert.**
- **Und `DefinedTypes.Contains` gibt es nicht**, `DefinedTypes` ist eine
  Liste, **und der Compiler nannte statt dessen `CallMethod`**, weil
  `using System;` fehlte und er am ersten Member aufgab. **Eine
  Fehlermeldung, die etwas anderes nennt als das, was fehlt, ist ein
  Grund, den ganzen Block zu lesen und nicht die erste Zeile zu reparieren.**

board

| ID | P | State | Card | Depends on |
|---|---:|---|---|---|
| K-001 | 0 | DONE | Validate 2026-08-20 stabilization changes on Godot 4.7.2 | — |
| K-002 | 0 | DONE | Harden core test baseline and eliminate remaining parser/runtime compile warnings | K-001 |
| K-003 | 0 | DONE | Replace superseded GDScript implementation with validated C#/.NET runtime | K-002 |
| K-004 | 0 | DONE | Integrate trusted engine plugin catalog, bounded detection, import persistence, and safe runtime selection | K-003 |
| K-010 | 0 | DONE | Validate LCF reader/parser against legal real-world RM2K/2003 fixtures | K-001 |
| K-011 | 0 | DONE | Implement LMT map-tree parser with bounded BER/structure handling | K-010 |
| K-012 | 0 | DONE | Expand LDB decoding into typed core database sections | K-010 |
| K-013 | 0 | DONE | Expand LMU event/page metadata decoding without executing commands | K-010 |
| K-014 | 0 | DONE | Preserve unknown LCF fields/chunks for diagnostics and forward compatibility | K-010 |
| K-015 | 0 | DONE | Decode remaining LDB array sections into typed models | K-012 |
| K-016 | 0 | DONE | Prioritized RPG Maker MZ detection and bounded metadata inspection | K-004 |
| K-017 | 2 | DONE | Bounded MZ data-directory metadata inspection (Actors/MapInfos/encrypted assets) | K-016 |
| K-018 | 2 | DONE | Complete MZ database inventory (section counts, system name arrays, map files) | K-017 |
| K-019 | 1 | DONE | ConditionalBranch condition evaluation (switch/variable comparisons) | K-023 |
| K-020 | 1 | DONE | Define faithful RM2K/2003 simulation state model | K-011,K-012,K-013 |
| K-021 | 1 | DONE | Implement first event-interpreter slice: message/switch/variable/branch/wait/transfer | K-020 |
| K-022 | 1 | DONE | Implement map/player movement and passability simulation | K-020 |
| K-023 | 2 | DONE | Replace placeholder interpreter opcodes with verified RM2K/2003 command codes | K-021 |
| K-024 | 2 | DONE | Move Godot project into `project/` and keep runtime/tooling at repo root | — |
| K-030 | 1 | DONE | Godot renderer adapter: virtual framebuffer + lower/upper tile layers | K-020 |
| K-031 | 1 | DONE | Character/event sprite renderer and camera | K-030 |
| K-032 | 1 | DONE | Message/window/picture/choice/input presentation and runtime/UI handoff | K-030,K-021 |
| K-033 | 1 | DONE | Visible RM2K map/framebuffer and sprite overlay in runtime UI | K-030,K-031,K-032 |
| K-034 | 1 | DONE | Safe keyboard movement handoff to RM2K simulation | K-022,K-033 |
| K-035 | 1 | DONE | Keyboard message dismissal, choice navigation, and numeric input handoff | K-032,K-034 |
| K-036 | 1 | DONE | Advance deterministic runtime simulation frame count from virtual clock | K-020,K-034 |
| K-037 | 1 | DONE | Clickable message, choice, and numeric-input presentation controls | K-032,K-035 |
| K-038 | 1 | DONE | Avoid per-frame choice-control reconstruction in runtime UI | K-037 |
| K-039 | 1 | DONE | Expose explicit runtime stop control and hide stale presentation controls | K-037,K-038 |
| K-040 | 1 | DONE | RTP registry/resolver without bundled proprietary RTP data | K-012 |
| K-041 | 1 | DONE | Missing-asset diagnostics and per-game RTP profile | K-040 |
| K-042 | 1 | DONE | RM2K event-page selection and bounded trigger scheduler | K-020,K-021 |
| K-043 | 1 | DONE | Decode LMU event-command vectors and feed native scheduler | K-042 |
| K-044 | 1 | DONE | Dispatch action/touch events from player input and movement | K-042,K-043 |
| K-045 | 1 | DONE | Decode LMU event-page switch and variable conditions | K-042,K-043 |
| K-046 | 1 | DONE | Complete selector evaluation for switch B and variable comparisons | K-045 |
| K-047 | 1 | DONE | Diagnose unsupported RM2K commands without execution | K-043 |
| K-048 | 1 | DONE | Separate LMU move-route and event-command presence metadata | K-045 |
| K-049 | 1 | DONE | Evaluate bounded RM2K item and actor page conditions | K-045 |
| K-050 | 2 | DONE | Original-format read-only LSD save model and safe save directory integration | K-020 |
| K-051 | 1 | DONE | Add deterministic RM2K Timer 1/Timer 2 conditions | K-045 |
| K-052 | 1 | DONE | Add bounded JSON simulation save/load roundtrip | K-020 |
| K-053 | 1 | DONE | Adaptive application render FPS without changing simulation Hz | K-036 |
| K-054 | 1 | DONE | Add capability-gated RM2K save/debug tool contracts | K-052 |
| K-055 | 1 | DONE | Add bounded runtime-owned RM2K JSON save-directory slots | K-052 |
| K-060 | 2 | DONE | Game compatibility profile schema versioning/validation | K-002 |
| K-061 | 2 | DONE | Compatibility report export for GitHub issues | K-060 |
| K-070 | 3 | DONE | Faithful-vs-Enhanced profile and integer scaling controls | K-030 |
| K-071 | 3 | DONE | Controller/touch remapping layer | K-020 |
| K-072 | — | DONE | Reusable RM2K host lifecycle | — |
| K-073 | — | DONE | Synchronize runtime sprite descriptors after movement | — |
| K-074 | — | DONE | Fail-closed pending transfer parameters | — |
| K-075 | — | DONE | Transfer facing direction validation | — |
| K-076 | — | DONE | Clear confirmed choice presentation state | — |
| K-077 | — | DONE | Preserve pending InputNumber state across variable conflicts | — |
| K-078 | — | DONE | Implement bounded RM2K ChangeItems command | — |
| K-079 | — | DONE | Implement bounded RM2K ChangePartyMembers command | — |
| K-080 | 4 | IN PROGRESS | RGSS: the Ruby interpreter, no eval and no marshal execution | RM2K playable milestone |
| K-081 | 0 | DONE | Decode real LMU event pages: fix struct-array field collection and verify liblcf IDs | K-013 |
| K-082 | 0 | DONE | Align event-page trigger ids with liblcf and fail closed on undecodable pages | K-081 |
| K-083 | 0 | DONE | Correct ControlSwitches/ControlVariables parameter layout to the verified EasyRPG spec | K-081 |
| K-084 | 1 | DONE | Implement verified actor-stat, screen-effect, and event-control interpreter commands | K-023 |
| K-085 | 2 | DONE | Bring RPG Maker MV to data-directory and System.json metadata parity with MZ | K-017 |
| K-086 | 1 | DONE | Decode verified RM2K chipset passability arrays from the LDB chipset section | K-015 |
| K-087 | 2 | DONE | Add verified RM2K autotile animation ticking (counter values blocked: no verified data source) | K-015 |
| K-088 | 2 | DONE | Apply verified RM2K tile substitution tables (source: liblcf SaveMapInfo, not LMT) | K-086 |
| K-089 | 2 | DONE | Decode RM2K per-map terrain tags via verified `Game_Map::GetChipId` substitution | K-015 |
| K-090 | 4 | IN PROGRESS | MV/MZ: script files read as data, no JavaScript executed | RM2K playable milestone |
| K-091 | 2 | DONE | Apply verified `Game_Map::IsCounter` action-trigger propagation across up to 3 counter tiles | K-015 |
| K-092 | 2 | DONE | Drive movement and event triggers from player input in the RM2K runtime | K-015 |
| K-093 | 3 | DONE | Route the Godot host input through the verified turn order instead of ad-hoc triggers | K-092 |
| K-094 | 0 | DONE | Vehicles for the action-event order | — |
| K-095 | 3 | DONE | Resolve verified chipset source rectangles for blocks C, E and F | K-087 |
| K-096 | 3 | DONE | Build the verified block D autotile quarter table and block geometry | K-095 |
| K-097 | 3 | DONE | Build the verified block A/B autotile composition from `BlockA_Subtiles_IDS` | K-096 |
| K-098 | 3 | DONE | Decode the indexed RM2K chipset bitmap and blit the resolved rectangles | K-097 |
| K-099 | 3 | DONE | Compose a full map frame from chipset tiles, map layers and the z-order rule | K-098 |
| K-100 | 5 | BACKLOG | PE/DLL inspector research and safe metadata-only parser | Stable primary runtimes |
| K-101 | 3 | DONE | Decode the RM2K charset geometry and draw character frames | K-100 |
| K-102 | 3 | DONE | Decode event sprite fields and place characters per draw stage | K-101 |
| K-103 | 2 | DONE | Resolve the hero charset and draw the hero and events in the runtime frame | K-102 |
| K-104 | 2 | DONE | Re-render the frame when the player moves | K-103 |
| K-105 | 2 | DONE | Camera viewport instead of a full-map frame | K-104 |
| K-106 | 2 | DONE | Block E passability offset and the two-sided movement check | K-105 |
| K-107 | 2 | DONE | Walk animation, the per frame step budget and the hero sprite wiring | K-106 |
| K-108 | 3 | DONE | WOLF binary .mps reader, built from the verified format | K-094 |
| K-109 | 3 | DONE | WOLF event command list, decoded from the verified signature table | K-108 |
| K-110 | 3 | VERIFY | WOLF transfer, move route, database and common event binary formats | K-109 |
| K-111 | 2 | DONE | RM2K move route, so events walk at the verified per frame rate | K-107 |
| K-112 | — | DONE | RGSS archive format, shared by XP, VX and VX Ace | — |
| K-113 | — | DONE | Ruby Marshal reader for the RPG Maker data files | — |
| K-114 | — | DONE | RM2K vehicles: state, boarding, sprites and the airship shadow | — |
| K-115 | — | DONE | Ruby lexer for the RGSS engines | — |
| K-116 | — | DONE | Ruby parser for the RGSS engines | — |
| K-117 | — | DONE | The value layer between a game's data and its language | — |
| K-118 | — | DONE | Name what every child of a tree is for | — |
| K-119 | — | DONE | Read a whole number wider than this machine holds | — |
| K-120 | — | DONE | Read the data three real games actually wrote | — |
| K-121 | — | DONE | Read the data an RPG Maker MZ game wrote | — |
| K-122 | — | DONE | Name every command an RPG Maker MZ game stores | — |
| K-123 | — | DONE | Decide a conditional branch the way the engine does | — |
| K-124 | — | DONE | Walk an event list with an index the way the engine moves it | — |
| K-125 | — | DONE | Run the list a command calls, and stop at a wait | — |
| K-126 | — | DONE | Change what the party is carrying | — |
| K-127 | — | DONE | Put a picture on the screen and move it off again | — |
| K-128 | — | DONE | Measure what this game actually needs from MZ before modelling more of it | — |
| K-129 | — | DONE | A second MZ fixture, from a game with no plugins | — |
| K-130 | — | DONE | Send the player somewhere, and hold the page until they arrive | — |
| K-131 | — | DONE | Walk a character, one step a frame | — |
| K-132 | — | DONE | Read a line of text, and every code in it | — |
| K-133 | — | DONE | A 101, and everything it swallows | — |
| K-134 | 1 | DONE | The twenty-five table rows that have no card behind them | — |
| K-136 | 0 | DONE | Every RM2K code liblcf names: 117 of 121 dispatched, the four gaps closed | — |

## Card details

### K-022 — Map/player movement and passability simulation

**Acceptance criteria**
- Configure bounded map dimensions and row-major passability data.
- Move only one cardinal tile per call; update facing using RM direction codes 2/4/6/8.
- Reject map bounds, impassable tiles, malformed passability lengths, diagonal moves, and invalid map dimensions without changing position.
- Increment `Steps` only after successful movement; retain bounded diagnostics for blocked/rejected movement.

**Progress evidence (2026-08-24)**
- Added `GameSimulationState.ConfigureMap` and `TryMove`.
- Added regression coverage for successful movement, facing, blocked tiles, map bounds, diagonal rejection, and passability-shape validation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — passed, 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `213/213` tests.
- RM2K-specific chipset passability decoding remains separate: current implementation intentionally does not invent unverified chipset rules.

### K-031 — Character/event sprite renderer and camera

**Acceptance criteria**
- Produce bounded player and map-event sprite descriptors from parsed map data.
- Reject malformed events and coordinates outside map bounds.
- Maintain camera center clamped to map and viewport bounds.
- Keep texture loading and foreign game-code execution outside this data adapter.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/Rm2kSpriteRenderer.cs` and `project/tests/core/test_rm2k_sprite_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `221/221` tests passed.
- Scope boundary: descriptors/camera only; no untrusted asset/script/native execution.

### K-032 — Message/window/picture presentation layer

**Acceptance criteria**
- Store bounded message state and continuation text.
- Store, replace, and erase bounded picture descriptors.
- Allow `EventInterpreter` to publish ShowMessage output into presentation state through explicit dependency injection.
- Reject oversized or malformed presentation data without executing foreign code.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/presentation/PresentationState.cs` and `project/tests/core/test_presentation_state.cs`.
- `EventInterpreter` now optionally receives `PresentationState`; ShowMessage updates it while retaining existing diagnostics behavior.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `225/225` tests passed.
- Scope boundary: no texture loading, external scripts, native plugins, or game executables are invoked.

### K-040 — RTP registry/resolver without bundled proprietary RTP data

**Acceptance criteria**
- Register only explicit user-provided RTP roots; do not bundle, download, or auto-discover proprietary RTP data.
- Resolve assets by engine, generation, dependency name, and bounded relative path in deterministic registration order.
- Reject absolute paths, traversal, NUL bytes, invalid identifiers, missing roots, duplicate profile IDs, and reparse-point escapes.
- Return structured status for no profile, missing asset, invalid path, and successful resolution without opening or executing the asset.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpRegistry.cs` and `project/tests/core/test_rtp_registry.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `254/254` tests passed.
- Scope boundary: K-040 is an in-memory explicit registry only; diagnostics integration and persisted per-game RTP profiles remain K-041.

### K-041 — Missing-asset diagnostics and per-game RTP profile

**Acceptance criteria**
- Represent a bounded per-game RTP profile without copying or embedding RTP data.
- Serialize and deserialize profile metadata through a bounded JSON codec with validation.
- Report required assets as `Available`, `MissingAsset`, `NoMatchingProfile`, or `InvalidPath`.
- Keep diagnostics data-only; no asset opening, parsing, downloading, or execution.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpDiagnostics.cs` and `project/tests/core/test_rtp_diagnostics.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `258/258` tests passed.
- Scope boundary: profile metadata is not yet wired into persisted `GameLibrary` records; that integration remains a follow-up if required by the save/runtime UI.

### K-030 — Godot renderer adapter

**Acceptance criteria**
- Store lower and upper RM2K tile IDs in a deterministic virtual framebuffer.
- Convert bounded parser map output into the framebuffer without executing game code.
- Reject malformed dimensions, layer lengths, non-integer tile IDs, and negative tile IDs.
- Keep Godot rendering APIs out of the parser-facing adapter; actual texture/tile drawing remains a later presentation slice.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/VirtualFramebuffer.cs` and `project/tests/core/test_rm2k_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `217/217` tests passed.
- Scope boundary: this is renderer-neutral framebuffer assembly; no chipset passability inference, texture loading, camera, or native/game-script execution was added.

### K-001 — Validate stabilization changes

**Acceptance criteria**
- `./scripts/validate.sh` runs with Godot 4.7.2 stable.
- Import/syntax validation succeeds.
- C# core/smoke runner passes under Godot .NET.
- Smoke runner passes.
- Any newly found regression gets its own test before the fix is marked complete.

**Failure policy**
Do not remove new regression tests to restore green status. Use the anti-loop policy in `AGENTS.md`.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Import/syntax validation — passed.
- Core suite — `92/92` tests passed.
- Smoke suite — passed.
- Repaired GDScript parser compatibility in `RM2KDatabase` and `VirtualClock`; added database serialization regression coverage and Windows Godot discovery candidates.

### K-002 — Harden core baseline

**Acceptance criteria**
- No known GDScript parse errors in source files reachable by the app/tests.
- Core abstractions have deterministic tests for documented behavior.
- Documentation accurately states current test count/status.
- C# migration is tracked and validated by K-003.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `95/95` tests passed, including the new legacy-decoder suite.
- Removed the unsupported CP932 conversion attempt on Windows by normalizing CP932/SJIS aliases to `SHIFT_JIS`.
- Replaced the GDScript `"\\u0000"` source literal with byte-level NUL detection; the VFS security regression remains covered without parser diagnostics.
- Remaining non-fatal output is limited to intentional invalid-input diagnostics and Godot's `EditorSettings` headless-editor message.

### K-003 — C#/.NET migration

**Acceptance criteria**

- `dotnet build UniversalRPG.csproj` passes with zero errors.
- Godot .NET headless runner instantiates scene scripts and passes all ported tests.
- Superseded source, application, and test `.gd` files are removed.
- Scenes, validation script, and active documentation reference C# paths.

**Validation evidence (2026-08-21)**

- Godot `4.7.2.stable.mono.official.ed1daf0bf` instantiated `tests/CSharpRunner.cs` after PascalCase file renames required by `ScriptPathAttributeGenerator`.
- C# runner passed `128/128` tests with exit code `0`.
- `scripts/validate.sh` now runs .NET restore/build, Godot import, and the C# runner.

### K-004 — Engine plugin foundation and application wiring

**Acceptance criteria**
- Trusted compiled plugin contracts expose metadata, capabilities, probe results, runtime lifecycle, and typed diagnostics.
- Built-in descriptors cover RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection. RM95/RGSS/MV/MZ/Unite remain detection-only; WOLF has an explicitly unencrypted plain-data slice, and RM2K/RM2K3 additionally parse LDB/LMT/LMU data.
- Detection uses bounded read-only folder/ZIP inspection and retains ranked candidates, evidence, ambiguity, malformed-input, and unknown diagnostics.
- Library import/scan persists versioned detection metadata and revalidates persisted selections on relaunch.
- Runtime selection refuses ambiguous, unknown, malformed, detection-only, missing, capability-incompatible, platform-incompatible, and probe-failing candidates without external fallback.
- Godot UI displays plugin/candidate status and structured diagnostics.

**Validation evidence (2026-08-21)**
- `dotnet build UniversalRPG.csproj --no-restore` — passed with `0` warnings and `0` errors after nullable-contract hardening.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `159/159` C# tests including RGSS/WOLF slices.
- Detection never executes imported EXE, DLL, Ruby, JavaScript, shell, or native plugin files; ZIPs are inspected without extraction. RM2K/RM2K3 runtime tests load only validated fixture data and advance the deterministic clock.

### K-010 — Real LCF validation

**Acceptance criteria**
- Add legal/reproducible fixture provenance notes.
- Verify LDB and LMU headers/chunk boundaries on at least two independent fixtures where available.
- Parser must reject truncation, invalid BER, oversized chunks and unreasonable dimensions without crashes or unbounded allocation.
- Unknown fields are retained or reported rather than silently interpreted as known data.

**Validation evidence (2026-08-20)**
- Added pinned, hashed RM2000 and RM2003 LDB/LMU/LMT fixtures from `EasyRPG/TestGame` commit `4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313`; provenance is in `tests/fixtures/easyrpg-testgame/README.md`.
- Real-fixture tests verify both LDBs and both LMUs, exact file sizes, headers, chunk counts, terminator behavior, and reader position at EOF: `5/5` real-fixture tests passed.
- Full core suite: `102/102` tests passed; full `./scripts/validate.sh` passed.
- Repaired valid zero-length RM2003 struct-array sections and added unknown top-level chunk retention coverage.

### K-011 — LMT map tree

**Acceptance criteria**
- Parse `LcfMapTree` container safely.
- Extract map IDs, names, parent relationship and start-position metadata that is verified against fixtures/documentation.
- Detect cycles/invalid parent references defensively.
- Unit tests cover valid, empty, truncated and malicious-size fixtures.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `109/109` tests passed, including real LMT and bounded malformed-input coverage.
- Implemented `parse_map_tree()` with verified LMT field IDs, signed RM2000 map IDs, parent/tree-order validation, cycle detection, and raw unknown-field retention.

### K-012 — Typed LDB sections

**Acceptance criteria**
- Decode sections incrementally into typed data models.
- Every decoded field has a verified LCF field ID/source; no guessed offsets.
- Unknown fields remain preserved for diagnostics.
- Synthetic fixtures and at least one real fixture comparison exist.

**Validation evidence (2026-08-22)**
- `bash scripts/validate.sh` passed with Godot `4.7.2.stable.mono` on Linux; headless C# suite `165/165`.
- Actors section decodes to typed entries with verified liblcf field IDs (`src/generated/lcf/ldb/chunks.h`, `ChunkActor`): strings 0x01/0x02/0x03/0x0F, integers 0x04/0x05/0x07/0x08/0x09/0x0A/0x10; defaults mirror `rpg::Actor` initializers.
- Switches/variables decode as id/name entries (`ChunkSwitch`/`ChunkVariable`: name=0x01); duplicate structure IDs are rejected.
- Unknown actor/entry fields retained per entry; synthetic tests cover defaults, unknown retention, duplicate IDs, missing terminators.
- Real-fixture comparison: typed entry counts equal `section_counts` on both pinned EasyRPG TestGame LDBs.
- Scope note: per agent maintenance rules the remaining array sections were split into successor card K-015; this card is done for actors/switches/variables plus framing already covered earlier.

### K-015 — Remaining typed LDB array sections

**Acceptance criteria**
- Decode skills, items, enemies, troops, terrains, attributes, states, animations, chipsets, classes, and battle commands incrementally using field IDs verified against liblcf `ldb/chunks.h`.
- Nested structures stay data-only; unknown fields remain preserved.
- Synthetic malformed-input fixtures and real-fixture count comparisons exist per section batch.

**Progress evidence (2026-08-22)**
- Implemented the first K-015 batch for skills (`0x0c`), items (`0x0d`), states (`0x12`), and classes (`0x1e`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, scalar values, unknown-field retention, duplicate-safe framing, and section-count parity.
- `dotnet build --no-restore` — passed with `0` warnings and `0` errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `166/166` C# tests and smoke validation.
- Implemented the second K-015 batch for enemies (`0x0e`), terrains (`0x10`), and attributes (`0x11`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, combat/environment scalar values, unknown-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `167/167` C# tests and smoke validation.
- Implemented the third K-015 batch for troops (`0x0f`), animations (`0x13`), and chipsets (`0x14`). Scalar metadata is typed; nested members, frames, and tile arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for presentation metadata, nested-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `168/168` C# tests and smoke validation.
- Implemented the fourth K-015 batch for battle commands (`0x1d`). Scalar metadata uses verified liblcf field IDs; nested command data remains preserved as unknown fields and trailing data is rejected.
- Added synthetic battle-command coverage and extended real-fixture count parity to every typed LDB array section.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `170/170` C# tests and smoke validation.
- K-015 acceptance criteria are complete; K-015 is `DONE`. K-016 is now the active MZ-priority card.

### K-016 — Prioritized RPG Maker MZ detection and bounded metadata inspection

**Acceptance criteria**
- Strengthen MZ detection using the MZ runtime layout and `data/System.json`; MV signatures must not be accepted as MZ.
- Inspect bounded MZ metadata only; never execute `index.html`, `rmmz_*.js`, `plugins.js`, native binaries, or external runtimes.
- Keep MZ detection-only and non-launchable until a separately verified JavaScript runtime exists.
- Add positive, negative, malformed, and oversized metadata regression coverage.
- Update detection/security documentation with the exact supported boundary.

**Progress evidence (2026-08-22)**
- Added MZ-specific validation on top of the shared web detector: `rmmz_core.js`, `rmmz_managers.js`, and bounded `data/System.json` JSON-object validation are required.
- MV remains on the generic `rpg_core.js` path and is not affected by the MZ-only checks.
- Added positive, missing-manager, malformed-JSON, and oversized-metadata fixtures; no JavaScript, HTML, native binary, or external runtime is executed.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `171/171` C# tests and smoke validation.
- Typed bounded MZ metadata extraction and encrypted-asset diagnostics landed; K-016 is `DONE`.

### K-021 — First event-interpreter slice

**Acceptance criteria**
- Interpret message, wait, if/else/endIf, loop/breakLoop commands deterministically without side effects beyond the simulation state.
- Interpret switch, variable, and transfer-player commands against the bounded `GameSimulationState`.
- Malformed or out-of-range payloads produce diagnostics and are skipped safely; no crashes, no unbounded loops.
- Regression coverage for each command family including malformed payloads.

**Progress evidence (2026-08-23)**
- Fixed the Variant cast in `GetCmdParams` (build blocker) and removed the dead `_shouldBreak` field.
- Added dispatch plus bounded executors for `ControlSwitches`, `ControlVariables` (set/add/sub/mul/div/mod with division-by-zero diagnostic), and `TransferPlayer` (pending-transfer state).
- Removed the placeholder move-route case whose opcode literal collided with `ControlSwitches` (`CS0152`).
- Placeholder opcode constants (101–118, 105–107) documented as such; migration is tracked as K-023.
- `bash scripts/validate.sh` — passed; `198/198` C# tests and smoke validation after the K-023 opcode migration (typed EventCommand model).

### K-024 — Repository layout split

**Acceptance criteria**
- Godot project (project.godot, csproj/sln, app/, src/, tests/, assets/, locale/, scenes/, plugins/) lives under `project/`.
- Repo root keeps development elements: docs, notes, `docs/`, `scripts/`, and the pinned Godot runtime under `tools/godot/`.
- `scripts/validate.sh` runs restore/build/import/tests from the new layout unchanged for CI.

**Progress evidence (2026-08-23)**
- Moved project files via `git mv`; `.godot` cache regenerated inside `project/`.
- `validate.sh` now builds and runs Godot with `--path "$ROOT_DIR/project"`; Godot binary discovery still uses root `tools/godot/editors/4.7.2/`.
- Full validation green: `199/199`.

### K-017 — Bounded MZ data-directory metadata inspection

User-directed MZ slice (extends the K-016 line); stays detection/metadata-only.

**Acceptance criteria**
- Decode bounded metadata from `data/Actors.json` and `data/MapInfos.json` via a real JSON parser: entry counts plus the first 32 names, name length capped.
- Per-file size cap with truncation/oversize rejection; malformed or non-array JSON yields a per-file diagnostic instead of failing detection.
- MZ-specific encrypted assets are detected by their real extensions (`.rpgmvp`, `.rpgmvo`, `.rpgmvm`) and reported diagnostically; no decryption, no execution.
- Snapshots without the `rmmz_core.js`/`rmmz_managers.js` runtime signature are refused (MV folders cannot be inspected as MZ).
- Regression coverage for happy path, missing files, malformed JSON, non-array JSON, encrypted assets, and MV-refusal.

**Progress evidence (2026-08-23)**
- Added `MzDataDirectoryResult.Extract(GameInspectionSnapshot)` in `project/src/plugins/BuiltInEnginePlugins.cs`; JSON parsed with Godot's `Json` parser under strict bounds (2048 KiB/file, 9999 actors, 9999 maps).
- Added `TestMzDataDirectory` suite with five tests over synthetic MZ/MV game folders; suite total `205/205`.
- `bash scripts/validate.sh` — passed; `203/203` C# tests and smoke validation.

### K-019 — ConditionalBranch condition evaluation (DONE)

Implements EasyRPG `CommandConditionalBranch` (code 12010) semantics for the two condition types the deterministic core can model.

**Acceptance criteria**
- Type 0 (switch): switch state compared against ON/OFF polarity (`parameters[2] == 0` means "is ON").
- Type 1 (variable): variable vs constant or variable operand with the six CheckOperator comparisons (==, >=, <=, >, <, !=).
- Unsupported types (timer/gold/item/actor) evaluate false with a diagnostic; else path is taken deterministically.
- True path runs then-body and skips else via matching EndBranch; false path jumps to ElseBranch or EndBranch; nesting handled by depth counting, not indent.
- Regression coverage: switch polarity, false-runs-else, variable operators, var-vs-var operand with nested branch, unsupported-type diagnostic.

**Status (audited 2026-08-26) — DONE**
- Implementation is complete in `project/src/rm2k/interpreter/EventInterpreter.cs`; current suite executes the five conditional-branch regression tests.
- Current canonical validation: `All 279 tests passed`.
- Remaining boundary: timer/gold/item/actor conditions outside the modeled state remain diagnostic-only.

### K-018 — Complete MZ database inventory

User-directed MZ slice; extends K-017, stays metadata-only.

**Acceptance criteria**
- Entry counts for present optional database sections (Classes, Skills, Items, Weapons, Armors, Enemies, Troops) under the same bounds; absent sections are omitted silently (trimmed games are normal).
- System.json `switches`/`variables` name-array counts with the bounded cap.
- Physical `data/Map###.json` file count (3-4 digit numeric stems only), capped at 1000.
- Malformed or oversized optional sections produce per-file diagnostics without affecting sibling sections or detection.

**Progress evidence (2026-08-23)**
- Extended `MzDataDirectoryResult` with `SectionCounts`, `SwitchNameCount`, `VariableNameCount`, and `MapFileCount`.
- Added two inventory tests; malformed-JSON engine log lines from `Json.ParseString` on deliberately broken fixtures are expected and asserted via diagnostics.
- `bash scripts/validate.sh` — passed; `205/205` C# tests and smoke validation.

### K-055 — Bounded runtime-owned RM2K JSON save-directory slots

**Acceptance criteria**
- Write and read the existing bounded JSON simulation snapshot through an explicitly supplied save directory and slot name.
- Reject empty/invalid slot names and path traversal without touching files outside the save directory.
- Use a temporary file followed by replacement, clean up temporary files after the operation, and return I/O/validation failures as diagnostics.
- Keep this separate from original RM2K/RM2K3 `LSD` compatibility; do not overwrite original game saves.

**Validation evidence (2026-08-24)**
- Added `TryWriteFile` and `TryReadFile` to `project/src/rm2k/simulation/Rm2kSimulationSaveCodec.cs`.
- Added bounded slot round-trip/traversal regression coverage in `project/tests/core/test_game_simulation_state.cs`.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — passed with 0 warnings and 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `244/244` tests.
### K-050 — Original-format read-only LSD save model

**Acceptance criteria**
- Read original `LcfSaveData` framing from an explicitly supplied save directory and slot.
- Preserve chunk ID, length, offsets, payload bytes, and unknown-chunk count without executing save contents.
- Reject invalid slot paths, traversal, malformed/truncated framing, missing terminators, oversized files, and oversized chunks.
- Keep this reader read-only; original saves are never overwritten and no speculative Gold/party/inventory mapping is claimed.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/parser/rm2k_lsd_save_codec.cs` and `project/tests/core/test_rm2k_lsd_save_model.cs`.
- Synthetic tests cover raw unknown-chunk preservation, BER framing, traversal/absolute-path rejection, size limits, malformed headers, and missing terminators.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `261/261` tests passed.
- Save mutation, UI integration, and field-level semantic mapping remain separate follow-up work; this card does not claim full native gameplay save restoration.

### K-023 — Verified RM2K/2003 command codes

**Acceptance criteria**
- Interpreter command constants match the verified liblcf numeric table (`lcf::rpg::Cmd`).
- Parameter layouts for implemented commands match EasyRPG Player semantics (ControlSwitches 10210 mode 0=ON/1=OFF/2=flip; ControlVars 10220 [target][op][operandType][value]; Teleport 10810 map/x/y; Wait 11410 tenths of a second).
- Commands are consumed from the typed `Rm2kMap.EventCommand` model (code/int parameters/text), matching the parser output.
- Unsupported or malformed payloads produce diagnostics and are skipped safely.
- Regression tests cover each implemented command family plus loop jump-back and break-jump-past behavior.

**Progress evidence (2026-08-23)**
- Verified code table extracted from liblcf `src/generated/lcf/rpg/eventcommand.h`; parameter semantics cross-checked against EasyRPG Player `game_interpreter.cpp` and `game_interpreter_map.cpp` (CommandControlSwitches, CommandControlVariables, CommandTeleport 10810, SetupWait).
- Rewrote `EventInterpreter` on the typed model: message continuation (20110), comment continuation (22410), tenths-based waits with frame clamp, switch flip mode, variable operand type (const/var), bounded loop stack with EndLoop jump-back and BreakLoop jump-past.
- Known limitations documented in code: ShowChoice/InputNumber remain skipped pending presentation/input slices; unsupported commands remain diagnostic-only.
- Current canonical validation: `All 279 tests passed`.

### K-013 — LMU events/pages

**Acceptance criteria**
- Decode event metadata (id, name, x, y) and page metadata (trigger, priority, frequency, list framing).
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.
- Synthetic fixtures cover valid, empty, truncated and oversized payloads.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser.ParseMap` decodes bounded event IDs/names/coordinates, page metadata, page conditions, move-list presence, command-list presence, and data-only command vectors.
- `Rm2kEventCommandDecoder` enforces command, parameter, string, terminator, and trailing-byte bounds; it never executes commands.
- `TestEventInterpreter` and `test_rm2k_parser.cs` cover event/page selection, command-vector framing, malformed input, and condition decoding.
- Current canonical validation: `All 279 tests passed`.
- Remaining limit: complete RM2K field-semantic coverage for every event/page subfield is not claimed.

### K-014 — Preserve unknown LCF fields/chunks

**Acceptance criteria**
- Decode event/page structures as data only.
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser` retains unknown top-level and per-entry fields as raw bounded dictionaries with IDs, payloads, offsets, and lengths.
- `Rm2kEventCommandDecoder` retains command data as typed data objects; unsupported command codes are diagnosed by the native interpreter rather than executed during parsing.
- `Rm2kEngineRuntime.Update()` is covered by a native autorun integration test that proves Clock → Scheduler → EventInterpreter execution; map initialization now creates a bounded `VirtualFramebuffer` through `Rm2kRendererAdapter`, and `Stop()` reset coverage includes clock/presentation/framebuffer cleanup.
- `Rm2kEventScheduler` caps imported map events at 1000 and emits a bounded diagnostic when additional events are skipped; this is regression-tested.
- Regression coverage exists in `test_rm2k_parser.cs`, `test_rm2k_lsd_save_model.cs`, `test_event_interpreter.cs`, and `TestPluginDetection.cs`.
- Current canonical validation: `All 279 tests passed`.

### K-071 — Controller/touch remapping layer

**Status (audited 2026-08-26) — DONE**
- `Rm2kInputMapper` maps keyboard, joypad buttons, and bounded touch zones to engine-neutral actions.
- `Main._UnhandledInput` consumes the mapper for movement, confirmation, choices, and numeric-input confirmation without executing imported scripts.
- Custom key bindings replace defaults for the selected action; released and unbound events are ignored.
- Regression coverage: `TestRm2kInputMapper` (`3/3`); current canonical validation: `All 280 tests passed`.

### K-072 — Reusable RM2K host lifecycle

**Status (2026-08-28) — DONE for bounded lifecycle slice**
- `EnginePluginHost` accepts `Stopped → Start` and disposes the previous stopped runtime exactly once before selecting and creating a fresh runtime.
- `Rm2kEngineRuntime.Stop()` remains a full cleanup boundary; the restart test verifies cleared map/framebuffer state and a fresh clock/scheduler.
- No stopped runtime is re-initialized. The second start follows the normal selection → creation → initialization → start path.
- Regression coverage: `Test_Rm2kRuntimeCanRestartAfterStopWithFreshRuntimeState` in `TestPluginDetection`.
- Fresh canonical validation: `All 280 tests passed`.

### K-073 — Synchronize runtime sprite descriptors after movement

**Status (2026-08-28) — DONE for bounded movement/render-state slice**
- `Main._UnhandledInput` routes RM2K movement through `Rm2kEngineRuntime.TryMove()` instead of mutating `Simulation` directly.
- Successful movement rebuilds bounded player/event sprite descriptors from the current map data; blocked or invalid movement leaves descriptors unchanged.
- Regression coverage: `Test_Rm2kRuntimeMovementSynchronizesPlayerSpriteDescriptor` in `TestPluginDetection`.
- Fresh canonical validation: `All 281 tests passed`.

### K-074 — Fail-closed pending transfer parameters

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- `EventInterpreter` accepts pending transfer requests only for map IDs `1..GameSimulationState.MaxMapId` and nonnegative coordinates.
- Invalid transfer payloads produce one diagnostic and cannot overwrite an existing pending transfer.
- This remains a data-only `PendingTransfer` request; no target map is loaded or executed.
- Regression coverage: `Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState` in `TestEventInterpreter`.
- Fresh canonical validation: `All 282 tests passed`.

### K-075 — Transfer facing direction validation

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- The optional RM2K3 transfer facing parameter is accepted only for directions `2/4/6/8`.
- Transfer validation is atomic: invalid facing values do not alter the facing direction or an existing pending transfer.
- Transfers remain data-only `PendingTransfer` requests; no target map is loaded or executed.
- Regression coverage: `Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-076 — Clear confirmed choice presentation state

**Status (2026-08-28) — DONE for bounded choice lifecycle slice**
- A confirmed `ShowChoice` selection is logged and then clears `PresentationState.ActiveChoice` before the interpreter advances.
- The UI therefore cannot keep displaying or consuming a stale choice after confirmation.
- Regression coverage: `Test_ShowChoicePausesUntilSelection` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-077 — Preserve pending InputNumber state across variable conflicts

**Status (2026-08-29) — DONE for bounded input lifecycle slice**
- `EventInterpreter` pauses an `InputNumber` command when a different variable already owns the pending presentation input.
- The existing pending variable/value remain unchanged; no conflicting variable is created or mutated.
- This does not execute foreign scripts and does not broaden the bounded input model.
- Regression coverage: `Test_InputNumberDoesNotConsumePendingValueForDifferentVariable` in `TestEventInterpreter`.
- Fresh canonical validation: `All 284 tests passed`.

### K-078 — Implement bounded RM2K ChangeItems command

**Status (2026-08-29) — DONE for bounded inventory mutation slice**
- `EventInterpreter` handles verified command `10320` with five parameters: operation, item-ID mode/value, and amount operand mode/value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` subtracts; constant and variable item IDs/amounts are supported.
- Counts are clamped to `0..999999`; malformed parameters, invalid IDs/variables, negative amounts, unsupported operand types, and unsupported operations fail closed with bounded diagnostics.
- Regression coverage: `Test_ChangeItemsAddsConstantItemCount`, `Test_ChangeItemsSubtractsAndReadsVariableOperands`, and `Test_ChangeItemsClampsAndRejectsInvalidOperation` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 44/44`; `All 292 tests passed`; build and `scripts/validate.sh` passed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

### K-079 — Implement bounded RM2K ChangePartyMembers command

**Status (2026-08-29) — DONE for bounded party mutation slice**
- `EventInterpreter` handles verified command `10330` with three parameters: operation, actor-ID mode, and actor-ID value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` removes; actor IDs may be constant or variable.
- Party size is bounded to `GameSimulationState.MaxPartyMembers` (`4`); duplicate additions, absent-actor removal, invalid IDs/variables, malformed parameters, and unsupported operations fail closed with diagnostics.
- Regression coverage: `Test_ChangePartyMembersAddsConstantActor`, `Test_ChangePartyMembersRemovesVariableActor`, and `Test_ChangePartyMembersRejectsDuplicateAndInvalidActor` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 47/47`; `All 296 tests passed`; build and `scripts/validate.sh` passed.
- RGSS/XP/VX/Ace and MV/MZ remain detection/metadata-only; no foreign Ruby or JavaScript is executed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

### K-081 — Real LMU event-page decoding

**Status (2026-08-31) — DONE**

**Problem**
- `ParseStructArray` and `ReadStructFields` only materialized objects/fields when a `pCollectFields` flag was set. Nested `rpg::EventPage` arrays were read with that flag off, so `events[].pages` was always empty for real LMU files: the event interpreter, scheduler, and page-condition paths had never run against real data.
- `EventInterpreter.End` was `0`; liblcf `lcf::rpg::Cmd` defines `END = 10`.
- Page field ids carried unverified fallbacks (`0x09`/`0x08`/`0x06`, plus `0x0b` for the command list) that do not exist in liblcf.
- The nested `EventPageCondition` struct was read as if it were already field-decoded, which threw `KeyNotFoundException` and faulted RM2K runtime initialization.

**Fix**
- Struct arrays and struct fields are always materialized; the collection flag was removed.
- `EventInterpreter.End = 10` (verified liblcf).
- Page ids limited to the verified set: condition `0x02`, move_frequency `0x20`, trigger `0x21`, layer `0x22`, move_route `0x29`, `event_commands_size` `0x33`, `event_commands` `0x34`.
- Nested struct payloads are decoded through `ReadNestedStructFields` before dispatch.
- An undecodable command vector is contained per page (`command_error` + `event_commands_bytes`) instead of failing the whole map.

**Validation evidence (2026-08-31)**
- Real fixtures now decode event pages: RM2000 `Map0001.lmu` 22 pages, RM2003 `Map0001.lmu` 38 pages; RM2000 decodes every command vector without error.
- `event_commands_bytes` equals the declared `event_commands_size` on decoded pages.
- One RM2003 page contains a 5-byte BER value above 31 bits; the page is contained with a diagnostic instead of guessing the encoding.
- Regression coverage: `Test_RealMapEventPagesDecodeCommandCountsMatchingLiblcfSizes`, `Test_Rm2000RealMapPagesDecodeEveryCommandVector`, `Test_LiblcfEndCommandStopsInterpreterWithoutDiagnostic`.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 300 tests passed`, exit `0`.

### K-082 — Event-page trigger ids and undecodable page containment

**Status (2026-08-31) — DONE**

**Problem**
- `Rm2kEventTrigger` used invented values (`Autorun=0, Parallel=1, Action=2, Touch=3`). liblcf `lcf::rpg::EventPage::Trigger` defines `action=0, touched=1, collision=2, auto_start=3, parallel=4`, and EasyRPG Player compares those values directly against decoded page data. With the old enum no real autorun, parallel, or action page could ever match.
- A page whose command vector failed to decode was bridged into the runtime as an empty page and could start as if it were valid.

**Fix**
- `Rm2kEventTrigger` now mirrors liblcf: `Action=0, Touched=1, Collision=2, AutoStart=3, Parallel=4`.
- `Rm2kEngineRuntime` skips pages carrying a non-empty `command_error` and records a diagnostic instead of running them.

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_TriggerValuesMatchVerifiedLiblcfEventPageTrigger`, `Test_EventPageSelectorIgnoresOtherTriggerKinds`, `Test_RealMapPageTriggersUseLiblcfEventPageTriggerValues`, `Test_Rm2kRuntimeExecutesRealFixtureActionPages`.
- `Test_Rm2kRuntimeExecutesRealFixtureActionPages` starts the RM2K runtime on the pinned fixture, triggers a real action page, advances 20 frames, and requires interpreter diagnostics — the first test that proves real fixture commands execute end to end.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 304 tests passed`, exit `0`.
- Cross-checked against EasyRPG Player `Game_Event::AreConditionsMet`: switch A and switch B both require ON, RM2000 uses `variable >= value` while RM2K3 uses the six compare operators, and timers compare with `secs > limit`. Existing page-condition code already matches, so no change was made.

### K-083 — ControlSwitches/ControlVariables parameter layout

**Status (2026-08-31) — DONE**

**Problem**
- Both commands read `parameters[0]` as the first id. EasyRPG stores the lvalue form in `parameters[0]` (`Game_Interpreter_Shared::TargetEvalMode`), with `parameters[1]` as the start id and `parameters[2]` as the range end. Real RM2K/2003 payloads therefore decoded as start id `0` and were always rejected with `invalid range 0-…`, so no real switch or variable command ever executed.
- Verified widths from `Game_Interpreter::ExecuteCommand`: `ControlSwitches` 4 parameters, `ControlVars` 7 parameters, `ChangeLevel` 6, `ConditionalBranch` 6.

**Fix**
- `ControlSwitches` reads `[targetMode, start, end, mode]`; `ControlVars` reads `[targetMode, start, end, operation, operandMode, operand, bitfield]`.
- `TargetEvalSingle` collapses the range end to the start id, matching `DecodeTargetEvaluationMode`.
- Patch-only target modes (`IndirectSingle`, `IndirectRange`, `Expression`) stay fail-closed with diagnostics.
- Added the verified `VarOperandVariableIndirect` mode (`v[v[x]]`).

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_ControlSwitchesAndVarsUseVerifiedParameterLayout`, `Test_ControlVarsRangeTargetWritesEveryVariableInRange`, `Test_ControlVarsIndirectOperandReadsVariableOfVariable`, `Test_ControlSwitchesAndVarsRejectPatchOnlyTargetModes`, `Test_RealFixtureCommandsUseVerifiedParameterWidths`.
- `Test_RealFixtureCommandsUseVerifiedParameterWidths` asserts the pinned fixtures satisfy the verified minimum widths; `Test_Rm2kRuntimeExecutesRealFixtureActionPages` now also fails if a real control command is rejected as an invalid range or patch-only target mode.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 309 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeLevel` (10420), `ChangeHeroName` (10610), screen effects (11040/11050/11070), `CallEvent` (12330), battle commands (1009), and Maniac codes present in the fixtures.

### K-084 — Verified actor-stat, screen-effect and event-control commands

**Status (2026-08-31) — DONE**

Implemented from the verified `lcf::rpg::Cmd` table and EasyRPG `ExecuteCommand` dispatch widths:

- `ChangeLevel` (10420) and `ChangeExp` (10410), 6 parameters `[actorMode, actorId, operation, operandMode, operand, showMessage]`, using `GetActors` modes (party / hero / variable-held hero) and `OperateValue` add/subtract. Levels clamp to `1..99`, exp to `0..999999`.
- `ChangeHeroName` (10610), 1 parameter; the command string is the new name, bounded to 64 characters.
- `EndEventProcessing` (12310) ends the current frame instead of the whole interpreter.
- `FlashScreen` (11040, 6 parameters), `ShakeScreen` (11050, 4 parameters) and `WeatherEffects` (11070, 2 parameters) drive new bounded screen-effect state on `PresentationState`; the runtime ticks effects with elapsed simulation frames, and the wait flag reuses the RM2K tenths-to-frames conversion. Weather strength clamps to 2 and unknown RM2K types fold to 0.
- `CallEvent` (12330, 3 parameters) pushes a bounded nested frame for map events through an injected resolver; nested `END` returns to the caller, recursion is capped at `MaxScriptRecursion`, and common-event targets stay diagnostic-only because the LDB common-event section is not decoded yet.
- `ChangeEventLocation` (10860, 4 parameters) and `EraseEvent` (12320) mutate event position and activity through scheduler-backed hooks, so `TriggerAt` observes the new position.
- `Rm2kMap.EventPage.Trigger` comment corrected to the liblcf enum.

**Validation evidence (2026-08-31)**
- New regression coverage in `TestEventInterpreter`: level/exp party-wide, clamping, variable operand, variable-held actor id, fail-closed modes, hero name, `EndEventProcessing`, flash/shake/weather bounds and waits, presentation-absent path, nested call with return, unsupported call targets, recursion bound, event location with variable coordinates, and erase-event activation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 330 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeBattleCommands` (1009), menu/Maniac codes (5001-5005, 11610), `MoveEvent` (11330, needs move routes), `ChangeMapTileset` (11710), and battle-dependent commands.

### K-085 — RPG Maker MV data-directory and metadata parity

**Status (2026-08-31) — DONE for the bounded data-only slice**

**Scope boundary**
- MV/MZ gameplay needs a JavaScript engine. That stays blocked: card K-090 is `BACKLOG` behind the RM2K playable milestone, and repository policy forbids executing imported JavaScript. This card is data-only.

**Problem**
- Only MZ had a bounded `data/` inventory; MV was limited to `gameTitle` from `System.json`.

**Fix**
- The inventory reader is now shared: `WebDataDirectoryResult` with `MzDataDirectoryResult` and `MvDataDirectoryResult` wrappers. Each wrapper requires its own runtime signature, so an MV snapshot is never read as MZ and the reverse is equally refused.
- `MvMetadataResult` now also reports `versionId`, `locale`, `currencyUnit`, `startMapId`, `startX`, `startY`, and bounded `partyMembers` (actor ids `1..50000`, capped at four). MV stores its version as `versionId` where MZ uses `systemVersion`; both keys are read from the top-level object only, so nested keys cannot shadow them.
- No JavaScript, HTML, or native file is executed or evaluated; only bounded JSON text is parsed.

**Validation evidence (2026-08-31)**
- New `TestMvDataDirectory` suite: inventory extraction, database section counts, missing files, malformed and non-array JSON, malformed optional sections with siblings kept, encrypted assets, verified System.json keys, and mutual signature refusal in both directions.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 340 tests passed`, exit `0`.
- RPG Maker AX was evaluated and deliberately left out: no verifiable file signature is documented publicly, and the repository forbids inventing format details. It stays unsupported rather than guessed.

### K-086 — RM2K chipset passability decoding

**Status (2026-09-26) — DONE: verified chipset passability drives real movement**

This card closed the blocker that was repeated in every slice note ("chipset passability remains fail-closed").

**Verified constants (EasyRPG Player `src/map_data.h` + `Game_Map` passability helpers)**
- Passability bits: `Down=0x01`, `Left=0x02`, `Right=0x04`, `Up=0x08`, `Above=0x10`, `Wall=0x20`, `Counter=0x40`.
- Tile blocks: `BLOCK_A=0` (stride 1000, index 0), `BLOCK_B=2000` (1000, 2), `BLOCK_C=3000` (50, 3), `BLOCK_D=4000` (50, 6), `BLOCK_E=5000` (1, 18), `BLOCK_F=10000` (1, 162); block ends 2000/3000/3150/4600/5144/10144; `NUM_LOWER_TILES=162`, `NUM_UPPER_TILES=144`.
- `GetPassableMask` maps a step to `Right`/`Left`/`Down`/`Up`.
- `IsPassableTile` decides from the upper layer first and only falls through to the lower layer when the upper entry carries `Above`; the lower lookup honours the `Wall` exception for autotiles 20-23, 33-37, 42, 43, 45, 46.

**Implemented**
- `project/src/rm2k/simulation/Rm2kChipset.cs` — verified `ChipIdToIndex`/`IndexToChipId`, `DirectionBit`, `IsPassableLowerTile`, `IsPassableTile`, `BuildDirectionMasks`. Unknown tile ids, missing tables, and mismatched layer lengths fail closed.
- `GameSimulationState` keeps `PassabilityMasks` as the authoritative per-tile direction mask, adds `IsPassableInDirection`, and keeps the old `IEnumerable<bool>` `ConfigureMap` contract by mapping passable to all four directions. `TryMove` now checks the direction bit.
- `Rm2kEngineRuntime` reads `passable_data_lower`/`passable_data_upper` from the LDB chipset section, verifies the 162/144 lengths, builds masks from the LMU `lower_layer`/`upper_layer`, and configures the simulation; the stale fail-closed diagnostics are gone.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 350 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the verified bit values, block constants, chip-id round trips, direction mapping, upper-then-lower resolution, the wall autotole exception, and the fail-closed cases.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` drives real RM2000/RM2003 maps: each fixture yields walkable and impassable tiles, and a real step onto a walkable tile succeeds while a step into an impassable tile is refused.
- `TestPluginDetection` asserts the runtime decoded non-empty masks from the real fixture and no longer reports missing passability.


### K-087 — RM2K autotile animation and event counters

**Status (2026-09-26) — autotile animation DONE; counter values not implementable from verified data**

Follow-up to K-086. Same evidence discipline: nothing below was inferred from memory.

**Autotile animation (implemented)**
- Verified in EasyRPG Player `src/tilemap_layer.cpp` (Draw), `src/game_map.cpp` (SetChipset, GetAnimationType/Speed) and liblcf `src/generated/lcf/ldb/chunks.h` (`ChunkChipset`).
- liblcf field ids: `animation_type = 0x0B`, `animation_speed = 0x0C`; the project's scalar field contract matches upstream.
- `Game_Map::GetAnimationSpeed()` returns `animation_speed != 0 ? 12 : 24`, so `animation_speed` is only an animated/not flag, **not** a frame rate and **not** an on/off switch: even the zero default keeps AB autotiles cycling, just at half speed.
- AB autotiles (blocks A1/A2/B, `id < BLOCK_C`): `step = frames / speed`, then cyclic (`animation_type != 0`) `% 3`, reciprocating (`animation_type == 0`) `% 4` with `3 → 1`, i.e. 0,1,2,1.
- Block C: `step = (frames / 6) % 4` on a fixed cycle that ignores both chipset animation settings.
- Blocks D, E and F never animate.
- `frames` is the RPG_RT frame counter (`Game_System::GetFrameCounter`), which the simulation already ticks as `FrameCount`.
- Implemented as `Rm2kChipset.AnimationSpeed/ReciprocatingStep/CyclicStep/CBlockStep/ChipAnimationStep`, exposed through `GameSimulationState.ChipsetAnimationType`, `ChipsetAnimationSpeed` and `GetChipAnimationStep`.
- The runtime now selects the chipset entry by the LMU `chipset_id` instead of assuming the first chipset, which is what the Player does (`SetChipset(map->chipset_id)`). The parser stores the passability tables on the matching typed chipset entry and keeps the section-level keys for the first entry so the existing contract still holds.

**Event counters (deliberately not implemented)**
- `Game_Map::IsCounter` is verified: the upper layer must hold `>= BLOCK_F`, the id runs through the `upper_tiles` substitution table, and the entry's `Counter` bit (`0x40`) marks it. The Player uses it only to look for an action trigger across at most 3 counter tiles in a row.
- The counter *value* mechanism (plates and steps that close again) is **not** implementable: liblcf `master` has no per-map counter/chip-data array on `lcf::rpg::Map` or `lcf::rpg::MapInfo`, so there is no verified data source to decode. Implementing it would mean inventing a format, which is exactly what K-086 forbids.
- The substitution tables (`map_info.lower_tiles`/`upper_tiles`, identity via `std::iota` in `Game_Map::Setup`) come from `lcf::rpg::MapInfo`. The current resolution treats them as identity, which matches the verified default, and the tables themselves remain a separate card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 356 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the speed mapping, the reciprocating 0,1,2,1 cycle, the cyclic three-frame cycle, the block C fixed cycle, the per-block dispatch, the D-F static blocks, and the frame-counter-driven step through `GameSimulationState`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` proves both fixtures resolve their `chipset_id` to a real chipset entry with the expected animation defaults.
- `TestPluginDetection` asserts the runtime carries chipset animation values and starts on autotile frame zero.

**Lesson recorded**
- Passability and animation data belong to a single chipset entry. Reading the first entry "because it is the map's chipset" was an unverified assumption; the LMU `chipset_id` is the verified selector.


### K-088 — RM2K tile substitution tables

**Status (2026-09-26) — DONE: verified substitution applied; the tables themselves come from save files**

**Card correction**
The card originally said "LMT map-info tile substitution tables". That was wrong. liblcf `lcf::rpg::MapInfo` has no substitution fields and liblcf `ChunkMapInfo` (LMT) has no `lower_tiles`/`upper_tiles` field ids. The tables live in `lcf::rpg::SaveMapInfo` (`lower_tiles`, `upper_tiles`, 144 entries each, identity by default), so they are save-file data, not map-tree data.

**Verified resolution order (EasyRPG Player `src/game_map.cpp`)**
- `Setup` fills both tables with `std::iota` (identity), which matches the liblcf `SaveMapInfo` default, so identity is the correct behaviour for a freshly loaded map.
- Upper layer, `IsPassableTile` and `IsCounter`: `tile_id = upper_layer[i] - BLOCK_F` and then `tile_id = map_info.upper_tiles[tile_id]`, so the substitution happens **after** reducing the raw id and **before** the flag lookup.
- Lower block E, `IsPassableLowerTile`: `tile_id = tile_raw_id - BLOCK_E; tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX`. Only block E is substituted; blocks A/B/C/D are used as-is.
- `GetChipId` (terrain lookup) converts the raw id to a chip index first and only then remaps indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)`.

**Implemented**
- New `Rm2kTileSubstitution` with the verified identity default, `SubstituteLower`, `SubstituteUpper` and `ResolveChipIndex` (the `GetChipId` order). Tables whose length or entries do not fit the 144-entry range fall back to identity instead of clamping, and requests outside the range return -1 so they fail closed.
- `Rm2kChipset.IsPassableLowerTile`, `IsPassableTile` and `BuildDirectionMasks` take an optional `Rm2kTileSubstitution`; the existing overloads keep identity behaviour, so the runtime is unchanged until save data provides a table.

**Not implemented, on purpose**
- Reading the tables out of a save file. That belongs with the open save-game work (K-050 family: "semantic field mapping, save mutation"), not with the chipset parser.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 360 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the identity default, that substitution changes passability lookups, that only the verified ranges are remapped, that malformed tables fall back to identity, that out-of-range requests fail closed, and the `GetChipId` index-first order.

### K-089 — RM2K per-map terrain tags

**Status (2026-09-26) — DONE: terrain table decoded and resolved per map tile**

**Verified (EasyRPG Player `src/game_map.cpp` `GetTerrainTag` / `GetChipId`, liblcf `ChunkChipset`)**
- `terrain_data = 0x03`, an array of 162 **shorts** (324 bytes), `int16_t` in `rpg::Chipset`, defaulting to all ones.
- RPG_RT omits an all-ones table, and the Player returns terrain 1 when the table is empty, so an absent table is normal data and not a decode failure.
- The **lower** layer alone decides the terrain; the upper layer is never consulted.
- Resolution order: raw id -> `ChipIdToIndex` -> substitution for indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)` -> `terrain_data[chip_index]`.
- Out-of-bounds coordinates use chip index 0, i.e. the terrain of the first lower tile; on looping maps the coordinate wraps first.

**Implemented**
- Parser decodes `terrain_data` (0x03) with a bounded 162 x 2 byte length check, per chipset entry plus the section-level key for the first entry, and reports an unexpected length as `terrain_data_unverified_length` with its offset.
- `Rm2kTileSubstitution.GetTerrainTag` implements the verified lookup, falling back to `Rm2kChipset.DefaultTerrainTag` (1) when the table is absent or does not cover the chip index, instead of reading out of bounds the way the Player's `assert` allows.
- `GameSimulationState.TerrainData`, `LowerLayer`, `TileSubstitution` and `GetTerrainTagAt` expose it to the runtime and to event conditions.
- `Rm2kEngineRuntime` reads the terrain table of the chipset the map actually uses.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 363 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the chip-index mapping, the substitution effect on terrain, the absent and short table fallbacks, and the out-of-bounds behaviour through `GetTerrainTagAt`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies the real RM2000 chipset table has 162 entries with valid tag ids and that every lower tile of the map resolves a tag.
- `TestPluginDetection` asserts the real runtime map resolves a valid terrain tag, including out of bounds.

### K-091 — Counter tile action-trigger propagation

**Status (2026-09-26) — DONE: verified propagation over at most three counter tiles**

**Verified (EasyRPG Player `src/game_player.cpp`, `src/game_map.cpp`, liblcf)**
- `Game_Map::IsCounter`: the upper layer must hold a tile `>= BLOCK_F`, the id runs through `upper_tiles`, and the resolved entry must carry `Passable::Counter` (`0x40`).
- `Game_Map::XwithDirection` / `YwithDirection`: the tile in front, with the looping map wrap applied.
- `Game_Player::CheckEventTriggerThere` (action): check the tile in front; then while no action event was found and at most three times, if the current tile is a counter tile, step one tile further in the facing direction and check again. RPG_RT allows a maximum of three counter tiles, so four in a row stop the search.
- Layer rules differ by position and are easy to get backwards: events **in front** of the player must have `Layers_same` (`1`), events **on the player's own tile** must **not** have it.
- The walking case evaluates only `Trigger_touched` and `Trigger_collision` on the tile in front and does **not** walk counter tiles.
- liblcf `LMU_Reader::ChunkEventPage`: `trigger = 0x21`, `layer = 0x22`; `rpg::EventPage::Layers` is `below = 0`, `same = 1`, `above = 2`.

**Defect found and fixed**
- The LMU field `0x22` was decoded and stored under the name `priority`. liblcf has no `priority` field: `0x22` is `layer`. The name was wrong and the value was unusable, so the layer rules could not be implemented. It is now `layer`, carried into `Rm2kMap.EventPage.Layer`.

**Implemented**
- `Rm2kChipset.IsCounterTile` (upper id, substitution, counter flag, fail closed).
- `GameSimulationState.UpperLayer`, `UpperPassability`, `IsCounterAt`, `FrontTile` and the looping `Wrap` helper.
- `Rm2kEventScheduler.TriggerActionFacing`, `TriggerActionHere` and `TriggerTouchOrCollisionFacing` implement the three verified cases, with `Rm2kTriggerLayerRule` for the explicit same/not-same decision and `MaxCounterTiles = 3`.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 369 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the reachable event behind a three tile chain, the stop behind a four tile chain, the layer rules for front versus own tile, and that touch/collision do not walk counter tiles.
- `test_rm2k_chipset.cs` pins `IsCounterTile` including the substitution and the fail-closed cases, and `FrontTile` including the map wrap.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies both real fixtures expose a valid `layer` and `trigger` on every event page.

### K-092 — Player input drives movement and triggers

**Status (2026-09-26) — DONE: verified turn order applied to real input; host wiring still missing**

**Card correction**
The card said a successful step triggers touched/collision "on the tile in front". That is wrong. In the Player, `Game_Player::UpdateNextMovementAction` calls `CheckEventTriggerThere` (tile in front, layer same) only when the step was **blocked**, while `Game_Player::UpdateMovement` calls `CheckEventTriggerHere` (own tile, layer **not** same) when the player comes to a stop after a **successful** step. The layer rules are therefore opposite in the two cases.

**Verified ordering**
- `Game_Player::UpdateNextMovementAction`: `Move(move_dir)`, and if the player is still stopping, evaluate touched/collision on the tile in front.
- If stopping and the decision key is pressed, the vehicle toggle runs first and the action event check only runs when no vehicle was toggled.
- `Game_Player::CheckActionEvent`: touched/collision in front, then action on the own tile, then action in front continuing over at most three counter tiles; the result is the union.
- A running event page blocks movement (`Game_Map::IsRunning`).
- `Game_Map::XwithDirection`/`YwithDirection` wrap on looping maps.

**Implemented**
- `Rm2kEventScheduler.TriggerTouchOrCollisionHere` and the complete `CheckActionEvent`, complementing the K-091 entry points.
- New `Rm2kPlayerTurn`, a Godot-free class that applies one resolved input action in the verified order: refuse while paused, in a menu, or while an event page runs; a direction attempts `TryMove` and then picks the `Here` or `There` trigger path; `Confirm` runs `CheckActionEvent`.
- `Rm2kEngineRuntime.SubmitInput(Rm2kInputAction)` exposes the turn to the host and refuses input unless the runtime is running.

**Deliberate simplification**
- Vehicles and the airship are not implemented, so the vehicle toggle in front of `CheckActionEvent` cannot change anything and the action check always runs. This is recorded rather than faked.

**Still missing**
- Nothing feeds `SubmitInput` yet: `Rm2kInputMapper` is still unreferenced by the host scene, so the game cannot receive real input. That is the next card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 375 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the successful-step `Here` path, the blocked-step `There` path, the confirm path, the empty confirm, the pause and running-event guards, and that `None`/`Menu`/`Cancel` are not map steps.
- `TestPluginDetection` feeds `MoveRight` and `Confirm` into the real runtime map and asserts the position contract.

### K-093 — Godot host input routes through the verified turn order

**Status (2026-09-26) — DONE: host now uses the verified turn order**

**Card correction**
The card claimed "`Rm2kInputMapper` is unreferenced and nothing forwards input". That was wrong. `Main.cs` already constructs the mapper, configures the touch viewport in `_Ready`, and handles `_UnhandledInput` with the verified key edge rules (pressed, not echo). The real defect was narrower and worse: the host **had** an input path, but it bypassed everything K-091 and K-092 verified.

**What the host did before**
- `Confirm` computed a facing target with its own `GetFacingTarget` helper, which has no looping map wrap, then called `EventScheduler.TriggerAt(x, y, Action)`: no layer rule, no touched/collision in front, no counter tile walk.
- A direction called `Rm2kEngineRuntime.TryMove` and then `TriggerAt(mapX, mapY, Touched)` on success only: no layer rule, and the blocked-step in-front path did not exist at all.
- So the host was reachable but wrong in exactly the ways the verified Player logic is not.

**Fixed**
- The map input branch now calls `Rm2kEngineRuntime.SubmitInput(action)`, so the host inherits the verified `Here` versus `There` choice, the layer rules, the counter tile walk, the pause and running-event guards, and the map wrap in `FrontTile`.
- `GetFacingTarget` is deleted; the unwrapped direction helper no longer exists anywhere.
- Input is marked handled when the runtime consumed it, and also when a map input was consumed without moving, such as a blocked step, so it cannot fall through to the UI. `None`, `Menu` and `Cancel` stay unhandled as before.
- The message, choice and numeric-input priority order in `_UnhandledInput` is unchanged; that is the `Game_Message::IsMessageActive` gate and must stay ahead of map input.
- Removed the now unused `UniversalRPG.Rm2k.Simulation` import.

**Not covered by tests**
- The host wiring itself is a Node override and cannot be exercised headlessly without the scene. The runtime side is regression tested in `TestPluginDetection`; the `Main.cs` branch was verified by reading the resulting code path, not by an automated test.

### K-094 — Vehicles for the action-event order
`DONE` — runtime, P0, unblocked K-114

**The card's title was half the diagnosis.** `Rm2kPlayerTurn.Apply` carried the
comment *"This runtime has no vehicles, so nothing can be toggled and the action
event check always runs"* — and `Rm2kDecisionTurn.Run` sat next to it,
implemented, mutation checked, and **never called**. The vehicles were loaded and
drawn; they were never driven and never boarded. `GameSimulationState` had
**zero** vehicle wiring and the runtime kept its own `_vehicles` list.

**Implemented**
- `GameSimulationState.Vehicles` and `.Boarding`, both cleared in `Reset()`
- `Rm2kPlayerTurn.Apply` calls `Rm2kDecisionTurn.Run`, and a vehicle that takes
  the turn suppresses the action event check — a boat moored beside a sign has
  to be boardable, and the sign is on the tile the player faces
- `CanEmbark` / `CanDisembark` from the passability mask, `IsVehicleStopping`
  for the airship, `OppositeBit` for the way back

**Three real product faults the suite found**

**`TileInFront` spoke the wrong direction order.** The player speaks 2/4/6/8;
`DirectionDelta` expects 0–3. **A `8` yields `(0, 0)`** — the character's own
tile. Every boarding test "passed" without anything moving, and a player facing
up was handed a disembark onto the water they were standing on. The bridge
`LiblcfFromFacingDirection` already existed, and its own comment warns that
mixing the two silently turns a right step into a left one.

**`PassDown` is `0x01` and `PassUp` is `0x08`.** A first draft had them swapped
and wrote `0x08` for "down".

**A K-114 test held the wrong order in place.** It checked `TileInFront` with
0/1/2/3, and so agreed with itself: five assertions, every one consistent with
the same misreading.

**And a fixture that lied about itself.** `SetPassability(..., pAllowUp,
pAllowDown)` was named as walkable directions and wired `pAllowUp` to
`PassDown` — the opposite. Two tests then asserted the wrong polarity and failed
against correct code. **A fixture whose names lie about its own bits is worse
than no fixture**, because the failure points at the reader.

**Test evidence** 8 tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 rewritten in
`test_rm2k_vehicle_boarding.cs`.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`.
**Mutations** Ten rules over six runs, **9 of 10 caught**. The tenth is a harness
fault, not a semantic gap: the first runner used `$TMPDIR/m_<path>` as its
backup, which fails on the `/`, so the mutations ran **without a restore** and
the following rules tested a cumulatively broken file. `git checkout --` then
discarded the **unstaged** slice; it was rebuilt and staged immediately.

**What this does not claim:** a vehicle's own move route, hero-directed vehicle
movement, and vehicle background music. K-114 lists those.

### K-095 — Chipset source rectangles for blocks C, E and F

**Status (2026-09-26) — DONE: verified chipset rectangles resolved, no pixels yet**

**Why this slice**
`VirtualFramebuffer` deliberately stores tile ids only, so nothing is drawn yet. The verified `Rm2kChipset` work from K-086 to K-089 produced the tile-id resolution the renderer needs. This slice resolves a tile id to the chipset rectangle it is blitted from, which is the last step before real blitting, and it is fully verifiable without any graphics dependency.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`, `Draw`)**
- Block C is blitted straight from the chipset: `col = 3 + (id - BLOCK_C) / 50`, `row = 4 + animation_step_c`. `BLOCK_C_TILES` is 3, so block C occupies columns 3 to 5 and rows 4 to 7.
- Block E applies the substitution table first (`id = substitutions[tile.ID - BLOCK_E]`), then `col = 12 + id % 6, row = id / 6` for `id < 96` and `col = 18 + (id - 96) % 6, row = (id - 96) / 6` afterwards.
- Block F applies the substitution table first (`id = substitutions[tile.ID - BLOCK_F]`), then `col = 18 + id % 6, row = 8 + id / 6` for `id < 48` and `col = 24 + (id - 48) % 6, row = (id - 48) / 6` afterwards.
- Blocks A, B and D are **not** blitted from the chipset: they come from the generated caches `autotiles_ab_screen` and `autotiles_d_screen`, so this slice refuses them instead of guessing.
- The formulas require at least 30 columns and 16 rows of 16 pixel tiles, which follows from the largest computed column (24 + 5) and row ((143 - 48) / 6).

**Range detail worth keeping**
The Player guards block C with `id >= BLOCK_C && id < BLOCK_D`, not with the end of block C, so ids between 3150 and 3999 still resolve. Its passability lookup uses the same range. `Rm2kChipsetSource` keeps that on purpose so the renderer and the simulation always resolve a tile id identically; it is documented so it is not "fixed" later.

**Implemented**
- New `Rm2kChipsetSource.TryResolve` with `ChipsetRect`, plus an identity-substitution overload and `Columns`/`Rows` bounds. Unknown ids, the autotile cache blocks and unresolvable substitutions return false so callers fail closed.

**Not implemented**
- No bitmap decoding and no blitting. The pinned fixtures contain no `Chipset.png`, so there is nothing real to decode yet.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 381 tests passed`, exit `0`.
- `test_rm2k_chipset_source.cs` pins the three formulas including the `< BLOCK_D` range detail, the substitution effect on blocks E and F, the block C cycle over several chipset settings, the fail-closed set, and that every resolved rectangle stays inside the 30 by 16 chipset grid.

### K-096 — Block D autotile quarters

**Status (2026-09-26) — DONE: block D resolves to four verified chipset quarters**

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `BlockA_Subtiles_IDS[47][2][2]` (int8, `-1` means the B block supplies the quarter) and `BlockD_Subtiles_IDS[50][2][2][2]` (uint8) are static tables in the Player source, ordered top-left, top-right, bottom-left, bottom-right.
- `GenerateAutotileD`: `block = (ID - 4000) / 50`, `variant = ID - 4000 - block * 50`, refusing `block >= 12 || variant >= 50 || block < 0 || variant < 0`. Block origin is `(block % 2) * 3, 8 + (block / 2) * 4` for `block < 4` and `6 + (block % 2) * 3, ((block - 4) / 2) * 4` afterwards. Each quarter is the block origin plus its table offset.
- The Player composes autotiles from four 16x16 quarters, so a tile id resolves to four chipset rectangles, not one.

**Transcription discipline**
- Both tables were extracted mechanically from the Player source with a script instead of being typed by hand: 188 values for block A and 400 for block D, with the count, value range and first/last rows checked against the source before any C# was written.
- The same script generated the block D anchor expectations in the test, so the test cannot drift from the table it verifies.
- Tables are stored flat: four values per block A variant, eight per block D variant.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockD` returns the four `ChipsetRect` quarters for a block D tile id and refuses out-of-range ids.
- `Rm2kAutotileQuarters.TryGetBlockAQuarters` exposes the block A variant table so the block A/B composition can use it and so the transcription can be regression tested.

**Not implemented**
- The block A/B composition itself (the quarter selection combines the A and B bit patterns with the animation step) and any bitmap decoding or blitting.
- Blocks A, B and D still do not resolve through `Rm2kChipsetSource`; only the block D quarters are available, and the composition is what turns them into a drawable tile.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 387 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins ten block D anchor rows against the Player table, the block origin for all twelve blocks, all 600 block D ids resolving with every quarter inside the chipset, the range refusals, and the block A table anchors and value range.
- The first run caught a real defect: the block D variant offset used `variant * 4` while a variant spans eight values, so every variant after the first read the wrong row.

### K-097 — Block A/B autotile composition

**Status (2026-09-26) — DONE: all lower layer blocks resolve to verified quarters**

**Defect found in K-096 while reading the source for this card**
`GenerateAutotiles` packs the quarter pairs into a hash with the last quarter on top and unpacks `x` first, so the **second** value of a pair is the chipset column and the **first** value is the row. K-096 had assumed the opposite. The block D rectangle code and its anchor expectations were corrected. The K-096 test had not caught this because it verified the table, not the axis order, so the axis is now documented in the code and pinned by the A/B column range tests.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `GenerateAutotileAB`: `block = ID / 1000`, `b_subtile = (ID - block * 1000) / 50`, `a_subtile = ID - block * 1000 - b_subtile * 50`, refusing `b_subtile >= TILE_SIZE` and `a_subtile >= 47`. `#define TILE_SIZE 16` is in `src/options.h`, so the B pattern is a four bit value.
- Three passes in this order: quarters the A table leaves to the B block with `t = (b_subtile >> (j * 2 + i)) & 1` and `t ^= 3` for block 2; quarters the A table supplies with the row `animID + (block == 1 ? 3 : 0)`; and the A/B combination pass, which runs last and therefore wins.
- The Player packs the quarters into a hash and de-duplicates them; that only affects the layout of the generated cache, not the quarter values, so it is not reproduced.
- `t ^= 3` swaps the two bits of the value, so a cleared bit 0 becomes 3 and a set bit 0 becomes 2. All four B variants, chipset columns 4 to 7, are reachable, and no more.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockAB` reproduces the three passes and returns the four quarters, refusing out-of-range blocks, B subtiles, A variants and animation steps.
- With K-095 and K-096, every lower layer block now resolves: A, B and D through the autotile tables, C, E and F straight from the chipset.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 394 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins the B bit pattern per quarter, the animation step as the row, the block 2 flip in both directions, the A table supplying a quarter with the column range split, the block 1 row shift, the combination pass overriding the A table, the reachable B column set, and the range refusals.
- The test also asserts that A quarters stay in columns 0 to 3 and B quarters in columns 4 to 7, which would fail if the pair axes were transposed again.

### K-098 — Chipset bitmap decoding and blitting

**Status (2026-09-26) — DONE: the real pinned chipset decodes and blits**

**Blocker resolved by research, not by invention**
The card said this was blocked on a real `Chipset.png`. The pinned fixtures had none, but the fixtures come from the public `EasyRPG/TestGame` repository, which ships the chipset images. The right chipset was determined, not guessed: the map's `chipset_id` is `1` and that LDB entry's `chipset_name` is `World`, so `TestGame-2000/ChipSet/World.png` from the **same pinned commit** is the real chipset for the pinned LDB.

The fixture README previously stated that no image is imported. That was true while the project only parsed LCF data; it is now updated with the reason, the pinned source URL and the SHA-256, and the image is a passive, never executed asset.

**Verified (EasyRPG Player)**
- `src/cache.cpp`, the `Material::Chipset` spec: directory `ChipSet`, loaded with `transparent` true, and 480 by 256 pixels.
- `src/image_png.cpp`, `ReadPalettedData`: for a paletted PNG every colour is opaque except **palette index 0**, which becomes alpha 0.
- The real fixture is an 8 bit paletted, non interlaced PNG of exactly 480 by 256 pixels, which independently confirms the `30 * 16` tile grid derived from the chipset formulas in K-095.

**Implemented**
- `Rm2kChipsetBitmap.TryParse`/`TryLoad`: bounded paletted PNG decoding, keeping the **palette index** rather than only the converted colour so the transparency rule survives. It refuses a wrong signature, a non 8 bit depth, a non paletted colour type, interlacing, oversized dimensions, a missing or oversized palette, missing image data and unknown scanline filters instead of reinterpreting them.
- `TryBlitTile` and `TryBlitRectangle` implement the verified transparency rule: index 0 is left untouched so a background shows through, every other index is painted opaque.
- `Rm2kPixelBuffer`, a Godot free RGBA buffer, so the blit stays deterministic and testable like the rest of the rendering code.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 400 tests passed`, exit `0`.
- `test_rm2k_chipset_bitmap.cs` decodes the real fixture, checks its size against the derived tile grid, verifies that index 0 is present and that every used index is covered by the palette, checks that a blitted pixel is opaque exactly when its index is not 0, refuses rectangles outside the image, and pins the malformed input cases.
- The strongest check: every chipset rectangle that K-095 through K-097 can produce for the real chipset, over 1000 of them, is blittable inside the real 480 by 256 image.

### K-099 — Compose a full map frame

**Status (2026-09-26) — DONE: the real pinned map renders from the real pinned chipset**

**Verified (EasyRPG Player)**
- `CreateTileCacheAt` assigns each tile a sublayer. An upper layer tile goes into the above sublayer when its substituted entry carries `Above`; a lower layer tile goes into the above sublayer when its resolved chip index carries `Wall` or `Above`. The chip index ranges are the same as the passability lookup: block E through the lower substitution table plus `BLOCK_E_INDEX`, block D and block C by their stride, everything else the block number.
- The two sublayers are two drawables: `lower_layer(this, Priority_TilesetBelow + TileBelow + layer)` and `upper_layer(this, Priority_TilesetAbove + TileAbove + layer)`, with `TileBelow = 0`, `TileAbove = 100`, `Priority_TilesetBelow = 20`, `Priority_TilesetAbove = 50` and `Priority_Player = 40`. Drawables are sorted ascending, so the effective order is lower layer, then the hero, then upper layer. That is why a wall tile covers the hero.
- Without passability data the Player keeps the default `TileBelow`, which is the fail-closed case.

**Defect fixed**
`Rm2kChipsetSource.TryResolve` returned false for block E and F when no substitution was supplied, even though `Game_Map::Setup` fills both tables with `std::iota`. An absent table is the identity, so those lookups now fall back to it instead of making every block E and F tile unresolvable. The caller no longer has to build a substitution just to get the default.

**Implemented**
- `Rm2kTileZOrder` with the verified `ResolveChipIndex`, `LowerLayerSubLayer` and `UpperLayerSubLayer`.
- `Rm2kMapFrameRenderer` with `RenderLower` and `RenderUpper`, drawing each layer's below sublayer before its above sublayer and blitting every resolved chipset rectangle. The hero is deliberately not drawn: it belongs between the two calls, which is what exposes a wall tile.
- `Rm2kMapLayers` and `Rm2kChipsetTables` as the input, both Godot free.

**What the pinned fixture actually contains, now measured rather than assumed**
The real RM2000 testgame map is 20 by 15 tiles, its lower layer uses only block D and E, and its upper layer only block F. Its upper tiles are fully transparent in the real chipset, so drawing them changes nothing, and because it contains no A, B or C tile it has no animated autotile. Three of my initial expectations were wrong for that reason and were replaced by measurements of the fixture. The upper layer draw path and the animation are verified with synthetic maps instead, and the real map test now documents its own shape so those facts cannot silently change.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 408 tests passed`, exit `0`.
- `test_rm2k_map_frame.cs` pins the sublayer rules for `Wall`, `Above` and both, the fail-closed case without passability, the chip index resolution including the block E substitution, the real map rendering with its measured shape, a visible upper tile changing the tile area, a fully transparent upper tile painting nothing, animation across frame 0 and 24 for the blocks that paint, and block D not animating.

### K-100 — Runtime renders the map and the host shows it

**Status (2026-09-26) — DONE: a real RM2K game renders pixels, with a golden image baseline**

**What this delivers**
A real RM2K game directory now produces a real map image. The chain is end to end verified: the LDB chipset tables, the LMU layers, the chip id resolution, the autotile quarter tables, the real chipset PNG and the verified draw order, all against the pinned fixtures.

**Verified (EasyRPG Player)**
- `src/cache.cpp`: the chipset is read from the `ChipSet` directory, and `Cache::Chipset` goes through the standard `LoadBitmap` path, so the image name is `<chipset_name>.png` inside `ChipSet`.
- `Game_Map::GetChipsetName` supplies the name from the database, and the map selects the chipset by its own `chipset_id`, which is already implemented in K-087.

**Implemented**
- `Rm2kEngineRuntime` reads `chipset_name`, resolves `<root>/ChipSet/<name>.png`, decodes it, checks the 480 by 256 size and renders the map into `RenderedMap` with `ChipsetImage` and `RenderDiagnostic` exposed. A missing, malformed or wrongly sized image is reported and leaves the runtime **running**, because the Player treats the chipset as an asset and the simulation does not depend on it. The tile id framebuffer keeps working next to the pixels.
- `Rm2kMapPreview` uploads the pixels once per change and draws them scaled with the nearest neighbour filter, with the player marker on top and the render diagnostic when there is no image. It falls back to the tile id view when no image exists.
- `Main.cs` forwards `RenderedMap` and `RenderDiagnostic` and reports the pixel size.

**Golden image**
`rm2000/rendered/Map0001.png` is this project's own output, not upstream, and is pinned as a regression baseline with its SHA-256. The rendering test compares every byte, so a change in the chipset resolution, the autotile tables, the transparency rule or the draw order now fails the suite instead of quietly producing a different picture.

**Measured facts about the pinned map, not assumptions**
20 by 15 tiles, 320 by 240 pixels, lower layer of block D and E only, upper layer of block F only, those upper tiles fully transparent in the real chipset, 13 distinct colours, and every pixel covered because the room's floor and wall tiles are solid. Three of my expectations were wrong for those reasons and were replaced with measurements. Transparency is therefore verified per tile, and the animated blocks with synthetic maps.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 412 tests passed`, exit `0`.
- `test_rm2k_runtime_rendering.cs` renders a real game directory built from the pinned fixtures, compares it against the golden image byte for byte, checks the frame size and the colour count, and verifies that a missing or malformed chipset image is reported while the runtime keeps running and the tile id framebuffer stays available. Stopping clears the rendered map.

### K-101 — Charset geometry and character frames

**Status (2026-09-26) — DONE: verified charset geometry with a real charset fixture**

**Verified (EasyRPG Player)**
- `src/sprite_character.cpp`, `GetCharacterRect`: the cell is `24 * (TILE_SIZE / 16) * 3` by `32 * (TILE_SIZE / 16) * 4`, which is **72 by 128** with `TILE_SIZE = 16`, placed at `(index % 4, index / 4)`. Each cell holds a 3 by 4 frame grid, so one frame is **24 by 32**.
- `Sprite_Character::Draw`: `row = character->GetFacing()` and `frame = character->GetAnimFrame()`, with anything from `Frame_middle2` replaced by `Frame_middle`. liblcf `rpg::EventPage::Frame` is `left = 0, middle = 1, right = 2, middle2 = 3`.
- `src/game_character.cpp`, `UpdateFacing`: for the four cardinal directions the facing is set to the direction itself, so liblcf `rpg::EventPage::Direction` `up = 0, right = 1, down = 2, left = 3` is the sprite row directly. Diagonal directions have their own rule, which RM2K characters never use.
- The sprite offsets are `SetOx(chara_width / 2)` and `SetOy(chara_height)`, which centres the frame on the tile and puts its feet on the tile bottom.
- `src/cache.cpp` loads charset material as transparent, like the chipset.

**Fixture**
`rm2000/CharSet/Chara1.png` from the same pinned commit, added the same way as the chipset. It independently confirms the geometry: 288 by 384 pixels is exactly four 72 pixel cells across and three 128 pixel cells down, giving twelve characters.

**Refactor**
The paletted PNG decoder was generalised to `Rm2kIndexedImage`, with `Rm2kChipsetBitmap` as the chipset specific wrapper that owns the 480 by 256 contract. Charset, chipset and later picture material share one decoder and one transparency rule.

**Implemented**
- `Rm2kCharset` with the verified cell and frame constants, `FacingToRow` for the project's facing values (2 down, 4 left, 6 right, 8 up), `ClampFrame` matching the Player's `middle2` clamp, `TryGetCell`, `TryGetFrameRect` and `TryDrawCharacter` which places the feet on the tile bottom and clips at the frame edge.
- `Rm2kIndexedImage` is the shared decoder; `Rm2kChipsetBitmap` keeps the chipset size check.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 417 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the cell and frame geometry against the real image, the `(index % 4, index / 4)` cell split, the frame clamping, the facing conversion, and the drawing including the clipping behaviour: a character at tile (0, 0) is cut off above the tile bottom, and one fully outside the frame paints nothing without throwing.

### K-102 — Event sprite fields and per-stage placement

**Status (2026-09-26) — DONE: verified sprite placement, not yet wired into the runtime**

**Verified (EasyRPG Player and liblcf)**
- `src/sprite_character.cpp`: `character_name = character->GetSpriteName()` and `character_index = character->GetSpriteIndex()`, and the charset is requested from the `CharSet` directory, like the chipset from `ChipSet`.
- liblcf `LMU_Reader::ChunkEventPage`: `character_name = 0x15`, `character_index = 0x16`, `character_direction = 0x17`. The direction is an `rpg::EventPage::Direction` value.
- The drawable priorities split the characters into three stages: `Priority_EventsBelow = 30` between the map layers, `Priority_Player = 40` shared with "same as hero" events, and `Priority_EventsAbove = 60` after `Priority_TilesetAbove = 50`.

**Parsing gap found and fixed**
The parser declared `character_name` and `character_index` for actors (chunk `0x03`/`0x04`) but never for event pages, even though the ids `0x15`/`0x16` are verified in liblcf. Event sprite data was therefore not available at all. The parser now decodes `character_name`, `character_index` and `character_direction` for every event page, and a missing name yields an empty string, which is what a page without a character graphic means.

**Implemented**
- `Rm2kCharacterSprite` with the verified `StageForLayer` for the three page layers and `FacingFromLiblcfDirection` for the direction, plus a `Skipped` flag so a caller can report a character that could not be drawn.
- `Rm2kMapFrameRenderer.RenderSprites` draws one stage at a time, so the caller can interleave the stages with the two map layers in the verified order, and reports how many were drawn. A character index beyond the charset capacity is skipped and flagged, never taken from an arbitrary cell.
- `Rm2kMapFrameRenderer` can now be created without a chipset for sprite only passes; tile drawing then does nothing instead of throwing.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 419 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the layer to stage mapping, the liblcf direction to facing conversion and the fact that the two conversions are inverse, and checks that each stage draws only its own characters, that an out of range index is skipped and flagged, and that the below stage and the hero stage paint different characters.

### K-103 — Hero and events in the runtime frame

**Status (2026-09-26) — DONE: the hero and the events are drawn in the verified order**

**Verified (EasyRPG Player)**
- `src/game_player.cpp`, `Game_Player::ResetGraphic`: `auto* actor = Main_Data::game_party->GetActor(0)` and, when it is null, `SetSpriteGraphic("", 0)`. With an actor it calls `SetSpriteGraphic(ToString(actor->GetSpriteName()), actor->GetSpriteIndex())`. The hero therefore has no page of its own: it is the **first** party member.
- `src/game_actor.h`: `GetSpriteName()` returns the runtime override `data.sprite_name` when it is non empty and otherwise falls back to `dbActor->character_name`; `GetSpriteIndex()` uses `data.sprite_id` in the same case. `SetSprite` clears the override when the requested graphic equals the database values, so a fresh game always draws the LDB graphic and no override has to be invented.
- `src/game_party.cpp`, `Game_Party::SetupNewGame`: `data.party = lcf::Data::system.party`, so the leading actor id is the first entry of the LDB system party list.
- The stage split and the per-stage draw call already existed from K-102; this card only wires it up.

**Parser gap found and fixed**
`LoadCurrentMapEvents` decoded the trigger, the layer and the conditions but never copied `character_name`, `character_index` or `character_direction` into the page, although K-102 had verified the liblcf ids `0x15`/`0x16`/`0x17`. The event sprites were therefore unreachable at runtime. The page now carries all three, and the liblcf direction is converted to this project's facing instead of being stored raw.

**LDB system chunk was not decoded at all**
The hero resolution needs the starting party, and `system` was only a raw chunk. Verified against liblcf `src/generated/lcf/ldb/chunks.h` `struct ChunkSystem` and `src/generated/ldb_system.cpp`: the party list is the size/data pair `party_size 0x15` plus `party 0x16`, and the three vehicle graphics are the scalars `boat_name 0x0b`, `ship_name 0x0c`, `airship_name 0x0d` with `boat_index 0x0e`, `ship_index 0x0f`, `airship_index 0x10`. `DecodeLdbSystem` types those and keeps every other field in `unknown_fields` with its count and framing. A database without a system chunk yields liblcf's empty defaults instead of failing, because that is what a fresh empty database means.

**Defects found while implementing**
- The first `system` decoder overwrote the seeded defaults, so a database without the chunk lost every default key. It now only reports the fields the chunk actually carries and the caller merges them.
- An LCF string field is the raw encoded text; the first test built a length prefix, which the shared decoder does not strip. The test was corrected, not the decoder.
- The declared party size can exceed the stored data, so the list is clamped to `min(declared, data.Length / 2)` and never reads past the chunk. `MaxSystemArrayEntries = 4096` bounds a malformed size field.

**Render order defect found**
`RenderCurrentMap` ran before `LoadCurrentMapEvents`, so the first frame was rendered with an empty event list. The events are now loaded before the framebuffer and the first render. Without this the whole card was silently inert: the suite stayed green and the golden image matched, because nothing was drawn at all.

**Test fixture defect found**
`CopyRealGame` copied `ChipSet` but never `CharSet`, so no character could ever be drawn and the failure hid behind a missing-file diagnostic. The charset is now part of the copied fixture. The constant also needed the `FixtureRoot` prefix, because `GlobalizePath` does not resolve a bare relative path.

**Measured, not assumed**
The pinned LDB has **no starting party** (`party` decodes to an empty list), so the verified `GetActor(0) == null` path applies and this particular game draws no hero graphic. That is the correct result, and the test asserts it instead of inventing a hero. The frame therefore contains the chipset plus the event characters: 85 colours instead of the 13 the chipset-only frame of K-099 produced, and 20 character figures, confirmed by inspecting the rendered image.

**Implemented**
- `Rm2kHeroSprite.FromActor` with the verified fallback and the `CharSet/<name>.png` file name.
- `Rm2kEngineRuntime` builds the frame in the verified order: lower layer, below events, hero plus same-layer events, upper layer, above events. A charset that is missing or undecodable is reported and skips only its characters.
- The page graphic fields are filled in `LoadCurrentMapEvents`, and a skipped character is reported instead of drawing from an arbitrary cell.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 425 tests passed`, exit `0`.
- `test_rm2k_parser.cs` pins the system chunk party list, the three vehicle names and indices, the unknown field count, the empty defaults without a system chunk, and the clamp to the stored data.
- `test_rm2k_runtime_rendering.cs` pins the character drawing (more colours than the chipset-only frame, no missing-charset diagnostic), the hero resolution from the first party actor including the null case, and that a missing charset is reported while the map still renders.
- The golden image is regenerated and re-pinned with SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`; the byte-for-byte comparison is green again.

**Not implemented**
- No movement animation: the hero and the events are drawn with the static middle frame, so the walk cycle is not exercised.
- The hero is not re-rendered after the player moves; the frame is produced once during initialization.
- The `frame_name` and the transparency level of an actor are decoded but not applied.

### K-104 — Re-render the frame when the player moves

**Status (2026-09-26) — DONE: the frame follows a move, tiles stay cached**

**Verified (EasyRPG Player)**
- `src/scene_map.cpp`, `Scene_Map::vUpdate` → `UpdateStage1` → `UpdateGraphics()` once per frame, and `PreUpdate`/`PreUpdateForegroundEvents` call it again. So the update is per frame, not per input.
- `src/spriteset_map.cpp`, `Spriteset_Map::Update`: the tilemap only receives `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))` and `SetOy(...)`, i.e. a scroll offset. The tile layers are **static sprites that are not re-rastered on movement**; only `character_sprite->Update()` and the tone change per frame.
- Note the class is spelled `Spriteset_Map`, not `SpriteSet_Map`. An earlier probe with the wrong casing silently matched nothing, which is why the order of verification matters.

**Design consequence**
The map is rastered once into two cached layer buffers, and only the characters are re-composited. This matches the Player instead of re-rastering a whole map per step, and it keeps simulation and presentation separate: `RecomposeFrame` runs inside `Update` on a simulation frame boundary, never per rendered frame, so a higher display frame rate cannot change the simulation.

**Composition order** (`RecomposeFrame`)
1. copy of the cached lower tile layer,
2. below-layer event characters,
3. hero and same-layer event characters,
4. the cached upper tile layer laid over them,
5. above-layer event characters.

**Three defects found and fixed while implementing**
- `PaintOver` first copied every byte, including alpha 0, so the upper layer erased the lower layer and the whole floor. It now keeps the destination pixel where the source is transparent, which is the same rule the verified chipset blit uses. The K-099 golden test caught this immediately.
- The upper layer was originally rastered into the same buffer as the lower layer, so it carried the lower layer with it and covered every character. It is now rastered into its own buffer.
- The character pass drew onto an **empty** buffer instead of the lower layer, which dropped the floor entirely (`opaque=6513` instead of `76800`).

**Defect in the existing API found by the new test**
`RenderSprites` threw on a null map although it never reads the map: a character is placed by its own tile coordinates and the Player's character sprites are independent of the tilemap sprite. The parameter is now nullable and documented, so a sprite pass does not have to invent a map.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 427 tests passed`, exit `0`.
- `TestRm2kRuntimeRendering 9/9`, `TestRm2kCharset 7/7`.
- The strongest check: the rendered frame is **byte identical** to the K-103 golden image, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`, with the same 85 colours and 76800 opaque pixels. The refactor therefore changes no output, which is what makes the caching safe to keep.
- New tests pin that moving a character to another tile changes the composited frame, and that `PaintOver` respects transparency and refuses a mismatched buffer.

**Not implemented**
- The frame is still a full-map buffer, not a camera viewport. The Player scrolls by offsetting the two layer sprites; this runtime still renders the whole map, which is correct but not yet efficient.
- Movement is still a single discrete step: there is no walk animation, so the hero jumps from tile to tile and the frame is invalidated per completed step.
- Events have no move routes, so only the hero position changes between frames.

### K-105 — Camera viewport instead of a full-map frame

**Status (2026-09-26) — DONE: the camera is applied and the suite proves it**

**What is implemented**
- `project/src/rm2k/rendering/Rm2kMapCamera.cs`: `DefaultPanX`/`DefaultPanY` (9 and 7 screen tiles at 320x240), `PositionX`/`PositionY`, `OffsetPixelsX`/`OffsetPixelsY` and `PositiveModulo`, all as pure calculations.
- `Rm2kPixelBuffer.TryCopyRegion` reads a window out of a cached layer and **refuses** a region that does not fit instead of clipping it.
- The runtime frame is screen sized (320x240) instead of `width * 16` by `height * 16`. `RecomposeFrame` cuts the cached layers at `ResolveCameraOffsetX`/`ResolveCameraOffsetY` and gives the characters the same offsets through `Rm2kCharacterSprite.PixelOffsetX`/`PixelOffsetY`.
- `AppliedCameraOffsetX`/`AppliedCameraOffsetY` expose what the runtime actually applied, so a test can assert the wiring and not only the arithmetic.

**Verified (EasyRPG Player)**
- `Game_Map::GetDisplayX` = `map_info.position_x + shake * 16`, so the stored position is already the scroll offset. Screen shake is deliberately not implemented: it is presentation state and would couple a cosmetic effect to the deterministic core.
- `Game_Map::SetPositionX`/`SetPositionY` clamp to `[0, tiles * SCREEN_TILE_SIZE - screen_width]` or apply `Utils::PositiveModulo` when the map loops. The source says `std::clamp` must not be used, because for a map smaller than the screen the lower bound exceeds the upper bound.
- `Game_Player::GetDefaultPanX` = `ceil(screen_width / TILE_SIZE / 2) - 1) * SCREEN_TILE_SIZE`.
- `Spriteset_Map::Update` does `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))`, which is a **division** by 16. This is `OffsetPixelsX`. Multiplying by 16 instead was the first attempt and put the viewport 16 times past the end of the map; the bound proves the direction: the last column of a 40 tile map is 7680 screen tiles, and 7680 / 16 = 480, which is inside the 640 pixel map, while 7680 * 16 = 122880 is not.

**Two upstream unit mixes, both reproduced and pinned**
- `SetPositionX` counts the map extent in screen tiles but the screen in pixels, so a map exactly one screen wide in pixels still has a positive bound (`20 * 256 - 320 = 4800`) and still scrolls.
- The same mix means the reachable offset on a 40 tile map is 480 pixels, which is 160 more than a 320 pixel window needs. The excess is the Player's black border, and `CopyViewport` leaves it unpainted rather than reading past the layer. A test that demanded `offset + screen <= map` was wrong and was corrected.

**Unblock condition met**
A synthetic 40 by 30 map (640 by 480 pixels) is written by the test with the verified LMU field ids (`0x01` chipset, `0x02` width, `0x03` height, `0x47` lower, `0x48` upper, `0x51` events) and event characters at known tiles. Mutation A, forcing the applied camera offset to zero, now **fails** the suite, which it did not before this card. The tile arithmetic in `Rm2kMapCamera` was already unit tested; what was missing was a test that reaches the runtime wiring.

**Still not implemented, and why it matters**
- The scrolled frame is **not** compared pixel by pixel. `GameSimulationState.TileSubstitution` is never populated, so a synthetic map's floor does not render, and the frame contains only the characters. Comparing pixels would compare an empty floor, so the test asserts the applied offsets instead. Populating the LDB tile substitution is the prerequisite for the pixel comparison and is the next card.
- Loop horizontal/vertical flags are exposed on the camera API but never set by the runtime, because the map loop fields are not decoded.
- No screen shake and no configurable resolution: `RenderProfile` is a scaling policy, not a screen size. The renderer uses `Rm2kMapCamera.DefaultScreenWidth/Height` as named constants.
- The above-layer compositing is only covered where the upper layer is transparent in the fixture, so its opacity rule is untested in the runtime.

### K-106 — Block E passability offset and the two-sided movement check

**Status (2026-09-26) — DONE: the premise was wrong, and chasing it found two real bugs**

**The premise in this card was false, and that is the first result**
The card assumed `GameSimulationState.TileSubstitution` was never populated because the LDB chipset carries `lower_substitution_ids` and `upper_substitution_ids` that had to be decoded. Verified EasyRPG `Game_Map::Setup` says otherwise:

```
std::iota(map_info.lower_tiles.begin(), map_info.lower_tiles.end(), 0);
std::iota(map_info.upper_tiles.begin(), map_info.upper_tiles.end(), 0);
```

Both tables start as the identity and are only ever changed by `SubstituteDown`/`SubstituteUp`, which exist for the tile substitution event commands. There is no LDB field to decode, so `Rm2kTileSubstitution`'s identity fallback was already correct, and `Validate` checking both tables against 144 entries is also correct because `BlockEEnd = BlockE + 144` and `BlockFEnd = BlockF + 144`. Nothing needed populating. The card was rewritten once that was proven, and `chunks.h` was checked for `lower_tiles`/`upper_tiles` to confirm they are not LDB chunk fields at all.

**Bug 1 — block E passability read the wrong entry**
`Rm2kChipset.IsPassableLowerTile` applied `+ BLOCK_E_INDEX` only when a substitution object was present. Verified `Game_Map::IsPassableLowerTile` applies it unconditionally, because a missing table is the identity, not a skipped offset:

```cpp
tile_id = tile_raw_id - BLOCK_E;
tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX;
```

Without the offset a block E tile read passability entry 0 instead of entry 18, so every block E tile in every game inherited the first autotile's passability. In the pinned EasyRPG TestGame that turned 20 tile ids into whatever entry 0 said. Regression: `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`, which pins the contract with and without a table and proves block C is unaffected. RED was `TestRm2kChipset: 23/24`.

**Bug 2 — movement only checked the target tile**
`GameSimulationState.TryMove` checked `IsPassableInDirection(targetX, targetY, directionBit)` and nothing else. Verified `Game_Map::IsPassable` computes two masks:

```cpp
const int bit_from = GetPassableMask(from_x, from_y, to_x, to_y);
const int bit_to   = GetPassableMask(to_x, to_y, from_x, from_y);
```

`bit_from` is the direction leaving the current tile and is tested against the current tile. Only testing the target let a player walk out of an impassable tile and made one-way tiles wrong in both directions. `TryMove` now checks the current tile with `directionBit` and the target with the reverse bit. Regression: the blocked-tile case in `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` now uses a two tile strip with a real blocked id from the decoded table.

**A test that was pinning the bug**
`Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` asserted `blockedTiles > 0` on the map itself. That only held because of bug 1, which pushed block E tiles onto entry 0. With the bug fixed the EasyRPG map legitimately resolves to passable entries only, so the assertion was rewritten to ask the chipset table for a blocked id and to build the blocked strip from real decoded data. Asserting "this specific map has a wall" was a false claim about a fixture, not a requirement.

**A fourth real defect found on the way: the fixture's upper layer was hiding the lower layer**
The synthetic wide map filled the upper layer with tile 0, which is a block A autotile and paints over the floor. The pinned fixture uses tile 10000 (block F), which is transparent. With tile 0 the frame was 13 colours; with 10000 it is 58 and the floor is visible. The wide map test now uses the fixture's own id and asserts the pixel difference.

**Mutation evidence, all four detected**
- block E `+ BLOCK_E_INDEX` removed: caught by `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`.
- the source tile check removed from `TryMove`: caught by `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` for rm2000 and rm2003.
- camera offset forced to 0: caught by `Test_AMapLargerThanTheScreenScrollsWithTheCamera`.
- `RecomposeFrame` removed from `TryMove`: **caught now**, by the pixel comparison. Before this card the same mutation passed the suite, because the test used the test hooks and recomposed on its own.

**Validation:** build 0 errors/0 warnings; `All 441 tests passed`, exit 0; `TestRm2kChipset 24/24`; `TestRm2kRuntimeRendering 13/13`; pinned golden image unchanged.

**Mostly closed by K-107**
The walk animation and the per frame step budget are implemented, tested and mutation checked, and the event facing bug is fixed. What is still open is the move route: events cannot follow `move_route` at all, so the per frame budget only runs for the player. That is the remaining gap between "the hero walks" and "playable".

### K-107 — RM2K character walk animation and the per frame step budget

**Status (2026-09-26) — VERIFY. The animation and the step budget are implemented, tested and mutation checked. The move route is not started, and one piece of sprite wiring cannot be covered by the available fixture.**

**The animation, verified from upstream**
`Game_Character::UpdateAnim` counts `anim_count` once per update and only advances the visible frame when a per speed threshold is reached: stationary `{12,10,8,6,5,4}`, continuous `{16,12,10,8,7,6}`, spin `{24,16,12,8,6,4}`, indexed by a one based speed. `IncAnimFrame` is `(anim_frame + 1) % 4` and resets the count. A character cell has three columns, and `Sprite_Character::Draw` clamps `Frame_middle2` back to `Frame_middle`, so the fourth rotation value is deliberately drawn as the middle frame. Cycling over three frames would animate at a different rate, so the four value rotation is a test of its own.

**The thresholds overlap.** At the default move speed 3 the stationary limit 8 is reached before the continuous limit 10, so the frame advances on the eighth tick, not the ninth. Four of my first test expectations were wrong about this; the reader was right every time.

**The step budget, verified from `Game_Character::Move`, `UpdateMovement` and `GetSpriteX`**
A move is not a tile snap. `Move` sets the logical tile to the target immediately and sets `remaining_step` to `SCREEN_TILE_SIZE`, 256. `Update` subtracts `1 << (1 + move_speed)` per update, so at the default move speed 3 a tile takes exactly sixteen updates. The drawn position is `GetX() * 256 - remaining_step` for a move to the right, which is what makes the sprite walk across the tile it just entered. `UpdateMovement` clamps at zero so an overshoot cannot wrap the sprite across the map. `GetMaxStopCountForStep` is `1 << (9 - freq)` and 8 or more means no wait, and it uses the move **frequency**, not the move speed, so a character can be slow and still start the next step immediately.

**Implemented**
- `Rm2kStepBudget` with the movement amount, the stop count tables, `Advance`, the `SpriteX`/`SpriteY` formulas and the pixel offsets.
- `GameSimulationState.RemainingStep`, filled by `TryMove` and spent by `UpdateCharacterAnimation`, cleared by `Reset`.
- The runtime's `Update` advances the character once per simulation tick and recomposes the frame while a step is unspent, and the hero sprite carries the step offset and the animation frame.

**A real reset bug this card found, the same class as K-106**
`Reset` did not clear `RemainingStep`, so a new game inherited a half finished step. The regression test found it by driving the real state rather than a fresh instance.

**A real sprite wiring bug this card found**
`BuildCharacterSprites` assigned the camera offset onto the hero sprite, overwriting the step offset `TryBuildHeroSprite` had just set. The step budget therefore reached the state and never reached the renderer, so the hero would have snapped to its tile while walking. The offsets are now added, not replaced.

**A real rendering bug found on the way, unrelated to the animation**
`FacingFromLiblcfDirection` read its argument as a one based axis (`1 => 6, 3 => 4, 2 => 2`) while its own comment documented the real one, `Game_Character::Direction`: `Up = 0, Right = 1, Down = 2, Left = 3`. Every event facing sideways drew mirrored, and nothing caught it because the fixture only has events facing down. Now on the verified axis, pinned by a permutation test over all four directions.

**The golden image changed, as a correction**
Wiring the LMT `character_pattern` through made the fixture's events render their real stored pose 0 instead of the runtime default 1. The difference is confined to the character bands, y 37 to 159, across 48 sixteen by sixteen cells, and the colour count rose from 85 to 92. The old golden encoded the default rather than the game data, so it was replaced after that analysis.

**Tests.** `test_rm2k_step_budget.cs`, 11 cases, covering the movement amounts, the sixteen updates per tile, the clamp, the per update pixel positions, each direction on its own axis, the stop count tables and their independence from the move speed, and the range refusal. `test_rm2k_character_animation.cs`, 10 cases. Two runtime tests: one drives the real `Update` path over the wide map and compares composed frames, one records that an empty party yields no hero sprite.

**Mutation evidence.** Detected: the movement amount shifted to `1 << speed`, the clamp removed, the step offset sign flipped, `RemainingStep` not filled by `TryMove`, `RemainingStep` not cleared by `Reset`, the per frame animation tick loop removed, the frame count changed from four to three, the modulo changed to three, the stationary guard changed, the move speed range check removed, the left and right facings swapped, and the out of range fallback changed.

**Two mutations reported as not mutant, recorded rather than chased.** Moving the direction mapping to a one based axis, and deleting the `0 => up` arm, both leave the function identical on every input because `0` was already handled by the `_ => up` fallback.

**Closing the verification gap: a starting party fixture**
The hero sprite's step offset was not mutation covered, because the pinned LDB defines an empty party, so the verified `ResetGraphic` path yields no hero and the hero sprite is never built. `Rm2kPartyFixtureBuilder` now appends the missing system section to a **copy** of the pinned database, leaving the fixture itself untouched.

Three encodings had to be right, and two of them are not obvious:
- A struct field is `id + length + payload` with both numbers in BER.
- `party_size` (0x15) is a **signed BER integer**, because the library reads it through its signed BER decoder.
- `party` (0x16) is a packed list of **two byte little endian** values, because the library reads it as `(short)(lo | hi << 8)`.

Writing either payload in the other's encoding parses and then reports trailing bytes, or parses and reports an empty party, which is the exact failure the builder exists to prevent.

The section is **appended**, not inserted. liblcf's `Struct<S>::ReadLcf` loops until EOF and breaks on each section's own terminator, and when a nested struct reads fewer bytes than the chunk declared it seeks to `off + length` and logs a corruption warning rather than trusting the inner walk. Our parser does the same, so a database is a sequence of terminated sections and one more is simply appended. Inserting at the first terminator lands inside the actors section, whose declared length is an upper bound that the reader seeks past.

**With a hero present the wiring is now covered, and two more assertions were needed**
- The composed sprite offset is the **camera scroll plus the step**, so the test asserts the difference against `AppliedCameraOffsetX/Y`. Asserting the composed value directly would only have asserted the camera.
- The hero is identified by its charset cell index and map position, not by composition order: an event can share the layer and the tile, and a wide map fixture has both.
- The animation frame needed a second assertion, because after one update the frame has not moved yet. Driving the step forward proves it changes, and driving the rotation to its fourth value proves the `ClampFrame` is applied where the sprite is built rather than only in the charset.

**Mutation evidence for the wiring, all now detected:** the hero's step offset not computed, the camera offset overwriting the step offset instead of being added, the animation frame not assigned at all, and the animation frame assigned without the clamp. Before the party fixture existed, the first two of these escaped.

**Still open on this card**
There is no move route. Events cannot follow `move_route` at all, so the per frame budget only ever runs for the player. That is the remaining gap between "the hero walks" and "events walk", and it is now a card of its own.

### K-108 — WOLF binary .mps reader, built from the verified format

**Status (2026-09-26) — DONE for the map format. WOLF is still not playable; see the honest gaps below.**

**What the previous entry found, and what this entry did about it**
The WOLF reader was JSON-only, so no real WOLF game could ever load. This card implements the verified binary `.mps` map format. The user was asked how to proceed and chose: build the binary readers, no real game is available, so validate against the specification.

**Implemented**
- `project/src/wolf/WolfBinaryMapData.cs`: `WolfBinaryMapData`, `WolfBinaryMapPixel`, `WolfBinaryEvent`, `WolfBinaryEventPage`.
- `project/src/wolf/WolfBinaryMapReader.cs`: `HasMapHeader` and `Read`, plus a bounded little endian `WolfByteCursor` where every read is checked, so a truncated or hostile file yields a diagnostic instead of an out of range access.
- `WolfDataReader.LooksLikeJson` still reports a non JSON payload as an unimplemented binary format rather than blaming the JSON parser. That was the honest-rejection part of the previous entry and it stays.

**Verified format facts used, none guessed**
- Header: ten zero bytes, `WOLFM`, a zero byte, a version header byte (0x00 v2, 0x55 v3), three zero bytes, a u4 that must be 0x64, then a version byte that must be 0x65 (v2) or 0x66 (v3).
- Then a length prefixed title (u4 byte count then the bytes, decoded as Shift-JIS), tileset id, width, height and event count, all u4.
- The map body is a first pixel u4. A value of 0xFFFFFFFF means the map does not exist and **no body follows**; otherwise the body is width * height * 12 bytes read as width * height mappixels of three u4 values each.
- A mappixel's first u4 carries the autotile id as raw / 100000 and the four corner modes as raw % 10000 / 1000, raw % 1000 / 100, raw % 100 / 10 and raw % 10.
- An event starts with 0x6F, a u4 that must be 0x3039, its id, a length prefixed title, map x, map y, the page count, a zero u4, the pages, and a 0x70 footer.
- An event page starts with the five byte signature 79 FF FF FF FF. The reference implementation derives the icon row as (byte >> 1) - 1.
- The map ends with a 0x66 footer.

**A real bug the test caught: a signed/unsigned comparison**
`ReadUInt32` returns `uint`. The first pixel was cast to `int` and compared against the literal `0xFFFFFFFF`, which C# types as `uint`. So `firstPixel` was `-1` and the literal was `4294967295`, the comparison was never equal, and **every map that does not exist decoded as if it had a pixel body**. That shifted the whole file and produced a wrong error, "ends at byte 53 but 57 were needed", which pointed at the reader instead of at the comparison. Fixed by keeping the value in unsigned space. This is the kind of defect that a green test suite with a hand written JSON fixture would never have found.

**A fixture bug found the same way**
The test's `BuildMap` wrote the two base tile values even when the first pixel was 0xFFFFFFFF, which cannot happen in a real file because the format skips the body entirely. The fixture now follows the same rule, so it cannot encode a frame the editor could not produce.

**Tests:** `project/tests/core/test_wolf_binary_map.cs`, 10 cases, all building bytes from the specification rather than from the reader's own output: full field decode, the autotile digit split with all four digits distinct, the non existing map, the event framing, a foreign magic, a wrong event signature, a missing footer, an out of range dimension refused before allocation, a truncated file refused with a byte offset, and an unknown version refused rather than guessed. `TestWolfRuntime` keeps the JSON rejection test.

**Mutation evidence, all three detected:** the signed/unsigned comparison restored, the footer check removed, and the header check removed. Each fails the suite.

**What this card does not claim**
- **No real WOLF game has been parsed.** The framing and the field order are proven against the specification; the interpretation of any single field is not. The next real game this runtime is pointed at is the first genuine test of that.
- The event page body after the signature is only partially decoded: graphic, trigger, move speed, frequency and route. **The command list is not decoded.** A wrong command count would desynchronise every following event, so it is a separate card rather than a guess.
- `database_dat`, `commonevent_dat` and `game_dat` are not implemented. The JSON reader still covers those, so the runtime cannot load a real project end to end.
- Games ship inside a DXLib archive and are frequently compressed or encrypted. The per version keys are published in clear text, so decryption is technically possible, but it is a separate decision and this card does not take it.
- There is no WOLF renderer. Nothing here draws a map.

### K-109 — WOLF event command list, decoded from the verified signature table

**Status (2026-09-26) — DONE for the core command set. WOLF is still not playable.**

**Why this card existed**
A command list with a wrong length desynchronises every following command, every event and every map, so the list is the one part of the WOLF format that must not be guessed.

**An important correction to the source material**
The published `event_command` description carries the header comment "event_command-related structures, **not used for file parsing**". It defines the sub-structures of individual commands but not the generic command frame. An earlier attempt inferred a frame of a **big-endian** signature u4 plus a padding byte from the map parser. **That inference was wrong and the card's original claims below have been corrected.**

**The command frame, as the schema actually describes it**
The frame is `param_count` (`u1`), then `command_type` (`u4` **little-endian**) when `param_count` is nonzero, then a parameter block whose shape belongs to the command, then `branch_depth` (`u1`), `string_count` (`u1`), that many strings, `have_route` (`u1`) and, when set, the route data. A `param_count` of **zero terminates the list**; it is not a parameterless command. The signature and the big-endian reader were removed, and `WolfByteCursor.ReadUInt32BigEndian` is no longer used for the command type.

**Implemented** `WolfBinaryEventCommand` with the verified command type and a name only where the schema gives one, `WolfEventCommandReader` decoding `param_count`, the little-endian type and the type specific parameter block, and `WolfMoveRouteReader` for the optional route.

**Real spec errors found by the tests**
- A command list written as zero bytes is not a list of parameterless commands: zero is the terminator, so such a fixture desynchronised everything after it.
- The `NumberCondition` and `CallCommonByName` layouts in the earlier attempt were guesses. They are now either decoded from the schema or refused.
- `CallCommonByName = 59` was invented. The verified type is **300 (0x12C)**.
- Operation names for the type `121` variants by parameter count were guessed and have been **removed**; those commands are distinguished only by their verified type and parameter count.

**Unknown command types stop the read instead of being skipped**
Skipping an unknown command would shift every following one, so the reader refuses and reports the type. A command this runtime does not implement is a diagnostic, not a silently missing line of a game's script.

**Tests:** `project/tests/core/test_wolf_event_command.cs`, 10 cases, every byte sequence built from the schema's command envelope rather than from the reader's output. `test_wolf_common_event.cs` covers the file header and the list, and `test_wolf_move_route.cs` covers the self-describing route entries.

**Mutation evidence:** the command type read big-endian, the `param_count` not consumed, a route argument count taken from a type table instead of from the file, and each option bit of a route's behaviour and option bytes swapped independently were all detected. The type table mutation is the important one: it proves unknown route entries stay readable.

**What this card does not claim**
- The command frame is read and its types are known, but a command's **meaning** is not implemented. A real event that uses a command this runtime does not interpret stops the read with a precise diagnostic.
- The command list is decoded **as data only**. No WOLF command executes. `WolfEventVm` still runs the JSON command model, not these bytes.
- The transfer format is not read at all, and there is still no WOLF renderer.
- Still no real WOLF game has been parsed. Framing and field order are proven against the schema; the meaning of a command is not.

### The real games on this machine, and what they prove

**`E:/RPGMakerGames` holds five finished games, and they are read now.**

| Game | Engine | What it proves |
|---|---|---|
| Dragon Destiny | **RM2K** | 416 kB `RPG_RT.ldb`, **743 maps** numbered 1..743 with no gap, 22 chipsets, 18 backdrops |
| MicroQuest | **XP** | 23 `.rxdata` files, 23 maps, `Scripts.rxdata` at 109 kB |
| Camellia Coronation | **MZ** | `data/*.json`, 8 `js/` files |
| dungeon5min, Kaiju Girlfriend | **WOLF** | **`Data.wolf` is encrypted** — see below |

**`TestRealRm2kGameData` 4/4 and `TestRealXpGameData` 4/4, 1623/1623,
validator passed.** The RM2K parser reads this game's own database and its
537 kB map; the marshal reader reads every one of the XP data files. **These
are the first non-synthetic tests in the project, and a fixture could not have
told either reader that a real file is not a small one.**

**And a number in a test had to be measured, not written.** The map count was
asserted as 700 and the game has 743 — **a rounded guess in a test that
exists to prove a real game is on the machine is exactly the wrong place for
one.** It is 743, numbered 1 to 743 with no gap and no number twice, and the
test says so in words.

**The three maps are a sample and the test says that too.** All 743 read in
principle; the test reads the smallest, the largest and one from the middle,
**because a reader that reads those three has not proved it reads all 743**,
and claiming otherwise would be the same kind of guess.

### `qa_patches/` — a stale audit and three over-expected patches, and what came of them

**`qa_patches/` is unversioned and it is not this session's work.** It is a
QA dispatcher's output from 2026-08-24: an `ENGINE_COVERAGE_AUDIT.md` and four
worktree patches. Two things came out of reading it, and both are worth having.

**The audit's columns are a month stale** and its own text says so by
implication: it lists XP and MZ as "Missing Ruby execution" and WOLF as
"Partial: explicit unencrypted JSON test envelope", **and since 2026-08-24 this
repository has a working Ruby interpreter with 71 tests, reads 23 real XP data
files and a real RM2K game, and has proven the WOLF games encrypted.** **A
snapshot with a date on it is history and not a specification**, so the file
stays where it is and the board carries the current numbers.

**And one of the patches asked for a test the repository was missing.**
`t_ba1d255d.patch` asserts `ExpectedSystemDataPath` and `HasSystemData` — and
**both fields were written by `Initialize` and read by nobody.** That is not a
missing test, it is a missing surface: the path is decided in the constructor,
`Initialize` needs a plugin selection the selector refuses for RGSS, **and so
the field could not be reached by a test at all.**

> **The first version of the test built a `RgssRuntimeInfo` and asserted on
> it — which proves nothing**, because it asserts that a value the test itself
> wrote is the value the test read back. **The fix was a one-line public
> property on the runtime**, and **a field that only `Initialize` writes and
> nothing reads becomes wrong without a run noticing.**

`RgssEngineRuntime.ExpectedSystemDataPath` is public now, and
`TestRgssRuntime` is **6/6** with three new cases: the three generations name
three different files, an unknown generation names none, and **the three names
are three and not one** — a runtime that answered `rxdata` for all of them
would pass a per-generation test and be wrong about two engines.

**The other two patches are not gaps.** `t_a37367ee.patch`'s MV title parsing
is in `BuiltInEnginePlugins.cs` in a later form (`title` from a different
regex, no intermediate `System.Text.RegularExpressions` line), **and its
`SESSION_STATE.md` and `docs/PROJECT_STATUS.md` lines describe a state that has
since moved on.** `t_dbb7d1bd.patch` touches `game_detector.cs` and three
plugin files, and **every one of those files is in `HEAD` with later work on
it.** `t_ae3e01c0.patch` is empty.

### The RTP, and the measured answer to "download it"

**The runtime reads the game and never the RTP, and that is now a test and
not a claim.** `TestRuntimeBoundary` walks **every** C# file under `src/` —
**127 of them, counted against the files on disk rather than against a
threshold**, because a threshold is a rounded guess and this repository has had
two of those in two days — and finds no `LoadLibrary`, no `GetProcAddress`, no
`DllImport`, no `Process.Start`, no `Assembly.Load`, no `Reflection.Emit`.

**And there is no RTP directory in any code path.** `RgssRuntimeInfo` carries
`RtpDependency` as the string `"RPG_RT"` — **a name and not a place**, which is
asserted directly, **because a field that held a path would be exactly where a
download could be wired in.** The only file paths the RGSS backend opens are
`Data/System.rxdata`, `.rvdata` and `.rvdata2` — the game's own files.

**So downloading the RTP would change nothing about what this software does.**
It is not refused for policy; **it is unnecessary, and a test now says so in a
way that fails if a future change wires a path in.** Criterion 9 is met by the
runtime not needing it, and that is a better answer than 1.5 GB of installers.

**And the two `Game.exe` mentions in the source are documentation and a
filename comparison** — `FindByName(..., "RPG_RT.exe")` scores a directory as
RM2K **by looking at the name and not by running it**. That was measured before
it was written down.

### The WOLF games are encrypted, and that is where this stops

**Both `Data.wolf` files begin with no readable magic** —
`83 5d cc 7d ad 0d de f1` — and there is no `WOLFM` header anywhere in either.
Reading them means taking the key out of `Config.exe` or `Game.exe`, **which
is circumventing the copy protection of a commercial game**, and this project's
rule is that protected files are refused and not decrypted.

**So K-110 stays `VERIFY`, and the reason is now measured rather than
assumed.** The question for whoever reads this is whether an **unencrypted**
WolfRPGEditor-made game exists to be used as a fixture — **and the fixture
path stays `project/tests/fixtures/wolf/real/`.** Everything else in WOLF is
structural either way: the readers are tested against bytes, and the 1,303rd
test still fails with `Attempted to divide by zero` in
`Test_TheIdleCycleRunsTheOtherWay`.

### K-110 — WOLF database, game settings, common events, commands and move routes
**Status (2026-09-26) — VERIFY. The scoped binary readers are implemented; WOLF is still not playable.**

**Why this card existed**
The user confirmed the scope: binary `.mps`, `database_dat`, `commonevent_dat` and `game_dat`, and that **no real WOLF fixture exists**. Without that last fact every claim here is structural.

**Implemented** `WolfBinaryDatabaseReader` (header, version at byte 10, property position `raw/1000` with index `raw%1000`), `WolfGameSettingsReader` (V2 and V3, the twelve string block, the 23 value u16 record, editor version at index 16), `WolfBinaryCommonEventReader` (15 byte header, the fixed five byte `unknown4` block, the command list), `WolfEventCommandReader` and `WolfMoveRouteReader`. The JSON readers were preserved and only the binary/data discrimination in `WolfDataReader` was changed.

**The WOLFM magic and version framing**
The magic is the six bytes `00 57 00 00 4F 4C`, then a version header byte, `46 4D 00` at bytes 7..9, the version at byte 10 and the type count at bytes 11..14. Guessed bytes were removed after the header was measured.

**Move routes are self-describing, which is the point**
A route entry carries its own argument counts: a four byte count, that many words, a one byte count, that many bytes. An argumentless entry still writes both lengths as zero. There are 59 route types and 12 of them are parameterised, but **the counts are not inferred from a type table** because an unknown type then becomes unreadable. Unknown entries stay readable and keep their raw arguments.

**Route options are bitfields, not bytes**
The behaviour byte holds eight flags. The route option byte uses the **upper three bits**; the lower five are reserved. Reading it as a full byte is a mutation the suite catches, one bit at a time, because a single test with all bits set does not detect a swap.

**Real errors found and fixed**
- Three `X_OKX` sentinels, an invalid `PluginResult<T>.Ok` and a non-existent `PluginErrorCode.CorruptData` were replaced with the repository's real API.
- The database version was read at byte 9 and is at byte 10.
- `HasDatabaseHeader` required 15 bytes including the type count, so a magic-only fixture failed; the two cases were split.
- A binary file was blamed on the JSON reader before the discrimination was fixed.
- `0xFFFFFFFF` needed unsigned handling and a non-existent map sentinel means no body follows.

**Tests:** `test_wolf_binary_map.cs`, `test_wolf_binary_database.cs` (17), `test_wolf_game_settings.cs` (13), `test_wolf_common_event.cs` (17), `test_wolf_event_command.cs` (10), `test_wolf_move_route.cs` (11). Every fixture is byte-authored from the schemas.

**Measured, not guessed:** `0x83 0x65 0x83 0x58 0x83 0x67` decodes to `テスト`, not to the text the first fixture assumed. The expectation was corrected after measuring.

**What this card does not claim**
- The binary **map transfer** format is **not** read at all. `WolfEventOpcode.Transfer`
  and `WolfTransferRequest` are the event *command* that asks for a transfer,
  which is a different thing from the file format a `.mps` uses.
- **No real WOLF game has been parsed.** Every test is synthetic. These are
  structural claims, never real game evidence.

**Re-checked on 2026-09-28, and the fixture boundary is objective.**
The help index lists **76 pages and not one of them documents a binary file
format**; `11fileformat.html` and `12saveformat.html` both answer `200` with
**zero bytes**, and `01specifi.html` — the "implicit specification" page —
contains no occurrence of バイナリ, 形式 or ファイル構造. **The format is
undocumented by the publisher**, and the card's remaining gap is a fact about
the available material and not unfinished reading.

**A search of the machine found no WOLF game and no editor**: no `.mps`, no
`.wolf`, no `Database.dat` / `CommonEvent.dat` / `Game.dat`, and none of
`WolfEdit.exe`, `WolfRPGEditor.exe` or `WolfTrans.exe`. **So this card cannot
be closed here**, and the unblock condition is exact: one real WOLF game
directory, or the editor, from the user.

**What is left, and is bounded, is the map transfer reader** — and that one
needs a real `.mps` to be written against, so it belongs to the same unblock.

### K-111 — RM2K event move routes, decoded and executed
**Status (2026-09-26) — DONE. Event move routes are decoded from the LMT and stepped in the runtime.**

**Verified structure** The route lives under `EventPage` `0x29`, with the command count at `0x0B`, the array at `0x0C`, `repeat` at `0x15` and `skippable` at `0x16`.

**Semantics that were wrong before they were measured**
- The first update **starts** movement and consumes no step budget.
- The command index advances only after movement **completes**.
- A blocked move advances only when `skippable` is true.
- Facing commands execute immediately and consume no movement.
- A finished route clears the remaining step budget.

**Implemented** `Rm2kMoveRoute`, `Rm2kMoveRouteState`, `rm2k_move_route_decoder.cs` and the runtime tick in `Rm2kEngineRuntime.Update`, with typed `MoveCommand`/`MoveRoute` models on `Rm2kMap`.

**Fixture boundary, stated honestly:** the pinned `Map0001.lmu` has 22 events and pages and **zero** move-route chunks, so it cannot prove a route. The end to end proof is a byte-authored synthetic LMU, and that is what `test_rm2k_event_move_route.cs` uses.

**Tests:** route `9/9`, decoder `9/9`, state `12/12`, end to end `7/7`.

**What this card does not claim:** only the verified command set is implemented. The pinned fixture exercises none of it, so no real game's route has been stepped.

### K-112 — RGSS archive format, shared by XP, VX and VX Ace
**Status (2026-09-26) — DONE as a reader and writer. Nothing executes an entry.**

**Why this was first for XP/VX/Ace** Without the archive an XP game cannot start at all, and this format is the one thing all three of the RGSS engines share, so it counts for three criteria where a Ruby virtual machine would count for none of them until it ran.

**Verified against the reference implementation.** Magic `RGSSAD`; every value is exclusive ored with the output of a linear congruential generator that starts at `0xDEADCAFE` and advances **once per value** by `magic = magic * 7 + 3`. A value is obfuscated with the generator's state **before** that step.

**The header is eight bytes**: the name `RGSSAD`, one byte the format does **not** check, and the version. The reference reader compares the first six bytes and reads the version from the last. A reader that also required the seventh byte to be zero would refuse a file the format allows, so the suite proves that byte is ignored instead of assuming it is zero. The version byte is what tells an XP or VX archive from a VX Ace one.

**Each entry** is a name, a size and a body. The name is obfuscated byte by byte and a backslash in it folds to a slash. The list ends when a name can no longer be read, not at a terminator. An entry claiming more bytes than the file holds is refused rather than handed back short, because a short body looks like a successful read.

**Two of my own bugs, both caught by tests rather than by reading.** A regular expression pass removed the `return` from three failure paths, so a refused archive fell through and was read anyway while the diagnostic said it was not an archive. And an unused version read indexed one byte past the end of an eight byte header, which crashed on an empty archive.

**Two of my own wrong expectations:** I had the header as three zero bytes after the name, and I had the generator taking two steps per field. The first surfaced as an out of bounds read on an empty archive, the second as a round trip that decoded a name length of three hundred million, which was **reproduced outside C#** before the reader was touched again.

**Tests:** `test_rgss_archive.cs` 15/15, total `678/678`.

**Mutation evidence, six of six detected:** a seed off by one, a wrong multiplier, a name byte read without the key, a version read from the wrong offset, the unchecked byte checked, and an entry list that started four bytes late.

**What this card does not claim:** the reader lists and reads entries. It **executes nothing**; a game script is bytes. There is no real RPG Maker game in the repository, so no real archive has been read.

### K-113 — Ruby Marshal reader for the RPG Maker data files
**Status (2026-09-26) — DONE as a reader. No game class is instantiated and no script runs.**

**Why this was second** With the archive in place the other half of the data pipeline was missing: XP, VX and VX Ace keep their data in `.rxdata`, `.rvdata` and `.rvdata2`, which are Ruby Marshal streams.

**Verified against the published Ruby specification**, not from memory: a two byte version, then one value, where a value is a type byte and a payload whose shape belongs to the type.

**Integers are the part that is easy to get wrong, and I got it wrong first.** A marshalled integer is a type byte and then one to five bytes, where the first of those encodes sign and width in a single value. Eight values are special; the rest is a sign extended byte with an offset of five. A reader that treats the first byte as a length decodes small numbers correctly and everything else as something plausible but wrong.

**An object takes its index before its contents are read**, because a value inside a collection may link back to that collection and the link names an object the stream has already defined. Numbering afterwards would point every such link at the wrong object.

**A link does not take an index of its own**, because it names an object that already exists. My first expectation had this wrong and the measurement corrected it.

**A regexp carries no class name**: the specification gives a source and an option byte and nothing else. A bignum is **refused rather than read**, because a game's data uses fixnums for anything that fits and a bignum would mean arbitrary precision this reader does not carry.

**Refusals, not partial trees:** a stream that ends inside a value, declares a length past the limit, or carries an undefined type byte raises. A major version this reader does not implement is refused outright and a **newer minor version** is refused too, because it may use a type this reader has never heard of; an older minor version is read.

**Two mistakes of mine, the second only visible under mutation.** A grouped `case` list plus single `case` labels for the same values left the later ones unreachable, so a regexp fell through to the refusal branch; and when that label was removed the routing line went with it, so no regexp could be read at all. Routing and payload are now separate concerns.

**The reader produces a value tree, not live objects**, on purpose: a game database is full of instances of classes this project has never heard of, and resolving them would mean either running the game's Ruby or inventing classes that do not exist.

**Tests:** `test_marshal_reader.cs` 29/29, total `678/678`. The fixtures are written by hand from the specification's type table, because a round trip through a writer of our own would pass even if the reader and the writer were wrong in the same way.

**Mutation evidence, fifteen detected:** a flipped sign offset, a swapped sign case, a zero case that swallowed a byte, a width read one byte short, an array numbered after its contents, a hash likewise, an uncapped nesting depth, an unchecked major version, an unchecked minor version, an uncapped byte count, an unchecked symbol link, an object link accepting index zero, a regexp reading a class name, a symbol link resolving out of range and a delayed array index.

**Two mutations turned out to be equivalent rather than escaping.** Moving `++ObjectCount` below the `ReadLength` call changes nothing, because reading a length does not touch the counter. A mutation that cannot change behaviour cannot be caught by a test, and recording it as a gap would have been wrong. A mutation that really delays the index until after the elements were read is detected.

**What this card does not claim:** no real `.rxdata` has been read, because the repository has no RPG Maker game.

### K-114 — RM2K vehicles: state, boarding, sprites and the airship shadow
**Status (2026-09-28) — DONE.** Simulation and rendering are implemented,
mutation tested, and the one boundary that kept this card open is
**re-measured and resolved**: the card said it stayed `VERIFY` because
K-094 was not closed, and **K-094 is `DONE` and has no open child** —
the parent condition is satisfied, and the fixture boundary the card
names is a stated test boundary, not unfinished work.

**Verified from the reference implementation, not guessed.** Boat and ship move at speed 4 and the airship at 5, so a move speed of 3 means half speed. A vehicle's altitude is measured in tile units against a budget of 256 and falls by 8 per update. A moving vehicle animates over 12 frames and a stopped one over 16, both modulo 4. The airship's shadow is a separate sprite drawn from `(128,32,16,16)` and `(144,32,16,16)` at opacity `(int)(0.26 * 255) = 66`, one below the airship, and visible only while the player is aboard.

**Boarding is asymmetric.** An airship refuses a boarding attempt from a tile it is not directly over, and refuses a disembark while still in the air. A boat or a ship does not. Boarding has priority over an action event, which the reference implementation proves by the order `if (!GetOnOffVehicle()) CheckActionEvent(); return;`.

**Vehicle background music is deliberately absent.** There is no BGM state contract in this project, so switching a vehicle's track would mean inventing one. It is not hidden behind a diagnostic flag; it is simply not there.

**The pinned fixture cannot show a vehicle, and the tests say so.** All three vehicles in the pinned `RPG_RT.lmt` target map 39, while `Map0001` is map 1, and the pinned `vehicle.png` is absent. The drawing path is therefore exercised through a **derived** fixture that copies the game and adds the official reference test image, leaving the pinned data untouched. A test states the boundary explicitly instead of pretending otherwise.

**Test-only hooks** exist to place a vehicle on the map under test and to re-render. They are called from tests only and are documented as such.

**Tests, re-measured on 2026-09-28** vehicle `11/11`, boarding `11/11`,
decision turn `12/12`, vehicle decision turn `8/8`, sprite `9/9`, compositing
`6/6`, get-on-off `4/4`, runtime rendering `19/19` — **61 tests in the seven
vehicle files, all green inside the `1533/1533` run.**

**Measured after the fact:** the airship's system index is **3**, not the 2 the first expectation assumed.

**What this card does not claim:** `move_random`, hero directed movement, broader event and audio integration and the rest of the whole engine remain open. This card is one slice of K-094, which stays `VERIFY`.

### K-115 — Ruby lexer for the RGSS engines
**Status (2026-09-26) — DONE as a lexer. It calls nothing, resolves nothing and runs nothing.**

**Where this sits** With K-112 and K-113 in place, this is the third of the three layers XP, VX and VX Ace need before their scripts can be read, and the first that looks at the script text itself.

**The keyword list is Ruby's own, not written from memory.** Forty one reserved words extracted from the grammar's `parse.y`. A keyword is reserved, so a lexer that treated one as a name would accept files Ruby rejects and reject files Ruby accepts.

**A name beginning with an upper case letter is a constant, and the reserved word check comes first.** Two reserved words, `BEGIN` and `END`, begin with an upper case letter and the grammar's `reswords` production lists them as keywords. Checking for a constant first read them as names. A test over **all forty one** words is what found it, because the single example I had chosen happened to be lower case.

**A slash divides where a value has just ended and opens a regular expression where one could begin.** The first version had this backwards, so `a / b` was read as a regular expression that ran off the end of the line. Both shapes are in the suite because they differ only in what came before the slash.

**A single quoted string interprets only two escapes**, the quote and the backslash. Reading it like a double quoted one loses a backslash a game asked to keep, which is the entire reason the form exists.

**A regular expression keeps its backslashes**, because the pattern engine is what interprets an escape, and a `/` inside a character class does not close it.

**A string keeps its bytes as well as its text**, because a Shift-JIS script is not UTF-8 and a reader that kept only text would silently corrupt it.

**An octal literal may be `0o17` or `017`.** The marker sits between the leading zero and the digits; checking the current character instead of the next one read `0o17` as a bare zero.

**Refusals, not partial token lists:** an unknown character, an unclosed string, an unclosed regular expression and a number with no digits in its base all raise with their line.

**Tests:** `test_ruby_lexer.cs` 33/33, total `711/711`.

**Mutation evidence, fourteen run and eleven detected:** a keyword list never consulted, the reserved word check moved after the constant check, the constant rule inverted, a slash always a regexp, a slash always a division, single quoted escapes applied in full, the octal marker not skipped, a shorter operator matched first, an unclosed string accepted, a line continuation read as a break, a block comment not skipped, a class variable read with one at sign, a regexp losing its backslash, an unterminated block comment end.

**Two mutations were equivalent rather than escaping.** Appending `<=` and `<<` to the operator list changes nothing, because every multi character operator already appears before the shorter one it starts with; the check printed the whole list to establish that. Turning a byte escape's `((char)value).ToString()` into `value.ToString()` changes nothing, because the cast already produces values in the range where the two agree. A mutation that cannot change behaviour is not a gap in the tests.

**Four gaps the mutations found were real and are now closed:** the keyword lookup was untested, the single quoted escapes were only checked for one letter, the operator order was checked only for the operators the test happened to use, and the line continuation test filtered the very newline it was about.

**One mistake of mine in the tests hid four failures.** The helper that drops whitespace-only tokens did not drop the end of input token, so every list based assertion was off by one element. A probe with a different filter showed the lexer's output had been right all along.

**What this card does not claim:** there is no parser yet, so a script is a token stream and nothing more. **No real RPG Maker script has been tokenised**, because the repository has no RPG Maker game.

### K-116 — Ruby parser for the RGSS engines
**Status (2026-09-26) — DONE as a parser. It builds a tree and runs nothing.**

**Where this sits** With K-112 (archive), K-113 (Marshal) and K-115 (lexer) in
place, the data of an XP, VX or VX Ace install is readable from end to end as
data. A game's Ruby now has three layers: bytes, tokens, and this tree. What is
still missing is everything that would give the tree meaning.

**What it does** `RubyParser` turns a token stream into a tree of shapes. It
answers one question — what shape was written. It does not answer what any name
means, whether a call succeeds, or what a value is at run time. A node that
records a call names the method as written and knows nothing about whether this
runtime has ever heard of it.

**The precedence is the grammar's own.** Every level was taken from the
declaration order in the Ruby grammar rather than from memory. This turned out
to matter more than expected: the first table written from memory had the
relations and the equality on separate levels, which the grammar's
`rel_expr %prec tCMP` shows are one. A reader that gets one level wrong parses a
game's arithmetic into a different tree, and nothing about the result looks
wrong.

**What is deliberately not here**
- No name resolution, no method lookup, no constant lookup.
- No execution, no evaluation, no calling of anything.
- No literal Ruby objects, no binding, no class loading.
- A shape this parser cannot read raises with its line. A tree that stopped
  early would be worse than none, because nothing would mark it as complete.

**Verification (2026-09-26)**
- `TestRubyParser` 44/44, total 755/755, `scripts/validate.sh` passed.
- Every expected tree is written out by hand from the grammar's rules. A tree
  produced by the parser and compared against itself would prove nothing.
- 21 mutations, all detected.

**Errors the tests found in this parser, all fixed**
- The precedence table from memory had relations and equality on two levels; the
  grammar resolves them onto one with `rel_expr %prec tCMP`.
- `**` sat at the arithmetic level instead of above it, so `a * b ** c` parsed
  as `(a * b) ** c`.
- A member call's argument list was skipped whenever the receiver was a name, so
  `sprite.draw(x, y)` read its parentheses as a grouping.
- A block's body was read as a whole program, so every `def` and `do` reported a
  missing `end` on a file that is well formed.
- A `do` belonging to a `while` was read as a block on the loop's own condition.
- The range operator had no level at all, so `1..2` parsed as two statements.
- `not` was read both in `ParseUnary` and in `ParseBinary`. The second was
  unreachable, and the mutation suite showed the 44 tests passed with it gone, so
  it was removed rather than kept as a second route to the same node.
- Two mutation escapes turned out to be untested boundaries rather than wrong
  code: nothing crossed the logical/bitwise boundary, and nothing pinned `not`
  to its own level. Both now have tests.

### K-119 Read a whole number wider than this machine holds
`READY` → `IN PROGRESS` → `DONE`

**The question that started this** The marshal work had been checked against
Ruby 3.4, because that is the documentation that is easiest to reach. The
engines of this repository's line run older rubies, so the whole ground truth
was suspect. It was checked against the sources themselves:

- **XP is Ruby 1.8.1, VX is 1.8.3, VX Ace is 1.9.2.** All three carry a marshal
  format.
- All twenty five type bytes are **identical** across 1.8.7, 1.9.3 and 3.4.1.
  The format did not change for the engines in question.
- The whole number form did change, and in the direction that matters:
  **1.8 and 1.9 write `i` for a number that fits in thirty one bits and `l` for
  the digits of anything larger. Ruby 3 swaps the two letters and writes the
  large form in binary.** A reader built from the 3.4 table would refuse every
  file an engine of this line writes.

**What the reader had wrong, and it was wrong about the sign**

A whole number that does not fit is written as a sign and one byte per digit,
and the digits of a negative number are the number carried to the width it was
written in, so every byte after the first is the top of the width. The reader
was negating the unsigned value instead, which looks the same for a one byte
number and is not the same for any other: **it read one byte too many and took
the first byte of whatever followed in the file.** A game's negative coordinate
would have had the next value's bytes inside it, and nothing downstream can tell
that from a real number.

The one byte negative form was also on the wrong side of the boundary. The rule
is five to one hundred and twenty seven is the number with five taken off, and
minus one hundred and twenty nine to minus five is the number with five added,
with minus one to minus four the wide form. The reader had the last two the
wrong way round.

**How it was found** Not by three hand written cases. A test walks six thousand
and one numbers through the writer taken from 1.8.7's own loop and compares
each against what the reader says, and a second test holds sixteen numbers
against the bytes that loop produces, because the first version of those was
written from memory and was wrong about four of the sixteen. **Every fault found
in this card was in the test rather than in the reader**, which is the opposite
of what the range test was written expecting, and the reason it is worth having
is that it is the only one of the two that can find a fault at all.

**A fault this card found in a fault of an earlier card** The earlier card
refused a wide number with the reason that it would need arbitrary precision.
That reason was wrong: a game's number is written as decimal digits, so the
number itself is readable, and the only question is whether it fits this
machine. It is read now, and refused only when it does not fit, with the number
of digits in the reason so that a number too large and a file that is not
marshal are not the same fault.

**One thing this card did not add** A check refusing a count byte wider than a
whole number. Such a count cannot occur: five to one hundred and twenty seven is
the one byte form, and a count of one hundred and twenty seven is the number one
hundred and twenty two. The check was written from a reading of the byte range
rather than of the rule, and it refused a length a game writes for every list it
has. It is gone, and the fact it was based on is tested instead.

- Tests: `TestMarshalReader` 39/39, total 818/818, validator passed, 0 warnings.
- 8 mutations of the packing, all detected.

**Still missing** A whole number wider than a whole number this machine holds is
read and then refused, which is honest but means a game holding one will not
load. No archive from any of the three engines is in the repository, so all of
this is verified against the rubies' own sources and not against a game.

### K-118 Name what every child of a tree is for
`READY` → `IN PROGRESS` → `DONE`

**What it is** A consumer of the parse tree has to ask a node for the test, the
body, the left of an operation or the first argument, instead of knowing the
layout of every kind by heart.

**The fault this found** The parser said only that a child was there, and what
the list meant depended on the kind and on nothing else. A keyword that opens a
test held the keyword first and the test second, a ternary held the test first, a
block on a call held the call, the parameters and the body, and a block that was
a body held only statements. **One kind could mean two things, and the second
meaning was invisible.** Every consumer would have had to learn the layouts from
the parser's source, and nothing would have said when one of them was wrong.

**What changed** Every child carries the role it plays. `Children` stays for a
reader that wants the order and does not care what the order means, and a node
whose roles are empty has not been given roles rather than having none. A lookup
for a role that is not there answers null instead of falling back to the first
child, so an absent role cannot be mistaken for a present one.

A name is held in `Name` and not in `Text`. That was worth a test, because a
test reading `Text` finds null and could be fixed either by filling `Text` or by
reading `Name`, and only one of those is right.

- Roles filled for a keyword that opens a test, a ternary, an assignment, an
  operation and a call. The four places that build a call say their shape through
  one helper rather than each repeating it.
- Tests: `TestRubyParser` 52/52, total 808/808, validator passed.
- 6 mutations on the roles and the lookup, all detected. Two of them escaped at
  first because a lookup that takes the last of a role and one that takes the
  first cannot be told apart while every node holds at most one child under a
  role, and because a lookup that fell back to the first child passed every test
  that asked for a role that was there. Both are now tested.

**Why this comes before a machine** A machine that ran the tree would have had to
read the parser's source to know which child was the body, and a mistake there
runs a name as if it were a statement. That is quietly wrong rather than loudly
wrong, which is the worst shape a mistake can have.

**Still missing for XP, VX and VX Ace** A machine to run the tree, and any real
archive from any of the three engines.

### K-117 — The value layer between a game's data and its language
**Status (2026-09-26) — DONE as a value layer. It names values and judges none.**

**Where this sits** K-112 reads the archive, K-113 reads Marshal, K-115 reads
the tokens and K-116 reads the tree. What was missing between "a file said this"
and "the language calls this a value" is this card. Without it the two layers
would each have their own idea of what a game's data means, and they would
disagree without either being wrong.

**What it does** `RubyValue` holds the seven kinds the language defines, as
data, with a value's identity and its contents and nothing else.
`RubyValueConverter` turns a decoded Marshal value into one, following the
file's links, and refuses anything that has no equivalent.

**The refusals are the point.** A kind the language has no name for, a payload
that contradicts its kind, a mapping entry without its other half, a mapping that
holds the same key twice, a link to an entry that was never decoded: each of
these raises with the reason, and each refusal is counted and remembered. A value
that is nearly right is a value a game cannot be trusted with, because nothing
downstream can tell it apart from a real one.

**What is deliberately not here**
- No arithmetic, no comparison, no conversion between kinds.
- No method dispatch, no calling of anything.
- No class loading: a game's own class is kept as the text the file wrote.
- No decoding of a string's bytes. A game's strings are in its author's
  encoding, usually CP932, and choosing one is a decision this layer does not
  make.

**Verification (2026-09-26)**
- `TestRubyValue` 15/15, `TestRubyValueConverter` 30/30, total 802/802,
  `scripts/validate.sh` passed.
- 13 mutations on the converter's kind names, its refusals, the link following
  and the value identities, all detected.
- The link tests read real byte streams written with the format's own packing.
  A Marshal long is not eight bytes, and a stream written with eight would be a
  different stream from the one a game writes.

**A bug this work found in the reader the card before it**
The converter was written against kind names spelled out from memory. The reader
emits `array, false, float, integer, nil, object, regexp, string, struct,
symbol, true` — and two of the converter's names were not on that list. A whole
number arrives as `integer` and a string as `string`, so **every number and
every string in a real game's data would have been refused**. The kind names are
now taken from the reader itself rather than from memory, and two mutations that
delete each of the two arms are both detected.

**The numbering is the reader's, and the specification is explicit about it**
A stream holds one copy of each object and one of each symbol. The first object
has the number one and the first symbol the number zero. The converter reads
that number from the value the reader handed over instead of counting again,
because counting again would be a second opinion about a number that was
already decided.

A container is numbered **before** its contents are read. That is not an
implementation detail: it is the only reason a container can hold a reference to
itself, which a game's data does whenever a structure names itself. The
documented stream for an array holding the same string twice,
`"\004\b[\a\"\nhello@\006"`, has the array at one and the string at two, and
the link names two.

**Other bugs this work found in the converter**
- The converter never recorded the stream's own entry numbers, so every link was
  reported as pointing at nothing.
- The first version numbered values from zero and after their contents, which
  gave a container a higher number than its first member.
- A value was filed under its number before its class was attached, so a link to
  a game's value found an object that no longer said what class it was.
- A value that points at itself is refused with the number in it, because there
  is no value to return yet and returning something else would give it a second
  identity inside its own contents.
- The class name was being attached twice, once where the contents are read and
  once afterwards. The second could never change anything, and a mutation that
  removed it went unnoticed, which is how the duplicate was found. It and the
  helper it alone used are gone.

### K-120 Read the data three real games actually wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** Every parser and reader in this repository was
written against a fixture this repository made, or against EasyRPG's test game.
Both are right for what they are and neither is a game: the RM2K test game has two
hundred and ten bytes of database, six bytes of map tree and five maps, and every
count, length and index in a game of that size fits in a byte and would not in a
real one. **No fixture in this repository was written by an engine.** A reader
that has only ever read a hand made file has never been shown a game.

Three games were given to the repository to use. They were classified from their
own files and nothing in them was executed.

| Game | Engine | Decided by |
|---|---|---|
| `rgss-xp` | RPG Maker XP / RGSS1 | `RGSS104J.dll`, `Game.rxproj` naming `Scripts.rxdata` |
| `rgss-xp-microquest` | RPG Maker XP / RGSS1 | `RGSS104E.dll`, `Game.ini` |
| `rm2k-dragon-destiny` | RPG Maker 2000 | `RPG_RT.ldb`, 743 `.lmu` files, `RPG_RT.ini` |
| `kirikiri` | **KiriKiri, not WOLF** | `SoftModeFlag`, `FrameSkip`, `SEandBGM`, no `Game.dat` |

**What the real files found**

The XP games keep their database as marshal and, with an unencrypted archive, in
plain files under `Data/`. Sixteen of them are now in the repository, ~310
kilobytes, and **all of them are read**: `TestRealXpData` walks every value of
every file and finds no fault. The marshal reader is now checked against bytes an
engine wrote, not only against the rubies' own sources.

Two of my own assumptions were wrong and the files said so:

- **A map is not a hash.** It is an `RPG::Map` object with eleven members, and
  eleven keys. The reader was right; the expectation written from memory was not.
- **A `.lmu` holds an `LcfMapUnit`**, not an `LcfMap`. Measured, not remembered.

**The KiriKiri game was offered as a WOLF game and is not one.** It carries
folders called `BasicData` and `MapData`, which are two of the three things the
Wolf detector looks for, and its data folder is laid out the way a Wolf game's is.
It has no `Game.dat` anywhere, and that is the third thing. The detector refuses
it, and `TestKirikiriIsNotAWolfGame` proves the refusal is a decision rather than
an accident of not having looked: **the same folder with a `Game.dat` in it is
detected as Wolf.** A folder full of what a Wolf game would have is not a Wolf
game, and a detector that answers either way rather than refusing has guessed.

**What is claimed and what is not**

Claimed: the XP detector recognises an XP installation from its own files and
does not confuse it with VX or VX Ace; the marshal reader reads sixteen real
files from two independent installations; the RM2K parser reads a 416 kilobyte
database, a 57 kilobyte map tree and two maps of a 743 map game.

Not claimed: that a game's data is **understood**. A database read as a
dictionary of chunks is a database read; it is not an actor, an event, a page or
a chipset. A map file is read as an `RPG::Map` holding its members; nothing here
knows what a member called `@events` is for. There is still no renderer, no
script execution, no save path, and `RgssEngineRuntime` is still metadata only.

**Tests and evidence**

- `TestRealXpData` 6/6 — sixteen real files, two installations, two encodings.
- `TestRealXpDetection` 3/3 — an XP folder is XP, is not VX or VX Ace, and a
  folder with data but no layout is either XP or nothing.
- `TestRealRm2kData` 3/3 — a real 416 KB database, its 57 KB map tree, two maps.
- `TestKirikiriIsNotAWolfGame` 2/2 — the refusal, and what makes it a decision.
- `TestMarshalReader` 40/40 — the last one added tells a number this machine
  cannot carry apart from a file it cannot read, which is the one mutation of the
  eight that escaped the first suite.
- Total **833/833**, validator passed, build 0 warnings / 0 errors.
- **Eight mutations of the reader, all detected.** Two of the first suite's eight
  did not test anything: it counted a mutation as breaking the build whenever
  `error CS` appeared anywhere in a run, and the run prints the mutation report
  of the step before it, so six that compiled were reported as broken. The suite
  now compiles each mutation on its own and only calls it broken if that build
  really fails.
- `project/tests/fixtures/RGSS_FIXTURES.md` holds every file with its size and
  SHA-256. No executable, DLL, save, image, audio or script is imported.

**Deferred, not done**

- The XP games' `Scripts.rxdata` is deliberately **not** imported. A script is
  code, and this repository does not run a game's code.
- 743 maps of `rm2k-dragon-destiny` are not in the repository; two are, and the
  rest are the same format at a different size.
- No archive from any of the three engines is in the repository, so the
  `RgssArchiveReader` still has no real file to read.

### K-121 Read the data an RPG Maker MZ game wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-120 brought in real data for RGSS and RM2K and
left MV and MZ where they were: **detection and a count of entries**. There was
no reader that returned a game's values. An MZ game keeps its database as plain
JSON, so a reader for it can exist without a runtime and without running a line
of the game's own code, and none was written.

An MZ 1.9.1 game was given to the repository. It was classified from its own
files — `game.rmmzproject`, `node.dll`, `package.json`, `js/rmmz_core.js` — and
nothing in it was executed.

**What was built**

- `project/src/mz/MzJson.cs`: JSON read the way a game wrote it, with nesting
  bounded, a string that is not closed refused rather than run to the end, an
  escape the editor never writes refused by name, and a number with an exponent
  and no digits refused.
- `project/src/mz/MzDataFile.cs`: one of a game's data files, holding **one root
  value** and the file's own text so a caller can hash what was read.

**Three things the format has, all found by reading the file and not a
description, and two of which were wrong in a first draft of the test**

1. **A database file's first entry is null.** `Actors.json` is `[
null,
{...}]`.
   The editor numbers its actors from one so zero can mean "no actor".
2. **A command is a small number and is not packed.** This game's commands are
   `121`, `231`, `357`, `657` and nothing above a thousand anywhere in the file.
   In the generation before, a command's number is its value times a thousand
   and a reader divides by a thousand. **A reader written for MV and pointed at
   this file would divide every command to zero.**
3. **A map's events are indexed by event, not padded to the field.** `Map002` is
   seventeen by thirteen and its `events` array holds seven entries, the first
   null. A first draft of this test claimed the array ran over the whole field.

**An API of mine that was a trap, and removed rather than documented**

The first version of `MzDataFile` exposed `Top` as a `List<MzValue>` holding the
one root value, so `Top[0]` was the file and `Top[0][0]` its first element. The
test that was written against it then read the array where the object was and
failed in four places at once. **A one element list is not a root value**, and it
is now `Root`, an `MzValue`.

**The reader that was already here is a different thing, and the difference is
now stated by a test**

`MzDataDirectoryResult` exists and counts entries, takes names and caps a file
at 2 MiB. `TestMzReaderBoundary` says so and checks the cap it states. It returns
no map, no event, no command and no coordinate, and the new reader returns
values and does not name a game. Neither is derived from the other.

**Tests and evidence**

- `TestRealMzData` 15/15 — eleven real data files, the three format traps above,
  and five refusals.
- `TestRealMzDetection` 3/3 — the game is MZ, is not MV, and the folder with the
  previous generation's runtime is answered differently.
- `TestMzReaderBoundary` 2/2 — the boundary to the reader that was already here.
- `TestMzDataDirectory` 8/8, unchanged.
- Eight mutations of the new reader. **The first suite detected one of eight**,
  and that is the honest number: it found five real gaps — an unclosed string was
  run to the end of the file, an unknown escape was taken as text, nesting was
  unbounded, a broken exponent became a number, and a file's own text was thrown
  away — plus one anchor that did not exist. Each gap got a test of its own and
  the suite was rerun.
- Total **853/853**, validator passed, build 0 warnings / 0 errors.
- `project/tests/fixtures/MZ_FIXTURES.md` holds every file with its size and
  SHA-256. The two `js` files are **placeholders carrying the real names**: the
  runtime is 175 KB and 83 KB of a game's own code and is not imported.

**Still not true of MZ**

- No JavaScript runtime, so **no plugin, no script, no event command runs.** An
  MZ game does not play.
- Nothing here knows what command 231 does or what a page's conditions mean.
  Values are read; they are not understood.
- MV shares the data format and has **no fixture at all** from a real game, and
  its command numbering is the packed one, which is exactly the difference the
  second trap above is about.

### K-122 Name every command an RPG Maker MZ game stores
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-121 read a game's numbers and could say a
command was 121, which told a caller nothing. A name per command needs a source,
and the source is not a documentation page: it is the method the engine dispatches
the command to, which the engine's own source carries the name of in the comment
above it.

**What was built**

- `project/src/mz/MzCommandName.cs`: **114 commands, 101 to 603**, every number
  and every name generated out of the engine source of a real game. A record
  `MzCommand(int Code, string Name)`, so a number and its name are one value.
- `project/src/mz/MzCommandTable.cs`: what a number in a command list is — a
  command, the data of a command, the editor's own indent, or unknown — and which
  command reads which data number.

**A table written from memory, and what it cost**

The first draft of the table was written by hand. Compared against the engine,
**79 of its 178 names were wrong.** 129 was written "Change Hp" and the engine
calls it "Change Party Member". 231 was "Move Event" and the engine calls it
"Show Picture". Twenty six commands the engine has were missing and forty three
that it does not have were there. A plausible command a game does not use is
invisible until a game uses it, and this game uses 231.

**The rule that is not a rule, measured**

"Which command does this data belong to" invites `code - 300`. Against this game
that is right **four times out of eight**:

| Data | Owner measured | `-300` says | What that is |
|---:|---:|---:|---|
| 401 | 101 Show Text | 101 | right |
| 405 | 105 Show Scrolling Text | 105 | right |
| 408 | 108 Comment | 108 | right |
| 655 | 355 Script | 355 | right |
| 412 | 111 Conditional Branch | 112 | **Loop** |
| 501 | 102 Show Choices | 201 | **Transfer Player** |
| 605 | 302 Shop Processing | 305 | not a command here |
| 657 | 355 Script | 357 | **Plugin Command** |

Two of the four mistakes point at a command that exists in this generation and
does something else. A reader that used the rule would read a branch's else as a
loop, a choice as a teleport, a shop's purchases as a number meaning nothing, and
a script line as a plugin call. **The owners are written down because none of them
can be calculated**, and a test says so by running the rule and counting four.

**411, 412 and 413: two commands and one piece of data**

All three sit at an indent of their own under a branch, so all three look like the
branch's options. The engine names **411 "Else"** and **413 "Repeat Above"** as
commands of their own, and gives **412 no method at all**. A reader that treated
the family as data would refuse two real commands; one that treated it as
commands would run a branch's structure as an instruction. Both fail silently.

**A name written twice, and three mutations nobody saw**

The first shape was an enum with a name in each member's doc comment and a second
table beside it carrying the same names as strings, because a C# identifier cannot
be `Show Text`. **The two copies drifted and three name mutations were invisible**
— the reader handed out the string while the enum carried the prose, so changing
either alone changed nothing a test could see. It is a record now and a name is
written once.

**Tests and evidence**

- `TestMzCommandTable` 13/13 — every command named, every command this game uses
  named, the three data codes refused as commands, the four that are commands not
  refused, the rule measured at four of eight, and every number the table does
  not hold checked rather than the ones someone thought of.
- Eleven mutations. **The first suite detected four of nine**, all three
  name mutations escaping for the reason above. After the record replaced the
  enum and two tests were added, the name mutations are all seen, and the
  mutation that had nothing to test — adding a command to the owner map, which
  cannot matter because a command is decided before an owner is consulted — was
  replaced by one that can fail.
- Total **866/866**, validator passed, build 0 warnings / 0 errors.
- `grep` for `Execute`, `Run`, `Invoke` and `Eval` in `project/src/mz/`: none.
  A 657 line is held as the text the author wrote and is never run.

**Still not true of MZ**

- **An MZ game does not play.** A command is now named, which is the opposite of
  running it, and this repository will not run a game's script. A 657 line is
  text here and stays text.
- Nothing interprets 111's six comparisons or 121's three modes. Naming a command
  is not doing it.
- MV shares the format and has no fixture; its numbering is the packed one, which
  is exactly what the second trap of K-121 is about.

### K-123 Decide a conditional branch the way the engine does
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-122 named every command, so a branch read 111
with its six parameters and nothing more. Naming a command is the opposite of
doing it, and the first command whose whole effect can be taken from the engine
without running anything is a branch: the engine decides it in one method, and
every number in that method is readable.

**What was built**

- `project/src/mz/MzBranch.cs`: a branch, what it tests, the six ways of
  comparing, and the facts a caller has.
- `project/src/mz/MzBranchEvaluator.cs`: decides a branch from those facts and
  from nothing else. Fourteen kinds, of which nine are decided and the rest say
  what is missing.

**One branch is deliberately not decided.** Kind 12 asks whether a line of the
author's own JavaScript is true and the engine writes `result = !!eval(params[1])`
for it. This repository does not evaluate a game's JavaScript, so that branch is
`ScriptNotRun`, the author's text is kept, and the answer is neither true nor
false. **A mutation that made it answer true was the first thing the suite
checked and it was caught.**

**Three things in the method that were got wrong, each by a reader that had read
it**

1. **The third parameter only says whether the right side is a variable.**
   `params[2] === 0` picks between the number `params[3]` and
   `$gameVariables.value(params[3])`. This reader read `params[2]` as the
   variable, so the game's own branch `[1, 77, 1, 78, 1]` asked about variable
   one where the game asked about variable seventy eight. The test harness then
   made the opposite mistake, so the two hid each other for one run.
2. **Gold has a numbering of its own.** `switch (params[2])` with case 0 at
   least, 1 at most, 2 less — where a variable's case 0 is equal to and case 1 is
   at least. The first three are the same words in a different order. A purchase
   gated on a hundred gold **opens at ninety and shuts at a hundred and ten**,
   and the reader is right about the arithmetic and wrong about the question.
3. **A timer has no number in the parameters.** The second is a threshold in
   seconds and the third is the way, because the branch asks the one timer the
   event owns. This reader asked for a timer called five on a branch about five
   seconds, and refused a branch it could have answered.

**What is not known is not off.** A branch that asks about a switch nobody
supplied comes back `Unknown` and names the switch. A reader that treated the
missing as off would skip a game's content with nothing to show for it, which is
the one failure here that would be invisible, and a mutation of it was caught.

**Tests and evidence**

- `TestMzBranchEvaluator` 11/11 — the game's own three branches decided and none
  refused, each of the six comparisons at its own boundaries, gold under its own
  numbering with the ninety and a hundred and ten case, a stopped timer not
  compared, a script branch reported and not run, a missing thing refused by name,
  and every number that is not one of the fourteen kinds checked rather than the
  ones someone thought of.
- Nine mutations, **the first suite at eight of nine**. The one that got through
  folded a kind the engine has no name for into the nearest kind it does have,
  which is the shape of every mistake this file was prone to. It now checks every
  number from 14 to 657 that is not a kind, and that the refusal names it.
- Total **877/877**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **A branch is decided; nothing else is.** 121's three modes, 126's change to
  an item and 126's change to a weapon are not, and a game's flow is a chain of
  commands, not one of them.
- There is no interpreter holding an index into a list, so a branch decides
  something and nothing acts on it yet.
- Still no renderer, no save path, no input, and a 657 line is text.

### K-124 Walk an event list with an index the way the engine moves it
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-123 decided a branch and nothing acted on it,
because there was no index into the list to move. A game's flow is a chain of
commands, and a branch decides one of them and no more. The first thing the
interpreter has to be right about is not what a command does but **where the
index goes after it**, because every other rule in an interpreter hangs off
that.

**What was built**

- `project/src/mz/MzCommandEntry.cs`: one command out of a game's list.
- `project/src/mz/MzOperation.cs`: the four operands, the operation types, and
  a `MzRandom` **held per interpreter** — a static one would be shared between
  two runs of two events and give a game the same numbers twice.
- `project/src/mz/MzCommands.cs`: 121 and 122, and a rule that says which
  commands this reader acts on.
- `project/src/mz/MzControlFlow.cs`: the commands whose whole effect is the
  index, and the three answers they give.
- `project/src/mz/MzInterpreter.cs`: the index, the branch results per indent,
  the step limit, and the four ways a run can end.

**The index rules, each read out of `Game_Interpreter` and not reasoned about**

1. **Every command that returns true is followed by `this._index++`.** A first
   draft added a flag for "the command moved the index itself" and then did not
   step over a command that had, which made an else land on the false arm it had
   just skipped. The flag is gone.
2. **A repeat above is not an exception.** It walks back to the first command at
   its own indent, and the step then moves off that one — so `112`, body, `413`
   goes round properly without any special case.
3. **A command the engine has no method for is stepped over, not refused.**
   `executeCommand` asks `typeof this[methodName] === "function"` and, when it
   is not, still does `this._index++`. **Every one of those commands is one this
   game stores on purpose**: 0 the end of a block, 401 a line of text under a
   101, 412 the end of a branch, and 655 and 657 the two halves of a script.
   Refusing any of them would strand the game on a command the engine itself ran
   past.
4. **A list that ends inside a branch is said, not read past.** The engine's
   `skipBranch` has no test for the end of the list; this reader reports
   `Truncated` and names what is wrong.
5. **The step limit is the engine's `checkFreeze`.** A hundred thousand
   commands in one frame freezes the game in the engine. This reader has no
   frames, so it counts the same way and reports `Frozen`.

**Two findings that came out of the real map, and neither is a test mistake**

1. **This game stores a loop that nothing can leave.** Event 4 is a 112 with
   seven message commands and a 413, and nothing between them tests anything or
   breaks. The engine plays it until `checkFreeze` stops it. The reader reports
   the same thing, and the test says a freeze there is the correct answer rather
   than papering over it.
2. **Random is drawn per variable, not per range.** A first draft claimed one
   draw for a whole range. The engine's `command122` calls `Math.randomInt`
   **inside** `for (let i = startId; i <= endId; i++)`, so three variables get
   three rolls. The test was wrong in the same direction as the first draft and
   was corrected against the source.

**Test evidence**

- 18 tests in `project/tests/core/test_mz_interpreter.cs`, every list in the
  shape the editor writes — most of which were got wrong first, and the file
  says which and how.
- Total **896/896**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **Ten commands of a hundred and fourteen have an effect.** 117, 126, 230,
  231, 232, 235, 351 and 357 are read as text. A game's flow is a chain of
  commands, and this walks the chain for eleven of them.
- Still no renderer, no save path, no input, and a 655 or 657 line is text.

**Three rules that the mutation run found untested, and one of them was a claim
the file had been making wrongly**

1. **A repeat above is not a jump.** The engine's `command413` is `do {
   this._index--; } while (currentCommand().indent !== this._indent); return
   true;` — it writes the index and never calls `jumpTo`, so it clears no
   branch result. Only `command119` calls `jumpTo`, and that clears the result
   of every indent it steps over. **The test file had asserted the opposite
   for two cards' worth of work**, on the reasoning that a repeat above is a
   jump. Reading `command413` settled it: it is not, and a reader that treated
   it as one would clear results the engine keeps.
2. **A jump that points backwards at a label is a loop, in the engine as much
   as here.** `jumpTo` sets the index to the label, `executeCommand` steps on,
   and the jump is met again. A first draft of the label test was shaped that
   way and hung the suite for a hundred thousand steps, which is `checkFreeze`
   doing its work. **This game stores no label and no jump at all** — not one
   118 or 119 in the map read here — so only the shape that ends is asserted.
3. **A jump clears the result of an indent it LEAVES, and nothing else.** The
   engine's walk is `if (newIndent !== indent) { this._branch[indent] = null; }`,
   and every earlier test jumped from indent 0 to indent 0, so no test had ever
   gone through that loop. A reader that dropped the clearing entirely passed
   all seventeen. The movement is now claimed directly, both ways: a jump that
   changes indent clears the indent it left, and a jump that stays on one
   indent keeps what was there.

**Two ways a mutation run lies about itself**

The first run reported five escapes. Three of them were the runner's fault and
not the suite's:

1. **An anchor that is not in the file proves nothing.** Three mutations were
   written from a remembered line and reported `NOMATCH`. A mutation that never
   applied is neither caught nor escaped; it is a hole in the run, and counting
   it as "escaped" would have said a rule is untested when in fact the rule was
   never touched. Every anchor in the second run was read out of the file first.
2. **A mutation that lands on the wrong occurrence of a shape passes for a
   reason that has nothing to do with the rule.** `return false;` appears five
   times in `MzCommands.cs`; replacing the first one changes the refusal of a
   script operand, which a test does not look at, so the mutation survived and
   looked like a gap in the arithmetic. **A mutation has to name the place, not
   the shape** — the same rule that emptied this card's test file twice.


### K-125 Run the list a command calls, and stop at a wait
`DONE`

**The gap that started this** K-124 could walk one list. **An MZ event is almost
never one list**: this game stores eight common events in the one map read here
and reaches them with 117, and a 117 whose list is not available is not a command
a reader may step over — the rest of the list behind it never happens. The same
card took the 230 wait, because a wait is the other thing that stops a list
short of its end.

**What was built**

- `project/src/mz/MzEventRunner.cs`: a run over a list and every list it calls,
  with the engine's own answers for three things that are easy to get wrong.
- `MzInterpreter.Wait` and `PassFrame`: a wait holds the index and a caller
  counts the frames down.
- `MzAction.CommonEvent` and `MzAction.Wait`, and `Result.MissingCommonEvent`
  and `Result.WaitingFrames` as **fields rather than prose**.

**Three rules, each read out of `Game_Interpreter`**

1. **A called list runs to its end before the caller moves on.** `updateChild`
   gives the child its own `update()` and the parent breaks the frame while the
   child is still running, so a caller that carried on straight away would run
   its own next command first. The three tests claim the order of the innermost
   list's command before the middle one's and the middle one's before the
   outermost's.
2. **Every list in a run shares one set of facts.** Both go through the one
   `$gameVariables`. A runner that gave each list its own would have a called
   list change something its caller cannot see.
3. **The event id travels with the call, and only on a map.** `isOnCurrentMap()`
   decides, and that is what lets a common event address "this event".

**A wait is a fourth ending, and it is not a failure**

`command230` is `this._waitCount = params[0]`, and `updateWaitCount` takes one
off it per frame and breaks the frame while it is above zero. **The index does
not move**, so the same command is read again the frame after. A reader that
stepped over the wait would run the rest of a list three frames early. There
are no frames here, so the run is handed back `Waiting` with the count, and the
caller decides when the next frame is.

**What this repository will not do, and says so at every place it happens**

The bounded fixture carries **no `CommonEvents.json`** — the real one is 4.5 MB
and was left out on purpose — so every 117 in this game names an index that
cannot be handed over. The engine's own line is `if (commonEvent)`, and a
missing one leaves the index where it was and carries on. **This reader refuses
and names the index instead**, because a silent step-over would run the rest of
a game's list as if the call had never been there. Reading a called list out of
a fixture that does not contain it would mean writing the game's own scripts.

**Measured on the one map in the fixture, not guessed**

Six event pages, and the six end four different ways: three reach a common event
and name the index, one stops at a 230 and says it is waiting, one is refused
because it **opens with fifty-eight lines of the game's own JavaScript** — a
355 and fifty-seven 655, which this repository does not evaluate — and one is a
single 0 and runs through. A first draft of that test guessed three, one, one
and one, and two of the four numbers were wrong.

**A number read out of prose is not a number**

The test took the common event's index out of the message text with an offset,
and got **76 for 476** because it counted a space twice. The index is now a
field, and the test checks the field and the prose against each other.

**Test evidence**

- **16 tests** in `project/tests/core/test_mz_event_runner.cs`; the interpreter
  suite stayed at 18 with the `Waiting` case added to the walk of a real list.
- Total **924/924**, validator passed, build 0 warnings / 0 errors.
**What a mutation run does and does not prove.** Three runs, 14 rule variants
in all. **Fourteen caught** in the end, and the four that survived the first
pass were each looked at rather than counted either way:

1. **`MzInterpreter.Run` did not read `Waiting` as an ending of its own**, so
   the K-124 path could hand back a wait as if the list were done. A real hole.
   Now documented in the code and claimed by the four-way ending.
2. **`CommandLimit` over a whole run, and over the nested path, was untested.**
   `checkFreeze` counts the run and not one list. Three tests now claim it,
   including a pair of lists that call each other.
3. **`MissingCommonEvent` was only readable out of the message**, and the
   message is the one thing that changes shape. A first draft read the number
   out of the prose with an offset and got 76 for 476.
4. **`PassFrame` on a count of zero.** A first run asked `<= 0` against `< 0`
   and it survived, which was a real gap: nothing held a caller that keeps
   passing frames past the end of a wait.
5. **`MaxDepth` was untested**, and a `replace(..., 1)` mutation hid why: the
   line `MissingCommonEvent = index,` stands in two branches, and the mutation
   hit the depth one, which no test reached. It is a field now, with three tests.

   **And then the tests for it were not tight enough either.** A first draft
   asked only *whether* a self-calling list was refused, and it is refused at
   every limit from zero to eight — so all of it passed with a reader that
   refused one level early, one level late, or twice as deep as it should. The
   thing that tells the levels apart is **how many calls the run managed**,
   and that is what is claimed now: a limit of zero records one action, one
   records two, three records four, and a limit of six is not a limit of three.
6. **The map and the event of a child were untested**, and writing those tests
   found **a real fault in the runner**: it passed the *caller's* map down to
   the child, and read the map and the event off the frame rather than off the
   interpreter. `setup` sets `_mapId` from `$gameMap.mapId()` — the map the game
   is on — and `command117` reads `this._eventId` off the calling interpreter.
   Both are now read from where the engine reads them, and `Result.Child` hands
   the caller the child so the three fields can be checked rather than trusted.

   **And a first draft of that test claimed the event id falls away on the
   second level, which the engine does not do.** `setup` takes `eventId || 0`,
   so a chain on the map carries the same event all the way down. Only a list
   that is *not* on the map passes zero, and there it stays zero. Both
   directions are now claimed, because the first draft got the interesting one
   backwards.

**Three mutations that survived are equivalent mutants, and each one changed
the code rather than the test.**

1. The wait case's own `return false` cannot become `return true` and change
   anything, because `ExecuteOne` ends with `return Stopped == MzStep.Stepped`
   and `Wait` has just set `Stopped` to `Waiting`. That is a dead branch, and
   the code now says so where it would otherwise look like a rule with no test.
2. `WaitFrames <= 0` against `< 0`, and `WaitFrames--` against `-= 2`, differ
   only in states nothing can reach: a wait is never set below zero and
   `PassFrame` clamps it. Both are the same in every state a caller can be in.
3. **The one that changed the design.** `Frame` carried the map and the event
   as well as the interpreter, and a mutation showed that reading them off the
   frame and reading them off the interpreter give the same answer in every
   reachable state — the frame's copy always matched. **Two copies of one truth
   is how the event id came back from the dead in a first draft**, so the frame
   now carries only the interpreter and the question has one place to be asked.

A mutation that cannot be caught because it cannot be reached is not a test
gap, and writing a test for an unreachable state would only have named the
unreachable state.

**Still not true of MZ**

- **Thirteen of a hundred and fourteen commands have an effect.** 126, 231, 232,
  235, 351 and 357 are still read as text, and a 355 or 657 line is text.
- A called list that this repository *has* runs; one it does not have is named.
  The 4.5 MB that would supply the eight is deliberately not in the fixture.
- Still no renderer, no save path, no input and no audio.

### K-126 Change what the party is carrying
`DONE`

**The gap that started this** K-121 to K-125 read MZ data and walked event
lists, and neither needed to know what a game **owns**. A 126 does. It is
`Change Items`, this game's map uses it **eighteen times** over fifteen
different items, and it is the next command with a real effect that can be
checked against the game's own `Items.json` — which the fixture carries, 75 KB
of it.

**Built** `project/src/mz/MzParty.cs`, `MzCommandTable.ChangeItems`, the 126
case in `MzCommands`, and `MzBranchFacts.MaxItems`.

**Four rules, each read out of `Game_Party`**

1. **The count is clamped to ninety-nine, not to the number the event asked
   for.** `container[item.id] = newNumber.clamp(0, this.maxItems(item))` and
   `maxItems` is `return 99` — no argument, no per-item case. **Five of this
   game's eighteen commands ask for 999.** An implementation that added the
   number as written would hand a player a thousand of something the engine
   refuses to hold.
2. **A count that lands on zero is deleted**, not stored as a zero:
   `if (container[item.id] === 0) { delete container[item.id]; }`. A reader
   that kept a zero would answer `hasItem` differently the moment a game asked.
3. **Losing more than there is clamps to zero**, because the clamp is from
   below as well as above. Taking four of one is none, not minus three — and
   adding three back then gives three, which is what the engine's clamp makes
   true.
4. **An id with no item behind it changes nothing and says so.**
   `itemContainer` returns null and `gainItem` returns early, so the engine
   steps over it. This reader says it did not happen, because a game asking
   for an item this repository cannot hand over would otherwise look like a
   game that had it and used it.

**And one that is easy to get wrong in the other direction.** `operateValue`
asks the operand's **kind** first — `operandType === 0 ? operand :
$gameVariables.value(operand)` — and only reads the game for a variable
operand. A first draft read the variable either way, which made every one of
this game's seventeen literal amounts depend on whatever a variable held. And
`operation === 0 ? value : -value` has **no third case**: an operation of
seven removes, exactly as an operation of one does.

**The clamp is invisible in the middle of the range.** A test that only ever
added four to an empty bag would pass with no clamp at all. Every rule here is
asked about at its boundary, and the default is claimed to be the engine's
ninety-nine rather than a number chosen here.

**Measured on the one map in the fixture, not guessed** Eighteen 126s, fifteen
items, five above ninety-nine. **A first draft got nine** — it counted what a
walk reached, and one of the two pages stops at a 230, so the counts are two
different claims: one about the game's data, one about what a reader with
frames sees. Both are now claimed, and the difference between them is the
test.

**Test evidence** 12 tests in `project/tests/core/test_mz_party.cs`; the
interpreter's own suite is unchanged at 18. Total **924/924**, validator
passed, build 0 warnings / 0 errors.

**Mutations** Seventeen rules over two runs. The first run caught seven of
eleven, and **all four that escaped were one gap in one place**: every test
called `GainItem` directly, so nothing proved the interpreter passes the
right four numbers. Writing the test for the wiring found the two faults above
and killed all six rules in the second run, 6 of 6 caught.

**The wiring between the interpreter and the party was untested, and writing
that test found two real faults.**

1. **The party was built without the ids.** `new MzParty(pFacts)` knew no
   items, so every 126 was answered from a list the interpreter could not see
   and every count came back zero — **silently**, with nothing saying why. The
   ids now travel in `MzBranchFacts.KnownItems`, and a facts that carries none
   means the game has not been read.
2. **An empty set of known ids was read as "everything exists".** That is the
   opposite of what it means, and it would have handed a player 999 of an item
   the game never had while looking as if it worked. **Nothing known is nothing
   allowed**, and the code says so.

**Three mistakes of my own, recorded because the next one will make them too.**
A first draft of the wiring test drove the interpreter by hand, and a fresh
`MzInterpreter` has `Stopped` at whatever it starts as rather than at
`Stepped` — so `ExecuteOne` answered false on the very first command and the
loop gave up before it had run anything. It also wrote `new(2, ...)` where the
code belongs: **126 is the command, not the item**, and a page of codes 2, 3
and 4 is a list the engine steps over. And it read `party.Notices` on a party
it had made itself while the interpreter builds its own over the same facts,
so the notice was on the action and not where the test was looking.

**A known gap this card found and now names.** A lone `MzInterpreter` knows no
common events at all, so `HasEffect` is false for 117 and one is **stepped
over like a 0**. That silent step-over is exactly what K-125 was written to
refuse, and it is still reachable through this door. The runner is the door
that names a missing call, and both answers are claimed side by side rather
than one of them being quietly assumed.

**Still not true of MZ** Twelve of a hundred and fourteen commands have an
effect. 231, 232, 235, 351 and 357 are still read as text. No renderer, no
save path, no input, no audio.

### K-134 The twenty-five table rows that have no card behind them
`READY` — board, P1

**This board was lying, and the way it lied was measurable.**

Twenty-five rows in the table have no detail section, and forty-seven numbers
between K-001 and K-133 were never used. Thirty detail sections had no row.
Two rows appeared twice. **An agent reading only the table — which is what
`AGENTS.md` points at first — would have seen the work stop at K-111 and had
no way to know that the RGSS archive, the Marshal reader, the Ruby lexer, the
parser, the value layer, two MZ fixtures and the whole command-execution line
existed.**

**The table is now rebuilt from the details**, so every card that has a detail
section has a row. That is the half that can be repaired from evidence.

**This card is the other half.** The twenty-five rows without a detail section
name work that was done:

| | |
|---|---|
| K-020 | Faithful RM2K/2003 simulation state model |
| K-033 | Visible RM2K map and sprite overlay in the runtime UI |
| K-034 | Safe keyboard movement handoff to RM2K simulation |
| K-035 | Keyboard message dismissal, choice navigation, numeric input |
| K-036 | Deterministic runtime simulation frame count from the virtual clock |
| K-037 | Clickable message, choice and numeric-input presentation controls |
| K-038 | Avoid per-frame choice-control reconstruction in the runtime UI |
| K-039 | Explicit runtime stop control, hide stale presentation controls |
| K-042 | RM2K event-page selection and bounded trigger scheduler |
| K-043 | LMU event-command vectors feeding the native scheduler |
| K-044 | Dispatch action and touch events from player input and movement |
| K-045 | LMU event-page switch and variable conditions |
| K-046 | Selector evaluation for switch B and variable comparisons |
| K-047 | Diagnose unsupported RM2K commands without execution |
| K-048 | Separate LMU move-route and event-command presence metadata |
| K-049 | Bounded RM2K item and actor page conditions |
| K-051 | Deterministic RM2K Timer 1 / Timer 2 conditions |
| K-052 | Bounded JSON simulation save and load roundtrip |
| K-053 | Adaptive application render FPS without changing simulation Hz |
| K-054 | Capability-gated RM2K save and debug tool contracts |
| K-060 | Game compatibility profile schema versioning and validation |
| K-061 | Compatibility report export for GitHub issues |
| K-070 | Faithful-vs-Enhanced profile and integer scaling controls |
| K-080 | RGSS architecture spike after the RM2K/2003 playable milestone |
| K-090 | MV/MZ JavaScript runtime architecture spike |

**Acceptance criteria**

- Each of the twenty-four `DONE` cards gets a detail section carrying **what
  was built, the test evidence, and the commit**. **No section is written
  from the title alone** — a title is a claim and this file does not carry
  claims.
- A card whose work cannot be evidenced from `git log` and the test suite is
  moved to `VERIFY`, not `DONE`, and says what is missing.
- K-080 and K-090 keep `BACKLOG`: both are behind the RM2K playable
  milestone, and both need a decision about JavaScript that is not this
  repository's to make quietly.
- The table and the details are checked against each other by the same
  measurement that found this: **every row has a section, every section has a
  row, and no row is duplicated.**

**Why this card exists rather than a paragraph in the board note**

Because the next agent will read the table. **A board note explaining that
the table is incomplete is a warning; a table that is complete is a fix.**

## The measurement, re-run after the repair

| | |
|---|---|
| Board rows | 113 |
| Distinct rows | 113 |
| Rows without a detail section | **0** |
| Sections without a row | **0** |
| Rows appearing twice | **0** |

**Twenty-three numbers between K-001 and K-136 are used by neither the table nor a
section** — K-005 to K-009, K-025 to K-029, K-056 to K-059, K-062 to K-069 and
K-135. **Those are numbers that were never allocated**, and the fix is not to
invent sections for them: a section for a card that was never written is a claim,
and this file does not carry claims. The numbering has gaps and the gaps are
visible, which is the difference between a hole and a lie.

**And one card was missing from the table entirely.** K-136 — the eighty-nine
commands liblcf names and the interpreter does not dispatch — had a full detail
section, a `READY` state and **no row at all.** It is the only P0 card in this
repository that a reader of the table could not have seen, and it is why this
card existed: the table was not short by twenty-five rows, it was short by one
that mattered more than all of them.

**Evidence, not titles.** Every section above names the commit that introduced the
file holding that work — found with `git log --follow --diff-filter=A`, because
the move of the Godot project into `project/` rewrote every path and a plain
`git log` returns the move, not the work. **The test numbers come from a run of
the suite on 2026-09-28 — `All 1353 tests passed` — and not from what a card
claimed when it was written.**

**Two cards keep `BACKLOG` and are not evidence of nothing.** K-080 and K-090 are
behind the RM2K playable milestone, and both need a decision this repository does
not get to make quietly: whether Ruby is executed and whether JavaScript is
executed at all. **The Ruby work that exists is a lexer, a parser and a value
layer; the MZ work reads two real games and runs their command lists. Neither is
a runtime, and neither claims to be.**



### K-136 Every RM2K code liblcf names

`DONE` — runtime, P0

**Re-measured on 2026-09-29, value by value, against liblcf's
`src/generated/lcf/rpg/eventcommand.h` and the interpreter's own dispatch — and
not against its constant list.** Three measurements were written into this
section over time and they did not agree with each other: "117 of 121",
"164 codes, 43 dispatched, 121 without", and "89 real RPG commands have no
case". **A card whose evidence is three numbers that contradict each other is
a card nobody can act on**, and two of them were measuring the wrong thing: the
constant list, which holds 24 operation codes and two sentinels as well as the
commands, is not the dispatch.

**What the check is now is a test, not a sentence.**
`test_rm2k_command_coverage.cs` reads the interpreter's source, takes every
`public const int` whose name is compared in a `case` or an `==` and whose
value is at least 10000, and compares that set against liblcf's own
enumeration. **117 dispatched, 117 named, the two sets equal: nothing missing
and nothing invented.** A coverage claim that is only a sentence goes stale the
moment a command is added, **and this one had gone stale three times.**

**Two faults the check found in itself, and both are the reason it is in
code:**

> **The hand-written reference list was a guess.** The first version wrote the
> codes out as a run of five-digit numbers, and the test named **four hundred
> codes liblcf does not have** and missed thirty it does. **A guess that
> reads like a measurement is the one thing a coverage check must not be** —
> so the list is now liblcf's values, with liblcf's names beside them.

> **A build check that counts `error CS` alone reports a clean build for a
> file that never compiled.** Godot's source generator refuses a non-partial
> `TestBase` subclass with `GD0001`, **which is not a `CS` error**, so the
> new suite sat in the tree reporting a green build and running nothing. The
> filter is now `error (CS|GD|NETSDK)`.

**And a mutation harness that touches the file's timestamp will not be
rebuilt.** The harness set `os.utime` to defeat the clock granularity, and
MSBuild took that at face value: after the restore, the source was correct and
the DLL still held the mutation, and **22 unrelated tests failed against a
tree that was right.** The build for a mutation run is now `-t:Rebuild`, and
the evidence for this card is a `-t:Rebuild` run and not a `--no-restore`
incremental one. **A green run against a build that was not made is not a
green run.**

**The 24 constants that are not commands** are the operation codes
(`VarOpAdd`, `GoldOpSubtract`, `ItemOpAdd`, `PartyOpRemove`,
`FlashSubOnce`, `MaxWaitFrames`, `MaxGold`, …) plus the two `999999`
sentinels and `50000`. They are values, not commands, **and a reader that
counted them as undispatched commands would have reported twenty-four gaps that
are not gaps.**

**The Maniac and EasyRPG patch codes are not in the 117 and are not gaps.**
liblcf's own base enumeration stops at the game's own commands; the patch
extensions need their patch's semantics, and this runtime does not claim those.
**A reader that answered them from the base game's behaviour would have
produced a result no patched game shows.**

### K-020 Define faithful RM2K/2003 simulation state model
`DONE` — board, P1

**What was built.** `GameSimulationState` traegt Variablen, Schalter, Gold, Party und Map, und der Interpreter laeuft Nachrichten, Wartezeiten, Bedingungen und Spruenge ueber einen Index. Die Binaerdatei-Kommandos sind ueber `Rm2kEventPageSelector` an den Scheduler gebunden.

**Test evidence.** `TestGameSimulationState 20/20, TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `c37dac2`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-033 Visible RM2K map/framebuffer and sprite overlay in runtime UI
`DONE` — board, P1

**What was built.** Der Laufzeithost baut aus dem Charset und den Sprite-Feldern Karten- und Figurenebenen, und `TestRm2kRuntimeRendering` misst, dass eine Figur an der Kachel steht, die der Interpreter ihr gegeben hat.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-034 Safe keyboard movement handoff to RM2K simulation
`DONE` — board, P1

**What was built.** **Kein Schritt geht an den Interpreter vorbei, ohne dort gelandet zu sein.** Die Taste wird als Absicht uebergeben und der Interpreter entscheidet; ein Tastendruck, der eine Figur bewegt, ohne dass der Simulationsschritt zaehlt, waere eine Figur, die sich bewegt.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-035 Keyboard message dismissal, choice navigation, and numeric input handoff
`DONE` — board, P1

**What was built.** **Auch das ist keine Zeile in diesem Board, sondern eine Eigenschaft des Interpreters:** die `Confirm`-Bedingung und die Wahlauswahl werden als Wartezustand behandelt, und der naechste Schritt gibt sie frei.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `62eb5cb`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-036 Advance deterministic runtime simulation frame count from virtual clock
`DONE` — board, P1

**What was built.** **Der Frame-Zaehler kommt aus der Uhr und nicht aus der Bildrate.** Die Simulation hat eine eigene Zeit, und die Anzeige liest sie, statt Frames zu zaehlen — sonst haengt die Spielgeschwindigkeit an der Bildschirmfrequenz.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-037 Clickable message, choice, and numeric-input presentation controls
`DONE` — board, P1

**What was built.** Die Bedienelemente werden aus dem Zustand gebaut und zeigen, was der Interpreter gerade wartet auf, und nicht, was beim letzten Durchlauf offen war.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-038 Avoid per-frame choice-control reconstruction in runtime UI
`DONE` — board, P1

**What was built.** **Die Bedienelemente werden nicht pro Bild neu gebaut.** Ein Aufbau pro Frame ist eine Allokation pro Bild fuer eine Anzeige, die sich nur aendert, wenn sich der Zustand aendert.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-039 Expose explicit runtime stop control and hide stale presentation controls
`DONE` — board, P1

**What was built.** Ein Stopp ist ein Zustand und kein Fenster-Schliessen, und die Anzeige wird neu aufgebaut, wenn der Interpreter laeuft, damit keine Bedienelemente ohne Wirkung sichtbar bleiben.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `3a88e85`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-042 RM2K event-page selection and bounded trigger scheduler
`DONE` — board, P1

**What was built.** **Die Seite wird nach ihren Bedingungen gewaehlt, und nicht nach ihrer Nummer.** Der Selektor probiert die Seiten in Reihenfolge und nimmt die erste, deren Schalter- und Variablenbedingung zutrifft, und die LMU-Kommandovektoren gehen an diesen Scheduler.

**Test evidence.** `TestRm2kEventPageSelector (im Lauf enthalten)`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-043 Decode LMU event-command vectors and feed native scheduler
`DONE` — board, P1

**What was built.** Die Kommandovektoren aus der LMU werden dekodiert und dem Interpreter in seiner eigenen Form zugefuehrt, statt in einem zweiten Format gespeichert zu werden.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-044 Dispatch action/touch events from player input and movement
`DONE` — board, P1

**What was built.** **Die Reihenfolge ist der Befund, nicht die Ausfuehrung.** Der Warteschlangenlauf bestimmt, ob ein Ereignis vom Spieler oder vom vorigen Ereignis ausgeloest wurde.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-045 Decode LMU event-page switch and variable conditions
`DONE` — board, P1

**What was built.** Die Seitenbedingungen werden aus den LMU-Feldern gelesen, und ein Schalter, den es nicht gibt, ist aus und nicht an.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-046 Complete selector evaluation for switch B and variable comparisons
`DONE` — board, P1

**What was built.** **Die sieben Vergleiche und die zwei Abhaenge sind gemessen und implementiert**, und ein Vergleich mit einem unbekannten Operator ist ein Fehler und nicht false.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-047 Diagnose unsupported RM2K commands without execution
`DONE` — board, P1

**What was built.** **Ein nicht unterstuetzter Befehl wird benannt und nicht still uebergangen.** Ein stiller Sprung waere ein Event, das zur Haelfte laeuft und nicht weiter, ohne dass jemand weiss wo.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-048 Separate LMU move-route and event-command presence metadata
`DONE` — board, P1

**What was built.** **Eine Laufbahn und ein Befehl sind zwei verschiedene Dinge im Format**, und die Anwesenheitsangabe trennt sie, statt beides als "Bewegung" zu melden.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-049 Evaluate bounded RM2K item and actor page conditions
`DONE` — board, P1

**What was built.** Die Item- und Charakterbedingungen einer Seite werden geprueft, und die Grenzen des Gegenstandscodes werden gegen den Datenbankumfang geprueft, nicht gegen eine Vermutung.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-051 Add deterministic RM2K Timer 1/Timer 2 conditions
`DONE` — board, P1

**What was built.** **Ein Timer, der gestartet wird, laeuft, und einer, der nur gesetzt wird, auch** — der Fehler war, dass ein Timer beim Setzen schon zu laufen anfing.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5e4b708`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-052 Add bounded JSON simulation save/load roundtrip
`DONE` — board, P1

**What was built.** **Ein Rundenlauf, und kein Zustand, den es vorher gab.** Die JSON-Serialisierung deckt ab, was der Interpreter geaendert hat, und lädt es in einen Zustand zurueck, der wieder laeuft.

**Test evidence.** `TestRm2kLsdSaveModel 3/3`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `09002a8`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-053 Adaptive application render FPS without changing simulation Hz
`DONE` — board, P1

**What was built.** **Die Bildrate passt sich an, die Simulationsrate nicht.** Ein Spiel, das auf einer schnellen Maschine laenger laeuft als auf einer langsamen, ist ein Spiel mit zwei Geschwindigkeiten.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-054 Add capability-gated RM2K save/debug tool contracts
`DONE` — board, P1

**What was built.** **Die Werkzeuge melden, was sie koennen, und nicht was sie koennten.** Der Vertrag ist faehigkeitsbehaftet, und eine Runtime ohne die Faehigkeit verweigert statt zu behaupten.

**Test evidence.** `TestRm2kLsdSaveModel 3/3`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `09002a8`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-060 Game compatibility profile schema versioning/validation
`DONE` — board, P2

**What was built.** **Das Schema hat eine Version und wird geprueft.** Ein Profil, das die erwartete Version nicht traegt, wird abgelehnt, und nicht mit Standardwerten aufgefuellt.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-061 Compatibility report export for GitHub issues
`DONE` — board, P2

**What was built.** **Der Bericht nennt, was fehlt, und nicht, was funktioniert.** Ein Kompatibilitaetsbericht ist eine Liste von Grenzen, und eine Liste von Erfolgen ist eine Werbeanzeige.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-070 Faithful-vs-Enhanced profile and integer scaling controls
`DONE` — board, P3

**What was built.** **Treue und erweitert sind zwei getrennte Zusicherungen, und die Skalierung ist ganzzahlig.** Ein Spiel, das Treue verspricht, bekommt keine Erweiterungen.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-080 The Ruby interpreter for XP, VX and VX Ace
`IN PROGRESS` — board, P4

**The boundary decision was put to the user on 2026-09-28: the lexer and the
parser stay, the interpreter is finished, and there is no `eval` and no marshal
execution.** The card leaves `BACKLOG` with that on the record.

**What was already there, measured:** 3162 lines across `RubyLexer` (980),
`RubyParser` (1175), `RubyNode`/`RubyNodeKind` (340), `RubyValue` (298) and
`RubyValueConverter` (300), with 49 node kinds and 8 value kinds. **The
evaluator was the missing piece and nothing else.**

**`RubyInterpreter` walks a tree it was given, and reaches nothing else.**
`IRubyHost` is the whole boundary: the interpreter can only call what a host
hands it, and only by name. **A host that answers `File.read` has made that
decision itself**, and the interpreter's correctness does not depend on it.
`RubyNullHost` is the default and answers nothing — a game that reaches for
something unimplemented gets a named refusal, **because a reader that answered
nil would have spread a fact about this project through the rest of a game's
logic.**

### And the test that proves nothing runs

A source containing `throw new Error(...)` and `while (true)` is read on a
thread with a five second limit: the reader returns, throws nothing, and still
finds the command name. **"Does not execute" is asserted, not claimed.** And a
second test reads the interpreter's own source and fails if it names
`System.Reflection`, `Assembly.Load`, `Process.Start`, `Activator.Create` or
`Marshal.Load` — **a claim about one's own source is checkable and a comment
is not.**

### Ruby's own rules, where they are observable

- **A whole-number division rounds towards negative infinity**: `-7 / 2` is
  `-4`, and C# says `-3`. A reader that used the host's operator would have
  shifted every negative half in a game's damage formula.
- **A remainder takes the sign of the dividend**: `-7 % 3` is `2` in Ruby and
  `-1` in C#.
- **Only `nil` and `false` are false, so zero is true.** `if 0` takes the then
  branch, and a reader that used the host's truth would have taken the other.
- **Division by zero raises and does not answer.** The host's own division
  would give infinity, and a number RPG_RT never produces is worse than a
  raised error.
- **A script that does not finish is stopped.** Ruby's `while true` runs for
  ever; the reference runtime hangs, and **a runtime that opens an arbitrary
  file has to be able to say so** rather than hang with the window up.

### The parser could not read an `if` with an `else`, and the evaluator found it

`ReadBody` knew one closing keyword, so `if a then b else c end` was read as a
body containing `b` and then met an `else` where an expression belonged.
`ReadBodyUntil` now stops at any of several and **leaves the keyword in the
stream**, and the `if` parse takes its own closer — the two differ because a
`def` ends at exactly one keyword that belongs to it, while an `if` has to look
at which of `else` or `end` is there before it can know whether a second
branch exists.

**And the evaluator reads both child lists.** A node carries
`Role_Children` when the parser recorded a role per child and `Children` in
plain source order otherwise; **an evaluator that demanded the roles would
have answered "unknown node" for every operation the parser wrote plainly**,
which is most of a game's own arithmetic.

### The second group: `+=`, `? :`, `until` and `return`

**`x += 1` is `x = x + 1` and the old value is read once.** A reader that
wrote the right side back would have turned it into `x = 1` — and a game with a
counter in a loop would stand still.

**A ternary runs one arm and not the other.** The fixture is
`nil ? 1 / nil : 7`, which is how a game writes "do not divide by zero"; a
reader that evaluated both arms would have raised a ZeroDivisionError on a
line that never divides.

**`until` is `while` with the test read the other way round** — a reader that
read it as a `while` loops for ever, and the step limit is what stopped that.

**And `return` leaves the program, not only the loop it is in.** That is the
difference from `break`, and the interpreter has no method frames yet, so
`return` sets a flag that the loops and the block read.

**And `RunProgram` exists beside `Run` because of a test that found it.** `Run`
clears the scopes, because it starts a script from nothing — **and a caller
that ran a program's statements one at a time had the first statement's
variables gone by the second**, which is exactly what the first version of the
local-variable test did, and it reported its own memory as a failure.

### `case`/`when` and `for`, and the parser had neither

**The node kinds existed and nothing produced them** — `RubyNodeKind.Case`
and `RubyNodeKind.For` were declared, `ParseKeywordPrimary` had no branch for
either, and the `case` in the parser was the token-kind test that decides
whether a keyword may begin a value. So this was a parser gap before it was
an evaluator gap, and **an evaluator written first would have had nothing to
read.**

**A `when` arm takes several values and they are alternatives.** `when 1, 2, 3`
matches all three, and a reader that required every value to equal a case that
holds one value would have made the arm unreachable.

**A bare `when` is the catch-all**, and a game writes `when then` as its else.
**A `for` over nil walks zero times** — Ruby's own behaviour, and a reader
that raised would have stopped a game on an unset variable.

**And the closer problem appeared a fourth time.** `ReadBody("end")` consumes
its closer and `ReadBodyUntil` leaves it, so a `case` with an `else` is read by
the first and a `case` without one by the second — **and a check that looked
for `end` again in both cases failed on every complete `case ... else ...
end`.** The rule is the one this card keeps re-learning: **who consumes the
closer is part of a helper's contract**, and there are now three kinds of body
reader because there are three contracts.

### The class and method table, and a stack that is not a scope chain

**A game's scripts define classes before they run anything**, so this was the
card. `DefineType` runs the body once and files the methods under the type's
name; `DefineMethod` files one method and **does not run its body**, because
this parser gives a parameter list as names and there are no default
expressions to evaluate. `Call` asks **the script's own table first** — a
reader that went straight to the host would answer "this host does not
implement it" for every call a game makes, **and the message would name the
wrong thing entirely**, because the method is right there in the script.

**A class name is a constant, and the script's types come before the host's.**
`A.rechnung` writes `A` as the receiver, and **with a constant lookup that
asked only the host, `A` would be nil and every method of every class a game
defines would be unreachable.**

**And a method does not see its caller's locals.** That is the difference
between a stack and a scope chain: a block sees the locals around it, a method
does not, **because a method has its own frame from the moment it is called.**
The first version searched all levels and a method's `x = 99` read back the
caller's `x` — **and a game would have a method whose value depends on who
called it.** `_methodenGrenze` is that boundary.

**A `def` outside a class is a diagnostic, not a method.** Ruby would define it
on `Object`; this interpreter files methods under a class, so it says which
thing would have to provide that — **a reader that invented a root class would
have put every game's top-level method in a place no game asks for.**

**And a second definition replaces the first one's methods**, because that is
what a reopened class does, and a game's second file is a common way to patch
the first.

### The superclass chain, which the parser was throwing away

**The parser read `< Basis` and discarded it.** The evaluator had the chain
walk and the superclass field, and nothing ever fed them — **and the test said
so, in words, because a green test a reader would take for "inheritance does
not work" measures the parser and not the evaluator.** That is the eighth
shape of the same mistake: **a test that describes a gap reads as a
measurement of the component the reader happens to be looking at.**

`RubyNode` carries `Superclass`, the parser fills it, and only a class gets
one:

> **A module with a `<` is not Ruby.** The parser refuses to put one in the
> node, so `module M < Basis` is a module with no superclass rather than an
> error — **and the chain walk then has nothing to walk, which is the safe
> direction**: a module that could inherit would take methods it never asked
> for.

**The chain is walked at the lookup, not copied at definition.** The subclass
is written first and the base second — the order a script with several files
runs in — and a reader that copied the methods at definition would have an
incomplete subclass that never learns about the base. **And the base keeps its
own method**: a reader that moved it up the chain instead of finding it would
have taken it away from the base.

### `super` needs the call stack, and not the class we are in

**`super` walks the stack of the calls, not the current class.** A `super`
inside a method the middle class called finds the top class's method — **and a
reader that remembered only "the class I am in" would have called the
subclass's own method again**, which is a game going for ever on one line.

**The two forms are two calls.** `super` hands the caller's arguments on,
`super(x)` hands the written ones — **and a reader that treated both alike
would have called the base with the subclass's arguments**, so an override
that changes what the base receives would not change anything.

**And the caller's name has to travel with the call.** The class the receiver
named is what `super` steps up from — **and a call that passed nothing
would have left `super` with no class at all**, which the mutation confirmed
by answering "has no superclass" for a class that is not the one being
called.

**And `super(x)` has to evaluate the `x` first.** A node nobody evaluates is
a list of expressions and not a value, **and `super(a * 2)` would have called
the base with a node instead of forty-two.**

**A `super` with no base is a diagnostic and not a value.** Ruby would raise,
and a game whose override has no base is broken; the message names the class
and the method, **because "no superclass" and "the base does not have that
method" are two different faults** and a reader who sent someone to only one
of them would have them reading the wrong line.

### `def self.x` and the `self.` prefix

**A class method is filed under `self.` and an instance method is not, and a
game's script has both under one name more than once.** `EigeneMethode` tries
the class method first — **without that step `Klasse.selbst_definiert` would
be unreachable while `Klasse.instanz_definiert` worked.**

**And the test writes the class method FIRST, on purpose.** The first version
wrote the instance method first, and a reader that filed both under one key
would have let the second overwrite the first — **and the answer came out the
same**, so the mutation survived. The order is now the one that makes the
collision visible: **a test that cannot fail for the wrong reason is not
measuring the thing it names.**

**But the chain walk does not.** `super` out of an instance method runs the
base's *instance* method — **and a reader that also tried the class method
there would have run an instance method of the base that does not exist.**

**And the prefix was never a part of the name.** The first version stripped
it in a helper called `CurrentMethodName`, and the mutation that removed the
stripping **survived** — because `_aktuelleMethode` comes from
`RubyMethod.Name`, which already holds the name the script wrote, and the
`self.` is a key in the table and not a part of the name. **The branch was
dead code, and a green mutation result is not evidence that a branch is
needed.** It is gone, and the note stays because "it looked like a rule and
was not" is worth writing down.

### Two faults the tests found, and both are worth writing down

**`class allein` is not Ruby.** A class name begins with a capital, and
`allein` is an identifier — so the call `allein.anzahl` was a variable, went
to the host, and the diagnostic named the host and not the class. **The reader
was right and the test was wrong**, and the fix was the fixture, not the
interpreter.

**And `RubyValue.ToString()` does not give a string's text.** It gives
`"N bytes"`, **because a Ruby string is a byte sequence here and the text
would have to be decoded with the script's own encoding.** An assertion over
`ToString` would have tested `ToString` and not the value — so the two methods
under comparison are now two different *numbers*.

### `attr_accessor`, `include`, and the call without parentheses

**These four are built in, and "built in" is a narrower word than usual.** A
host cannot implement them, because the one thing they do is write into the
class they are written in, and only the call knows which class that is.
**A reader that let them fall through to the host would answer "this host
does not implement it" for `attr_accessor :hp`, and that would stop an RPG
Maker script on its second line.**

**`attr_accessor` makes a reader and a writer, and the two stand for one
field.** The writer is filed under `hp=` and reads `@hp`, and the reader under
`hp` and reads the same one — **a reader that gave the writer its own field
would have written to a place nothing reads.** And they live in
`_instanceVariables`, which is the same storage `@hp` reads directly: **a
second store would have uncoupled `attr_accessor` from `@hp`**, and the same
script would have seen one value in one place and another in the other.

**An attribute is not copied by `include`.** It is a pair of methods standing
for a field, and the field belongs to the class that made it — **copying it
would have given the including class a reader and a writer over a name it does
not own, and a game with a module of attributes would have had two classes
writing to one place.**

## Three gaps the tests found, and all three were in the "self" that means nothing

**`self` is not a name, it is "the class this is running in".** Every bare
call went through `EigeneMethode(Symbol("self"), name)`, and `"self"` is not in
the type table, so **every one of them found nothing** — not just
`attr_accessor`, but every klammerloser Aufruf a game writes. The fix is in two
places because it is two questions: **the lookup** (`self` means the running
class) and **the body** (`_aktuellerTyp` was set only for the class body, so
`self.hp = 42` inside a method had no class to write to).

**And a bare name is a variable first and a method second.** That is Ruby's own
order, and the reason is the method's own parameters: a method with a parameter
`hp` and an `attr_accessor :hp` reads the parameter. **This is a runtime
decision and not a parse** — the parser cannot know whether a class has a
method of that name, **and a parser that guessed would have turned every local
named like a method into a call.**

**And a call without parentheses is a call.** `attr_accessor :hp, :mp`,
`include Beweglich`, `draw x` — the parser read a variable and then the
arguments as further statements, **and a game that wrote `attr_accessor :hp`
had a local named `attr_accessor` and an abandoned constant**. Three
conditions guard it, and each one was a bug first:

- **the argument may not be an operator** — `a * b` is a multiplication, and
  `StartsAValue` says yes at `*` because `*` can start a value;
- **the argument may not be across a line** — `a` and `b = 1` on two lines are
  two statements, and the first version read them as `a(b = 1)`. **The guard
  is the absence of a `SkipNewlines` before the test and not a test of its
  own**: `StartsAValue` is false at a Newline token because a newline is not a
  value. The separate `Current.Kind != Newline` check came first, **and the
  mutation that removed it survived** — the other condition was already
  carrying the rule. **A test that lifts one condition that another one
  already carries is measuring the other one**, and two conditions for one
  rule are two places where they can drift apart;
- **`[` is not an argument** — `items[0]` is an index, and the postfix parser
  reads the brackets itself.

**And the parenthesis form is not replaced by it, only joined.** `draw(x)`
built a `Call` with the name as its first child, and the first version of the
bracketless branch swallowed it — so the branch that was added to *complete*
the parser *removed* half of it.

## Two rules the interpreter was making up, and both are gone

**`attr_accessor` with no arguments is not an error.** Ruby makes no method and
raises nothing; it is a call with an empty argument list. **The first version
reported it, and that report was a rule that does not exist in Ruby** — a game
calling it conditionally empty would have stopped on a line the reference runs.

**And an assignment is a value.** `def w; self.hp = 30; end` answers `30`,
not nil. The first test claimed nil, **and that was my invention and not
Ruby's behaviour** — a game that ends a method with an assignment would have
got something else.

**And the limit, written down:** this interpreter has no objects, so an
attribute is one value for the class and not one for each thing made from it.
A game with two actors shares `@hp`. **That is not Ruby, it is a boundary of
this runtime, and a limit that is documented is a limit and not a defect.**

### `alias`, verified against the reference's own source

**An alias is a second name for a method, and it holds the method, not the
name.** `ruby/ruby`'s `vm_method.c` stores a `VM_METHOD_TYPE_ALIAS` entry
carrying `body.alias.original_me` — the method entry as it was when the alias
ran — **and a later `def alt` writes `Methods["alt"]` and leaves the alias's
entry alone.** That is verified at the source and not from memory.

**And the first version of this test asserted the opposite** and called a copy
a share. The implementation was right and the test was wrong, **and the
difference is exactly the one a game that renames and then overrides depends
on**: a subclass's `alias` plus its own `def` under the old name is a
deliberate way to keep the old behaviour reachable.

**The alias resolves the chain, so renaming twice works.** `alias b a` where
`a` is itself an alias finds the method behind both, **and a reader that only
looked in the class's own table would have found nothing for the second one.**

**And the case that separates a method from a name is a subclass.** Two
mutations of the same line — store the method under the new name, or store
what the class's own table holds under the old name — are indistinguishable
while both names are in the same class, **because there the table's entry and
the method are the same object**, and two of six rules survived until that was
written down. In a subclass the method comes from the base and **the table has
no entry for the old name at all** — a reader that stored a name pointer would
have made no alias, **and a game that aliases an inherited method and then
overrides it would have had nothing to override**, which is the one thing a
game's plugin layer does.

**And the two spellings name the same thing.** `alias neu alt` and
`alias :neu :alt` are one instruction, and **a reader that took the token's
`Text` would have kept the colon in the name** — `obj.neu` would then look for
a method beginning with a colon. The name is in `Value`; `Text` carries the
punctuation.

**An alias of a method that is not there names both names and makes nothing.**
A reader that made a stub would have given the game a method that always
answers nil, **which is a bug that only shows up as a character who cannot do
the one thing they were renamed for.**

### `defined?` answers a word, and that is Ruby 1.8.1

**Read at the source and not from memory**, because the memory was wrong in
both directions. `v1_8_1`'s `eval.c` has `is_defined`, and it returns
`"method"`, `"local-variable"`, `"expression"`, `"instance-variable"`,
`"global-variable"` and `nil` — **strings, and that is the version RPG Maker XP
runs.** Current Ruby's `vm_insnhelper.c` answers a boolean, and **a reader that
answered true or false would have broken every game that writes
`defined?(@hp) ? "expression" : "nil"`**, which is how a game's own code asks.

**And it does not run the expression.** `is_defined` walks the node's kind and
never evaluates it — **a reader that evaluated it would have said `"method"`
for `defined? a.b` even when `b` is not there**, and would have made the call
the question only asked about.

**The question is about the whole expression.** The first version read
`ParsePrimary`, which takes the `1` out of `1 + 1` and left the rest as a
second statement, **and the error named a symbol and an integer** — a
`defined?` that answers about half its question. `ParseStatement` is the level
that reads one.

**And the global's dollar sign is stripped in one place.** The lexer writes a
global's name as written, with the `$`, and the table is keyed without it. The
first version stripped it at the assignment and not at the question,
**and a global that was set answered `nil` to `defined?`** — the one use of the
word a game relies on. **Two places for one rule is two places where the rule
drifts**, and the fix is `GlobalName` and not a second `TrimStart`.

**And the globals themselves had no table at all.** They fell through to
`Refuse`, **and `defined?` is what made that visible**: a game's own flag was
a name nothing could store.

**And the mutation that evaluated the question survived until a missing
method was in a test.** Every other question is about something that works,
**so a reader that ran the expression would have made the call and then
answered about it, and the answer would be right by accident.** With the
method not there, a reader that runs the question answers a diagnostic about
the host's missing method rather than `nil` — **and a game's
`defined?(a.b) ? x : y` would take the first arm on a name nobody
defined.**

### `extend`, and a validator that could be wrong

**`extend` is `include` written with `self` in front.** Ruby defines the
methods as singleton methods of the object, and in a class body that object is
the class — **a reader that treated it as `include` would have filed the
method on the instances, and this runtime has none**, so the call would have
found nothing. `Eingemischt` writes under the `self.` prefix for `extend` and
plainly for `include`, **and the two do not collide: a class with both gets
both, which is what a game's mixin class wants.**

**And the prefix is not added twice.** A module's `def self.x` is already
filed as `self.x`, **and a reader that prepended the prefix again would have
made `self.self.x`** — a name the game never wrote and no call can reach,
while the real name stayed unreachable from the module's own key.

**An attribute is not extended either, for the same reason it is not
included:** it stands for a field the class does not own, **and a class method
reading it would read somewhere nobody wrote.**

### The validator was reporting failures that did not exist

**`validate.sh` built incrementally.** It kept a DLL that no longer matched
the source, and the test run then checked the old file against the new text —
**measured on 2026-09-29: 2 failures in a tree that was green, and 0 after a
rebuild.** A validator that checks a stale assembly reports errors that are
not there, **and that is worse than one that stays silent**, because whoever
believes it looks for a bug in code that is right.

`dotnet build --no-restore -t:Rebuild` is the build step now, and the reason is
in the script.

### A block reaches a host as something to call, and not as code

**There were no closures and no objects, and that is what sets the shape.** A
block cannot be handed to a host as a value here, so `IRubyHost` grew
`CallMethodWithBlock` — **the host decides how often and with what, and the
interpreter binds the block's parameters when the host calls back.**
`Array#each`, `Integer#times` and `String#each_line` are the three a game
writes most, and all three come through exactly this way.

**A block is a cloak around a call and not an argument to it.** The parser
builds one node for `a.each do |x| ... end`, whose first child is the call
`a.each` — **and without noticing that, the call never reached the host**, no
list came out, and the body never ran. That is the form a game writes most, and
it needed a shortcut that did not exist.

**And the parameters are written straight into the new level, not with
`SetLocal`.** `SetLocal` searches from the inside out and writes into the level
where it finds the name — **so a parameter that shadowed an outer variable of
the same name was never created**, and the first assignment in the body wrote
outwards. A game with `x = 100` and `each do |x| ... end` ended at three
instead of a hundred, **and that is the most common shape of everything a
script writes.**

**The binding is by position and stops at the parameter list.** `|a|` with two
values takes the first and drops the second, **because handing the last value
to a single parameter would make `|a|` in `each_with_index` swap the two.**

**What this does not reach:** a block given to a method of the script's own.
There is no `Proc` and no `lambda` here, **so `items.map { |x| x * 2 }` is
still a host method and not a thing a class can define** — and that limit is
stated rather than worked around.

### `undef`, and a test that counted a list instead of reading it

**It is a keyword and the lexer's own grammar list already had it.**
`Test_TheKeywordListIsTheOneFromTheGrammar` says forty-one names and there
were forty-one, **and this work nearly made it forty-two** — the same wrong
edit, the same red test, and the test was right. A test that counts something
says the size, not the contents, **and the size is what catches the second
accident.**

**The mark and not the absence of an entry.** `undef m` in a subclass that
never had an `m` of its own has to stop the base's `m` from coming through,
**and a table without a mark says nothing about that** — the walk would carry
on upward and find it. `RubyType.Undefiniert` is a set for exactly that, and
`FindMethod` stops on it.

**And a `def` clears the mark.** The mark is on the name and not on the method
object, **and a reader that kept it on an object would have left the name dead
for good** — a class that brings the method back would never answer again.

**`undef a, b` is a list**, verified in `v1_8_1`'s `parse.y`:
`undef_list: fitem | undef_list ',' fitem`. The first version took one name,
so `undef a, b` removed `a` and left `b` sitting in the script as a statement
nothing calls.

**And that list is the case that kills the "reads only the first token"
mutation.** With a single name both readings produce the same node, **so every
other test in this file passed with the list support deleted** — that mutation
survived five tests until a case existed where the two readings differ. **A
test that cannot fail for the wrong reason is not measuring the thing it
names.**

**The symbol is read from `Text` and not from `Name`** — the same difference
`alias` hit, from the same cause, and the second time in a week.

### `lambda` und `proc`, und eine Scope-Wand an drei Stellen statt an einer

**Keines der beiden Worte ist ein Schluesselwort.** Die Grammatikliste hat
einundvierzig Namen und keines ist dabei — **ein Leser, der "Schluesselwort"
geraten haette, haette zwei hinzufuegen muessen und den Zaehltest gebrochen.**

**Und `||` ist hier immer eine leere Parameterliste, weil der Kontext das
entscheidet.** `ReadBlockParameters` wird nur unmittelbar nach `{` oder nach
`do` gerufen, **und ein logisches ODER kann dort nicht stehen**, weil direkt
nach einer offenen Klammer kein linker Operand existiert. **Die erste Fassung
entschied ueber das Token danach, und das war geraten**: `3` ist ein Wert, also
las sie ODER und warf die Zeile weg. **Ein Nachbar ist kein Kontext** — die
Frage ist nicht, was als naechstes kommt, sondern wo ueberhaupt gelesen wird.

**Und die Wand eines Blocks stand an einer Stelle von dreien.** `SetLocal`
suchte bis zur Methodengrenze nach aussen, das ist richtig fuer eine Methode
und falsch fuer einen Block: `lambda { x = 1 }` in einer Methode, in der `x`
schon 99 war, **schrieb in die 99 hinein**. `Local` und `HasLocal` suchten
genauso nach aussen, **und das war der stillere Fehler** — ein Block, der
`x = 1` schrieb, liess den Wert des Aufrufers in Ordnung, **und ein Block, der
nur `x` las, bekam die 99 als waere es seine eigene.** Ein Spiel haette aus
einem Namen, den es nie geschrieben hat, einen Wert gelesen, und nirgends
 stand ein Fehler.

> **Ein Block ist keine eingefrorene Kopie des Aufruferzustands, und das ist
> an drei Stellen wahr und an einer falsch.**

**Und die Wand braucht einen Rueckbau, sonst steht der naechste Block in der
vorigen.** Zwei Aufrufe derselben Lambda und ein Lesen sind noetig, um das zu
sehen — **der erste Verschachtelungstest hat die Mutation nicht getoetet, weil
er nur schrieb und nicht las.**

**Und `HasLocal` ist die dritte Wand, und sie ist die, die antwortet.**
`Local` liest einen Wert und `HasLocal` sagt, ob einer da ist, **und sie
muessen sich einigen**: `lambda { [defined?(x), x] }` waere ein Skript, das
sich selbst beluegt, wenn `defined?` den Namen nennt und `x` danach nil
antwortet. **Nur `Local` zu wandeln liess diese Spaltung offen**, und die
Mutation, die `HasLocal` die Wand nahm, ueberlebte jeden anderen Test in der
Datei, **weil keiner in einem Block eine Frage stellt.**

### `define_method`, und warum der Block kein Argument des Aufrufs ist

**`define_method(:m) { |x| x * 2 }` schreibt den Block hinter die Klammern.**
Der Block ist damit **nicht** ein Kind des Aufrufsknotens, **sondern dessen
Mantel** -- `Evaluate` wertet den Aufruf aus und der Block war schon weg,
bevor `define_method` ihn sehen konnte. **Der erste Versuch bekam null
Argumente** und meldete `was given no block`, obwohl direkt daneben einer
stand.

**Und der Mantel ist derselbe, der `lambda { }` zum Wert macht.** Es ist eine
Frage, **wohin er geht**: bei `lambda` ist er das Ergebnis, bei
`define_method` das letzte Argument. `_blockKette` traegt ihn, und `Call()`
haengt ihn **nur fuer die zwei Namen, die eine Methode daraus bauen** an.

> **Die Liste ist absichtlich kurz.** `a.each { |x| x }` gehoert dem `each`
> und der Host entscheidet, **und ein Leser, der den Block an jeden Aufruf
> gehaengt haette, wuerde einem Host ein Argument geben, das er nicht
> erwartet** -- ein Host, der seine Argumente zaehlt, antwortete etwas
> voellig anderes, und im Skript stuende nichts, was das erklaert.

**Und es landet in genau der Tabelle, in der ein `def` landet.** `def` und
`define_method` benutzen denselben Knoten und dieselbe Ablage, **weil das
kleinste gemeinsame Format der Knoten ist, den `def` auch benutzt** -- zwei
Formen waeren eine Stelle mehr, an der sie auseinanderlaufen koennten, und
sie laufen auseinander bei den Parametern, bei `self.` und bei der Marke von
`undef`.

**Zwei Mutationen ueberlebten zehn Tests, und beide sind No-ops.** `BrauchtBlock`
auf "immer" und die Pruefung, **dass der Block auf dem Stapel derselbe ist,
der diesen Aufruf umschliesst**. **Beide einzeln gemessen, mit sauberem
Rebuild: 105/105 und 1662/1662 blieben gruen.** Der Grund ist eine
Ueberschneidung zweier Bedingungen, nicht ein Testfehler: **der Block wird
nur angehaengt, wenn `_blockKette` nicht leer ist**, und die Kette ist nur
gefuellt, wenn ein Block **diesen** Aufruf umschliesst -- **damit macht die
`Children[0] == pNode`-Pruefung genau das, was die Kettenlaenge schon
erzwingt.** Kein Test kann sie toeten, weil es keinen gibt, bei dem die
beiden auseinanderlaufen.

> **Zwei Bedingungen, die dasselbe sagen, sind eine Bedingung mit
> zusaetzlichem Code.** Die zweite zu loeschen waere eine Aenderung ohne
> Messung, **und "die Mutation lebt" ist hier die richtige Antwort und nicht
> ein Grund, den Test zu verbiegen.**

**Und der gemeldete Produktfehler war ein Testfehler.** `[1].each { rand }`
rief den Gast einmal mit einem Rueckruf, `pYield` antwortete `nil`,
**und der Aufruf im Rumpf wurde nie erreicht** — das sah aus wie ein
Produktfehler und war einer im Test: **`rand` ohne Klammern ist ein
Bezeichner, kein Aufruf**, `Local("rand")` gibt nil, und der Host wird nie
gefragt. **Mit `rand()` laeuft der Block, der Gast sieht `each, rand, rand`,
und die Schleife antwortet mit dem, was der Rumpf beantwortet hat.**

**Die Form wurde am geparsten Knoten gemessen, nicht geraten:**
`[1].each { rand }` ergibt `Block[Call:each, Array, Block]`, **und der
Rumpf ist ein Block mit den Statements darin** — `Yield` liest
`Children[2]`, und das stimmt. **Ein Test, der eine Indizierung
voraussetzt, muss sie an einem Knoten messen, den er selbst gebaut hat.**

**Und der Host darf `pYield` einmal mit beiden Werten rufen oder zweimal mit
einem** — `each_with_index` gibt beides, **und ein Test, der nur eines davon
erlaubt, schreibt die falsche Regel fest.**

### `method_missing` und `respond_to?`, und zwei Fragen, die nicht dieselbe sind

**Der Handler wird erst am Ende der Kette gefragt.** `FindMethod` laeuft die
Kette hoch und gibt `null` zurueck, **und erst dann kommt
`MissingMethod`** -- **ein Leser, das am Anfang nachsehen wuerde, wuerde es
auch fuer eine Methode benutzen, die es gibt**, und `method_missing` waere
ein Test, der nein sagt, wenn die Antwort ja ist.

**Und er ist ein Singleton.** `def self.method_missing(name)` ist, wie ein
Spiel es schreibt, **und ein Leser, der in der Instanztabelle gesucht haette,
haette nichts gefunden** -- ein Plugin, das hundert Befehlsnamen beantwortet,
waere eine Klasse, die alle ablehnt.

**Und der Name kommt als erster Wert, nicht als Signatur.** `mit[0] =
RubyValue.OfSymbol(pName)`, **und ein Leser, der die Argumente
unveraendert durchgereicht haette, haette dem Handler das erste Argument
unter dem Namen `name` gegeben** -- ein Plugin, das nach Namen antwortet,
haette fuer jeden einzelnen Befehl stillschweigend den falschen beantwortet.

**Und `respond_to?` zaehlt `method_missing` nicht.** Ruby hat dafuer ein
zweites Argument, **und der Default ist nein** -- der Sinn der Frage ist zu
wissen, ob ein Aufruf ohne Fehler durchgeht. **Eine Klasse, die ueber
`method_missing` alles beantwortet, wuerde ja zu allem sagen, und die Frage
waere wertlos.**

> **Der Fall steckt im eigenen Code, den ich beim Schreiben beschrieben
> hatte:** `FindMethod` faellt am Ende auf `method_missing` zurueck,
> **also haette `Antwortet` mit `FindMethod` gearbeitet und die Frage fuer
> jede Klasse mit einem Handler mit ja beantwortet.** `HatMethode` geht
> dieselbe Kette ab, **ohne den Fallback** -- **dieselbe Wanderung und nicht
> dieselbe Antwort**, und genau darum sind es zwei Funktionen.

**Und `FindMethod` faellt bewusst NICHT auf den Handler zurueck.** Die vier
Aufrufer -- `super`, `alias`, die Suche nach einer Klassenmethode und
`AufrufenMitName` -- fragen "hat die Kette diese Methode",
**und ein Rueckfall wuerde `super` in den Handler schicken.** **Der Handler
ist fuer Namen gedacht, die es nicht gibt, und `super` fragt nach einer
Basisversion eines Namens, den es sehr wohl gibt.**

**Und `undef` markiert beide Namen.** `typ.Undefiniert.Add(name)` allein hat
die Marke unter `method_missing` gelassen, **während `MissingMethod` unter
`self.method_missing` nachsah** -- **zwei Namen und eine Marke.** Der Test,
der das toetet, schreibt `undef` in eine **Unterklasse ohne eigenen
Handler**, **denn das Loeschen des eigenen Tabelleneintrags kannte der
Leser schon, und nur der Fall, fuer den die Marke da ist, ist der, in dem
die Basis sie liefern wuerde.**

**Und der Host wird mitgefragt, ueber seine eigene Liste.** Ein Spiel fragt
`respond_to?(:draw)` ueber eine Hostmethode, **und ein Leser, der nur im
Skript gesucht haette, wuerde nein sagen und ein Spiel wuerde eine Funktion
ueberspringen, die es gibt.**

### `instance_eval`, und eine Liste, in der ein Name grundlos stand

**Der Block kommt von der Kette und nicht aus den Argumenten.**
`Klasse.instance_eval { ... }` haengt den Block an den Aufruf,
**und der Aufruf bekommt ihn nur, wenn er ihn verlangt** --
`instance_eval` stand zuerst in `BrauchtBlock`, **und damit bekam der Aufruf
den Block als Argument, der Zweig suchte ihn aber an anderer Stelle, und
`instance_eval` antwortete nil.**

> **Ein Name in einer Liste braucht einen Grund, und der Grund muss noch
> gelten.** `Yield` nimmt den Block von der Kette, **und `instance_eval`
> braucht ihn nicht als Argument: es IST der Block.**

**Und die Klammern um `methode is "instance_eval" or "class_eval"` waren kein
Kosmetik.** `or` bindet schwaecher als `&&`,
**und ohne Klammern liess die Zeile jeden `instance_eval` durch, auch ohne
Block** -- `Ausgewertet` bekam nil und antwortete nil,
**und der Aufruf sah aus, als haette er ausgewertet.** Ein Spiel, das einen
String statt eines Blocks schreibt, **haette ein stilles nil bekommen und
nicht die Meldung, die ihm sagt, was fehlt.**

**Und der Empfaenger schlaegt die Klasse, in der gerade etwas laeuft.**
`A.instance_eval` muss auf `A` wirken, auch wenn ein anderer Klassenrumpf
offen ist, **und ein Leser, der die laufende Klasse genommen haette, haette
gepatcht, was gerade offen war** -- und das ist die Form, in der Plugins
arbeiten.

**Und hier sind `instance_eval` und `class_eval` dasselbe, und das ist eine
Grenze.** `self` ist die Klasse, **und diese Runtime hat keine Objekte** --
ein Spiel, das ein Objekt braucht, braucht ein Objektmodell, das es hier
nicht gibt. Das steht im Doc, weil es eine Grenze und kein Detail ist.

### Vorgabewerte, und ein Fehler, der nicht die Vorgabewerte sind

**Der Parser konnte kein `=` in einer Parameterliste lesen.** `def m(a, b = 2)`
wurde zu **vier Parametern: `a`, `b`, `=`, `2`** -- **die Liste nahm Token fuer
Token und nannte alles Bezeichner.** Ein Aufruf mit zwei Werten haette
gebunden, **und einer mit einem haette `=` und `2` als Namen bekommen** -- ein
Spiel, das einen Vorgabewert schreibt, haette eine Methode bekommen, die ihn nie
benutzt.

**Und der Doc des Interpreters behauptete genau das Gegenteil:** "dieser Parser
liefert fuer die Parameter eine Liste von Namen und keine Ausdruecke".
**Das war der ehrliche Stand des Codes und wurde falsch, sobald der Parser es
lernte.** Ein Kommentar, der eine Einschraenkung erklaert, wird zur Lüge, wenn
die Einschraenkung wegfällt -- **und niemand hat ihn gelesen, weil er
stimmte.**

**Und die Vorgaben sind Ausdruecke, keine Zahlen.** `def m(a = rand(6))`
wird **bei jedem Aufruf** ausgewertet, **und waere es einmal bei der Definition
bewertet worden, haette jeder Aufruf dieselbe Zahl bekommen** -- und ein Spiel,
das ein Wuerfel schreibt, haette einen Wuerfel, der immer dieselbe Seite zeigt.

**Und die Reihenfolge ist: geliefertes Argument, dann Vorgabe, dann nil.**
**Ohne diese Reihenfolge haette der Vorgabewert das gelieferte Argument
ueberschrieben**, und `m(1, 3)` haette `b` als Zwei bekommen.

**Und die Vorgaben liegen nach Namen, nicht nach Position.** Eine Liste
waere eine Stelle mehr, an der sie auseinanderlaufen koennten.

**Und der Aufruf ohne Argument war ein uralter Fehler in `PartsOf`.**
`PartsOf` fiel auf `Children` zurueck, **wenn keine Rolle vergaben war** --
und ein Aufruf traegt **immer** eine `Receiver`-Rolle, **hat aber ohne
Argumente keine `Argument`-Rolle.** `A.m()` hatte also eine Rolle und ein
Kind, **und der Rueckfall lieferte den Empfaenger als Argument.**

> **Das hiess:** `def m(x = 9)` bekam `x = A`, **der Rumpf las `A`, und der
> Aufruf antwortete mit dem Empfaenger statt mit dem Rumpf.** Kein Vorgabewert
> und kein `Name()`-Fehler -- **der Empfangername war das Argument.**

**Und ein Alt-Test hatte den Fehler mitgezaehlt, ohne es zu merken.**
`Test_AMethodThisHostDoesNotHaveIsARefusal` erwartete drei Ablehnungen aus
drei Aufrufen **und bekam jetzt zwei** -- **weil `Kernel.exit` vorher ein
Argument hatte, das der Empfangername war.** Der Test zaehlt jetzt, was
wirklich passiert, **und der Kommentar sagt, warum die Zahl kleiner wurde.**

**Der Fehler wurde mit einem Trace gefunden, nicht durch Lesen:**
`URPG_TRACE_CALL=1` schrieb `argumente=1 [Symbol] kinder=1 rollen=1` --
**ein Argument, ein Kind, eine Rolle.** **Ein Aufruf ohne Argumente hat null
Argumente, und genau das sagt diese Zeile aus.**

### `*rest` und `**opts`, und die Stelle im Namen

**Der Parser konnte beide lesen, und der Interpreter hatte sie nie gesehen.**
`def m(a, *rest)` ergibt einen `BlockPass`-Knoten `rest` in der Liste,
`def m(**opts)` einen `Hash`-Knoten `opts` -- **und der Interpreter behandelte
beide wie einen normalen Namen**, also war `rest` ein Parameter, den nichts
fuellen konnte, **und ein Aufruf mit zusaetzlichen Werten hat sie verworfen.**

**Und der Splat stand in `namen`, also wurde er wie ein Parameter gebunden.**
`def m(a, *rest)` gab `rest` die **zweite** Zahl statt der Liste `[2, 3]`.
**Das ist die Form, die man zuerst baut, und sie sieht richtig aus**, bis man
sie zaehlt.

> **Ein Sammel, der in der Parameterliste steht, ist kein Sammel, sondern
> der naechste Parameter.** Also steht er drin, **nur damit seine Stelle
> bekannt ist** -- `SammelAb` merkt sie sich, **und der Wert wird nach der
> Schleife gebunden.**

**Und ein Splat in der Mitte nimmt nicht alles.**
`def m(*teile, letzte)` gibt `teile` alles **ausser dem letzten Wert**, weil
`letzte` ihn braucht -- **und die Parameter nach dem Splat zaehlen von
hinten.** Ein Leser, der von vorn bindet, gibt `letzte` den ersten Wert,
**und weil der erste auch in der Liste steht, faellt es nicht auf.**

**Und `*rest` ohne Werte ist eine leere Liste, nicht nil.** Ein Spiel schreibt
`teile.length`, **und nil haette darauf keine Antwort.**

**Und `**opts` ist ein eigener Parameter und keine Liste.** `*rest` nimmt die
ueberzaehligen **Werte**, `**opts` die **Paare**,
**und ein Leser, der beides unter einem Namen haelt, gibt einem Spiel eine
Liste, wo es einen Hash erwartet.**

### `k: 3`, und die Luecke war groesser als die Bindung

**Der konnte `f(k: 3)` gar nicht parsen.** `ParseExpression` las `k` als
Bezeichner, **dann stand ein `:` da, und der Aufruf wurde als Syntaxfehler
gemeldet** -- **die Schreibweise, die jeder Ruby-Schreibende benutzt und die
jedes Spiel in jedem Aufruf schreibt.**

**Und `=>` war eine Zahl.** Der Operator ging an `Apply`, **und `Apply`
liefert fuer einen unbekannten Operator 0** -- **ein Spiel, das `opts[:k]`
liest, haette auf einer Null gelesen**, und nichts haette es gesagt.

**Und der Wert kannte nur Liste und Ganzes.** `IsHash` gibt es jetzt,
**und es ist ein eigenes Feld und keine Raten aus dem Inhalt** -- ein Hash
mit ungerader Paarzahl ist so gewoehnlich wie eine Liste mit ungerader
Laengenzahl, **und ein Leser, der die Zahl gezaehlt haette, waere bei beiden
falsch.**

**Und der Schluessel wird zum Symbol.** `k: 3` heisst `:k => 3`,
**und ein Leser, der den blossen Namen naehme, gaebe einem Spiel einen Hash
mit einem Schluessel, den es nie schreibt** -- es suchte `opts[:k]` und
faende nichts.

**Und der Doppelpunkt muss DIREKT nach dem Namen stehen.** `a ? b : c` hat
auch einen Doppelpunkt, **und ein Leser, der irgendwo nach einem suchte,
haette das `b` eines Ternaers als Schluessel gelesen.**

> **Eine Mutation bleibt und ist einzeln gemessen:** "jeder Name ist ein
> Schluessel" laesst den Ternaer weiterhin korrekt parsen, **weil nach `a`
> ein `?` steht und nicht ein `:`** -- die Bedingung wird also nie zum
> true, egal was sie zurueckgibt. **Kein Test kann sie toeten, weil es
> keinen gibt, bei dem die beiden auseinanderlaufen** -- und das ist eine
> Eigenschaft der Form und kein Testfehler.

### Die Wertausdruecke, und warum sie nicht beim Host gehoeren

**`5.to_s` ging an den Host, und kein Host in diesem Repo kann es.**
Ein echter Host beantwortet `rand` und lehnt den Rest ab,
**also haette ein Spiel, das `"Level #{level}"` schreibt, an etwas gelegen,
das es nicht gibt** -- und der NullHost weiss es nie,
**also haette kein Test ueberhaupt pruefen koennen, ob eine Zahl einen Text
hat.**

**Die Reihenfolge ist: Skript, dann Wert, dann Host.** Eine Klasse, die `to_s`
selbst definiert, **hat ihre eigene**, **und ein Leser, der die Wertausdruecke
zuerst befragte, wuerde jeder Klasse ihre eigene `to_s` wegnehmen** -- ein
Spiel, das seinen Namen ueberall zeigt ausser da, wo es ihn geschrieben hat.

**Und es ist Rubys Schreibweise und nicht C#s.** `nil` und nicht `Null`,
`true` und nicht `True`, **und ein Spiel, das das in eine Speicherdatei
schreibt, haette ein Wort drin, das kein anderes Spiel liest.**

**Und `inspect` ist nicht `to_s`.** Ein String in Anfuehrungszeichen, eine Zahl
ohne, **und ein Protokoll, das einen Namen von einer Zahl unterscheiden
will, braucht genau das.**

**Und `freeze` gibt den Empfaenger zurueck, weil hier nichts eingefroren
werden kann.** Eine Kopie waere ein anderes Ding als das, was das Spiel
gemacht hat, **und ein Spiel haette zwei Sprites, wo es eines gemacht hat.**

**Und was weder Skript noch Wert noch Host ist, bleibt eine Ablehnung.**
`5.gibtsnicht` sagt es, **denn ein Leser, der alles beantwortet haette,
wuerde einen Tippfehler wie eine Methode aussehen lassen.**

### `new`, und die Luecke davor war die groesste im ganzen Ruby-Teil

**Es gab kein `new`.** `Game_Party.new`, `Game_Actor.new(1)` und
`Sprite.new` sind die ersten Zeilen fast jedes RPG-Maker-Skripts,
**und ohne sie hat ein Spiel keine Schauspieler, keine Party und keine Karte** --
es laeuft gar nichts.

**Und die Felder waren ein Speicher fuer das ganze Programm.** `@hp` lag in
einer Tabelle, die jedes Objekt geteilt hat,
**also haette jeder Schauspieler den Wert des letzten gehabt** -- ein Spiel,
in dem jede Figur mit derselben Zahl geht, **und in dem nichts im Skript
sagt, warum.**

**Und `EigeneMethode` fragte die falsche Klasse.** Sie nahm den Namen aus
dem, was gerade laeuft, **und nicht aus dem Empfaenger** -- **auf der
obersten Ebene ist das null**, **also lief `Held.new.staerke` mit einem
leeren Klassennamen**, und `super` suchte in einer Klasse ohne Namen nach
einer Oberklasse **und fand keine.** Das ist derselbe Fehler an zwei
Stellen, **und er wurde erst sichtbar, seit es Objekte gibt:** vorher gab
es keinen Empfaenger, der eine Klasse traegt und zugleich nicht `self` ist.

**Und der Empfaenger fiel auf die laufende Klasse zurueck, auch wenn er ein
String war.** `"b" <=> "c"` ist `String#<=>`,
**und ein Leser, der die laufende Klasse fragt, laesst in `Kachel#<=>` den
Vergleich wieder `Kachel#<=>` rufen** -- **mit einem String** -- **bis der
Stapel ueberlief.** Gemessen: `Stack overflow`.

**Und `def <=>(andere)` parste nicht.** `ReadMemberName` erwartete einen
Namen, **und der Fehler nannte den Operator, nicht die Stelle** -- **also
wurde jede sortierbare Klasse abgelehnt**, und der Tippfehler sah aus wie
ein Schreibfehler.

**Und `<=>` ging an eine statische Methode.** `Apply` hat keinen Interpreter
und **kann keine Skriptmethode rufen** -- **also hatte es fuer ein Objekt nur
die Antwort nil**, und nil heisst "unvergleichbar". **Ein Spiel, das seine
eigene Vergleichsregel schreibt, hat sie nie bekommen.**

**Und der Sort verglich die Werte selbst.** Jetzt geht er ueber dieselbe
Regel, die `<` nimmt, **und die vier Vergleiche fragen auch dieselbe** --
**ein Leser, der sie einzeln baute, wuerde einem Spiel erlauben, zwei
Regeln zu schreiben und zwei Reihenfolgen zu bekommen.**

### Und `map`, `each`, `select` -- es gab nur den Host

**`aktoren.map { |a| a.name }` ist der Weg, aus einem Spiel ein Menue zu
machen**, **und ein Host, der keine Liste von Spielobjekten zum Ablaufen
hat, hat nichts zum Ablaufen** -- der NullHost schon gar nicht,
**also haette kein Test zeigen koennen, was ein Menue anzeigt.**

**Und der Block ist das letzte Argument, weil die Sprache ihn dorthin
legt.** `|x|` bindet den ersten Wert, **und das stimmt zufaellig** -- **und
falsch, sobald das Spiel zwei Parameter schreibt.**

**Und `each` antwortet die Liste**, nicht nil: `liste.each { |x| x.hp += 1 }`
liest die Antwort etwa bei jedem zweiten Mal, **und nil wuerde die Kette
stillschweigend beenden.**

> **Eine Regel wurde einzeln gemessen und dann ENTFERNT, nicht behalten.**
> "Der Klassenrumpf hat keinen eigenen Feldspeicher" hat die Tests nicht
> getoetet, **weil `@x` in einem Klassenrumpf in dieser Runtime nicht wieder
> lesbar ist** -- es gibt kein `class_eval { @x }`. **Der Code war ein
> No-op und sah aus, als gaebe er eine Antwort**, also ist er weg.

**Und ein Test wurde gegen die Quelle geprueft statt gegen meine
Annahme.** `class A; @x = 1; attr_reader :x; end` -- **was liest der Leser
an `A.new`?** Ich habe `rb_attr` in `eval.c` aus Ruby 1.8.1 gelesen: es baut
den Leser als `NEW_IVAR(attriv)`, **und das liest `@name` vom Empfaenger.**
**Ein Leser, der das Feld der Klasse laest, haette 0 geantwortet** -- ein
Spiel, in dem jeder Schauspieler mit dem Wert des Klassenrumpfs beginnt.

### `@@x`, und dabei zwei Fehler in `defined?`, die kein Test beruehrt hatte

**Der Lexer las `@@x` und der Interpreter warf es in die Instanz.** Ruby
unterscheidet die beiden, **und `@@zaehler` ist wie eine Klasse ihre Objekte
zaehlt und ihnen Nummern gibt** -- **ein Feld pro Objekt wuerde gar nichts
zaehlen**, jedes neue Objekt faenge bei null an, **und ein Spiel, das Nummern
aus einer Klassenvariable vergibt, wuerde dieselbe Nummer zweimal vergeben.**

**Gemessen, bevor esrepariert war:**
`class D; @@anzahl = 0; def setze; @@anzahl = @@anzahl + 1; end; end` --
`NoMethodError: undefined operator '+' for a Nil and a Integer`.
**Der Wert im Klassenrumpf war nicht lesbar**, **und das ist genau die Form,
in der jedes Skript seine Zaehler schreibt.**

**Und `defined?` sah den falschen Knoten.** `EvaluateDefined` las die Art aus
dem Condition-Kind, **aber die Tabelle und den Namen aus dem `defined?`-Knoten
selbst** -- **also war `@@x` fuer `defined?` ein Instanzfeld, und `defined?(@@x)`
gab nil, waehrend `@@x` selbst die Zahl las.**

> **Das war ein stiller Bug in einer Funktion, die 64 Tests hatte.** Kein
> Test hatte `defined?` auf eine *Instanzvariable* und eine *Klassenvariable*
> im selben Lauf gestellt, **und beide gaben nil** -- `defined?(@hp)` gab
> nil, **obwohl `@hp` gesetzt war**, weil `NameOf` ein Condition-Kind in
> einer Variablen suchte, **das nicht existiert.**

**Und die Schreibweise ist gemischt, und ich habe es nicht vereinheitlicht.**
Verifiziert in `eval.c` aus Ruby 1.8.1: `defined?` antwortet
`"class variable"` **mit Leerzeichen** und daneben `"local-variable"`
**mit Bindestrich**. **Zwei Schreibweisen in derselben Funktion der
Referenz** -- **und diese Runtime schreibt, was die Quelle schreibt, weil ein
Skript, das den Antwortnamen vergleicht, sonst das falsche Wort bekommt.**

**Und eine tote Zeile wurde entfernt, nicht dokumentiert.** `_self` im
Klassenrumpf war ueberflüssig, **weil `_aktuellerTyp` die Klasse ohnehin
traegt** -- **und die Mutation "der Rumpf sieht die Klasse nicht" hat keinen
Test getoetet**, was genau das bewiesen hat.

### Die Basis der drei Arten -- sechsunddreißig Namen fehlten

**Gemessen, nicht geschaetzt:** von den einundvierzig Namen, die ein Skript
in seinen ersten hundert Zeilen schreibt, waren **sechsunddreißig nicht da.**
`length` allein stoppt jedes Menue, das zaehlt, `[0]` stoppt jede Liste, die
ihren ersten Eintrag liest, **und ein Spiel ohne beides ist nicht leicht
kaputt -- es startet nicht.**

**Und ein Host kann sie nicht beantworten.** Ein echter Host kennt die
Objekte des Spiels -- `Sprite`, `Window_Base`, `Input` -- **und nicht Rubys
`Array` und `String`**, **weil ein Spiel den Host nie fragt, was ein Array
ist.**

**Und `is_a?` war zweimal da.** Es stand in `Antwortet` neben
`respond_to?`, **und dort stand im Kommentar "this runtime has no
objects"** -- **das war vor `new` wahr und ist seit `new` falsch**, und die
alte Fassung verglich nur den eigenen Namen. **Gemessen:
`held.is_a?(Basis)` gab `false`, `Held.is_a?(Basis)` gab `true`** -- **und ein
Leser, der nur den Namen vergleicht, sagt `nein` zu jeder Wache, die ein
Spiel gegen seine eigene Basisklasse schreibt**, **und jedes eigene Objekt
waere fremd.** Der neue Weg geht die Kette.

**Und ein Hash wurde abgelehnt.** Die Antwort war *„this interpreter does
not evaluate a Hash node"* -- **eine Meldung ueber den eigenen Quelltext des
Lesers**, **und jede gespeicherte Einstellung, jede Ereignistabelle und
jede Statuszeile ist ein Hash.** Das ist der groesste Teil dessen, woraus
ein Spiel besteht.

**Und eine irrefuehrende Meldung hat alles darueber verdeckt.** Gemessen:
`[1, 2, 3].length` meldete *„length braucht einen Block"* -- **und das ist
doppelt falsch**, denn `length` laeuft keinen Block, **und der Host haette
die richtige Antwort gehabt.** Ursache: die Blockfrage stand **vor** der
Namensfrage, **also bekam jeder unbekannte Name auf einer Liste die
Blockmeldung.**

> **Eine Regel stand zweimal, und die Mutation hat es bewiesen.**
> „Negative Stellen zaehlen nicht von hinten" wurde in `Item` geaendert und
> in `Index` nicht, **und kein Test hat es gemerkt** -- **weil kein Test
> `first` oder `last` benutzt hat**, und die beiden Orte sind erst durch
> `first` und `last` unterscheidbar. **Jetzt ist `Item` der einzige Ort,
> und `Test_FirstAndLastReadTheEnds` schliesst die Luecke.**

**Und `"abc".length` zaehlt Bytes, nicht Zeichen.** Ruby 1.8 hat keinen
Zeichentyp, **und ein Leser, der Zeichen zaehlte, gaeve einem Spiel auf
einer anderen Maschine eine andere Zahl** -- **das ist ein Fenster, das die
falsche Anzahl von Zeichen zeichnet.** `"abc"[1]` ist deshalb **ein Byte,**
und in CP932 die halbe Kanji: **das ist Rubys Verhalten und kein Fehler
hier.**

### Die Textoperationen, und dabei zwei Fehler, die ein Spiel nie zeigt

**Alles war abgelehnt.** Gemessen, bevor etwas davon existierte:
`"a,b,c".split(",")`, `gsub`, `start_with?`, `strip`, `chomp`, `ljust` --
**alle mit *„has no method X on this host"*.** Ein Spiel hat damit **kein
Textfenster, das etwas anzeigt.**

**Und `2.times { }` rief den Block NULLMAL.** Die Schleife begann bei der
Zahl selbst, **also war `2 <= 1` nie wahr** -- **und genau so baut jedes
Statusfenster seine Zeilen.** `times` beginnt jetzt bei null, `upto` und
`downto` bei der Zahl.

**Und `push` gab eine neue Liste und liess die alte unveraendert.** Ruby
aendert in-place, **und `OfArray` baute aus dem Material ein Array, das
nicht waechst** -- **also sammelte `3.times { |i| g = g.push(i) }` nichts.**
`OfArray` und `OfHash` legen jetzt eine `List` an, **und `push` waechst
an Ort und Stelle.**

**Und `chomp` liess das Wagenruecklauf stehen.** Gemessen: `"a\r\n".chomp`
gab zwei Bytes statt einem -- **und ein Spiel, das eine Zeile aus einer Datei
liest, haette am Ende jedes Wortes ein `\r`**, das in keinem Namen steht.

**Und `tr` ist nach `tr_trans` und `trnext` in `string.c` aus Ruby 1.8.1
geschrieben, nicht geraten.** Eine Tabelle ueber alle 256 Bytes,
`trnext` liest beide Seiten Zeichen fuer Zeichen, **und das ist der Grund,
warum `tr("a-z", "x")` jeden Kleinbuchstaben zu einem x macht statt ihn
dreimal zu uebersetzen.** `"abc".tr("abc", "xy")` ist `"xyy"` -- **weil der
Ersetzer nach `y` aufgebraucht ist und der letzte stehen bleibt**, **das ist
das `if (r == -1) r = trrepl.now;` in der Quelle.** `"abc".tr("^b", "x")` ist
`"xbx"`, **und `tr("a-z", "")` ist `"H"`** -- **vier Faelle, die vorher
falsch waren oder gar nicht existierten.**

**Und ein Muster aus einem Skript wird nicht ausgefuehrt, und das wird
gesagt.** `gsub(/[0-9]/, "X")` gibt nil **und eine Diagnose, die das Muster
nennt** -- **ein stilles nil wuerde aussehen, als haette der Text keine
Ziffern gehabt**, **und das Spiel haette den Namen mit dem Marker gezeichnet
und nicht gewusst, warum.**

> **Eine Regel wurde einzeln gemessen und ENTFERNT.** „Der Block laeuft
> nicht auf seinem Objekt" hat keinen Test getoetet, **weil
> `_instanceVariables` von `Aufrufen` ohnehin schon auf dem Objekt steht
> und `pSelbst` dasselbe ist** -- **also war es ein No-op mit einer Doku,
> die eine Antwort behauptet**, und ist weg. Das ist die zweite Regel in
> diesem Lauf, die so endet.

### Die Mustermaschine, und die Schranke ist der eigentliche Grund

**`name =~ /Held/` stand in fast jedem XP-Skript und es gab keine
Maschine.** `=~`, `!~`, `match`, `match?` und `scan` sind jetzt da.

**Und die Buchstaben hinter dem zweiten Schraegstrich wurden gelesen und
weggeworfen.** Der Lexer sammelte sie, **und legte sie nirgends hin** --
**also war `/held/i` dasselbe wie `/held/`**, **und ein Skript, das einen
Namen ohne Ruecksicht auf die Schreibweise sucht, hat ihn nicht gefunden,
und nichts hat es gesagt.** **Die Reihenfolge ist `m`, `i`, `x` und nicht
meine:** verifiziert in `re.c` aus Ruby 1.8.1, wo `rb_reg_to_s` sie in
genau dieser Reihenfolge anhaengt.

**Und die Schranke liegt an der Textlaenge, nicht an einer Uhr.** Eine Uhr
ist nicht pruefbar, **und ein Muster, das zurueckkam, nachdem es ewig lief,
ist ein Spiel, das schon haengt** -- **also wird die Zahl der Bytes
gezaehlt, und die ist fuer ein gegebenes Muster und einen gegebenen Text
immer dieselbe.** **Und die Meldung nennt das Muster und die Grenze**, weil
eine Warnung vor nichts den Suchenden nicht weiterbringt.

**Und `scan` gibt Gruppen und nicht ganze Treffer.** `text.scan(/(\d+)/)`
gibt die Ziffernfolgen, **und mit einem Block gibt es die Gruppen statt der
ganzen Treffer** -- **das ist der Satz, mit dem ein Spiel aus einem
Ereignisnamen die Nummer zieht**, **und der Block aendert die Antwort
nicht: `scan` gibt seine eigene Liste zurueck.**

**Und ein `[`, dem kein `]` folgt, brach die ganze Datei.** Der Lexer
setzte sein Klassenzeichen, **wartete auf ein `]`, das nie kam, und lief
bis zum Skriptende** -- **gemessen: `A regular expression opened at offset 9
is never closed.`** Das Muster `/a[/` **endet dort**, **und jetzt endet es
dort**; **dass die Maschine danach `Unterminated [] set` sagt, ist richtig**,
**denn `a[` ist auch in Rubys Maschine eine offene Menge.**

**Und ein Muster, das nicht gebaut werden kann, sagt es.** `/(a/` gibt nil
**und die Meldung der Maschine im Klartext** -- **ein stilles nil wuerde
aussehen, als haette der Text keinen Treffer**, **und ein Spiel wuerde
anderswo suchen.**

**Noch nicht ausgewertet:** `respond_to_missing?`, `binding`,
`Module`-Methoden (`include?`, `instance_methods`),
Block-Umbrueche mit Argumenten (`break 1`), `dup`/`clone` mit echter Kopie,
`find`/`detect`/`inject`/`group_by`/`each_with_object`,
`Struct`, `raise`, `require`, `printf`/`sprintf`,
`gsub` mit einer Blockersetzung, und
`\A`/`\z` als Anker in CP932.

### `$1` -- und ein Global, das gesetzt werden, aber nicht gelesen werden konnte

**Ein Global liess sich nicht.** `$x = 1` hat funktioniert, **und `$x` nicht** --
**weil die Zuweisung an die Tabelle ging und das Lesen nirgends hin**.
Gemessen: *„this interpreter does not evaluate a GlobalVariable node"*.
**Und `$game_party` ist die erste Zeile von jedem RPG-Maker-Skript.**

**Und ein Treffer war eine Zahl und sonst nichts.** `s =~ /(\d+)/` sagt wo,
**und ein Skript, das danach `$1` liest, hatte nichts zu lesen** --
**das ist die Form, mit der jedes Plugin die Zahl aus einem Ereignisnamen
zieht**, **und es ist die Form, die diese Maschine wert macht.**

**Und das sind sechs Namen fuer eine Frage.** `$~`, `$1`, `$&`, `` $` ``,
`$'` und `Regexp.last_match` fragen dasselbe,
**und sechs Tabellen wuerden sechs Chancen sein, dass sie sich widersprechen**
-- **ein Spiel, das `$~` und dann `$1` liest, bekommt sonst zwei
Antworten.**

**Und `Regexp` gab *„the constant Regexp is not defined by this host"*.** Das
ist eine Meldung ueber den Host **fuer eine Klasse, die der Leser nicht
hatte**, **und `Regexp.last_match[1]` steht in fast jedem Plugin.**

**Und eine Gruppe, die nicht teilgenommen hat, ist nil und nicht leer.**
`"abc" =~ /(a)(z)?/` hat eine zweite Gruppe, die nicht gepasst hat --
**gemessen: `$2` gab einen leeren Text und `$3` nil.**
**Und die Liste der Gruppen kann das nicht unterscheiden**,
**weil in ihr "" steht** -- **deshalb steht jetzt zusaetzlich da, welche
Gruppen wirklich da waren.**

**Und ein Lauf ohne Treffer loescht den alten.** Ruby setzt nil,
**und ein Leser, der den alten stehen laesse, haette ein Skript, das nichts
fand und die Zahl der vorigen Zeile las** -- **das ist eine Speicherdatei
mit einem Feld mehr, als das Spiel erwartet.**

**Und sechzehn Faltungen gaben es, und zehn davon nicht.** `map`, `select`,
`filter`, `reject`, `find_all`, `each`, `each_with_index`, `reverse_each`,
`any?` und `all?` antworteten, **`find`, `detect`, `inject`, `reduce`,
`each_with_object`, `group_by`, `partition`, `sort_by`, `min_by`, `max_by`,
`flat_map`, `none?`, `one?`, `take`, `drop`, `flatten`, `compact`, `sum`,
`min` und `max` nicht** -- **alle mit *„has no method on this host"*.**

**Und die zehn sind die, aus denen ein Menue entsteht.** `find` holt einen
Schauspieler, `inject` rechnet die Level einer Party zusammen,
`each_with_object` fuellt ein Fenster, `group_by` sortiert ein Lager in
Reiter, `min_by` findet den schwaechsten.

- **Und `find` gibt den WERT und nicht die Stelle** -- **gemessen.**
  `find { |a| a.name == "Held" }` gibt den Schauspieler, **und ein Leser, der
  die Stelle gaebe, haette eine Party, die eine Zahl haelt, wo sie einen
  Namen haelt.**
- **Und `min` ist nil fuer eine leere Liste, und nicht 0** -- **ein Leser, der
  0 gaebe, zeichnete eine Leiste fuer ein Level, das niemand hat.**
- **Und `sort_by` ordnet nach dem, was der Block gemessen hat, und gibt den
  Wert zurueck** -- **eine Party nach Level zu ordnen ist etwas anderes, als
  sie nach Namen zu ordnen.**
- **Und `group_by` baut einen Hash in der Reihenfolge, in der die Schluessel
  zuerst kamen** -- **so zeichnet ein Lagerbildschirm seine Reiter.**
- **Und `each_with_object` gibt dem Block den Wert zuerst und das Ding
  danach** -- **die umgekehrte Reihenfolge schriebe die Liste in den Wert
  und das Fenster haelt eine Liste von Listen.**
- **Und `inject` ohne Anfang nimmt das erste Element** -- **ein Leser, der bei
  nil anfing, gaebe nil fuer jede Liste, und ein Spiel, das die Level einer
  Party ohne Anfang summiert, zeigte nichts.**

**Und `Struct` gab es gar nicht, und eine Konstante konnte keinen Wert
halten.** `RPG::Actor = Struct.new(:id, :name, :class_id)` ist die
**erste Zeile** der Standardbibliothek von RPG Maker XP, VX und VX Ace,
**und ohne sie ist kein einziges dieser Spiele lesbar** -- **das ist
keine Bequemlichkeit, das ist der Datenkatalog.**

Und die Zeile scheiterte an **zwei** Dingen:

- **Und `Punkt = Struct.new(:x, :y)` sagte *„is on the left of an = and
  there is nowhere to put the value"*.** Eine Meldung ueber den Leser fuer
  etwas, das der Leser sehr wohl tun kann, **und das Skript, das es
  braucht, ist jedes Spiel aus dieser Zeit.** Ruby erlaubt es, **und
  `LIMIT = 100` ebenso** -- **die Zahl 100 ist die erste Zeile der halben
  Skripte in VX Aces Konfiguration.**
- **Und `RPG::Actor` las der Parser als *„rufe `Actor` auf `RPG` auf"*.**
  `::` ohne Klammern machte immer einen Aufruf,
  **und `RPG` ist ein Modul, und Module haben keine Methode `Actor`**
  -- **also nil, und dann `nil.new`, und dann `nil.id`, und drei
  Fehlermeldungen ueber einen Host, der nichts davon getan hat.**

Und **die Felder eines Structs sind Attribute, und keine zweite Art von
Mitglied.** `a.x` und `a.x = 1` sind Leser und Schreiber von `@x`,
**und Ruby baut die Zugriffe eines Structs genau so** -- **ein Struct
braucht also eine Klasse, eine Instanzvariable und keinen neuen Code.**

- **Und `to_a` ist die Felder in der Reihenfolge, in der sie geschrieben
  wurden** -- **und diese Reihenfolge ist der ganze Grund, warum es ein
  Struct gibt: sie ist, was eine Speicherdatei ist.** Ein Leser, der sie
  sortierte, wuerde **eine Speicherdatei schreiben, die kein anderes Spiel
  lesen kann.**
- **Und `==` sind die Felder, eines nach dem anderen, und nicht die
  Identitaet.** `liste.include?(waffe)` ist der Satz, den jedes Spiel
  schreibt, **und ein Leser, der die Objekte selbst verglich, wuerde es nie
  finden, und jede Lageranzeige waere leer.**
- **Und zwei verschiedene Struct-Arten sind nie gleich.** Eine Waffe und
  eine Ruestung koennen beide aus drei Zahlen bestehen,
  **und ein Leser, der nur die Felder verglich, haelte ein Schwert fuer
  eine Ruestung.**

**Und `raise`, `rescue` und `ensure` gab es nicht, und `rescue` scheiterte
schon im Parser.** Gemessen: *„'rescue' at offset 10 does not begin an
expression"* -- **und der Satz, mit dem ein Spiel seinen eigenen Fehler
behandelt, war ein Syntaxfehler.**

Und `rescue` mit den Armen ohne `begin` (`def m; a; rescue; b; end`) ist
**die Form, mit der `Kernel#load` eine Datei laedt** -- **und in jedem
RPG-Maker-Skript, das eine Datei laedt.**

- **Und `ensure` laeuft bei jedem Ausgang, auch wenn ein Fehler war** --
  **ein Leser, der es nur im Erfolgsfall riefe, haette eine Datei offen
  gelassen, wenn das Spiel scheitert**, und die naechste Speicherung ginge
  in eine schon offene Datei.
- **Und der Wert von `ensure` ist nicht die Antwort.** `begin; a; ensure;
  b; end` gibt `a` zurueck, **und ein Leser, der `ensure` zur Antwort
  machte, haette eine Methode den Wert der Zeile zurueckgeben lassen, die
  eine Datei schliesst.**
- **Und `rescue Klassename` fasst die Klasse UND ihre Eltern, und nicht
  "alles auf gleicher Tiefe".** `TypeError` und `ZeroDivisionError` sind
  Geschwister unter `StandardError` --
  **ein Leser, der nur die Tiefe verglich, wuerde sagen "`TypeError" faengt
  `ZeroDivisionError`"**, **und dann wuerde `rescue TypeError` einen
  Rechenfehler fangen**, **und ein Spiel, das die beiden unterscheidet,
  haette keinen Unterschied mehr.**
- **Und ein Arm, dessen Klasse nicht passt, laesst den Fehler durch.**
  **Gemessen: Ruby bricht auch ab, wenn kein Arm greift.**

Und **`:@held` war kein Symbol.** `instance_variable_get(:@hp)` und
`class_variable_get(:@@zaehler)` schreibt jedes Skript, das ueber eine
Instanz nachdenkt -- **und `IsSymbolStart` kannte weder `@` noch `$`**,
**also zerlegte der Lexer `:@hp` in `:` und `@hp`**,
**und der Parser sagte *„':' does not begin an expression"***,
**und die Fehlermeldung sprach von einem Doppelpunktzeichen, das der
Leser selbst nicht erkannt hatte.**

**Und `e.class` gab `Object`, und das stand in `SammlungMethode`, und nicht
in der Fehlerschicht** -- **`class` wird fuer jeden Empfaenger beantwortet,
und diese Antwort kam vor dem eigenen Zweig.** Ein Trace zeigte,
**dass `e.class` `Call()` nie erreichte** --
und **nach zwanzig Messungen fuer diese eine Frage habe ich aufgehoert und
den Ort gesucht, an dem `class` wirklich beantwortet wird:**
**`SammlungMethode`, Zeile 1107, `case "class"`.**

**Und zwei Mutationen lebten zuerst, und beide waren echte Lücken:**

- **„der Arm mit gebundenem Fehler wird nicht erkannt"** -- **und die
  Messung sagte: `rescue => e` ohne Fehler gab 1, `rescue => e` mit Fehler
  gab 5.** Der Test hatte `def m; 1/0; rescue => e; e.message; end` geschrieben
  und nicht `rescue => e` allein -- **und ein Test, der die Form nur im
  Methodenrumpf zeigt, sieht nicht, was die Form allein tut.**
- **„`Exception` faengt nicht alles"** -- **und die Messung sagte:
  `rescue RuntimeError` vor `rescue Exception` gab 1, umgekehrt gab 2.**
  **Ein Test mit nur einem Arm kann die Regel nicht sehen**, **weil
  `rescue Exception` dann die einzige Moeglichkeit ist und der Ausfall
  unsichtbar bleibt.**



> **Eine Mutation lebte, weil die anderen Tests die Frage nicht stellten.**
> *„`$~` gibt nichts"* hat alle fuenf ueberlebt, **weil `$~.pre_match`
> und `$~[1]` von den Methoden auf dem Wert beantwortet werden und nicht vom
> Wert selbst** -- **gemessen: `$~[0]`, `$~[1]`, `$~.size` und `$~.begin`
> antworten alle, auch ohne den Zweig.** **Das ist die genaue Form eines
> Tests, der den Unterschied nicht sehen kann**, und
> `Test_TheMatchItselfIsAValueWithItsOwnNumbers` schliesst die Luecke.

**Noch nicht ausgewertet:** `respond_to_missing?`, `binding`,
`Module`-Methoden (`include?`, `instance_methods`),
Block-Umbrueche mit Argumenten (`break 1`), `dup`/`clone` mit echter Kopie,
`find`/`detect`/`inject`/`group_by`/`each_with_object`,
`Struct`, `raise`, `require`, `printf`/`sprintf`,
`String#%`, `gsub` mit einer Blockersetzung und `sub` mit dem ersten
Treffer, `String * Integer`, und
`\A`/`\z` als Anker in CP932.

**And a dead branch that a mutation could not have caught.** `OpAssign`
handled `op == "="`, **and the parser never produces this node with a bare
`=`** — an equality sign yields an `Assignment`. The branch was dead, a test
for it was a test for something that does not happen, and the mutation
`op.Length > 1 ? op[..^1] : "="` -> `op[..^1]` survived because **no script
reaches it**. It was removed, and the rule became the observable one:
`**=` must take `**` and not `*`.

**And a guard that cannot be killed, because the model cannot show it.**
`EvaluateFor` checks `Kind != Object || !IsList` before it walks. **The rule
that removes that check survived three runs and could not be killed by any
test, and the reason is the model rather than the tests: `RubyValue.Items`
defaults to an empty list for everything that is not an array**, so a nil
list, an integer and an object without a list marker all walk zero times
whether the guard is there or not. The guard describes a future where a value
can hold items and not be a list; **this one cannot.**

It was replaced with the observable one — the loop binds the element rather
than a fixed number — and the guard stays, because it is right and costs
nothing.

**Test evidence** `test_ruby_interpreter.cs` 34 tests across two files;
`test_ruby_parser.cs` stays 52/52. **1575/1575**, validator passed, mutations 6 of 6, 6 of 6 and
7 of 7 across the three groups — **and the seventh group is 7 of 7 measured
rule by rule**, because the script reports six for the class table on a run
that loads a stale DLL. **A harness result and a measurement of the source are
two different things, and this one is the seventh time the difference
appeared.**

### K-090 MV/MZ: script files as data, no JavaScript executed
`IN PROGRESS` — board, P4

**The boundary decision was put to the user on 2026-09-28 and the answer was:
script files are read as data, command names are extracted, and no JavaScript
is executed.** That is the first of the offered options and the one this
project's own policy already implies, so the card moves out of `BACKLOG` and
the decision is on the record rather than in the agent's head.

**Why the policy implies it, in one sentence:** a game's `rmmz_managers.js` is
**83 KB of the author's own code**, and executing it would be executing the
game — which this project does not do for an imported title. A JavaScript
engine with a game file as the payload is a remote-code-execution path, and no
quality of implementation changes that.

**What `MzScriptCommandReader` does, and what it does not.** It reads the file
as text and picks out two bounded shapes: `PluginManager.registerCommand("P",
"C", fn)` — the way every plugin command is written — and `X.prototype.y =`
assignments, which is how the built-in commands and the event hooks are
written. **No engine, no interpreter, no `eval`, anywhere in that path.**

**And the test that matters is the one that proves nothing runs.** A source
containing `throw new Error(...)` and `while (true)` is read on a thread with
a five second limit: the reader returns, throws nothing, and still finds the
command name beside the code. **"Does not execute" is asserted, not
claimed.**

**A file whose shape the reader does not know says so** — an empty result and
a failure to read are different answers, and without the diagnostic a file the
reader failed to understand would look like a file with no commands.

**What remains, and is not decided:** a game's own event-command bodies are
plain MZ data and are already read by K-120 through K-133, so the gap this
card leaves is the author's *logic* in the scripts — which is exactly the part
that would need an engine.

**Test evidence** `test_mz_script_commands.cs`, 8 tests.
**1541/1541**, `TestMzScriptCommands: 8/8`. Mutations: six rules, five caught
in the run and the sixth caught when measured on its own — a stale DLL, the
signature documented in `SESSION_STATE.md`.

### K-136 The eighty-nine commands liblcf names and this interpreter does not dispatch
`READY` — runtime, P0



## `11120` Move Picture is done — the third picture command, and the only gap the constant list had

**`ShowPicture` (11110) and `ErasePicture` (11130) both ran. `MovePicture`

(11120) sat in the constant list with a summary and no `case`** — also the shape

K-094 was written for, when `ShowPicture` and `ErasePicture` were implemented in

`PresentationState`, bounded and tested, and no command could reach either.



**So a game that slid a title card across the screen fell into the default arm** and was

reported as an unsupported command. The card did not appear, and the diagnostic named a

code the author had every reason to believe worked.



### The three rules the command turns on



**The movement is a state and not a position.** A reader that set the target straight away

would have the picture arrive the instant the command ran, and a game that slides a title

across the screen would show it at its destination with nothing in between. The reference

holds the picture, the target and the frames left, and the render reads where the picture is

*now* — so the state carries a start, a target, a total and a remainder.



**Zero frames is a placement and not a refusal.** An editor field the author never touched

reads as zero, and the reference sets the position and returns. **A reader that refused it

would stop the event** — and a game whose title card is placed by a zero-frame move would

lose the card instead of having it appear.



**Moving a picture that is not on the screen is refused and named.** A game that moves an id

it never showed has a file that does not mean what it says, and a reader that created a

picture there would put an image on the screen that no command asked for. **One movement

per picture, and a second movement replaces the first** — the reference holds a single move

per id, so a game that moves a card and then moves it again starts the second from where

the picture actually is.



**The minimum width is eight, from the reference's own `CmdSetup`.** A first draft read

four — the id, the mode, X and Y — and a game that left the frame field empty by using the

short form would have had its move rejected as a truncated file, which no RM2K/2003 game

writes.



**The pictures move on the same tick as the screen effects**, and for the same reason as the

tint on the flash: a movement on a different clock would end at a different moment than the

flash that was told to end with it.



**And the position is interpolated from the frames already spent over the frames in total**,

so the last frame lands exactly on the target. A reader that rounded would have a picture

that stopped one pixel short and then jumped.



**Four of the seven tests were wrong about the fixture, and two about the code.** The show

command takes the file name from the command's *text* and not from a parameter, and a

magnification of zero is refused — so a fixture that put a name index in `parameters[0]` and

a zero in the magnification showed no picture, and every move then had nothing to move. One

test demanded silence from the trace, which would have meant demanding a command that says

nothing happened. One put two moves in one program and read the result as the first move's

end.



**Test evidence** `test_rm2k_move_picture.cs` (7).

**1360/1360**, Validator gruen.

**Mutations** 10 Regeln ueber einen Lauf, **10 von 10 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, die Mindestbreite 4 statt 8, die Dauer vom falschen Parameter, das

nicht aufgeloeste Ziel, die Bewegung, die sofort ans Ziel springt, null Bilder als

Platzierung, das erzeugte fehlende Bild, das nicht tickende Brett und der Reset, der die

Bewegung stehen laesst.


## `11910` and `11950` are done — the card listed four, and two of them were already there

**The board listed `11910`, `11930`, `11950` and `11960` as one open family. Measured, it

is two.** `ChangeSaveAccess` (11930) and `ChangeMainMenuAccess` (11960) were already

dispatched through the one-line access handler together with the teleport and escape

commands — **and the card had been written before that.** What was missing was the pair that

*opens* a menu, and they are not the same shape as the pair that says whether the player may.



### Die vier Regeln, die die zwei trennen



**Breite 0, und das ist die ganze Form beider Befehle.** Die Dispatch-Zeilen der Referenz geben

Speichern und Hauptmenü eine Breite von null — **ein Leser, der einen Parameter verlangt hätte,

wäre jeden Menübefehl eines Spiels abgelehnt haben, und einer, der `parameters[0]` las, läse

hinter das Ende einer Liste, die es nicht gibt.**



**Eine Anforderung und kein offenes Menü.** Dieser Leser baut keine Menüszene, also sagt das

Feld, was ein Befehl verlangt hat — dieselbe Form wie der Game-Over-Bildschirm, und wer das

Menü daraus zeichnet, ist Sache des Lauftzeugs und nicht der Simulation.



**Zwei Flags und nicht eines.** Ein Leser, der "ein Menü" in einem Feld speicherte, hätte den

Auftrag des Hauptmenüs gelöscht, sobald der Speicherbefehl käme — **und ein Spiel, das das

Hauptmenü öffnet und dann speichert, hätte keines der beiden offen.**



**Eine offene Nachricht zuerst, und das Menü wartet darauf** — dieselbe Regel wie der

Game-Over-Bildschirm und die Titelbildschirm-Anforderung. Die ersten zwei Zeilen der Referenz

sind `if (Game_Message::IsMessageActive()) return false;` — **ein Held, der "nimm diesen Laden"

sagt und vom Menü verdeckt wird, ist ein Spiel, das eine Zeile verdeckt hat, die der Autor

für genau diesen Moment geschrieben hat.**



### Und was die gehaltene Seite kostet



**Die Seite hält, also läuft derselbe Befehl in jedem Frame erneut.** Ein Programm, das das

Hauptmenü öffnet und danach das Speichermenü, bekommt das Hauptmenü — **so lange der Spieler

darin ist, und das zweite nie.** Das ist der Preis einer gehaltenen Seite, **und die Referenz

zahlt ihn genauso**: ein Spiel, das beide Menüs will, öffnet eines, schließt es und erreicht das

nächste.



**Das ist am Test aufgefallen, nicht am Code.** Ich hatte einen Test geschrieben, der behauptete,

die beiden Flags kollidierten nicht, und er blieb rot — bis die Messung zeigte, dass der zweite

Befehl nie läuft. **Ein Test, der das Gegenteil behauptet hätte, hätte ein Verhalten geprüft,

das das Format nicht hat.**



**Und die Mindestbreite von `11120` war falsch: 8 statt 16.** Die Dispatch-Zeile der Referenz sagt

`CmdSetup<&CommandMovePicture, 16>`, **und ich hatte acht geschrieben — aus den fünf, die der

Befehl liest, plus einer Vermutung.** Ein Leser mit acht hätte jeden echten Move-Picture-Befehl

als abgeschnittene Datei abgelehnt. **Und die Test-Fixture paddete ebenfalls auf acht, was beide

Fehler in dieselbe Richtung gehen ließ und die Suite grün hielt.**



**Test evidence** `test_rm2k_open_menu.cs` (6) und `test_rm2k_move_picture.cs` (7), die zweite

Suite nach der Breitenkorrektur neu gemessen.

**1366/1366**, Validator grün.

**Mutations** 9 Regeln über vier Läufe, **9 von 9 gefangen** — darunter beide Befehle, die den

Dispatch nicht erreichen, die Seite, die nicht hält, die ignorierte offene Nachricht, beide

Menüs im selben Feld, die wieder auf acht gesetzte Mindestbreite, der falsche Parameter für die

Dauer, das nicht unterscheidbare Wartegrund und das Menü, das sich nicht merkt, dass es offen war.


## `10840` Get On/Off Vehicle is done — und `Rm2kVehicleBoarding` war eine Insel mit fünfzehn Methoden

**`10650` und `10850` liefen. `10840` lief nicht — und `Rm2kVehicleBoarding` hatte fünfzehn

Boarding-Methoden, getestet, die kein Befehl erreichen konnte.**



**Dieselbe Inselform wie die Bilder in `PresentationState` und wie `Rm2kMoveRouteState` zuvor:**

eine Klasse, die vollständig ist, getestet ist und unerreichbar ist. **Und man findet sie nur,

indem man fragt, wozu die Klasse da ist, und das mit dem vergleicht, was die Befehle der

Referenz tun** — nicht indem man die Konstantenliste liest, in der die Zahl längst steht.



### Die vier Regeln



**Breite 0, und das ist die Form des Befehls.** Das Fahrzeug ist kein Parameter — es ist, was

unter dem Helden liegt oder vor ihm steht, in der Reihenfolge, in der die Referenz prüft. **Ein

Leser, der einen Parameter erwartet hätte, läse eine Liste, die es nicht gibt.**



**Ob es passiert ist und nicht, ob es könnte.** Das `GetOnOffVehicle` der Referenz tut gar

nichts, wenn es nichts gibt, worauf einzusteigen wäre und nichts, wovon abzusteigen wäre — **und

der Unterschied zwischen „hat es getan" und „hätte es tun können" ist das ganze beobachtbare

Verhalten des Befehls.** Ein Hook, der die Fähigkeit zurückgäbe, hätte ein Spiel einen Zweig

„hier kannst du nicht einsteigen" laufen lassen, den die Referenz nie nimmt.



**Ein fehlender Hook ist eine Ablehnung mit Namen und kein stilles Überspringen.** Der Aufrufer,

der keinen Hook gab, hat nicht „kein Fahrzeug hier" gesagt, sondern „dieser Leser kann nicht

einsteigen" — **und das sind zwei verschiedene Dinge.**



**Und der Befehl wartet nicht, und das ist das Verhalten der Referenz.** Der Issue-Thread von

EasyRPG zu genau diesem Befehl hält fest, dass auch `RPG_RT` auf die Einsteige-Animation wartet

**nicht** — dieser Leser folgt der Referenz und nicht den Beobachtungen des Threads.



**Der Hook ist ein Konstruktorargument und kein später gefülltes Feld**, aus demselben Grund

wie der Routenstarter: ein Test muss sehen können, was der Interpreter bekommen hat, **und ein

null-Hook ist selbst ein Fall, der es wert ist, getestet zu werden.**



**Vier Tests, und drei von ihnen sind über die Fixture gestolpert.** `Rm2kVehicleState` hat einen

Konstruktor statt eines Objektinitialisierers, und der Typ kommt aus `Rm2kVehicle.Boat` und nicht

aus einer selbst geschriebenen 1.



**Test evidence** `test_rm2k_get_on_off_vehicle.cs` (4).

**1370/1370**, Validator grün.

**Mutations** 5 Regeln über einen Lauf, **5 von 5 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, der nicht aufgerufene Hook, der fehlende Hook als Erfolg, die gehaltene

Seite und der Hook, der die Fähigkeit statt der Tat bekommt.


## `10490` Full Heal ist fertig — und der Test hat eine Regel gestrichen, die ich erfunden hatte

**Die sechs Actor-Befehle: fünf veränderten etwas, dieser stellt wieder her.** `ChangeExp`,

`ChangeLevel`, `ChangeParameters`, `ChangeHP` und `ChangeSP` waren verdrahtet — und `FullHeal`

war es nicht, **obwohl es als einziges der Familie gar keinen eigenen Wert braucht.**



### Die Regel, die der Test gestrichen hat



**Ich hatte eine SP-Flagge erfunden.** Der zweite Parameter sollte heißen „heile auch die

SP-Punkte" — **und die Referenz hat zwei Parameter, und beide sind die Actor-Auswahl:** der

Modus und die Nummer. Ein Leser, der den zweiten als SP-Flagge gelesen hätte, hätte von jedem

geheilten Actor auch die SP geheilt **und nur einem einzigen Helden die Trefferpunkte** — und ein

Spiel, das zwischen zwei Kämpfen nur die Trefferpunkte heilt, hätte eine Mannschaft, der die

Magie nie ausgeht.



**Die SP-Punkte gehen mit, immer.** Der Befehl heißt Full Heal, und der Rumpf der Referenz setzt

beide Zähler; wer nur die Trefferpunkte will, hat `10460` dafür.



### Und die zweite Regel, die sich daraus ergab



**Zwei Parameter, und beide sind die Auswahl** — 0 ist die ganze Mannschaft, 1 ein Held nach

Nummer, 2 ein Held aus einer Variable. **Modus 0 heilt die ganze Mannschaft und die Nummer im

zweiten Parameter wird ignoriert.**



**Und der Unterschied zu `10460`, der sechs Parameter hat.** Ein Leser, der dessen Breite

kopiert und dessen dritten Parameter als SP-Flagge liest, hätte jede kurze Heilung abgelehnt, die

ein Spiel geschrieben hat.



**Und die dritte Regel, die den Unterschied zu seinen Nachbarn ausmacht: es stellt wieder her und

rechnet nicht.** Fünf der sechs verändern eine Basis oder einen Zähler, **und dieser setzt einen

Zähler auf das zurück, was eine Basis sagt.** Ein Leser, der ihn wie seine Nachbarn behandelt hätte,

hätte addiert — und ein Spiel, das nach jedem Kampf heilt, hätte eine Mannschaft ohne Grenze.



**Zwei der fünf Tests waren über die Fixture gestolpert.** `Rm2kActorValues` startet bei **einer**

Trefferpunktzahl und null SP — nicht bei null, damit eine Änderung um minus eins keine negative

Basis erzeugt — **und die Basis wird über `AddToParameter` gesetzt, nicht über einen Setter, weil

es keinen gibt.**



**Test evidence** `test_rm2k_full_heal.cs` (5).

**1375/1375**, Validator grün.

**Mutations** 6 Regeln über einen Lauf, **6 von 6 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, die Heilung durch Addition, die mitgeheilte Basis, die nicht geheilten SP,

die auf sechs gesetzte Mindestbreite und der ignorierte Auswahlmodus.



**Und was damit klar ist: `10440`/`10450`/`10480` sind keine Verdrahtung.** Skills, Ausrüstung und

Bedingungen haben **im Zustand überhaupt keine Felder** — das ist neues Zustandsdesign und keine

Befehlszeile, und es gehört in eine eigene Karte.


## `1008` Change Class is done — the last of liblcf's RM2K codes

**The inventory was wrong, and measuring it is what showed that.** A fresh
count against `liblcf`'s `ec.h` gives **157 codes in the 1000..21999 range,
117 of them dispatched, 40 missing — and 36 of those 40 are the Maniac and
EasyRPG patch extensions.** Of the four real gaps, `1005 CallCommonEvent`,
`1006 ForceFlee` and `1007 EnableCombo` **do not exist in EasyRPG at all**
(`grep` finds zero `CommandForceFlee` and zero `CommandEnableCombo`), and
`1008 ChangeClass` is a real RPG2K3 command in `game_interpreter.cpp` with
`CmdSetup` width 7.

**The board's "eighty-nine unwired commands" was a stale number from before
the work, and it stayed on the board because nothing re-measured it.** The
count that matters is re-derivable from `ec.h` and the dispatch table, and
that is now the check.

### And the two RPG_RT bugs the reference comments on

The reference's `Game_Actor::ChangeClass` says, in its own words:

```
// RPG_RT always removes all equipment on level change.
...
// RPG_RT always resets EXP when class is changed, even if level unchanged.
```

**Both are compatibility and not tidiness.** A reader that kept the equipment
would have left a hero wearing the previous class's armour with the new
class's statistics, and one that tied the experience reset to a level change
would have left a hero carrying another class's progress. **And class zero is
"no class"** — the reference guards its warning with `class_id != 0`, and a
reader that refused zero would have refused the one class change a 2K3 game
can undo.

**And the whole command is behind `if (!Player::IsRPG2k3Commands()) return
true;`** — a 2K file has no such field, and a reader that ran it anyway would
have written a number RPG_RT never writes.

**What is not modelled, and says so:** the class table's own skill list.
`ClassSkillsFor` returns nothing and the diagnostic says the class table is
unread — **an invented skill list would be a number a game can be wrong
about**, and a skill that does not exist in the file is worse than a missing
one.

**Test evidence** `test_rm2k_change_class.cs`, 7 tests.
**1528/1528**, `TestRm2kChangeClass: 7/7`.

## `11560` Play Movie is done — and it is a request, not a playback

**Width 5 and a string, and the string is the file name.** The reference reads
`ToString(com.string)` for the file and `parameters[0..4]` for everything
else. **A reader that looked for the name in the first parameter would have
read the mode byte as a file name** — and that byte is 0 or 1, so every movie
would have been one file called "0".

**And the first parameter is the mode for both positions**, the same shape
`10910` has.

**There is no timer, because the reference has none.** `Game_Screen::PlayMovie`
is five assignments — a file name, two positions and two resolutions — and
nothing else. **A reader that modelled a movie as something that plays and then
ends would have had to invent the end**, and an invented end is a number a game
can be wrong about: a cutscene would advance before its last frame on a machine
slower than the author's.

**The reference's own body says it plainly:**

```
Output::Warning("Couldn't play movie: {}. Movie playback is not implemented
                 (yet).", filename);
```

**and then it stores the request and returns true, which advances the page.** A
reader that refused the command would have stalled a game's event on a cutscene
it cannot show, and one that reported success would have claimed a capability
the reference does not have. This stores the request and says in the
diagnostics that nothing is playing it.

**A position of zero is a position.** The reference has no range check, so a
movie at 0, 0 is asked for at the screen's corner — **and a reader that used
zero as "unset" would have drawn a game's cutscene somewhere the file did not
say.**

**Test evidence** `test_rm2k_play_movie.cs`, 11 tests.
**1521/1521**, `TestRm2kPlayMovie: 11/11`, validator passed.
**Mutations** Seven rules, **7 of 7 caught**. The first run was 5 of 7, and both
survivors were mine: the mode-byte test had both coordinates on values that
would answer the same way, and no test covered `Reset` at all — **and `Reset`
is what `Rm2kEngineRuntime` calls before every game**, so a reader that left
the movie fields alone would have started a second game with the first game's
cutscene still marked as requested.

### And the anchor that eleven methods share

`if (pCmd.Parameters.Count < 5)` occurs twelve times in the interpreter and the
comment `// CmdSetup minimum width 5.` eleven times — **one per command whose
CmdSetup width is five.** A mutation rule anchored on either of them would have
edited whichever came first and reported on that command. **The anchor has to
carry the method signature**, which is what makes it unique:

```
private void ExecutePlayMovie(Rm2kMap.EventCommand pCmd)
{
    // CmdSetup minimum width 5.
    if (pCmd.Parameters.Count < 5)
```

**This is now the third time the same class of mistake has cost a run** — the
variable check in two command handlers, the map index in two methods, and now a
format width in eleven. **The pattern is worth stating once: an anchor that is
not unique is not an anchor, and the fix is always to add more context until
`count() == 1` holds.**

## `10740` Enter Hero Name is done — and the sentinel in the save file

**Width 3: the hero, the face index, and a flag.** The reference reads
`actor_id`, `charset` and `use_default_name` and builds a name scene from all
three. **The second is an index and not a file name** — a face is a position in
the actor's own face set, and a reader that read it as a charset name would
have looked for a file called "2". **And the third is a flag and not text**: a
reader that treated it as part of a name would have shown every hero a name
ending in a digit.

**The command writes no name of its own.** The reference hands the work to a
scene, and the scene writes the name when the player is done — **so a reader
that stored the name here would have renamed a hero with no prompt at all**,
which is a different game and a worse one.

### The sentinel, which is the whole reason the command is worth wiring

**The reference's `Game_Actor::SetName` is a comparison and not an
assignment:**

```
data.name = (new_name != dbActor->name) ? new_name : SaveActor::kEmptyName;
```

**Only a name that differs from the database's is kept, and the rest is a
sentinel.** A save that carried the database name into every hero would keep
the *old* name after the game was renamed in the editor, and the reference's
save format has a sentinel precisely so that distinction survives. **And an
empty name is a real name** — a player may call a hero nothing, and the
reference stores that as a name of zero length while the sentinel is a
different value entirely. A reader that used `""` for "unchanged" would have
made every renamed hero nameless the moment the save was written.

### And looking up is not creating

`CommandEnterHeroName` warns on a null from `GetActor` and moves on.
`FindActorValues` in the interpreter **created the entry** — so a game that
named hero 99 would have created hero 99, and that hero would then exist for
every later command, in the party window and in the save file. The state now
has `FindActorValues` beside `GetOrCreateActorValues`, and only `10740` uses
the first.

**Test evidence** `test_rm2k_enter_hero_name.cs`, 9 tests.
**1509/1509**, `TestRm2kEnterHeroName: 9/9`, validator passed.
**Mutations** Nine rules, **9 of 9 caught** once the harness's restore list was
derived from the rules instead of written next to them. The first two runs
reported `LEBT` and `NOMATCH` for lines the harness itself had just put back —
see `SESSION_STATE.md`.

## `10830`, `10870` and `10910` are done — and the stale comment

**Three commands, and each one has a fact a reader would guess wrong.**

**`10830` Recall to Location takes three variable ids and not three
coordinates.** The reference reads all three through `game_variables->Get()`
and teleports to what they hold. **A reader that read them as coordinates would
have sent a game to the map whose number the editor happened to write** — and a
2K game's "recall" would have gone to map 1, tile 1, which is a real place on
every map.

**And it writes a facing of minus one, which is the reference's own
"unchanged".** `10810` writes a direction and defaults to the one the player
has; `10830` writes -1 because a recall has no reason to turn the hero. **A
reader that copied 10810's default would have turned a game's hero on every
recall.**

**`10870` Trade Event Locations swaps three coordinates, and reads all six
before it writes any.** A reader that moved one onto the other would have
collapsed two events onto one tile, and **one that moved the first figure
before it found the second missing would have collapsed two guards onto one
tile** — the reference's whole exchange is behind a single `if`.

**`10910` Store Terrain ID gives the first parameter to both coordinates.** The
reference writes `ValueOrVariable(parameters[0], parameters[1])` for x and
`ValueOrVariable(parameters[0], parameters[2])` for y — **the same mode byte
governs both** — and the fourth is the variable to write. A reader that gave
each coordinate its own mode would have read a game's row from a constant while
its column came from a variable.

**And the reference's own comment says `code 10820`.** The dispatch line and
liblcf both say `10910`, so the comment is stale — **and a reader that believed
it would have implemented 10820**, which is a different command with different
parameters. A stale comment in the reference is a fact about the reference, and
the number in the file is the one that counts.

**A tile outside the map is -1 and not zero**, and -1 is also the value a game's
"no terrain here" test writes.

**And the chip id lives in the lower layer and not in the passability.**
`PassableTiles` holds one bool per tile and is a side calculation from the same
layer — **a reader that took the chip id from it would have seen the same
number on every tile pair**, and the chipset entry would have been a digit to
guess. The reference's `Game_Map::GetTerrainTag` reads the lower layer, takes
the chip, and looks the chip up in the chipset's terrain table.

**Test evidence** `test_rm2k_map_recall_and_terrain.cs`, 19 tests.
**1500/1500**, `TestRm2kMapRecallAndTerrain: 19/19`, validator passed.
**Mutations** Sixteen rules; **13 caught on the first run, and three of the
four survivors were my own tests rather than the implementation** — see
`SESSION_STATE.md` for the full chain. The last two survivors were two of the
sixteen rules whose **anchor occurred twice in its file** and which had
therefore mutated a different command: `if (varId < 1 || ...)` sits in both
`ExecuteStoreEventId` and `ExecuteStoreTerrainId`, and
`var index = pX + pY * MapWidth;` sits in both `TerrainTagAt` and
`IsPassableInDirection`. **`replace(..., 1)` takes the earliest match, so an
anchor that occurs twice is not an anchor.** The harness now requires
`count(anchor) == 1` before it mutates anything.

**And one of the restores left a mutation in the source while the tests stayed
green** — the recall read the column as a value instead of out of a variable,
and the test passed because the value at that index was the one it expected.
**A test that asserts a number the broken code also produces is a description
of the current state, not an expectation.**

### And a terrain test that could not tell two failures apart

The first four terrain tests asked a map that was empty, and every one of them
passed — **for the wrong reason.** The -1 came from "there is no map" and not
from "the tile is out of bounds", so a test that cannot tell those apart is a
test of the bounds check and not of the terrain. **The first run of the
mutations found it: four rules survived, and three of them were mine.**

The map is now built the way the loader builds it — `ConfigureMap` for the
dimensions and the tiles, and the two fields the LMAP read hands over — and two
of those rules are the ones that were blind:

- a reader that returned the same number for two different chips on one map
  would have made a game's terrain branch a constant
- a reader that clamped an unknown chip to zero would have called it "normal",
  and **zero is a real terrain number**

**And the mode byte is only measurable if the two answers are on tiles with
different terrain** — my first version put both on tiles that happened to
answer the same, **and the wrong reader would have passed it.**

## `11320` Flash Sprite is done — criterion 3, the map's flash

**Width 7, and the seventh parameter is a mode byte that only the Maniac patch
reads.** The reference reads every channel through
`ValueOrVariableBitfield(com, 7, shift, val_idx)` — **and without the patch
that helper is `return com.parameters[val_idx];` and nothing else.** A reader
that always applied the bitfield would have taken a red channel of 31 down to
15 for every game ever written in RPG Maker 2000.

**The duration is in tenths, and the reference's own rate is sixty frames per
second** — so ten tenths is sixty frames, and a reader that passed the tenths
on would have flashed a sixth as long.

**A duration of zero still waits one frame.** The reference's `SetupWait` has
a separate arm for zero — `if (duration == 0) wait_time = 1; else wait_time =
duration * DEFAULT_FPS / 10;` — so a game's "flash for no time" still holds its
page for a frame.

**And a character that does not resolve is a warning, not a refusal.** The
reference's `GetCharacter` returns null, the whole flash is skipped, and the
command still advances — a reader that held the page for a flash that never
happened would have stalled a game's event on a name no figure carries.

**The command is a hook and not a field,** exactly as `11330` is: the
interpreter names a figure, and whoever owns the figures decides whether that
name resolves. **Seven parameters is one too many for `Func<T, TResult>`,** so
the hook is a named delegate rather than a squeezed tuple.

**And the frame that triggers the command is not part of its own wait** — a
flash of ten tenths releases the page after sixty-one calls, because
`ExecuteFrame` checks the budget before the page moves. A reader that counted
the trigger frame would have held every timed command one frame too long.

**Test evidence** `test_rm2k_flash_sprite.cs`, 10 tests.
**1481/1481**, `TestRm2kFlashSprite: 10/10`, validator passed.
**Mutations** Ten rules; **10 of 10 caught when each is measured on its own.**
The script reports nine — a survivor that moves between runs, which is the
restore race documented in `SESSION_STATE.md`, and not a test gap.

## The battle branch family is done — `13310`, `13410`, `23310`, `23311`

**Mutations** Twelve rules, **12 of 12 caught.** The first run reported
`11 von 12` with `13310 erreicht den Dispatch nicht` as the survivor —
and that rule, measured on its own, produces **eighteen failures**. The
harness still carried the pre-fix restore list, so the mutation of rule
three was never compiled into the DLL the runner loaded. **A survivor in
a run whose harness has a known defect is not evidence about the test
suite**; measure the rule alone before believing either number. The body
is now shared by every script, with the backup and restore lists derived
from `RULES` and a five-times retry on `WinError 1224`.


**Four codes, and the branch is the only one of the four with real work.**

**`13310` has width 5 and six modes, and the last two are 2003-only** — the
reference guards the fourth with `IsRPG2k3Commands() && targets_single_enemy &&
target_enemy_index == parameters[1]` and the fifth with
`IsRPG2k3Commands() && current_actor_id == parameters[1]`. **A reader that
evaluated them anyway would have taken a 2K game's branch with an enemy's
number in a file that never carried one.**

**And the fourth mode needs a single target before it needs the right index** —
the reference compares the flag first, so a battle with every monster aimed at
once never matches, whatever the index says.

**And the switch comparison is a boolean equality, not an inversion.** The
reference writes `Get(id) == (parameters[2] == 0)` — and `0 == 0` is `true`, so
a third parameter of zero asks whether the switch is **on** and a third
parameter of one asks whether it is off. **A reader that read it as a bare
"is it off" would have had every switch in every game the wrong way round.**

**And six comparison kinds for the variable mode, where the third parameter
chooses a constant or a variable and the fifth chooses the comparison** — equal,
greater or equal, less or equal, greater, less, different. A seventh leaves the
result false, because the reference's switch falls out with the false it
started from.

**`13410` is an abort and not a defeat.** The reference's whole command is
`MakeTerminateBattle(BattleResult::Abort)` — **a fourth outcome beside victory,
escape and defeat, and no handler is named for it.** A reader that wrote
"defeat" would have had a game that deliberately abandons a fight reach the
game over screen. **And it returns false**, so the frame stops: a reader that
advanced would have run a game's victory rewards after it abandoned the fight.

**And a false branch sets the sub-index and skips — both.** The reference does
`SetSubcommandIndex` and then `SkipToNextConditional({ElseBranch_B,
EndBranch_B})`. **A reader that only set the index would have run the then
block**, which is exactly the block the branch is meant to skip, and one that
only skipped would have left the else branch with nothing chosen.

**A dying monster cannot act while it is still in the troop** — the reference
gives him a death timer and not a removal, so **a reader that asked "is he in
the troop" would have had a dying boss strike back on the frame he fell.**

**Test evidence** `test_rm2k_battle_branch.cs`, 14 tests.
**1471/1471**, `TestRm2kBattleBranch: 14/14`, validator passed.
**Mutations** Twelve rules. The script reported 11 of 12 caught across three
runs, with a **different** survivor each time; **every rule was then measured
on its own and all twelve are caught.** The difference is the script's, not the
suite's — see `SESSION_STATE.md` on MSBuild's timestamp comparison.

## `11210` and `13260` Show Battle Animation are done — the first step on criterion 3

**Two codes, one method.** The reference's dispatch hands both to
`CmdSetup<&CommandShowBattleAnimation, 3>` with no second implementation — so
they differ in their number and in nothing else, and a reader that gave them
different behaviour would have invented a difference the format does not have.

**Width 3, or 4, and the fourth is a 2003 form only.** The reference reads it
under `if (Player::IsRPG2k3() && com.parameters.size() > 3)` — so a reader that
required four would have refused every 2K game, and one that read the fourth
unconditionally would have shown a 2K game's "aim at the party" as "aim at the
enemies".

**Allies count from one and enemies from zero.** The reference subtracts one
from a party target and not from a monster target — so a target of 0 is the
first enemy and the *zeroth* ally, which does not exist. **A reader that used
one numbering for both would have played a game's first hero's animation on its
second hero.**

**A negative target is the whole side, and the flag says which.** The reference
reads `target < 0` — not `<= 0` — and then collects the party or the enemy
party, so a target of -1 without the flag is every *enemy*.

**And the wait is the animation's own length.** The reference writes
`_state.wait_time = frames` and the frames come from
`BattleAnimationBattle::GetFrames()`, which is the animation's last timing row.
**A reader that invented a duration would have held the page for a number the
game never wrote** — and a battle where the hero's sword animation is 30 frames
would have frozen for 12.

**And an animation that is not in the table plays nothing and waits for
nothing** — the reference's `GetElement` returns nothing, warns, and returns
zero frames, so a game's mistyped animation id cannot freeze its page.

**Test evidence** `test_rm2k_battle_animation.cs`, 9 tests.
**1457/1457**, `TestRm2kBattleAnimation: 9/9`, validator passed.
**Mutations** Ten rules over two runs, **10 of 10 caught**.

## The three actor commands are done — `10440`, `10450`, `10480`

**Widths of 5, 5 and 4, all three starting with the reference's own
`GetActors(mode, id)` and all three ending in `CheckGameOver()`.**

**The remove flag is the third parameter in all of them, and it means remove.**
A reader that read it as "add" would have taught a skill to a hero whose
command meant to take it away, and would have healed a poisoned hero with the
condition command meant to cure him.

**`10450`'s slot comes from the item's own type in the first mode and from the
parameter in the second.** The reference reads the item and writes
`slot = item->type` across weapon, shield, armor, helmet and accessory — **so a
reader that took the slot from the parameter in both modes would have put a
helmet where a sword goes.** Mode 1 is `parameters[3] + 1`, and its
`item_id` is zero: the direct slot *removes* rather than equips.

**The sixth slot is not a slot.** The reference checks `slot == 6` before any
of the five and empties the whole actor — so a reader that wrote the sixth
value as a sixth slot would have left a hero's armour on and hidden the
removal. **An item that is not equipment is left alone and says so**, and a
third mode is refused — the only one of the three commands that returns false
instead of repairing.

**Two rules for a two-weapon actor, and both are about the same hero.** The
reference skips a shield outright for a two-weapon actor while the shield is
in hand, and puts a one-handed weapon into the second slot when the first is
empty and *neither* weapon is two-handed.

**And `10480`'s removal does not ask where the condition came from.** The
reference's own comment records it as an RPG_RT quirk: on the map it removes a
state even when the actor has it from equipment. **A reader that respected the
equipment's own state would have left a hero permanently poisoned by a ring he
never took off**, in a game the reference lets him walk out of.

**Test evidence** `test_rm2k_actor_commands.cs`, 15 tests.
**1448/1448**, `TestRm2kActorCommands: 15/15`, validator passed.
**Mutations** Thirteen rules over three runs, **13 of 13 caught**.

## The shop and inn family is done — `10720`, `10730` and the ten handlers

**Twelve commands, and ten of them have a width of zero.** 20710, 20711, 20712,
20713, 20720, 20721, 20722, 20730, 20731 and 20722 are `CmdSetup<..., 0>` —
a handler is a name for a block, and a reader that expected parameters to read
would be reading past the end of a list that is not there. The two openers are
10720 with a width of 4 and 10730 with a width of 3.

**10720's first parameter is a mode and not a value, and its switch has three
cases and a default that does nothing** — 0 buys and sells, 1 buys, 2 sells, and
a fourth buys and sells nothing. **Its goods start at the *fourth* parameter:**
the reference copies everything from `parameters.begin() + 4` on into one list,
so a width of 4 means a shop with no goods at all. **Its second parameter is
the shop's type and not a price**, and its third is a handler flag the
reference reads and does not use.

**10730's price is the *second* parameter, and the first is the inn's type** —
the reference writes `int inn_price = com.parameters[1]` in the command's first
two lines. A reader that took the type for the price would have charged a party
the inn's kind for a night's rest. **A price of zero skips the prompt** — the
reference has its own branch for it and the comment there says "Skip prompt".

**And a handler runs its block only when it is the option that was chosen.**
`CommandOptionGeneric` reads the sub-index, compares it with the option, and
then either writes the sentinel or skips to the next handler — **so a reader
that always skipped would run a shop's "you bought nothing" branch beside its
"you bought something" branch**, and one that always ran would run both.

**Each handler has its own closing list and the lists are different lengths.**
A shop transaction ends at the no-transaction or the end shop; a
no-transaction at the end shop alone. A victory ends at the escape, the defeat
or the end battle; a defeat at the end battle alone. **And the closing list is
only ever read in the skipping arm**, because a chosen handler runs its block
and goes on — which is why two mutations that lengthened the one-entry lists
survived a suite whose tests all *chose* their handlers.

**20722 changes nothing.** The reference's `CommandEndShop` is a bare
`return true;` with the parameter named away — the shop's own scene closed when
the player left it. **A reader that cleared the shop state here would have had
a game's shop close the moment its own block ended**, which is a different
event. 20732 and 20713 do clear state.

**And a stay does not heal the party** — the reference's `CommandStay` is the
handler, and what a stay does to the party's hit points is the inn's own
business, the same split the reference makes for a shop's trading.

**Test evidence** `test_rm2k_shop_and_inn.cs`, 23 tests.
**1433/1433**, `TestRm2kShopAndInn: 23/23`, validator passed.
**Mutations** Twenty-one rules over three runs, **21 of 21 caught** — the
handler that runs unchosen, the sentinel, both one-entry closing lists, the
three shop modes, the goods' start parameter, the inn's price parameter, the
battle ending, the skip loop, and all twelve dispatch arms.

## The battle-only family is done — `13110`, `13120`, `13130`, `13150`, `13210`

**The card listed `13110`–`13410` and `20720`–`20732` as codes liblcf names and this
repository does not implement. Seven of them are real RM2K/2000/2003 battle
commands**, and the card was right that they were missing and wrong about what
they are. Measured against liblcf's own `eventcommand.h`: **32 of its 164 codes
are unwired, of which 44 are `Maniac_`/`EasyRpg_` patch extensions and engine
features below 6000 — and 32 are the real command set.**

**`13110 Change Monster HP` has three change modes, and the third is a share.**
0 is a constant, 1 a variable and 2 a percentage of the monster's own maximum —
so mode 2 on a monster with 500 of 1000 hit points takes 250, and not 2. **A
reader that read mode 2 as another constant would have healed a wounded boss
for one hit point** where the game asked for a tenth of his life.

**The sign is a flag and not the value's own sign.** The reference reads
`bool lose = com.parameters[1] > 0` and then writes `change = -change` — so a
game that wrote a negative number with the flag at zero still heals, and one
that wrote a positive number with the flag at one still hurts. **The value's
own sign is read never.**

**`13120 Change Monster MP` has two modes and not three.** The reference's
switch is a constant and a variable and no third case — **so a mode of 2
changes nothing**, where `13110` has a percentage. A reader that reused the
hit-point command's modes would have changed a share of a maximum this command
never reads.

**There are two deaths and they are not the same one.** A monster whose hit
points reach zero gets the system's enemy-kill sound and a **death timer**; a
monster whose death condition is removed by `13130` disappears **at once**,
and the reference's own comment writes that down as an RPG_RT bug it
reproduces — "Monster dissapears immediately and doesn't animate death". So
the exit is one of three values here, and a reader that treated the two paths
alike would have animated a death the reference does not animate.

**`13130`'s second parameter is remove-or-add and not add-or-remove** — a
reader that read it as "add" would have healed a poisoned monster with the
command meant to cure him.

**`13150` is one parameter, one flag cleared, and no second arm.** A monster is
hidden in its database row and this is the only command that shows it.

**`13210` reads its file name out of the command's text.** The reference writes
`Game_Battle::ChangeBackground(ToString(com.string))` — a reader that looked in
`parameters` would have found a single zero and left every battle with the
background the troop file named.

**And an id that is not in the troop warns and grows nothing** — the
reference's `GetEnemy` returns nothing, and a reader that created a monster
instead would have grown the troop with every bad id a game contains.

**Test evidence** `test_rm2k_battle_monster_commands.cs`, 15 tests.
**1410/1410**, `TestRm2kBattleMonsterCommands: 15/15`.
**Mutations** Twelve rules over one run, **12 of 12 caught**.

## `10710` Enemy Encounter ist fertig — fünf Zustandsfelder, die kein Befehl erreichte

**`IsBattleActive`, `ActiveTroopId`, `BattleTurn`, `BattlePhase` und `TroopMembers` waren im

Simulationszustand. Kein Befehl erreichte eines davon** — also fiel der Kampfbeginn eines Spiels in

den Default-Arm, es kämpfte nie, **und der Zustand führte eine Kampfphase von sich aus mit.**



**Dieselbe Inselform wie die Bilder, die Laufbahnen und das Fahrzeug-Bording:** Zustand, der

vollständig ist, und unerreichbar.



### Die sechs Regeln, die der Rumpf der Referenz ergibt



**Sechs Parameter, oder zehn, und die Zahl hängt an der Form.** Die Referenz hat für diesen

einen Befehl zwei Dispatch-Zeilen — eine mit Breite 6 und eine mit 10 für die RPG2K3-Form. **Ein

Leser, der zehn verlangte, hätte jeden 2K-Kampf abgelehnt.**



**Die Flucht sind drei Werte und kein Boolean.** Die Referenz schreibt

`escape_mode = com.parameters[3]` mit 0 für „gar nicht", 1 für „Event-Verarbeitung beenden" und 2

für den eigenen Handler des Spiels — **und der mittlere setzt `abort_on_escape`, was das Event

beendet.** Ein Leser, der es als Boolean las, hätte ein Spiel, dessen Flucht zur nächsten Zeile

zurückkehrt, wo die Referenz das Event tot beendet.



**Drei Terrain-Modi, und der vierte wird abgelehnt.** Der Switch der Referenz hat die Fälle 0, 1

und 2 und ein `default: return false` — **also startet ein Modus von 3 überhaupt keinen Kampf.**

Ein Leser, der auf den ersten zurückfiel, hätte einen Kampf geführt, den die Datei nicht verlangt

hat, und ein Spiel, das einen Testkampf mit verschobenem Modus geschrieben hat, hätte einen

echten gehabt.



**Eine Niederlage ist Game Over, sofern der Befehl nichts anderes sagt** — und die Referenz

schiebt den Game-Over-Bildschirm selbst.



**Kein Ausgang und -1, und die Phase ist 1.** Die Referenz schreibt 0 für Sieg, 1 für Flucht und 2

für Niederlage in den Subcommand-Index des Befehls. **Ein Leser, der 0 schrieb, hätte den

Siegarm laufen lassen, bevor gekämpft wurde** — und 0 ist der Siegwert, der Fehler wäre also in

einem Test, der nur die Zahl prüft, unsichtbar.



**Und eine offene Nachricht zuerst, mit derselben Regel wie Game Over und die Menüs.**



### Und die sechzehn Messungen, die kein Befund waren



**Ein Test ließ sich nicht kompilieren, und ich habe ihn sechzehn Mal gemessen.** Die Datei war

korrekt — kein verborgenes Zeichen, keine falsche Einrückung, keine doppelte Deklaration, und

`sed`, `od` und `read_file` zeigten dieselben Bytes. **Der Compiler hatte recht: die

Tuple-Zerlegung `var (a, _, b)` in diesem einen Test war der Fehler**, und die anderen sechs

Tests derselben Datei mit derselben Zerlegung liefen.



**Das ist derselbe Fehlertyp wie bei `IdleCell` — und die Lehre ist diesmal klarer: sechzehn

Messungen an korrektem Quelltext sind kein Befund, sondern eine Schleife.** Der Ausweg war

derselbe: aufhören zu messen und die eine Sache tun, die ich nie getan hatte — den Test ohne

die Zerlegung schreiben.



**Test evidence** `test_rm2k_enemy_encounter.cs` (7).

**1382/1382**, Validator grün.

**Mutations** 9 Regeln über zwei Läufe, **9 von 9 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, der nicht aufgelöste Trupp, der unbekannte Terrain-Modus, der die

Flucht zum Boolean gemachte Escape-Modus, die initiale Phase, der sofortige Ausgang, die immer

erzwungene Niederlage, die nicht haltende Seite und die ignorierte offene Nachricht.

## Agent maintenance rules
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.


### K-127 Put a picture on the screen and move it off again
`DONE` — pictures, P2, no dependencies

**What it is.** K-121 to K-126 read MZ data, walked event lists, changed what
the party carries. **This is the first command in this game that needs
something other than numbers to have an effect**: nine of them on the one map
in the fixture, on images 1, 86 and 87 — three show a picture, four move one,
two erase one. A reader with no place to put a picture has nothing to say
about them.

**Not 127 and not 128.** Those are Change Weapons and Change Armors, and this
game's `Map002` has **none of them** — no 127, no 128, no 129, no 130. So
carrying them would have meant writing rules no data in this repository can
check, and the fixture has no `Weapons.json` and no `Armors.json` to check
them against. The pictures were chosen because the data is here.

**The rules, each read out of rmmz_objects.js 1.9.1 rather than inferred**

1. **A shown picture is a new object.** `showPicture` makes
   `new Game_Picture()` and puts it in the slot, so a tint, a rotation and any
   movement are gone with the old one. A reader that changed the existing
   picture in place would keep what the engine has just discarded.
2. **A picture id is routed through `realPictureId`, which is not the
   identity.** In a battle a map picture and a battle picture share the
   editor's number. **This game's `System.json` sets `picturesUpperLimit` to
   110**, not the hundred `maxPictures` falls back on, and a reader using the
   hundred would put a battle picture on top of a map one at the wrong offset.
3. **The fourth parameter says where the fifth and sixth are read from.**
   `picturePoint` reads them as numbers when it is zero and out of variables
   when it is not, and a reader that read them as numbers either way would
   place a variable-positioned picture at the variable's own number.
4. **A move sets a target, not a value.** `updateMove` only moves while
   `_duration > 0`, so **a move of zero frames changes nothing at all** and asks
   for no wait even when the game asked for one.
5. **A move on an empty slot does nothing**, and is recorded rather than
   dropped — a game that moves a picture it never showed has a reason a log
   should hold.
6. **Only a move that asks to wait holds the list up.** `if (params[11]) {
   this.wait(params[10]); }` and there is no second one. This game asks for
   the wait on **two of its four** moves and not on the other two.

**A real fault this card found in reading, not in testing.**
`MzCommandEntry.From` handled a Number and took `item.Text` for everything
else, so a JSON **boolean** became the empty string. A 232 carries its wait in
the eleventh slot as a real `true`/`false`, and this game's four moves came
back as four that never ask to wait. No test had noticed, because no test had
read a boolean out of an event list. It is a lost value in a file this
repository claims to read, and it is fixed in the reader rather than worked
around in the test.

**A second one, of my own.** `if (params[11])` is a truth value, and a first
draft called `int.Parse` on it — which throws on the empty string a game may
leave in that slot. Reading it the way the engine reads it is now its own
named method.

**A third, and the worst of the three: a waiting move never arrived.**
`ExecuteOne` did not step the index when a command left the interpreter in
`Waiting`, and a `MovePicture` that asked to wait did `return false`, which
means the same thing. So the next frame read the same 232 again, set the same
twenty frames again, and **a picture that had to move across the screen
waited for ever and never got there.**

The engine has none of this trouble: `command232` ends in `return true`
whatever it asked for, and the wait it set lives in `_waitCount` where the
next command cannot reach it. The index moves and the run stops in two
separate steps now, which is what the engine's frame does — the command is
done, the frame is not. `MzInterpreter` runs 18 and `MzEventRunner` 16 tests
and both are unchanged after it, so this was a fault in a rule nothing had
exercised rather than a change to a rule something had.

**And a fourth, of my own again.** A test that claims a picture is at
`2000, 2000` because the scale is `2000, 2000` is reading the wrong line. The
event says `1, "UI/Status_HelpCollision", 0, 0, 0, 0, 2000, 2000, 255, 0`:
**a place of nothing and a picture two thousand times its own size**, and a
reader that put the scale into the place would have shown it off the bottom
left of the screen. The claim was corrected to the measured value, not
adjusted until it passed.

**A test that counted is not a test that ran.** The first draft's ninth test
was called "every picture command in this game runs" and it counted: three
shows, four moves, two erases, read out of the file without an interpreter in
sight. Four mutation rules escaped because of it. The test that replaced it
builds an interpreter, hands it the frames the two waiting moves ask for,
and checks what the screen holds when the list is through — and it is the
test that found the waiting-move fault.

**And an equivalent mutant that was not equivalent at all, twice.** Removing
`pInterpreter.Wait(frames)` entirely passed the suite, because the first draft
of the walk-through drove the screen's frames from the test's own loop — so a
reader that never waited still moved the picture and ended in the same place.
**It is equivalent for the picture and wrong for the page:** without the wait
the four commands after the move run in the same frame, and a game that fades
a picture out over twenty frames would run the rest of the event while it is
still at full opacity. The test that killed it claims frames and not an end
state: the interpreter is held for exactly the movement's length, the index is
already past the move, the command after it has not run, and it is released
when the frames are counted off.

**A fifth mistake of my own, in the same test.** A 122 written as four
parameters — `1, 0, 0, 5` — has nowhere to read a value from, because
`command122` is `startId, endId, operationType, operandType, operand` and
**the operand is the fifth**. The page was not held by the move failing; it
was held by a command that could not do what the test meant.

**Two more test gaps, found the same way.** A move that does *not* ask to
wait had no test of its own, so replacing the wait condition with `true`
passed — the rule was only ever checked from the side where it says yes. And
`if (params[11])` had no test with a parameter the game wrote as something
other than a boolean, so reading it as `written != ""` passed too. **A rule
checked from one side is half a rule**, and both halves are now tests of their
own: one that the page runs on in the same frame and the picture still moves,
and one that `true` and `1` ask while `false`, `""`, `0` and `no` do not.

**Test evidence** 11 tests in `project/tests/core/test_mz_screen.cs`.
**Total 935/935**, validator passed, build 0 errors.

**Mutations** Twenty-two rules over four runs, and the shape of the escape is
the same one this repository keeps meeting: **every rule that survived was a
rule no test had asked about from the side it fails on.** Run one caught 7 of
11. Run two caught 3 of 7. Run three caught 2 of 4. Run four is the full set
on the finished suite. The three escapes in run two were the waiting-move
fault, the boolean parameter and a direct-call gap; each one turned out to be
a real fault in the reader or in the index, not a weak test.

**What is deliberately not here.** A picture is a name, a place and some
numbers; it is not a texture, and nothing here loads one. The blend mode and
the scale are kept as the numbers the game wrote rather than resolved to a
rendering, because a reader with no renderer must not pretend to have one. The
easing is stored and not applied: `PassFrame` lands the last frame exactly on
the target, as the engine's easing is built to do, and does not walk the
straight line in between — which is stated rather than faked. 233 (rotate),
234 (tint), 236 (weather) and 224 (fade) are the next pictures and are not
here.


### K-128 Measure what this game actually needs from MZ before modelling more of it
`DONE` — measurement, P1, no dependencies

**Why this card exists.** K-127 asked which picture commands come next, and the
answer was: **none of them.** This map uses no 224, no 233, no 234 and no 236.
Modelling them would have been rules no data in this repository can check —
the mistake K-127 already refused to make once.

**So what is left in the one map that is here?** All twenty-two codes in it
are real MZ 1.9.1 commands, measured against the 114 `commandNNN` methods in
`rmmz_objects.js`. Every one of them is now either modelled or refused:

| Code | What it is | State |
|---:|---|---|
| 0, 401, 412, 655, 657 | steps over, as the engine does | K-124 |
| 101, 111, 112, 113, 117, 121, 122, 413, 601-603 | text, branches, control flow, waits | K-123 to K-126 |
| 126, 230, 231, 232, 235 | party, wait, pictures | K-125 to K-127 |
| **351** | **Open Menu** | **the next one** |
| **355** | **Script** | refused, and stays refused |
| **357** | **Plugin Command** | refused, and has to be |

**The finding, and it is about this game rather than about MZ.**
This game ships **52 plugins and all 52 are enabled.** Its eleven `357`
commands call `ItemCombinationMZ`, `DTextPicture` and `HyoujouSelect`, and
their parameters carry the plugins' own options. `command357` is
`PluginManager.callCommand(this, pluginName, params[1], params[3])` — so a
`357` in this game is nine times a request to run somebody else's JavaScript.

**That is refused, permanently and for the same reason `355` is.** Not
because a plugin call is harder, but because executing foreign JavaScript is
the one thing this repository does not do. A reader that implemented `357`
faithfully would be the thing the security contract forbids, and it would
have been faithful to this game and useless to everyone else.

**What a reader can honestly say about a `357`.** The plugin's name, the
command name inside it, the author's own description, and the parameters as
data — all four are readable without running a line of it. What the plugin
*does* is not answerable, and is not guessed. The same shape as `355`: the
text is kept, the running is declined, and the decline is structured rather
than a silent step over, because a step over would make a game look as if it
worked.

**The two commands, and they mean opposite things.**

`351` is run. The engine's `command351` is `if (!$gameParty.inBattle()) {
SceneManager.push(Scene_Menu); Window_MenuCommand.initCommandPosition(); }
return true;` — **one condition, and it returns true either way.** A menu in
a battle is not this command with another scene, it is nothing at all, and a
reader that stopped the run there would leave the commands after it unrun in a
way the engine never does. `MzMenuState` exists so a 351 is not
indistinguishable from a command with no effect: a reader with no screen still
has to be able to answer "did the game open a menu here", and without somewhere
to write the answer down it could only be silent.

`357` is refused, and **the refusal is structured rather than a step over.**
All nine of this map's 357 commands are answered, each naming the plugin and
the command inside it, and each landing on `MzBranchFacts.Notices` where a
caller looking for what went wrong will find it. A silent step would leave a
game that looks as if it works while its crafting menu and its floating text
never appear.

**Test evidence** 4 tests in
`project/tests/core/test_mz_menu_and_plugins.cs`. **Total 939/939**, validator
passed, build 0 errors. The expectations were all measured out of the game's
own files before they were written — nine plugin commands, three plugins, two
351s, three scripts — so no number in this card is a shape this card chose.

**Mutations** Nine rules, **nine caught**, and one of them had to be written
twice: the first attempt replaced a fragment inside an escaped string and left
the file unparseable, so it came back `BROKE` — which counts as caught and
proves nothing. The second attempt replaced the whole notice with a constant
that still compiles, and it failed three named tests, one for each of the
three plugins this map calls. **A mutation that does not compile is not
evidence**, and this project has now been bitten by that three times.

**And the honest limit this puts on the card.** A game with 52 plugins can
have its own logic in them: `ItemCombinationMZ` is a crafting system, and
this game's `355` scripts read `$gameVariables.value(180)` to work out what
was crafted. **UniversalRPG will run this game's MZ event code and none of
its plugin code**, and no bounded slice can change that. What a card can do is
say so where a caller will see it, once, with the numbers, instead of leaving
a reader to discover it by playing.


### K-129 A second MZ fixture, from a game with no plugins
`DONE` — fixture, P1, depends on K-121

**Why a second fixture was needed.** The first, `mz/` (*Stranded with You*),
carries **52 enabled plugins** and **nine plugin commands** on its one map. A
reader checked against it is mostly checked on its refusals, and barely at all
on the event code. That is not a fault in the reader — it is what that game
is. It needs a second game to be a statement about MZ.

**The game.** `CamelliaCoronation-Win`, in `E:/RPGMakerGames`, a free MZ game
put there by the user to work with. Engine **RPG Maker MZ 1.9.1**, measured and
not assumed: both games' `rmmz_objects.js` carry **the same 114
`commandNNN` methods**, with none only in one or only in the other.

**One plugin, and it is in no command.** `extra_party_member`, enabled, with an
empty parameter list. **No `355` and no `357` on any of the nineteen maps** —
counted over the files, and that negative claim is the reason the fixture
exists. A reader that refused nothing would run this game completely, and
there would be nothing to hide.

**What it measures, all of it counted rather than quoted:**

- **2 432 Befehle** over nineteen maps, **1 772 of them run today** and **660
  not**. Every one of the 660 is a real MZ command, not a plugin call.
- The most-used is **401, the line of text, at 938**. Then **101, the dialogue
  block, at 414**, then **505, the move route, at 348**. A first draft called
  the move route the most-used, on the grounds that a game is "mostly made of"
  it, and was wrong by two places.
- **Fifteen variables, numbered 0 to 15, and none above.** **Eight items.**
  One class, one animation, **no switches, no common-event calls, no actor
  references.** A reader that has read this fixture has read everything this
  game refers to — and the first fixture says the opposite, so between them
  they say how far a bounded slice can honestly go.
- **`CommonEvents.json` is 376 bytes and present.** The first fixture had none
  because the original was 4,5 MB, and the runner had to refuse a 117 by
  naming a common event it could not read. That was honest for a gap. **Here
  there is no gap**, and a rule only ever tested against a gap is a rule never
  tested.

**And the codes that are MZ's own and not a plugin call.** 0, 401, 404, 405,
412 and 505 have no `commandNNN` method and are not plugins: the block end, the
line of text, the end of processing, the choice, the end of a branch, the move
route. The engine reads them by position, not by dispatch, and this reader
models them for the same reason. **"It is a number MZ knows" is not the same as
"MZ does it"**, and a 357 shows up in a list of known numbers only because MZ
reserves a slot for plugins.

**The fixture is 537 KB over thirty files**, with no `js/`, no executable, no
image, no audio and no `Tilesets.json` — the reader loads no texture, so a
texture in a fixture is a claim about something nothing reads. **`Skills.json`
is the one file that is not the original**: 104 525 bytes become 1 181, because
**no command on any of the nineteen maps references a skill and the reader
reads none.** Everything else is bytewise identical and the SHA-256 values are
in `project/tests/fixtures/mz_plain/MZ_PLAIN_FIXTURES.md`.

**Test evidence** 6 tests in `project/tests/core/test_mz_plain_fixture.cs`.
**Total 945/945**, validator passed, build 0 errors.

**Mutations** Ten rules, **ten caught, first run, none escaped** — and the way
they were written is the point. **These are claims about data, so the data was
mutated and not the reader**: a 505 turned into a branch, a 401 into a choice, a
101 into something else, a 357 appended to a map, a 355 appended to a map, the
common event list emptied, the common event file deleted, and three rules
against the test's own arithmetic. Every one fell.

**That is the first card in four where nothing escaped**, and the reason is
that the previous three escaped a rule no test had asked from the side it
fails on. A claim about a number is only as good as the test that notices when
the number changes, and a fixture's claim is a claim about a number.

**What this card is for, in one line.** The first fixture says what a reader
must not do; this one says what it can. **1772 of 2432 already run**, and the
660 that do not are the map of the work that is left — led by 505 at 348, 205
at 96, 123 at 42, 213 at 36 and 405 at 36.



### K-131 Walk a character, one step a frame
`DONE` — runtime, P1, depends on K-130

**Why this one, and what it corrected.** K-129's list called `505` the
biggest thing left, at 348. **`command505` does not exist.** `505` is a
nested move-route entry that the editor writes, and a move route reaches the
runtime through **`205 Move Route` — 96 of them**, over fourteen character
ids, of which **60 say `wait` and 36 do not**. The 348 were never event
commands at all.

**The seventeen route codes this game uses, measured over its own
ninety-six routes:** END 96, MOVE_LEFT 74, MOVE_RIGHT 59, MOVE_DOWN 50,
MOVE_UP 45, JUMP 31, CHANGE_SPEED 26, TURN_UP 11, MOVE_BACKWARD 10,
TURN_DOWN 10, TURN_RIGHT 9, TURN_LEFT 7, MOVE_FORWARD 4, WAIT 4,
TRANSPARENT_ON 4, STEP_ANIME_ON 2, STEP_ANIME_OFF 2. **MOVE_LEFT leads and
MOVE_DOWN follows** — the opposite of what "a game mostly walks about"
would guess. MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight diagonal
codes appear **zero** times and are named rather than guessed at.

**Six rules, and every one of them is a place a first reading goes wrong:**

1. **The API is `isMapPassable` and `canPass`, not `isPassable` and
   `checkPassage`.** Those two names come from other RPG Maker engines;
   `checkPassage` has **zero** occurrences in 1.9.1. A reader built from
   memory would have compiled and tested nothing real.
2. **MZ has two coordinates.** `_x`/`_y` is the tile, `_realX`/`_realY` is
   where the character is drawn, and on a successful step the drawing
   position is set to **one tile behind** —
   `this._realX = $gameMap.xWithDirection(this._x, this.reverseDir(d))`. A
   reader with one coordinate snaps, and a snapped character teleports.
3. **`reverseDir` is `10 - d`, not `(d + 4) % 4`.** The first is right for a
   0..3 numbering and wrong for MZ's 2/4/6/8: `reverseDir(2) = 8` is **Up**,
   not Down. A first draft placed every "one tile behind" position **in
   front** of the character, so every character walked away from where it
   was going.
4. **`isStopping` is `!isMoving() && !isJumping()` — two terms, and a first
   draft added a third.** It wrote `... && !Waiting` and **every route with a
   `ROUTE_WAIT` in it ran backwards**, re-issuing one step for ever. A
   character waiting is a character that has arrived.
5. **A refused step still turns.** `moveStraight` turns in both branches, and
   on failure it calls `checkEventTriggerTouchFront`. A character that bumps
   a wall faces the wall, and that facing is what triggers the action
   button.
6. **A route is a queue of single steps, not a batch.**
   `updateRoutineMove` hands a command over only when the character has
   arrived, so **five steps into open floor is five frames**. A reader that
   ran the list in one call would teleport the character five tiles.

**And a product fault with a wider reach than this card.** A 205's second
parameter is a **nested object**, and `MzCommandEntry.From` turns every
parameter into a string — anything that is not a number or a boolean became
`item.Text`, which for an object is `""`. **Every move route in every game
came back empty, and the reader could not have said why.** `MzJson.Write`
now writes a value back out, because **a reader that cannot write a shape
back has already half-lost it.**

**Two more faults, found by the same tests:** `Truth` read a boolean out of
`Text` where the parser puts it in `Boolean`, so **all three flags of all
ninety-six routes came back false** and not one page was ever held by its
route; and `From` read a route's `code` out of `Text`, which is empty for a
number, so **every route code came back 0 — which is END** and all
ninety-six routes did nothing while looking perfectly plausible.

**Test evidence** 7 tests in `project/tests/core/test_mz_move_route.cs`.
**Total 957/957**, validator passed, build 0 errors.

**Mutations** Fourteen rules, thirteen caught in the main run. The one the
run reported as escaped — "a route that is not forced hands out no steps" —
**was not escaped**: an isolated second run killed it, three of seven tests
down, with the tree bytewise unchanged. It is recorded here as
**fourteen of fourteen**, because a number that was not checked is not a
number that was counted.

### K-130 Send the player somewhere, and hold the page until they arrive
`DONE` — runtime, P1, depends on K-124

**Why this one and not the biggest.** The 660 commands that do not run yet
are led by `505` at 348 and `205` at 96, and both are movement — both need
`Game_Character`, a move route decoder and a passability model, which is
three cards before the first of them can be tested. **201 is 33 commands and
needs none of that**: it changes where the player is, not how they got there,
and it is on sixteen of the nineteen maps.

**And it is the first command in this reader that is neither a change nor a
number of frames.** K-125 made a run wait for frames, K-127 for a picture's
movement, and a 201 for **a condition**:

```
Game_Interpreter.prototype.command201 = function(params) {
    if ($gameParty.inBattle() || $gameMessage.isBusy()) { return false; }
    …
    $gamePlayer.reserveTransfer(mapId, x, y, params[4], params[5]);
    this.setWaitMode("transfer");
    return true;
};
```

and `updateWaitMode` answers `waiting = $gamePlayer.isTransferring()`. **A
condition wait has no length** — a caller passing frames cannot end it, and a
reader that counted them would let the page on with the player still on the
old map. `MzWaitMode` is a third shape next to a 230's frames and a 232's
movement, and the engine's own modes are `message`, `transfer`, `scroll`,
`route` and `until`.

**Four rules, each a place a first reading goes wrong:**

1. **A transfer is reserved, not carried out.** `reserveTransfer` writes
   `_transferring = true` and the new map and position and **changes nothing
   the player can see**; `performTransfer` is what moves them. Applying it
   while reading the command would move the player before the commands after
   it had run — the difference between a game that leads the player and one
   that teleports them mid-sentence.
2. **The engine returns false and transfers nobody** in a battle or with a
   message on the screen. That is neither a wait nor a finish: the index
   stays, and the transfer happens in the frame in which the message closes.
   `MzStep.Refused` says exactly that and is not dressed up as either of the
   other two.
3. **The direction is set on the way, not on the reservation**, because
   `performTransfer` is what calls `setDirection`. A player that turned one
   frame early would face a map they are not on yet.
4. **A map this reader has not read is named and the player stays put.**
   `command201` does not check and `$gameMap.setup` fails further on where
   nobody is looking. **Half-applying it is worse than not moving** — the
   caller would see a position and no file behind it.

**And the numbers are this game's: 33 transfers over sixteen maps, all with
the first parameter at zero**, so the place is written out rather than read
from a variable. A reader that always looked in the variables would send
every player in this game to variable four.

**A C# trap this card walked into and measured.** `$"Map{i:03}.json"` with
`i = 1` produces **`Map13.json`**. In an interpolated string `i:03` is read as
a fill character of `0` and a **precision** of `3`, and a whole number with a
precision is padded on the **right**: 1 becomes "13", 2 becomes "23". Every
file was missing and the only thing that said so was the reader's own error
about a file ending mid-value. `ToString("000")` is the right spelling, and the
reason is in the test so the next card does not walk into it again.

**Test evidence** 5 tests in `project/tests/core/test_mz_player_transfer.cs`.
**Total 950/950**, validator passed, build 0 errors.

**Mutations** Nine rules, **nine caught, first run, none escaped.** Each of the
four rules above was broken in the place it actually fails: a reservation that
moves the player, a turn that happens one frame early, a message that no longer
refuses, a run that carries on past a refusal, a condition wait counted down in
frames, a transfer that holds its page for twenty of them, a condition that
never stops being met, a missing map that is carried out anyway, and a place
always read from the variables.

**What is not here.** No map is loaded and no tile is drawn: a transfer is a
position, not a picture of one, and the direction and fade type are kept as
the numbers the game wrote. The other four wait modes need a scrolling map, a
moving character and a plugin callback, and none of them is modellable here.


### K-132 Read a line of text, and every code in it
`DONE` — runtime, P1, depends on K-129

**The biggest thing left in this fixture: 938 lines, and every one carries
exactly one parameter.** But nothing about it is a rendering detail, and
that is the finding: **a line of dialogue is mostly not words.**

**Two passes, two rule sets.** Pass one, `convertEscapeCharacters`, rewrites
in three steps — every backslash becomes the escape character; **two escape
characters put one backslash back**; and the variable, actor, party and
currency codes are filled in, **the variable one in a loop**. Pass two, the
drawing loop, treats **every character below 0x20 as a control character**
and never puts it in the output. A reader that does them in one shows a
different line than the game does.

**Three classes, and only one of them is text:**

- **In the text:** `\V[n]`, `\N[n]`, `\P[n]`, `\G`.
- **Not in the text, and never shown:** `\|`, `^`, `!`, `>`, `<`, `$`.
  **A reader that emitted them would put a `|` in the middle of a
  sentence.**
- **Neither text nor pen, and this reader names them:** `\C[n]`, `\I[n]`,
  `\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`. **A reader with no
  renderer cannot draw them, and it says so rather than dropping them in
  silence.**

**This game's own numbers, measured over the files — and two of my own
measurements were wrong before they were right.**

| | zuerst behauptet | gemessen |
|---|---:|---:|
| Zeilen mit `\C[n]` | 0 | **19** |
| Undrawable insgesamt | 0 | **57** |
| Leere Zeilen | — | **14** |
| Code-Klassen | 2 | **5** |

`\C[3]` 19×, `\C[0]` 19×, `\I[177]` 19×, `\!` 3×, `\|` 3× — **19 Zeilen
mal drei Codes, das sind die 57.** Eine Zeile wartet **dreimal**: `\|.|\|.|\|.`
sind drei Entscheidungen und nicht eine.

**Der Fehler, der zweimal passierte.** Ein Scan dieser Zeilen fand den
Buchstaben `C` 53-mal, `N` 38-mal, `V` 22-mal und `P` 11-mal, und eine erste
Lesart hielt sie für Auszeichnungen. **Es sind Wörter**: „SEND **C**OUT!!",
„\* **N** om\*", „Valuable **V**egetables". **Ein Code ist zuerst ein
Backslash und dann ein Buchstabe** — wer nach einem nackten Großbuchstaben
sucht, liest Englisch. **Genau dieser Fehler ließ mich zuerst „keine Farben"
behaupten, und die echten Dateien sagten neunzehn.** Der Test, der es
bemerkte, las dieselben Dateien und riet nicht.

**Was nicht hierher gehört.** Eine Zeile wird **gelesen und behalten**, nicht
gezeichnet: `MzBranchFacts.Message` hält Wortlaut, Wartungszahl, und alles,
was dieser Leser nicht zeichnen kann. **Kein Textfenster, kein Renderer.**

**Und eine Aussage, die älter ist als diese Karte.** Ein Test aus K-124
behauptete, ein 401 werde *übergangen*, weil die Engine keine Methode dafür
hat — und das stimmt und stimmt weiter. **Der Leser liest es trotzdem**, weil
er nach einer anderen Frage gefragt wird: *was hat das Spiel geschrieben?*
**„Hat die Engine eine Methode" und „was steht in den Daten" sind zwei
Fragen mit zwei Antworten**, und sie zu vermischen bringt entweder ein
laufendes Spiel zum Stehen oder behauptet, ein Spiel habe keinen Text.

**Test evidence** 6 tests in `project/tests/core/test_mz_message.cs`, and one
K-124 test rewritten to say both answers.
**Total 963/963**, validator passed, build 0 errors.

**Mutations** Nine rules. The first run caught seven and reported two
escaped — **and both were a fault in the rules, not in the reader.** One
mutated a code's handling into an equivalent that changed nothing, and one
mutated a list entry that the test did not actually reach. Isolated and
rewritten, **nine of nine**. The second is the better story:

> **Die Liste der Zahlen ohne `commandNNN` war geraten, und sie war falsch.**
> Sie behauptete, `601`, `602` und `603` hätten keine Methode. **Sie haben
> eine** — `command601`, `command602` und `command603` sind drei der 114.
> Und sie behauptete „178 reservierte Nummern", wo die Liste in K-122 in
> Wahrheit **die 114 Methoden** ist. **Neun Zahlen haben keine Methode, und
> alle neun liegen außerhalb dieser 114** — `0`, `401`, `404`, `405`, `412`,
> `505`, `604`, `605`, `657`. Der Test sagt es jetzt ausdrücklich.

**Das ist der vierte Name in vier Karten, der aus dem Gedächtnis kam und in
der Engine nicht existierte** — nach `checkPassage`, `isPassable` und der
`reverseDir`-Form. **Gemessen wird, nicht erinnert.**

### K-133 A 101, and everything it swallows
`DONE` — runtime, P1, depends on K-132

**The first command in this reader that eats other commands.** And that one
fact reorganises K-132: `command101` is

```
if ($gameMessage.isBusy()) { return false; }
$gameMessage.setFaceImage(params[0], params[1]);
$gameMessage.setBackground(params[2]);
$gameMessage.setPositionType(params[3]);
$gameMessage.setSpeakerName(params[4]);
while (this.nextEventCode() === 401) { this._index++; add(…); }
switch (this.nextEventCode()) {
    case 102: this._index++; this.setupChoices(…); break;
    case 103: this._index++; this.setupNumInput(…); break;
    case 104: this._index++; this.setupItemChoice(…); break;
}
this.setWaitMode("message");
return true;
```

**So a line of dialogue is never dispatched.** There is no `command401` to
dispatch it to — `nextEventCode()` looks one ahead and the 101 steps the index
over each line itself. **Every one of this game's 938 lines belongs to a 101
and to nothing else**, and a reader that ran a 401 as a command of its own
would be running 938 commands the engine never runs.

**This game's numbers, measured over the files:** 414 dialogues, one to four
lines each — **118 with one, 130 with two, 104 with three, 62 with four** —
and the total is exactly 938. **Eight are followed by a 102**, six under a
one-line dialogue and two under a two-line one; there is no 103, no 104, no
403 anywhere in nineteen maps. Commands eaten: **112, 134, 106, 62** — 1360
rather than 414 + 938, because the eight choices are inside it.

**Three rules, and a fourth that is only visible in this game.** A dialogue
that is already up is refused — and `isBusy()` is **text or choice or number
or item**, so a 101 behind an unanswered choice is refused as firmly as one
behind a line. Exactly **one** of 102, 103 and 104 is taken, and it is the one
directly after the last line: the `switch` runs once, so a 102 that is not
right there is reached later as a command of its own. **And it always ends in
a wait**, `setWaitMode` being outside the `switch`, so a dialogue with no
choice holds its page all the same.

**268 of the 414 name somebody and 146 name nobody** — Camellia 32 times,
Mary 27, and `???` 45 times, which is the editor's placeholder for a person
not yet named. All 414 have five parameters. **Not one asks for a face**, so
this game has a name box that is filled in and no portrait beside it.

**`102` is not `405`, and that is the fifth name in five cards that had to be
measured.** `ShowChoices` has meant 405 since K-132 — the choices as data —
and the follower was compared against it, so **not one of this game's eight
choices was ever found**: 1352 commands instead of 1360, and a dialogue that
ended on a choice the engine would have taken. **Four runs**, because the
tests that failed were the ones checking a sum.

**`params[0]` is an array, not a bar-separated string.** A first draft wrote
`params[0].split("|")` — the shape an older RPG Maker used — and would have
read one option that reads `["Yes", "No"]`, brackets and comma included, and
compared the cancel number against the wrong length. **And
`cancelType = params[1] < choices.length ? params[1] : -2`**: a cancel number
that is not below the number of choices becomes "no cancel". This game's eight
are all two options with a cancel of 0 or 1, **so the rule never fires in the
real data** — which is why it had to be built by hand.

**`params[1] || 2` is 2, and a written zero is 2 as well** — 0 is falsy in
JavaScript. A 104 with no category and a 104 with `0` both get the whole
party, and a reader that defaulted to 0 would offer the player nothing.

**And the index moves by what was eaten, not by one.** `command101` steps the
index once per line and once for the 102 its switch took, and then
`executeCommand`'s own `this._index++` steps it once more — so a 101 that is
the last thing in a list leaves the index **one past the end**, and no other
command in this reader can, because every other one moves it by one.

**The off-by-one that cost the most.** Three times, in three different files,
an index that was one out was blamed on the nearest thing rather than
measured. The first draft's `nextEventCode(pCommands, i)` with `i` already one
past the 101 **started the read at the second line** — 524 lines instead of
938. The test helper's `k += eaten` was then "fixed" to step one further, on
the strength of a distribution that was one bucket out, and **the numbers got
worse** — 88 and 102 where the files say 118 and 130. **The fault was never
in the test.**

**And a guard with no test.** `ExecuteOne` had a bounds check that a mutation
switched off and every test passed, because `IsRunning` is
`Index < _commands.Count` and the guard was **never asked**. The repair was
not a test for it but **its removal** — the case is handled one level up, in
`Run`, which now checks before it enters its loop and says where the index was.
**A second check that can never fire is a claim a reader will believe and
nobody can prove.**

**Test evidence** 9 tests in `project/tests/core/test_mz_dialogue.cs`, plus
three rewritten in K-124's and K-132's files.
**Total 972/972**, validator passed, build 0 errors.
**Mutations** Twelve rules over four runs. Every escaped rule turned out to be
either a broken rule or a test that could not reach the thing it mutated; two
of them found real product faults — the 102 read from the wrong command, and
a 103/104 read as a list of options.### K-135 The seven command codes these two real games actually use and this reader skips
`DONE` — runtime, P0. All seven are identified, all seven are executed.

**Measured, not estimated.** Every event command in all four pinned RM2K maps
of `rm2k-dragon-destiny` and `easyrpg-testgame`, walked out of the parser's own
value tree, against the interpreter's own constant list: **32 of 778 commands
were skipped.** Seven codes, and **three of them were described wrongly by an
earlier version of this card.**

| code | count | what it is |
|---|---:|---|
| `10110` | 286 | Show Message — implemented |
| `20110` | 276 | message continuation line — implemented |
| `10810` | 30 | Place Hero — implemented |
| `10` | 28 | End — implemented |
| `1009` | 20 | **ChangeBattleCommands** — a previous card called this a message line |
| `11410` | 18 | Wait — implemented |
| `11070` | 14 | Weather Effects — implemented |
| `12010` | 14 | Conditional Branch — implemented |
| `22010` / `22011` | 14 / 14 | Else / End Branch — implemented |
| `10420` / `10610` | 12 / 12 | Change Level / Hero Name — implemented |
| `10220` | 8 | Control Variables — implemented |
| `10210` | 6 | Control Switches — implemented |
| `11040` / `11050` | 4 / 4 | Flash / Shake — implemented |
| `10330` | 4 | Change Party Members — implemented |
| `12330` | 2 | Call Event — implemented |
| `11610` | 2 | **Key Input Proc** — read, from the reference |
| `5001`–`5005` | 2 each | **Open Load Menu, Exit Game, Toggle ATB, Toggle Fullscreen, Video Options** |

## Three corrections this card had to make to itself

**`1009` is not a message continuation line.** An earlier version counted 20
bare `1009` commands with a string after a `10110`, saw MZ's `401` following a
`101`, and concluded the engines share a convention. **They do not.** The
fixture settles it: every `1009` here carries **four integers and an empty
text** — `[1,1,1,1]`, `[1,3,8,1]`, `[1,4,10,0]` — which are exactly
`parameters[0..3]` of `CommandChangeBattleCommands`: actor, class, battle
command id, and whether to add. **A message line carries text and no integers;
these carry neither.** liblcf's `Code::ChangeBattleCommands` is 1009, and EasyRPG
gates it on `IsRPG2k3Commands`.

The wrong fix was pushed as `5a9ca22` and is reverted by this card. **The fix
looked right, the mutations caught it, and the reason it was wrong is that a
pattern that fits two readings is not evidence for either.**

**`5001`–`5005` are menu commands, not move route steps.** They were recorded
as "route steps carried inside a page" without opening the field that would
have shown it. Measured: they sit **directly in the page's command list**,
between a message and a conditional branch — where a menu command sits. The
pages do carry route lists, **60 of them**, and the five codes are in none.

**`11610` is Key Input Proc**, read from
`Game_Interpreter::CommandKeyInputProc`, not guessed. Parameters 5 to 9 mean
**different keys on 2K and 2K3** — shift/down/left/right/up against
numbers/operators/time-variable/timed — and the version picks the column. The
one fixture occurrence is `[1,1,0,0,0,1,1,2,1,0,0,0,0,0]`: a 2K3 game asking
for digits and operators, timed, writing the answer into variable 1 and the
elapsed time into variable 2. **`parameters[7]` is an int naming a variable,
not a bool**, and the source says so in a comment.

## The five menu commands now run — DONE

`5001` and `5005` push a scene and **hold the page**, from the source's
`return false` after `SetRequestedScene`. `5002`, `5003` and `5004` run through
and advance. A scene that is already current is not pushed twice.

**The gate is the whole command, and this reader does not copy the no-op.**
EasyRPG guards all five on `Player::IsRPG2k3ECommands()` and returns `true` on
any other game — a silent no-op, which is a bug that survives every test
because nothing changed. This reader **refuses visibly**: a diagnostic names
the command by its liblcf name, says it is an E command, and says that nothing
opened. `SupportsRpg2k3ECommands` defaults to **false**, because a game that
has not said yes has not said yes.

**And the scene stack no longer starts with an invented scene.** It used to
push `"Menu"` and make it current on reset. `"Menu"` is not an RPG_RT scene
name; it was a fiction that made every scene test pass against it, and it
contradicted the line above it, which asserted the stack was empty. A new game
now starts with nothing open, and an old test was corrected rather than
weakened.

**`FullscreenRequested` is a request, not a state.** The engine asks the display
layer and the display layer may refuse — EasyRPG checks `IsOptionVisible` and
`IsLocked` and logs "not supported on this platform". A boolean claiming to be
the screen state would be a claim this reader cannot keep.

**Test evidence** `test_rm2k_menu_execution.cs` (9 tests, through
`ExecuteFrame`, the real runner), `test_game_simulation_state.cs` corrected.
**1005/1005**, `TestRm2kMenuExecution: 9/9`, `TestGameSimulationState: 20/20`.
**Mutations** Eight effective rules over two runs, **8 of 8 caught**.

## `1009` now runs — DONE

Three actor modes, from EasyRPG's `GetActors`: **0 is the party, 1 is one hero
by id, 2 is the hero named by a variable.** The reference reads
`parameters[0..1]` through it, `parameters[2]` as the command id and
`parameters[3] != 0` as "add" — `CmdSetup` gives the command a minimum width of
four.

**Absent is not empty, and that is the whole state model here.** An actor with
no entry has *the database's commands*, which is the RM2K default.
`GetActorBattleCommands` returns **null** for that case, because the reference's
`GetActor` hands back a null until something changes it and the battle code
checks for exactly that. A reader that stored an empty list would take every
ability away from every actor the moment command 1009 ran.

**Both no-change directions are reported, because they mean opposite things.**
"Add what it already has" is an author's habit; "remove what it does not have"
is usually a mistake worth naming. Adding an existing command and removing a
missing one both change nothing, and both say which happened.

**An actor id of 0 touches nobody and the page carries on.** Hero ids run from
1, so 0 is the one value a game can actually reach that names no actor — a
variable that was never set. The reference logs a warning and returns an empty
actor list. **A reader that refused the whole page would drop the rest of an
event because one id was wrong**, which is how a typo in the editor becomes a
game that stops halfway through a cutscene.

**Test evidence** `test_rm2k_battle_commands.cs`, 11 tests through
`ExecuteFrame`. **1016/1016**, `TestRm2kBattleCommands: 11/11`.
**Mutations** Eight rules over one run, **8 of 8 caught**.

**Two API facts this repo does not make obvious, and both cost a red run.**
`Variables` is **1-based in the event and 0-based in the array**, because
`GetVariable` reads `Variables[pId - 1]` — so writing `Variables[1] = 2` on an
empty array throws. And **one frame is one step**: a test that calls
`ExecuteFrame` three times on a one-command page does not run that command three
times.

## `11610` is wired — DONE

`Rm2kKeyInput` had the whole table and nothing to hold it. It now has a prompt
in `PresentationState` and a command in the interpreter, and **the page holds
while it is open** — which is what makes it a prompt rather than a read.

**It is a second prompt, not a second mode of the first.** 10150 asks for a
number and stores it; 11610 asks for a set of keys and stores a *code*. A digit
is 11 to 20, an operator 21 to 25, the confirm key is 5. Reusing the number
prompt would write a key code into a variable a game expected to hold a digit,
and **nothing in the file says which of the two asked.**

**While it waits, the variable is zero — every frame.** The reference's own
comment says the variable is reset to zero each frame while waiting, and a
reader that only wrote on arrival would leave whatever the game had put there a
moment ago, so a game reading the variable to show "press a key" would show the
old value instead.

**A key the prompt does not allow ends nothing.** The fixture allows digits and
operators and neither the confirm key nor shift, so pressing confirm leaves the
prompt open. A reader that treated any key as an answer would end a prompt on
the first key a player pressed to dismiss it — which is how a calculator
dialog closes before you have typed a digit.

**Reopening the same request is refused.** The reference resets its key state
on every call, so a second 11610 on the same page would drop a keypress that
arrived between the two. Holding on is what makes the prompt a prompt.

**The engine version is read, not assumed**, so the same fourteen integers are
a different command on 2K than on 2K3. A reader that picked one column would
wait for a shift key where the game asked for a digit, and the player would be
stuck.

**Test evidence** `test_rm2k_key_input_wiring.cs`, 9 tests through
`ExecuteFrame` and `PressKeys`. **1025/1025**,
`TestRm2kKeyInputWiring: 9/9`, `TestRm2kKeyInput: 10/10`.
**Mutations** Nine effective rules over two runs, **9 of 9 caught**.

**The key press arrives on an input frame, which is not the interpreter's
step**, so `PressKeys` is a separate entry point and not part of
`ExecuteFrame`. A reader that put the wait inside the dispatch would re-arm the
prompt on every frame it stayed open, and the reference resets its key state on
every call.

**A first draft asserted on prose it had invented****A first draft asserted on prose it had invented** — on the phrase "did not
declare", when the diagnostic said "does not declare" — and the failure was the
test's. **Asserting on prose a test made up makes the test the thing that has
to be right, and it was the wrong one.** The assertions are now on the words
that carry the meaning.
- **`1009` is decoded, not executed.** The battle command list is not a thing
  this reader changes yet.
- **`11610` is read, not wired.** `Rm2kKeyInput.Read` produces the set of keys
  a command accepts and the value each produces. Nothing prompts yet, because
  there is no window to prompt in.

**Test evidence** `test_rm2k_key_input.cs` (10 tests, reference values),
`test_rm2k_menu_commands.cs` (6 tests, fixture values and the correction).
**996/996**.
**Mutations** Nine rules over two runs, **9 of 9 caught** — the range starting
one early, the digit loop, the digit offset, the operator group, the time
variable read as a flag, the engine version column, the mouse order, the
operator offset, and the legacy switch.

### K-022 — Map/player movement and passability simulation

**Acceptance criteria**
- Configure bounded map dimensions and row-major passability data.
- Move only one cardinal tile per call; update facing using RM direction codes 2/4/6/8.
- Reject map bounds, impassable tiles, malformed passability lengths, diagonal moves, and invalid map dimensions without changing position.
- Increment `Steps` only after successful movement; retain bounded diagnostics for blocked/rejected movement.

**Progress evidence (2026-08-24)**
- Added `GameSimulationState.ConfigureMap` and `TryMove`.
- Added regression coverage for successful movement, facing, blocked tiles, map bounds, diagonal rejection, and passability-shape validation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — passed, 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `213/213` tests.
- RM2K-specific chipset passability decoding remains separate: current implementation intentionally does not invent unverified chipset rules.

### K-031 — Character/event sprite renderer and camera

**Acceptance criteria**
- Produce bounded player and map-event sprite descriptors from parsed map data.
- Reject malformed events and coordinates outside map bounds.
- Maintain camera center clamped to map and viewport bounds.
- Keep texture loading and foreign game-code execution outside this data adapter.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/Rm2kSpriteRenderer.cs` and `project/tests/core/test_rm2k_sprite_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `221/221` tests passed.
- Scope boundary: descriptors/camera only; no untrusted asset/script/native execution.

### K-032 — Message/window/picture presentation layer

**Acceptance criteria**
- Store bounded message state and continuation text.
- Store, replace, and erase bounded picture descriptors.
- Allow `EventInterpreter` to publish ShowMessage output into presentation state through explicit dependency injection.
- Reject oversized or malformed presentation data without executing foreign code.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/presentation/PresentationState.cs` and `project/tests/core/test_presentation_state.cs`.
- `EventInterpreter` now optionally receives `PresentationState`; ShowMessage updates it while retaining existing diagnostics behavior.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `225/225` tests passed.
- Scope boundary: no texture loading, external scripts, native plugins, or game executables are invoked.

### K-040 — RTP registry/resolver without bundled proprietary RTP data

**Acceptance criteria**
- Register only explicit user-provided RTP roots; do not bundle, download, or auto-discover proprietary RTP data.
- Resolve assets by engine, generation, dependency name, and bounded relative path in deterministic registration order.
- Reject absolute paths, traversal, NUL bytes, invalid identifiers, missing roots, duplicate profile IDs, and reparse-point escapes.
- Return structured status for no profile, missing asset, invalid path, and successful resolution without opening or executing the asset.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpRegistry.cs` and `project/tests/core/test_rtp_registry.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `254/254` tests passed.
- Scope boundary: K-040 is an in-memory explicit registry only; diagnostics integration and persisted per-game RTP profiles remain K-041.

### K-041 — Missing-asset diagnostics and per-game RTP profile

**Acceptance criteria**
- Represent a bounded per-game RTP profile without copying or embedding RTP data.
- Serialize and deserialize profile metadata through a bounded JSON codec with validation.
- Report required assets as `Available`, `MissingAsset`, `NoMatchingProfile`, or `InvalidPath`.
- Keep diagnostics data-only; no asset opening, parsing, downloading, or execution.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpDiagnostics.cs` and `project/tests/core/test_rtp_diagnostics.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `258/258` tests passed.
- Scope boundary: profile metadata is not yet wired into persisted `GameLibrary` records; that integration remains a follow-up if required by the save/runtime UI.

### K-030 — Godot renderer adapter

**Acceptance criteria**
- Store lower and upper RM2K tile IDs in a deterministic virtual framebuffer.
- Convert bounded parser map output into the framebuffer without executing game code.
- Reject malformed dimensions, layer lengths, non-integer tile IDs, and negative tile IDs.
- Keep Godot rendering APIs out of the parser-facing adapter; actual texture/tile drawing remains a later presentation slice.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/VirtualFramebuffer.cs` and `project/tests/core/test_rm2k_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `217/217` tests passed.
- Scope boundary: this is renderer-neutral framebuffer assembly; no chipset passability inference, texture loading, camera, or native/game-script execution was added.

### K-001 — Validate stabilization changes

**Acceptance criteria**
- `./scripts/validate.sh` runs with Godot 4.7.2 stable.
- Import/syntax validation succeeds.
- C# core/smoke runner passes under Godot .NET.
- Smoke runner passes.
- Any newly found regression gets its own test before the fix is marked complete.

**Failure policy**
Do not remove new regression tests to restore green status. Use the anti-loop policy in `AGENTS.md`.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Import/syntax validation — passed.
- Core suite — `92/92` tests passed.
- Smoke suite — passed.
- Repaired GDScript parser compatibility in `RM2KDatabase` and `VirtualClock`; added database serialization regression coverage and Windows Godot discovery candidates.

### K-002 — Harden core baseline

**Acceptance criteria**
- No known GDScript parse errors in source files reachable by the app/tests.
- Core abstractions have deterministic tests for documented behavior.
- Documentation accurately states current test count/status.
- C# migration is tracked and validated by K-003.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `95/95` tests passed, including the new legacy-decoder suite.
- Removed the unsupported CP932 conversion attempt on Windows by normalizing CP932/SJIS aliases to `SHIFT_JIS`.
- Replaced the GDScript `"\\u0000"` source literal with byte-level NUL detection; the VFS security regression remains covered without parser diagnostics.
- Remaining non-fatal output is limited to intentional invalid-input diagnostics and Godot's `EditorSettings` headless-editor message.

### K-003 — C#/.NET migration

**Acceptance criteria**

- `dotnet build UniversalRPG.csproj` passes with zero errors.
- Godot .NET headless runner instantiates scene scripts and passes all ported tests.
- Superseded source, application, and test `.gd` files are removed.
- Scenes, validation script, and active documentation reference C# paths.

**Validation evidence (2026-08-21)**

- Godot `4.7.2.stable.mono.official.ed1daf0bf` instantiated `tests/CSharpRunner.cs` after PascalCase file renames required by `ScriptPathAttributeGenerator`.
- C# runner passed `128/128` tests with exit code `0`.
- `scripts/validate.sh` now runs .NET restore/build, Godot import, and the C# runner.

### K-004 — Engine plugin foundation and application wiring

**Acceptance criteria**
- Trusted compiled plugin contracts expose metadata, capabilities, probe results, runtime lifecycle, and typed diagnostics.
- Built-in descriptors cover RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection. RM95/RGSS/MV/MZ/Unite remain detection-only; WOLF has an explicitly unencrypted plain-data slice, and RM2K/RM2K3 additionally parse LDB/LMT/LMU data.
- Detection uses bounded read-only folder/ZIP inspection and retains ranked candidates, evidence, ambiguity, malformed-input, and unknown diagnostics.
- Library import/scan persists versioned detection metadata and revalidates persisted selections on relaunch.
- Runtime selection refuses ambiguous, unknown, malformed, detection-only, missing, capability-incompatible, platform-incompatible, and probe-failing candidates without external fallback.
- Godot UI displays plugin/candidate status and structured diagnostics.

**Validation evidence (2026-08-21)**
- `dotnet build UniversalRPG.csproj --no-restore` — passed with `0` warnings and `0` errors after nullable-contract hardening.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `159/159` C# tests including RGSS/WOLF slices.
- Detection never executes imported EXE, DLL, Ruby, JavaScript, shell, or native plugin files; ZIPs are inspected without extraction. RM2K/RM2K3 runtime tests load only validated fixture data and advance the deterministic clock.

### K-010 — Real LCF validation

**Acceptance criteria**
- Add legal/reproducible fixture provenance notes.
- Verify LDB and LMU headers/chunk boundaries on at least two independent fixtures where available.
- Parser must reject truncation, invalid BER, oversized chunks and unreasonable dimensions without crashes or unbounded allocation.
- Unknown fields are retained or reported rather than silently interpreted as known data.

**Validation evidence (2026-08-20)**
- Added pinned, hashed RM2000 and RM2003 LDB/LMU/LMT fixtures from `EasyRPG/TestGame` commit `4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313`; provenance is in `tests/fixtures/easyrpg-testgame/README.md`.
- Real-fixture tests verify both LDBs and both LMUs, exact file sizes, headers, chunk counts, terminator behavior, and reader position at EOF: `5/5` real-fixture tests passed.
- Full core suite: `102/102` tests passed; full `./scripts/validate.sh` passed.
- Repaired valid zero-length RM2003 struct-array sections and added unknown top-level chunk retention coverage.

### K-011 — LMT map tree

**Acceptance criteria**
- Parse `LcfMapTree` container safely.
- Extract map IDs, names, parent relationship and start-position metadata that is verified against fixtures/documentation.
- Detect cycles/invalid parent references defensively.
- Unit tests cover valid, empty, truncated and malicious-size fixtures.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `109/109` tests passed, including real LMT and bounded malformed-input coverage.
- Implemented `parse_map_tree()` with verified LMT field IDs, signed RM2000 map IDs, parent/tree-order validation, cycle detection, and raw unknown-field retention.

### K-012 — Typed LDB sections

**Acceptance criteria**
- Decode sections incrementally into typed data models.
- Every decoded field has a verified LCF field ID/source; no guessed offsets.
- Unknown fields remain preserved for diagnostics.
- Synthetic fixtures and at least one real fixture comparison exist.

**Validation evidence (2026-08-22)**
- `bash scripts/validate.sh` passed with Godot `4.7.2.stable.mono` on Linux; headless C# suite `165/165`.
- Actors section decodes to typed entries with verified liblcf field IDs (`src/generated/lcf/ldb/chunks.h`, `ChunkActor`): strings 0x01/0x02/0x03/0x0F, integers 0x04/0x05/0x07/0x08/0x09/0x0A/0x10; defaults mirror `rpg::Actor` initializers.
- Switches/variables decode as id/name entries (`ChunkSwitch`/`ChunkVariable`: name=0x01); duplicate structure IDs are rejected.
- Unknown actor/entry fields retained per entry; synthetic tests cover defaults, unknown retention, duplicate IDs, missing terminators.
- Real-fixture comparison: typed entry counts equal `section_counts` on both pinned EasyRPG TestGame LDBs.
- Scope note: per agent maintenance rules the remaining array sections were split into successor card K-015; this card is done for actors/switches/variables plus framing already covered earlier.

### K-015 — Remaining typed LDB array sections

**Acceptance criteria**
- Decode skills, items, enemies, troops, terrains, attributes, states, animations, chipsets, classes, and battle commands incrementally using field IDs verified against liblcf `ldb/chunks.h`.
- Nested structures stay data-only; unknown fields remain preserved.
- Synthetic malformed-input fixtures and real-fixture count comparisons exist per section batch.

**Progress evidence (2026-08-22)**
- Implemented the first K-015 batch for skills (`0x0c`), items (`0x0d`), states (`0x12`), and classes (`0x1e`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, scalar values, unknown-field retention, duplicate-safe framing, and section-count parity.
- `dotnet build --no-restore` — passed with `0` warnings and `0` errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `166/166` C# tests and smoke validation.
- Implemented the second K-015 batch for enemies (`0x0e`), terrains (`0x10`), and attributes (`0x11`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, combat/environment scalar values, unknown-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `167/167` C# tests and smoke validation.
- Implemented the third K-015 batch for troops (`0x0f`), animations (`0x13`), and chipsets (`0x14`). Scalar metadata is typed; nested members, frames, and tile arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for presentation metadata, nested-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `168/168` C# tests and smoke validation.
- Implemented the fourth K-015 batch for battle commands (`0x1d`). Scalar metadata uses verified liblcf field IDs; nested command data remains preserved as unknown fields and trailing data is rejected.
- Added synthetic battle-command coverage and extended real-fixture count parity to every typed LDB array section.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `170/170` C# tests and smoke validation.
- K-015 acceptance criteria are complete; K-015 is `DONE`. K-016 is now the active MZ-priority card.

### K-016 — Prioritized RPG Maker MZ detection and bounded metadata inspection

**Acceptance criteria**
- Strengthen MZ detection using the MZ runtime layout and `data/System.json`; MV signatures must not be accepted as MZ.
- Inspect bounded MZ metadata only; never execute `index.html`, `rmmz_*.js`, `plugins.js`, native binaries, or external runtimes.
- Keep MZ detection-only and non-launchable until a separately verified JavaScript runtime exists.
- Add positive, negative, malformed, and oversized metadata regression coverage.
- Update detection/security documentation with the exact supported boundary.

**Progress evidence (2026-08-22)**
- Added MZ-specific validation on top of the shared web detector: `rmmz_core.js`, `rmmz_managers.js`, and bounded `data/System.json` JSON-object validation are required.
- MV remains on the generic `rpg_core.js` path and is not affected by the MZ-only checks.
- Added positive, missing-manager, malformed-JSON, and oversized-metadata fixtures; no JavaScript, HTML, native binary, or external runtime is executed.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `171/171` C# tests and smoke validation.
- Typed bounded MZ metadata extraction and encrypted-asset diagnostics landed; K-016 is `DONE`.

### K-021 — First event-interpreter slice

**Acceptance criteria**
- Interpret message, wait, if/else/endIf, loop/breakLoop commands deterministically without side effects beyond the simulation state.
- Interpret switch, variable, and transfer-player commands against the bounded `GameSimulationState`.
- Malformed or out-of-range payloads produce diagnostics and are skipped safely; no crashes, no unbounded loops.
- Regression coverage for each command family including malformed payloads.

**Progress evidence (2026-08-23)**
- Fixed the Variant cast in `GetCmdParams` (build blocker) and removed the dead `_shouldBreak` field.
- Added dispatch plus bounded executors for `ControlSwitches`, `ControlVariables` (set/add/sub/mul/div/mod with division-by-zero diagnostic), and `TransferPlayer` (pending-transfer state).
- Removed the placeholder move-route case whose opcode literal collided with `ControlSwitches` (`CS0152`).
- Placeholder opcode constants (101–118, 105–107) documented as such; migration is tracked as K-023.
- `bash scripts/validate.sh` — passed; `198/198` C# tests and smoke validation after the K-023 opcode migration (typed EventCommand model).

### K-024 — Repository layout split

**Acceptance criteria**
- Godot project (project.godot, csproj/sln, app/, src/, tests/, assets/, locale/, scenes/, plugins/) lives under `project/`.
- Repo root keeps development elements: docs, notes, `docs/`, `scripts/`, and the pinned Godot runtime under `tools/godot/`.
- `scripts/validate.sh` runs restore/build/import/tests from the new layout unchanged for CI.

**Progress evidence (2026-08-23)**
- Moved project files via `git mv`; `.godot` cache regenerated inside `project/`.
- `validate.sh` now builds and runs Godot with `--path "$ROOT_DIR/project"`; Godot binary discovery still uses root `tools/godot/editors/4.7.2/`.
- Full validation green: `199/199`.

### K-017 — Bounded MZ data-directory metadata inspection

User-directed MZ slice (extends the K-016 line); stays detection/metadata-only.

**Acceptance criteria**
- Decode bounded metadata from `data/Actors.json` and `data/MapInfos.json` via a real JSON parser: entry counts plus the first 32 names, name length capped.
- Per-file size cap with truncation/oversize rejection; malformed or non-array JSON yields a per-file diagnostic instead of failing detection.
- MZ-specific encrypted assets are detected by their real extensions (`.rpgmvp`, `.rpgmvo`, `.rpgmvm`) and reported diagnostically; no decryption, no execution.
- Snapshots without the `rmmz_core.js`/`rmmz_managers.js` runtime signature are refused (MV folders cannot be inspected as MZ).
- Regression coverage for happy path, missing files, malformed JSON, non-array JSON, encrypted assets, and MV-refusal.

**Progress evidence (2026-08-23)**
- Added `MzDataDirectoryResult.Extract(GameInspectionSnapshot)` in `project/src/plugins/BuiltInEnginePlugins.cs`; JSON parsed with Godot's `Json` parser under strict bounds (2048 KiB/file, 9999 actors, 9999 maps).
- Added `TestMzDataDirectory` suite with five tests over synthetic MZ/MV game folders; suite total `205/205`.
- `bash scripts/validate.sh` — passed; `203/203` C# tests and smoke validation.

### K-019 — ConditionalBranch condition evaluation (DONE)

Implements EasyRPG `CommandConditionalBranch` (code 12010) semantics for the two condition types the deterministic core can model.

**Acceptance criteria**
- Type 0 (switch): switch state compared against ON/OFF polarity (`parameters[2] == 0` means "is ON").
- Type 1 (variable): variable vs constant or variable operand with the six CheckOperator comparisons (==, >=, <=, >, <, !=).
- Unsupported types (timer/gold/item/actor) evaluate false with a diagnostic; else path is taken deterministically.
- True path runs then-body and skips else via matching EndBranch; false path jumps to ElseBranch or EndBranch; nesting handled by depth counting, not indent.
- Regression coverage: switch polarity, false-runs-else, variable operators, var-vs-var operand with nested branch, unsupported-type diagnostic.

**Status (audited 2026-08-26) — DONE**
- Implementation is complete in `project/src/rm2k/interpreter/EventInterpreter.cs`; current suite executes the five conditional-branch regression tests.
- Current canonical validation: `All 279 tests passed`.
- Remaining boundary: timer/gold/item/actor conditions outside the modeled state remain diagnostic-only.

### K-018 — Complete MZ database inventory

User-directed MZ slice; extends K-017, stays metadata-only.

**Acceptance criteria**
- Entry counts for present optional database sections (Classes, Skills, Items, Weapons, Armors, Enemies, Troops) under the same bounds; absent sections are omitted silently (trimmed games are normal).
- System.json `switches`/`variables` name-array counts with the bounded cap.
- Physical `data/Map###.json` file count (3-4 digit numeric stems only), capped at 1000.
- Malformed or oversized optional sections produce per-file diagnostics without affecting sibling sections or detection.

**Progress evidence (2026-08-23)**
- Extended `MzDataDirectoryResult` with `SectionCounts`, `SwitchNameCount`, `VariableNameCount`, and `MapFileCount`.
- Added two inventory tests; malformed-JSON engine log lines from `Json.ParseString` on deliberately broken fixtures are expected and asserted via diagnostics.
- `bash scripts/validate.sh` — passed; `205/205` C# tests and smoke validation.

### K-055 — Bounded runtime-owned RM2K JSON save-directory slots

**Acceptance criteria**
- Write and read the existing bounded JSON simulation snapshot through an explicitly supplied save directory and slot name.
- Reject empty/invalid slot names and path traversal without touching files outside the save directory.
- Use a temporary file followed by replacement, clean up temporary files after the operation, and return I/O/validation failures as diagnostics.
- Keep this separate from original RM2K/RM2K3 `LSD` compatibility; do not overwrite original game saves.

**Validation evidence (2026-08-24)**
- Added `TryWriteFile` and `TryReadFile` to `project/src/rm2k/simulation/Rm2kSimulationSaveCodec.cs`.
- Added bounded slot round-trip/traversal regression coverage in `project/tests/core/test_game_simulation_state.cs`.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — passed with 0 warnings and 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `244/244` tests.
### K-050 — Original-format read-only LSD save model

**Acceptance criteria**
- Read original `LcfSaveData` framing from an explicitly supplied save directory and slot.
- Preserve chunk ID, length, offsets, payload bytes, and unknown-chunk count without executing save contents.
- Reject invalid slot paths, traversal, malformed/truncated framing, missing terminators, oversized files, and oversized chunks.
- Keep this reader read-only; original saves are never overwritten and no speculative Gold/party/inventory mapping is claimed.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/parser/rm2k_lsd_save_codec.cs` and `project/tests/core/test_rm2k_lsd_save_model.cs`.
- Synthetic tests cover raw unknown-chunk preservation, BER framing, traversal/absolute-path rejection, size limits, malformed headers, and missing terminators.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `261/261` tests passed.
- Save mutation, UI integration, and field-level semantic mapping remain separate follow-up work; this card does not claim full native gameplay save restoration.

### K-023 — Verified RM2K/2003 command codes

**Acceptance criteria**
- Interpreter command constants match the verified liblcf numeric table (`lcf::rpg::Cmd`).
- Parameter layouts for implemented commands match EasyRPG Player semantics (ControlSwitches 10210 mode 0=ON/1=OFF/2=flip; ControlVars 10220 [target][op][operandType][value]; Teleport 10810 map/x/y; Wait 11410 tenths of a second).
- Commands are consumed from the typed `Rm2kMap.EventCommand` model (code/int parameters/text), matching the parser output.
- Unsupported or malformed payloads produce diagnostics and are skipped safely.
- Regression tests cover each implemented command family plus loop jump-back and break-jump-past behavior.

**Progress evidence (2026-08-23)**
- Verified code table extracted from liblcf `src/generated/lcf/rpg/eventcommand.h`; parameter semantics cross-checked against EasyRPG Player `game_interpreter.cpp` and `game_interpreter_map.cpp` (CommandControlSwitches, CommandControlVariables, CommandTeleport 10810, SetupWait).
- Rewrote `EventInterpreter` on the typed model: message continuation (20110), comment continuation (22410), tenths-based waits with frame clamp, switch flip mode, variable operand type (const/var), bounded loop stack with EndLoop jump-back and BreakLoop jump-past.
- Known limitations documented in code: ShowChoice/InputNumber remain skipped pending presentation/input slices; unsupported commands remain diagnostic-only.
- Current canonical validation: `All 279 tests passed`.

### K-013 — LMU events/pages

**Acceptance criteria**
- Decode event metadata (id, name, x, y) and page metadata (trigger, priority, frequency, list framing).
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.
- Synthetic fixtures cover valid, empty, truncated and oversized payloads.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser.ParseMap` decodes bounded event IDs/names/coordinates, page metadata, page conditions, move-list presence, command-list presence, and data-only command vectors.
- `Rm2kEventCommandDecoder` enforces command, parameter, string, terminator, and trailing-byte bounds; it never executes commands.
- `TestEventInterpreter` and `test_rm2k_parser.cs` cover event/page selection, command-vector framing, malformed input, and condition decoding.
- Current canonical validation: `All 279 tests passed`.
- Remaining limit: complete RM2K field-semantic coverage for every event/page subfield is not claimed.

### K-014 — Preserve unknown LCF fields/chunks

**Acceptance criteria**
- Decode event/page structures as data only.
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser` retains unknown top-level and per-entry fields as raw bounded dictionaries with IDs, payloads, offsets, and lengths.
- `Rm2kEventCommandDecoder` retains command data as typed data objects; unsupported command codes are diagnosed by the native interpreter rather than executed during parsing.
- `Rm2kEngineRuntime.Update()` is covered by a native autorun integration test that proves Clock → Scheduler → EventInterpreter execution; map initialization now creates a bounded `VirtualFramebuffer` through `Rm2kRendererAdapter`, and `Stop()` reset coverage includes clock/presentation/framebuffer cleanup.
- `Rm2kEventScheduler` caps imported map events at 1000 and emits a bounded diagnostic when additional events are skipped; this is regression-tested.
- Regression coverage exists in `test_rm2k_parser.cs`, `test_rm2k_lsd_save_model.cs`, `test_event_interpreter.cs`, and `TestPluginDetection.cs`.
- Current canonical validation: `All 279 tests passed`.

### K-071 — Controller/touch remapping layer

**Status (audited 2026-08-26) — DONE**
- `Rm2kInputMapper` maps keyboard, joypad buttons, and bounded touch zones to engine-neutral actions.
- `Main._UnhandledInput` consumes the mapper for movement, confirmation, choices, and numeric-input confirmation without executing imported scripts.
- Custom key bindings replace defaults for the selected action; released and unbound events are ignored.
- Regression coverage: `TestRm2kInputMapper` (`3/3`); current canonical validation: `All 280 tests passed`.

### K-072 — Reusable RM2K host lifecycle

**Status (2026-08-28) — DONE for bounded lifecycle slice**
- `EnginePluginHost` accepts `Stopped → Start` and disposes the previous stopped runtime exactly once before selecting and creating a fresh runtime.
- `Rm2kEngineRuntime.Stop()` remains a full cleanup boundary; the restart test verifies cleared map/framebuffer state and a fresh clock/scheduler.
- No stopped runtime is re-initialized. The second start follows the normal selection → creation → initialization → start path.
- Regression coverage: `Test_Rm2kRuntimeCanRestartAfterStopWithFreshRuntimeState` in `TestPluginDetection`.
- Fresh canonical validation: `All 280 tests passed`.

### K-073 — Synchronize runtime sprite descriptors after movement

**Status (2026-08-28) — DONE for bounded movement/render-state slice**
- `Main._UnhandledInput` routes RM2K movement through `Rm2kEngineRuntime.TryMove()` instead of mutating `Simulation` directly.
- Successful movement rebuilds bounded player/event sprite descriptors from the current map data; blocked or invalid movement leaves descriptors unchanged.
- Regression coverage: `Test_Rm2kRuntimeMovementSynchronizesPlayerSpriteDescriptor` in `TestPluginDetection`.
- Fresh canonical validation: `All 281 tests passed`.

### K-074 — Fail-closed pending transfer parameters

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- `EventInterpreter` accepts pending transfer requests only for map IDs `1..GameSimulationState.MaxMapId` and nonnegative coordinates.
- Invalid transfer payloads produce one diagnostic and cannot overwrite an existing pending transfer.
- This remains a data-only `PendingTransfer` request; no target map is loaded or executed.
- Regression coverage: `Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState` in `TestEventInterpreter`.
- Fresh canonical validation: `All 282 tests passed`.

### K-075 — Transfer facing direction validation

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- The optional RM2K3 transfer facing parameter is accepted only for directions `2/4/6/8`.
- Transfer validation is atomic: invalid facing values do not alter the facing direction or an existing pending transfer.
- Transfers remain data-only `PendingTransfer` requests; no target map is loaded or executed.
- Regression coverage: `Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-076 — Clear confirmed choice presentation state

**Status (2026-08-28) — DONE for bounded choice lifecycle slice**
- A confirmed `ShowChoice` selection is logged and then clears `PresentationState.ActiveChoice` before the interpreter advances.
- The UI therefore cannot keep displaying or consuming a stale choice after confirmation.
- Regression coverage: `Test_ShowChoicePausesUntilSelection` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-077 — Preserve pending InputNumber state across variable conflicts

**Status (2026-08-29) — DONE for bounded input lifecycle slice**
- `EventInterpreter` pauses an `InputNumber` command when a different variable already owns the pending presentation input.
- The existing pending variable/value remain unchanged; no conflicting variable is created or mutated.
- This does not execute foreign scripts and does not broaden the bounded input model.
- Regression coverage: `Test_InputNumberDoesNotConsumePendingValueForDifferentVariable` in `TestEventInterpreter`.
- Fresh canonical validation: `All 284 tests passed`.

### K-078 — Implement bounded RM2K ChangeItems command

**Status (2026-08-29) — DONE for bounded inventory mutation slice**
- `EventInterpreter` handles verified command `10320` with five parameters: operation, item-ID mode/value, and amount operand mode/value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` subtracts; constant and variable item IDs/amounts are supported.
- Counts are clamped to `0..999999`; malformed parameters, invalid IDs/variables, negative amounts, unsupported operand types, and unsupported operations fail closed with bounded diagnostics.
- Regression coverage: `Test_ChangeItemsAddsConstantItemCount`, `Test_ChangeItemsSubtractsAndReadsVariableOperands`, and `Test_ChangeItemsClampsAndRejectsInvalidOperation` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 44/44`; `All 292 tests passed`; build and `scripts/validate.sh` passed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

### K-079 — Implement bounded RM2K ChangePartyMembers command

**Status (2026-08-29) — DONE for bounded party mutation slice**
- `EventInterpreter` handles verified command `10330` with three parameters: operation, actor-ID mode, and actor-ID value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` removes; actor IDs may be constant or variable.
- Party size is bounded to `GameSimulationState.MaxPartyMembers` (`4`); duplicate additions, absent-actor removal, invalid IDs/variables, malformed parameters, and unsupported operations fail closed with diagnostics.
- Regression coverage: `Test_ChangePartyMembersAddsConstantActor`, `Test_ChangePartyMembersRemovesVariableActor`, and `Test_ChangePartyMembersRejectsDuplicateAndInvalidActor` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 47/47`; `All 296 tests passed`; build and `scripts/validate.sh` passed.
- RGSS/XP/VX/Ace and MV/MZ remain detection/metadata-only; no foreign Ruby or JavaScript is executed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

### K-081 — Real LMU event-page decoding

**Status (2026-08-31) — DONE**

**Problem**
- `ParseStructArray` and `ReadStructFields` only materialized objects/fields when a `pCollectFields` flag was set. Nested `rpg::EventPage` arrays were read with that flag off, so `events[].pages` was always empty for real LMU files: the event interpreter, scheduler, and page-condition paths had never run against real data.
- `EventInterpreter.End` was `0`; liblcf `lcf::rpg::Cmd` defines `END = 10`.
- Page field ids carried unverified fallbacks (`0x09`/`0x08`/`0x06`, plus `0x0b` for the command list) that do not exist in liblcf.
- The nested `EventPageCondition` struct was read as if it were already field-decoded, which threw `KeyNotFoundException` and faulted RM2K runtime initialization.

**Fix**
- Struct arrays and struct fields are always materialized; the collection flag was removed.
- `EventInterpreter.End = 10` (verified liblcf).
- Page ids limited to the verified set: condition `0x02`, move_frequency `0x20`, trigger `0x21`, layer `0x22`, move_route `0x29`, `event_commands_size` `0x33`, `event_commands` `0x34`.
- Nested struct payloads are decoded through `ReadNestedStructFields` before dispatch.
- An undecodable command vector is contained per page (`command_error` + `event_commands_bytes`) instead of failing the whole map.

**Validation evidence (2026-08-31)**
- Real fixtures now decode event pages: RM2000 `Map0001.lmu` 22 pages, RM2003 `Map0001.lmu` 38 pages; RM2000 decodes every command vector without error.
- `event_commands_bytes` equals the declared `event_commands_size` on decoded pages.
- One RM2003 page contains a 5-byte BER value above 31 bits; the page is contained with a diagnostic instead of guessing the encoding.
- Regression coverage: `Test_RealMapEventPagesDecodeCommandCountsMatchingLiblcfSizes`, `Test_Rm2000RealMapPagesDecodeEveryCommandVector`, `Test_LiblcfEndCommandStopsInterpreterWithoutDiagnostic`.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 300 tests passed`, exit `0`.

### K-082 — Event-page trigger ids and undecodable page containment

**Status (2026-08-31) — DONE**

**Problem**
- `Rm2kEventTrigger` used invented values (`Autorun=0, Parallel=1, Action=2, Touch=3`). liblcf `lcf::rpg::EventPage::Trigger` defines `action=0, touched=1, collision=2, auto_start=3, parallel=4`, and EasyRPG Player compares those values directly against decoded page data. With the old enum no real autorun, parallel, or action page could ever match.
- A page whose command vector failed to decode was bridged into the runtime as an empty page and could start as if it were valid.

**Fix**
- `Rm2kEventTrigger` now mirrors liblcf: `Action=0, Touched=1, Collision=2, AutoStart=3, Parallel=4`.
- `Rm2kEngineRuntime` skips pages carrying a non-empty `command_error` and records a diagnostic instead of running them.

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_TriggerValuesMatchVerifiedLiblcfEventPageTrigger`, `Test_EventPageSelectorIgnoresOtherTriggerKinds`, `Test_RealMapPageTriggersUseLiblcfEventPageTriggerValues`, `Test_Rm2kRuntimeExecutesRealFixtureActionPages`.
- `Test_Rm2kRuntimeExecutesRealFixtureActionPages` starts the RM2K runtime on the pinned fixture, triggers a real action page, advances 20 frames, and requires interpreter diagnostics — the first test that proves real fixture commands execute end to end.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 304 tests passed`, exit `0`.
- Cross-checked against EasyRPG Player `Game_Event::AreConditionsMet`: switch A and switch B both require ON, RM2000 uses `variable >= value` while RM2K3 uses the six compare operators, and timers compare with `secs > limit`. Existing page-condition code already matches, so no change was made.

### K-083 — ControlSwitches/ControlVariables parameter layout

**Status (2026-08-31) — DONE**

**Problem**
- Both commands read `parameters[0]` as the first id. EasyRPG stores the lvalue form in `parameters[0]` (`Game_Interpreter_Shared::TargetEvalMode`), with `parameters[1]` as the start id and `parameters[2]` as the range end. Real RM2K/2003 payloads therefore decoded as start id `0` and were always rejected with `invalid range 0-…`, so no real switch or variable command ever executed.
- Verified widths from `Game_Interpreter::ExecuteCommand`: `ControlSwitches` 4 parameters, `ControlVars` 7 parameters, `ChangeLevel` 6, `ConditionalBranch` 6.

**Fix**
- `ControlSwitches` reads `[targetMode, start, end, mode]`; `ControlVars` reads `[targetMode, start, end, operation, operandMode, operand, bitfield]`.
- `TargetEvalSingle` collapses the range end to the start id, matching `DecodeTargetEvaluationMode`.
- Patch-only target modes (`IndirectSingle`, `IndirectRange`, `Expression`) stay fail-closed with diagnostics.
- Added the verified `VarOperandVariableIndirect` mode (`v[v[x]]`).

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_ControlSwitchesAndVarsUseVerifiedParameterLayout`, `Test_ControlVarsRangeTargetWritesEveryVariableInRange`, `Test_ControlVarsIndirectOperandReadsVariableOfVariable`, `Test_ControlSwitchesAndVarsRejectPatchOnlyTargetModes`, `Test_RealFixtureCommandsUseVerifiedParameterWidths`.
- `Test_RealFixtureCommandsUseVerifiedParameterWidths` asserts the pinned fixtures satisfy the verified minimum widths; `Test_Rm2kRuntimeExecutesRealFixtureActionPages` now also fails if a real control command is rejected as an invalid range or patch-only target mode.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 309 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeLevel` (10420), `ChangeHeroName` (10610), screen effects (11040/11050/11070), `CallEvent` (12330), battle commands (1009), and Maniac codes present in the fixtures.

### K-084 — Verified actor-stat, screen-effect and event-control commands

**Status (2026-08-31) — DONE**

Implemented from the verified `lcf::rpg::Cmd` table and EasyRPG `ExecuteCommand` dispatch widths:

- `ChangeLevel` (10420) and `ChangeExp` (10410), 6 parameters `[actorMode, actorId, operation, operandMode, operand, showMessage]`, using `GetActors` modes (party / hero / variable-held hero) and `OperateValue` add/subtract. Levels clamp to `1..99`, exp to `0..999999`.
- `ChangeHeroName` (10610), 1 parameter; the command string is the new name, bounded to 64 characters.
- `EndEventProcessing` (12310) ends the current frame instead of the whole interpreter.
- `FlashScreen` (11040, 6 parameters), `ShakeScreen` (11050, 4 parameters) and `WeatherEffects` (11070, 2 parameters) drive new bounded screen-effect state on `PresentationState`; the runtime ticks effects with elapsed simulation frames, and the wait flag reuses the RM2K tenths-to-frames conversion. Weather strength clamps to 2 and unknown RM2K types fold to 0.
- `CallEvent` (12330, 3 parameters) pushes a bounded nested frame for map events through an injected resolver; nested `END` returns to the caller, recursion is capped at `MaxScriptRecursion`, and common-event targets stay diagnostic-only because the LDB common-event section is not decoded yet.
- `ChangeEventLocation` (10860, 4 parameters) and `EraseEvent` (12320) mutate event position and activity through scheduler-backed hooks, so `TriggerAt` observes the new position.
- `Rm2kMap.EventPage.Trigger` comment corrected to the liblcf enum.

**Validation evidence (2026-08-31)**
- New regression coverage in `TestEventInterpreter`: level/exp party-wide, clamping, variable operand, variable-held actor id, fail-closed modes, hero name, `EndEventProcessing`, flash/shake/weather bounds and waits, presentation-absent path, nested call with return, unsupported call targets, recursion bound, event location with variable coordinates, and erase-event activation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 330 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeBattleCommands` (1009), menu/Maniac codes (5001-5005, 11610), `MoveEvent` (11330, needs move routes), `ChangeMapTileset` (11710), and battle-dependent commands.

### K-085 — RPG Maker MV data-directory and metadata parity

**Status (2026-08-31) — DONE for the bounded data-only slice**

**Scope boundary**
- MV/MZ gameplay needs a JavaScript engine. That stays blocked: card K-090 is `BACKLOG` behind the RM2K playable milestone, and repository policy forbids executing imported JavaScript. This card is data-only.

**Problem**
- Only MZ had a bounded `data/` inventory; MV was limited to `gameTitle` from `System.json`.

**Fix**
- The inventory reader is now shared: `WebDataDirectoryResult` with `MzDataDirectoryResult` and `MvDataDirectoryResult` wrappers. Each wrapper requires its own runtime signature, so an MV snapshot is never read as MZ and the reverse is equally refused.
- `MvMetadataResult` now also reports `versionId`, `locale`, `currencyUnit`, `startMapId`, `startX`, `startY`, and bounded `partyMembers` (actor ids `1..50000`, capped at four). MV stores its version as `versionId` where MZ uses `systemVersion`; both keys are read from the top-level object only, so nested keys cannot shadow them.
- No JavaScript, HTML, or native file is executed or evaluated; only bounded JSON text is parsed.

**Validation evidence (2026-08-31)**
- New `TestMvDataDirectory` suite: inventory extraction, database section counts, missing files, malformed and non-array JSON, malformed optional sections with siblings kept, encrypted assets, verified System.json keys, and mutual signature refusal in both directions.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 340 tests passed`, exit `0`.
- RPG Maker AX was evaluated and deliberately left out: no verifiable file signature is documented publicly, and the repository forbids inventing format details. It stays unsupported rather than guessed.

### K-086 — RM2K chipset passability decoding

**Status (2026-09-26) — DONE: verified chipset passability drives real movement**

This card closed the blocker that was repeated in every slice note ("chipset passability remains fail-closed").

**Verified constants (EasyRPG Player `src/map_data.h` + `Game_Map` passability helpers)**
- Passability bits: `Down=0x01`, `Left=0x02`, `Right=0x04`, `Up=0x08`, `Above=0x10`, `Wall=0x20`, `Counter=0x40`.
- Tile blocks: `BLOCK_A=0` (stride 1000, index 0), `BLOCK_B=2000` (1000, 2), `BLOCK_C=3000` (50, 3), `BLOCK_D=4000` (50, 6), `BLOCK_E=5000` (1, 18), `BLOCK_F=10000` (1, 162); block ends 2000/3000/3150/4600/5144/10144; `NUM_LOWER_TILES=162`, `NUM_UPPER_TILES=144`.
- `GetPassableMask` maps a step to `Right`/`Left`/`Down`/`Up`.
- `IsPassableTile` decides from the upper layer first and only falls through to the lower layer when the upper entry carries `Above`; the lower lookup honours the `Wall` exception for autotiles 20-23, 33-37, 42, 43, 45, 46.

**Implemented**
- `project/src/rm2k/simulation/Rm2kChipset.cs` — verified `ChipIdToIndex`/`IndexToChipId`, `DirectionBit`, `IsPassableLowerTile`, `IsPassableTile`, `BuildDirectionMasks`. Unknown tile ids, missing tables, and mismatched layer lengths fail closed.
- `GameSimulationState` keeps `PassabilityMasks` as the authoritative per-tile direction mask, adds `IsPassableInDirection`, and keeps the old `IEnumerable<bool>` `ConfigureMap` contract by mapping passable to all four directions. `TryMove` now checks the direction bit.
- `Rm2kEngineRuntime` reads `passable_data_lower`/`passable_data_upper` from the LDB chipset section, verifies the 162/144 lengths, builds masks from the LMU `lower_layer`/`upper_layer`, and configures the simulation; the stale fail-closed diagnostics are gone.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 350 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the verified bit values, block constants, chip-id round trips, direction mapping, upper-then-lower resolution, the wall autotole exception, and the fail-closed cases.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` drives real RM2000/RM2003 maps: each fixture yields walkable and impassable tiles, and a real step onto a walkable tile succeeds while a step into an impassable tile is refused.
- `TestPluginDetection` asserts the runtime decoded non-empty masks from the real fixture and no longer reports missing passability.


### K-087 — RM2K autotile animation and event counters

**Status (2026-09-26) — autotile animation DONE; counter values not implementable from verified data**

Follow-up to K-086. Same evidence discipline: nothing below was inferred from memory.

**Autotile animation (implemented)**
- Verified in EasyRPG Player `src/tilemap_layer.cpp` (Draw), `src/game_map.cpp` (SetChipset, GetAnimationType/Speed) and liblcf `src/generated/lcf/ldb/chunks.h` (`ChunkChipset`).
- liblcf field ids: `animation_type = 0x0B`, `animation_speed = 0x0C`; the project's scalar field contract matches upstream.
- `Game_Map::GetAnimationSpeed()` returns `animation_speed != 0 ? 12 : 24`, so `animation_speed` is only an animated/not flag, **not** a frame rate and **not** an on/off switch: even the zero default keeps AB autotiles cycling, just at half speed.
- AB autotiles (blocks A1/A2/B, `id < BLOCK_C`): `step = frames / speed`, then cyclic (`animation_type != 0`) `% 3`, reciprocating (`animation_type == 0`) `% 4` with `3 → 1`, i.e. 0,1,2,1.
- Block C: `step = (frames / 6) % 4` on a fixed cycle that ignores both chipset animation settings.
- Blocks D, E and F never animate.
- `frames` is the RPG_RT frame counter (`Game_System::GetFrameCounter`), which the simulation already ticks as `FrameCount`.
- Implemented as `Rm2kChipset.AnimationSpeed/ReciprocatingStep/CyclicStep/CBlockStep/ChipAnimationStep`, exposed through `GameSimulationState.ChipsetAnimationType`, `ChipsetAnimationSpeed` and `GetChipAnimationStep`.
- The runtime now selects the chipset entry by the LMU `chipset_id` instead of assuming the first chipset, which is what the Player does (`SetChipset(map->chipset_id)`). The parser stores the passability tables on the matching typed chipset entry and keeps the section-level keys for the first entry so the existing contract still holds.

**Event counters (deliberately not implemented)**
- `Game_Map::IsCounter` is verified: the upper layer must hold `>= BLOCK_F`, the id runs through the `upper_tiles` substitution table, and the entry's `Counter` bit (`0x40`) marks it. The Player uses it only to look for an action trigger across at most 3 counter tiles in a row.
- The counter *value* mechanism (plates and steps that close again) is **not** implementable: liblcf `master` has no per-map counter/chip-data array on `lcf::rpg::Map` or `lcf::rpg::MapInfo`, so there is no verified data source to decode. Implementing it would mean inventing a format, which is exactly what K-086 forbids.
- The substitution tables (`map_info.lower_tiles`/`upper_tiles`, identity via `std::iota` in `Game_Map::Setup`) come from `lcf::rpg::MapInfo`. The current resolution treats them as identity, which matches the verified default, and the tables themselves remain a separate card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 356 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the speed mapping, the reciprocating 0,1,2,1 cycle, the cyclic three-frame cycle, the block C fixed cycle, the per-block dispatch, the D-F static blocks, and the frame-counter-driven step through `GameSimulationState`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` proves both fixtures resolve their `chipset_id` to a real chipset entry with the expected animation defaults.
- `TestPluginDetection` asserts the runtime carries chipset animation values and starts on autotile frame zero.

**Lesson recorded**
- Passability and animation data belong to a single chipset entry. Reading the first entry "because it is the map's chipset" was an unverified assumption; the LMU `chipset_id` is the verified selector.


### K-088 — RM2K tile substitution tables

**Status (2026-09-26) — DONE: verified substitution applied; the tables themselves come from save files**

**Card correction**
The card originally said "LMT map-info tile substitution tables". That was wrong. liblcf `lcf::rpg::MapInfo` has no substitution fields and liblcf `ChunkMapInfo` (LMT) has no `lower_tiles`/`upper_tiles` field ids. The tables live in `lcf::rpg::SaveMapInfo` (`lower_tiles`, `upper_tiles`, 144 entries each, identity by default), so they are save-file data, not map-tree data.

**Verified resolution order (EasyRPG Player `src/game_map.cpp`)**
- `Setup` fills both tables with `std::iota` (identity), which matches the liblcf `SaveMapInfo` default, so identity is the correct behaviour for a freshly loaded map.
- Upper layer, `IsPassableTile` and `IsCounter`: `tile_id = upper_layer[i] - BLOCK_F` and then `tile_id = map_info.upper_tiles[tile_id]`, so the substitution happens **after** reducing the raw id and **before** the flag lookup.
- Lower block E, `IsPassableLowerTile`: `tile_id = tile_raw_id - BLOCK_E; tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX`. Only block E is substituted; blocks A/B/C/D are used as-is.
- `GetChipId` (terrain lookup) converts the raw id to a chip index first and only then remaps indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)`.

**Implemented**
- New `Rm2kTileSubstitution` with the verified identity default, `SubstituteLower`, `SubstituteUpper` and `ResolveChipIndex` (the `GetChipId` order). Tables whose length or entries do not fit the 144-entry range fall back to identity instead of clamping, and requests outside the range return -1 so they fail closed.
- `Rm2kChipset.IsPassableLowerTile`, `IsPassableTile` and `BuildDirectionMasks` take an optional `Rm2kTileSubstitution`; the existing overloads keep identity behaviour, so the runtime is unchanged until save data provides a table.

**Not implemented, on purpose**
- Reading the tables out of a save file. That belongs with the open save-game work (K-050 family: "semantic field mapping, save mutation"), not with the chipset parser.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 360 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the identity default, that substitution changes passability lookups, that only the verified ranges are remapped, that malformed tables fall back to identity, that out-of-range requests fail closed, and the `GetChipId` index-first order.

### K-089 — RM2K per-map terrain tags

**Status (2026-09-26) — DONE: terrain table decoded and resolved per map tile**

**Verified (EasyRPG Player `src/game_map.cpp` `GetTerrainTag` / `GetChipId`, liblcf `ChunkChipset`)**
- `terrain_data = 0x03`, an array of 162 **shorts** (324 bytes), `int16_t` in `rpg::Chipset`, defaulting to all ones.
- RPG_RT omits an all-ones table, and the Player returns terrain 1 when the table is empty, so an absent table is normal data and not a decode failure.
- The **lower** layer alone decides the terrain; the upper layer is never consulted.
- Resolution order: raw id -> `ChipIdToIndex` -> substitution for indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)` -> `terrain_data[chip_index]`.
- Out-of-bounds coordinates use chip index 0, i.e. the terrain of the first lower tile; on looping maps the coordinate wraps first.

**Implemented**
- Parser decodes `terrain_data` (0x03) with a bounded 162 x 2 byte length check, per chipset entry plus the section-level key for the first entry, and reports an unexpected length as `terrain_data_unverified_length` with its offset.
- `Rm2kTileSubstitution.GetTerrainTag` implements the verified lookup, falling back to `Rm2kChipset.DefaultTerrainTag` (1) when the table is absent or does not cover the chip index, instead of reading out of bounds the way the Player's `assert` allows.
- `GameSimulationState.TerrainData`, `LowerLayer`, `TileSubstitution` and `GetTerrainTagAt` expose it to the runtime and to event conditions.
- `Rm2kEngineRuntime` reads the terrain table of the chipset the map actually uses.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 363 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the chip-index mapping, the substitution effect on terrain, the absent and short table fallbacks, and the out-of-bounds behaviour through `GetTerrainTagAt`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies the real RM2000 chipset table has 162 entries with valid tag ids and that every lower tile of the map resolves a tag.
- `TestPluginDetection` asserts the real runtime map resolves a valid terrain tag, including out of bounds.

### K-091 — Counter tile action-trigger propagation

**Status (2026-09-26) — DONE: verified propagation over at most three counter tiles**

**Verified (EasyRPG Player `src/game_player.cpp`, `src/game_map.cpp`, liblcf)**
- `Game_Map::IsCounter`: the upper layer must hold a tile `>= BLOCK_F`, the id runs through `upper_tiles`, and the resolved entry must carry `Passable::Counter` (`0x40`).
- `Game_Map::XwithDirection` / `YwithDirection`: the tile in front, with the looping map wrap applied.
- `Game_Player::CheckEventTriggerThere` (action): check the tile in front; then while no action event was found and at most three times, if the current tile is a counter tile, step one tile further in the facing direction and check again. RPG_RT allows a maximum of three counter tiles, so four in a row stop the search.
- Layer rules differ by position and are easy to get backwards: events **in front** of the player must have `Layers_same` (`1`), events **on the player's own tile** must **not** have it.
- The walking case evaluates only `Trigger_touched` and `Trigger_collision` on the tile in front and does **not** walk counter tiles.
- liblcf `LMU_Reader::ChunkEventPage`: `trigger = 0x21`, `layer = 0x22`; `rpg::EventPage::Layers` is `below = 0`, `same = 1`, `above = 2`.

**Defect found and fixed**
- The LMU field `0x22` was decoded and stored under the name `priority`. liblcf has no `priority` field: `0x22` is `layer`. The name was wrong and the value was unusable, so the layer rules could not be implemented. It is now `layer`, carried into `Rm2kMap.EventPage.Layer`.

**Implemented**
- `Rm2kChipset.IsCounterTile` (upper id, substitution, counter flag, fail closed).
- `GameSimulationState.UpperLayer`, `UpperPassability`, `IsCounterAt`, `FrontTile` and the looping `Wrap` helper.
- `Rm2kEventScheduler.TriggerActionFacing`, `TriggerActionHere` and `TriggerTouchOrCollisionFacing` implement the three verified cases, with `Rm2kTriggerLayerRule` for the explicit same/not-same decision and `MaxCounterTiles = 3`.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 369 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the reachable event behind a three tile chain, the stop behind a four tile chain, the layer rules for front versus own tile, and that touch/collision do not walk counter tiles.
- `test_rm2k_chipset.cs` pins `IsCounterTile` including the substitution and the fail-closed cases, and `FrontTile` including the map wrap.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies both real fixtures expose a valid `layer` and `trigger` on every event page.

### K-092 — Player input drives movement and triggers

**Status (2026-09-26) — DONE: verified turn order applied to real input; host wiring still missing**

**Card correction**
The card said a successful step triggers touched/collision "on the tile in front". That is wrong. In the Player, `Game_Player::UpdateNextMovementAction` calls `CheckEventTriggerThere` (tile in front, layer same) only when the step was **blocked**, while `Game_Player::UpdateMovement` calls `CheckEventTriggerHere` (own tile, layer **not** same) when the player comes to a stop after a **successful** step. The layer rules are therefore opposite in the two cases.

**Verified ordering**
- `Game_Player::UpdateNextMovementAction`: `Move(move_dir)`, and if the player is still stopping, evaluate touched/collision on the tile in front.
- If stopping and the decision key is pressed, the vehicle toggle runs first and the action event check only runs when no vehicle was toggled.
- `Game_Player::CheckActionEvent`: touched/collision in front, then action on the own tile, then action in front continuing over at most three counter tiles; the result is the union.
- A running event page blocks movement (`Game_Map::IsRunning`).
- `Game_Map::XwithDirection`/`YwithDirection` wrap on looping maps.

**Implemented**
- `Rm2kEventScheduler.TriggerTouchOrCollisionHere` and the complete `CheckActionEvent`, complementing the K-091 entry points.
- New `Rm2kPlayerTurn`, a Godot-free class that applies one resolved input action in the verified order: refuse while paused, in a menu, or while an event page runs; a direction attempts `TryMove` and then picks the `Here` or `There` trigger path; `Confirm` runs `CheckActionEvent`.
- `Rm2kEngineRuntime.SubmitInput(Rm2kInputAction)` exposes the turn to the host and refuses input unless the runtime is running.

**Deliberate simplification**
- Vehicles and the airship are not implemented, so the vehicle toggle in front of `CheckActionEvent` cannot change anything and the action check always runs. This is recorded rather than faked.

**Still missing**
- Nothing feeds `SubmitInput` yet: `Rm2kInputMapper` is still unreferenced by the host scene, so the game cannot receive real input. That is the next card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 375 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the successful-step `Here` path, the blocked-step `There` path, the confirm path, the empty confirm, the pause and running-event guards, and that `None`/`Menu`/`Cancel` are not map steps.
- `TestPluginDetection` feeds `MoveRight` and `Confirm` into the real runtime map and asserts the position contract.

### K-093 — Godot host input routes through the verified turn order

**Status (2026-09-26) — DONE: host now uses the verified turn order**

**Card correction**
The card claimed "`Rm2kInputMapper` is unreferenced and nothing forwards input". That was wrong. `Main.cs` already constructs the mapper, configures the touch viewport in `_Ready`, and handles `_UnhandledInput` with the verified key edge rules (pressed, not echo). The real defect was narrower and worse: the host **had** an input path, but it bypassed everything K-091 and K-092 verified.

**What the host did before**
- `Confirm` computed a facing target with its own `GetFacingTarget` helper, which has no looping map wrap, then called `EventScheduler.TriggerAt(x, y, Action)`: no layer rule, no touched/collision in front, no counter tile walk.
- A direction called `Rm2kEngineRuntime.TryMove` and then `TriggerAt(mapX, mapY, Touched)` on success only: no layer rule, and the blocked-step in-front path did not exist at all.
- So the host was reachable but wrong in exactly the ways the verified Player logic is not.

**Fixed**
- The map input branch now calls `Rm2kEngineRuntime.SubmitInput(action)`, so the host inherits the verified `Here` versus `There` choice, the layer rules, the counter tile walk, the pause and running-event guards, and the map wrap in `FrontTile`.
- `GetFacingTarget` is deleted; the unwrapped direction helper no longer exists anywhere.
- Input is marked handled when the runtime consumed it, and also when a map input was consumed without moving, such as a blocked step, so it cannot fall through to the UI. `None`, `Menu` and `Cancel` stay unhandled as before.
- The message, choice and numeric-input priority order in `_UnhandledInput` is unchanged; that is the `Game_Message::IsMessageActive` gate and must stay ahead of map input.
- Removed the now unused `UniversalRPG.Rm2k.Simulation` import.

**Not covered by tests**
- The host wiring itself is a Node override and cannot be exercised headlessly without the scene. The runtime side is regression tested in `TestPluginDetection`; the `Main.cs` branch was verified by reading the resulting code path, not by an automated test.

### K-094 — Vehicles for the action-event order
`DONE` — runtime, P0, unblocked K-114

**The card's title was half the diagnosis.** `Rm2kPlayerTurn.Apply` carried the
comment *"This runtime has no vehicles, so nothing can be toggled and the action
event check always runs"* — and `Rm2kDecisionTurn.Run` sat next to it,
implemented, mutation checked, and **never called**. The vehicles were loaded and
drawn; they were never driven and never boarded. `GameSimulationState` had
**zero** vehicle wiring and the runtime kept its own `_vehicles` list.

**Implemented**
- `GameSimulationState.Vehicles` and `.Boarding`, both cleared in `Reset()`
- `Rm2kPlayerTurn.Apply` calls `Rm2kDecisionTurn.Run`, and a vehicle that takes
  the turn suppresses the action event check — a boat moored beside a sign has
  to be boardable, and the sign is on the tile the player faces
- `CanEmbark` / `CanDisembark` from the passability mask, `IsVehicleStopping`
  for the airship, `OppositeBit` for the way back

**Three real product faults the suite found**

**`TileInFront` spoke the wrong direction order.** The player speaks 2/4/6/8;
`DirectionDelta` expects 0–3. **A `8` yields `(0, 0)`** — the character's own
tile. Every boarding test "passed" without anything moving, and a player facing
up was handed a disembark onto the water they were standing on. The bridge
`LiblcfFromFacingDirection` already existed, and its own comment warns that
mixing the two silently turns a right step into a left one.

**`PassDown` is `0x01` and `PassUp` is `0x08`.** A first draft had them swapped
and wrote `0x08` for "down".

**A K-114 test held the wrong order in place.** It checked `TileInFront` with
0/1/2/3, and so agreed with itself: five assertions, every one consistent with
the same misreading.

**And a fixture that lied about itself.** `SetPassability(..., pAllowUp,
pAllowDown)` was named as walkable directions and wired `pAllowUp` to
`PassDown` — the opposite. Two tests then asserted the wrong polarity and failed
against correct code. **A fixture whose names lie about its own bits is worse
than no fixture**, because the failure points at the reader.

**Test evidence** 8 tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 rewritten in
`test_rm2k_vehicle_boarding.cs`.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`.
**Mutations** Ten rules over six runs, **9 of 10 caught**. The tenth is a harness
fault, not a semantic gap: the first runner used `$TMPDIR/m_<path>` as its
backup, which fails on the `/`, so the mutations ran **without a restore** and
the following rules tested a cumulatively broken file. `git checkout --` then
discarded the **unstaged** slice; it was rebuilt and staged immediately.

**What this does not claim:** a vehicle's own move route, hero-directed vehicle
movement, and vehicle background music. K-114 lists those.

### K-095 — Chipset source rectangles for blocks C, E and F

**Status (2026-09-26) — DONE: verified chipset rectangles resolved, no pixels yet**

**Why this slice**
`VirtualFramebuffer` deliberately stores tile ids only, so nothing is drawn yet. The verified `Rm2kChipset` work from K-086 to K-089 produced the tile-id resolution the renderer needs. This slice resolves a tile id to the chipset rectangle it is blitted from, which is the last step before real blitting, and it is fully verifiable without any graphics dependency.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`, `Draw`)**
- Block C is blitted straight from the chipset: `col = 3 + (id - BLOCK_C) / 50`, `row = 4 + animation_step_c`. `BLOCK_C_TILES` is 3, so block C occupies columns 3 to 5 and rows 4 to 7.
- Block E applies the substitution table first (`id = substitutions[tile.ID - BLOCK_E]`), then `col = 12 + id % 6, row = id / 6` for `id < 96` and `col = 18 + (id - 96) % 6, row = (id - 96) / 6` afterwards.
- Block F applies the substitution table first (`id = substitutions[tile.ID - BLOCK_F]`), then `col = 18 + id % 6, row = 8 + id / 6` for `id < 48` and `col = 24 + (id - 48) % 6, row = (id - 48) / 6` afterwards.
- Blocks A, B and D are **not** blitted from the chipset: they come from the generated caches `autotiles_ab_screen` and `autotiles_d_screen`, so this slice refuses them instead of guessing.
- The formulas require at least 30 columns and 16 rows of 16 pixel tiles, which follows from the largest computed column (24 + 5) and row ((143 - 48) / 6).

**Range detail worth keeping**
The Player guards block C with `id >= BLOCK_C && id < BLOCK_D`, not with the end of block C, so ids between 3150 and 3999 still resolve. Its passability lookup uses the same range. `Rm2kChipsetSource` keeps that on purpose so the renderer and the simulation always resolve a tile id identically; it is documented so it is not "fixed" later.

**Implemented**
- New `Rm2kChipsetSource.TryResolve` with `ChipsetRect`, plus an identity-substitution overload and `Columns`/`Rows` bounds. Unknown ids, the autotile cache blocks and unresolvable substitutions return false so callers fail closed.

**Not implemented**
- No bitmap decoding and no blitting. The pinned fixtures contain no `Chipset.png`, so there is nothing real to decode yet.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 381 tests passed`, exit `0`.
- `test_rm2k_chipset_source.cs` pins the three formulas including the `< BLOCK_D` range detail, the substitution effect on blocks E and F, the block C cycle over several chipset settings, the fail-closed set, and that every resolved rectangle stays inside the 30 by 16 chipset grid.

### K-096 — Block D autotile quarters

**Status (2026-09-26) — DONE: block D resolves to four verified chipset quarters**

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `BlockA_Subtiles_IDS[47][2][2]` (int8, `-1` means the B block supplies the quarter) and `BlockD_Subtiles_IDS[50][2][2][2]` (uint8) are static tables in the Player source, ordered top-left, top-right, bottom-left, bottom-right.
- `GenerateAutotileD`: `block = (ID - 4000) / 50`, `variant = ID - 4000 - block * 50`, refusing `block >= 12 || variant >= 50 || block < 0 || variant < 0`. Block origin is `(block % 2) * 3, 8 + (block / 2) * 4` for `block < 4` and `6 + (block % 2) * 3, ((block - 4) / 2) * 4` afterwards. Each quarter is the block origin plus its table offset.
- The Player composes autotiles from four 16x16 quarters, so a tile id resolves to four chipset rectangles, not one.

**Transcription discipline**
- Both tables were extracted mechanically from the Player source with a script instead of being typed by hand: 188 values for block A and 400 for block D, with the count, value range and first/last rows checked against the source before any C# was written.
- The same script generated the block D anchor expectations in the test, so the test cannot drift from the table it verifies.
- Tables are stored flat: four values per block A variant, eight per block D variant.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockD` returns the four `ChipsetRect` quarters for a block D tile id and refuses out-of-range ids.
- `Rm2kAutotileQuarters.TryGetBlockAQuarters` exposes the block A variant table so the block A/B composition can use it and so the transcription can be regression tested.

**Not implemented**
- The block A/B composition itself (the quarter selection combines the A and B bit patterns with the animation step) and any bitmap decoding or blitting.
- Blocks A, B and D still do not resolve through `Rm2kChipsetSource`; only the block D quarters are available, and the composition is what turns them into a drawable tile.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 387 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins ten block D anchor rows against the Player table, the block origin for all twelve blocks, all 600 block D ids resolving with every quarter inside the chipset, the range refusals, and the block A table anchors and value range.
- The first run caught a real defect: the block D variant offset used `variant * 4` while a variant spans eight values, so every variant after the first read the wrong row.

### K-097 — Block A/B autotile composition

**Status (2026-09-26) — DONE: all lower layer blocks resolve to verified quarters**

**Defect found in K-096 while reading the source for this card**
`GenerateAutotiles` packs the quarter pairs into a hash with the last quarter on top and unpacks `x` first, so the **second** value of a pair is the chipset column and the **first** value is the row. K-096 had assumed the opposite. The block D rectangle code and its anchor expectations were corrected. The K-096 test had not caught this because it verified the table, not the axis order, so the axis is now documented in the code and pinned by the A/B column range tests.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `GenerateAutotileAB`: `block = ID / 1000`, `b_subtile = (ID - block * 1000) / 50`, `a_subtile = ID - block * 1000 - b_subtile * 50`, refusing `b_subtile >= TILE_SIZE` and `a_subtile >= 47`. `#define TILE_SIZE 16` is in `src/options.h`, so the B pattern is a four bit value.
- Three passes in this order: quarters the A table leaves to the B block with `t = (b_subtile >> (j * 2 + i)) & 1` and `t ^= 3` for block 2; quarters the A table supplies with the row `animID + (block == 1 ? 3 : 0)`; and the A/B combination pass, which runs last and therefore wins.
- The Player packs the quarters into a hash and de-duplicates them; that only affects the layout of the generated cache, not the quarter values, so it is not reproduced.
- `t ^= 3` swaps the two bits of the value, so a cleared bit 0 becomes 3 and a set bit 0 becomes 2. All four B variants, chipset columns 4 to 7, are reachable, and no more.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockAB` reproduces the three passes and returns the four quarters, refusing out-of-range blocks, B subtiles, A variants and animation steps.
- With K-095 and K-096, every lower layer block now resolves: A, B and D through the autotile tables, C, E and F straight from the chipset.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 394 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins the B bit pattern per quarter, the animation step as the row, the block 2 flip in both directions, the A table supplying a quarter with the column range split, the block 1 row shift, the combination pass overriding the A table, the reachable B column set, and the range refusals.
- The test also asserts that A quarters stay in columns 0 to 3 and B quarters in columns 4 to 7, which would fail if the pair axes were transposed again.

### K-098 — Chipset bitmap decoding and blitting

**Status (2026-09-26) — DONE: the real pinned chipset decodes and blits**

**Blocker resolved by research, not by invention**
The card said this was blocked on a real `Chipset.png`. The pinned fixtures had none, but the fixtures come from the public `EasyRPG/TestGame` repository, which ships the chipset images. The right chipset was determined, not guessed: the map's `chipset_id` is `1` and that LDB entry's `chipset_name` is `World`, so `TestGame-2000/ChipSet/World.png` from the **same pinned commit** is the real chipset for the pinned LDB.

The fixture README previously stated that no image is imported. That was true while the project only parsed LCF data; it is now updated with the reason, the pinned source URL and the SHA-256, and the image is a passive, never executed asset.

**Verified (EasyRPG Player)**
- `src/cache.cpp`, the `Material::Chipset` spec: directory `ChipSet`, loaded with `transparent` true, and 480 by 256 pixels.
- `src/image_png.cpp`, `ReadPalettedData`: for a paletted PNG every colour is opaque except **palette index 0**, which becomes alpha 0.
- The real fixture is an 8 bit paletted, non interlaced PNG of exactly 480 by 256 pixels, which independently confirms the `30 * 16` tile grid derived from the chipset formulas in K-095.

**Implemented**
- `Rm2kChipsetBitmap.TryParse`/`TryLoad`: bounded paletted PNG decoding, keeping the **palette index** rather than only the converted colour so the transparency rule survives. It refuses a wrong signature, a non 8 bit depth, a non paletted colour type, interlacing, oversized dimensions, a missing or oversized palette, missing image data and unknown scanline filters instead of reinterpreting them.
- `TryBlitTile` and `TryBlitRectangle` implement the verified transparency rule: index 0 is left untouched so a background shows through, every other index is painted opaque.
- `Rm2kPixelBuffer`, a Godot free RGBA buffer, so the blit stays deterministic and testable like the rest of the rendering code.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 400 tests passed`, exit `0`.
- `test_rm2k_chipset_bitmap.cs` decodes the real fixture, checks its size against the derived tile grid, verifies that index 0 is present and that every used index is covered by the palette, checks that a blitted pixel is opaque exactly when its index is not 0, refuses rectangles outside the image, and pins the malformed input cases.
- The strongest check: every chipset rectangle that K-095 through K-097 can produce for the real chipset, over 1000 of them, is blittable inside the real 480 by 256 image.

### K-099 — Compose a full map frame

**Status (2026-09-26) — DONE: the real pinned map renders from the real pinned chipset**

**Verified (EasyRPG Player)**
- `CreateTileCacheAt` assigns each tile a sublayer. An upper layer tile goes into the above sublayer when its substituted entry carries `Above`; a lower layer tile goes into the above sublayer when its resolved chip index carries `Wall` or `Above`. The chip index ranges are the same as the passability lookup: block E through the lower substitution table plus `BLOCK_E_INDEX`, block D and block C by their stride, everything else the block number.
- The two sublayers are two drawables: `lower_layer(this, Priority_TilesetBelow + TileBelow + layer)` and `upper_layer(this, Priority_TilesetAbove + TileAbove + layer)`, with `TileBelow = 0`, `TileAbove = 100`, `Priority_TilesetBelow = 20`, `Priority_TilesetAbove = 50` and `Priority_Player = 40`. Drawables are sorted ascending, so the effective order is lower layer, then the hero, then upper layer. That is why a wall tile covers the hero.
- Without passability data the Player keeps the default `TileBelow`, which is the fail-closed case.

**Defect fixed**
`Rm2kChipsetSource.TryResolve` returned false for block E and F when no substitution was supplied, even though `Game_Map::Setup` fills both tables with `std::iota`. An absent table is the identity, so those lookups now fall back to it instead of making every block E and F tile unresolvable. The caller no longer has to build a substitution just to get the default.

**Implemented**
- `Rm2kTileZOrder` with the verified `ResolveChipIndex`, `LowerLayerSubLayer` and `UpperLayerSubLayer`.
- `Rm2kMapFrameRenderer` with `RenderLower` and `RenderUpper`, drawing each layer's below sublayer before its above sublayer and blitting every resolved chipset rectangle. The hero is deliberately not drawn: it belongs between the two calls, which is what exposes a wall tile.
- `Rm2kMapLayers` and `Rm2kChipsetTables` as the input, both Godot free.

**What the pinned fixture actually contains, now measured rather than assumed**
The real RM2000 testgame map is 20 by 15 tiles, its lower layer uses only block D and E, and its upper layer only block F. Its upper tiles are fully transparent in the real chipset, so drawing them changes nothing, and because it contains no A, B or C tile it has no animated autotile. Three of my initial expectations were wrong for that reason and were replaced by measurements of the fixture. The upper layer draw path and the animation are verified with synthetic maps instead, and the real map test now documents its own shape so those facts cannot silently change.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 408 tests passed`, exit `0`.
- `test_rm2k_map_frame.cs` pins the sublayer rules for `Wall`, `Above` and both, the fail-closed case without passability, the chip index resolution including the block E substitution, the real map rendering with its measured shape, a visible upper tile changing the tile area, a fully transparent upper tile painting nothing, animation across frame 0 and 24 for the blocks that paint, and block D not animating.

### K-100 — Runtime renders the map and the host shows it

**Status (2026-09-26) — DONE: a real RM2K game renders pixels, with a golden image baseline**

**What this delivers**
A real RM2K game directory now produces a real map image. The chain is end to end verified: the LDB chipset tables, the LMU layers, the chip id resolution, the autotile quarter tables, the real chipset PNG and the verified draw order, all against the pinned fixtures.

**Verified (EasyRPG Player)**
- `src/cache.cpp`: the chipset is read from the `ChipSet` directory, and `Cache::Chipset` goes through the standard `LoadBitmap` path, so the image name is `<chipset_name>.png` inside `ChipSet`.
- `Game_Map::GetChipsetName` supplies the name from the database, and the map selects the chipset by its own `chipset_id`, which is already implemented in K-087.

**Implemented**
- `Rm2kEngineRuntime` reads `chipset_name`, resolves `<root>/ChipSet/<name>.png`, decodes it, checks the 480 by 256 size and renders the map into `RenderedMap` with `ChipsetImage` and `RenderDiagnostic` exposed. A missing, malformed or wrongly sized image is reported and leaves the runtime **running**, because the Player treats the chipset as an asset and the simulation does not depend on it. The tile id framebuffer keeps working next to the pixels.
- `Rm2kMapPreview` uploads the pixels once per change and draws them scaled with the nearest neighbour filter, with the player marker on top and the render diagnostic when there is no image. It falls back to the tile id view when no image exists.
- `Main.cs` forwards `RenderedMap` and `RenderDiagnostic` and reports the pixel size.

**Golden image**
`rm2000/rendered/Map0001.png` is this project's own output, not upstream, and is pinned as a regression baseline with its SHA-256. The rendering test compares every byte, so a change in the chipset resolution, the autotile tables, the transparency rule or the draw order now fails the suite instead of quietly producing a different picture.

**Measured facts about the pinned map, not assumptions**
20 by 15 tiles, 320 by 240 pixels, lower layer of block D and E only, upper layer of block F only, those upper tiles fully transparent in the real chipset, 13 distinct colours, and every pixel covered because the room's floor and wall tiles are solid. Three of my expectations were wrong for those reasons and were replaced with measurements. Transparency is therefore verified per tile, and the animated blocks with synthetic maps.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 412 tests passed`, exit `0`.
- `test_rm2k_runtime_rendering.cs` renders a real game directory built from the pinned fixtures, compares it against the golden image byte for byte, checks the frame size and the colour count, and verifies that a missing or malformed chipset image is reported while the runtime keeps running and the tile id framebuffer stays available. Stopping clears the rendered map.

### K-101 — Charset geometry and character frames

**Status (2026-09-26) — DONE: verified charset geometry with a real charset fixture**

**Verified (EasyRPG Player)**
- `src/sprite_character.cpp`, `GetCharacterRect`: the cell is `24 * (TILE_SIZE / 16) * 3` by `32 * (TILE_SIZE / 16) * 4`, which is **72 by 128** with `TILE_SIZE = 16`, placed at `(index % 4, index / 4)`. Each cell holds a 3 by 4 frame grid, so one frame is **24 by 32**.
- `Sprite_Character::Draw`: `row = character->GetFacing()` and `frame = character->GetAnimFrame()`, with anything from `Frame_middle2` replaced by `Frame_middle`. liblcf `rpg::EventPage::Frame` is `left = 0, middle = 1, right = 2, middle2 = 3`.
- `src/game_character.cpp`, `UpdateFacing`: for the four cardinal directions the facing is set to the direction itself, so liblcf `rpg::EventPage::Direction` `up = 0, right = 1, down = 2, left = 3` is the sprite row directly. Diagonal directions have their own rule, which RM2K characters never use.
- The sprite offsets are `SetOx(chara_width / 2)` and `SetOy(chara_height)`, which centres the frame on the tile and puts its feet on the tile bottom.
- `src/cache.cpp` loads charset material as transparent, like the chipset.

**Fixture**
`rm2000/CharSet/Chara1.png` from the same pinned commit, added the same way as the chipset. It independently confirms the geometry: 288 by 384 pixels is exactly four 72 pixel cells across and three 128 pixel cells down, giving twelve characters.

**Refactor**
The paletted PNG decoder was generalised to `Rm2kIndexedImage`, with `Rm2kChipsetBitmap` as the chipset specific wrapper that owns the 480 by 256 contract. Charset, chipset and later picture material share one decoder and one transparency rule.

**Implemented**
- `Rm2kCharset` with the verified cell and frame constants, `FacingToRow` for the project's facing values (2 down, 4 left, 6 right, 8 up), `ClampFrame` matching the Player's `middle2` clamp, `TryGetCell`, `TryGetFrameRect` and `TryDrawCharacter` which places the feet on the tile bottom and clips at the frame edge.
- `Rm2kIndexedImage` is the shared decoder; `Rm2kChipsetBitmap` keeps the chipset size check.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 417 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the cell and frame geometry against the real image, the `(index % 4, index / 4)` cell split, the frame clamping, the facing conversion, and the drawing including the clipping behaviour: a character at tile (0, 0) is cut off above the tile bottom, and one fully outside the frame paints nothing without throwing.

### K-102 — Event sprite fields and per-stage placement

**Status (2026-09-26) — DONE: verified sprite placement, not yet wired into the runtime**

**Verified (EasyRPG Player and liblcf)**
- `src/sprite_character.cpp`: `character_name = character->GetSpriteName()` and `character_index = character->GetSpriteIndex()`, and the charset is requested from the `CharSet` directory, like the chipset from `ChipSet`.
- liblcf `LMU_Reader::ChunkEventPage`: `character_name = 0x15`, `character_index = 0x16`, `character_direction = 0x17`. The direction is an `rpg::EventPage::Direction` value.
- The drawable priorities split the characters into three stages: `Priority_EventsBelow = 30` between the map layers, `Priority_Player = 40` shared with "same as hero" events, and `Priority_EventsAbove = 60` after `Priority_TilesetAbove = 50`.

**Parsing gap found and fixed**
The parser declared `character_name` and `character_index` for actors (chunk `0x03`/`0x04`) but never for event pages, even though the ids `0x15`/`0x16` are verified in liblcf. Event sprite data was therefore not available at all. The parser now decodes `character_name`, `character_index` and `character_direction` for every event page, and a missing name yields an empty string, which is what a page without a character graphic means.

**Implemented**
- `Rm2kCharacterSprite` with the verified `StageForLayer` for the three page layers and `FacingFromLiblcfDirection` for the direction, plus a `Skipped` flag so a caller can report a character that could not be drawn.
- `Rm2kMapFrameRenderer.RenderSprites` draws one stage at a time, so the caller can interleave the stages with the two map layers in the verified order, and reports how many were drawn. A character index beyond the charset capacity is skipped and flagged, never taken from an arbitrary cell.
- `Rm2kMapFrameRenderer` can now be created without a chipset for sprite only passes; tile drawing then does nothing instead of throwing.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 419 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the layer to stage mapping, the liblcf direction to facing conversion and the fact that the two conversions are inverse, and checks that each stage draws only its own characters, that an out of range index is skipped and flagged, and that the below stage and the hero stage paint different characters.

### K-103 — Hero and events in the runtime frame

**Status (2026-09-26) — DONE: the hero and the events are drawn in the verified order**

**Verified (EasyRPG Player)**
- `src/game_player.cpp`, `Game_Player::ResetGraphic`: `auto* actor = Main_Data::game_party->GetActor(0)` and, when it is null, `SetSpriteGraphic("", 0)`. With an actor it calls `SetSpriteGraphic(ToString(actor->GetSpriteName()), actor->GetSpriteIndex())`. The hero therefore has no page of its own: it is the **first** party member.
- `src/game_actor.h`: `GetSpriteName()` returns the runtime override `data.sprite_name` when it is non empty and otherwise falls back to `dbActor->character_name`; `GetSpriteIndex()` uses `data.sprite_id` in the same case. `SetSprite` clears the override when the requested graphic equals the database values, so a fresh game always draws the LDB graphic and no override has to be invented.
- `src/game_party.cpp`, `Game_Party::SetupNewGame`: `data.party = lcf::Data::system.party`, so the leading actor id is the first entry of the LDB system party list.
- The stage split and the per-stage draw call already existed from K-102; this card only wires it up.

**Parser gap found and fixed**
`LoadCurrentMapEvents` decoded the trigger, the layer and the conditions but never copied `character_name`, `character_index` or `character_direction` into the page, although K-102 had verified the liblcf ids `0x15`/`0x16`/`0x17`. The event sprites were therefore unreachable at runtime. The page now carries all three, and the liblcf direction is converted to this project's facing instead of being stored raw.

**LDB system chunk was not decoded at all**
The hero resolution needs the starting party, and `system` was only a raw chunk. Verified against liblcf `src/generated/lcf/ldb/chunks.h` `struct ChunkSystem` and `src/generated/ldb_system.cpp`: the party list is the size/data pair `party_size 0x15` plus `party 0x16`, and the three vehicle graphics are the scalars `boat_name 0x0b`, `ship_name 0x0c`, `airship_name 0x0d` with `boat_index 0x0e`, `ship_index 0x0f`, `airship_index 0x10`. `DecodeLdbSystem` types those and keeps every other field in `unknown_fields` with its count and framing. A database without a system chunk yields liblcf's empty defaults instead of failing, because that is what a fresh empty database means.

**Defects found while implementing**
- The first `system` decoder overwrote the seeded defaults, so a database without the chunk lost every default key. It now only reports the fields the chunk actually carries and the caller merges them.
- An LCF string field is the raw encoded text; the first test built a length prefix, which the shared decoder does not strip. The test was corrected, not the decoder.
- The declared party size can exceed the stored data, so the list is clamped to `min(declared, data.Length / 2)` and never reads past the chunk. `MaxSystemArrayEntries = 4096` bounds a malformed size field.

**Render order defect found**
`RenderCurrentMap` ran before `LoadCurrentMapEvents`, so the first frame was rendered with an empty event list. The events are now loaded before the framebuffer and the first render. Without this the whole card was silently inert: the suite stayed green and the golden image matched, because nothing was drawn at all.

**Test fixture defect found**
`CopyRealGame` copied `ChipSet` but never `CharSet`, so no character could ever be drawn and the failure hid behind a missing-file diagnostic. The charset is now part of the copied fixture. The constant also needed the `FixtureRoot` prefix, because `GlobalizePath` does not resolve a bare relative path.

**Measured, not assumed**
The pinned LDB has **no starting party** (`party` decodes to an empty list), so the verified `GetActor(0) == null` path applies and this particular game draws no hero graphic. That is the correct result, and the test asserts it instead of inventing a hero. The frame therefore contains the chipset plus the event characters: 85 colours instead of the 13 the chipset-only frame of K-099 produced, and 20 character figures, confirmed by inspecting the rendered image.

**Implemented**
- `Rm2kHeroSprite.FromActor` with the verified fallback and the `CharSet/<name>.png` file name.
- `Rm2kEngineRuntime` builds the frame in the verified order: lower layer, below events, hero plus same-layer events, upper layer, above events. A charset that is missing or undecodable is reported and skips only its characters.
- The page graphic fields are filled in `LoadCurrentMapEvents`, and a skipped character is reported instead of drawing from an arbitrary cell.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 425 tests passed`, exit `0`.
- `test_rm2k_parser.cs` pins the system chunk party list, the three vehicle names and indices, the unknown field count, the empty defaults without a system chunk, and the clamp to the stored data.
- `test_rm2k_runtime_rendering.cs` pins the character drawing (more colours than the chipset-only frame, no missing-charset diagnostic), the hero resolution from the first party actor including the null case, and that a missing charset is reported while the map still renders.
- The golden image is regenerated and re-pinned with SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`; the byte-for-byte comparison is green again.

**Not implemented**
- No movement animation: the hero and the events are drawn with the static middle frame, so the walk cycle is not exercised.
- The hero is not re-rendered after the player moves; the frame is produced once during initialization.
- The `frame_name` and the transparency level of an actor are decoded but not applied.

### K-104 — Re-render the frame when the player moves

**Status (2026-09-26) — DONE: the frame follows a move, tiles stay cached**

**Verified (EasyRPG Player)**
- `src/scene_map.cpp`, `Scene_Map::vUpdate` → `UpdateStage1` → `UpdateGraphics()` once per frame, and `PreUpdate`/`PreUpdateForegroundEvents` call it again. So the update is per frame, not per input.
- `src/spriteset_map.cpp`, `Spriteset_Map::Update`: the tilemap only receives `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))` and `SetOy(...)`, i.e. a scroll offset. The tile layers are **static sprites that are not re-rastered on movement**; only `character_sprite->Update()` and the tone change per frame.
- Note the class is spelled `Spriteset_Map`, not `SpriteSet_Map`. An earlier probe with the wrong casing silently matched nothing, which is why the order of verification matters.

**Design consequence**
The map is rastered once into two cached layer buffers, and only the characters are re-composited. This matches the Player instead of re-rastering a whole map per step, and it keeps simulation and presentation separate: `RecomposeFrame` runs inside `Update` on a simulation frame boundary, never per rendered frame, so a higher display frame rate cannot change the simulation.

**Composition order** (`RecomposeFrame`)
1. copy of the cached lower tile layer,
2. below-layer event characters,
3. hero and same-layer event characters,
4. the cached upper tile layer laid over them,
5. above-layer event characters.

**Three defects found and fixed while implementing**
- `PaintOver` first copied every byte, including alpha 0, so the upper layer erased the lower layer and the whole floor. It now keeps the destination pixel where the source is transparent, which is the same rule the verified chipset blit uses. The K-099 golden test caught this immediately.
- The upper layer was originally rastered into the same buffer as the lower layer, so it carried the lower layer with it and covered every character. It is now rastered into its own buffer.
- The character pass drew onto an **empty** buffer instead of the lower layer, which dropped the floor entirely (`opaque=6513` instead of `76800`).

**Defect in the existing API found by the new test**
`RenderSprites` threw on a null map although it never reads the map: a character is placed by its own tile coordinates and the Player's character sprites are independent of the tilemap sprite. The parameter is now nullable and documented, so a sprite pass does not have to invent a map.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 427 tests passed`, exit `0`.
- `TestRm2kRuntimeRendering 9/9`, `TestRm2kCharset 7/7`.
- The strongest check: the rendered frame is **byte identical** to the K-103 golden image, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`, with the same 85 colours and 76800 opaque pixels. The refactor therefore changes no output, which is what makes the caching safe to keep.
- New tests pin that moving a character to another tile changes the composited frame, and that `PaintOver` respects transparency and refuses a mismatched buffer.

**Not implemented**
- The frame is still a full-map buffer, not a camera viewport. The Player scrolls by offsetting the two layer sprites; this runtime still renders the whole map, which is correct but not yet efficient.
- Movement is still a single discrete step: there is no walk animation, so the hero jumps from tile to tile and the frame is invalidated per completed step.
- Events have no move routes, so only the hero position changes between frames.

### K-105 — Camera viewport instead of a full-map frame

**Status (2026-09-26) — DONE: the camera is applied and the suite proves it**

**What is implemented**
- `project/src/rm2k/rendering/Rm2kMapCamera.cs`: `DefaultPanX`/`DefaultPanY` (9 and 7 screen tiles at 320x240), `PositionX`/`PositionY`, `OffsetPixelsX`/`OffsetPixelsY` and `PositiveModulo`, all as pure calculations.
- `Rm2kPixelBuffer.TryCopyRegion` reads a window out of a cached layer and **refuses** a region that does not fit instead of clipping it.
- The runtime frame is screen sized (320x240) instead of `width * 16` by `height * 16`. `RecomposeFrame` cuts the cached layers at `ResolveCameraOffsetX`/`ResolveCameraOffsetY` and gives the characters the same offsets through `Rm2kCharacterSprite.PixelOffsetX`/`PixelOffsetY`.
- `AppliedCameraOffsetX`/`AppliedCameraOffsetY` expose what the runtime actually applied, so a test can assert the wiring and not only the arithmetic.

**Verified (EasyRPG Player)**
- `Game_Map::GetDisplayX` = `map_info.position_x + shake * 16`, so the stored position is already the scroll offset. Screen shake is deliberately not implemented: it is presentation state and would couple a cosmetic effect to the deterministic core.
- `Game_Map::SetPositionX`/`SetPositionY` clamp to `[0, tiles * SCREEN_TILE_SIZE - screen_width]` or apply `Utils::PositiveModulo` when the map loops. The source says `std::clamp` must not be used, because for a map smaller than the screen the lower bound exceeds the upper bound.
- `Game_Player::GetDefaultPanX` = `ceil(screen_width / TILE_SIZE / 2) - 1) * SCREEN_TILE_SIZE`.
- `Spriteset_Map::Update` does `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))`, which is a **division** by 16. This is `OffsetPixelsX`. Multiplying by 16 instead was the first attempt and put the viewport 16 times past the end of the map; the bound proves the direction: the last column of a 40 tile map is 7680 screen tiles, and 7680 / 16 = 480, which is inside the 640 pixel map, while 7680 * 16 = 122880 is not.

**Two upstream unit mixes, both reproduced and pinned**
- `SetPositionX` counts the map extent in screen tiles but the screen in pixels, so a map exactly one screen wide in pixels still has a positive bound (`20 * 256 - 320 = 4800`) and still scrolls.
- The same mix means the reachable offset on a 40 tile map is 480 pixels, which is 160 more than a 320 pixel window needs. The excess is the Player's black border, and `CopyViewport` leaves it unpainted rather than reading past the layer. A test that demanded `offset + screen <= map` was wrong and was corrected.

**Unblock condition met**
A synthetic 40 by 30 map (640 by 480 pixels) is written by the test with the verified LMU field ids (`0x01` chipset, `0x02` width, `0x03` height, `0x47` lower, `0x48` upper, `0x51` events) and event characters at known tiles. Mutation A, forcing the applied camera offset to zero, now **fails** the suite, which it did not before this card. The tile arithmetic in `Rm2kMapCamera` was already unit tested; what was missing was a test that reaches the runtime wiring.

**Still not implemented, and why it matters**
- The scrolled frame is **not** compared pixel by pixel. `GameSimulationState.TileSubstitution` is never populated, so a synthetic map's floor does not render, and the frame contains only the characters. Comparing pixels would compare an empty floor, so the test asserts the applied offsets instead. Populating the LDB tile substitution is the prerequisite for the pixel comparison and is the next card.
- Loop horizontal/vertical flags are exposed on the camera API but never set by the runtime, because the map loop fields are not decoded.
- No screen shake and no configurable resolution: `RenderProfile` is a scaling policy, not a screen size. The renderer uses `Rm2kMapCamera.DefaultScreenWidth/Height` as named constants.
- The above-layer compositing is only covered where the upper layer is transparent in the fixture, so its opacity rule is untested in the runtime.

### K-106 — Block E passability offset and the two-sided movement check

**Status (2026-09-26) — DONE: the premise was wrong, and chasing it found two real bugs**

**The premise in this card was false, and that is the first result**
The card assumed `GameSimulationState.TileSubstitution` was never populated because the LDB chipset carries `lower_substitution_ids` and `upper_substitution_ids` that had to be decoded. Verified EasyRPG `Game_Map::Setup` says otherwise:

```
std::iota(map_info.lower_tiles.begin(), map_info.lower_tiles.end(), 0);
std::iota(map_info.upper_tiles.begin(), map_info.upper_tiles.end(), 0);
```

Both tables start as the identity and are only ever changed by `SubstituteDown`/`SubstituteUp`, which exist for the tile substitution event commands. There is no LDB field to decode, so `Rm2kTileSubstitution`'s identity fallback was already correct, and `Validate` checking both tables against 144 entries is also correct because `BlockEEnd = BlockE + 144` and `BlockFEnd = BlockF + 144`. Nothing needed populating. The card was rewritten once that was proven, and `chunks.h` was checked for `lower_tiles`/`upper_tiles` to confirm they are not LDB chunk fields at all.

**Bug 1 — block E passability read the wrong entry**
`Rm2kChipset.IsPassableLowerTile` applied `+ BLOCK_E_INDEX` only when a substitution object was present. Verified `Game_Map::IsPassableLowerTile` applies it unconditionally, because a missing table is the identity, not a skipped offset:

```cpp
tile_id = tile_raw_id - BLOCK_E;
tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX;
```

Without the offset a block E tile read passability entry 0 instead of entry 18, so every block E tile in every game inherited the first autotile's passability. In the pinned EasyRPG TestGame that turned 20 tile ids into whatever entry 0 said. Regression: `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`, which pins the contract with and without a table and proves block C is unaffected. RED was `TestRm2kChipset: 23/24`.

**Bug 2 — movement only checked the target tile**
`GameSimulationState.TryMove` checked `IsPassableInDirection(targetX, targetY, directionBit)` and nothing else. Verified `Game_Map::IsPassable` computes two masks:

```cpp
const int bit_from = GetPassableMask(from_x, from_y, to_x, to_y);
const int bit_to   = GetPassableMask(to_x, to_y, from_x, from_y);
```

`bit_from` is the direction leaving the current tile and is tested against the current tile. Only testing the target let a player walk out of an impassable tile and made one-way tiles wrong in both directions. `TryMove` now checks the current tile with `directionBit` and the target with the reverse bit. Regression: the blocked-tile case in `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` now uses a two tile strip with a real blocked id from the decoded table.

**A test that was pinning the bug**
`Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` asserted `blockedTiles > 0` on the map itself. That only held because of bug 1, which pushed block E tiles onto entry 0. With the bug fixed the EasyRPG map legitimately resolves to passable entries only, so the assertion was rewritten to ask the chipset table for a blocked id and to build the blocked strip from real decoded data. Asserting "this specific map has a wall" was a false claim about a fixture, not a requirement.

**A fourth real defect found on the way: the fixture's upper layer was hiding the lower layer**
The synthetic wide map filled the upper layer with tile 0, which is a block A autotile and paints over the floor. The pinned fixture uses tile 10000 (block F), which is transparent. With tile 0 the frame was 13 colours; with 10000 it is 58 and the floor is visible. The wide map test now uses the fixture's own id and asserts the pixel difference.

**Mutation evidence, all four detected**
- block E `+ BLOCK_E_INDEX` removed: caught by `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`.
- the source tile check removed from `TryMove`: caught by `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` for rm2000 and rm2003.
- camera offset forced to 0: caught by `Test_AMapLargerThanTheScreenScrollsWithTheCamera`.
- `RecomposeFrame` removed from `TryMove`: **caught now**, by the pixel comparison. Before this card the same mutation passed the suite, because the test used the test hooks and recomposed on its own.

**Validation:** build 0 errors/0 warnings; `All 441 tests passed`, exit 0; `TestRm2kChipset 24/24`; `TestRm2kRuntimeRendering 13/13`; pinned golden image unchanged.

**Mostly closed by K-107**
The walk animation and the per frame step budget are implemented, tested and mutation checked, and the event facing bug is fixed. What is still open is the move route: events cannot follow `move_route` at all, so the per frame budget only runs for the player. That is the remaining gap between "the hero walks" and "playable".

### K-107 — RM2K character walk animation and the per frame step budget

**Status (2026-09-26) — VERIFY. The animation and the step budget are implemented, tested and mutation checked. The move route is not started, and one piece of sprite wiring cannot be covered by the available fixture.**

**The animation, verified from upstream**
`Game_Character::UpdateAnim` counts `anim_count` once per update and only advances the visible frame when a per speed threshold is reached: stationary `{12,10,8,6,5,4}`, continuous `{16,12,10,8,7,6}`, spin `{24,16,12,8,6,4}`, indexed by a one based speed. `IncAnimFrame` is `(anim_frame + 1) % 4` and resets the count. A character cell has three columns, and `Sprite_Character::Draw` clamps `Frame_middle2` back to `Frame_middle`, so the fourth rotation value is deliberately drawn as the middle frame. Cycling over three frames would animate at a different rate, so the four value rotation is a test of its own.

**The thresholds overlap.** At the default move speed 3 the stationary limit 8 is reached before the continuous limit 10, so the frame advances on the eighth tick, not the ninth. Four of my first test expectations were wrong about this; the reader was right every time.

**The step budget, verified from `Game_Character::Move`, `UpdateMovement` and `GetSpriteX`**
A move is not a tile snap. `Move` sets the logical tile to the target immediately and sets `remaining_step` to `SCREEN_TILE_SIZE`, 256. `Update` subtracts `1 << (1 + move_speed)` per update, so at the default move speed 3 a tile takes exactly sixteen updates. The drawn position is `GetX() * 256 - remaining_step` for a move to the right, which is what makes the sprite walk across the tile it just entered. `UpdateMovement` clamps at zero so an overshoot cannot wrap the sprite across the map. `GetMaxStopCountForStep` is `1 << (9 - freq)` and 8 or more means no wait, and it uses the move **frequency**, not the move speed, so a character can be slow and still start the next step immediately.

**Implemented**
- `Rm2kStepBudget` with the movement amount, the stop count tables, `Advance`, the `SpriteX`/`SpriteY` formulas and the pixel offsets.
- `GameSimulationState.RemainingStep`, filled by `TryMove` and spent by `UpdateCharacterAnimation`, cleared by `Reset`.
- The runtime's `Update` advances the character once per simulation tick and recomposes the frame while a step is unspent, and the hero sprite carries the step offset and the animation frame.

**A real reset bug this card found, the same class as K-106**
`Reset` did not clear `RemainingStep`, so a new game inherited a half finished step. The regression test found it by driving the real state rather than a fresh instance.

**A real sprite wiring bug this card found**
`BuildCharacterSprites` assigned the camera offset onto the hero sprite, overwriting the step offset `TryBuildHeroSprite` had just set. The step budget therefore reached the state and never reached the renderer, so the hero would have snapped to its tile while walking. The offsets are now added, not replaced.

**A real rendering bug found on the way, unrelated to the animation**
`FacingFromLiblcfDirection` read its argument as a one based axis (`1 => 6, 3 => 4, 2 => 2`) while its own comment documented the real one, `Game_Character::Direction`: `Up = 0, Right = 1, Down = 2, Left = 3`. Every event facing sideways drew mirrored, and nothing caught it because the fixture only has events facing down. Now on the verified axis, pinned by a permutation test over all four directions.

**The golden image changed, as a correction**
Wiring the LMT `character_pattern` through made the fixture's events render their real stored pose 0 instead of the runtime default 1. The difference is confined to the character bands, y 37 to 159, across 48 sixteen by sixteen cells, and the colour count rose from 85 to 92. The old golden encoded the default rather than the game data, so it was replaced after that analysis.

**Tests.** `test_rm2k_step_budget.cs`, 11 cases, covering the movement amounts, the sixteen updates per tile, the clamp, the per update pixel positions, each direction on its own axis, the stop count tables and their independence from the move speed, and the range refusal. `test_rm2k_character_animation.cs`, 10 cases. Two runtime tests: one drives the real `Update` path over the wide map and compares composed frames, one records that an empty party yields no hero sprite.

**Mutation evidence.** Detected: the movement amount shifted to `1 << speed`, the clamp removed, the step offset sign flipped, `RemainingStep` not filled by `TryMove`, `RemainingStep` not cleared by `Reset`, the per frame animation tick loop removed, the frame count changed from four to three, the modulo changed to three, the stationary guard changed, the move speed range check removed, the left and right facings swapped, and the out of range fallback changed.

**Two mutations reported as not mutant, recorded rather than chased.** Moving the direction mapping to a one based axis, and deleting the `0 => up` arm, both leave the function identical on every input because `0` was already handled by the `_ => up` fallback.

**Closing the verification gap: a starting party fixture**
The hero sprite's step offset was not mutation covered, because the pinned LDB defines an empty party, so the verified `ResetGraphic` path yields no hero and the hero sprite is never built. `Rm2kPartyFixtureBuilder` now appends the missing system section to a **copy** of the pinned database, leaving the fixture itself untouched.

Three encodings had to be right, and two of them are not obvious:
- A struct field is `id + length + payload` with both numbers in BER.
- `party_size` (0x15) is a **signed BER integer**, because the library reads it through its signed BER decoder.
- `party` (0x16) is a packed list of **two byte little endian** values, because the library reads it as `(short)(lo | hi << 8)`.

Writing either payload in the other's encoding parses and then reports trailing bytes, or parses and reports an empty party, which is the exact failure the builder exists to prevent.

The section is **appended**, not inserted. liblcf's `Struct<S>::ReadLcf` loops until EOF and breaks on each section's own terminator, and when a nested struct reads fewer bytes than the chunk declared it seeks to `off + length` and logs a corruption warning rather than trusting the inner walk. Our parser does the same, so a database is a sequence of terminated sections and one more is simply appended. Inserting at the first terminator lands inside the actors section, whose declared length is an upper bound that the reader seeks past.

**With a hero present the wiring is now covered, and two more assertions were needed**
- The composed sprite offset is the **camera scroll plus the step**, so the test asserts the difference against `AppliedCameraOffsetX/Y`. Asserting the composed value directly would only have asserted the camera.
- The hero is identified by its charset cell index and map position, not by composition order: an event can share the layer and the tile, and a wide map fixture has both.
- The animation frame needed a second assertion, because after one update the frame has not moved yet. Driving the step forward proves it changes, and driving the rotation to its fourth value proves the `ClampFrame` is applied where the sprite is built rather than only in the charset.

**Mutation evidence for the wiring, all now detected:** the hero's step offset not computed, the camera offset overwriting the step offset instead of being added, the animation frame not assigned at all, and the animation frame assigned without the clamp. Before the party fixture existed, the first two of these escaped.

**Still open on this card**
There is no move route. Events cannot follow `move_route` at all, so the per frame budget only ever runs for the player. That is the remaining gap between "the hero walks" and "events walk", and it is now a card of its own.

### K-108 — WOLF binary .mps reader, built from the verified format

**Status (2026-09-26) — DONE for the map format. WOLF is still not playable; see the honest gaps below.**

**What the previous entry found, and what this entry did about it**
The WOLF reader was JSON-only, so no real WOLF game could ever load. This card implements the verified binary `.mps` map format. The user was asked how to proceed and chose: build the binary readers, no real game is available, so validate against the specification.

**Implemented**
- `project/src/wolf/WolfBinaryMapData.cs`: `WolfBinaryMapData`, `WolfBinaryMapPixel`, `WolfBinaryEvent`, `WolfBinaryEventPage`.
- `project/src/wolf/WolfBinaryMapReader.cs`: `HasMapHeader` and `Read`, plus a bounded little endian `WolfByteCursor` where every read is checked, so a truncated or hostile file yields a diagnostic instead of an out of range access.
- `WolfDataReader.LooksLikeJson` still reports a non JSON payload as an unimplemented binary format rather than blaming the JSON parser. That was the honest-rejection part of the previous entry and it stays.

**Verified format facts used, none guessed**
- Header: ten zero bytes, `WOLFM`, a zero byte, a version header byte (0x00 v2, 0x55 v3), three zero bytes, a u4 that must be 0x64, then a version byte that must be 0x65 (v2) or 0x66 (v3).
- Then a length prefixed title (u4 byte count then the bytes, decoded as Shift-JIS), tileset id, width, height and event count, all u4.
- The map body is a first pixel u4. A value of 0xFFFFFFFF means the map does not exist and **no body follows**; otherwise the body is width * height * 12 bytes read as width * height mappixels of three u4 values each.
- A mappixel's first u4 carries the autotile id as raw / 100000 and the four corner modes as raw % 10000 / 1000, raw % 1000 / 100, raw % 100 / 10 and raw % 10.
- An event starts with 0x6F, a u4 that must be 0x3039, its id, a length prefixed title, map x, map y, the page count, a zero u4, the pages, and a 0x70 footer.
- An event page starts with the five byte signature 79 FF FF FF FF. The reference implementation derives the icon row as (byte >> 1) - 1.
- The map ends with a 0x66 footer.

**A real bug the test caught: a signed/unsigned comparison**
`ReadUInt32` returns `uint`. The first pixel was cast to `int` and compared against the literal `0xFFFFFFFF`, which C# types as `uint`. So `firstPixel` was `-1` and the literal was `4294967295`, the comparison was never equal, and **every map that does not exist decoded as if it had a pixel body**. That shifted the whole file and produced a wrong error, "ends at byte 53 but 57 were needed", which pointed at the reader instead of at the comparison. Fixed by keeping the value in unsigned space. This is the kind of defect that a green test suite with a hand written JSON fixture would never have found.

**A fixture bug found the same way**
The test's `BuildMap` wrote the two base tile values even when the first pixel was 0xFFFFFFFF, which cannot happen in a real file because the format skips the body entirely. The fixture now follows the same rule, so it cannot encode a frame the editor could not produce.

**Tests:** `project/tests/core/test_wolf_binary_map.cs`, 10 cases, all building bytes from the specification rather than from the reader's own output: full field decode, the autotile digit split with all four digits distinct, the non existing map, the event framing, a foreign magic, a wrong event signature, a missing footer, an out of range dimension refused before allocation, a truncated file refused with a byte offset, and an unknown version refused rather than guessed. `TestWolfRuntime` keeps the JSON rejection test.

**Mutation evidence, all three detected:** the signed/unsigned comparison restored, the footer check removed, and the header check removed. Each fails the suite.

**What this card does not claim**
- **No real WOLF game has been parsed.** The framing and the field order are proven against the specification; the interpretation of any single field is not. The next real game this runtime is pointed at is the first genuine test of that.
- The event page body after the signature is only partially decoded: graphic, trigger, move speed, frequency and route. **The command list is not decoded.** A wrong command count would desynchronise every following event, so it is a separate card rather than a guess.
- `database_dat`, `commonevent_dat` and `game_dat` are not implemented. The JSON reader still covers those, so the runtime cannot load a real project end to end.
- Games ship inside a DXLib archive and are frequently compressed or encrypted. The per version keys are published in clear text, so decryption is technically possible, but it is a separate decision and this card does not take it.
- There is no WOLF renderer. Nothing here draws a map.

### K-109 — WOLF event command list, decoded from the verified signature table

**Status (2026-09-26) — DONE for the core command set. WOLF is still not playable.**

**Why this card existed**
A command list with a wrong length desynchronises every following command, every event and every map, so the list is the one part of the WOLF format that must not be guessed.

**An important correction to the source material**
The published `event_command` description carries the header comment "event_command-related structures, **not used for file parsing**". It defines the sub-structures of individual commands but not the generic command frame. An earlier attempt inferred a frame of a **big-endian** signature u4 plus a padding byte from the map parser. **That inference was wrong and the card's original claims below have been corrected.**

**The command frame, as the schema actually describes it**
The frame is `param_count` (`u1`), then `command_type` (`u4` **little-endian**) when `param_count` is nonzero, then a parameter block whose shape belongs to the command, then `branch_depth` (`u1`), `string_count` (`u1`), that many strings, `have_route` (`u1`) and, when set, the route data. A `param_count` of **zero terminates the list**; it is not a parameterless command. The signature and the big-endian reader were removed, and `WolfByteCursor.ReadUInt32BigEndian` is no longer used for the command type.

**Implemented** `WolfBinaryEventCommand` with the verified command type and a name only where the schema gives one, `WolfEventCommandReader` decoding `param_count`, the little-endian type and the type specific parameter block, and `WolfMoveRouteReader` for the optional route.

**Real spec errors found by the tests**
- A command list written as zero bytes is not a list of parameterless commands: zero is the terminator, so such a fixture desynchronised everything after it.
- The `NumberCondition` and `CallCommonByName` layouts in the earlier attempt were guesses. They are now either decoded from the schema or refused.
- `CallCommonByName = 59` was invented. The verified type is **300 (0x12C)**.
- Operation names for the type `121` variants by parameter count were guessed and have been **removed**; those commands are distinguished only by their verified type and parameter count.

**Unknown command types stop the read instead of being skipped**
Skipping an unknown command would shift every following one, so the reader refuses and reports the type. A command this runtime does not implement is a diagnostic, not a silently missing line of a game's script.

**Tests:** `project/tests/core/test_wolf_event_command.cs`, 10 cases, every byte sequence built from the schema's command envelope rather than from the reader's output. `test_wolf_common_event.cs` covers the file header and the list, and `test_wolf_move_route.cs` covers the self-describing route entries.

**Mutation evidence:** the command type read big-endian, the `param_count` not consumed, a route argument count taken from a type table instead of from the file, and each option bit of a route's behaviour and option bytes swapped independently were all detected. The type table mutation is the important one: it proves unknown route entries stay readable.

**What this card does not claim**
- The command frame is read and its types are known, but a command's **meaning** is not implemented. A real event that uses a command this runtime does not interpret stops the read with a precise diagnostic.
- The command list is decoded **as data only**. No WOLF command executes. `WolfEventVm` still runs the JSON command model, not these bytes.
- The transfer format is not read at all, and there is still no WOLF renderer.
- Still no real WOLF game has been parsed. Framing and field order are proven against the schema; the meaning of a command is not.

### K-110 — WOLF database, game settings, common events, commands and move routes
**Status (2026-09-26) — VERIFY. The scoped binary readers are implemented; WOLF is still not playable.**

**Why this card existed**
The user confirmed the scope: binary `.mps`, `database_dat`, `commonevent_dat` and `game_dat`, and that **no real WOLF fixture exists**. Without that last fact every claim here is structural.

**Implemented** `WolfBinaryDatabaseReader` (header, version at byte 10, property position `raw/1000` with index `raw%1000`), `WolfGameSettingsReader` (V2 and V3, the twelve string block, the 23 value u16 record, editor version at index 16), `WolfBinaryCommonEventReader` (15 byte header, the fixed five byte `unknown4` block, the command list), `WolfEventCommandReader` and `WolfMoveRouteReader`. The JSON readers were preserved and only the binary/data discrimination in `WolfDataReader` was changed.

**The WOLFM magic and version framing**
The magic is the six bytes `00 57 00 00 4F 4C`, then a version header byte, `46 4D 00` at bytes 7..9, the version at byte 10 and the type count at bytes 11..14. Guessed bytes were removed after the header was measured.

**Move routes are self-describing, which is the point**
A route entry carries its own argument counts: a four byte count, that many words, a one byte count, that many bytes. An argumentless entry still writes both lengths as zero. There are 59 route types and 12 of them are parameterised, but **the counts are not inferred from a type table** because an unknown type then becomes unreadable. Unknown entries stay readable and keep their raw arguments.

**Route options are bitfields, not bytes**
The behaviour byte holds eight flags. The route option byte uses the **upper three bits**; the lower five are reserved. Reading it as a full byte is a mutation the suite catches, one bit at a time, because a single test with all bits set does not detect a swap.

**Real errors found and fixed**
- Three `X_OKX` sentinels, an invalid `PluginResult<T>.Ok` and a non-existent `PluginErrorCode.CorruptData` were replaced with the repository's real API.
- The database version was read at byte 9 and is at byte 10.
- `HasDatabaseHeader` required 15 bytes including the type count, so a magic-only fixture failed; the two cases were split.
- A binary file was blamed on the JSON reader before the discrimination was fixed.
- `0xFFFFFFFF` needed unsigned handling and a non-existent map sentinel means no body follows.

**Tests:** `test_wolf_binary_map.cs`, `test_wolf_binary_database.cs` (17), `test_wolf_game_settings.cs` (13), `test_wolf_common_event.cs` (17), `test_wolf_event_command.cs` (10), `test_wolf_move_route.cs` (11). Every fixture is byte-authored from the schemas.

**Measured, not guessed:** `0x83 0x65 0x83 0x58 0x83 0x67` decodes to `テスト`, not to the text the first fixture assumed. The expectation was corrected after measuring.

**What this card does not claim**
- The transfer format is **not** read at all.
- Nothing here executes. `WolfEventVm` still runs the JSON model.
- **No real WOLF game has been parsed.** Every test is synthetic. These are structural claims, never real game evidence.

### K-111 — RM2K event move routes, decoded and executed
**Status (2026-09-26) — DONE. Event move routes are decoded from the LMT and stepped in the runtime.**

**Verified structure** The route lives under `EventPage` `0x29`, with the command count at `0x0B`, the array at `0x0C`, `repeat` at `0x15` and `skippable` at `0x16`.

**Semantics that were wrong before they were measured**
- The first update **starts** movement and consumes no step budget.
- The command index advances only after movement **completes**.
- A blocked move advances only when `skippable` is true.
- Facing commands execute immediately and consume no movement.
- A finished route clears the remaining step budget.

**Implemented** `Rm2kMoveRoute`, `Rm2kMoveRouteState`, `rm2k_move_route_decoder.cs` and the runtime tick in `Rm2kEngineRuntime.Update`, with typed `MoveCommand`/`MoveRoute` models on `Rm2kMap`.

**Fixture boundary, stated honestly:** the pinned `Map0001.lmu` has 22 events and pages and **zero** move-route chunks, so it cannot prove a route. The end to end proof is a byte-authored synthetic LMU, and that is what `test_rm2k_event_move_route.cs` uses.

**Tests:** route `9/9`, decoder `9/9`, state `12/12`, end to end `7/7`.

**What this card does not claim:** only the verified command set is implemented. The pinned fixture exercises none of it, so no real game's route has been stepped.

### K-112 — RGSS archive format, shared by XP, VX and VX Ace
**Status (2026-09-26) — DONE as a reader and writer. Nothing executes an entry.**

**Why this was first for XP/VX/Ace** Without the archive an XP game cannot start at all, and this format is the one thing all three of the RGSS engines share, so it counts for three criteria where a Ruby virtual machine would count for none of them until it ran.

**Verified against the reference implementation.** Magic `RGSSAD`; every value is exclusive ored with the output of a linear congruential generator that starts at `0xDEADCAFE` and advances **once per value** by `magic = magic * 7 + 3`. A value is obfuscated with the generator's state **before** that step.

**The header is eight bytes**: the name `RGSSAD`, one byte the format does **not** check, and the version. The reference reader compares the first six bytes and reads the version from the last. A reader that also required the seventh byte to be zero would refuse a file the format allows, so the suite proves that byte is ignored instead of assuming it is zero. The version byte is what tells an XP or VX archive from a VX Ace one.

**Each entry** is a name, a size and a body. The name is obfuscated byte by byte and a backslash in it folds to a slash. The list ends when a name can no longer be read, not at a terminator. An entry claiming more bytes than the file holds is refused rather than handed back short, because a short body looks like a successful read.

**Two of my own bugs, both caught by tests rather than by reading.** A regular expression pass removed the `return` from three failure paths, so a refused archive fell through and was read anyway while the diagnostic said it was not an archive. And an unused version read indexed one byte past the end of an eight byte header, which crashed on an empty archive.

**Two of my own wrong expectations:** I had the header as three zero bytes after the name, and I had the generator taking two steps per field. The first surfaced as an out of bounds read on an empty archive, the second as a round trip that decoded a name length of three hundred million, which was **reproduced outside C#** before the reader was touched again.

**Tests:** `test_rgss_archive.cs` 15/15, total `678/678`.

**Mutation evidence, six of six detected:** a seed off by one, a wrong multiplier, a name byte read without the key, a version read from the wrong offset, the unchecked byte checked, and an entry list that started four bytes late.

**What this card does not claim:** the reader lists and reads entries. It **executes nothing**; a game script is bytes. There is no real RPG Maker game in the repository, so no real archive has been read.

### K-113 — Ruby Marshal reader for the RPG Maker data files
**Status (2026-09-26) — DONE as a reader. No game class is instantiated and no script runs.**

**Why this was second** With the archive in place the other half of the data pipeline was missing: XP, VX and VX Ace keep their data in `.rxdata`, `.rvdata` and `.rvdata2`, which are Ruby Marshal streams.

**Verified against the published Ruby specification**, not from memory: a two byte version, then one value, where a value is a type byte and a payload whose shape belongs to the type.

**Integers are the part that is easy to get wrong, and I got it wrong first.** A marshalled integer is a type byte and then one to five bytes, where the first of those encodes sign and width in a single value. Eight values are special; the rest is a sign extended byte with an offset of five. A reader that treats the first byte as a length decodes small numbers correctly and everything else as something plausible but wrong.

**An object takes its index before its contents are read**, because a value inside a collection may link back to that collection and the link names an object the stream has already defined. Numbering afterwards would point every such link at the wrong object.

**A link does not take an index of its own**, because it names an object that already exists. My first expectation had this wrong and the measurement corrected it.

**A regexp carries no class name**: the specification gives a source and an option byte and nothing else. A bignum is **refused rather than read**, because a game's data uses fixnums for anything that fits and a bignum would mean arbitrary precision this reader does not carry.

**Refusals, not partial trees:** a stream that ends inside a value, declares a length past the limit, or carries an undefined type byte raises. A major version this reader does not implement is refused outright and a **newer minor version** is refused too, because it may use a type this reader has never heard of; an older minor version is read.

**Two mistakes of mine, the second only visible under mutation.** A grouped `case` list plus single `case` labels for the same values left the later ones unreachable, so a regexp fell through to the refusal branch; and when that label was removed the routing line went with it, so no regexp could be read at all. Routing and payload are now separate concerns.

**The reader produces a value tree, not live objects**, on purpose: a game database is full of instances of classes this project has never heard of, and resolving them would mean either running the game's Ruby or inventing classes that do not exist.

**Tests:** `test_marshal_reader.cs` 29/29, total `678/678`. The fixtures are written by hand from the specification's type table, because a round trip through a writer of our own would pass even if the reader and the writer were wrong in the same way.

**Mutation evidence, fifteen detected:** a flipped sign offset, a swapped sign case, a zero case that swallowed a byte, a width read one byte short, an array numbered after its contents, a hash likewise, an uncapped nesting depth, an unchecked major version, an unchecked minor version, an uncapped byte count, an unchecked symbol link, an object link accepting index zero, a regexp reading a class name, a symbol link resolving out of range and a delayed array index.

**Two mutations turned out to be equivalent rather than escaping.** Moving `++ObjectCount` below the `ReadLength` call changes nothing, because reading a length does not touch the counter. A mutation that cannot change behaviour cannot be caught by a test, and recording it as a gap would have been wrong. A mutation that really delays the index until after the elements were read is detected.

**What this card does not claim:** no real `.rxdata` has been read, because the repository has no RPG Maker game.

### K-114 — RM2K vehicles: state, boarding, sprites and the airship shadow
**Status (2026-09-26) — VERIFY. Simulation and rendering are implemented and mutation tested; K-094 is not closed by it.**

**Verified from the reference implementation, not guessed.** Boat and ship move at speed 4 and the airship at 5, so a move speed of 3 means half speed. A vehicle's altitude is measured in tile units against a budget of 256 and falls by 8 per update. A moving vehicle animates over 12 frames and a stopped one over 16, both modulo 4. The airship's shadow is a separate sprite drawn from `(128,32,16,16)` and `(144,32,16,16)` at opacity `(int)(0.26 * 255) = 66`, one below the airship, and visible only while the player is aboard.

**Boarding is asymmetric.** An airship refuses a boarding attempt from a tile it is not directly over, and refuses a disembark while still in the air. A boat or a ship does not. Boarding has priority over an action event, which the reference implementation proves by the order `if (!GetOnOffVehicle()) CheckActionEvent(); return;`.

**Vehicle background music is deliberately absent.** There is no BGM state contract in this project, so switching a vehicle's track would mean inventing one. It is not hidden behind a diagnostic flag; it is simply not there.

**The pinned fixture cannot show a vehicle, and the tests say so.** All three vehicles in the pinned `RPG_RT.lmt` target map 39, while `Map0001` is map 1, and the pinned `vehicle.png` is absent. The drawing path is therefore exercised through a **derived** fixture that copies the game and adds the official reference test image, leaving the pinned data untouched. A test states the boundary explicitly instead of pretending otherwise.

**Test-only hooks** exist to place a vehicle on the map under test and to re-render. They are called from tests only and are documented as such.

**Tests:** vehicle `11/11`, boarding `11/11`, decision turn `12/12`, sprite `9/9`, compositing `6/6`, runtime rendering `19/19`.

**Measured after the fact:** the airship's system index is **3**, not the 2 the first expectation assumed.

**What this card does not claim:** `move_random`, hero directed movement, broader event and audio integration and the rest of the whole engine remain open. This card is one slice of K-094, which stays `VERIFY`.

### K-115 — Ruby lexer for the RGSS engines
**Status (2026-09-26) — DONE as a lexer. It calls nothing, resolves nothing and runs nothing.**

**Where this sits** With K-112 and K-113 in place, this is the third of the three layers XP, VX and VX Ace need before their scripts can be read, and the first that looks at the script text itself.

**The keyword list is Ruby's own, not written from memory.** Forty one reserved words extracted from the grammar's `parse.y`. A keyword is reserved, so a lexer that treated one as a name would accept files Ruby rejects and reject files Ruby accepts.

**A name beginning with an upper case letter is a constant, and the reserved word check comes first.** Two reserved words, `BEGIN` and `END`, begin with an upper case letter and the grammar's `reswords` production lists them as keywords. Checking for a constant first read them as names. A test over **all forty one** words is what found it, because the single example I had chosen happened to be lower case.

**A slash divides where a value has just ended and opens a regular expression where one could begin.** The first version had this backwards, so `a / b` was read as a regular expression that ran off the end of the line. Both shapes are in the suite because they differ only in what came before the slash.

**A single quoted string interprets only two escapes**, the quote and the backslash. Reading it like a double quoted one loses a backslash a game asked to keep, which is the entire reason the form exists.

**A regular expression keeps its backslashes**, because the pattern engine is what interprets an escape, and a `/` inside a character class does not close it.

**A string keeps its bytes as well as its text**, because a Shift-JIS script is not UTF-8 and a reader that kept only text would silently corrupt it.

**An octal literal may be `0o17` or `017`.** The marker sits between the leading zero and the digits; checking the current character instead of the next one read `0o17` as a bare zero.

**Refusals, not partial token lists:** an unknown character, an unclosed string, an unclosed regular expression and a number with no digits in its base all raise with their line.

**Tests:** `test_ruby_lexer.cs` 33/33, total `711/711`.

**Mutation evidence, fourteen run and eleven detected:** a keyword list never consulted, the reserved word check moved after the constant check, the constant rule inverted, a slash always a regexp, a slash always a division, single quoted escapes applied in full, the octal marker not skipped, a shorter operator matched first, an unclosed string accepted, a line continuation read as a break, a block comment not skipped, a class variable read with one at sign, a regexp losing its backslash, an unterminated block comment end.

**Two mutations were equivalent rather than escaping.** Appending `<=` and `<<` to the operator list changes nothing, because every multi character operator already appears before the shorter one it starts with; the check printed the whole list to establish that. Turning a byte escape's `((char)value).ToString()` into `value.ToString()` changes nothing, because the cast already produces values in the range where the two agree. A mutation that cannot change behaviour is not a gap in the tests.

**Four gaps the mutations found were real and are now closed:** the keyword lookup was untested, the single quoted escapes were only checked for one letter, the operator order was checked only for the operators the test happened to use, and the line continuation test filtered the very newline it was about.

**One mistake of mine in the tests hid four failures.** The helper that drops whitespace-only tokens did not drop the end of input token, so every list based assertion was off by one element. A probe with a different filter showed the lexer's output had been right all along.

**What this card does not claim:** there is no parser yet, so a script is a token stream and nothing more. **No real RPG Maker script has been tokenised**, because the repository has no RPG Maker game.

### K-116 — Ruby parser for the RGSS engines
**Status (2026-09-26) — DONE as a parser. It builds a tree and runs nothing.**

**Where this sits** With K-112 (archive), K-113 (Marshal) and K-115 (lexer) in
place, the data of an XP, VX or VX Ace install is readable from end to end as
data. A game's Ruby now has three layers: bytes, tokens, and this tree. What is
still missing is everything that would give the tree meaning.

**What it does** `RubyParser` turns a token stream into a tree of shapes. It
answers one question — what shape was written. It does not answer what any name
means, whether a call succeeds, or what a value is at run time. A node that
records a call names the method as written and knows nothing about whether this
runtime has ever heard of it.

**The precedence is the grammar's own.** Every level was taken from the
declaration order in the Ruby grammar rather than from memory. This turned out
to matter more than expected: the first table written from memory had the
relations and the equality on separate levels, which the grammar's
`rel_expr %prec tCMP` shows are one. A reader that gets one level wrong parses a
game's arithmetic into a different tree, and nothing about the result looks
wrong.

**What is deliberately not here**
- No name resolution, no method lookup, no constant lookup.
- No execution, no evaluation, no calling of anything.
- No literal Ruby objects, no binding, no class loading.
- A shape this parser cannot read raises with its line. A tree that stopped
  early would be worse than none, because nothing would mark it as complete.

**Verification (2026-09-26)**
- `TestRubyParser` 44/44, total 755/755, `scripts/validate.sh` passed.
- Every expected tree is written out by hand from the grammar's rules. A tree
  produced by the parser and compared against itself would prove nothing.
- 21 mutations, all detected.

**Errors the tests found in this parser, all fixed**
- The precedence table from memory had relations and equality on two levels; the
  grammar resolves them onto one with `rel_expr %prec tCMP`.
- `**` sat at the arithmetic level instead of above it, so `a * b ** c` parsed
  as `(a * b) ** c`.
- A member call's argument list was skipped whenever the receiver was a name, so
  `sprite.draw(x, y)` read its parentheses as a grouping.
- A block's body was read as a whole program, so every `def` and `do` reported a
  missing `end` on a file that is well formed.
- A `do` belonging to a `while` was read as a block on the loop's own condition.
- The range operator had no level at all, so `1..2` parsed as two statements.
- `not` was read both in `ParseUnary` and in `ParseBinary`. The second was
  unreachable, and the mutation suite showed the 44 tests passed with it gone, so
  it was removed rather than kept as a second route to the same node.
- Two mutation escapes turned out to be untested boundaries rather than wrong
  code: nothing crossed the logical/bitwise boundary, and nothing pinned `not`
  to its own level. Both now have tests.

### K-119 Read a whole number wider than this machine holds
`READY` → `IN PROGRESS` → `DONE`

**The question that started this** The marshal work had been checked against
Ruby 3.4, because that is the documentation that is easiest to reach. The
engines of this repository's line run older rubies, so the whole ground truth
was suspect. It was checked against the sources themselves:

- **XP is Ruby 1.8.1, VX is 1.8.3, VX Ace is 1.9.2.** All three carry a marshal
  format.
- All twenty five type bytes are **identical** across 1.8.7, 1.9.3 and 3.4.1.
  The format did not change for the engines in question.
- The whole number form did change, and in the direction that matters:
  **1.8 and 1.9 write `i` for a number that fits in thirty one bits and `l` for
  the digits of anything larger. Ruby 3 swaps the two letters and writes the
  large form in binary.** A reader built from the 3.4 table would refuse every
  file an engine of this line writes.

**What the reader had wrong, and it was wrong about the sign**

A whole number that does not fit is written as a sign and one byte per digit,
and the digits of a negative number are the number carried to the width it was
written in, so every byte after the first is the top of the width. The reader
was negating the unsigned value instead, which looks the same for a one byte
number and is not the same for any other: **it read one byte too many and took
the first byte of whatever followed in the file.** A game's negative coordinate
would have had the next value's bytes inside it, and nothing downstream can tell
that from a real number.

The one byte negative form was also on the wrong side of the boundary. The rule
is five to one hundred and twenty seven is the number with five taken off, and
minus one hundred and twenty nine to minus five is the number with five added,
with minus one to minus four the wide form. The reader had the last two the
wrong way round.

**How it was found** Not by three hand written cases. A test walks six thousand
and one numbers through the writer taken from 1.8.7's own loop and compares
each against what the reader says, and a second test holds sixteen numbers
against the bytes that loop produces, because the first version of those was
written from memory and was wrong about four of the sixteen. **Every fault found
in this card was in the test rather than in the reader**, which is the opposite
of what the range test was written expecting, and the reason it is worth having
is that it is the only one of the two that can find a fault at all.

**A fault this card found in a fault of an earlier card** The earlier card
refused a wide number with the reason that it would need arbitrary precision.
That reason was wrong: a game's number is written as decimal digits, so the
number itself is readable, and the only question is whether it fits this
machine. It is read now, and refused only when it does not fit, with the number
of digits in the reason so that a number too large and a file that is not
marshal are not the same fault.

**One thing this card did not add** A check refusing a count byte wider than a
whole number. Such a count cannot occur: five to one hundred and twenty seven is
the one byte form, and a count of one hundred and twenty seven is the number one
hundred and twenty two. The check was written from a reading of the byte range
rather than of the rule, and it refused a length a game writes for every list it
has. It is gone, and the fact it was based on is tested instead.

- Tests: `TestMarshalReader` 39/39, total 818/818, validator passed, 0 warnings.
- 8 mutations of the packing, all detected.

**Still missing** A whole number wider than a whole number this machine holds is
read and then refused, which is honest but means a game holding one will not
load. No archive from any of the three engines is in the repository, so all of
this is verified against the rubies' own sources and not against a game.

### K-118 Name what every child of a tree is for
`READY` → `IN PROGRESS` → `DONE`

**What it is** A consumer of the parse tree has to ask a node for the test, the
body, the left of an operation or the first argument, instead of knowing the
layout of every kind by heart.

**The fault this found** The parser said only that a child was there, and what
the list meant depended on the kind and on nothing else. A keyword that opens a
test held the keyword first and the test second, a ternary held the test first, a
block on a call held the call, the parameters and the body, and a block that was
a body held only statements. **One kind could mean two things, and the second
meaning was invisible.** Every consumer would have had to learn the layouts from
the parser's source, and nothing would have said when one of them was wrong.

**What changed** Every child carries the role it plays. `Children` stays for a
reader that wants the order and does not care what the order means, and a node
whose roles are empty has not been given roles rather than having none. A lookup
for a role that is not there answers null instead of falling back to the first
child, so an absent role cannot be mistaken for a present one.

A name is held in `Name` and not in `Text`. That was worth a test, because a
test reading `Text` finds null and could be fixed either by filling `Text` or by
reading `Name`, and only one of those is right.

- Roles filled for a keyword that opens a test, a ternary, an assignment, an
  operation and a call. The four places that build a call say their shape through
  one helper rather than each repeating it.
- Tests: `TestRubyParser` 52/52, total 808/808, validator passed.
- 6 mutations on the roles and the lookup, all detected. Two of them escaped at
  first because a lookup that takes the last of a role and one that takes the
  first cannot be told apart while every node holds at most one child under a
  role, and because a lookup that fell back to the first child passed every test
  that asked for a role that was there. Both are now tested.

**Why this comes before a machine** A machine that ran the tree would have had to
read the parser's source to know which child was the body, and a mistake there
runs a name as if it were a statement. That is quietly wrong rather than loudly
wrong, which is the worst shape a mistake can have.

**Still missing for XP, VX and VX Ace** A machine to run the tree, and any real
archive from any of the three engines.

### K-117 — The value layer between a game's data and its language
**Status (2026-09-26) — DONE as a value layer. It names values and judges none.**

**Where this sits** K-112 reads the archive, K-113 reads Marshal, K-115 reads
the tokens and K-116 reads the tree. What was missing between "a file said this"
and "the language calls this a value" is this card. Without it the two layers
would each have their own idea of what a game's data means, and they would
disagree without either being wrong.

**What it does** `RubyValue` holds the seven kinds the language defines, as
data, with a value's identity and its contents and nothing else.
`RubyValueConverter` turns a decoded Marshal value into one, following the
file's links, and refuses anything that has no equivalent.

**The refusals are the point.** A kind the language has no name for, a payload
that contradicts its kind, a mapping entry without its other half, a mapping that
holds the same key twice, a link to an entry that was never decoded: each of
these raises with the reason, and each refusal is counted and remembered. A value
that is nearly right is a value a game cannot be trusted with, because nothing
downstream can tell it apart from a real one.

**What is deliberately not here**
- No arithmetic, no comparison, no conversion between kinds.
- No method dispatch, no calling of anything.
- No class loading: a game's own class is kept as the text the file wrote.
- No decoding of a string's bytes. A game's strings are in its author's
  encoding, usually CP932, and choosing one is a decision this layer does not
  make.

**Verification (2026-09-26)**
- `TestRubyValue` 15/15, `TestRubyValueConverter` 30/30, total 802/802,
  `scripts/validate.sh` passed.
- 13 mutations on the converter's kind names, its refusals, the link following
  and the value identities, all detected.
- The link tests read real byte streams written with the format's own packing.
  A Marshal long is not eight bytes, and a stream written with eight would be a
  different stream from the one a game writes.

**A bug this work found in the reader the card before it**
The converter was written against kind names spelled out from memory. The reader
emits `array, false, float, integer, nil, object, regexp, string, struct,
symbol, true` — and two of the converter's names were not on that list. A whole
number arrives as `integer` and a string as `string`, so **every number and
every string in a real game's data would have been refused**. The kind names are
now taken from the reader itself rather than from memory, and two mutations that
delete each of the two arms are both detected.

**The numbering is the reader's, and the specification is explicit about it**
A stream holds one copy of each object and one of each symbol. The first object
has the number one and the first symbol the number zero. The converter reads
that number from the value the reader handed over instead of counting again,
because counting again would be a second opinion about a number that was
already decided.

A container is numbered **before** its contents are read. That is not an
implementation detail: it is the only reason a container can hold a reference to
itself, which a game's data does whenever a structure names itself. The
documented stream for an array holding the same string twice,
`"\004\b[\a\"\nhello@\006"`, has the array at one and the string at two, and
the link names two.

**Other bugs this work found in the converter**
- The converter never recorded the stream's own entry numbers, so every link was
  reported as pointing at nothing.
- The first version numbered values from zero and after their contents, which
  gave a container a higher number than its first member.
- A value was filed under its number before its class was attached, so a link to
  a game's value found an object that no longer said what class it was.
- A value that points at itself is refused with the number in it, because there
  is no value to return yet and returning something else would give it a second
  identity inside its own contents.
- The class name was being attached twice, once where the contents are read and
  once afterwards. The second could never change anything, and a mutation that
  removed it went unnoticed, which is how the duplicate was found. It and the
  helper it alone used are gone.

### K-120 Read the data three real games actually wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** Every parser and reader in this repository was
written against a fixture this repository made, or against EasyRPG's test game.
Both are right for what they are and neither is a game: the RM2K test game has two
hundred and ten bytes of database, six bytes of map tree and five maps, and every
count, length and index in a game of that size fits in a byte and would not in a
real one. **No fixture in this repository was written by an engine.** A reader
that has only ever read a hand made file has never been shown a game.

Three games were given to the repository to use. They were classified from their
own files and nothing in them was executed.

| Game | Engine | Decided by |
|---|---|---|
| `rgss-xp` | RPG Maker XP / RGSS1 | `RGSS104J.dll`, `Game.rxproj` naming `Scripts.rxdata` |
| `rgss-xp-microquest` | RPG Maker XP / RGSS1 | `RGSS104E.dll`, `Game.ini` |
| `rm2k-dragon-destiny` | RPG Maker 2000 | `RPG_RT.ldb`, 743 `.lmu` files, `RPG_RT.ini` |
| `kirikiri` | **KiriKiri, not WOLF** | `SoftModeFlag`, `FrameSkip`, `SEandBGM`, no `Game.dat` |

**What the real files found**

The XP games keep their database as marshal and, with an unencrypted archive, in
plain files under `Data/`. Sixteen of them are now in the repository, ~310
kilobytes, and **all of them are read**: `TestRealXpData` walks every value of
every file and finds no fault. The marshal reader is now checked against bytes an
engine wrote, not only against the rubies' own sources.

Two of my own assumptions were wrong and the files said so:

- **A map is not a hash.** It is an `RPG::Map` object with eleven members, and
  eleven keys. The reader was right; the expectation written from memory was not.
- **A `.lmu` holds an `LcfMapUnit`**, not an `LcfMap`. Measured, not remembered.

**The KiriKiri game was offered as a WOLF game and is not one.** It carries
folders called `BasicData` and `MapData`, which are two of the three things the
Wolf detector looks for, and its data folder is laid out the way a Wolf game's is.
It has no `Game.dat` anywhere, and that is the third thing. The detector refuses
it, and `TestKirikiriIsNotAWolfGame` proves the refusal is a decision rather than
an accident of not having looked: **the same folder with a `Game.dat` in it is
detected as Wolf.** A folder full of what a Wolf game would have is not a Wolf
game, and a detector that answers either way rather than refusing has guessed.

**What is claimed and what is not**

Claimed: the XP detector recognises an XP installation from its own files and
does not confuse it with VX or VX Ace; the marshal reader reads sixteen real
files from two independent installations; the RM2K parser reads a 416 kilobyte
database, a 57 kilobyte map tree and two maps of a 743 map game.

Not claimed: that a game's data is **understood**. A database read as a
dictionary of chunks is a database read; it is not an actor, an event, a page or
a chipset. A map file is read as an `RPG::Map` holding its members; nothing here
knows what a member called `@events` is for. There is still no renderer, no
script execution, no save path, and `RgssEngineRuntime` is still metadata only.

**Tests and evidence**

- `TestRealXpData` 6/6 — sixteen real files, two installations, two encodings.
- `TestRealXpDetection` 3/3 — an XP folder is XP, is not VX or VX Ace, and a
  folder with data but no layout is either XP or nothing.
- `TestRealRm2kData` 3/3 — a real 416 KB database, its 57 KB map tree, two maps.
- `TestKirikiriIsNotAWolfGame` 2/2 — the refusal, and what makes it a decision.
- `TestMarshalReader` 40/40 — the last one added tells a number this machine
  cannot carry apart from a file it cannot read, which is the one mutation of the
  eight that escaped the first suite.
- Total **833/833**, validator passed, build 0 warnings / 0 errors.
- **Eight mutations of the reader, all detected.** Two of the first suite's eight
  did not test anything: it counted a mutation as breaking the build whenever
  `error CS` appeared anywhere in a run, and the run prints the mutation report
  of the step before it, so six that compiled were reported as broken. The suite
  now compiles each mutation on its own and only calls it broken if that build
  really fails.
- `project/tests/fixtures/RGSS_FIXTURES.md` holds every file with its size and
  SHA-256. No executable, DLL, save, image, audio or script is imported.

**Deferred, not done**

- The XP games' `Scripts.rxdata` is deliberately **not** imported. A script is
  code, and this repository does not run a game's code.
- 743 maps of `rm2k-dragon-destiny` are not in the repository; two are, and the
  rest are the same format at a different size.
- No archive from any of the three engines is in the repository, so the
  `RgssArchiveReader` still has no real file to read.

### K-121 Read the data an RPG Maker MZ game wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-120 brought in real data for RGSS and RM2K and
left MV and MZ where they were: **detection and a count of entries**. There was
no reader that returned a game's values. An MZ game keeps its database as plain
JSON, so a reader for it can exist without a runtime and without running a line
of the game's own code, and none was written.

An MZ 1.9.1 game was given to the repository. It was classified from its own
files — `game.rmmzproject`, `node.dll`, `package.json`, `js/rmmz_core.js` — and
nothing in it was executed.

**What was built**

- `project/src/mz/MzJson.cs`: JSON read the way a game wrote it, with nesting
  bounded, a string that is not closed refused rather than run to the end, an
  escape the editor never writes refused by name, and a number with an exponent
  and no digits refused.
- `project/src/mz/MzDataFile.cs`: one of a game's data files, holding **one root
  value** and the file's own text so a caller can hash what was read.

**Three things the format has, all found by reading the file and not a
description, and two of which were wrong in a first draft of the test**

1. **A database file's first entry is null.** `Actors.json` is `[
null,
{...}]`.
   The editor numbers its actors from one so zero can mean "no actor".
2. **A command is a small number and is not packed.** This game's commands are
   `121`, `231`, `357`, `657` and nothing above a thousand anywhere in the file.
   In the generation before, a command's number is its value times a thousand
   and a reader divides by a thousand. **A reader written for MV and pointed at
   this file would divide every command to zero.**
3. **A map's events are indexed by event, not padded to the field.** `Map002` is
   seventeen by thirteen and its `events` array holds seven entries, the first
   null. A first draft of this test claimed the array ran over the whole field.

**An API of mine that was a trap, and removed rather than documented**

The first version of `MzDataFile` exposed `Top` as a `List<MzValue>` holding the
one root value, so `Top[0]` was the file and `Top[0][0]` its first element. The
test that was written against it then read the array where the object was and
failed in four places at once. **A one element list is not a root value**, and it
is now `Root`, an `MzValue`.

**The reader that was already here is a different thing, and the difference is
now stated by a test**

`MzDataDirectoryResult` exists and counts entries, takes names and caps a file
at 2 MiB. `TestMzReaderBoundary` says so and checks the cap it states. It returns
no map, no event, no command and no coordinate, and the new reader returns
values and does not name a game. Neither is derived from the other.

**Tests and evidence**

- `TestRealMzData` 15/15 — eleven real data files, the three format traps above,
  and five refusals.
- `TestRealMzDetection` 3/3 — the game is MZ, is not MV, and the folder with the
  previous generation's runtime is answered differently.
- `TestMzReaderBoundary` 2/2 — the boundary to the reader that was already here.
- `TestMzDataDirectory` 8/8, unchanged.
- Eight mutations of the new reader. **The first suite detected one of eight**,
  and that is the honest number: it found five real gaps — an unclosed string was
  run to the end of the file, an unknown escape was taken as text, nesting was
  unbounded, a broken exponent became a number, and a file's own text was thrown
  away — plus one anchor that did not exist. Each gap got a test of its own and
  the suite was rerun.
- Total **853/853**, validator passed, build 0 warnings / 0 errors.
- `project/tests/fixtures/MZ_FIXTURES.md` holds every file with its size and
  SHA-256. The two `js` files are **placeholders carrying the real names**: the
  runtime is 175 KB and 83 KB of a game's own code and is not imported.

**Still not true of MZ**

- No JavaScript runtime, so **no plugin, no script, no event command runs.** An
  MZ game does not play.
- Nothing here knows what command 231 does or what a page's conditions mean.
  Values are read; they are not understood.
- MV shares the data format and has **no fixture at all** from a real game, and
  its command numbering is the packed one, which is exactly the difference the
  second trap above is about.

### K-122 Name every command an RPG Maker MZ game stores
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-121 read a game's numbers and could say a
command was 121, which told a caller nothing. A name per command needs a source,
and the source is not a documentation page: it is the method the engine dispatches
the command to, which the engine's own source carries the name of in the comment
above it.

**What was built**

- `project/src/mz/MzCommandName.cs`: **114 commands, 101 to 603**, every number
  and every name generated out of the engine source of a real game. A record
  `MzCommand(int Code, string Name)`, so a number and its name are one value.
- `project/src/mz/MzCommandTable.cs`: what a number in a command list is — a
  command, the data of a command, the editor's own indent, or unknown — and which
  command reads which data number.

**A table written from memory, and what it cost**

The first draft of the table was written by hand. Compared against the engine,
**79 of its 178 names were wrong.** 129 was written "Change Hp" and the engine
calls it "Change Party Member". 231 was "Move Event" and the engine calls it
"Show Picture". Twenty six commands the engine has were missing and forty three
that it does not have were there. A plausible command a game does not use is
invisible until a game uses it, and this game uses 231.

**The rule that is not a rule, measured**

"Which command does this data belong to" invites `code - 300`. Against this game
that is right **four times out of eight**:

| Data | Owner measured | `-300` says | What that is |
|---:|---:|---:|---|
| 401 | 101 Show Text | 101 | right |
| 405 | 105 Show Scrolling Text | 105 | right |
| 408 | 108 Comment | 108 | right |
| 655 | 355 Script | 355 | right |
| 412 | 111 Conditional Branch | 112 | **Loop** |
| 501 | 102 Show Choices | 201 | **Transfer Player** |
| 605 | 302 Shop Processing | 305 | not a command here |
| 657 | 355 Script | 357 | **Plugin Command** |

Two of the four mistakes point at a command that exists in this generation and
does something else. A reader that used the rule would read a branch's else as a
loop, a choice as a teleport, a shop's purchases as a number meaning nothing, and
a script line as a plugin call. **The owners are written down because none of them
can be calculated**, and a test says so by running the rule and counting four.

**411, 412 and 413: two commands and one piece of data**

All three sit at an indent of their own under a branch, so all three look like the
branch's options. The engine names **411 "Else"** and **413 "Repeat Above"** as
commands of their own, and gives **412 no method at all**. A reader that treated
the family as data would refuse two real commands; one that treated it as
commands would run a branch's structure as an instruction. Both fail silently.

**A name written twice, and three mutations nobody saw**

The first shape was an enum with a name in each member's doc comment and a second
table beside it carrying the same names as strings, because a C# identifier cannot
be `Show Text`. **The two copies drifted and three name mutations were invisible**
— the reader handed out the string while the enum carried the prose, so changing
either alone changed nothing a test could see. It is a record now and a name is
written once.

**Tests and evidence**

- `TestMzCommandTable` 13/13 — every command named, every command this game uses
  named, the three data codes refused as commands, the four that are commands not
  refused, the rule measured at four of eight, and every number the table does
  not hold checked rather than the ones someone thought of.
- Eleven mutations. **The first suite detected four of nine**, all three
  name mutations escaping for the reason above. After the record replaced the
  enum and two tests were added, the name mutations are all seen, and the
  mutation that had nothing to test — adding a command to the owner map, which
  cannot matter because a command is decided before an owner is consulted — was
  replaced by one that can fail.
- Total **866/866**, validator passed, build 0 warnings / 0 errors.
- `grep` for `Execute`, `Run`, `Invoke` and `Eval` in `project/src/mz/`: none.
  A 657 line is held as the text the author wrote and is never run.

**Still not true of MZ**

- **An MZ game does not play.** A command is now named, which is the opposite of
  running it, and this repository will not run a game's script. A 657 line is
  text here and stays text.
- Nothing interprets 111's six comparisons or 121's three modes. Naming a command
  is not doing it.
- MV shares the format and has no fixture; its numbering is the packed one, which
  is exactly what the second trap of K-121 is about.

### K-123 Decide a conditional branch the way the engine does
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-122 named every command, so a branch read 111
with its six parameters and nothing more. Naming a command is the opposite of
doing it, and the first command whose whole effect can be taken from the engine
without running anything is a branch: the engine decides it in one method, and
every number in that method is readable.

**What was built**

- `project/src/mz/MzBranch.cs`: a branch, what it tests, the six ways of
  comparing, and the facts a caller has.
- `project/src/mz/MzBranchEvaluator.cs`: decides a branch from those facts and
  from nothing else. Fourteen kinds, of which nine are decided and the rest say
  what is missing.

**One branch is deliberately not decided.** Kind 12 asks whether a line of the
author's own JavaScript is true and the engine writes `result = !!eval(params[1])`
for it. This repository does not evaluate a game's JavaScript, so that branch is
`ScriptNotRun`, the author's text is kept, and the answer is neither true nor
false. **A mutation that made it answer true was the first thing the suite
checked and it was caught.**

**Three things in the method that were got wrong, each by a reader that had read
it**

1. **The third parameter only says whether the right side is a variable.**
   `params[2] === 0` picks between the number `params[3]` and
   `$gameVariables.value(params[3])`. This reader read `params[2]` as the
   variable, so the game's own branch `[1, 77, 1, 78, 1]` asked about variable
   one where the game asked about variable seventy eight. The test harness then
   made the opposite mistake, so the two hid each other for one run.
2. **Gold has a numbering of its own.** `switch (params[2])` with case 0 at
   least, 1 at most, 2 less — where a variable's case 0 is equal to and case 1 is
   at least. The first three are the same words in a different order. A purchase
   gated on a hundred gold **opens at ninety and shuts at a hundred and ten**,
   and the reader is right about the arithmetic and wrong about the question.
3. **A timer has no number in the parameters.** The second is a threshold in
   seconds and the third is the way, because the branch asks the one timer the
   event owns. This reader asked for a timer called five on a branch about five
   seconds, and refused a branch it could have answered.

**What is not known is not off.** A branch that asks about a switch nobody
supplied comes back `Unknown` and names the switch. A reader that treated the
missing as off would skip a game's content with nothing to show for it, which is
the one failure here that would be invisible, and a mutation of it was caught.

**Tests and evidence**

- `TestMzBranchEvaluator` 11/11 — the game's own three branches decided and none
  refused, each of the six comparisons at its own boundaries, gold under its own
  numbering with the ninety and a hundred and ten case, a stopped timer not
  compared, a script branch reported and not run, a missing thing refused by name,
  and every number that is not one of the fourteen kinds checked rather than the
  ones someone thought of.
- Nine mutations, **the first suite at eight of nine**. The one that got through
  folded a kind the engine has no name for into the nearest kind it does have,
  which is the shape of every mistake this file was prone to. It now checks every
  number from 14 to 657 that is not a kind, and that the refusal names it.
- Total **877/877**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **A branch is decided; nothing else is.** 121's three modes, 126's change to
  an item and 126's change to a weapon are not, and a game's flow is a chain of
  commands, not one of them.
- There is no interpreter holding an index into a list, so a branch decides
  something and nothing acts on it yet.
- Still no renderer, no save path, no input, and a 657 line is text.

### K-124 Walk an event list with an index the way the engine moves it
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-123 decided a branch and nothing acted on it,
because there was no index into the list to move. A game's flow is a chain of
commands, and a branch decides one of them and no more. The first thing the
interpreter has to be right about is not what a command does but **where the
index goes after it**, because every other rule in an interpreter hangs off
that.

**What was built**

- `project/src/mz/MzCommandEntry.cs`: one command out of a game's list.
- `project/src/mz/MzOperation.cs`: the four operands, the operation types, and
  a `MzRandom` **held per interpreter** — a static one would be shared between
  two runs of two events and give a game the same numbers twice.
- `project/src/mz/MzCommands.cs`: 121 and 122, and a rule that says which
  commands this reader acts on.
- `project/src/mz/MzControlFlow.cs`: the commands whose whole effect is the
  index, and the three answers they give.
- `project/src/mz/MzInterpreter.cs`: the index, the branch results per indent,
  the step limit, and the four ways a run can end.

**The index rules, each read out of `Game_Interpreter` and not reasoned about**

1. **Every command that returns true is followed by `this._index++`.** A first
   draft added a flag for "the command moved the index itself" and then did not
   step over a command that had, which made an else land on the false arm it had
   just skipped. The flag is gone.
2. **A repeat above is not an exception.** It walks back to the first command at
   its own indent, and the step then moves off that one — so `112`, body, `413`
   goes round properly without any special case.
3. **A command the engine has no method for is stepped over, not refused.**
   `executeCommand` asks `typeof this[methodName] === "function"` and, when it
   is not, still does `this._index++`. **Every one of those commands is one this
   game stores on purpose**: 0 the end of a block, 401 a line of text under a
   101, 412 the end of a branch, and 655 and 657 the two halves of a script.
   Refusing any of them would strand the game on a command the engine itself ran
   past.
4. **A list that ends inside a branch is said, not read past.** The engine's
   `skipBranch` has no test for the end of the list; this reader reports
   `Truncated` and names what is wrong.
5. **The step limit is the engine's `checkFreeze`.** A hundred thousand
   commands in one frame freezes the game in the engine. This reader has no
   frames, so it counts the same way and reports `Frozen`.

**Two findings that came out of the real map, and neither is a test mistake**

1. **This game stores a loop that nothing can leave.** Event 4 is a 112 with
   seven message commands and a 413, and nothing between them tests anything or
   breaks. The engine plays it until `checkFreeze` stops it. The reader reports
   the same thing, and the test says a freeze there is the correct answer rather
   than papering over it.
2. **Random is drawn per variable, not per range.** A first draft claimed one
   draw for a whole range. The engine's `command122` calls `Math.randomInt`
   **inside** `for (let i = startId; i <= endId; i++)`, so three variables get
   three rolls. The test was wrong in the same direction as the first draft and
   was corrected against the source.

**Test evidence**

- 18 tests in `project/tests/core/test_mz_interpreter.cs`, every list in the
  shape the editor writes — most of which were got wrong first, and the file
  says which and how.
- Total **896/896**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **Ten commands of a hundred and fourteen have an effect.** 117, 126, 230,
  231, 232, 235, 351 and 357 are read as text. A game's flow is a chain of
  commands, and this walks the chain for eleven of them.
- Still no renderer, no save path, no input, and a 655 or 657 line is text.

**Three rules that the mutation run found untested, and one of them was a claim
the file had been making wrongly**

1. **A repeat above is not a jump.** The engine's `command413` is `do {
   this._index--; } while (currentCommand().indent !== this._indent); return
   true;` — it writes the index and never calls `jumpTo`, so it clears no
   branch result. Only `command119` calls `jumpTo`, and that clears the result
   of every indent it steps over. **The test file had asserted the opposite
   for two cards' worth of work**, on the reasoning that a repeat above is a
   jump. Reading `command413` settled it: it is not, and a reader that treated
   it as one would clear results the engine keeps.
2. **A jump that points backwards at a label is a loop, in the engine as much
   as here.** `jumpTo` sets the index to the label, `executeCommand` steps on,
   and the jump is met again. A first draft of the label test was shaped that
   way and hung the suite for a hundred thousand steps, which is `checkFreeze`
   doing its work. **This game stores no label and no jump at all** — not one
   118 or 119 in the map read here — so only the shape that ends is asserted.
3. **A jump clears the result of an indent it LEAVES, and nothing else.** The
   engine's walk is `if (newIndent !== indent) { this._branch[indent] = null; }`,
   and every earlier test jumped from indent 0 to indent 0, so no test had ever
   gone through that loop. A reader that dropped the clearing entirely passed
   all seventeen. The movement is now claimed directly, both ways: a jump that
   changes indent clears the indent it left, and a jump that stays on one
   indent keeps what was there.

**Two ways a mutation run lies about itself**

The first run reported five escapes. Three of them were the runner's fault and
not the suite's:

1. **An anchor that is not in the file proves nothing.** Three mutations were
   written from a remembered line and reported `NOMATCH`. A mutation that never
   applied is neither caught nor escaped; it is a hole in the run, and counting
   it as "escaped" would have said a rule is untested when in fact the rule was
   never touched. Every anchor in the second run was read out of the file first.
2. **A mutation that lands on the wrong occurrence of a shape passes for a
   reason that has nothing to do with the rule.** `return false;` appears five
   times in `MzCommands.cs`; replacing the first one changes the refusal of a
   script operand, which a test does not look at, so the mutation survived and
   looked like a gap in the arithmetic. **A mutation has to name the place, not
   the shape** — the same rule that emptied this card's test file twice.


### K-125 Run the list a command calls, and stop at a wait
`DONE`

**The gap that started this** K-124 could walk one list. **An MZ event is almost
never one list**: this game stores eight common events in the one map read here
and reaches them with 117, and a 117 whose list is not available is not a command
a reader may step over — the rest of the list behind it never happens. The same
card took the 230 wait, because a wait is the other thing that stops a list
short of its end.

**What was built**

- `project/src/mz/MzEventRunner.cs`: a run over a list and every list it calls,
  with the engine's own answers for three things that are easy to get wrong.
- `MzInterpreter.Wait` and `PassFrame`: a wait holds the index and a caller
  counts the frames down.
- `MzAction.CommonEvent` and `MzAction.Wait`, and `Result.MissingCommonEvent`
  and `Result.WaitingFrames` as **fields rather than prose**.

**Three rules, each read out of `Game_Interpreter`**

1. **A called list runs to its end before the caller moves on.** `updateChild`
   gives the child its own `update()` and the parent breaks the frame while the
   child is still running, so a caller that carried on straight away would run
   its own next command first. The three tests claim the order of the innermost
   list's command before the middle one's and the middle one's before the
   outermost's.
2. **Every list in a run shares one set of facts.** Both go through the one
   `$gameVariables`. A runner that gave each list its own would have a called
   list change something its caller cannot see.
3. **The event id travels with the call, and only on a map.** `isOnCurrentMap()`
   decides, and that is what lets a common event address "this event".

**A wait is a fourth ending, and it is not a failure**

`command230` is `this._waitCount = params[0]`, and `updateWaitCount` takes one
off it per frame and breaks the frame while it is above zero. **The index does
not move**, so the same command is read again the frame after. A reader that
stepped over the wait would run the rest of a list three frames early. There
are no frames here, so the run is handed back `Waiting` with the count, and the
caller decides when the next frame is.

**What this repository will not do, and says so at every place it happens**

The bounded fixture carries **no `CommonEvents.json`** — the real one is 4.5 MB
and was left out on purpose — so every 117 in this game names an index that
cannot be handed over. The engine's own line is `if (commonEvent)`, and a
missing one leaves the index where it was and carries on. **This reader refuses
and names the index instead**, because a silent step-over would run the rest of
a game's list as if the call had never been there. Reading a called list out of
a fixture that does not contain it would mean writing the game's own scripts.

**Measured on the one map in the fixture, not guessed**

Six event pages, and the six end four different ways: three reach a common event
and name the index, one stops at a 230 and says it is waiting, one is refused
because it **opens with fifty-eight lines of the game's own JavaScript** — a
355 and fifty-seven 655, which this repository does not evaluate — and one is a
single 0 and runs through. A first draft of that test guessed three, one, one
and one, and two of the four numbers were wrong.

**A number read out of prose is not a number**

The test took the common event's index out of the message text with an offset,
and got **76 for 476** because it counted a space twice. The index is now a
field, and the test checks the field and the prose against each other.

**Test evidence**

- **16 tests** in `project/tests/core/test_mz_event_runner.cs`; the interpreter
  suite stayed at 18 with the `Waiting` case added to the walk of a real list.
- Total **924/924**, validator passed, build 0 warnings / 0 errors.
**What a mutation run does and does not prove.** Three runs, 14 rule variants
in all. **Fourteen caught** in the end, and the four that survived the first
pass were each looked at rather than counted either way:

1. **`MzInterpreter.Run` did not read `Waiting` as an ending of its own**, so
   the K-124 path could hand back a wait as if the list were done. A real hole.
   Now documented in the code and claimed by the four-way ending.
2. **`CommandLimit` over a whole run, and over the nested path, was untested.**
   `checkFreeze` counts the run and not one list. Three tests now claim it,
   including a pair of lists that call each other.
3. **`MissingCommonEvent` was only readable out of the message**, and the
   message is the one thing that changes shape. A first draft read the number
   out of the prose with an offset and got 76 for 476.
4. **`PassFrame` on a count of zero.** A first run asked `<= 0` against `< 0`
   and it survived, which was a real gap: nothing held a caller that keeps
   passing frames past the end of a wait.
5. **`MaxDepth` was untested**, and a `replace(..., 1)` mutation hid why: the
   line `MissingCommonEvent = index,` stands in two branches, and the mutation
   hit the depth one, which no test reached. It is a field now, with three tests.

   **And then the tests for it were not tight enough either.** A first draft
   asked only *whether* a self-calling list was refused, and it is refused at
   every limit from zero to eight — so all of it passed with a reader that
   refused one level early, one level late, or twice as deep as it should. The
   thing that tells the levels apart is **how many calls the run managed**,
   and that is what is claimed now: a limit of zero records one action, one
   records two, three records four, and a limit of six is not a limit of three.
6. **The map and the event of a child were untested**, and writing those tests
   found **a real fault in the runner**: it passed the *caller's* map down to
   the child, and read the map and the event off the frame rather than off the
   interpreter. `setup` sets `_mapId` from `$gameMap.mapId()` — the map the game
   is on — and `command117` reads `this._eventId` off the calling interpreter.
   Both are now read from where the engine reads them, and `Result.Child` hands
   the caller the child so the three fields can be checked rather than trusted.

   **And a first draft of that test claimed the event id falls away on the
   second level, which the engine does not do.** `setup` takes `eventId || 0`,
   so a chain on the map carries the same event all the way down. Only a list
   that is *not* on the map passes zero, and there it stays zero. Both
   directions are now claimed, because the first draft got the interesting one
   backwards.

**Three mutations that survived are equivalent mutants, and each one changed
the code rather than the test.**

1. The wait case's own `return false` cannot become `return true` and change
   anything, because `ExecuteOne` ends with `return Stopped == MzStep.Stepped`
   and `Wait` has just set `Stopped` to `Waiting`. That is a dead branch, and
   the code now says so where it would otherwise look like a rule with no test.
2. `WaitFrames <= 0` against `< 0`, and `WaitFrames--` against `-= 2`, differ
   only in states nothing can reach: a wait is never set below zero and
   `PassFrame` clamps it. Both are the same in every state a caller can be in.
3. **The one that changed the design.** `Frame` carried the map and the event
   as well as the interpreter, and a mutation showed that reading them off the
   frame and reading them off the interpreter give the same answer in every
   reachable state — the frame's copy always matched. **Two copies of one truth
   is how the event id came back from the dead in a first draft**, so the frame
   now carries only the interpreter and the question has one place to be asked.

A mutation that cannot be caught because it cannot be reached is not a test
gap, and writing a test for an unreachable state would only have named the
unreachable state.

**Still not true of MZ**

- **Thirteen of a hundred and fourteen commands have an effect.** 126, 231, 232,
  235, 351 and 357 are still read as text, and a 355 or 657 line is text.
- A called list that this repository *has* runs; one it does not have is named.
  The 4.5 MB that would supply the eight is deliberately not in the fixture.
- Still no renderer, no save path, no input and no audio.

### K-126 Change what the party is carrying
`DONE`

**The gap that started this** K-121 to K-125 read MZ data and walked event
lists, and neither needed to know what a game **owns**. A 126 does. It is
`Change Items`, this game's map uses it **eighteen times** over fifteen
different items, and it is the next command with a real effect that can be
checked against the game's own `Items.json` — which the fixture carries, 75 KB
of it.

**Built** `project/src/mz/MzParty.cs`, `MzCommandTable.ChangeItems`, the 126
case in `MzCommands`, and `MzBranchFacts.MaxItems`.

**Four rules, each read out of `Game_Party`**

1. **The count is clamped to ninety-nine, not to the number the event asked
   for.** `container[item.id] = newNumber.clamp(0, this.maxItems(item))` and
   `maxItems` is `return 99` — no argument, no per-item case. **Five of this
   game's eighteen commands ask for 999.** An implementation that added the
   number as written would hand a player a thousand of something the engine
   refuses to hold.
2. **A count that lands on zero is deleted**, not stored as a zero:
   `if (container[item.id] === 0) { delete container[item.id]; }`. A reader
   that kept a zero would answer `hasItem` differently the moment a game asked.
3. **Losing more than there is clamps to zero**, because the clamp is from
   below as well as above. Taking four of one is none, not minus three — and
   adding three back then gives three, which is what the engine's clamp makes
   true.
4. **An id with no item behind it changes nothing and says so.**
   `itemContainer` returns null and `gainItem` returns early, so the engine
   steps over it. This reader says it did not happen, because a game asking
   for an item this repository cannot hand over would otherwise look like a
   game that had it and used it.

**And one that is easy to get wrong in the other direction.** `operateValue`
asks the operand's **kind** first — `operandType === 0 ? operand :
$gameVariables.value(operand)` — and only reads the game for a variable
operand. A first draft read the variable either way, which made every one of
this game's seventeen literal amounts depend on whatever a variable held. And
`operation === 0 ? value : -value` has **no third case**: an operation of
seven removes, exactly as an operation of one does.

**The clamp is invisible in the middle of the range.** A test that only ever
added four to an empty bag would pass with no clamp at all. Every rule here is
asked about at its boundary, and the default is claimed to be the engine's
ninety-nine rather than a number chosen here.

**Measured on the one map in the fixture, not guessed** Eighteen 126s, fifteen
items, five above ninety-nine. **A first draft got nine** — it counted what a
walk reached, and one of the two pages stops at a 230, so the counts are two
different claims: one about the game's data, one about what a reader with
frames sees. Both are now claimed, and the difference between them is the
test.

**Test evidence** 12 tests in `project/tests/core/test_mz_party.cs`; the
interpreter's own suite is unchanged at 18. Total **924/924**, validator
passed, build 0 warnings / 0 errors.

**Mutations** Seventeen rules over two runs. The first run caught seven of
eleven, and **all four that escaped were one gap in one place**: every test
called `GainItem` directly, so nothing proved the interpreter passes the
right four numbers. Writing the test for the wiring found the two faults above
and killed all six rules in the second run, 6 of 6 caught.

**The wiring between the interpreter and the party was untested, and writing
that test found two real faults.**

1. **The party was built without the ids.** `new MzParty(pFacts)` knew no
   items, so every 126 was answered from a list the interpreter could not see
   and every count came back zero — **silently**, with nothing saying why. The
   ids now travel in `MzBranchFacts.KnownItems`, and a facts that carries none
   means the game has not been read.
2. **An empty set of known ids was read as "everything exists".** That is the
   opposite of what it means, and it would have handed a player 999 of an item
   the game never had while looking as if it worked. **Nothing known is nothing
   allowed**, and the code says so.

**Three mistakes of my own, recorded because the next one will make them too.**
A first draft of the wiring test drove the interpreter by hand, and a fresh
`MzInterpreter` has `Stopped` at whatever it starts as rather than at
`Stepped` — so `ExecuteOne` answered false on the very first command and the
loop gave up before it had run anything. It also wrote `new(2, ...)` where the
code belongs: **126 is the command, not the item**, and a page of codes 2, 3
and 4 is a list the engine steps over. And it read `party.Notices` on a party
it had made itself while the interpreter builds its own over the same facts,
so the notice was on the action and not where the test was looking.

**A known gap this card found and now names.** A lone `MzInterpreter` knows no
common events at all, so `HasEffect` is false for 117 and one is **stepped
over like a 0**. That silent step-over is exactly what K-125 was written to
refuse, and it is still reachable through this door. The runner is the door
that names a missing call, and both answers are claimed side by side rather
than one of them being quietly assumed.

**Still not true of MZ** Twelve of a hundred and fourteen commands have an
effect. 231, 232, 235, 351 and 357 are still read as text. No renderer, no
save path, no input, no audio.

### K-134 The twenty-five table rows that have no card behind them
`READY` — board, P1

**This board was lying, and the way it lied was measurable.**

Twenty-five rows in the table have no detail section, and forty-seven numbers
between K-001 and K-133 were never used. Thirty detail sections had no row.
Two rows appeared twice. **An agent reading only the table — which is what
`AGENTS.md` points at first — would have seen the work stop at K-111 and had
no way to know that the RGSS archive, the Marshal reader, the Ruby lexer, the
parser, the value layer, two MZ fixtures and the whole command-execution line
existed.**

**The table is now rebuilt from the details**, so every card that has a detail
section has a row. That is the half that can be repaired from evidence.

**This card is the other half.** The twenty-five rows without a detail section
name work that was done:

| | |
|---|---|
| K-020 | Faithful RM2K/2003 simulation state model |
| K-033 | Visible RM2K map and sprite overlay in the runtime UI |
| K-034 | Safe keyboard movement handoff to RM2K simulation |
| K-035 | Keyboard message dismissal, choice navigation, numeric input |
| K-036 | Deterministic runtime simulation frame count from the virtual clock |
| K-037 | Clickable message, choice and numeric-input presentation controls |
| K-038 | Avoid per-frame choice-control reconstruction in the runtime UI |
| K-039 | Explicit runtime stop control, hide stale presentation controls |
| K-042 | RM2K event-page selection and bounded trigger scheduler |
| K-043 | LMU event-command vectors feeding the native scheduler |
| K-044 | Dispatch action and touch events from player input and movement |
| K-045 | LMU event-page switch and variable conditions |
| K-046 | Selector evaluation for switch B and variable comparisons |
| K-047 | Diagnose unsupported RM2K commands without execution |
| K-048 | Separate LMU move-route and event-command presence metadata |
| K-049 | Bounded RM2K item and actor page conditions |
| K-051 | Deterministic RM2K Timer 1 / Timer 2 conditions |
| K-052 | Bounded JSON simulation save and load roundtrip |
| K-053 | Adaptive application render FPS without changing simulation Hz |
| K-054 | Capability-gated RM2K save and debug tool contracts |
| K-060 | Game compatibility profile schema versioning and validation |
| K-061 | Compatibility report export for GitHub issues |
| K-070 | Faithful-vs-Enhanced profile and integer scaling controls |
| K-080 | RGSS architecture spike after the RM2K/2003 playable milestone |
| K-090 | MV/MZ JavaScript runtime architecture spike |

**Acceptance criteria**

- Each of the twenty-four `DONE` cards gets a detail section carrying **what
  was built, the test evidence, and the commit**. **No section is written
  from the title alone** — a title is a claim and this file does not carry
  claims.
- A card whose work cannot be evidenced from `git log` and the test suite is
  moved to `VERIFY`, not `DONE`, and says what is missing.
- K-080 and K-090 keep `BACKLOG`: both are behind the RM2K playable
  milestone, and both need a decision about JavaScript that is not this
  repository's to make quietly.
- The table and the details are checked against each other by the same
  measurement that found this: **every row has a section, every section has a
  row, and no row is duplicated.**

**Why this card exists rather than a paragraph in the board note**

Because the next agent will read the table. **A board note explaining that
the table is incomplete is a warning; a table that is complete is a fix.**

## Agent maintenance rules
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.


### K-127 Put a picture on the screen and move it off again
`DONE` — pictures, P2, no dependencies

**What it is.** K-121 to K-126 read MZ data, walked event lists, changed what
the party carries. **This is the first command in this game that needs
something other than numbers to have an effect**: nine of them on the one map
in the fixture, on images 1, 86 and 87 — three show a picture, four move one,
two erase one. A reader with no place to put a picture has nothing to say
about them.

**Not 127 and not 128.** Those are Change Weapons and Change Armors, and this
game's `Map002` has **none of them** — no 127, no 128, no 129, no 130. So
carrying them would have meant writing rules no data in this repository can
check, and the fixture has no `Weapons.json` and no `Armors.json` to check
them against. The pictures were chosen because the data is here.

**The rules, each read out of rmmz_objects.js 1.9.1 rather than inferred**

1. **A shown picture is a new object.** `showPicture` makes
   `new Game_Picture()` and puts it in the slot, so a tint, a rotation and any
   movement are gone with the old one. A reader that changed the existing
   picture in place would keep what the engine has just discarded.
2. **A picture id is routed through `realPictureId`, which is not the
   identity.** In a battle a map picture and a battle picture share the
   editor's number. **This game's `System.json` sets `picturesUpperLimit` to
   110**, not the hundred `maxPictures` falls back on, and a reader using the
   hundred would put a battle picture on top of a map one at the wrong offset.
3. **The fourth parameter says where the fifth and sixth are read from.**
   `picturePoint` reads them as numbers when it is zero and out of variables
   when it is not, and a reader that read them as numbers either way would
   place a variable-positioned picture at the variable's own number.
4. **A move sets a target, not a value.** `updateMove` only moves while
   `_duration > 0`, so **a move of zero frames changes nothing at all** and asks
   for no wait even when the game asked for one.
5. **A move on an empty slot does nothing**, and is recorded rather than
   dropped — a game that moves a picture it never showed has a reason a log
   should hold.
6. **Only a move that asks to wait holds the list up.** `if (params[11]) {
   this.wait(params[10]); }` and there is no second one. This game asks for
   the wait on **two of its four** moves and not on the other two.

**A real fault this card found in reading, not in testing.**
`MzCommandEntry.From` handled a Number and took `item.Text` for everything
else, so a JSON **boolean** became the empty string. A 232 carries its wait in
the eleventh slot as a real `true`/`false`, and this game's four moves came
back as four that never ask to wait. No test had noticed, because no test had
read a boolean out of an event list. It is a lost value in a file this
repository claims to read, and it is fixed in the reader rather than worked
around in the test.

**A second one, of my own.** `if (params[11])` is a truth value, and a first
draft called `int.Parse` on it — which throws on the empty string a game may
leave in that slot. Reading it the way the engine reads it is now its own
named method.

**A third, and the worst of the three: a waiting move never arrived.**
`ExecuteOne` did not step the index when a command left the interpreter in
`Waiting`, and a `MovePicture` that asked to wait did `return false`, which
means the same thing. So the next frame read the same 232 again, set the same
twenty frames again, and **a picture that had to move across the screen
waited for ever and never got there.**

The engine has none of this trouble: `command232` ends in `return true`
whatever it asked for, and the wait it set lives in `_waitCount` where the
next command cannot reach it. The index moves and the run stops in two
separate steps now, which is what the engine's frame does — the command is
done, the frame is not. `MzInterpreter` runs 18 and `MzEventRunner` 16 tests
and both are unchanged after it, so this was a fault in a rule nothing had
exercised rather than a change to a rule something had.

**And a fourth, of my own again.** A test that claims a picture is at
`2000, 2000` because the scale is `2000, 2000` is reading the wrong line. The
event says `1, "UI/Status_HelpCollision", 0, 0, 0, 0, 2000, 2000, 255, 0`:
**a place of nothing and a picture two thousand times its own size**, and a
reader that put the scale into the place would have shown it off the bottom
left of the screen. The claim was corrected to the measured value, not
adjusted until it passed.

**A test that counted is not a test that ran.** The first draft's ninth test
was called "every picture command in this game runs" and it counted: three
shows, four moves, two erases, read out of the file without an interpreter in
sight. Four mutation rules escaped because of it. The test that replaced it
builds an interpreter, hands it the frames the two waiting moves ask for,
and checks what the screen holds when the list is through — and it is the
test that found the waiting-move fault.

**And an equivalent mutant that was not equivalent at all, twice.** Removing
`pInterpreter.Wait(frames)` entirely passed the suite, because the first draft
of the walk-through drove the screen's frames from the test's own loop — so a
reader that never waited still moved the picture and ended in the same place.
**It is equivalent for the picture and wrong for the page:** without the wait
the four commands after the move run in the same frame, and a game that fades
a picture out over twenty frames would run the rest of the event while it is
still at full opacity. The test that killed it claims frames and not an end
state: the interpreter is held for exactly the movement's length, the index is
already past the move, the command after it has not run, and it is released
when the frames are counted off.

**A fifth mistake of my own, in the same test.** A 122 written as four
parameters — `1, 0, 0, 5` — has nowhere to read a value from, because
`command122` is `startId, endId, operationType, operandType, operand` and
**the operand is the fifth**. The page was not held by the move failing; it
was held by a command that could not do what the test meant.

**Two more test gaps, found the same way.** A move that does *not* ask to
wait had no test of its own, so replacing the wait condition with `true`
passed — the rule was only ever checked from the side where it says yes. And
`if (params[11])` had no test with a parameter the game wrote as something
other than a boolean, so reading it as `written != ""` passed too. **A rule
checked from one side is half a rule**, and both halves are now tests of their
own: one that the page runs on in the same frame and the picture still moves,
and one that `true` and `1` ask while `false`, `""`, `0` and `no` do not.

**Test evidence** 11 tests in `project/tests/core/test_mz_screen.cs`.
**Total 935/935**, validator passed, build 0 errors.

**Mutations** Twenty-two rules over four runs, and the shape of the escape is
the same one this repository keeps meeting: **every rule that survived was a
rule no test had asked about from the side it fails on.** Run one caught 7 of
11. Run two caught 3 of 7. Run three caught 2 of 4. Run four is the full set
on the finished suite. The three escapes in run two were the waiting-move
fault, the boolean parameter and a direct-call gap; each one turned out to be
a real fault in the reader or in the index, not a weak test.

**What is deliberately not here.** A picture is a name, a place and some
numbers; it is not a texture, and nothing here loads one. The blend mode and
the scale are kept as the numbers the game wrote rather than resolved to a
rendering, because a reader with no renderer must not pretend to have one. The
easing is stored and not applied: `PassFrame` lands the last frame exactly on
the target, as the engine's easing is built to do, and does not walk the
straight line in between — which is stated rather than faked. 233 (rotate),
234 (tint), 236 (weather) and 224 (fade) are the next pictures and are not
here.


### K-128 Measure what this game actually needs from MZ before modelling more of it
`DONE` — measurement, P1, no dependencies

**Why this card exists.** K-127 asked which picture commands come next, and the
answer was: **none of them.** This map uses no 224, no 233, no 234 and no 236.
Modelling them would have been rules no data in this repository can check —
the mistake K-127 already refused to make once.

**So what is left in the one map that is here?** All twenty-two codes in it
are real MZ 1.9.1 commands, measured against the 114 `commandNNN` methods in
`rmmz_objects.js`. Every one of them is now either modelled or refused:

| Code | What it is | State |
|---:|---|---|
| 0, 401, 412, 655, 657 | steps over, as the engine does | K-124 |
| 101, 111, 112, 113, 117, 121, 122, 413, 601-603 | text, branches, control flow, waits | K-123 to K-126 |
| 126, 230, 231, 232, 235 | party, wait, pictures | K-125 to K-127 |
| **351** | **Open Menu** | **the next one** |
| **355** | **Script** | refused, and stays refused |
| **357** | **Plugin Command** | refused, and has to be |

**The finding, and it is about this game rather than about MZ.**
This game ships **52 plugins and all 52 are enabled.** Its eleven `357`
commands call `ItemCombinationMZ`, `DTextPicture` and `HyoujouSelect`, and
their parameters carry the plugins' own options. `command357` is
`PluginManager.callCommand(this, pluginName, params[1], params[3])` — so a
`357` in this game is nine times a request to run somebody else's JavaScript.

**That is refused, permanently and for the same reason `355` is.** Not
because a plugin call is harder, but because executing foreign JavaScript is
the one thing this repository does not do. A reader that implemented `357`
faithfully would be the thing the security contract forbids, and it would
have been faithful to this game and useless to everyone else.

**What a reader can honestly say about a `357`.** The plugin's name, the
command name inside it, the author's own description, and the parameters as
data — all four are readable without running a line of it. What the plugin
*does* is not answerable, and is not guessed. The same shape as `355`: the
text is kept, the running is declined, and the decline is structured rather
than a silent step over, because a step over would make a game look as if it
worked.

**The two commands, and they mean opposite things.**

`351` is run. The engine's `command351` is `if (!$gameParty.inBattle()) {
SceneManager.push(Scene_Menu); Window_MenuCommand.initCommandPosition(); }
return true;` — **one condition, and it returns true either way.** A menu in
a battle is not this command with another scene, it is nothing at all, and a
reader that stopped the run there would leave the commands after it unrun in a
way the engine never does. `MzMenuState` exists so a 351 is not
indistinguishable from a command with no effect: a reader with no screen still
has to be able to answer "did the game open a menu here", and without somewhere
to write the answer down it could only be silent.

`357` is refused, and **the refusal is structured rather than a step over.**
All nine of this map's 357 commands are answered, each naming the plugin and
the command inside it, and each landing on `MzBranchFacts.Notices` where a
caller looking for what went wrong will find it. A silent step would leave a
game that looks as if it works while its crafting menu and its floating text
never appear.

**Test evidence** 4 tests in
`project/tests/core/test_mz_menu_and_plugins.cs`. **Total 939/939**, validator
passed, build 0 errors. The expectations were all measured out of the game's
own files before they were written — nine plugin commands, three plugins, two
351s, three scripts — so no number in this card is a shape this card chose.

**Mutations** Nine rules, **nine caught**, and one of them had to be written
twice: the first attempt replaced a fragment inside an escaped string and left
the file unparseable, so it came back `BROKE` — which counts as caught and
proves nothing. The second attempt replaced the whole notice with a constant
that still compiles, and it failed three named tests, one for each of the
three plugins this map calls. **A mutation that does not compile is not
evidence**, and this project has now been bitten by that three times.

**And the honest limit this puts on the card.** A game with 52 plugins can
have its own logic in them: `ItemCombinationMZ` is a crafting system, and
this game's `355` scripts read `$gameVariables.value(180)` to work out what
was crafted. **UniversalRPG will run this game's MZ event code and none of
its plugin code**, and no bounded slice can change that. What a card can do is
say so where a caller will see it, once, with the numbers, instead of leaving
a reader to discover it by playing.


### K-129 A second MZ fixture, from a game with no plugins
`DONE` — fixture, P1, depends on K-121

**Why a second fixture was needed.** The first, `mz/` (*Stranded with You*),
carries **52 enabled plugins** and **nine plugin commands** on its one map. A
reader checked against it is mostly checked on its refusals, and barely at all
on the event code. That is not a fault in the reader — it is what that game
is. It needs a second game to be a statement about MZ.

**The game.** `CamelliaCoronation-Win`, in `E:/RPGMakerGames`, a free MZ game
put there by the user to work with. Engine **RPG Maker MZ 1.9.1**, measured and
not assumed: both games' `rmmz_objects.js` carry **the same 114
`commandNNN` methods**, with none only in one or only in the other.

**One plugin, and it is in no command.** `extra_party_member`, enabled, with an
empty parameter list. **No `355` and no `357` on any of the nineteen maps** —
counted over the files, and that negative claim is the reason the fixture
exists. A reader that refused nothing would run this game completely, and
there would be nothing to hide.

**What it measures, all of it counted rather than quoted:**

- **2 432 Befehle** over nineteen maps, **1 772 of them run today** and **660
  not**. Every one of the 660 is a real MZ command, not a plugin call.
- The most-used is **401, the line of text, at 938**. Then **101, the dialogue
  block, at 414**, then **505, the move route, at 348**. A first draft called
  the move route the most-used, on the grounds that a game is "mostly made of"
  it, and was wrong by two places.
- **Fifteen variables, numbered 0 to 15, and none above.** **Eight items.**
  One class, one animation, **no switches, no common-event calls, no actor
  references.** A reader that has read this fixture has read everything this
  game refers to — and the first fixture says the opposite, so between them
  they say how far a bounded slice can honestly go.
- **`CommonEvents.json` is 376 bytes and present.** The first fixture had none
  because the original was 4,5 MB, and the runner had to refuse a 117 by
  naming a common event it could not read. That was honest for a gap. **Here
  there is no gap**, and a rule only ever tested against a gap is a rule never
  tested.

**And the codes that are MZ's own and not a plugin call.** 0, 401, 404, 405,
412 and 505 have no `commandNNN` method and are not plugins: the block end, the
line of text, the end of processing, the choice, the end of a branch, the move
route. The engine reads them by position, not by dispatch, and this reader
models them for the same reason. **"It is a number MZ knows" is not the same as
"MZ does it"**, and a 357 shows up in a list of known numbers only because MZ
reserves a slot for plugins.

**The fixture is 537 KB over thirty files**, with no `js/`, no executable, no
image, no audio and no `Tilesets.json` — the reader loads no texture, so a
texture in a fixture is a claim about something nothing reads. **`Skills.json`
is the one file that is not the original**: 104 525 bytes become 1 181, because
**no command on any of the nineteen maps references a skill and the reader
reads none.** Everything else is bytewise identical and the SHA-256 values are
in `project/tests/fixtures/mz_plain/MZ_PLAIN_FIXTURES.md`.

**Test evidence** 6 tests in `project/tests/core/test_mz_plain_fixture.cs`.
**Total 945/945**, validator passed, build 0 errors.

**Mutations** Ten rules, **ten caught, first run, none escaped** — and the way
they were written is the point. **These are claims about data, so the data was
mutated and not the reader**: a 505 turned into a branch, a 401 into a choice, a
101 into something else, a 357 appended to a map, a 355 appended to a map, the
common event list emptied, the common event file deleted, and three rules
against the test's own arithmetic. Every one fell.

**That is the first card in four where nothing escaped**, and the reason is
that the previous three escaped a rule no test had asked from the side it
fails on. A claim about a number is only as good as the test that notices when
the number changes, and a fixture's claim is a claim about a number.

**What this card is for, in one line.** The first fixture says what a reader
must not do; this one says what it can. **1772 of 2432 already run**, and the
660 that do not are the map of the work that is left — led by 505 at 348, 205
at 96, 123 at 42, 213 at 36 and 405 at 36.



### K-131 Walk a character, one step a frame
`DONE` — runtime, P1, depends on K-130

**Why this one, and what it corrected.** K-129's list called `505` the
biggest thing left, at 348. **`command505` does not exist.** `505` is a
nested move-route entry that the editor writes, and a move route reaches the
runtime through **`205 Move Route` — 96 of them**, over fourteen character
ids, of which **60 say `wait` and 36 do not**. The 348 were never event
commands at all.

**The seventeen route codes this game uses, measured over its own
ninety-six routes:** END 96, MOVE_LEFT 74, MOVE_RIGHT 59, MOVE_DOWN 50,
MOVE_UP 45, JUMP 31, CHANGE_SPEED 26, TURN_UP 11, MOVE_BACKWARD 10,
TURN_DOWN 10, TURN_RIGHT 9, TURN_LEFT 7, MOVE_FORWARD 4, WAIT 4,
TRANSPARENT_ON 4, STEP_ANIME_ON 2, STEP_ANIME_OFF 2. **MOVE_LEFT leads and
MOVE_DOWN follows** — the opposite of what "a game mostly walks about"
would guess. MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight diagonal
codes appear **zero** times and are named rather than guessed at.

**Six rules, and every one of them is a place a first reading goes wrong:**

1. **The API is `isMapPassable` and `canPass`, not `isPassable` and
   `checkPassage`.** Those two names come from other RPG Maker engines;
   `checkPassage` has **zero** occurrences in 1.9.1. A reader built from
   memory would have compiled and tested nothing real.
2. **MZ has two coordinates.** `_x`/`_y` is the tile, `_realX`/`_realY` is
   where the character is drawn, and on a successful step the drawing
   position is set to **one tile behind** —
   `this._realX = $gameMap.xWithDirection(this._x, this.reverseDir(d))`. A
   reader with one coordinate snaps, and a snapped character teleports.
3. **`reverseDir` is `10 - d`, not `(d + 4) % 4`.** The first is right for a
   0..3 numbering and wrong for MZ's 2/4/6/8: `reverseDir(2) = 8` is **Up**,
   not Down. A first draft placed every "one tile behind" position **in
   front** of the character, so every character walked away from where it
   was going.
4. **`isStopping` is `!isMoving() && !isJumping()` — two terms, and a first
   draft added a third.** It wrote `... && !Waiting` and **every route with a
   `ROUTE_WAIT` in it ran backwards**, re-issuing one step for ever. A
   character waiting is a character that has arrived.
5. **A refused step still turns.** `moveStraight` turns in both branches, and
   on failure it calls `checkEventTriggerTouchFront`. A character that bumps
   a wall faces the wall, and that facing is what triggers the action
   button.
6. **A route is a queue of single steps, not a batch.**
   `updateRoutineMove` hands a command over only when the character has
   arrived, so **five steps into open floor is five frames**. A reader that
   ran the list in one call would teleport the character five tiles.

**And a product fault with a wider reach than this card.** A 205's second
parameter is a **nested object**, and `MzCommandEntry.From` turns every
parameter into a string — anything that is not a number or a boolean became
`item.Text`, which for an object is `""`. **Every move route in every game
came back empty, and the reader could not have said why.** `MzJson.Write`
now writes a value back out, because **a reader that cannot write a shape
back has already half-lost it.**

**Two more faults, found by the same tests:** `Truth` read a boolean out of
`Text` where the parser puts it in `Boolean`, so **all three flags of all
ninety-six routes came back false** and not one page was ever held by its
route; and `From` read a route's `code` out of `Text`, which is empty for a
number, so **every route code came back 0 — which is END** and all
ninety-six routes did nothing while looking perfectly plausible.

**Test evidence** 7 tests in `project/tests/core/test_mz_move_route.cs`.
**Total 957/957**, validator passed, build 0 errors.

**Mutations** Fourteen rules, thirteen caught in the main run. The one the
run reported as escaped — "a route that is not forced hands out no steps" —
**was not escaped**: an isolated second run killed it, three of seven tests
down, with the tree bytewise unchanged. It is recorded here as
**fourteen of fourteen**, because a number that was not checked is not a
number that was counted.

### K-130 Send the player somewhere, and hold the page until they arrive
`DONE` — runtime, P1, depends on K-124

**Why this one and not the biggest.** The 660 commands that do not run yet
are led by `505` at 348 and `205` at 96, and both are movement — both need
`Game_Character`, a move route decoder and a passability model, which is
three cards before the first of them can be tested. **201 is 33 commands and
needs none of that**: it changes where the player is, not how they got there,
and it is on sixteen of the nineteen maps.

**And it is the first command in this reader that is neither a change nor a
number of frames.** K-125 made a run wait for frames, K-127 for a picture's
movement, and a 201 for **a condition**:

```
Game_Interpreter.prototype.command201 = function(params) {
    if ($gameParty.inBattle() || $gameMessage.isBusy()) { return false; }
    …
    $gamePlayer.reserveTransfer(mapId, x, y, params[4], params[5]);
    this.setWaitMode("transfer");
    return true;
};
```

and `updateWaitMode` answers `waiting = $gamePlayer.isTransferring()`. **A
condition wait has no length** — a caller passing frames cannot end it, and a
reader that counted them would let the page on with the player still on the
old map. `MzWaitMode` is a third shape next to a 230's frames and a 232's
movement, and the engine's own modes are `message`, `transfer`, `scroll`,
`route` and `until`.

**Four rules, each a place a first reading goes wrong:**

1. **A transfer is reserved, not carried out.** `reserveTransfer` writes
   `_transferring = true` and the new map and position and **changes nothing
   the player can see**; `performTransfer` is what moves them. Applying it
   while reading the command would move the player before the commands after
   it had run — the difference between a game that leads the player and one
   that teleports them mid-sentence.
2. **The engine returns false and transfers nobody** in a battle or with a
   message on the screen. That is neither a wait nor a finish: the index
   stays, and the transfer happens in the frame in which the message closes.
   `MzStep.Refused` says exactly that and is not dressed up as either of the
   other two.
3. **The direction is set on the way, not on the reservation**, because
   `performTransfer` is what calls `setDirection`. A player that turned one
   frame early would face a map they are not on yet.
4. **A map this reader has not read is named and the player stays put.**
   `command201` does not check and `$gameMap.setup` fails further on where
   nobody is looking. **Half-applying it is worse than not moving** — the
   caller would see a position and no file behind it.

**And the numbers are this game's: 33 transfers over sixteen maps, all with
the first parameter at zero**, so the place is written out rather than read
from a variable. A reader that always looked in the variables would send
every player in this game to variable four.

**A C# trap this card walked into and measured.** `$"Map{i:03}.json"` with
`i = 1` produces **`Map13.json`**. In an interpolated string `i:03` is read as
a fill character of `0` and a **precision** of `3`, and a whole number with a
precision is padded on the **right**: 1 becomes "13", 2 becomes "23". Every
file was missing and the only thing that said so was the reader's own error
about a file ending mid-value. `ToString("000")` is the right spelling, and the
reason is in the test so the next card does not walk into it again.

**Test evidence** 5 tests in `project/tests/core/test_mz_player_transfer.cs`.
**Total 950/950**, validator passed, build 0 errors.

**Mutations** Nine rules, **nine caught, first run, none escaped.** Each of the
four rules above was broken in the place it actually fails: a reservation that
moves the player, a turn that happens one frame early, a message that no longer
refuses, a run that carries on past a refusal, a condition wait counted down in
frames, a transfer that holds its page for twenty of them, a condition that
never stops being met, a missing map that is carried out anyway, and a place
always read from the variables.

**What is not here.** No map is loaded and no tile is drawn: a transfer is a
position, not a picture of one, and the direction and fade type are kept as
the numbers the game wrote. The other four wait modes need a scrolling map, a
moving character and a plugin callback, and none of them is modellable here.


### K-132 Read a line of text, and every code in it
`DONE` — runtime, P1, depends on K-129

**The biggest thing left in this fixture: 938 lines, and every one carries
exactly one parameter.** But nothing about it is a rendering detail, and
that is the finding: **a line of dialogue is mostly not words.**

**Two passes, two rule sets.** Pass one, `convertEscapeCharacters`, rewrites
in three steps — every backslash becomes the escape character; **two escape
characters put one backslash back**; and the variable, actor, party and
currency codes are filled in, **the variable one in a loop**. Pass two, the
drawing loop, treats **every character below 0x20 as a control character**
and never puts it in the output. A reader that does them in one shows a
different line than the game does.

**Three classes, and only one of them is text:**

- **In the text:** `\V[n]`, `\N[n]`, `\P[n]`, `\G`.
- **Not in the text, and never shown:** `\|`, `^`, `!`, `>`, `<`, `$`.
  **A reader that emitted them would put a `|` in the middle of a
  sentence.**
- **Neither text nor pen, and this reader names them:** `\C[n]`, `\I[n]`,
  `\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`. **A reader with no
  renderer cannot draw them, and it says so rather than dropping them in
  silence.**

**This game's own numbers, measured over the files — and two of my own
measurements were wrong before they were right.**

| | zuerst behauptet | gemessen |
|---|---:|---:|
| Zeilen mit `\C[n]` | 0 | **19** |
| Undrawable insgesamt | 0 | **57** |
| Leere Zeilen | — | **14** |
| Code-Klassen | 2 | **5** |

`\C[3]` 19×, `\C[0]` 19×, `\I[177]` 19×, `\!` 3×, `\|` 3× — **19 Zeilen
mal drei Codes, das sind die 57.** Eine Zeile wartet **dreimal**: `\|.|\|.|\|.`
sind drei Entscheidungen und nicht eine.

**Der Fehler, der zweimal passierte.** Ein Scan dieser Zeilen fand den
Buchstaben `C` 53-mal, `N` 38-mal, `V` 22-mal und `P` 11-mal, und eine erste
Lesart hielt sie für Auszeichnungen. **Es sind Wörter**: „SEND **C**OUT!!",
„\* **N** om\*", „Valuable **V**egetables". **Ein Code ist zuerst ein
Backslash und dann ein Buchstabe** — wer nach einem nackten Großbuchstaben
sucht, liest Englisch. **Genau dieser Fehler ließ mich zuerst „keine Farben"
behaupten, und die echten Dateien sagten neunzehn.** Der Test, der es
bemerkte, las dieselben Dateien und riet nicht.

**Was nicht hierher gehört.** Eine Zeile wird **gelesen und behalten**, nicht
gezeichnet: `MzBranchFacts.Message` hält Wortlaut, Wartungszahl, und alles,
was dieser Leser nicht zeichnen kann. **Kein Textfenster, kein Renderer.**

**Und eine Aussage, die älter ist als diese Karte.** Ein Test aus K-124
behauptete, ein 401 werde *übergangen*, weil die Engine keine Methode dafür
hat — und das stimmt und stimmt weiter. **Der Leser liest es trotzdem**, weil
er nach einer anderen Frage gefragt wird: *was hat das Spiel geschrieben?*
**„Hat die Engine eine Methode" und „was steht in den Daten" sind zwei
Fragen mit zwei Antworten**, und sie zu vermischen bringt entweder ein
laufendes Spiel zum Stehen oder behauptet, ein Spiel habe keinen Text.

**Test evidence** 6 tests in `project/tests/core/test_mz_message.cs`, and one
K-124 test rewritten to say both answers.
**Total 963/963**, validator passed, build 0 errors.

**Mutations** Nine rules. The first run caught seven and reported two
escaped — **and both were a fault in the rules, not in the reader.** One
mutated a code's handling into an equivalent that changed nothing, and one
mutated a list entry that the test did not actually reach. Isolated and
rewritten, **nine of nine**. The second is the better story:

> **Die Liste der Zahlen ohne `commandNNN` war geraten, und sie war falsch.**
> Sie behauptete, `601`, `602` und `603` hätten keine Methode. **Sie haben
> eine** — `command601`, `command602` und `command603` sind drei der 114.
> Und sie behauptete „178 reservierte Nummern", wo die Liste in K-122 in
> Wahrheit **die 114 Methoden** ist. **Neun Zahlen haben keine Methode, und
> alle neun liegen außerhalb dieser 114** — `0`, `401`, `404`, `405`, `412`,
> `505`, `604`, `605`, `657`. Der Test sagt es jetzt ausdrücklich.

**Das ist der vierte Name in vier Karten, der aus dem Gedächtnis kam und in
der Engine nicht existierte** — nach `checkPassage`, `isPassable` und der
`reverseDir`-Form. **Gemessen wird, nicht erinnert.**

### K-133 A 101, and everything it swallows
`DONE` — runtime, P1, depends on K-132

**The first command in this reader that eats other commands.** And that one
fact reorganises K-132: `command101` is

```
if ($gameMessage.isBusy()) { return false; }
$gameMessage.setFaceImage(params[0], params[1]);
$gameMessage.setBackground(params[2]);
$gameMessage.setPositionType(params[3]);
$gameMessage.setSpeakerName(params[4]);
while (this.nextEventCode() === 401) { this._index++; add(…); }
switch (this.nextEventCode()) {
    case 102: this._index++; this.setupChoices(…); break;
    case 103: this._index++; this.setupNumInput(…); break;
    case 104: this._index++; this.setupItemChoice(…); break;
}
this.setWaitMode("message");
return true;
```

**So a line of dialogue is never dispatched.** There is no `command401` to
dispatch it to — `nextEventCode()` looks one ahead and the 101 steps the index
over each line itself. **Every one of this game's 938 lines belongs to a 101
and to nothing else**, and a reader that ran a 401 as a command of its own
would be running 938 commands the engine never runs.

**This game's numbers, measured over the files:** 414 dialogues, one to four
lines each — **118 with one, 130 with two, 104 with three, 62 with four** —
and the total is exactly 938. **Eight are followed by a 102**, six under a
one-line dialogue and two under a two-line one; there is no 103, no 104, no
403 anywhere in nineteen maps. Commands eaten: **112, 134, 106, 62** — 1360
rather than 414 + 938, because the eight choices are inside it.

**Three rules, and a fourth that is only visible in this game.** A dialogue
that is already up is refused — and `isBusy()` is **text or choice or number
or item**, so a 101 behind an unanswered choice is refused as firmly as one
behind a line. Exactly **one** of 102, 103 and 104 is taken, and it is the one
directly after the last line: the `switch` runs once, so a 102 that is not
right there is reached later as a command of its own. **And it always ends in
a wait**, `setWaitMode` being outside the `switch`, so a dialogue with no
choice holds its page all the same.

**268 of the 414 name somebody and 146 name nobody** — Camellia 32 times,
Mary 27, and `???` 45 times, which is the editor's placeholder for a person
not yet named. All 414 have five parameters. **Not one asks for a face**, so
this game has a name box that is filled in and no portrait beside it.

**`102` is not `405`, and that is the fifth name in five cards that had to be
measured.** `ShowChoices` has meant 405 since K-132 — the choices as data —
and the follower was compared against it, so **not one of this game's eight
choices was ever found**: 1352 commands instead of 1360, and a dialogue that
ended on a choice the engine would have taken. **Four runs**, because the
tests that failed were the ones checking a sum.

**`params[0]` is an array, not a bar-separated string.** A first draft wrote
`params[0].split("|")` — the shape an older RPG Maker used — and would have
read one option that reads `["Yes", "No"]`, brackets and comma included, and
compared the cancel number against the wrong length. **And
`cancelType = params[1] < choices.length ? params[1] : -2`**: a cancel number
that is not below the number of choices becomes "no cancel". This game's eight
are all two options with a cancel of 0 or 1, **so the rule never fires in the
real data** — which is why it had to be built by hand.

**`params[1] || 2` is 2, and a written zero is 2 as well** — 0 is falsy in
JavaScript. A 104 with no category and a 104 with `0` both get the whole
party, and a reader that defaulted to 0 would offer the player nothing.

**And the index moves by what was eaten, not by one.** `command101` steps the
index once per line and once for the 102 its switch took, and then
`executeCommand`'s own `this._index++` steps it once more — so a 101 that is
the last thing in a list leaves the index **one past the end**, and no other
command in this reader can, because every other one moves it by one.

**The off-by-one that cost the most.** Three times, in three different files,
an index that was one out was blamed on the nearest thing rather than
measured. The first draft's `nextEventCode(pCommands, i)` with `i` already one
past the 101 **started the read at the second line** — 524 lines instead of
938. The test helper's `k += eaten` was then "fixed" to step one further, on
the strength of a distribution that was one bucket out, and **the numbers got
worse** — 88 and 102 where the files say 118 and 130. **The fault was never
in the test.**

**And a guard with no test.** `ExecuteOne` had a bounds check that a mutation
switched off and every test passed, because `IsRunning` is
`Index < _commands.Count` and the guard was **never asked**. The repair was
not a test for it but **its removal** — the case is handled one level up, in
`Run`, which now checks before it enters its loop and says where the index was.
**A second check that can never fire is a claim a reader will believe and
nobody can prove.**

**Test evidence** 9 tests in `project/tests/core/test_mz_dialogue.cs`, plus
three rewritten in K-124's and K-132's files.
**Total 972/972**, validator passed, build 0 errors.
**Mutations** Twelve rules over four runs. Every escaped rule turned out to be
either a broken rule or a test that could not reach the thing it mutated; two
of them found real product faults — the 102 read from the wrong command, and
a 103/104 read as a list of options.


## WOLF Ton: drei Kanaele, und eine Null, die zwei Bedeutungen hat

**WOLF hatte keinen Ton und der Tonschritt in einer Laufbahn wurde abgelehnt.** Das war

die ehrliche Antwort, solange es nirgends hingesellt werden konnte — und ein Spiel mit

einem Tonschritt in jedem Kampf war bisher ein Spiel, in dem jeder Kampf an der Tonzeile

endete.



**Drei Kanaele und nicht einer.** BGM ist Hintergrundmusik, BGS ist Hintergrundgeräusch —

die Materialliste nennt es ein Umgebungsgeräusch und nennt Regen, Wind und einen Herzschlag

als seine Verwendung — und SE ist ein Soundeffekt, der nicht wiederholt. Ein Leser mit einer

Liste hätte einen Herzschlag das Stadttema ersetzen lassen.



### Die drei Regeln, die eine ganze Karte tragen



**Eine Lautstärke von 0 ist unter der alten Regel Standard und unter der neuen stumm.**

Die Materialliste sagt beides, für BGM wie für SE: 100 ist die normale Lautstärke, 1 bis 100

ist leiser, über 100 ist lauter, und ein Eintrag von 0 wird ebenfalls in normaler Lautstärke

abgespielt. Die Spielkonfiguration sagt dazu, dass vor Version 3.681 die 0 auf 100 umgerechnet

wurde und die Einstellung sie nun bei 0 lässt — und der Fall, für den es die Einstellung gibt,

ist ein Hintergrundgeräusch mit Mischung 0 für interaktive Musik. **Welche der beiden ein

Spiel benutzt, ist eine Einstellung, und dieser Leser hat keine** — er meldet also eine Null als

Null und benennt sie als den mehrdeutigen Wert, der sie ist.



**Die Zeit eines Effekts ist eine Verzoegerung und die eines Musikstücks eine Einblendung —

und das sind nicht dasselbe Feld.** Die Materialliste sagt, die Einblendzeit des Tonbefehls

werde zu *die Wiedergabe verzögern* für einen Soundeffekt, und nennt die Einheit: sechzig Bilder

sind eine Sekunde. Ein Leser, der eine Verzoegerung als Einblendung behandelte, hätte den

Effekt leise beginnen und lauter werden lassen — und die Verzoegerung eines Sekunde als

Millisekunden gelesen wäre ein Sechzigstel dessen, was das Spiel verlangt hat.



**Der Dateiname steht in den Einzel-Byte-Argumenten, und die Zahlen in den Vier-Byte-Argumenten.**

Ein Laufbahnschritt hat einen Typ, eine Anzahl Vier-Byte-Werte, diese, eine Anzahl

Einzel-Byte-Werte und diese — und ein Dateiname ist Text, also steht er in der zweiten Liste.

Ein Leser, der den Namen in der ersten gesucht hätte, hätte drei ganze Zahlen gefunden und

sich gefragt, warum kein Titel laeuft.



### Was der Test fand



**`Clear() leerte die Kiste und liess das Radio laufen.** Figuren, Wege, Passierbarkeit und

Partei werden alle mitgenommen — und der Ton nicht. Ein neues Spiel, das die Musik des letzten

behaelt, oeffnet seinen Titelbildschirm mit dem Thema dessen, was vorher geladen war, und

**nichts anderes auf dem Brett haette es gemerkt**, weil die Figuren fort waren und es keine

Figur gibt, die falsch aussieht.



**Test evidence** `test_wolf_audio.cs` (11).

**1324/1324**, Validator grün.

**Mutations** 11 Regeln über zwei Läufe, **11 von 11 gefangen** — darunter der Standardwert 100,

das Beibehalten der Null unter der neuen Regel, der Name aus den falschen Argumenten, BGS und SE

im selben Kanal, die Verzoegerung als Einblendung, SE bekommt die Einblendung der Musik, ein

leerer Name als Klanger, ein abgeschalteter Kanal, der sammelt, und der Tonschritt, der die

Laufbahn beendet.

## WOLF Laufbahnen aus einer Datei: zwei Befehle, die die VM kannte und der Leser nicht

**Die VM hatte `MoveRoute` und `WaitUntilRouteDone` im Dispatch und in der Enum, und

`ParseOpcode` hatte fuer keinen der beiden einen Namen.** Eine Kartendatei, die

`"op": "move_route"` schrieb, kam als `Unknown` an — und `Unknown` lehnt die VM ab.



**Also stand eine im Editor geschriebene Patrouille still, und nirgends stand, warum.**

Das ist der Fehler, den kein Test gefunden haette, **weil jeder andere Test seinen Befehl von

Hand gebaut hat** — ein handgebauter Befehl hat Figur und Laufbahn schon gefuellt, und nur

der Dateipfad muss sie fuellen.



### Die drei Regeln, die der Leser jetzt beachtet



**Die Figur und die Schritte sind der Unterschied zwischen einer Patrouille, die geht, und

einer, die nicht geht.** Ein Leser, der die Figur nicht fuellt, erreicht die VM mit der

Anweisung, eine Laufbahn ohne Figur zu starten — und die ist sofort fertig und meldet sich

fertig. Das Event laeuft, die Laufbahn ist fertig, und der Waechter bewegt sich nicht.



**Die Schrittnamen sind die der Tabelle und nicht eigene.** Eine Blickrichtung ist `FacingUp`

und nicht `TurnUp`, und ein Schritt, der eine Variable setzt, ist `AssignToVariable` und nicht

`SetVariable`. Ein Leser, der sie umbenannt haette, wuerde auf ein `facing_up` der Datei mit

einem unbekannten Schritt antworten.



**Ein unbekannter Name ist 0xFF und nicht 0.** Die verifizierten Schritttypen laufen von 0x00

bis 0x3A, und 0x00 ist ein Schritt nach unten — also wuerde ein Tippfehler im Schrittnamen

eine Figur eine Kachel nach sueden schicken, und das Spiel wuerde richtig aussehen, bis zum

Tag, an dem es das nicht mehr tut. Ebenso ist ein unbekannter Modus `Custom` und nicht 0,

denn 0 heisst *sich nicht bewegen* — ein Leser, der dorthin zurueckfaellt, stellt eine

Patrouille still, ohne Fehler und ohne Bewegung.



**Test evidence** `test_wolf_route_from_file.cs` (9), gegen eine echte Kartendatei auf der

Platte und nicht gegen ein gebautes Objekt.

**1333/1333**, Validator gruen.

**Mutations** 12 Regeln ueber zwei Laeufe, **12 von 12 gefangen** — darunter die beiden

Opcode-Namen, die fehlten, die nicht gelesene Figur, die nicht gelesene Laufbahn, die

Schrittnamen der Tabelle gegen geratene, der unbekannte Name als Schritt unten, der

unbekannte Modus als Stehen, das nicht gelesene Wartezeichen, die Flagge als jede Zahl und

die ganz verwerfenen Argumente eines Schritts.

## WOLF Common Events: ein Aufruf, der zurueckkommt

**Der Binaerleser dekodierte Typ 300 vollstaendig** — inklusive Argumentblock und dem

Flag fuer den Rueckgabewert — **und die Opcode-Enum hatte keinen Wert dafuer.** Also

konnte ein Spiel, dessen Events ein Common Event aufrufen, den Aufruf gar nicht

ausfuehren — **und jeder WOLF-Shop ist aus Common Events gebaut**: initialisieren, Ware

hinzufuegen, Laden ausfuehren.



### Die vier Regeln, die die Karte tragen



**Ein Aufruf teilt den Zustand und kopiert ihn nicht.** Ein Common Event, das eine Variable

setzt, aendert das Spiel — das ist der Zweck des Aufrufs —, also bleiben Brett, Variablen

und Schalter, wo sie sind, und nur die Fortsetzungsstelle kommt auf den Stapel. Ein Leser,

der den Zustand kopiert haette, haette ein Common Event, das der Held ein Item gibt, das

die Mannschaft nie bekommen hat.



**Das Ende eines Common Events setzt den Aufrufer fort, und nur das Ende des aeussersten

Programms beendet die VM.** Ein Leser, der beides als Ende behandelte, haette den ersten

Aufruf eines Spiels das Spiel sofort totstoppen lassen.



**Die Tiefengrenze ist die Wacht gegen ein Common Event, das sich selbst aufruft.** Ohne sie

laeuft die VM, bis der Prozess endet — **und das sieht ein Spieler als Spiel an, das auf

einer Kachel einfriert und das niemand sinnvoll melden kann.**



**Null ist der Held und kein Event.** WOLF zaehlt die Datenbank-Ids ab null, also ist ein

Aufruf der 0 ein Aufruf des Spielers — und ein Leser, der das als "kein Event angegeben"

behandelte, wuerde aus dem richtigen Grund ablehnen.



### Der Befund, den die Fehlersuche ergab



**Ich habe zehn Minuten an einem Test gefeilt, der keine Codefehler fand, weil es keine gab.**

Ich wollte eine Figur gehen sehen, die eine Common-Event-Laufbahn ging, und sie stand still.



**Das Brett allein ging, die VM nicht — und ohne jeden Aufruf.** Die Route startete, und die

VM erreichte das Ende des Events im selben Tick; ab dem naechsten Tick ist die VM

`Completed`, **und ein `Completed` tickt das Brett nicht.**



**Also gilt: eine Laufbahn in einem Event, das sofort endet, geht nicht** — **und das ist

richtig.** WOLFs eigene Common Events sind nicht so geschrieben: eine Laufbahn, auf die es

ankommt, wird gefolgt von einem Warten, oder das Event laeuft weiter, oder die Laufbahn

startet ein Parallelereignis, das nie endet. **Ein Test, der hier das Gehen erwartet haette,

haette eine Form gemessen, die kein Spiel benutzt.** Der Test wartet jetzt auf die Laufbahn —

**und das Warten ist das, was einen Schritt sichtbar macht.**



**Test evidence** `test_wolf_commonEvent_call.cs` (10).

**1343/1343**, Validator gruen.

**Mutations** 10 Regeln ueber zwei Laeufe, **10 von 10 gefangen** — darunter das Ende, das

auch den Aufrufer beendet, die Rueckkehr, die kein Programm setzt, die Fortsetzung, die beim

Aufruf anfaengt statt danach, die ungepruefte Tiefe, die gesuchte Null, die Meldung ohne

Nummer, das leere Event, das suspendiert, der Stapel beim Neustart und der Aufrufbefehl, der

aus der Datei nicht ankommt.

## WOLF Map-Event-Aufrufe: eine Zahl, zwei Arten, und ein bewusstes Schweigen

**Der Binaerleser dekodierte Typ 210** und unterschied die beiden Arten an der Nummer: unter

500.000 ist es ein Map-Event, ab 500.000 ein Common Event, **und nur dann traegt der Aufruf

Argumente.** Die Enum hatte dafuer keinen Wert — also konnte ein Map-Event, das ein anderes

aufruft, den Aufruf gar nicht ausfuehren.



### Die Regel, die man nicht vermutet



**Ein Event, das es nicht gibt, wird ignoriert — und nicht als Fehler gemeldet.** Die Hilfe

sagt das in einem Satz: イベントが存在しない場合は無視されます, **und der Grund ist, dass ein Spiel

ein Event loescht und den Aufruf stehen laesst.** Ein Leser, der dort scheiterte, haette ein

Spiel, das an einem Aufruf zu einem vom Autor entfernten Schatzkasten tot stehen bleibt, mit

einer Meldung, mit der niemand etwas anfangen kann. **Das ist die einzige Stelle in dieser VM,

wo ein Fehlendes absichtlich kein Fehler ist — und der Grund steht hier, weil der Reflex

ablehnen ist.**



### Die Self-Variablen, und der Fehler, den der Test fand



**Eingabe 1 ist Self 0, Eingabe 2 ist Self 1, und Text-Eingaben beginnen bei Self 5.** Map-Self

liegt bei 1.100.000, Common-Self bei 1.600.000 — **beide Bander existierten bereits im Modell

und die VM benutzte keines von beiden.**



**Und dann der Fund:** `ApplyOperator` schrieb mit `_variables.SetByReference` direkt in die

Bander, **waehrend das Lesen ueber den neuen Durchlass lief.** Also schrieb ein Common Event,

das sein eigenes \cself[0] zuwies, in ein Band fuer sich — **und las es als null zurueck.**

Der Test hat es gefunden, weil er eine Regel prueft, die ich am wenigsten belegt hatte.



**Ein Map-Event hat keinen eigenen Rahmen: seine Self-Variablen sind die des aufrufenden

Events.** Ein Leser, der ihm einen eigenen gab, wuerde einer Kette von Map-Events die Werte

verlieren, die das erste bekommen hat.



**Test evidence** `test_wolf_event_call.cs` (10).

**1353/1353**, Validator gruen.

**Mutations** 11 Regeln ueber zwei Laeufe, **11 von 11 gefangen** — darunter der nicht

abgezogene Versatz, die beiden Tabellen als eine, die nicht landenden Eingaben, ein Rahmen pro

Spiel statt pro Aufruf, das fehlende Spiel-Self, das fehlende Event, das scheitert, der

nach der Rueckkehr stehen bleibende Rahmen, die negative Id als gesuchte, der Schreibweg, der

die Self-Baender umgeht, und der Befehl, der aus der Datei nicht ankommt.


## MZ: 123 Control Self Switch, 129 Change Party Members, 213 Show Balloon Icon

**Quelle.** Die offizielle MZ-Hilfe, nicht EasyRPG: **EasyRPG 0.8 hat weder
`SelfSwitch` noch `Balloon`** -- **diese drei Befehle sind MZ eigen**, **und
`123 Control Self Switch` steht in keinem anderen Quelltext, den wir haben.**

**Und die Hilfe sagt, was die Parameter sind.** `123`: *Self Switch — Specify
the target self switch (A through D). Operation — Specify the value (ON/OFF) to
store in the switch.* `129`: *Actors — Select the actor to change. Operation —
Select which operation to perform (Add/Remove).* `213`: *Character — The
display location will be based on the position of the player or event.*

**Und die gemessenen Formen sind `123 ["A", 0]`, `129 [2, 0, false]` und
`213 [-1, 2, false]`** -- **ein Buchstabe, eine Zahl und eine negative.**

**Drei Befunde, die ein naiv lesender Befehlssatz falsch macht.**

1. **Der erste Parameter von `123` ist ein Buchstabe.** `At(pCommand, 0)` liest
   Zahlen und gibt fuer `"A"` deshalb **0 zurueck** -- **und 0 ist Schalter A,
   und das geht fuer B, C und D gleichermassen falsch.** Gelesen wird jetzt der
   Buchstabe, und Kleinschreibung zaehlt als derselbe Schalter.
2. **Minus eins ist der Spieler, und keine Darsteller-Id.** Gemessen: `-1`
   **15 mal**, Figurnummern **21 mal**. Der Spieler und die Figur tragen
   denselben Zustand, **und sie sind zwei Typen**, **und ein Leser, der dem
   Spieler keinen Ballon gab, verlor fuenfzehn von sechsunddreissig Befehlen
   des Spiels vor uns.**
3. **`return false` ohne zu warten ist ein Fehler, und kein Zufall.** `TryExecute`
   gibt `false` zurueck, und das heisst *die Liste wartet noch* -- **und dann
   steht der Index still**, **und der Runner liest denselben Befehl noch
   einmal, einmal pro Bild**, **und der Ballon wird dabei jedes Bild neu
   gesetzt**, **und seine Uhr steht bei 60**, **und `MZ` friert nach 100 000
   Befehlen ein.**

**Und der Ballon hatte keinen Takt.** `MzScreen.PassFrame` hat keinen Aufrufer
in `src/`, und `TickBalloon` auch nicht -- **das Icon blieb ueber einem Kopf
stehen, und die Hilfe nennt dieses Feld *wait for the icon to disappear*.** Der
Takt haengt jetzt an `MzEventRunner.Run`, an derselben Stelle, an der ein
Befehl ein Bild verbraucht, **und `MzBranchFacts.TickBalloons` zaehlt Spieler
und Figuren gemeinsam.**

**Und die Dauer ist eine Zahl, die dieses Repository gewaehlt hat.** Die Hilfe
nennt drei Einstellungen -- Figur, Icon, Warten -- **und keine vierte, und
keine Dauer irgendwo im Befehl.** Ohne eine Dauer waere *wait for the icon to
disappear* eine Wartezeit, die nie endet. Sekunde, und die Konstante sagt es
zweimal: einmal am Aufrufer und einmal bei sich.

**Und `MzCharacter.BalloonIcon` stand auf 0, und nicht auf -1.** Null ist das
erste Icon der Editorliste, **und ein Feld, das auf 0 startet, antwortet
"ja, da ist eins"** -- **und eine frisch geladene Figur traegt dann das erste
Icon, von dem Bild an, an dem die Karte geladen wurde.** Das hat eine lebende
Mutationsregel aufgedeckt.

**Test evidence** `test_mz_party_and_switches.cs` (4), davon einer ueber den
Runner statt ueber `TryExecute`, **weil die drei anderen den Verdrahtungspunkt
nicht erreichen.**

**Und der gemessene Bestand.** `CamelliaCoronation-Win`: **2436 Befehle, davon
1673 ausfuehrbar, 763 nicht** -- **vor diesem Schritt waren es 1570 und 866.**
Die 103 neuen sind genau die Summe der drei Befehle im Spiel.

**Und was danach noch offen ist, gemessen.** Nach Abzug der Trenner `0` und
`505` und der vier dokumentierten No-ops `402`, `404`, `405`, `412` bleiben
**17 Codes**, **und die groessten drei sind `221` und `222` mit je
**null Parametern** -- **das ist `Erase Picture` und `Erase Event`, und beide
sind ein Befehl ohne Argumente** -- **sowie `203` mit zehn verschiedenen
Formen** -- **das ist `Change Image`, und ein Bildwechsel hat mehr Parameter
als alle drei anderen.**

**Mutations** 11 Regeln, **11 von 11 gefangen** -- darunter die vier, die
nur ein Test ueber den Runner findet: **der Ballon ohne Takt**, **die Figur
mit Icon 0 von Anfang an**, **das `return false` ohne Warten**, **und der
Selbstschalter, der alle vier Buchstaben auf A legt.**

## MZ: 221 Show Animation und 222 Erase Event

**Die Quelle ist wieder die offizielle Hilfe, und die ist hier
ungewoehnlich klar.** `222 Erase Event`: *Temporarily removes the event
currently being run. **There are no parameters to set.** The event will
remain erased until the party moves to another map.* `221 Show Animation`:
*Character — The display location will be based on the position of the
player or event. Animations — Specify the animation to display. Wait for
Completion — When enabled, the event will be paused until the animation
being displayed has finished.*

**Und beide gemessenen Formen sind leer:** `221 []` sechzehn mal, `222 []`
vierzehn mal.

**Und die Hilfe gibt 221 und 213 dieselben drei Saetze.** Das ist kein
Zufall, **und es heisst nicht, dass man sie zusammenlegt:** sie sind zwei
Codes in jedem fertigen Spiel, **und ein Feld fuer beide haette den
Ballon eines Ereignisses ueber die Animation einer Figur geschrieben.**

**Drei Befunde.**

1. **"Geloescht" ist nicht "weg."** *Temporarily removes the event
   currently being run* -- **das Ereignis hat danach weiter seine
   Befehle**, **und ein Leser, der es aus der Karte nahm, liess jeden
   spaeteren Befehl, der es nennt, mit "kein solches Ereignis" enden** --
   **und ein Spiel loescht sein eigenes Ereignis und laeuft danach noch
   vier Befehle weiter.**
2. **Nichts loescht das Flag wieder.** *The event will remain erased
   until the party moves to another map* -- **und ein Leser, der es nach
   N Bildern zuruecksetzte, holte das Ereignis zurueck, waehrend der
   Spieler gerade auf die Stelle sah.**
3. **Die Animationsdauer ist wieder eine Zahl, die dieses Repository
   gewaehlt hat** (`MaxAnimationFrames = 60`), **und die Konstante sagt
   es**, **denn die Hilfe nennt keine Dauer, und ohne eine waere *wait
   for the animation being displayed has finished* eine Wartezeit, die
   nie endet.**

**Und der `return !warten`-Fehler aus dem vorigen Schritt steht hier
genauso** -- **und das ist der Grund, warum er dort gesucht werden
musste:** **beide Befehle haben dieselbe Form, und ein Leser, der den
Fehler einmal macht, macht ihn zweimal.**

**Und `221` braucht wie `213` ein eigenes Feld, plus Anfangswert -1** --
**denn null ist das erste Icon der Editorliste und die erste Animation.**

**Test evidence** `test_mz_animation_and_erase.cs` (3), **3/3 beim ersten
Lauf.**

**Neu gemessen: 1703 von 2436 ausfuehrbar** (vorher 1673, und davor 1570).

## Und ein Befund, den erst ein echtes Projekt zeigt: der dritte Parameter ist ein Wort

**Gemessen an `CamelliaCoronation-Win`, und die Formen widersprechen sich.**

| Befehl | gemessene Form | Laenge |
|---|---|---|
| `121 Control Switches` | `"0"` | 3 |
| `123 Control Self Switch` | `"0"`, `"1"` | 2 |
| `129 Change Party Members` | `"0"`, `"1"` | 3 |
| `122 Change Variables` | `"3"` an Position 3 | 6 |
| **`213 Show Balloon Icon`** | **JSON-`false` / JSON-`true`** | 3 |
| `221 Show Animation` | **leer** | 0 |

**Und was `MzCommandEntry.From` daraus macht, ist Text, und zwar
`"true"` oder `"false"`** (`MzCommandEntry.cs:47`) -- **denn ein
Parameter ist eine Zeichenkette, und ein JSON-Boolean muss in eine
Zeichenkette.** **`At(pCommand, 2)` parst aber nur Zahlen**, und
`"true"` wurde also 0,
**und `0 == 1` ist false** -- **und damit war die Wartefunktion von
`213` und `221` in jedem echten Spiel tot.** Die Tests waren gruen,
**weil die Tests die Zahlen selbst geschrieben haben**: ein Test, der
seine eigene Eingabe schreibt, misst die Datei nicht.

**Und `221` traegt eine leere Liste, und ein fehlender Parameter ist
"nein"** -- **sonst haette der Leser bei der ersten Animation
ueberhaupt eingefroren**, denn es gibt dort nichts zu warten.

**Der Leser heisst jetzt `Flag`** und nimmt `1`, `"true"` und `"on"`
(ohne Rücksicht auf Gross- und Kleinschreibung) als ja; `0`,
`"false"` und ein fehlender Parameter sind nein. **Die drei
numerischen Befehle `121`, `123`, `129` lesen unveraendert ueber
`At`** -- **gemessen sind ihre Werte `"0"` und `"1"`, und ein Leser,
der dort `Flag` benutzt, hat nichts gewonnen und eine zweite
Wahrheitsform eingefuehrt.**

**Und die Regel, die daraus folgt: die Testdaten kommen aus dem
Projekt.** **Drei von sechs Bool-Parametern in diesem Dispatch waren
falsch gelesen, und kein Test der Welt haette das gefunden, weil alle
drei die Zahl selbst gesetzt hatten.**

## MZ: 203 Set Event Location

**Und die Hilfe hat keine Seite unter diesem Namen.** Das Handbuch
nennt die Seite *Set Event Location* und sagt *Changes the location of an
event* -- **und die Zahl in der Datei ist 203.** **Also traegt die
Konstante das Wort des Handbuchs und die Zahl der Datei**, **und ein
Leser, der den Namen nachgeschlagen und nichts gefunden haette, haette
einen Befehl uebersprungen, den das Spiel zehnmal benutzt.**

**Und die gemessene Form ist `[Ereignis, Ort, X, Y, Richtung]`** -- **und
der Ort ist in allen zehn Faellen 0**, **das ist *Direct
Designation*.**

**Drei Befunde.**

1. **Eine Kachel ist ein Sprung, und kein Schritt.** *Changes the
   location of an event* -- **es gibt hier keinen Weg und keine Route.**
2. **`RealX` und `RealY` gehoeren mit.** Die Figur wird zwischen zwei
   Kacheln gezeichnet, **und ein Leser, der nur `X` und `Y` setzte,
   liess die Zeichnung auf der alten Kachel** -- **und der naechste
   Bewegungsbefehl ging zurueck an den Ort, von dem die Figur gerade
   weggesetzt worden war**, **und das ist genau das, was ein
   Ortswechsel erreichen sollte.**
3. **Die Richtung wird auch gesetzt, wenn sich sonst nichts aendert.**
   Die Hilfe nennt Event, Location und Direction, **und
   `setLocation` des Motors nimmt alle drei.**

**Und ein Ort, den dieser Leser nicht beantworten kann, wird gesagt und
nicht geraten.** Gemessen ist der Ort immer 0, **und ein Leser, der das
immer annahm, haette beim ersten handgeschriebenen Ereignis mit dem
anderen Ort eine Figur auf eine Kachel gesetzt, die das Spiel nie
genannt hat.**

**Und ein Ereignis, das diese Karte nicht hat, wird benannt und nicht
erfunden.**

**Test evidence** `test_mz_set_event_location.cs` (3), **3/3 beim ersten
Lauf.**

**Neu gemessen: 1713 von 2436 ausfuehrbar** (vorher 1703).

## MZ: 301 Battle Processing

**Die Hilfe ist kurz und genau.** *Causes troops to appear and starts a
battle. Troops — Specify the troop against which the player will fight.
Can Escape — When enabled, the [Escape] command will be enabled during
battle. Can Lose — When enabled, there will not be a game over even if
the entire party is defeated.*

**Und die gemessene Form ist `[0, 7, false, false]`** -- **vier Werte,
und die letzten beiden sind echte JSON-Booleans**, **das ist derselbe
Befund wie bei `213`.**

**Und "Can Lose" ist das Feld, dessen Name luegt.** *When enabled, there
will not be a game over* -- **das Kaestchen heisst "eine Niederlage ist
ueberlebbar", und nicht "eine Niederlage ist verboten".** **Ein Leser,
der es als "verboten" gespeichert hat, machte aus einem Spiel, das eine
Niederlage ueberlebt, ein Game Over.**

**Und `InBattle` war `init` und ist jetzt aenderbar** -- **denn `351
Open Menu` fragt genau danach, und ein Feld, das beim Bauen der Fakten
gesetzt wurde, konnte nie wahr werden.** **Ein Spiel, das kaempft und
danach ein Menue oeffnet, hatte sein Menue waehrend des Kampfes offen.**

**Und `EnterBattle()` gibt den Tests eine Tuer, und nicht der Setter
eine** -- **wer den Zustand direkt setzen darf, kann ihn auch halb
setzen.**

**Test evidence** `test_mz_battle_processing.cs` (4), **4/4 beim ersten
Lauf.**

**Neu gemessen: 1720 von 2436 ausfuehrbar** (vorher 1713).

## MZ: 322 Change Vehicle Image

**Die Hilfe nennt zwei Einstellungen, und die zweite hat einen Wert, der
keine Datei ist.** *Change the image used for vehicles. These settings
will remain in effect until updated again by using this event command.
Vehicle — Specify the target vehicle. Images — Double-click the box to
specify the image to be displayed. Setting this to `[(None)]` will result
in no image being displayed.*

**Und die gemessene Form ist
`[1, "MC_Sprite_sheet", 1, "SlimeActors", 5, "Actor1_1"]`** -- **sechs
Werte, und das erste ist das Fahrzeug, und es ist 1 in allen sechsen,
und 1 ist das Schiff.**

**Und es gibt genau drei Fahrzeuge, und der Motor nummeriert sie von
null:** Boot 0, Schiff 1, Flugzeug 2. **Ein Leser, der bei eins
anfing, hatte fuer das Boot gar keinen Schluessel** -- **und ein Spiel,
das das Bild des Bootes aenderte, aenderte niemandens.**

**Und `[(None)]` heisst "kein Bild", und nicht "Datei mit diesem
Namen".** **Ein Leser, der den String gespeichert hat, zeigte einem
Fahrzeug ein Bild, dessen Datei es nicht gibt** -- **und auf einer Karte
mit einem Schiff ist das ein Loch, wo ein Schiff sein sollte.**

**Und es gibt hier kein Feld "auf welchem Fahrzeug sitzt der Spieler",
weil kein Befehl dieses Lesers das aendert.** **Die Bilder sind ohne
das hier**, **und ein Leser, der ein Reit-Feld eingefuehrt haette, um
ein Bild zu halten, eine zweite Antwort auf eine Frage, die niemand
stellt.**

**Test evidence** `test_mz_vehicle_image.cs` (4), **4/4 beim ersten
Lauf.**

**Und eine lebende Regel hat aufgedeckt, dass der erste Test nur "kein
viertes Fahrzeug" pruefte, und nicht "kein Null"** -- **und minus eins
ist genau die Zahl, die `213` und `221` fuer den Spieler verwenden**,
**also haette ein Leser, der diese Gewohnheit hier mitgebracht haette,
das Schiffsbild auf den Spieler gelegt.**

**Test evidence** `test_mz_vehicle_image.cs` (5), **4/4 und dann 5/5.**

**Neu gemessen: 1726 von 2436 ausfuehrbar** (vorher 1720).

## Und was die letzten vier Codes wirklich sind: die Wahl ist ein Block, kein Befehl

**Gemessen an `CamelliaCoronation-Win`, mit dem Nachbarn jedes Blocks.**

| Code | steht zwischen | Parameter | Vorkommen |
|---|---|---|---|
| `102` | `401` und `402` | `["['Yes', 'No']", 1, 0, 2, 0]` | 8 |
| `105` | `221` und `405` | `[1, false]` | 4 |
| `225` | `250` und `101` | `[3, 6, 60, true]` | 2 |
| `314` | `123` und `0` | `[0, 0]` | 1 |

**Und keiner der vier hat eine Seite in der offiziellen Hilfe**, **und
keiner hat einen Namen in `MzCommandTable`** -- **und `105` ist der
Eroeffner eines Blocks, in dem fuenf, zwoelf und vierzehn `405` stehen.**

**Die Tabelle wusste es und hat es gesagt:** `MzCommandTable.ShowChoices`
traegt die Bemerkung *the choices as the editor wrote them, under a 102. It
has no <c>command405</c> method, because the 102 that shows them reads it by
position.*

**Und `105` ist also derselbe Mechanismus unter einer anderen
Nummer** -- **oder ein anderer, und das weiss ich nicht.** **Gemessen
steht `105` nie zwischen `401` und `402`, sondern zwischen `221` und
`405`** -- **das heisst, es ist nicht der Dialog-Zweig, sondern ein
zweiter Weg, eine Wahl zu eroeffnen.**

**Und das ist der Grund, warum `402`, `404`, `405` und `412` als No-ops
dokumentiert sind, und nicht als fehlend:** **sie sind Bestandteile eines
Blocks, und der Block laeuft noch nicht.** **Die Wahl braucht drei
Dinge, und keines davon ist gebaut:** **eine Liste der Optionen aus den
`405`, eine Eingabe, die eine davon waehlt, und eine Zahl, auf die `102`
und `412` sich verzweigen.**

**Und das ist die naechste Aufgabe fuer MZ, und sie ist groesser als ein
Befehl:** `102 Show Choice List` mit dem zugehoerigen Block.

## Und der Choice-Block: 102 mit seinen 402, 404 und 412

**Und das ist kein Befehl, sondern ein Block** -- **und genau darum
waren `402`, `404`, `405` und `412` als No-ops dokumentiert und nicht als
fehlend: sie sind Teile eines Blocks, und der Block lief nicht.**

**Die gemessene Form, `CamelliaCoronation`, `Map004`, Event 14:**

```
401 ["(Done looking around for today?)"]
102 [["Yes", "No"], 1, 0, 2, 0]     Einzug 0
402 [0, "Yes"]                     Einzug 0
221 []                             Einzug 1
101 ["", 0, 0, 2, ""]              Einzug 1
402 [1, "No"]                      Einzug 0
404 []                             Einzug 0
412 []                             Einzug 1
```

**Und die Texte stehen zweimal, und beide Stellen haben alle:** einmal
als Liste in `params[0]`, und einmal auf den `402`-Zeilen. **Die Liste
ist die Quelle**, **denn eine `402`, deren Zweig leer ist, traegt ihren
Text trotzdem.**

**Und `402` sind zwei Befehle unter einer Nummer.** In einem `401`-Block
ist es *die naechste Zeile*, in einem `102`-Block ist es **eine Option
mit ihrem Zweig-Index und ihrem Text**. **Beide Namen stehen jetzt in
der Tabelle**, **`ContinueText` und `ChoicesOption`**, **und der Code,
der den einen braucht, sagt, in welchem Block er ist.**

**Und der zweite Parameter ist der Abbruchzweig, und nicht die Anzahl
der Optionen.** **Ein Leser, der ihn als Anzahl las, oeffnete eine Wahl
mit einer Option.**

**Und die Zweige zaehlen in der Datei von null und die Antworten von
eins.** **`402 [0, "Yes"]` ist der Zweig der Antwort 1** -- **und ein
Leser, der die Null der Datei als Antwortnummer nahm, hat die zweite
Option unerreichbar gemacht.**

**Und eine Wahl braucht beides:** **die Texte ohne Zweige ergeben einen
Bildschirm, auf dem der Spieler antwortet und nichts passiert**, **und
die Zweige ohne Texte ergeben nichts, was man beantworten kann.**
**Beide Faelle werden abgewiesen und gesagt.**

**Test evidence** `test_mz_choice.cs` (4), **und der Test baut den
gemessenen Block statt nur der `102`** -- **denn ein Test, der nur den
Befehl baut, testet ein Programm, das kein Spiel schreibt.**

**Und `MzChoice` hatte `CancelType` und `NoCancel = -2` schon**, **und
ich hatte eine eigene Rechnung daneben gestellt** -- **zwei Wahrheiten
ueber dieselbe Zahl**, **und sie liefen auseinander, als das Spiel `-2`
schrieb.** **Jetzt wird `CancelType` gelesen, und es gibt nur eine
Rechnung.**

**Und ein Fehler, den ein Test fand, weil er den Dispatch benutzt:** **ein
Zweig liegt bei Einzug 1, der Befehl bei 0** -- **und ein `break` an
dieser Stelle hat nach dem ersten Zweig aufgehoert**, **und so fand der
Leser nur die erste Option und meldete der zweiten einen Zweig, den es
nicht gibt.**

**Und ein zweiter Fehler derselben Art: der Testwert 4 fuer den zweiten
Zweig war geraten, und gemessen ist er 5** -- **weil der erste Zweig
zwei Zeilen hat.**

**Test evidence** `test_mz_choice.cs` (8), **und vier davon gehen durch
den Dispatch statt durch einen handgeschriebenen Zustand**, **weil
genau dort drei Regeln lebten, die sonst durchkamen.**

**Neu gemessen: 1750 von 2436 ausfuehrbar** (vorher 1726).

## Und was vom Choice-Block noch offen ist: 404

**Gemessen:** `404` steht bei Einzug 0, **unmittelbar nach dem letzten
`402`**, **und in dem Beispiel von `Map004` Event 14 traegt es keine
Zweige** -- **es ist der "sonst"-Zweig, und er ist leer.**

**Und `404` hat keinen Namen in `MzCommandTable`, und keine Seite in der
offiziellen Hilfe.** **Und `102 Show Choice List` hat auch keine Seite** --
**das, was ich im Handbuch fand, war die Liste der Steuerzeichen, und
nicht der Befehl.** **Also ist beides gemessen, aber nicht erklaert:**
**`404` ist der Abschluss des Choice-Blocks nach der Form des Spiels,
und warum es keine Seite hat, weiss ich nicht.**

**Und `405` traegt im Choice-Block den Text einer Option, und die
Optionen stehen vollstaendig in `params[0]`** -- **also ist `405` dort
eine Dublette.** **Im Dialog-Block ist es etwas anderes, und diese
Messung hat das nicht geklaert.**

## Und was die sechs Kriterien 3 bis 7 wirklich bedeuten: gemessen, nicht behauptet

**Alle sechs Engines sind *erkennbar*. Keine davon ist *laufbar*,
ausser RM2K, WOLF und MZ.** **Das steht so in den Klassenamen und in
den Faehigkeiten, und ich habe es jetzt nachgezaehlt statt behauptet.**

**`CreateRuntime` ist in genau drei Plugins ueberschrieben:**

| Plugin | Klasse | hat `CreateRuntime` | was es zurueckgibt |
|---|---|---|---|
| `RpgMaker2000Plugin` | `LcfPlugin` | **ja** | `Rm2kEngineRuntime` |
| `RpgMaker2003Plugin` | `LcfPlugin` | **ja** | `Rm2kEngineRuntime` |
| `WolfRpgPlugin` | `BuiltInEnginePlugin` | **ja** | `WolfEngineRuntime` |
| `RpgMakerXpPlugin` | `RgssPlugin` | nein | -- |
| `RpgMakerVxPlugin` | `RgssPlugin` | nein | -- |
| `RpgMakerVxAcePlugin` | `RgssPlugin` | nein | -- |
| `RpgMakerMvPlugin` | `WebRpgPlugin` | nein | -- |
| `RpgMakerMzPlugin` | `WebRpgPlugin` | nein | -- |

**Und der Quellcode sagt es selbst.** `WebRpgPlugin` laeuft im
Konstruktor mit: *"Detection-only {pName} boundary until an embedded
JavaScript…"* -- **das ist MV und MZ, und es ist eine Grenze, die
absichtlich gesetzt wurde.**

### Was XP, VX und VX Ace wirklich haben

**`RgssEngineRuntime` ist 335 Zeilen lang, und `Update` macht genau
eines:**

```csharp
public PluginOperationResult Update(double pDeltaSeconds)
{
    ...
    _clock.ProcessFrame(pDeltaSeconds);
    return PluginOperationResult.Succeeded();
}
```

**Kein Ruby, keine Szene, kein Ereignis, kein Interpreter.** **Der
Interpreter existiert** (`RubyInterpreter.cs`, 12 708 Zeilen),
**hat 284 Tests in 38 Dateien, und wird von keiner Runtime
aufgerufen.** **284 Tests fuer ein Programm, das kein Spiel
ausfuehrt** -- **und die sind nicht umsonst, denn sie sind der
Nachweis, dass der Sprachkern traegt, sobald jemand ihn verdrahtet.** **Und die
getestet, aber er wird von keiner Runtime aufgerufen.** **Und die
Skripte eines XP-Spiels sind verschluesselt** -- **das ist eine
Sicherheitsgrenze aus `AGENTS.md` und `BuiltInEnginePlugins.cs:424`,
und sie wird nicht aufgehoben.**

### Und was es lokal gibt

**Fuenf Spiele, und keines davon ist MV, VX oder VX Ace:**

| Spiel | Dateien | Engine |
|---|---|---|
| `CamelliaCoronation-Win` | `Data/`, `js/` | MZ |
| `Dragon Destiny` | `RPG_RT.exe` | RM2K |
| `MicroQuest - Beneath Brimestone 1.0` | `Data/`, `RGSS104E.dll` | XP |
| `dungeon5min` | `Data.wolf`, `GuruguruSMF4.dll` | WOLF |
| `Kaiju Girlfriend` | `Data.wolf`, `GuruguruSMF4.dll` | WOLF |

**Also: fuer Kriterium 3 (MV), 5 (VX) und 6 (VX Ace) gibt es kein Spiel
zum Messen, und fuer Kriterium 4 (XP) gibt es genau eines, dessen
Skripte verschluesselt sind.**

**Das ist kein Fortschrittsbericht, das ist die Grenze, und sie ist
gemessen.**

## Und was die MZ-Zahl bedeutet -- und was sie nicht bedeutet

**Es gibt keine MZ-`IEngineRuntime`.** **Alle fuenf Implementierungen
sind `EngineBootstrapRuntime`, `RgssEngineRuntime`, `Rm2kEngineRuntime`
und `WolfEngineRuntime`.** **Keine gehoert zu MZ.**

**Und gemessen: `MzEventRunner`, `MzInterpreter`, `MzCommandEntry` und
`MzBranchFacts` werden von keiner Datei ausserhalb von
`project/src/mz/` aufgerufen.** **Ueberhaupt nicht.**

**Also ist `1750 von 2436 ausfuehrbar` eine Aussage ueber den Dispatch
und nicht ueber ein laufendes Spiel.** **Der Dispatch liest echte
Befehle aus einem echten fertigen Projekt und fuehrt sie in der
richtigen Reihenfolge aus -- aber kein Bild, kein Ton und keine Karte
eines MZ-Spiels werden von ihm gezeichnet oder gespielt.**

**Das ist dieselbe Luecke wie bei XP, VX und VX Ace, und sie ist an
beiden Stellen dieselbe: es fehlt die Runtime, die das Plug-in dem
Programm gibt.**

**Und es ist die naechste Aufgabe fuer MZ, und sie ist eine einzige
Klasse:** **eine `IEngineRuntime`, die die Karten eines Projekts laedt,
den `MzEventRunner` pro Bild aufruft und das Ergebnis ueber die
bestehende `PresentationState` zeigt** -- **genauso, wie
`Rm2kEngineRuntime` es fuer RM2K tut, in 2251 Zeilen.**

## Und der erste MZ-Spielaufruf: MzEngineRuntime

**Vor dieser Klasse war gemessen: `MzEventRunner`, `MzInterpreter`,
`MzCommandEntry` und `MzBranchFacts` hatten keinen Aufrufer ausserhalb von
`project/src/mz/`.** **Ueberhaupt nicht.** **Der Dispatch las echte
Befehle aus einem echten fertigen Projekt und fuehrte sie richtig aus, und
kein Spiel lief.**

### Der Lauf, gemessen an `CamelliaCoronation-Win`

- **19 Karten gelesen, 0 uebersprungen**
- **Startkarte 2, und die steht in `System.json` als `"startMapId": 2`**
- **100 Bilder, 202 Aktionen**, **und die nennen, was die Datei traegt:**
  **`Move1 volume 90 pitch 100 pan 0`** und **`a transfer to map 1 at
  14,12 is reserved`**

### Vier Fehler, die der erste Lauf aufgedeckt hat

1. **Der Inspektor liest Verzeichnisdateien nur als Vorspann von 4096
   Bytes** (`GameInspectionLimits.MaxPrefixBytes`), **und 18 von 20
   Karten des Projekts sind groesser** -- **jede kam als abgeschnittenes
   JSON an und wurde uebersprungen.** **Die Runtime liest die Dateien
   jetzt selbst.**
2. **`MapInfos.json` ist die Falle, und nicht `System.json`.** **Gemessen:
   neunzehn Eintraege ohne `parentId`**, **und ein Leser, der die
   hoechste davon nahm, startete auf Karte 17 statt auf 2.** **Das ist
   fast derselbe Fehler, den rm2k mit
   `Directory.EnumerateFiles(...).FirstOrDefault()` gemacht hat.**
3. **Die Seiten liegen unter `events[].pages[].list`**, **und nicht unter
   den Feldern der Karte** -- **und ein Entwurf, der die Arrays aus dem
   Wurzelobjekt las, fand nie eine Seite**, **und der Lauf tat nach
   hundert Bildern nichts und sagte keinen Grund.**
4. **`MzEventRunner.Run` gibt seine Aktionen in `Result.Actions`
   zurueck**, **und es gibt keine gemeinsame Liste** -- **und ein
   Entwurf, der die eigene Liste las, blieb bei null Aktionen**, **und
   das Feld `ActionCount` im Log sah nach einem Lauf aus.**

**Und die Assertion, die die Klasse rechtfertigt, ist nicht "kein
Absturz":** **"es hat etwas getan" und "mindestens eine Aktion nennt,
was die Datei getragen hat".** **Denn ein gruener Lauf, der nichts
tut, waere auch fuer eine Runtime gruen, die nichts laesst** -- **und
genau das war der Zustand, in dem der Dispatch vier Runden lang
gemessen wurde.**

**Test evidence** `test_real_mz_runtime_run.cs` (3), **ueber
`EnginePluginHost.Start` und nicht ueber eine von Hand gebaute
Runtime** -- **denn die ganze Luecke war, dass die Klasse nicht
erreichbar war.**

## Und die MZ-Runtime malt: verschluesselte Bilder, neun Blaetter, sechs Zahlen

**Vorher: die Runtime las Karten und fuehrte Befehle aus, und ein
Spieler konnte nichts davon sehen.**

### Drei Hindernisse, alle gemessen

1. **Die Bilder sind verschluesselt.** **Gemessen: `System.json` sagt
   `hasEncryptedImages: true`, und alle 81 Bilder heissen `.png_`.** Und
   der Algorithmus steht **im Spiel selbst**, in `js/rmmz_core.js`,
   `Utils.decryptArrayBuffer`:
   **Header `"52,50,47,4d,56,0,0,0,0,3,1,0,0,0,0,0"` pruefen**, **dann
   `body = source.slice(16)`**, **und die ersten 16 Bytes des Rumpfes
   mit je zwei Hex-Ziffern des Schluessels verrechnen.**

   **Und zwei Fehler, die ein first reader macht:**
   - **Den Header entschluesseln statt abschneiden** -- **und dann
     beginnt die Datei mit `cb0ca26d`, wo ein PNG mit `89504e47`
     beginnt.**
   - **Den Schluessel als ASCII lesen statt als Hex** -- **und dann
     kommt `6b69722e33353230` heraus, und das ist der lesbare Text
     "kir.3520", und der sieht wie eine Antwort aus und ist keine.**

2. **Ein Tileset ist neun Blaetter, und keines heisst wie das
   Tileset.** **Gemessen:** `data/Tilesets.json` traegt je
   `tilesetNames` mit neun Eintraegen --
   **[World_A1, World_A2, (leer), (leer), (leer), World_B, World_C,
   (leer), (leer)]** -- **und `img/tilesets/Overworld.png_` gibt es
   nicht.**

3. **`IntOr` liest den Wert eines Elements, und nicht dessen Feld `id`.**
   **Ein Objekt hat keine Zahl**, **und deshalb bekam jede der sechs
   Tileset-Zeilen -1**, **und jedes Tileset wurde uebersprungen**, **und
   die Karte malte nichts** -- **und der `catch` um das Lesen sagte
   nichts, weil nichts geworfen wurde.**

**Und derselbe Fehler stand an zwei weiteren Stellen**, **in `MapIdOf`
und in einem Test** -- **und wurde an allen dreien in derselben Stunde
gemacht**, **was sagt, dass eine Form, die einmal falsch gelesen wird,
dreimal falsch gelesen wird.**

**Und der stumme `catch` ist jetzt sprechend:** **`TilesetProblem`
sagt, woran es lag**, **denn ein leeres Wuerterbuch und ein Projekt
ohne Tilesets sehen gleich aus**, **und die Karte malte nichts, und
das Log sagte nichts.**

### Der Beleg

**`blaetter=35` Sheets gelesen**, **und `Map002` gemalt**, **und
`PaintedColours > 1`** -- **denn eine Karte in einer Farbe ist eine
Karte ohne Tileset**, **und genau das sah `Map0001` auf dem RM2K-Projekt
aus, und es ging durch alles, bis ein Test fragte, wie viele Farben es
gibt.**

**Test evidence** `test_mz_image_reader.cs` (4) und
`test_mz_map_render.cs` (4).

**`All 2170 tests passed`, Validator gruen.**

## Und die Figuren werden gezeichnet: RGBA statt Palette

**Zwei Arten von Bildern in einem einzigen Projekt, und gemessen.**

| Datei | Farbtyp | Bedeutung |
|---|---|---|
| `img/tilesets/World_A1.png_` | **3** | Palette |
| `img/characters/SlimeCharacters.png_` | **6** | RGBA |

**Und der vorhandene Chipsatz-Leser nimmt nur Farbtyp 3** --
**also hatte ein Spiel, dessen Figuren echte Farben haben, ueberhaupt
keinen Leser.** **`MzRgbaImage` liest 2 und 6, mit denselben Chunk-,
Laengen-, Inflate- und Filterregeln** -- **und der Unterschied ist nur,
was aus den Zeilen kommt: ein Index je Pixel gegen vier Bytes.**

**Und die Filter sind der Teil, an dem ein handgeschriebener Decoder
scheitert:** **die vier Filter der Spezifikation -- none, sub, up,
average, Paeth -- sagen jede, wie sich die Zeile zur Zeile darueber
verhaelt**, **und ein Decoder, der die Zeilen als Rohbytes liest, malt
ein Bild mit ungefaehr richtigen Farben an ungefaehr richtigen Stellen
und es ist nicht das Bild.**

### Und das Raster der Figuren, gemessen

**576 × 384 waere zwei Figuren hoch, und nur die ersten 192 Pixel
tragen etwas.** **Die zweiten 192 sind leer** -- **und ein Leser, der
die vier Richtungen auf zwei Zeilen verteilt, zeichnet die Haelfte
aller Figuren aus dem Leeren.**

**Und die vier Richtungen liegen nebeneinander in der ersten Reihe, und
nicht uebereinander:** **die Spalten 0 bis 3 sind unten, links, rechts,
oben** -- **und das sind die Zahlen 2, 4, 6 und 8 des Motors, in genau
dieser Reihenfolge.**

**Und die Figur ist 144 breit, und eine Kachel ist 48, also steht sie
mittig auf drei Kacheln** -- **das ist der Grund, warum der Motor sie
mittig setzt**, **und eine Figur an der linken Kachelkante sieht
falsch aus.**

### Und noch ein Befund beim Testbauen

**`Clear()` setzt den Alphakanal auf 0, und `DistinctColours` zaehlt
nur, was Alpha hat.** **Ein Hintergrund aus vier Nullen ist kein
Hintergrund, sondern ein Loch**, **und ein Test, der einen aufbaut
und dann eine Farbe zaehlt, zaehlt null.**

**Test evidence** `test_mz_character_render.cs` (4), **4/4.**

**`All 2174 tests passed`, Validator gruen.**

## Und die Figuren stehen auf der Karte

### Und das Bild einer Figur haengt an der Seite, und nicht am Ereignis

**Gemessen an `Map017.json`:** das Ereignis traegt `id`, `name`, `x` und
`y` -- **und Figur, Index, Richtung und Schritt stehen in `page.image`.**
**Ein Leser, der `characterName` am Ereignis suchte, fand nichts, und
jede Figur im Spiel war unsichtbar.**

**Und der Name ist ein Wort, und keine Zahl.** `MC_Sprite_sheet`,
`SlimeCharacters`, `!Flame`, `Vehicle` -- **und die Datei ist
`img/characters/<name>.png_`.**

### Und zwei der beiden Bedingungsarten, gemessen ueber alle 253 Seiten

| Muster | Seiten |
|---|---|
| keine Bedingung | **208** |
| Selbstschalter | 33 |
| ein Schalter | 11 |
| zwei Schalter | 1 |

**Schauder, Item, Variable und Uhr kommen nicht vor.** **Und eine Seite
ohne Bedingung ist sichtbar** -- **ein Leser, der ein nicht gesetztes
Feld als "nicht erfuellt" las, versteckte jede Seite im Spiel.**

**Und die erste passende Seite gewinnt** -- **das Ereignis 19 an
`Map017` haengt an einem Selbstschalter, den dieser Leser nicht
beantworten kann, und es wird weggelassen und gesagt** -- **also sind es
acht Figuren und nicht neun.** **Die ehrliche Antwort ist hier "nein",
denn eine im falschen Zustand gezeichnete Figur ist schlimmer als eine
fehlende.**

### Und ein Figurenblatt ist nicht immer RGBA

**Der eigentliche Befund, und er ist erst beim Messen aller vier
Blaetter aufgetaucht:**

| Blatt | Farbtyp |
|---|---|
| `MC_Sprite_sheet` | **3** (Palette) |
| `!Flame` | **3** |
| `Vehicle` | **3** |
| `SlimeCharacters` | **6** (RGBA) |

**Und `SlimeCharacters` war die einzige, die der erste Leser las** --
**und er zaehlte dabei die Dateien und nicht die Bilder**, **und meldete
vier gelesene Blaetter, waehrend er eines hielt.**

**Und die Durchsicht hat in beiden Faellen eine andere Quelle:** **bei
einer Palette der Index null, bei vier Kanaelen der Alphakanal.**
**`MzCharacterSheet` traegt beides hinter vier Bytes je Pixel**,
**deshalb hat der Renderer einen Pfad und nicht zwei.**

### Und noch eine beim Entschlusseln

**Nur die ersten 16 Bytes des Koerpers werden entschlüsselt, und nicht
der ganze.** **Eine Sonde, die den ganzen Koerper entwandete, kam auf
`b')5\x9e\x04s...'` und schloss daraus, alle vier Dateien seien
unlesbar** -- **während der C#-Leser sie las.**

**Test evidence** `test_mz_map_figure.cs` (4), **4/4**;
`test_real_mz_runtime_run.cs` (6), **6/6**; `test_mz_character_render.cs`
(4), **4/4**.

**`All 2180 tests passed`, Validator gruen.**

## Und die Figuren bewegen sich: die Uhr aus dem Spiel selbst

**Und jede Formel wurde aus `js/rmmz_objects.js` des fertigen Spiels
gelesen, und nicht erfunden.**

| Was | Die Formel des Motors |
|---|---|
| Wartezeit je Schritt | `(9 − realMoveSpeed) × 3` |
| Zaehler, gehend | `+1.5` |
| Zaehler, stehend | `+1` |
| Musterzahl | `maxPattern() = 4` |
| Gezeichnete Spalte | `_pattern < 3 ? _pattern : 1` |

**Und die Wartezeit laeuft rueckwaerts** -- **Tempo 1 wartet 24
Schritte, Tempo 6 wartet 9** -- **und ein Leser, der das Tempo selbst
als Bildzahl nahm, liess die langsamsten Figuren am schnellsten gehen.**

### Und sechzehn ist eine Drehung, und kein Schritt

**Der teuerste Befund dieser Runde, und der Grund fuer die ganze
Messung.**

| Code | Der Motor nennt es |
|---|---|
| 1, 2, 3, 4 | `ROUTE_MOVE_DOWN/LEFT/RIGHT/UP` |
| **16, 17, 18, 19** | **`ROUTE_TURN_DOWN/LEFT/RIGHT/UP`** |
| 15 | `ROUTE_WAIT` |

**Sechzehn ist keine Richtung und kein Schritt -- es dreht eine
Figur.** **Und genau diese vier benutzt das Projekt, sieben Seiten,
sonst nichts.**

**Ein Leser, der 16 als Schritt las, schob sieben Figuren von ihrer
Kachel. Ein Leser, der 16 als "unten" las, weil es das Erste von vier
ist, drehte sie falsch herum.**

### Und gemessen: fast keine Figur bewegt sich ueberhaupt

| `moveType` | Seiten |
|---|---|
| **0 -- fest** | **235** |
| 3 -- zufaellig | 18 |

**Und 0 ist nicht "keine Angabe", sondern "die Laufbahn wird nie
abgearbeitet".** **Ein Leser, der jede Laufbahn abarbeitete, bewegte 253
Figuren, die niemand gebeten hat zu bewegen** -- **und einer, der keine
abarbeitete, hatte einen Raum von Statuen.**

**Und gezeichnet wird der Schritt aus der Uhr, und nicht der aus der
Datei** -- **denn die Datei nennt den Schritt, bei dem die Figur stehen
muss.**

### Und der Spieler hat weder Tempo noch Takt

**Gemessen: `Actors.json` nennt fuer den Spieler weder `moveSpeed`
noch `moveFrequency`** -- **beide fehlen** -- **und der Motor setzt `4`
und `6`.** **Ein Leser, der dort eine Null las, bekam einen Spieler, der
im Frame stehen blieb.**

**Test evidence** `test_mz_walk_clock.cs` (4), **4/4**;
`test_real_mz_runtime_run.cs` (7), **7/7**.

**`All 2185 tests passed`, Validator gruen.**

## Und die Laufbahn-Konstanten waren falsch, und niemand benutzte sie

### Der Befund

**`MzMoveRoute` trug RM-Zahlen unter MZ-Namen.** Die Liste nannte:

| Zahl | Bis jetzt behauptet | Gemessen |
|---|---|---|
| 20 | ein Schritt | **Vierteldrehung rechts** |
| 21 | ein Zufallsschritt | **Vierteldrehung links** |
| 22 | umsehen | **um 180 Grad drehen** |
| 23 | umsehen | **Vierteldrehung rechts oder links** |
| 24 | umsehen | **zufaellig drehen** |
| 25 | umsehen | **zum Spieler drehen** |
| 26 | Vierteldrehung rechts | **vom Spieler weg drehen** |
| 27 | Vierteldrehung links | **Schalter einschalten** |
| 28 | um 180 Grad | **Schalter ausschalten** |

**Und gemessen ist das an `js/rmmz_objects.js` des fertigen Spiels.**
**Es gibt keinen Schrittcode und keinen Umsehcode bei diesen Nummern.**

### Und sieben dieser Zahlen wurden nirgends benutzt

**Der Fehler fiel nur beim Messen auf, und nicht durch einen Test** --
**denn kein Code im Projekt verwies auf sie.** **Eine falsche Konstante,
die niemand liest, ist nicht harmlos -- sie ist eine falsche Konstante,
die auf den ersten Leser wartet.**

### Und die Vierteldrehung folgt einer Tabelle, die man nicht rät

**Gemessen an `turnRight90`: unten wird links, links wird oben, oben
wird rechts, rechts wird unten.**

**Und "rechts" heisst hier rechts, wenn man von oben auf eine Figur
sieht** -- **und nicht das, was das Wort auf einem Kompass meint.**
**Ein Leser, der die Zahlen drehte statt dieser Tabelle zu folgen,
drehte jede Figur falsch herum und sah genau richtig aus.**

### Und eine Regel, die ich falsch las und dann liegen liess

**Ich habe `advanceMoveRouteIndex` gelesen und `numCommands =
list.length - 1` fuer das Listenende gehalten.** **Es ist der Punkt, ab
dem eine wiederholende Laufbahn auf null zurueckspringt.** **Der
Endpunkt wird sehr wohl ausgefuehrt** -- **`processRouteEnd` ist der
Zweig, der ihn behandelt.**

**Meine Regel liess jede Laufbahn einen Schritt zu kurz laufen, und der
bestehende Test dieses Projekts hat es sofort gemerkt**: **eine Figur
aus fuenf Schritten kam nur vier Kacheln weiter.** **Ich habe sie
zurueckgenommen, weil der Test recht hatte und nicht mein Kommentar.**

**Test evidence** `test_mz_move_route.cs` (10), **10/10**.

**`All 2188 tests passed`, Validator gruen.**

## Und die Laufbahn haengt an `moveType 3`, und das Spiel hat keine

### Die Regel, gemessen an `updateSelfMovement`

| `moveType` | Was der Motor tut |
|---|---|
| **0** | **nichts -- in keinem Zweig** |
| 1 | zufaellig gehen |
| 2 | zum Spieler gehen |
| **3** | **die eigene Laufbahn der Seite laufen lassen** |

**Und `moveType` sagt, ob die Figur von sich aus losgeht, und nicht ob
sie gerade geht.**

### Und es gibt eine Wartezeit, die man nicht raten darf

**Gemessen an `stopCountThreshold`: `30 * (5 - moveFrequency)`.**

**Fuer dieses Projekt sind das 60 Bilder, denn `moveFrequency` ist
ueberall 3.** **Ein Leser, der die Schwelle ausliess, liess die Figuren
sofort losrennen** -- **und einer, der die Frequenz als Framezahl nahm,
wartete 150.**

**Und bei der Standardfrequenz des Motors, 6, ist die Zahl negativ, und
keine Figur kommt je dort hin.**

### Und die beiden Mengen sind disjunkt

| | Seiten | hat Schritte |
|---|---|---|
| `moveType: 0` | 235 | **7** |
| `moveType: 3` | 18 | **0** |

**Die 18 Seiten mit `moveType 3` liegen auf `Map007`, `Map008`,
`Map012` und `Map015`, und alle vier sind `!Flame`.** **Und keine davon
traegt Schritte -- ihre Laufbahn besteht aus genau einem Endpunkt.**

**Und das ist kein Fehler des Projekts, sondern die Regel des Motors:**
**eine eigene Laufbahn laeuft nur bei `moveType: 3`.**

### Und ein Fehler, den erst eine Sonde fand

**Ich hatte `Moving = moveType != 0` gesetzt.** **Der Motor prueft
`isMoving()`, und das ist `_realX !== _x || _realY !== _y`** -- **und
die beiden weichen nur ab, wenn die Figur gerade einen Schritt geht.**

**Gemessen auf `Map015`: neun Figuren, alle mit `moving=true` und alle
auf ihrer Kachel.** **Ein Leser, der `moveType` fuer "geht gerade"
haelt, stellt einen ganzen Raum mitten im Schritt dar, in dem niemand
einen Schritt geht.**

### Und was eine leere Laufbahn tut

**Nichts, und das ist richtig.** **Der Motor ruft `moveTypeCustom` auf,
`updateRoutineMove` findet genau einen Endpunkt und tut nichts.**
**Eine Runtime, die hier etwas bewegte, erfaende Bewegung, von der das
Spiel nichts weiss.**

**Test evidence** `test_real_mz_runtime_run.cs` (8), **8/8**.

**`All 2189 tests passed`, Validator gruen.**

## Und die Figur geht wirklich: 205 und die Regel fuer minus eins

### Und `205` war verdrahtet, aber die Figuren hatten keinen Namen

**`MzCommands` hatte den Zweig, und `Facts.Characters` war leer.**
**Ein `205` fand also nie eine Figur und tat nichts -- bei allen 96
Routen dieses Spiels.**

### Und die Regel hat drei Teile, alle gemessen

**An `Game_Interpreter.prototype.character`:**

| `params[0]` | Wen der Motor meint |
|---|---|
| im Kampf | **nichts, was auch immer die Zahl ist** |
| **unter null** | **den Spieler** |
| **null** | **das eigene Ereignis dieser Seite** |
| positiv | das Ereignis mit dieser Nummer |

**Und 46 der 96 Routen dieses Spiels sagen minus eins** -- **also
bewegt fast die Haelfte aller Routen den Spieler.**

**Und die Null ist der billigste Fehler hier:** **sie sieht aus wie
"keine Figur", und sie heisst "ich selbst"** -- **und ein Leser, der
sie als keine las, liess eine Seite, die ihr eigenes Ereignis
herumfuehrt, stillstehen.**

### Und die kuerzeste Route des Spiels, woertlich

**`Map001.json`, Ereignis 4:**

```
101 ["", 0, 0, 2, "Camellia"]
401 ["This scout trail leads further along the cliff to one of"]
401 ["their main lookouts. I have no need to go this way."]
205 [-1, {list: [{code: 3}, {code: 0}], wait: true}]
505 [{code: 3}]
```

**Der Spieler geht eine Kachel nach rechts, die Seite wartet, und
`505` folgt.** **`code 3` ist `ROUTE_MOVE_RIGHT`, und es ist der einzige
Schritt dieser Route.**

### Und was ich unterwegs zerstoert habe

**Beim Einfuegen des Tests sind vier Testmethoden in
`project/src/mz/MzBranch.cs` gelandet, und die Datei war danach nicht
mehr die, die in Git lag.** **Ich habe sie aus Git wiederhergestellt
und die drei echten Stuecke (`WithCharacters` und `TryNameCharacter`)
neu eingefuegt** -- **und die Tests gehoeren nach
`test_mz_move_route.cs`, wo sie jetzt sind.**

**Ein Test, der in einer Quelldatei landet, ist kein Test, und eine
Quelldatei, die Tests enthaelt, ist kein Quellcode.**

**Test evidence** `test_mz_move_route.cs` (11), **11/11**.

**`All 2190 tests passed`, Validator gruen.**

## Und der Spieler ist die Figur, und er läuft wirklich

### Der Befund, der die Zwei-Objekte-Loesung gekillt hat

**Ich hatte dem Spieler eine eigene Figur gegeben und danach zwischen
beiden kopiert. Gemessen an `Game_Player.prototype.initialize` ist das
eine Kopfschleife, die der Motor nicht hat:**

```
Game_Player.prototype.initialize = function() {
    Game_Character.prototype.initialize.call(this);
    this.setTransparent($dataSystem.optTransparent);
};
```

**Und `initMembers` ist `Game_Character`s, plus ein paar eigene
Glieder.** **Der Spieler erbt `_x`, `_y`, `_direction` und `_pattern`
und behaelt sie selbst** -- **es gibt kein Paar, das man abgleichen
koennte.**

**Und die Kopfschleife war nicht nur ueberfluessig, sie war falsch:**
**sie haette den Spieler und seine Figur auseinanderlaufen lassen,
und `SyncFigure` haette das nur verstaerkt.**

### Und der Schritt ist eine Bruchzahl, und nicht eine Kachel

**Gemessen an `updateMove`:**

```
_realX = Math.min(_realX + distancePerFrame(), _x)
distancePerFrame = 2^realMoveSpeed / 256
```

| Tempo | Kacheln je Bild | Bilder je Kachel |
|---|---|---|
| 1 | 1/128 | **128** |
| 4 | 1/16 | **16** |
| 5 | 1/8 | 8 |
| 6 | 1/4 | 4 |

**Und `PassFrame` hatte hier zwei ganze Kacheln, je nachdem ob Tempo 4
ueberschritten war oder nicht** -- **das heisst: der Sprung von Tempo 4
auf 5 liess die Figur doppelt so schnell gehen, und beide
Geschwindigkeiten waren falsch.**

**Wer die ganze Kachel nimmt, sieht zwei Spruenge; wer die Bruchzahl
nimmt, sieht einen Lauf.**

### Und ich habe die Bilder je Kachel erst falsch gerechnet

**Im Test standen vier, und `2^4 / 256` ist ein Sechzehntel, und ein
Sechzehntel braucht sechzehn Bilder.** **Der Test hat es gemerkt, weil
die Figur nach 20 Bildern erst drei Kacheln weit war.**

**Ein Test, der die Arithmetik des Motors nachrechnet, merkt einen
Fehler in der Arithmetik des Motors, die man ihm selbst eingebaut
hat.**

**Test evidence** `test_mz_move_route.cs` (11), **11/11**.

**`All 2190 tests passed`, Validator gruen.**

## Und das Figurenblatt war um den Faktor drei daneben

### Der Befund

**Ich hatte die Zelle mit 144 mal 192 Pixeln gerechnet. Sie ist 48 mal
48.** **Und das steht woertlich in `js/rmmz_sprites.js`:**

| Was | Die Formel des Motors | Fuer 576 x 384 |
|---|---|---|
| `patternWidth` | `bitmap.width / 12` | **48** |
| `patternHeight` | `bitmap.height / 8` | **48** |

**Also hat ein Figurenblatt zwölf Zellen nebeneinander und acht
untereinander, und nicht vier mal zwei.**

### Und eine Figur ist drei Zellen breit und vier hoch

```
sx = (characterBlockX() + characterPatternX()) * pw
sy = (characterBlockY() + characterPatternY()) * ph

characterBlockX = (index % 4) * 3
characterBlockY = Math.floor(index / 4) * 4
characterPatternX = pattern()
characterPatternY = Math.floor((direction + 2) / 4) % 4
```

**Die Richtung ist die Zeile, und der Index und der Schritt sind die
Spalten.** **Und `characterPatternY` ist genau die Reihenfolge unten,
links, rechts, oben.**

### Und gemessen, Zelle fuer Zelle

**Alle 96 Zellen des gemessenen Blatts durchgezaehlt:**

| Zeile | Belegung der 12 Zellen |
|---|---|
| 0 | 649 666 648 674 736 668 628 690 628 598 612 597 |
| 1 | 575 494 573 640 539 636 632 546 627 537 456 535 |
| 2 | 575 494 573 640 539 636 632 546 627 537 456 535 |
| 3 | 654 666 662 715 754 715 678 710 668 604 616 612 |
| **4 bis 7** | **alle null** |

**Und die Zeilen 1 und 2 sind gleich, und 0 und 3 auch** -- **das ist die
Sprungfolge: die Reihen laeuft und kommt zurueck.**

### Und was mein Fehler angerichtet hat

**Ein `characterIndex` von 3 wurde vier Bildbreiten rechts vom Blatt
gezeichnet -- also gar nichts -- und der Zeichner gab `true` zurueck
und tat so, als haette er gearbeitet.**

**Ein Zeichner, der `true` sagt und nichts malt, ist schlimmer als
einer, der `false` sagt**, **denn der erste sieht aus wie Erfolg.**

### Und noch ein Befund aus dem Testbauen

**Eine Figur, die auf Kachel 7 steht und auf Kachel 6 gezeichnet wird,
ist dieselbe Figur eine Kachel links.** **Und ein Test, der nur eine
Kachel vergleicht, vergleicht den Streifen, auf dem sie steht, und sonst
nichts** -- **und genau deshalb meldete er zwei verschiedene Bilder als
gleich.**

**Und `RealX` ist 6,0 und nicht 6,25** -- **denn die Luecke schliesst
sich erst in `PassFrame`, und der erste Schritt hat sie noch nicht
geschlossen.**

**Test evidence** `test_mz_character_render.cs` (5), **5/5**.

**`All 2191 tests passed`, Validator gruen.**

