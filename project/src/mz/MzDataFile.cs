using System;
using System.Collections.Generic;
using System.Text;
using Godot;

namespace UniversalRPG.Web;

/// <summary>
/// The data files an RPG Maker MV or MZ game keeps as plain JSON.
/// </summary>
/// <remarks>
/// <para>
/// Both generations write their database as JSON in a <c>data</c> folder, and
/// neither is encrypted unless the author asked for it. That is what makes a
/// reader possible here without a runtime: a JSON file is a value, and this
/// turns it back into the same values it was written from. **No JavaScript of a
/// game is read, parsed as code, or run** — the data files are read as data and
/// the <c>js</c> folder is a signature, not an input.
/// </para>
/// <para>
/// The shape of these files has one trap that a reader written from the
/// documentation alone walks straight into, and it is worth stating because it
/// cost a day once: <see cref="MapDataReader"/>, <see cref="ActorDataReader"/>
/// and the rest all write a <c>null</c> at index zero. <c>MapInfos.json</c> is an
/// array of seventeen entries whose first is null, and a map's <c>events</c> is a
/// sparse array over the whole field with nulls where no event stands. **Index
/// zero is not an actor, is not a map, and is not an event.** A reader that
/// treats the first element as the first entry of the game reads a null and calls
/// it a database.
/// </para>
/// <para>
/// Every value a game writes is preserved as it was found, including the fields
/// this reader has no name for. An MZ file carries fields from plugins and from
/// the editor's own state, and a reader that dropped the ones it does not know
/// would lose a game's save data the first time anything was written back.
/// </para>
/// </remarks>
public sealed class MzDataFile
{
    private MzDataFile(
        string pRelativePath, string pText, MzValue pRoot, bool pKnownShape)
    {
        RelativePath = pRelativePath;
        Text = pText;
        Root = pRoot;
        HasKnownShape = pKnownShape;
    }

    public string RelativePath { get; }

    /// <summary>The file's own text, kept so a caller can hash what it read.</summary>
    public string Text { get; }

    /// <summary>
    /// The file's one top level value. A JSON file has exactly one, so this is
    /// that value and not a list to be indexed: for a database file it is the
    /// array, whose first element is null, and for a map file it is the object
    /// holding the map's fields.
    /// </summary>
    public MzValue Root { get; }

    /// <summary>
    /// Whether the file is one of the shapes this reader knows by name. A file
    /// that is valid JSON and not one of them is still returned whole; this only
    /// says whether the shortcuts below may be used.
    /// </summary>
    public bool HasKnownShape { get; }

    /// <summary>
    /// The entries of a database file, with the null at index zero left out.
    /// </summary>
    public List<MzValue> Entries
    {
        get
        {
            var entries = new List<MzValue>();
            if (Root.Kind != MzKind.Array)
        {
            return entries;
        }
        foreach (var value in Root.Items)
            {
                if (value.Kind != MzKind.Null)
                {
                    entries.Add(value);
                }
            }

            return entries;
        }
    }

    /// <summary>
    /// Reads one of a game's data files, or refuses it with a reason.
    /// </summary>
    public static MzDataFile Read(
        string pRelativePath, byte[] pBytes, int pMaxBytes = 8 * 1024 * 1024)
    {
        if (pBytes.Length > pMaxBytes)
        {
            return new MzDataFile(
                pRelativePath, "", new MzValue(MzKind.Null), false);
        }

        var text = DecodeUtf8(pBytes);
        if (!MzJson.TryParse(text, out var top, out var failure))
        {
            throw new MzDataException(
                $"{pRelativePath} is not JSON: {failure}");
        }

        return new MzDataFile(
            pRelativePath, text, top, MzShapeNames.IsKnown(pRelativePath));
    }

    public static MzDataFile ReadText(string pRelativePath, string pText)
    {
        if (!MzJson.TryParse(pText, out var top, out var failure))
        {
            throw new MzDataException($"{pRelativePath} is not JSON: {failure}");
        }

        return new MzDataFile(
            pRelativePath, pText, top, MzShapeNames.IsKnown(pRelativePath));
    }

    private static string DecodeUtf8(byte[] pBytes)
    {
        // A game's JSON is written as UTF-8 without a byte order mark, and the
        // editor writes it that way. A file that begins with a mark is still
        // UTF-8, and a file that does not decode as UTF-8 is not this reader's
        // to guess at, so the check is here rather than left to the decoder to
        // substitute a replacement character for.
        if (pBytes.Length >= 3 && pBytes[0] == 0xEF && pBytes[1] == 0xBB
            && pBytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(pBytes, 3, pBytes.Length - 3);
        }

        return new UTF8Encoding(false, true).GetString(pBytes);
    }
}

/// <summary>A value in a game's data, as it was written.</summary>
public sealed class MzValue
{
    public MzValue(MzKind pKind) => Kind = pKind;

    public MzKind Kind { get; }
    public string Text { get; init; } = "";
    public double Number { get; init; }
    public bool Boolean { get; init; }
    public List<MzValue> Items { get; init; } = new();
    public List<string> Keys { get; init; } = new();
    public Dictionary<string, MzValue> Members { get; init; } = new();

    public MzValue? Member(string pName) =>
        Members.TryGetValue(pName, out var value) ? value : null;

    public string StringOr(string pFallback) =>
        Kind == MzKind.String ? Text : pFallback;

    public int IntOr(int pFallback) =>
        Kind == MzKind.Number ? (int)Math.Round(Number) : pFallback;
}

public enum MzKind
{
    Null,
    Bool,
    Number,
    String,
    Array,
    Object,
}

public sealed class MzDataException : Exception
{
    public MzDataException(string pMessage) : base(pMessage) { }
}

internal static class MzShapeNames
{
    public static bool IsKnown(string pPath)
    {
        var name = pPath.GetFile();
        return name is "Actors.json" or "Classes.json" or "Items.json"
            or "Enemies.json" or "Skills.json" or "States.json"
            or "Animations.json" or "MapInfos.json" or "System.json"
            or "CommonEvents.json" || name.StartsWith("Map", StringComparison.Ordinal);
    }
}
