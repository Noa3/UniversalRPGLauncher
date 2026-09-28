using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Presentation;

public sealed class ChoiceState
{
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
    public int SelectedIndex { get; private set; } = -1;

    public bool Select(int pIndex)
    {
        if (pIndex < 0 || pIndex >= Options.Count) return false;
        SelectedIndex = pIndex;
        return true;
    }
}

/// <summary>
/// One picture, from EasyRPG's <c>Game_Pictures::ShowParams</c>.
/// </summary>
/// <remarks>
/// <para>
/// This was four fields and a validation check, and <c>ShowPicture</c> had no
/// caller: the state existed, the bounds existed, and no event command reached
/// either. <strong>A validated island is not a feature.</strong>
/// </para>
/// <para>
/// The fields that came with it are the ones the reader can keep. Alpha,
/// saturation, the effect mode and the top and bottom transparency are
/// <em>stored, not claimed</em>: a colour that this reader does not draw is a
/// number it read, and saying a picture is 40 percent transparent without a
/// renderer to honour it would be a claim nothing backs.
/// </para>
/// </remarks>
public sealed class PictureState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    /// <summary>Whether the picture moves with the map, from <c>parameters[4]</c>.</summary>
    public bool FixedToMap { get; init; }

    /// <summary>
    /// The magnification in percent, from <c>parameters[5]</c>. The reference
    /// sets <c>magnify_height = magnify_width</c> — **one value, not two** —
    /// and a reader that read a height from the next parameter would be reading
    /// the top transparency.
    /// </summary>
    public int Magnify { get; init; }

    /// <summary>
    /// The top transparency in percent, from <c>parameters[6]</c>.
    /// </summary>
    /// <remarks>
    /// **A percentage and not a colour.** See
    /// <see cref="PresentationState.MaxTransparencyPercent"/>, and see the
    /// PicPointer branch, which reads a value above 10000 as a variable index
    /// rather than as a percentage at all.
    /// </remarks>
    public int TopTransparency { get; init; }

    /// <summary>
    /// The bottom transparency in percent, from <c>parameters[14]</c>.
    /// </summary>
    /// <remarks>
    /// **It is 0 when the command is short, and that is not the same as the
    /// top colour.** The reference has a corner case: 2k maps inside 2k3 before
    /// 1.10 carry no second chunk, and it copies the top colour into the
    /// bottom in exactly that case. A reader that always copied would apply that
    /// rule to every modern command, where the two chunks are independent.
    /// </remarks>
    public int BottomTransparency { get; init; }

    /// <summary>Whether the top colour is a transparency key, from <c>parameters[7]</c>.</summary>
    public bool UseTransparentColor { get; init; }

    public int Red { get; init; }
    public int Green { get; init; }
    public int Blue { get; init; }

    /// <summary>Saturation from <c>parameters[11]</c>, in percent.</summary>
    public int Saturation { get; init; }

    /// <summary>The effect mode from <c>parameters[12]</c>.</summary>
    /// <remarks>
    /// 0 none, 1 rotate, 2 flip horizontally, 3 flip vertically — the four the
    /// format defines, and the bound is <see cref="PresentationState.MaxEffectMode"/>.
    /// </remarks>
    public int EffectMode { get; init; }

    /// <summary>The effect power from <c>parameters[13]</c>, in percent.</summary>
    public int EffectPower { get; init; }
}

public sealed class PresentationState
{
    public const int MaxMessageCharacters = 4096;
    public const int MaxPictures = 100;
    public const int MaxChoices = 4;
    public const int MaxChoiceCharacters = 128;
    public const int MaxPictureNameCharacters = 256;
    public const int MaxPictureDimension = 8192;
    public const int MaxColorChannel = 255;
    public const int MaxEffectFrames = 600; // 10 seconds at 60 Hz, matching EventInterpreter.MaxWaitFrames
    public const int MaxShakeStrength = 8;
    public const int MaxShakeSpeed = 8;
    public const int MaxWeatherType = 2;
    /// <summary>
    /// The highest effect mode, from liblcf's enumeration: none, rotate, flip
    /// horizontally, flip vertically.
    /// </summary>
    public const int MaxEffectMode = 3;

    /// <summary>
    /// The highest magnification and saturation in percent, from the range the
    /// editor writes.
    /// </summary>
    public const int MaxMagnifyPercent = 2000;
    public const int MaxSaturationPercent = 200;

    /// <summary>
    /// The highest transparency, in percent.
    /// </summary>
    /// <remarks>
    /// <strong>Transparency is a percentage, not a colour.</strong> The
    /// reference clamps it with <c>std::min(top_trans, 100)</c>, and a first
    /// draft validated it as a colour channel against
    /// <see cref="MaxColorChannel"/> — so every real picture in a game, which
    /// carries 0, 50 or 100, was fine by accident and any game using the
    /// colour a draft had in mind would have been refused.
    /// </remarks>
    public const int MaxTransparencyPercent = 100;
    public const int MaxWeatherStrength = 2;

    public bool MessageVisible { get; private set; }
    public string MessageText { get; private set; } = "";
    public ChoiceState? ActiveChoice { get; private set; }
    public int? PendingInputVariableId { get; private set; }
    public int? InputValue { get; private set; }

    // Command 11610, Key Input Proc. It is a second, different prompt: the
    // number command asks for a number with the number pad, and this one asks
    // for whatever set of keys the game listed, and writes a code that is not
    // the key's own value.
    public int? PendingKeyInputVariableId { get; private set; }

    /// <summary>
    /// The keys the pending 11610 accepts, in the order the reference tests
    /// them, as <c>(value, name)</c> pairs.
    /// </summary>
    public IReadOnlyList<(int Value, string Key)> PendingKeyInputKeys
    {
        get; private set;
    } = [];

    /// <summary>
    /// The variable the elapsed tenths go into while a timed 11610 waits, or 0.
    /// </summary>
    public int PendingKeyInputTimeVariableId { get; private set; }

    /// <summary>
    /// Whether the pending 11610 reports the elapsed time, from
    /// <c>parameters[8]</c>.
    /// </summary>
    public bool PendingKeyInputIsTimed { get; private set; }

    /// <summary>
    /// Tenths a timed 11610 has waited, which the reference counts in frames
    /// and divides by six.
    /// </summary>
    public int PendingKeyInputTenths { get; private set; }
    public Dictionary<int, PictureState> Pictures { get; } = new();

    // Screen effects (liblcf FlashScreen 11040, ShakeScreen 11050, WeatherEffects 11070).
    public bool IsFlashActive { get; private set; }
    public int FlashRed { get; private set; }
    public int FlashGreen { get; private set; }
    public int FlashBlue { get; private set; }
    public int FlashAlpha { get; private set; }
    public int FlashFramesRemaining { get; private set; }

    public bool IsShakeActive { get; private set; }
    public int ShakeStrength { get; private set; }
    public int ShakeSpeed { get; private set; }
    public int ShakeFramesRemaining { get; private set; }

    public int WeatherType { get; private set; }
    public int WeatherStrength { get; private set; }

    public void Reset()
    {
        MessageVisible = false;
        MessageText = "";
        ActiveChoice = null;
        PendingInputVariableId = null;
        InputValue = null;
        PendingKeyInputVariableId = null;
        PendingKeyInputKeys = [];
        PendingKeyInputTimeVariableId = 0;
        PendingKeyInputIsTimed = false;
        PendingKeyInputTenths = 0;
        Pictures.Clear();
        IsFlashActive = false;
        FlashRed = FlashGreen = FlashBlue = FlashAlpha = 0;
        FlashFramesRemaining = 0;
        IsShakeActive = false;
        ShakeStrength = 0;
        ShakeSpeed = 0;
        ShakeFramesRemaining = 0;
        WeatherType = 0;
        WeatherStrength = 0;
    }

    /// <summary>Advances timed screen effects by whole simulation frames.</summary>
    public void Tick(int pFrames)
    {
        if (pFrames < 0) throw new ArgumentOutOfRangeException(nameof(pFrames));
        if (IsFlashActive)
        {
            FlashFramesRemaining -= pFrames;
            if (FlashFramesRemaining <= 0)
            {
                IsFlashActive = false;
                FlashFramesRemaining = 0;
            }
        }
        if (IsShakeActive)
        {
            ShakeFramesRemaining -= pFrames;
            if (ShakeFramesRemaining <= 0)
            {
                IsShakeActive = false;
                ShakeFramesRemaining = 0;
            }
        }
    }

    public bool FlashOnce(int pRed, int pGreen, int pBlue, int pAlpha, int pFrames)
    {
        if (!IsChannel(pRed) || !IsChannel(pGreen) || !IsChannel(pBlue) || !IsChannel(pAlpha)) return false;
        if (pFrames < 0 || pFrames > MaxEffectFrames) return false;
        FlashRed = pRed;
        FlashGreen = pGreen;
        FlashBlue = pBlue;
        FlashAlpha = pAlpha;
        FlashFramesRemaining = pFrames;
        IsFlashActive = pFrames > 0;
        return true;
    }

    public void FlashEnd()
    {
        IsFlashActive = false;
        FlashFramesRemaining = 0;
    }

    public bool ShakeOnce(int pStrength, int pSpeed, int pFrames)
    {
        if (pStrength < 0 || pStrength > MaxShakeStrength) return false;
        if (pSpeed < 0 || pSpeed > MaxShakeSpeed) return false;
        if (pFrames < 0 || pFrames > MaxEffectFrames) return false;
        ShakeStrength = pStrength;
        ShakeSpeed = pSpeed;
        ShakeFramesRemaining = pFrames;
        IsShakeActive = pFrames > 0;
        return true;
    }

    public void ShakeEnd()
    {
        IsShakeActive = false;
        ShakeFramesRemaining = 0;
    }

    public bool SetWeather(int pType, int pStrength)
    {
        if (pType < 0 || pType > MaxWeatherType) return false;
        if (pStrength < 0 || pStrength > MaxWeatherStrength) return false;
        WeatherType = pType;
        WeatherStrength = pStrength;
        return true;
    }

    private static bool IsChannel(int pValue) => pValue >= 0 && pValue <= MaxColorChannel;

    public bool BeginInput(int pVariableId)
    {
        if (pVariableId <= 0) return false;
        PendingInputVariableId = pVariableId;
        InputValue = null;
        return true;
    }

    public bool SetInputValue(int pValue)
    {
        if (PendingInputVariableId == null) return false;
        InputValue = pValue;
        return true;
    }

    /// <summary>
    /// Opens the 11610 prompt, from the request
    /// <see cref="Interpreter.Rm2kKeyInput.Request"/> decoded from its
    /// parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A second prompt, not a second mode of the first.</strong> 10150
    /// asks for a number and stores it; 11610 asks for a set of keys and
    /// stores a <em>code</em> — a digit is 11 to 20, an operator 21 to 25, the
    /// confirm key is 5. Reusing the number prompt would write a key code into
    /// a variable a game expected to hold a digit, and nothing in the file says
    /// which of the two asked.
    /// </para>
    /// <para>
    /// <strong>Reopening the same request is refused.</strong> The reference
    /// resets the key state on every call, so a second 11610 on the same page
    /// would drop a keypress that arrived between the two. Holding on is what
    /// makes the prompt a prompt.
    /// </para>
    /// </remarks>
    public bool BeginKeyInput(Interpreter.Rm2kKeyInput.Request pRequest)
    {
        if (pRequest == null || pRequest.VariableId <= 0)
        {
            return false;
        }
        if (PendingKeyInputVariableId == pRequest.VariableId
            && PendingKeyInputKeys.Count > 0)
        {
            return false;
        }
        PendingKeyInputVariableId = pRequest.VariableId;
        PendingKeyInputKeys = pRequest.AllowedKeys;
        PendingKeyInputTimeVariableId = pRequest.TimeVariableId;
        PendingKeyInputIsTimed = pRequest.Timed;
        PendingKeyInputTenths = 0;
        return true;
    }

    /// <summary>
    /// The value one key press produces, or 0 when nothing this prompt allows
    /// was pressed.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is a real answer and also "nothing pressed".</strong> The
    /// reference returns 0 from <c>CheckInput</c> when no key matched, and the
    /// caller treats that as "still waiting". This returns false in that case
    /// so the page holds, and the two are not the same thing: a key that is not
    /// on the list must not end the prompt.
    /// </remarks>
    public bool TryConsumeKeyInput(IReadOnlyList<string> pPressed, out int pValue)
    {
        pValue = 0;
        if (PendingKeyInputVariableId == null)
        {
            return false;
        }
        pValue = Interpreter.Rm2kKeyInput.ValueFor(
            new Interpreter.Rm2kKeyInput.Request
            {
                VariableId = PendingKeyInputVariableId.Value,
                AllowedKeys = PendingKeyInputKeys,
            },
            pPressed);
        if (pValue == Interpreter.Rm2kKeyInput.ValueNone)
        {
            pValue = 0;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Closes the 11610 prompt and reports what it was waiting for.
    /// </summary>
    public bool TryConsumeKeyInput(
        out int pVariableId, out int pValue, out int pTimeVariableId, out int pTenths)
    {
        pVariableId = 0;
        pValue = 0;
        pTimeVariableId = 0;
        pTenths = 0;
        if (PendingKeyInputVariableId is not int variableId)
        {
            return false;
        }
        pVariableId = variableId;
        pValue = InputValue ?? 0;
        pTimeVariableId = PendingKeyInputIsTimed ? PendingKeyInputTimeVariableId : 0;
        pTenths = PendingKeyInputTenths;
        PendingKeyInputVariableId = null;
        PendingKeyInputKeys = [];
        PendingKeyInputTimeVariableId = 0;
        PendingKeyInputIsTimed = false;
        PendingKeyInputTenths = 0;
        return true;
    }

    /// <summary>
    /// Records a key press against the open 11610 prompt.
    /// </summary>
    public bool SetKeyInputValue(int pValue)
    {
        if (PendingKeyInputVariableId == null)
        {
            return false;
        }
        InputValue = pValue;
        return true;
    }

    public bool TryConsumeInput(out int pVariableId, out int pValue)
    {
        if (PendingInputVariableId is not int variableId || InputValue is not int value)
        {
            pVariableId = 0;
            pValue = 0;
            return false;
        }
        pVariableId = variableId;
        pValue = value;
        PendingInputVariableId = null;
        InputValue = null;
        return true;
    }

    public bool ShowMessage(string pText)
    {
        if (pText == null || pText.Length > MaxMessageCharacters)
        {
            return false;
        }
        MessageText = pText;
        MessageVisible = true;
        return true;
    }

    public void DismissMessage()
    {
        MessageText = "";
        MessageVisible = false;
        ActiveChoice = null;
    }

    public bool ShowChoices(IEnumerable<string> pOptions)
    {
        var options = new List<string>();
        foreach (var option in pOptions)
        {
            if (options.Count >= MaxChoices || string.IsNullOrWhiteSpace(option) || option.Length > MaxChoiceCharacters)
            {
                return false;
            }
            options.Add(option);
        }
        if (options.Count == 0) return false;
        ActiveChoice = new ChoiceState { Options = options };
        return true;
    }

    public bool SelectChoice(int pIndex) => ActiveChoice?.Select(pIndex) == true;

    public void ClearChoice()
    {
        ActiveChoice = null;
    }

    /// <summary>
    /// Shows a picture from command 11110, with the whole
    /// <see cref="PictureState"/> and every bound the reference applies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The old signature took six scalars and threw the other eight
    /// parameters away. Every one of them is a number the reference reads, and
    /// dropping them is how a picture arrives with the wrong transparency
    /// colour and nobody can say why.
    /// </para>
    /// <para>
    /// <strong>Width and height come from the magnification, not from
    /// parameters.</strong> The reference sets
    /// <c>magnify_height = magnify_width</c> — one value, square — and this
    /// takes the picture's natural size and scales it by the same factor in
    /// both directions. A reader that read a height from
    /// <c>parameters[6]</c> would be reading the top transparency.
    /// </para>
    /// </remarks>
    /// <param name="pId">
    /// The picture number, from <c>parameters[0]</c>. Bound to
    /// <see cref="MaxPictures"/>.
    /// </param>
    /// <param name="pName">The file name, from the command's string field.</param>
    /// <param name="pX">Position or the variable holding it, from <c>parameters[2]</c>.</param>
    /// <param name="pY">Position or the variable holding it, from <c>parameters[3]</c>.</param>
    /// <param name="pFixedToMap">
    /// Whether the picture moves with the map, from <c>parameters[4]</c>.
    /// </param>
    /// <param name="pMagnify">Magnification in percent, from <c>parameters[5]</c>.</param>
    /// <param name="pTopTransparency">The top colour, from <c>parameters[6]</c>.</param>
    /// <param name="pUseTransparentColor">Whether it is a key, from <c>parameters[7]</c>.</param>
    /// <param name="pRed">From <c>parameters[8]</c>.</param>
    /// <param name="pGreen">From <c>parameters[9]</c>.</param>
    /// <param name="pBlue">From <c>parameters[10]</c>.</param>
    /// <param name="pSaturation">From <c>parameters[11]</c>, in percent.</param>
    /// <param name="pEffectMode">From <c>parameters[12]</c>.</param>
    /// <param name="pEffectPower">From <c>parameters[13]</c>, in percent.</param>
    /// <param name="pBottomTransparency">
    /// The second colour, from <c>parameters[14]</c>, or null when the command
    /// does not carry it. Null is not zero: the reference copies the top colour
    /// into the bottom for one specific old-map case and not for any other, and
    /// a reader that always copied would apply that to every modern command.
    /// </param>
    /// <param name="pNaturalWidth">
    /// The file's own width, which the magnification scales. Zero means the
    /// caller has not measured the file, and the picture keeps its own size.
    /// </param>
    /// <param name="pNaturalHeight">The file's own height.</param>
    public bool ShowPicture(
        int pId, string pName, int pX, int pY,
        bool pFixedToMap, int pMagnify,
        int pTopTransparency, bool pUseTransparentColor,
        int pRed, int pGreen, int pBlue,
        int pSaturation, int pEffectMode, int pEffectPower,
        int? pBottomTransparency = null,
        int pNaturalWidth = 0, int pNaturalHeight = 0)
    {
        // Every bound here is the reference's own clamp, not one this
        // repository chose: magnify 0..2000, transparency 0..100 percent,
        // saturation 0..200, effect power 0..100.
        if (pId <= 0 || pId > MaxPictures || string.IsNullOrWhiteSpace(pName) ||
            pName.Length > MaxPictureNameCharacters ||
            pMagnify < 0 || pMagnify > MaxMagnifyPercent ||
            pTopTransparency < 0 || pTopTransparency > MaxTransparencyPercent ||
            pBottomTransparency is int bottom
                && (bottom < 0 || bottom > MaxTransparencyPercent) ||
            !IsChannel(pRed) || !IsChannel(pGreen) || !IsChannel(pBlue) ||
            pSaturation < 0 || pSaturation > MaxSaturationPercent ||
            pEffectMode < 0 || pEffectMode > MaxEffectMode ||
            pEffectPower < 0 || pEffectPower > 100)
        {
            return false;
        }
        // The reference clamps the magnification to 0..2000 and allows 0, so
        // this does too. **A first draft refused 0 as "no size"**, and a game
        // that deliberately shows a picture at zero magnification would have
        // had its command refused over a value the engine accepts.
        // A magnification of 0 is accepted and produces no area, because the
        // reference's own clamp is 0..2000 and 0 is inside it.
        var magnify = pMagnify;
        var breite = pNaturalWidth > 0
            ? Math.Clamp(pNaturalWidth * magnify / 100, 1, MaxPictureDimension)
            : pNaturalWidth;
        var hoehe = pNaturalHeight > 0
            ? Math.Clamp(pNaturalHeight * magnify / 100, 1, MaxPictureDimension)
            : pNaturalHeight;
        Pictures[pId] = new PictureState
        {
            Id = pId,
            Name = pName,
            X = pX,
            Y = pY,
            Width = breite,
            Height = hoehe,
            FixedToMap = pFixedToMap,
            Magnify = magnify,
            TopTransparency = pTopTransparency,
            BottomTransparency = pBottomTransparency ?? 0,
            UseTransparentColor = pUseTransparentColor,
            Red = pRed,
            Green = pGreen,
            Blue = pBlue,
            Saturation = pSaturation,
            EffectMode = pEffectMode,
            EffectPower = pEffectPower,
        };
        return true;
    }

    /// <summary>
    /// Erases a picture from command 11130, and says whether there was one.
    /// </summary>
    /// <remarks>
    /// The old version returned false for an id that had no picture, which a
    /// reader could not tell from a refusal. <strong>Erasing a picture twice is
    /// a game that runs twice, and it is not an error</strong> — the caller
    /// gets the difference.
    /// </remarks>
    public bool ErasePicture(int pId, out bool pHadPicture)
    {
        pHadPicture = pId > 0 && Pictures.ContainsKey(pId);
        return pId > 0 && Pictures.Remove(pId);
    }
}

public sealed class PresentationResult
{
    private PresentationResult(bool pSuccess, string pError)
    {
        Success = pSuccess;
        Error = pError;
    }

    public bool Success { get; }
    public string Error { get; }
    public static PresentationResult Succeeded() => new(true, "");
    public static PresentationResult Failed(string pError) => new(false, pError);
}

public sealed class PresentationAdapter
{
    private readonly PresentationState _state;

    public PresentationAdapter(PresentationState pState)
    {
        _state = pState ?? throw new ArgumentNullException(nameof(pState));
    }

    public PresentationResult ShowMessage(string pText) =>
        _state.ShowMessage(pText) ? PresentationResult.Succeeded() : PresentationResult.Failed("Message exceeds presentation bounds.");

    /// <summary>
    /// The adapter's picture call, with the natural size and every default the
    /// format allows.
    /// </summary>
    /// <remarks>
    /// The interpreter does not go through here — it calls the state directly,
    /// because it has the parameters and the version gate — but a caller that
    /// only knows a name and a position still needs a way in. The defaults are
    /// the format's own: no magnification, no transparency key, full colour, no
    /// effect.
    /// </remarks>
    public PresentationResult ShowPicture(
        int pId, string pName, int pX, int pY,
        int pNaturalWidth, int pNaturalHeight) =>
        _state.ShowPicture(
            pId, pName, pX, pY,
            pFixedToMap: false,
            pMagnify: 100,
            pTopTransparency: 0,
            pUseTransparentColor: false,
            pRed: 255, pGreen: 255, pBlue: 255,
            pSaturation: 100, pEffectMode: 0, pEffectPower: 100,
            pBottomTransparency: null,
            pNaturalWidth: pNaturalWidth,
            pNaturalHeight: pNaturalHeight)
            ? PresentationResult.Succeeded()
            : PresentationResult.Failed("Picture data exceeds presentation bounds.");

    /// <summary>
    /// The adapter's erase, which reports whether there was a picture.
    /// </summary>
    public PresentationResult ErasePicture(int pId) =>
        _state.ErasePicture(pId, out _)
            ? PresentationResult.Succeeded()
            : PresentationResult.Failed("Picture ID is out of bounds.");
}

public sealed class PresentationStatePlaceholder { }
