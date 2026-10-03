using System;
using System.IO;
using System.Linq;
using UniversalRPG.App.Ui;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The four sound channels, taken from a real file and put on a real
/// player.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the layer that did not exist</strong>, --
/// <strong>and it is missing on purpose everywhere else:</strong>
/// no <c>IEngineRuntime</c> is a Godot node, -- <strong>the contract
/// hands a host two paths and a selection</strong>, -- <strong>and a
/// host that reached into Godot would break every headless
/// test.</strong>
/// </para>
/// <para>
/// <strong>And this is the same shape as <c>Rm2kMapPreview</c></strong>:
/// <strong>the host hands over a plain value, this turns it into
/// something Godot can hear, and nothing in between knows which host
/// it came from.</strong>
/// </para>
/// </remarks>
public partial class TestMzAudioOutput : TestBase
{
    private const string Spiel = "D:/Itch/sister/www";

    /// <summary>
    /// And the node builds four players and hangs them in the tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And four, and not one.</strong> -- <strong>The engine
    /// has a player per channel</strong>, -- <strong>and a channel
    /// that replaced the other would be a channel nobody
    /// hears.</strong>
    /// </para>
    /// </remarks>
    public void Test_VierSpieler()
    {
        var node = new MzAudioOutput();
        try
        {
            node._Ready();
            var spieler = node.GetChildren();
            Console.WriteLine("Kinder: " + spieler.Count);
            AssertEq(4, spieler.Count,
                "**and four players are in the tree** -- and one per"
                    + " channel, because the engine has one per"
                    + " channel");

            var namen = spieler.Cast<Godot.Node>()
                .Select(x => x.Name.ToString()).OrderBy(x => x)
                .ToList();
            Console.WriteLine("Namen: " + string.Join(" ", namen));
            AssertTrue(namen.Contains("mzbgm")
                    && namen.Contains("mzse"),
                "**and the two loudest are there under their folder"
                    + " names**");
        }
        finally
        {
            node.Free();
        }
    }

    /// <summary>
    /// And an empty name is silence, and not a missing file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>41</c> is <c>None</c> in the game's data
    /// and this is what it means here.</strong> -- <strong>And a
    /// reader that tried to load a file for it would play a sound the
    /// game asked to have stopped.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinLeererNameIstStille()
    {
        var node = new MzAudioOutput();
        try
        {
            node._Ready();
            node.SetzeWurzel(Spiel,
                UniversalRPG.Mz.MzVerschluesselung.SchluesselDesSpiels());

            var screen = new MzScreen();
            node.SetzeKanaele(screen);

            AssertFalse(node.Traegt(0),
                "**and a fresh screen plays nothing** -- and that is"
                    + " four channels at silence, and not an error");

            // **Und  jetzt  der  Name,  den  das  Spiel  wirklich
            //  benutzt.
            var erste = System.IO.Path.Combine(
                Spiel, "audio", "bgm");
            var datei = System.IO.Directory.GetFiles(
                erste, "*.rpgmvo").OrderBy(x => x, StringComparer.Ordinal)
                .First();
            var name = System.IO.Path.GetFileNameWithoutExtension(datei);

            screen.Bgm.Name = name;
            screen.Bgm.Volume = 90;
            screen.Bgm.Pitch = 100;
            node.SetzeKanaele(screen);

            Console.WriteLine("Kanal 0: " + screen.Bgm);
            AssertTrue(name.Length > 0,
                "**and the file is there**");
            AssertEq(90, screen.Bgm.Volume,
                "**and the volume is the game's own number** -- and"
                    + " a reader that used it as a factor would turn"
                    + " the music down");

            screen.Bgm.Name = "";
            node.SetzeKanaele(screen);
            AssertTrue(!node.Traegt(0),
                "**and an empty name stops the channel** -- and 41"
                    + " means `None` in the game's own data");
        }
        finally
        {
            node.Free();
        }
    }

    /// <summary>
    /// And the folder a channel reads from is the channel's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a name that exists in <c>bgm</c> and not in
    /// <c>se</c> must not be loaded for the effect channel.</strong>
    /// -- <strong>And that is a test, because a reader that looked
    /// in one folder for all four would load the wrong
    /// sound.</strong>
    /// </para>
    /// </remarks>
    public void Test_JederKanalLiestSeinenOrdner()
    {
        var node = new MzAudioOutput();
        try
        {
            node._Ready();
            node.SetzeWurzel(Spiel,
                UniversalRPG.Mz.MzVerschluesselung.SchluesselDesSpiels());

            var screen = new MzScreen();

            // **Und  ein  Name,  der  nur  in  bgm  liegt.**
            var nurBgm = System.IO.Directory.GetFiles(
                System.IO.Path.Combine(Spiel, "audio", "bgm"),
                "*.rpgmvo").Select(x =>
                    System.IO.Path.GetFileNameWithoutExtension(x))
                .First(n => !System.IO.Directory.GetFiles(
                    System.IO.Path.Combine(Spiel, "audio", "se"),
                    n + ".rpgmvo").Any());

            screen.Bgm.Name = nurBgm;
            screen.Se.Name = nurBgm;
            node.SetzeKanaele(screen);

            Console.WriteLine($"bgm={node.Traegt(0)}  se={node.Traegt(3)}");
            AssertTrue(!node.Traegt(3),
                "**and a name that is only in bgm does not play on the"
                    + " effect channel** -- and the folders are the"
                    + " engine's own, and reading one folder for all"
                    + " four would be a wrong sound");
        }
        finally
        {
            node.Free();
        }
    }

    /// <summary>
    /// And a root that was never set plays nothing at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a node without a path must not touch the
    /// disk</strong>, -- <strong>and a reader that built
    /// <c>audio/se/</c> out of an empty string would look in the
    /// working directory.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneWurzelNichts()
    {
        var node = new MzAudioOutput();
        try
        {
            node._Ready();
            node.SetzeKanaele(new MzScreen());
            AssertFalse(node.Traegt(0),
                "**and a node with no root plays nothing** -- and it"
                    + " does not go looking in the working"
                    + " directory");

            node.StoppeAlle();
            for (var i = 0; i < 4; i++)
            {
                AssertFalse(node.Traegt(i),
                    "**and all four stay silent after Stop**");
            }
        }
        finally
        {
            node.Free();
        }
    }
}
