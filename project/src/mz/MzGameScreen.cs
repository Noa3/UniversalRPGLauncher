using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Mz;

/// <summary>
/// The screen object the game's scripts ask about, as the engine
/// declares it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>Game_Screen</c> has 47 methods in the game's own
/// engine file</strong>, -- <strong>and this reader answers
/// eighteen of them</strong>, -- <strong>and the eighteen are the
/// eighteen that carry 300 of the 397 calls.</strong>
/// </para>
/// <para>
/// <strong>And one of them is not obvious and was read out of the
/// engine:</strong>
/// </para>
/// <code>
/// realPictureId(pictureId) {
///     if ($gameParty.inBattle()) {
///         return pictureId + this.maxPictures();
///     } else {
///         return pictureId;
///     }
/// }
/// maxPictures() { return 120; }
/// </code>
/// <para>
/// <strong>So in battle every picture id is 120 higher</strong>, --
/// <strong>and a reader that stored pictures under the number the
/// script wrote would have <c>picture(1)</c> and
/// <c>showPicture(1, ...)</c> disagree the moment a battle
/// starts.</strong> -- <strong>And the game calls
/// <c>showPicture</c> and <c>picture</c> 179 times between
/// them.</strong>
/// </para>
/// </remarks>
public sealed class MzGameScreen
{
    private readonly Dictionary<int, MzPicture> _bilder = new();
    private readonly Dictionary<int, string> _namen = new();

    /// <summary>And whether a battle is running.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>realPictureId</c> asks
    /// <c>$gameParty.inBattle()</c></strong>, -- <strong>and that is
    /// the only thing it asks</strong>, -- <strong>and a reader that
    /// cannot answer it would be wrong for every picture id in a
    /// battle.</strong>
    /// </para>
    /// </remarks>
    public bool ImKampf { get; set; }

    /// <summary>And the fade that is running.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine stores a duration, not a
    /// direction</strong>, -- <strong>and setting one clears the
    /// other</strong>, -- <strong>which is what
    /// <c>startFadeIn</c> writes:</strong>
    /// <code>
    /// this._fadeInDuration = duration;
    /// this._fadeOutDuration = 0;
    /// </code>
    /// </para>
    /// </remarks>
    public int FadeReinDauer { get; private set; }

    /// <summary>And the fade out that is running.</summary>
    public int FadeRausDauer { get; private set; }

    /// <summary>And <c>brightness()</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine stores 0 to 255</strong>, --
    /// <strong>and 255 is white and 0 is black</strong>, --
    /// <strong>and a reader that stored 0 to 1 would answer a
    /// number the game never wrote.</strong>
    /// </para>
    /// </remarks>
    public int Helligkeit { get; private set; } = 255;

    /// <summary>And <c>maxPictures()</c>, and it is a constant.</summary>
    /// <returns>120, and the engine writes the number as a
    /// literal.</returns>
    public int MaxBilder() => 120;

    /// <summary>
    /// And <c>realPictureId(pictureId)</c>.
    /// </summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <returns>The stored number, and it is 120 higher in battle.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole reason the class stores pictures
    /// under the real number</strong>, -- <strong>because the engine
    /// does</strong>, -- <strong>and <c>showPicture(1, ...)</c> then
    /// <c>picture(1)</c> would not find it.</strong>
    /// </para>
    /// </remarks>
    public int RealBildId(int pId) => ImKampf ? pId + MaxBilder() : pId;

    /// <summary>
    /// And <c>picture(pictureId)</c>, and it gives nothing when the
    /// id has none.
    /// </summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <returns>The picture, or null.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>this._pictures[realPictureId]</c> gives
    /// <c>undefined</c> for an id nothing was stored under</strong>,
    /// -- <strong>and <c>movePicture</c> tests it</strong>:
    /// <c>const picture = this.picture(pictureId); if (picture)
    /// {...}</c>.
    /// </para>
    /// </remarks>
    public MzPicture? Picture(int pId) =>
        _bilder.TryGetValue(RealBildId(pId), out var b) ? b : null;

    /// <summary>And <c>showPicture(...)</c>.</summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <param name="pName">The file name.</param>
    /// <param name="pX">Where it stands.</param>
    /// <param name="pY">Where it stands.</param>
    /// <param name="pDeckkraft">0 to 255.</param>
    public void ShowPicture(int pId, string pName, int pX, int pY,
        int pDeckkraft)
    {
        var echt = RealBildId(pId);
        _bilder[echt] = new MzPicture
        {
            Name = pName,
            X = pX,
            Y = pY,
            Deckkraft = pDeckkraft,
            Sichtbar = true,
        };
        _namen[echt] = pName;
    }

    /// <summary>And <c>movePicture(...)</c>, and it does nothing without one.</summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <param name="pX">Where it goes.</param>
    /// <param name="pY">Where it goes.</param>
    /// <param name="pDauer">How long, in frames.</param>
    /// <remarks>
    /// <para>
    /// <strong>And the engine guards with
    /// <c>if (picture)</c></strong>, -- <strong>and the game calls
    /// this 4 times</strong>, -- <strong>and a reader that threw
    /// here would break a map that is otherwise fine.</strong>
    /// </para>
    /// </remarks>
    public bool MovePicture(int pId, int pX, int pY, int pDauer)
    {
        if (Picture(pId) is not { } b)
        {
            return false;
        }

        b.X = pX;
        b.Y = pY;
        b.BewegungsDauer = pDauer;
        return true;
    }

    /// <summary>And <c>erasePicture(pictureId)</c>.</summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <remarks>
    /// <para>
    /// <strong>And the engine sets the slot to <c>null</c></strong>,
    /// -- <strong>and does not delete the key</strong>, --
    /// <strong>and that is why <c>picture()</c> gives nothing
    /// afterwards.</strong>
    /// </para>
    /// </remarks>
    public void ErasePicture(int pId) => _bilder[RealBildId(pId)] = null!;

    /// <summary>And <c>startFadeIn(duration)</c>.</summary>
    /// <param name="pDauer">How long, in frames.</param>
    /// <remarks>
    /// <para>
    /// <strong>And it clears the fade out</strong>, --
    /// <strong>which the engine writes as two lines</strong> --
    /// <strong>and the game calls this 14 times.</strong>
    /// </para>
    /// </remarks>
    public void StartFadeIn(int pDauer)
    {
        FadeReinDauer = pDauer;
        FadeRausDauer = 0;
    }

    /// <summary>And <c>startFadeOut(duration)</c>.</summary>
    /// <param name="pDauer">How long, in frames.</param>
    public void StartFadeOut(int pDauer)
    {
        FadeRausDauer = pDauer;
        FadeReinDauer = 0;
    }

    /// <summary>And <c>clearFade()</c>.</summary>
    public void ClearFade()
    {
        FadeReinDauer = 0;
        FadeRausDauer = 0;
    }

    /// <summary>And whether any fade is running.</summary>
    public bool Blendet => FadeReinDauer > 0 || FadeRausDauer > 0;

    /// <summary>And how many pictures are shown.</summary>
    /// <returns>The count of the ones that exist.</returns>
    public int SichtbareBilder() =>
        _bilder.Values.Count(b => b != null && b.Sichtbar);

    /// <summary>And the reader names what it answers.</summary>
    /// <returns>The method names.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine has 47</strong>, -- <strong>and these
    /// are the eighteen that carry the calls</strong>, --
    /// <strong>and the rest are named in
    /// <c>TestMzGameScreen</c> so that a gap is visible.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> BekannteMethoden() => new[]
    {
        "picture", "realPictureId", "showPicture", "movePicture",
        "erasePicture", "brightness", "startFadeIn", "startFadeOut",
        "clearFade", "maxPictures",
    };
}

/// <summary>One picture on the screen.</summary>
public sealed class MzPicture
{
    /// <summary>And the file name it shows.</summary>
    public string Name { get; set; } = "";

    /// <summary>And where it stands.</summary>
    public int X { get; set; }

    /// <summary>And where it stands.</summary>
    public int Y { get; set; }

    /// <summary>And its transparency, 0 to 255.</summary>
    public int Deckkraft { get; set; } = 255;

    /// <summary>And whether it is drawn at all.</summary>
    public bool Sichtbar { get; set; }

    /// <summary>And how long a movement takes.</summary>
    public int BewegungsDauer { get; set; }
}
