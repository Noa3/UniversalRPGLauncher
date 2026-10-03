using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Mz;
using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The audio commands, and that five of them did nothing before this.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the engine's numbers, read out of the game's own
/// <c>rpg_objects.js</c>:</strong>
/// </para>
/// <code>
/// command241() { AudioManager.playBgm(this._params[0]); return true; }
/// command242() { AudioManager.fadeOutBgm(this._params[0]); return true; }
/// command245() { AudioManager.playBgs(this._params[0]); return true; }
/// command246() { AudioManager.fadeOutBgs(this._params[0]); return true; }
/// command249() { AudioManager.playMe(this._params[0]); return true; }
/// command250() { AudioManager.playSe(this._params[0]); return true; }
/// command251() { AudioManager.stopSe(); return true; }
/// </code>
/// <para>
/// <strong>And I said "245 PlayBGM" for a whole session and it is
/// 245 <c>playBgs</c>.</strong> -- <strong>The numbering is 241,
/// 242, 245, 246, 249, 250, 251</strong>, -- <strong>and the gaps are
/// 243, 244, 247 and 248.</strong>
/// </para>
/// <para>
/// <strong>And five case branches were empty</strong>, -- <strong>
/// one line each and no body</strong>, -- <strong>while
/// <c>PlaySe</c> and <c>StopSe</c> carried the whole path and their
/// own bodies already named all four channels.</strong> -- <strong>So
/// the music of this game has never reached a channel.</strong>
/// </para>
/// </remarks>
public partial class TestMzAudiodaten : TestBase
{
    private static MzCommandEntry Befehl(int pCode, string pObjekt)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":0,\"parameters\":["
            + pObjekt + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }

    private const string Scene = "{\"name\":\"Scene8\",\"volume\":40,"
        + "\"pitch\":80,\"pan\":10}";

    /// <summary>
    /// And the four play commands reach four different channels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the number 41 appears three times</strong>, --
    /// <strong>and that is what decides whether this is a
    /// measurement or a claim</strong>, -- <strong>and once is
    /// silence</strong>, -- <strong>which is what a reader that
    /// always sets a name would break.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVierKanaele()
    {
        var fakten = new MzBranchFacts();
        var aktionen = new List<MzAction>();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(241, Scene),
            Befehl(245, Scene),
            Befehl(249, Scene),
            Befehl(250, Scene),
            Befehl(0, "\"\""),
        });

        interp.Setup(1, 1);
        interp.Run(aktionen, fakten);

        Console.WriteLine($"bgm {fakten.Screen.Bgm}");
        Console.WriteLine($"bgs {fakten.Screen.Bgs}");
        Console.WriteLine($"me  {fakten.Screen.Me}");
        Console.WriteLine($"se  {fakten.Screen.Se}");

        AssertEq("Scene8", fakten.Screen.Bgm.Name,
            "**and 241 fills the music channel** -- and it was an"
                + " empty branch before this");
        AssertEq("Scene8", fakten.Screen.Bgs.Name,
            "**and 245 fills the background sound channel**");
        AssertEq("Scene8", fakten.Screen.Me.Name,
            "**and 249 fills the one that plays alone**");
        AssertEq("Scene8", fakten.Screen.Se.Name,
            "**and 250 fills the sound effect**");

        AssertEq(40, fakten.Screen.Bgm.Volume,
            "**and the volume is the object's own number** -- and"
                + " the parameter is an object, not four integers");
        AssertEq(80, fakten.Screen.Bgm.Pitch,
            "**and so is the pitch**");
        AssertEq(10, fakten.Screen.Bgm.Pan,
            "**and so is the pan**");
    }

    /// <summary>
    /// And 41 means silence, and 42 fades out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>41</c> is <c>None</c> in the engine</strong>
    /// -- <strong>and a reader that always set a name would give a
    /// channel a file that the game asked to have
    /// emptied.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEinsUndVierzigIstStille()
    {
        var fakten = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(241, Scene),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq("Scene8", fakten.Screen.Bgm.Name,
            "**and it plays**");

        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(241, "{\"name\":\"\",\"volume\":0,"
                + "\"pitch\":0,\"pan\":0}"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);

        AssertTrue(fakten.Screen.Bgm.Name.Length == 0,
            "**and 41 empties the channel** -- and that is what the"
                + " game's own data means by it");
    }

    /// <summary>
    /// And the two fades and the stop, on three channels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the two fades were empty branches too</strong>,
    /// -- <strong>and `StopSe` already had the code that names all
    /// three cases</strong>, -- <strong>so the bodies were written
    /// and then never reached.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieAusblendungen()
    {
        var fakten = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(241, Scene),
            Befehl(245, Scene),
            Befehl(249, Scene),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);

        AssertTrue(fakten.Screen.Bgm.FadeFramesLeft == 0,
            "**and nothing is fading yet**");

        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(242, "30"),
            Befehl(246, "45"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(30, fakten.Screen.Bgm.FadeFramesLeft,
            "**and 242 fades the music over thirty frames** -- and"
                + " it was an empty branch before this");
        AssertEq(45, fakten.Screen.Bgs.FadeFramesLeft,
            "**and 246 fades the background sound over forty-five**");

        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(251, "\"\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(0, fakten.Screen.Se.FadeFramesLeft,
            "**and 251 stops the sound effect at once** -- and 251"
                + " carries no parameter, and the engine writes"
                + " `AudioManager.stopSe()`");
    }

    /// <summary>
    /// And the game's own files are named the way the data names them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 106 BGM files and 46 BGS</strong>, -- <strong>and
    /// a name the data gives must be a file that is there</strong>,
    /// -- <strong>and this is where a wrong name would show
    /// itself.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDateienGibtEs()
    {
        var ordner = new Dictionary<string, string>
        {
            ["bgm"] = "D:/Itch/sister/www/audio/bgm",
            ["bgs"] = "D:/Itch/sister/www/audio/bgs",
            ["se"] = "D:/Itch/sister/www/audio/se",
            ["me"] = "D:/Itch/sister/www/audio/me",
        };

        Console.WriteLine(string.Join("  ", ordner.Select(x =>
            x.Key + " " + Directory.GetFiles(x.Value, "*.rpgmvo").Length)));

        AssertEq(106,
            Directory.GetFiles(ordner["bgm"], "*.rpgmvo").Length,
            "**and 106 music files**");
        AssertEq(46,
            Directory.GetFiles(ordner["bgs"], "*.rpgmvo").Length,
            "**and 46 background sounds**");
        AssertEq(557,
            Directory.GetFiles(ordner["se"], "*.rpgmvo").Length,
            "**and 557 sound effects, and the game plays one of them"
                + " 1012 times**");
    }
}
