using System;
using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a save reaches the disk, and comes back, and a failure eats nothing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the format is measured in the project's own
/// <c>rmmz_managers.js</c>:</strong>
/// </para>
/// <code>
/// StorageManager.jsonToZip = function(json) {
///     const zip = pako.deflate(json, { to: "string", level: 1 });
///     ...
/// };
/// StorageManager.filePath = function(saveName) {
///     return this.fileDirectoryPath() + saveName + ".rmmzsave";
/// };
/// DataManager.makeSavename = function(savefileId) {
///     return "file%1".format(savefileId);
/// };
/// </code>
/// <para>
/// <strong>So the file is zlib-compressed UTF-8 JSON</strong>, <strong>and
/// the first two bytes of a zlib stream are <c>0x78</c></strong>, and a
/// reader that wrote plain JSON would produce a file a real game cannot
/// open.
/// </para>
/// </remarks>
public partial class TestMzSaveStore : TestBase
{
    /// <summary>
    /// And the bytes on the disk are zlib, and the name is the engine's.
    /// </summary>
    public void Test_DieDateiIstZlibUndTraegtDenNamenDerEngine()
    {
        var ordner = NeuerOrdner();
        try
        {
            var (inhalt, _) = MzSaveContents.Capture(Fakten(), 1, 900);
            AssertTrue(MzSaveStore.Save(ordner, MzSaveStore.SlotName(1), inhalt, out var problem),
                "**and the save is written** -- and the store said: " + problem);

            // **And the path is the engine's own.** Measured:
            // `makeSavename` is "file" + id and `filePath` appends
            // ".rmmzsave", so slot one is save/file1.rmmzsave.
            var pfad = MzSaveStore.PathOf(ordner, "file1");
            AssertTrue(File.Exists(pfad),
                "**and it sits where the engine would put it** -- and the "
                    + $"folder holds: {string.Join(", ", Directory.GetFiles(ordner))}");
            AssertTrue(pfad.EndsWith("file1.rmmzsave", StringComparison.Ordinal),
                "**and its name ends in .rmmzsave** -- and the name is the "
                + "engine's and not this launcher's, so the saves stay "
                + "portable");

            // **And it is zlib and not text.** Measured: pako.deflate
            // writes the two-byte zlib header, and 0x78 is what every real
            // save starts with.
            var bytes = File.ReadAllBytes(pfad);
            Console.WriteLine($"MZ save: file1.rmmzsave is {bytes.Length} bytes, "
                + $"starts {bytes[0]:X2} {bytes[1]:X2}");
            AssertEq(bytes[0], 0x78,
                "**and the first byte is 0x78** -- which is the zlib header, "
                    + "and a reader that wrote plain JSON would fail here");
            AssertFalse(bytes[0] == (byte)'{',
                "**and it does not start with a brace** -- so it is not the "
                    + "JSON itself");

            // **And no leftover is left behind.**
            AssertFalse(File.Exists(pfad + "_"),
                "**and the temporary file is gone**");
            AssertFalse(File.Exists(pfad + "__"),
                "**and so is the backup** -- and a save that left one would "
                    + "leave a file the engine would never write");
        }
        finally
        {
            Aufraeumen(ordner);
        }
    }

    /// <summary>
    /// And what was written is read back through the file.
    /// </summary>
    public void Test_DieDateiGibtDieselbenWerteZurueck()
    {
        var ordner = NeuerOrdner();
        try
        {
            var (inhalt, _) = MzSaveContents.Capture(Fakten(), 7, 4321);
            MzSaveStore.Save(ordner, MzSaveStore.SlotName(7), inhalt, out _);

            AssertTrue(MzSaveStore.TryLoad(
                    ordner, MzSaveStore.SlotName(7), out var gelesen, out var problem),
                "**and it reads back** -- and the store said: " + problem);

            AssertTrue(MzSaveContents.TryRestore(gelesen!, out var geladen, out var fehler),
                "**and the contents read back** -- and the reader said: " + fehler);
            AssertEq(geladen!.SavefileId, 7,
                "**and the slot number survived the file** -- and it is "
                    + "written into the save itself, which is what a game "
                    + "reads back to know where it is");
            AssertEq(geladen.FramesOnSave, 4321,
                "**and the frame it was saved at**");
            AssertTrue(geladen.Switches.TryGetValue(5, out var an) && an,
                "**and the switch is still on**");
            AssertEq(geladen.PlayerX, 4, "**and the player is still where it was**");

            // **And the slot list is what a load screen would draw.**
            var slots = MzSaveStore.Slots(ordner);
            AssertEq(slots.Count, 1, "**and one slot has a save**");
            AssertEq(slots[0], 7,
                "**and it is slot seven** -- and the list counts from one, "
                    + "because `emptySavefileId` starts at one and slot zero "
                    + "is not a slot the engine offers");
        }
        finally
        {
            Aufraeumen(ordner);
        }
    }

    /// <summary>
    /// And a missing file is a refusal and not an empty save.
    /// </summary>
    /// <remarks>
    /// <strong>Measured:</strong> the engine's own
    /// <c>loadFromLocalFile</c> rejects with
    /// <c>new Error("Savefile not found")</c>. <strong>And a reader that
    /// returned an empty object would let a game start a new one over a slot
    /// the player thought was full.</strong>
    /// </remarks>
    public void Test_EineFehlendeDateiIstEineAbsage()
    {
        var ordner = NeuerOrdner();
        try
        {
            AssertFalse(MzSaveStore.Exists(ordner, MzSaveStore.SlotName(3)),
                "**and the slot has no file**");
            AssertFalse(MzSaveStore.TryLoad(
                    ordner, MzSaveStore.SlotName(3), out var gelesen, out var problem),
                "**and reading it fails**");
            AssertTrue(gelesen == null,
                "**and it hands back nothing at all** -- and an empty object "
                    + "would be an empty save");
            AssertTrue(problem.Contains("no save"),
                "**and it says why** -- and it said: " + problem);
            AssertEq(MzSaveStore.Slots(ordner).Count, 0,
                "**and the slot list is empty** -- and a load screen that "
                    + "listed it would offer a slot that cannot be read");
        }
        finally
        {
            Aufraeumen(ordner);
        }
    }

    /// <summary>
    /// And a save that cannot be written eats nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test that matters most in this file.</strong>
    /// The engine's own <c>saveToLocalFile</c> moves the old file aside
    /// first and puts it back if the write throws:
    /// </para>
    /// <code>
    /// this.fsUnlink(backupFilePath);
    /// this.fsRename(filePath, backupFilePath);
    /// try {
    ///     this.fsWriteFile(filePath, zip);
    ///     this.fsUnlink(backupFilePath);
    ///     resolve();
    /// } catch (e) {
    ///     try { this.fsUnlink(filePath); this.fsRename(backupFilePath, filePath); }
    ///     catch (e2) { }
    ///     reject(e);
    /// }
    /// </code>
    /// <para>
    /// The failure is forced by a non-finite value that cannot be serialized
    /// into valid JSON. The safety suite also holds a Windows sharing lock
    /// on the destination to verify preservation after a failed atomic swap.
    /// </para>
    /// </remarks>
    public void Test_EinFehlgeschlagenerSchreibvorgangFrisstNichts()
    {
        var ordner = NeuerOrdner();
        try
        {
            var (erster, _) = MzSaveContents.Capture(Fakten(), 2, 111);
            AssertTrue(MzSaveStore.Save(ordner, MzSaveStore.SlotName(2), erster, out _),
                "**and the first save lands**");

            // Block serialization before the atomic commit, independent of
            // the staging-file naming strategy.
            var zweiter = new MzValue(MzKind.Number) { Number = double.NaN };
            AssertFalse(MzSaveStore.Save(ordner, MzSaveStore.SlotName(2), zweiter, out var problem),
                "**and the second save fails** -- and it said: " + problem);

            // **And the first save is still there, and still readable, and
            // still says 111.**
            AssertTrue(MzSaveStore.TryLoad(
                    ordner, MzSaveStore.SlotName(2), out var gelesen, out var fehler),
                "**and the save that was there is still readable** -- and "
                    + "the store said: " + fehler);
            AssertTrue(MzSaveContents.TryRestore(gelesen!, out var geladen, out _),
                "**and its contents read back**");
            AssertEq(geladen!.FramesOnSave, 111,
                "**and it is the FIRST save and not the second** -- and a "
                    + "reader that wrote straight into the file would have "
                    + "lost the game the player had");
        }
        finally
        {
            Aufraeumen(ordner);
        }
    }

    /// <summary>And the slot names are the engine's own.</summary>
    public void Test_DieNamenDerSpeicherplaetzeSindDieDerEngine()
    {
        AssertEq(MzSaveStore.SlotName(1), "file1",
            "**and slot one is file1** -- measured: `makeSavename` is "
                + "\"file%1\".format(savefileId)");
        AssertEq(MzSaveStore.SlotName(20), "file20", "**and slot twenty is file20**");
        AssertEq(MzSaveStore.GlobalName, "global",
            "**and the info file is called global** -- measured: "
                + "`DataManager.saveGlobalInfo` writes "
                + "`StorageManager.saveObject(\"global\", this._globalInfo)`");
        AssertEq(MzSaveStore.MaxSavefiles, 20,
            "**and there are twenty slots** -- measured: "
                + "`DataManager.maxSavefiles` returns 20");
        AssertEq(MzSaveStore.Suffix, ".rmmzsave",
            "**and the suffix is .rmmzsave**");
    }

    private static MzBranchFacts Fakten()
    {
        var spieler = new MzPlayer();
        spieler.StandAt(3, 4, 11, 2);

        return new MzBranchFacts
        {
            Player = spieler,
            Switches = { [5] = true },
            Variables = { [9] = 42 },
            SelfSwitches = { ["3,9,A"] = true },
        };
    }

    private static string NeuerOrdner() =>
        Path.Combine(
            Path.GetTempPath(), "urpg-mz-save-" + Guid.NewGuid().ToString("N"));

    private static void Aufraeumen(string pOrdner)
    {
        try
        {
            if (Directory.Exists(pOrdner))
            {
                Directory.Delete(pOrdner, true);
            }
        }
        catch (IOException)
        {
            // **And a scratch folder that will not go is not worth failing a
            // test over**; the operating system will take it in its own time.
        }
    }
}
