using System;
using System.Collections.Generic;

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
