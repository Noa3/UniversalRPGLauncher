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
    // Screen tint, from command 11030 Tint Screen and EasyRPG's
    // `Game_Screen::TintScreen`. **The four numbers are the screen's own
    // channels, not a colour the reader picks** — the reference passes r, g, b
    // and a saturation straight to the screen and never reads them back, so
    // what is stored here is what was asked for.
    public bool IsTintActive { get; private set; }

    public int TintRed { get; private set; }
    public int TintGreen { get; private set; }
    public int TintBlue { get; private set; }

    /// <summary>
    /// The saturation in percent, from <c>parameters[3]</c>.
    /// </summary>
    /// <remarks>
    /// <strong>0 to 100, and 100 is the untinted screen.</strong> A reader that
    /// treated 0 as "no tint" would have tinted the screen to grey at the one
    /// value that means "leave it alone" — and that is the value a game writes
    /// when it wants no tint.
    /// </remarks>
    public int TintSaturation { get; private set; }

    /// <summary>
    /// The tint's duration in frames, from <c>tenths * DEFAULT_FPS / 10</c>.
    /// </summary>
    /// <remarks>
    /// The reference converts tenths to frames itself rather than storing
    /// tenths, and the conversion matters: at 60 frames per second tenths times
    /// six is exact, but <c>DEFAULT_FPS</c> is a named constant and a reader
    /// that hardcoded 6 would disagree with any game that declared another
    /// rate.
    /// </remarks>
    public int TintFramesRemaining { get; private set; }

    /// <summary>
    /// The transition a command asked for, or <c>TransitionNone</c>.
    /// </summary>
    /// <remarks>
    /// <strong>This is data and not an animation.</strong> A transition is a
    /// sequence the renderer plays; nothing here plays it, and a value in this
    /// field is a request that a renderer may honour or may not.
    /// </remarks>
    public Rm2kTransition PendingTransition { get; private set; }

    /// <summary>
    /// Whether the last transition was a show or an erase, from
    /// <c>11020</c> and <c>11010</c>.
    /// </summary>
    public Rm2kTransitionDirection PendingTransitionDirection
    {
        get; private set;
    }

    /// <summary>
    /// Starts a screen tint, from <c>CommandTintScreen</c>.
    /// </summary>
    /// <returns>False when a value was out of range, and nothing changed.</returns>
    public bool TintScreen(
        int pRed, int pGreen, int pBlue, int pSaturation, int pFrames)
    {
        // **Saturation is bounded separately from the channels**, because it is
        // a percentage and not a colour: 0 to 100, and 100 is untinted.
        if (!IsChannel(pRed) || !IsChannel(pGreen) || !IsChannel(pBlue)
            || pSaturation < 0 || pSaturation > 100
            || pFrames < 0 || pFrames > MaxEffectFrames)
        {
            return false;
        }
        IsTintActive = true;
        TintRed = pRed;
        TintGreen = pGreen;
        TintBlue = pBlue;
        TintSaturation = pSaturation;
        TintFramesRemaining = pFrames;
        return true;
    }

    /// <summary>
    /// Asks for a screen transition, from <c>11010</c> and <c>11020</c>.
    /// </summary>
    /// <remarks>
    /// The kind comes straight from the command's one parameter, and the
    /// reference's <c>switch</c> has no default arm — **a value it does not
    /// know falls through to <c>TransitionNone</c>**, which is not an error and
    /// not a no-op either: the screen changes with no transition at all.
    /// </remarks>
    public bool RequestTransition(
        Rm2kTransitionDirection pDirection, Rm2kTransition pTransition)
    {
        if (!Enum.IsDefined(typeof(Rm2kTransition), pTransition))
        {
            return false;
        }
        PendingTransition = pTransition;
        PendingTransitionDirection = pDirection;
        return true;
    }

    /// <summary>
    /// A picture's movement, from command 11120, and the frames it has left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The movement is a state and not a position.</strong> A reader that
    /// set the target straight away would have the picture arrive the instant
    /// the command ran, and a game that slides a title across the screen would
    /// show it at its destination with nothing in between. The reference holds
    /// the picture, the target and the remaining frames, and the render reads
    /// where the picture is <em>now</em>.
    /// </para>
    /// <para>
    /// <strong>Moving a picture that is not there is refused</strong> and not
    /// silently created: a game that moves an id it never showed has a file
    /// that does not mean what it says, and inventing a picture there would put
    /// an image on the screen that no command asked for.
    /// </para>
    /// </remarks>
    public bool IsPictureMoving(int pId) =>
        _movingPictures.ContainsKey(pId);

    /// <summary>Where a moving picture will end up.</summary>
    public int PictureMoveTargetX(int pId) =>
        _movingPictures.TryGetValue(pId, out var move) ? move.TargetX : 0;

    /// <summary>Where a moving picture will end up, vertically.</summary>
    public int PictureMoveTargetY(int pId) =>
        _movingPictures.TryGetValue(pId, out var move) ? move.TargetY : 0;

    /// <summary>How many frames a picture's movement has left.</summary>
    public int PictureMoveFramesLeft(int pId) =>
        _movingPictures.TryGetValue(pId, out var move) ? move.FramesLeft : 0;

    /// <summary>How far a picture has come along its movement.</summary>
    public int PictureMoveFrame(int pId) =>
        _movingPictures.TryGetValue(pId, out var move)
            ? move.TotalFrames - move.FramesLeft
            : 0;

    private readonly Dictionary<int, PictureMove> _movingPictures = new();

    private sealed class PictureMove
    {
        public int StartX { get; init; }
        public int StartY { get; init; }
        public int TargetX { get; init; }
        public int TargetY { get; init; }
        public int TotalFrames { get; init; }
        public int FramesLeft { get; set; }
    }

    /// <summary>
    /// Moves a picture to a position over a number of frames, from command
    /// 11120.
    /// </summary>
    /// <param name="pId">The picture, which has to exist.</param>
    /// <param name="pTargetX">Where it ends up.</param>
    /// <param name="pTargetY">Where it ends up, vertically.</param>
    /// <param name="pFrames">
    /// How long the movement takes. **Zero is a jump and not a refusal**: the
    /// reference sets the position and calls it done, and a game that wrote a
    /// zero because the editor left the field empty means "now".
    /// </param>
    /// <returns>True when the picture exists and the movement was recorded.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Zero frames is a placement and not an error.</strong> An editor
    /// field the author never touched reads as zero, and refusing it would stop
    /// the event — so a game whose title card is positioned by a zero-frame
    /// move would lose its title card instead of having it appear.
    /// </para>
    /// <para>
    /// <strong>The picture has to exist.</strong> Moving an id that was never
    /// shown is a file that does not mean what it says, and creating one there
    /// would put an image on the screen that no command asked for.
    /// </para>
    /// </remarks>
    public bool MovePicture(int pId, int pTargetX, int pTargetY, int pFrames)
    {
        if (!Pictures.ContainsKey(pId))
        {
            return false;
        }
        var picture = Pictures[pId];
        if (pFrames <= 0)
        {
            // **The place, now.** The reference writes the position and returns,
            // so a zero-frame move is a move that has already happened and not
            // a move that never starts.
            Pictures[pId] = CloneAt(picture, pTargetX, pTargetY);
            _movingPictures.Remove(pId);
            return true;
        }
        _movingPictures[pId] = new PictureMove
        {
            StartX = picture.X,
            StartY = picture.Y,
            TargetX = pTargetX,
            TargetY = pTargetY,
            TotalFrames = pFrames,
            FramesLeft = pFrames,
        };
        return true;
    }

    /// <summary>
    /// A copy of a picture at another position, keeping every other field.
    /// </summary>
    /// <remarks>
    /// <strong>A copy and not a field write.</strong> A picture's fields are
    /// <c>init</c>, so moving one means building a new value — and building it
    /// by hand would mean listing fifteen fields and forgetting the one that
    /// changed, which is the position itself. A reader that forgot the size would
    /// have a moving picture that also changed shape.
    /// </remarks>
    private static PictureState CloneAt(PictureState pPicture, int pX, int pY)
    {
        return new PictureState
        {
            Id = pPicture.Id,
            Name = pPicture.Name,
            X = pX,
            Y = pY,
            Width = pPicture.Width,
            Height = pPicture.Height,
            FixedToMap = pPicture.FixedToMap,
            Magnify = pPicture.Magnify,
            TopTransparency = pPicture.TopTransparency,
            UseTransparentColor = pPicture.UseTransparentColor,
            Red = pPicture.Red,
            Green = pPicture.Green,
            Blue = pPicture.Blue,
            Saturation = pPicture.Saturation,
            EffectMode = pPicture.EffectMode,
            EffectPower = pPicture.EffectPower,
            BottomTransparency = pPicture.BottomTransparency,
        };
    }

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

    /// <summary>
    /// Advances every moving picture by the frames that passed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The position is interpolated, and not stepped once at the
    /// start.</strong> The reference's picture move travels from where it is to
    /// where it goes over the frames it was given, and a reader that set the
    /// target at the first tick would have a picture that jumps and then sits
    /// there for the rest of the movement's time.
    /// </para>
    /// <para>
    /// <strong>Integer division and not rounding, and the last frame lands
    /// exactly on the target</strong> — because the step is computed from the
    /// frames already spent over the frames in total, and at the last frame
    /// that fraction is one. A reader that rounded would have a picture that
    /// stopped one pixel short and then jumped.
    /// </para>
    /// <para>
    /// <strong>A picture that is erased mid-movement stops being moved.</strong>
    /// The entry is dropped, so an erase during a slide does not leave a
    /// movement that walks towards a position for a picture that is not there.
    /// </para>
    /// </remarks>
    private void TickPictureMoves(int pFrames)
    {
        if (_movingPictures.Count == 0 || pFrames == 0)
        {
            return;
        }
        // **A copy of the keys, because a movement can end and would then
        // change the dictionary while it is being walked.** Every other
        // collection in this class is walked the same way for the same reason.
        var ids = new List<int>(_movingPictures.Keys);
        foreach (var id in ids)
        {
            var move = _movingPictures[id];
            move.FramesLeft -= pFrames;
            if (move.FramesLeft <= 0)
            {
                if (Pictures.ContainsKey(id))
                {
                    Pictures[id] = CloneAt(Pictures[id], move.TargetX, move.TargetY);
                }
                _movingPictures.Remove(id);
                continue;
            }
            if (!Pictures.ContainsKey(id))
            {
                _movingPictures.Remove(id);
                continue;
            }
            var spent = move.TotalFrames - move.FramesLeft;
            Pictures[id] = CloneAt(
                Pictures[id],
                move.StartX + (move.TargetX - move.StartX) * spent / move.TotalFrames,
                move.StartY + (move.TargetY - move.StartY) * spent / move.TotalFrames);
        }
    }

    public void Reset()
    {
        MessageVisible = false;
        MessageText = "";
        MessageTransparent = false;
        MessagePosition = MessagePositionBottom;
        MessagePositionFixed = true;
        MessageContinuesEvents = false;
        FaceName = "";
        FaceIndex = 0;
        FaceOnRight = false;
        FaceFlipped = false;
        ActiveChoice = null;
        PendingInputVariableId = null;
        InputValue = null;
        PendingKeyInputVariableId = null;
        PendingKeyInputKeys = [];
        PendingKeyInputTimeVariableId = 0;
        PendingKeyInputIsTimed = false;
        PendingKeyInputTenths = 0;
        Pictures.Clear();
        // **The movements go with the pictures.** A new game that kept a
        // movement would have the first tick walk a picture towards a position
        // for an image that no longer exists, and the entry would sit in the
        // dictionary for ever because nothing else removes it.
        _movingPictures.Clear();
        IsTintActive = false;
        TintRed = 0; TintGreen = 0; TintBlue = 0; TintSaturation = 0;
        TintFramesRemaining = 0;
        PendingTransition = Rm2kTransition.None;
        PendingTransitionDirection = Rm2kTransitionDirection.Show;
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
        // **The pictures move on the same tick as the screen effects**, and for
        // the same reason: a movement that ran on a different clock would end
        // at a different moment than the flash that was told to end with it,
        // and a game that moves a picture while the screen flashes would have
        // the picture still sliding after the flash is gone.
        TickPictureMoves(pFrames);
        if (IsFlashActive)
        {
            FlashFramesRemaining -= pFrames;
            if (FlashFramesRemaining <= 0)
            {
                IsFlashActive = false;
                FlashFramesRemaining = 0;
            }
        }
        if (IsTintActive)
        {
            // The tint runs on the same tick as the flash and the shake, so all
            // three end together when they were given the same duration. **A
            // tint that outlived its flash would be a screen that stayed
            // coloured after the game said it was over.**
            TintFramesRemaining -= pFrames;
            if (TintFramesRemaining <= 0)
            {
                IsTintActive = false;
                TintFramesRemaining = 0;
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

    // Message options, from 10120 Message Options. **Four independent flags and
    // not one "style"**, because the reference stores four fields and a reader
    // that collapsed them would make a transparent bottom message and a
    // top-positioned one the same request.

    /// <summary>
    /// Whether the message window is transparent, from
    /// <c>parameters[0] != 0</c>.
    /// </summary>
    public bool MessageTransparent { get; private set; }

    /// <summary>
    /// Where the window sits, from <c>parameters[1]</c>: 0 top, 1 middle,
    /// 2 bottom.
    /// </summary>
    /// <remarks>
    /// **Three positions and not two.** A reader that stored a boolean would put
    /// the middle where the top belongs, and every game that centres its
    /// dialogue would open its window at the top.
    /// </remarks>
    public int MessagePosition { get; private set; }

    /// <summary>
    /// Whether the window holds its position while the map scrolls, from
    /// <c>parameters[2] == 0</c> — **inverted, so a zero means fixed**.
    /// </summary>
    public bool MessagePositionFixed { get; private set; } = true;

    /// <summary>
    /// Whether the map's events keep running while the message is open, from
    /// <c>parameters[3] != 0</c>.
    /// </summary>
    public bool MessageContinuesEvents { get; private set; }

    // Face graphic, from 10130 Change Face Graphic. **A request, not a drawn
    // portrait** — nothing here loads a file, and a face that is "set" but has
    // no renderer behind it is what this reader can honestly say.

    /// <summary>The face file name, from the command's string field.</summary>
    public string FaceName { get; private set; } = "";

    /// <summary>
    /// Which of the four faces in the file, from <c>parameters[0]</c>.
    /// </summary>
    public int FaceIndex { get; private set; }

    /// <summary>
    /// Whether the face sits on the right instead of the left, from
    /// <c>parameters[1] != 0</c>.
    /// </summary>
    public bool FaceOnRight { get; private set; }

    /// <summary>Whether the face is mirrored, from <c>parameters[2] != 0</c>.</summary>
    public bool FaceFlipped { get; private set; }

    /// <summary>The highest face index the file's four slots allow.</summary>
    public const int MaxFaceIndex = 3;

    /// <summary>
    /// The longest a face file name may be, from
    /// <see cref="MaxPictureNameCharacters"/> and the same reasoning: no
    /// filesystem is touched, so the bound is about what a diagnostic carries.
    /// </summary>
    public const int MaxFaceNameCharacters = 256;

    /// <summary>
    /// The three message positions, from <c>parameters[1]</c>.
    /// </summary>
    public const int MessagePositionTop = 0;
    public const int MessagePositionMiddle = 1;
    public const int MessagePositionBottom = 2;

    /// <summary>
    /// Applies the four message options, from <c>10120</c>.
    /// </summary>
    /// <returns>
    /// False when a value was out of range, and nothing changed — the reference
    /// has no validation at all here, so a reader that stored a position of
    /// seven would put a window nowhere.
    /// </returns>
    public bool SetMessageOptions(
        bool pTransparent, int pPosition, bool pFixed, bool pContinuesEvents)
    {
        if (pPosition < MessagePositionTop || pPosition > MessagePositionBottom)
        {
            return false;
        }
        MessageTransparent = pTransparent;
        MessagePosition = pPosition;
        MessagePositionFixed = pFixed;
        MessageContinuesEvents = pContinuesEvents;
        return true;
    }

    /// <summary>
    /// Sets the face, from <c>10130</c>.
    /// </summary>
    /// <returns>False when the name was empty, too long or the index was out of range.</returns>
    public bool SetFace(
        string pName, int pIndex, bool pOnRight, bool pFlipped)
    {
        if (string.IsNullOrWhiteSpace(pName) || pName.Length > MaxFaceNameCharacters
            || pIndex < 0 || pIndex > MaxFaceIndex)
        {
            return false;
        }
        FaceName = pName;
        FaceIndex = pIndex;
        FaceOnRight = pOnRight;
        FaceFlipped = pFlipped;
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

    /// <summary>
    /// The adapter's move, for command 11120.
    /// </summary>
    /// <remarks>
    /// <strong>The failure names the reason</strong> — a picture that is not on
    /// the screen and a picture that cannot move are two different mistakes in
    /// a game's file, and one message for both would have a person looking in
    /// the wrong place.
    /// </remarks>
    public PresentationResult MovePicture(
        int pId, int pTargetX, int pTargetY, int pFrames) =>
        _state.MovePicture(pId, pTargetX, pTargetY, pFrames)
            ? PresentationResult.Succeeded()
            : PresentationResult.Failed(
                $"Picture {pId} is not on the screen, so it cannot be moved.");
}

public sealed class PresentationStatePlaceholder { }
