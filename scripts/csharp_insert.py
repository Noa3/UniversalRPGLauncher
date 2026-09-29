"""Insert a C# method block before a named member, never after the class brace.

The repeated failure in this project was an insertion point found by
`str.find` on a signature that also appeared past the closing brace, so a
helper block landed outside the class and the file failed with CS1519
("invalid token } in a member declaration").  This finds the line index of
the member inside the class body and verifies the insertion point is before
the class's closing brace before writing.
"""

import pathlib


def setze_vor(pfad, mitglied, block, einmal=True):
    """Insert `block` before the member declaration `mitglied`.

    Args:
        pfad: Path to the .cs file.
        mitglied: The exact declaration line, e.g. "    private bool Definiert(".
        block: The full text to insert, without a trailing blank line.
        einmal: Require that `mitglied` appears exactly once.

    Returns:
        The number of lines the file grew by.

    Raises:
        ValueError: The member was missing, ambiguous, or the insertion
            point was outside the class body.
    """
    datei = pathlib.Path(pfad)
    zeilen = datei.read_text(encoding='utf-8').split('\n')
    treffer = [n for n, z in enumerate(zeilen) if z.startswith(mitglied)]
    if einmal and len(treffer) != 1:
        raise ValueError(
            f'{mitglied!r} steht {len(treffer)}x in {datei}, erwartet 1x')

    # Die Klassenklammer ist die letzte Zeile, die nur "}" ist.  Eine
    # Einfuegestelle nach ihr ist der Fehler, den dieser Helfer verhindert.
    klasse_ende = max(n for n, z in enumerate(zeilen) if z == '}')
    stelle = treffer[0]
    if stelle > klasse_ende:
        raise ValueError(
            f'{mitglied!r} steht bei {stelle}, die Klasse endet aber bei '
            f'{klasse_ende} -- der Anker zeigt aus der Klasse heraus')

    # Der Doc-Kommentar direkt davor gehoert zum Mitglied.
    doc = stelle
    while doc > 0 and (zeilen[doc - 1].strip().startswith('///')
                       or zeilen[doc - 1].strip() == ''):
        doc -= 1
        if zeilen[doc].strip() == '':
            break

    neuer = zeilen[:doc] + block.split('\n') + [''] + zeilen[doc:]
    datei.write_text('\n'.join(neuer), encoding='utf-8', newline='')
    return len(neuer) - len(zeilen)
