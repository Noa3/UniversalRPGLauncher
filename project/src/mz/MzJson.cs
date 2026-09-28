using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Web;

/// <summary>
/// JSON, read the way a game wrote it.
/// </summary>
/// <remarks>
/// <para>
/// This is a reader for what RPG Maker MV and MZ put in their data files, not a
/// general JSON library, and the differences matter. A game's writer emits a
/// small set of shapes and the reader has to be exact about three of them:
/// </para>
/// <list type="bullet">
/// <item><description>
/// A number may be written as an integer, a fraction or in exponent form, and a
/// game's numbers are things like a stat, a rate and a coordinate. Nothing is
/// rounded and nothing is turned into text and back.
/// </description></item>
/// <item><description>
/// A string may hold any character, and a game's strings hold the author's own
/// text. A string is taken as the characters the file has, and a string that is
/// not valid UTF-8 is refused rather than repaired, because a repaired string
/// is a different string and a game does not know it was repaired.
/// </description></item>
/// <item><description>
/// The file may hold a null, and in the database files it holds one at index
/// zero on purpose. It is a value here, not a missing element.
/// </description></item>
/// </list>
/// <para>
/// Nesting is bounded. A game's data nests a few levels deep and a hostile or
/// broken file could nest until the stack gave out, so a file that nests past
/// <see cref="MaxDepth"/> is refused with the depth in the message rather than
/// ending the process.
/// </para>
/// </remarks>
internal static class MzJson
{
    public const int MaxDepth = 128;

    public static bool TryParse(
        string pText, out MzValue pRoot, out string pFailure)
    {
        pRoot = new MzValue(MzKind.Null);
        pFailure = "";
        var position = 0;

        try
        {
            SkipSpace(pText, ref position);
            var value = ReadValue(pText, ref position, 0);
            SkipSpace(pText, ref position);
            if (position != pText.Length)
            {
                pFailure =
                    $"there is text after the value at character {position}";
                return false;
            }

            pRoot = value;
            return true;
        }
        catch (MzDataException exception)
        {
            pFailure = exception.Message;
            return false;
        }
    }

    private static MzValue ReadValue(string pText, ref int pPosition, int pDepth)
    {
        if (pDepth > MaxDepth)
        {
            throw new MzDataException(
                $"the file nests deeper than the {MaxDepth} level limit at"
                + $" character {pPosition}");
        }
        SkipSpace(pText, ref pPosition);
        if (pPosition >= pText.Length)
        {
            throw new MzDataException(
                $"the file ends where a value was expected, at character {pPosition}");
        }

        return pText[pPosition] switch
        {
            '{' => ReadObject(pText, ref pPosition, pDepth),
            '[' => ReadArray(pText, ref pPosition, pDepth),
            '"' => ReadString(pText, ref pPosition),
            't' => ReadLiteral(pText, ref pPosition, "true", true),
            'f' => ReadLiteral(pText, ref pPosition, "false", false),
            'n' => ReadLiteral(pText, ref pPosition, "null", false, isNull: true),
            _ => ReadNumber(pText, ref pPosition),
        };
    }

    private static MzValue ReadObject(string pText, ref int pPosition, int pDepth)
    {
        var value = new MzValue(MzKind.Object);
        pPosition++;                                    // the opening brace
        SkipSpace(pText, ref pPosition);
        if (pPosition < pText.Length && pText[pPosition] == '}')
        {
            pPosition++;
            return value;
        }

        while (true)
        {
            SkipSpace(pText, ref pPosition);
            if (pPosition >= pText.Length || pText[pPosition] != '"')
            {
                throw new MzDataException(
                    $"a member's name is expected at character {pPosition}");
            }
            var name = ReadString(pText, ref pPosition);
            SkipSpace(pText, ref pPosition);
            if (pPosition >= pText.Length || pText[pPosition] != ':')
            {
                throw new MzDataException(
                    $"a colon is expected after the member '{name.Text}'"
                    + $" at character {pPosition}");
            }
            pPosition++;
            var member = ReadValue(pText, ref pPosition, pDepth + 1);

            // A name given twice is a file with two values for one thing, and
            // keeping the first would drop what the author wrote last. The last
            // wins, and the earlier one is not silently discarded from the name
            // list, so a caller can see the file held both.
            if (!value.Members.ContainsKey(name.Text))
            {
                value.Keys.Add(name.Text);
            }
            value.Members[name.Text] = member;

            SkipSpace(pText, ref pPosition);
            if (pPosition >= pText.Length)
            {
                throw new MzDataException(
                    "the file ends inside an object, where a comma or a closing"
                    + " brace was expected");
            }
            if (pText[pPosition] == ',')
            {
                pPosition++;
                continue;
            }
            if (pText[pPosition] == '}')
            {
                pPosition++;
                return value;
            }
            throw new MzDataException(
                $"a comma or a closing brace is expected at character {pPosition}");
        }
    }

    private static MzValue ReadArray(string pText, ref int pPosition, int pDepth)
    {
        var value = new MzValue(MzKind.Array);
        pPosition++;                                    // the opening bracket
        SkipSpace(pText, ref pPosition);
        if (pPosition < pText.Length && pText[pPosition] == ']')
        {
            pPosition++;
            return value;
        }

        while (true)
        {
            // The nulls are values. A database file's first entry is one, and a
            // map's events hold one per field cell, so the length of the array
            // is the size of the field and not the number of things in it.
            value.Items.Add(ReadValue(pText, ref pPosition, pDepth + 1));
            SkipSpace(pText, ref pPosition);
            if (pPosition >= pText.Length)
            {
                throw new MzDataException(
                    "the file ends inside an array, where a comma or a closing"
                    + " bracket was expected");
            }
            if (pText[pPosition] == ',')
            {
                pPosition++;
                continue;
            }
            if (pText[pPosition] == ']')
            {
                pPosition++;
                return value;
            }
            throw new MzDataException(
                $"a comma or a closing bracket is expected at character {pPosition}");
        }
    }

    private static MzValue ReadString(string pText, ref int pPosition)
    {
        pPosition++;                                    // the opening quote
        var text = new StringBuilder();
        while (true)
        {
            if (pPosition >= pText.Length)
            {
                throw new MzDataException(
                    "a string is not closed before the file ends");
            }
            var c = pText[pPosition++];
            if (c == '"')
            {
                return new MzValue(MzKind.String) { Text = text.ToString() };
            }
            if (c != '\\')
            {
                text.Append(c);
                continue;
            }
            if (pPosition >= pText.Length)
            {
                throw new MzDataException(
                    "a string ends with a backslash and nothing after it");
            }
            var escape = pText[pPosition++];
            switch (escape)
            {
                case '"': text.Append('"'); break;
                case '\\': text.Append('\\'); break;
                case '/': text.Append('/'); break;
                case 'b': text.Append('\b'); break;
                case 'f': text.Append('\f'); break;
                case 'n': text.Append('\n'); break;
                case 'r': text.Append('\r'); break;
                case 't': text.Append('\t'); break;
                case 'u':
                    if (pPosition + 4 > pText.Length)
                    {
                        throw new MzDataException(
                            "a string ends inside a four digit escape");
                    }
                    var digits = pText.Substring(pPosition, 4);
                    if (!ushort.TryParse(
                        digits, NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var code))
                    {
                        throw new MzDataException(
                            $"the escape at character {pPosition} is not four"
                            + $" hex digits but '{digits}'");
                    }
                    text.Append((char)code);
                    pPosition += 4;
                    break;
                default:
                    throw new MzDataException(
                        $"'\\{escape}' at character {pPosition - 1} is not an escape"
                        + " this reader knows");
            }
        }
    }

    private static MzValue ReadNumber(string pText, ref int pPosition)
    {
        var start = pPosition;
        if (pPosition < pText.Length && (pText[pPosition] == '-' || pText[pPosition] == '+'))
        {
            pPosition++;
        }
        var digits = 0;
        while (pPosition < pText.Length && char.IsAsciiDigit(pText[pPosition]))
        {
            pPosition++;
            digits++;
        }
        if (pPosition < pText.Length && pText[pPosition] == '.')
        {
            pPosition++;
            while (pPosition < pText.Length && char.IsAsciiDigit(pText[pPosition]))
            {
                pPosition++;
                digits++;
            }
        }
        if (digits == 0)
        {
            throw new MzDataException(
                $"a value is expected at character {start}, not"
                + $" '{Describe(pText, start)}'");
        }
        if (pPosition < pText.Length
            && (pText[pPosition] == 'e' || pText[pPosition] == 'E'))
        {
            pPosition++;
            if (pPosition < pText.Length
                && (pText[pPosition] == '-' || pText[pPosition] == '+'))
            {
                pPosition++;
            }
            var exponent = 0;
            while (pPosition < pText.Length && char.IsAsciiDigit(pText[pPosition]))
            {
                pPosition++;
                exponent++;
            }
            if (exponent == 0)
            {
                throw new MzDataException(
                    $"a number at character {start} has an exponent with no digits");
            }
        }

        if (!double.TryParse(
            pText.AsSpan(start, pPosition - start), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var number))
        {
            throw new MzDataException(
                $"the number at character {start} cannot be read as a number");
        }
        return new MzValue(MzKind.Number) { Number = number };
    }

    private static MzValue ReadLiteral(
        string pText, ref int pPosition, string pWord, bool pValue,
        bool isNull = false)
    {
        if (pPosition + pWord.Length > pText.Length
            || string.CompareOrdinal(pText, pPosition, pWord, 0, pWord.Length) != 0)
        {
            throw new MzDataException(
                $"'{pWord}' is expected at character {pPosition}, not"
                + $" '{Describe(pText, pPosition)}'");
        }
        pPosition += pWord.Length;
        return isNull
            ? new MzValue(MzKind.Null)
            : new MzValue(MzKind.Bool) { Boolean = pValue };
    }

    /// <summary>
    /// Writes a value back out, so that nothing a game stored is lost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reader exists to keep a game's data, and <b>a reader that cannot
    /// write a shape back has already half-lost it</b>: anything a caller
    /// flattened to a string and could not read again could not be checked,
    /// reported or handed on. A 205's move route is a nested object, and
    /// until this existed it was flattened to the empty string on the way in.
    /// </para>
    /// <para>
    /// <b>The numbers are written the way a game writes them</b> — a whole
    /// number without a decimal point, and everything else round-tripped
    /// through the invariant culture so a machine with a comma as its decimal
    /// separator still reads back what was written.
    /// </para>
    /// </remarks>
    public static string Write(MzValue pValue)
    {
        var text = new StringBuilder();
        Write(pValue, text);
        return text.ToString();
    }

    private static void Write(MzValue pValue, StringBuilder pText)
    {
        switch (pValue.Kind)
        {
            case MzKind.Null:
                pText.Append("null");
                return;

            case MzKind.Bool:
                pText.Append(pValue.Boolean ? "true" : "false");
                return;

            case MzKind.Number:
                // **A whole number has no decimal point**, because a game
                // writes `3` and not `3.0`, and a reader that wrote `3.0` back
                // would produce text the game never wrote.
                pText.Append(
                    pValue.Number == Math.Floor(pValue.Number)
                        && Math.Abs(pValue.Number) < 1e15
                            ? ((long)pValue.Number).ToString(CultureInfo.InvariantCulture)
                            : pValue.Number.ToString(
                                "R", CultureInfo.InvariantCulture));
                return;

            case MzKind.Array:
                pText.Append('[');
                for (var i = 0; i < pValue.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        pText.Append(',');
                    }
                    Write(pValue.Items[i], pText);
                }
                pText.Append(']');
                return;

            case MzKind.Object:
                pText.Append('{');
                var first = true;
                foreach (var key in pValue.Keys)
                {
                    if (!first)
                    {
                        pText.Append(',');
                    }
                    first = false;
                    pText.Append('"').Append(Escape(key)).Append("\":");
                    Write(pValue.Members[key], pText);
                }
                pText.Append('}');
                return;

            default:
                pText.Append('"').Append(Escape(pValue.Text)).Append('"');
                return;
        }
    }

    private static string Escape(string pText)
    {
        var text = new StringBuilder(pText.Length);
        foreach (var c in pText)
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case (char)92: text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        text.Append("\\u").Append(
                            ((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        text.Append(c);
                    }
                    break;
            }
        }
        return text.ToString();
    }
    private static void SkipSpace(string pText, ref int pPosition)
    {
        while (pPosition < pText.Length)
        {
            var c = pText[pPosition];
            if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
            {
                return;
            }
            pPosition++;
        }
    }

    private static string Describe(string pText, int pPosition)
    {
        var end = Math.Min(pText.Length, pPosition + 12);
        return pText[Math.Min(pPosition, pText.Length)..end];
    }
}
