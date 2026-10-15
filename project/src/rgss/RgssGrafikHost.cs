using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// Drives the runtime's frame while a game's own loop is running.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the inversion a real RGSS host has.</strong> XP's
/// <c>Scene_Title#main</c> is a <c>loop do ... end</c> whose body calls
/// <c>Graphics.update</c>, <c>Input.update</c> and the scene's own update --
/// **the loop belongs to the game**, and <c>Graphics.update</c> is the frame:
/// it presents one and returns, and the game calls it again.
/// </para>
/// <para>
/// <strong>And that is why this host exists and the module does not answer
/// it.</strong> Measured: the built-in <c>Graphics</c> module counted frames in
/// Ruby, so the game's own <c>main</c> was never run -- the runtime called
/// <c>$scene.update</c> instead, the scene's setup (where XP builds its windows
/// and sprites) never happened, and nothing was drawn while every report said
/// zero problems. <strong>And a frame cannot be counted in Ruby</strong>: a
/// frame is the host's work, and the host is where the surface will be.
/// </para>
/// <para>
/// <strong>And the budget is the only way out, and it is honest.</strong> The
/// game's loop has no exit until the player leaves the scene, so a host that
/// runs a game must be able to say "that is enough frames" -- and it says it by
/// throwing out of <c>update</c>, which unwinds the game's own loop the same way
/// a real quit does. **And a run that stopped at its budget is not a
/// failure**: it is a run that measured what it came for.
/// </para>
/// </remarks>
public sealed class RgssGrafikHost : IRubyHost
{
    /// <summary>The host this one asks when the name is not its own.</summary>
    public IRubyHost? Fallback { get; set; }

    /// <summary>The game's own folder, which is where its images are.</summary>
    public string? Wurzel { get; set; }

    /// <summary>
    /// The folder each kind of image lives in, in XP's own layout.
    /// </summary>
    /// <remarks>
    /// <strong>And these are XP's names and not a reading of one game.</strong>
    /// Measured, the game this runtime is tested against ships three folders of
    /// its own -- Battlers, Pictures, Titles -- and takes its windowskin from the
    /// RTP, which its Game.ini names as RTP1=Standard; a cache built from that one
    /// game would find a windowskin in one game in a thousand.
    /// </remarks>
    private static readonly Dictionary<string, string> Ordner =
        new(StringComparer.Ordinal)
        {
            ["windowskins"] = "Windowskins",
            ["pictures"] = "Pictures",
            ["battlebacks"] = "Battlebacks",
            ["animations"] = "Animations",
            ["characters"] = "Characters",
            ["tilesets"] = "Tilesets",
            ["autotiles"] = "Autotiles",
            ["battlers"] = "Battlers",
            ["fogs"] = "Fogs",
            ["panoramas"] = "Panoramas",
            ["titles"] = "Titles",
        };

    /// <summary>The width in pixels, 640 for RGSS 1.0.</summary>
    public int Breite { get; } = 640;

    /// <summary>The height in pixels, 480 for RGSS 1.0.</summary>
    public int Hoehe { get; } = 480;

    /// <summary>The frames this host has presented.</summary>
    public int Frames { get; private set; }

    /// <summary>
    /// How many frames to present before stopping the game's own loop.
    /// </summary>
    /// <remarks>
    /// <strong>And zero means no budget, which is what a real game wants.</strong>
    /// A measurement sets it; a player does not.
    /// </remarks>
    public int Budget { get; set; }

    /// <summary>Whether the run ended because the budget was reached.</summary>
    public bool BudgetErreicht { get; private set; }

    /// <summary>
    /// What the runtime does for one frame.
    /// </summary>
    /// <remarks>
    /// <strong>And this is where the surface will be.</strong> Today it is the
    /// frame count and nothing else, because there is nothing to draw on yet.
    /// </remarks>
    public Action<int>? Rahmen { get; set; }

    /// <summary>The names this host answers, and its fallback's.</summary>
    public IReadOnlyList<string> KnownMethods
    {
        get
        {
            var namen = new List<string> { "update", "wait" };
            if (Fallback != null)
            {
                namen.AddRange(Fallback.KnownMethods);
            }

            return namen;
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver, string pMethod, IReadOnlyList<RubyValue> pArguments)
    {
        // **And the cache asks for a size by a bare name**, which is how a
        // module reaches the host for something only the host can do -- reading
        // a file. **And the name is the host's own**, so it is answered here and
        // nowhere else.
        if (pMethod == "BildGroesse")
        {
            var art = pArguments.Count > 0 && pArguments[0].Kind == RubyValueKind.Symbol
                ? pArguments[0].Name ?? string.Empty
                : string.Empty;
            var name = pArguments.Count > 1 && pArguments[1].Kind == RubyValueKind.String
                ? System.Text.Encoding.UTF8.GetString(pArguments[1].Bytes)
                : string.Empty;
            return BildGroesse(art, name);
        }

        if (pReceiver.Kind != RubyValueKind.Symbol || pReceiver.Name != "Graphics")
        {
            return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }

        switch (pMethod)
        {
            case "update":
                Frames++;
                Rahmen?.Invoke(Frames);

                // **And the budget ends the run, and it ends it loudly enough to
                // unwind the game's own loop.** A silent return would leave the
                // game looping and the measurement waiting.
                if (Budget > 0 && Frames >= Budget)
                {
                    BudgetErreicht = true;
                    throw new RgssFrameBudgetErreicht(Frames);
                }

                return RubyValue.Nil;

            case "wait":
                // **And `wait` is a number of frames, and each one is a frame.**
                var anzahl = pArguments.Count > 0
                    && pArguments[0].Kind == RubyValueKind.Integer
                        ? (int)pArguments[0].Integer
                        : 0;
                for (var i = 0; i < anzahl; i++)
                {
                    Frames++;
                    Rahmen?.Invoke(Frames);
                    if (Budget > 0 && Frames >= Budget)
                    {
                        BudgetErreicht = true;
                        throw new RgssFrameBudgetErreicht(Frames);
                    }
                }

                return RubyValue.Nil;

            default:
                return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield) =>
        Fallback?.CallMethodWithBlock(pReceiver, pMethod, pArguments, pYield);

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName) => Fallback?.LookupConstant(pName);
    /// <summary>
    /// The size of an image, or nil and a diagnostic when there is none.
    /// </summary>
    /// <remarks>
    /// <strong>And the size is read out of the file and not assumed.</strong> A
    /// bitmap whose size was a placeholder would be one a game lays out against
    /// numbers nobody measured, and a window that centres text in it would centre
    /// it in the wrong place. The PNG header carries the size in its first bytes,
    /// so the file is read and the real numbers come back.
    /// </remarks>
    public RubyValue? BildGroesse(string pArt, string pName)
    {
        if (Wurzel == null || !Ordner.TryGetValue(pArt, out var ordner))
        {
            _fehlende.Add(pArt + "/" + pName);
            return null;
        }

        var pfad = Path.Combine(Wurzel, "Graphics", ordner, pName + ".png");
        if (!File.Exists(pfad))
        {
            // **And a name that is not there is said by name.** A game that draws
            // with a nil bitmap should know that it is drawing nothing -- and the
            // RTP is a real answer to where it is, which is why the name of the
            // file is reported and not a bare nil.
            _fehlende.Add("Graphics/" + ordner + "/" + pName + ".png");
            return null;
        }

        var kopf = new byte[24];
        using (var strom = File.OpenRead(pfad))
        {
            if (strom.Read(kopf, 0, kopf.Length) < 24)
            {
                _fehlende.Add("Graphics/" + ordner + "/" + pName + ".png (truncated)");
                return null;
            }
        }

        return RubyValue.OfArray(
            new List<RubyValue>
            {
                RubyValue.OfInteger((kopf[16] << 24) | (kopf[17] << 16)
                    | (kopf[18] << 8) | kopf[19]),
                RubyValue.OfInteger((kopf[20] << 24) | (kopf[21] << 16)
                    | (kopf[22] << 8) | kopf[23]),
            });
    }

    /// <summary>The images a game asked for and this runtime could not find.</summary>
    public IReadOnlyList<string> FehlendeBilder => _fehlende;

    private readonly List<string> _fehlende = new();
}

/// <summary>
/// The end of a measured run: the host presented the frames it was asked for.
/// </summary>
/// <remarks>
/// <strong>And this is not an error in the game.</strong> A game's loop runs
/// until the player leaves the scene, so a host that measures has to end it --
/// and this exception is how the run says "enough frames" instead of waiting for
/// a scene change that a measurement will never produce.
/// </remarks>
public sealed class RgssFrameBudgetErreicht : Exception
{
    public RgssFrameBudgetErreicht(int pFrames)
        : base("the frame budget was reached after " + pFrames + " frames")
    {
        Frames = pFrames;
    }

    /// <summary>The frames that were presented.</summary>
    public int Frames { get; }

}
