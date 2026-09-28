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

public sealed class PictureState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
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

    public bool ShowPicture(int pId, string pName, int pX, int pY, int pWidth, int pHeight)
    {
        if (pId <= 0 || pId > MaxPictures || string.IsNullOrWhiteSpace(pName) ||
            pName.Length > MaxPictureNameCharacters || pWidth <= 0 || pWidth > MaxPictureDimension ||
            pHeight <= 0 || pHeight > MaxPictureDimension)
        {
            return false;
        }
        Pictures[pId] = new PictureState { Id = pId, Name = pName, X = pX, Y = pY, Width = pWidth, Height = pHeight };
        return true;
    }

    public bool ErasePicture(int pId)
    {
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

    public PresentationResult ShowPicture(int pId, string pName, int pX, int pY, int pWidth, int pHeight) =>
        _state.ShowPicture(pId, pName, pX, pY, pWidth, pHeight)
            ? PresentationResult.Succeeded()
            : PresentationResult.Failed("Picture data exceeds presentation bounds.");

    public PresentationResult ErasePicture(int pId) =>
        _state.ErasePicture(pId) ? PresentationResult.Succeeded() : PresentationResult.Failed("Picture ID is not present.");
}

public sealed class PresentationStatePlaceholder { }
