using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UniversalRPG.Web;

/// <summary>
/// The files an MZ game saves into, written the way the engine writes them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the format is measured in the project's own
/// <c>rmmz_managers.js</c>:</strong>
/// </para>
/// <code>
/// StorageManager.saveObject = function(saveName, object) {
///     return this.objectToJson(object)
///         .then(json => this.jsonToZip(json))
///         .then(zip => this.saveZip(saveName, zip));
/// };
/// StorageManager.jsonToZip = function(json) {
///     const zip = pako.deflate(json, { to: "string", level: 1 });
///     if (zip.length >= 50000) { console.warn("Save data is too big."); }
///     return zip;
/// };
/// StorageManager.filePath = function(saveName) {
///     return this.fileDirectoryPath() + saveName + ".rmmzsave";
/// };
/// </code>
/// <para>
/// <strong>So a save file is zlib-compressed UTF-8 JSON</strong> -- not a
/// zip archive, whatever its name suggests, and not plain text. <strong>And
/// a reader that wrote plain JSON would produce a file a real MZ game
/// cannot open</strong>, and one that read plain JSON would fail on every
/// save a real game wrote.
/// </para>
/// <para>
/// <strong>And the directory is given to this store and not taken from the
/// game.</strong> The engine writes into the game's own folder, because the
/// engine owns that folder. <strong>An imported game is someone else's
/// folder</strong> -- it may be read-only, it may be shared, and it is not
/// this launcher's to write into. <strong>The file names inside are the
/// engine's own</strong>, so the saves stay portable.
/// </para>
/// </remarks>
public static class MzSaveStore
{
    /// <summary>
    /// How many slots the engine offers, which is
    /// <c>DataManager.maxSavefiles</c>.
    /// </summary>
    public const int MaxSavefiles = 20;

    /// <summary>
    /// The engine's own suffix, which is
    /// <c>StorageManager.filePath</c>'s <c>".rmmzsave"</c>.
    /// </summary>
    public const string Suffix = ".rmmzsave";

    /// <summary>
    /// The file name of a slot, which is
    /// <c>DataManager.makeSavename</c>'s <c>"file%1".format(id)</c>.
    /// </summary>
    public static string SlotName(int pSavefileId) => "file" + pSavefileId;

    /// <summary>
    /// The name of the global info file, which is what
    /// <c>DataManager.saveGlobalInfo</c> writes.
    /// </summary>
    public const string GlobalName = "global";

    /// <summary>The path a slot is stored at.</summary>
    public static string PathOf(string pDirectory, string pName) =>
        Path.Combine(pDirectory, pName + Suffix);

    /// <summary>
    /// Writes an object into a slot, the way <c>saveObject</c> does.
    /// </summary>
    /// <param name="pDirectory">Where this game's saves live.</param>
    /// <param name="pName">The engine's name for the file.</param>
    /// <param name="pObject">The contents, already in the engine's shape.</param>
    /// <param name="pProblem">What went wrong, when something did.</param>
    /// <returns>Whether the file was written.</returns>
    /// <remarks>
    /// <strong>And a save that fails must not eat the save that was
    /// there.</strong> The engine's own <c>saveToLocalFile</c> moves the old
    /// file aside first and puts it back if the write throws, and this
    /// keeps that contract with the swap the platform does atomically --
    /// <strong>because a save that a crash left half-written is a save that
    /// a player loses a game to.</strong>
    /// </remarks>
    public static bool Save(
        string pDirectory,
        string pName,
        MzValue pObject,
        out string pProblem)
    {
        pProblem = "";
        var path = PathOf(pDirectory, pName);
        var temp = path + "_";
        var backup = path + "__";

        try
        {
            Directory.CreateDirectory(pDirectory);
            File.WriteAllBytes(temp, Zip(MzJson.Write(pObject)));

            if (File.Exists(path))
            {
                File.Replace(temp, path, backup, true);
                File.Delete(backup);
            }
            else
            {
                File.Move(temp, path);
            }

            return true;
        }
        catch (Exception ausnahme)
        {
            pProblem = $"the save could not be written: {ausnahme.Message}";
            TryDelete(temp);
            TryDelete(backup);
            return false;
        }
    }

    /// <summary>
    /// Reads an object back out of a slot.
    /// </summary>
    /// <param name="pDirectory">Where this game's saves live.</param>
    /// <param name="pName">The engine's name for the file.</param>
    /// <param name="pObject">The contents, when they could be read.</param>
    /// <param name="pProblem">What went wrong, when something did.</param>
    /// <returns>Whether a file was there and could be read.</returns>
    /// <remarks>
    /// <strong>And a file that is not there is a refusal and not an empty
    /// save.</strong> The engine's own <c>loadFromLocalFile</c> rejects with
    /// <c>new Error("Savefile not found")</c> -- <strong>and a reader that
    /// returned an empty object would let a game start a new one over a
    /// slot the player thought was full.</strong>
    /// </remarks>
    public static bool TryLoad(
        string pDirectory,
        string pName,
        out MzValue? pObject,
        out string pProblem)
    {
        pObject = null;
        pProblem = "";
        var path = PathOf(pDirectory, pName);

        if (!File.Exists(path))
        {
            pProblem = $"there is no save at {path}";
            return false;
        }

        try
        {
            var json = Unzip(File.ReadAllBytes(path));
            if (!MzJson.TryParse(json, out var gelesen, out var fehler))
            {
                pProblem = $"the save at {path} is not readable JSON: {fehler}";
                return false;
            }

            pObject = gelesen;
            return true;
        }
        catch (Exception ausnahme)
        {
            pProblem = $"the save at {path} could not be read: "
                + $"{ausnahme.Message}";
            return false;
        }
    }

    /// <summary>Whether a slot has a file, which is <c>savefileExists</c>.</summary>
    public static bool Exists(string pDirectory, string pName) =>
        File.Exists(PathOf(pDirectory, pName));

    /// <summary>Removes a slot's file, which is <c>StorageManager.remove</c>.</summary>
    public static bool Remove(string pDirectory, string pName)
    {
        var path = PathOf(pDirectory, pName);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>
    /// The slots that have a save, which is what a load screen lists.
    /// </summary>
    /// <remarks>
    /// <strong>And it counts from one</strong>, because
    /// <c>DataManager.emptySavefileId</c> starts at 1 and slot 0 is not a
    /// slot the engine offers.
    /// </remarks>
    public static IReadOnlyList<int> Slots(string pDirectory)
    {
        var slots = new List<int>();
        for (var id = 1; id <= MaxSavefiles; id++)
        {
            if (Exists(pDirectory, SlotName(id)))
            {
                slots.Add(id);
            }
        }

        return slots;
    }

    /// <summary>
    /// zlib deflate, which is what <c>pako.deflate</c> produces.
    /// </summary>
    /// <remarks>
    /// <strong>And it is zlib and not raw deflate.</strong>
    /// <c>pako.deflate</c> writes the two-byte zlib header and its own
    /// checksum, and <c>pako.inflate</c> reads them -- <strong>so a reader
    /// that used <c>DeflateStream</c> would write a file no MZ game could
    /// open, and the first two bytes of every real save are
    /// <c>0x78</c>.</strong>
    /// </remarks>
    public static byte[] Zip(string pJson)
    {
        using var ausgabe = new MemoryStream();
        using (var deflate = new ZLibStream(
            ausgabe, CompressionLevel.Fastest, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(pJson);
            deflate.Write(bytes, 0, bytes.Length);
        }

        return ausgabe.ToArray();
    }

    /// <summary>zlib inflate, which is what <c>pako.inflate</c> reads.</summary>
    public static string Unzip(byte[] pZip)
    {
        using var eingabe = new MemoryStream(pZip);
        using var inflate = new ZLibStream(eingabe, CompressionMode.Decompress);
        using var ausgabe = new MemoryStream();
        inflate.CopyTo(ausgabe);
        return Encoding.UTF8.GetString(ausgabe.ToArray());
    }

    private static void TryDelete(string pPath)
    {
        try
        {
            if (File.Exists(pPath))
            {
                File.Delete(pPath);
            }
        }
        catch (IOException)
        {
            // **And a file that cannot be cleaned up is not worth losing the
            // save over.** The write itself already reported what went
            // wrong.
        }
    }
}
