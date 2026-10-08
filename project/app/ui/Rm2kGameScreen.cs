using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.App.Ui;

/// <summary>
/// And the game fills the whole window, -- **because a run that lives in a
/// small corner of a settings panel is a run a player cannot play.**
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the frame is scaled to the window</strong>, keeping the
/// 320 by 240 pixel screen, -- <strong>and the title bar carries the F4
/// hint</strong>, -- **and F4 opens the pause menu with resume, options,
/// cheats, stop runtime and close program.**
/// </para>
/// <para>
/// <strong>And the view rectangle is computed from the game's own
/// numbers</strong>: <see cref="Rm2kMapCamera"/> turns the player tile into
/// the same scroll offset the Player stores, -- **and the arithmetic here is
/// a pure function so a test can check the letterbox without a
/// window.**
/// </para>
/// </remarks>
public partial class Rm2kGameScreen : Control
{
    /// <summary>The RM2000/2003 screen in pixels, from the map camera type.</summary>
    public const int ScreenWidth = Rm2kMapCamera.DefaultScreenWidth;

    /// <summary>The RM2000/2003 screen in pixels, from the map camera type.</summary>
    public const int ScreenHeight = Rm2kMapCamera.DefaultScreenHeight;

    private ImageTexture? _mapTexture;
    private string _mapSignature = "";
    private byte[] _viewPixels = new byte[ScreenWidth * ScreenHeight * 4];
    private int _viewWidth = ScreenWidth;
    private int _viewHeight = ScreenHeight;
    private int _frameCount;
    private int _mapPixelWidth;
    private int _mapPixelHeight;
    private bool _integerScale;

    private string _messageText = "";
    private bool _messageVisible;
    private readonly List<string> _choiceOptions = new();
    private int _choiceSelected = -1;
    private bool _inputPending;
    private int _inputValue;

    // **And a command window, drawn over the game's own frame.**
    // `Window_Command` and everything above it needs a canvas and a font,
    // which is the sandbox's missing half; what this draws is the list the
    // engine and its plugins left on the window, placed where the engine
    // places it and scaled with the frame.
    private bool _commandVisible;
    private int _commandX;
    private int _commandY;
    private int _commandWidth;
    private int _commandHeight;
    private int _commandRowHeight = 36;
    private int _commandIndex;
    private readonly List<string> _commandRows = new();

    private PanelContainer _messagePanel = null!;
    private Label _messageLabel = null!;
    private PanelContainer _choicePanel = null!;
    private VBoxContainer _choiceBox = null!;
    private PanelContainer _inputPanel = null!;
    private SpinBox _inputSpin = null!;
    private Rm2kBattleView _battleView = null!;
    private ColorRect _pauseShade = null!;
    private PanelContainer _pausePanel = null!;
    private VBoxContainer _pauseMain = null!;
    private VBoxContainer _pauseOptions = null!;
    private VBoxContainer _pauseCheats = null!;
    private Label _pauseTitle = null!;
    private OptionButton _languageChoice = null!;
    private CheckBox _integerScaleBox = null!;

    /// <summary>And the player asked to continue from a message.</summary>
    public event Action? ContinueRequested;

    /// <summary>And the player clicked one choice row, by index.</summary>
    public event Action<int>? ChoiceSelected;

    /// <summary>And the player submitted the input variable's value.</summary>
    public event Action<int>? InputSubmitted;

    /// <summary>And the player chose resume in the pause menu.</summary>
    public event Action? ResumeRequested;

    /// <summary>And the player asked to stop the running game.</summary>
    public event Action? StopRuntimeRequested;

    /// <summary>And the player asked to close the whole program.</summary>
    public event Action? CloseProgramRequested;

    /// <summary>And the player changed the interface language.</summary>
    public event Action<string>? LanguageRequested;

    /// <summary>And the player toggled integer scaling.</summary>
    public event Action<bool>? IntegerScaleRequested;

    /// <summary>And whether the pause menu is open.</summary>
    public bool IsPauseOpen { get; private set; }

    /// <summary>And whether integer scaling is on.</summary>
    public bool IntegerScale => _integerScale;

    /// <summary>Why the map image is missing, empty when the map is rendered.</summary>
    public string RenderDiagnostic { get; set; } = "";

    public Rm2kGameScreen()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        Visible = false;
        BuildChildren();
    }

    private void BuildChildren()
    {
        _battleView = new Rm2kBattleView();
        _battleView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _battleView.Visible = false;
        AddChild(_battleView);

        _messagePanel = new PanelContainer();
        _messagePanel.AnchorLeft = 0.0f;
        _messagePanel.AnchorRight = 1.0f;
        _messagePanel.AnchorTop = 1.0f;
        _messagePanel.AnchorBottom = 1.0f;
        _messagePanel.OffsetLeft = 24;
        _messagePanel.OffsetRight = -24;
        _messagePanel.OffsetTop = -108;
        _messagePanel.OffsetBottom = -18;
        var messageMargin = new MarginContainer();
        messageMargin.AddThemeConstantOverride("margin_left", 14);
        messageMargin.AddThemeConstantOverride("margin_right", 14);
        messageMargin.AddThemeConstantOverride("margin_top", 10);
        messageMargin.AddThemeConstantOverride("margin_bottom", 10);
        _messagePanel.AddChild(messageMargin);
        _messageLabel = new Label();
        _messageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _messageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        messageMargin.AddChild(_messageLabel);
        _messagePanel.Visible = false;
        _messagePanel.GuiInput += OnMessageInput;
        AddChild(_messagePanel);

        _choicePanel = new PanelContainer();
        _choicePanel.AnchorLeft = 0.5f;
        _choicePanel.AnchorRight = 0.5f;
        _choicePanel.AnchorTop = 0.35f;
        _choicePanel.AnchorBottom = 0.35f;
        _choicePanel.OffsetLeft = -220;
        _choicePanel.OffsetRight = 220;
        var choiceMargin = new MarginContainer();
        choiceMargin.AddThemeConstantOverride("margin_left", 14);
        choiceMargin.AddThemeConstantOverride("margin_right", 14);
        choiceMargin.AddThemeConstantOverride("margin_top", 10);
        choiceMargin.AddThemeConstantOverride("margin_bottom", 10);
        _choicePanel.AddChild(choiceMargin);
        _choiceBox = new VBoxContainer();
        choiceMargin.AddChild(_choiceBox);
        _choicePanel.Visible = false;
        AddChild(_choicePanel);

        _inputPanel = new PanelContainer();
        _inputPanel.AnchorLeft = 0.5f;
        _inputPanel.AnchorRight = 0.5f;
        _inputPanel.AnchorTop = 1.0f;
        _inputPanel.AnchorBottom = 1.0f;
        _inputPanel.OffsetLeft = -190;
        _inputPanel.OffsetRight = 190;
        _inputPanel.OffsetTop = -128;
        _inputPanel.OffsetBottom = -64;
        var inputMargin = new MarginContainer();
        inputMargin.AddThemeConstantOverride("margin_left", 12);
        inputMargin.AddThemeConstantOverride("margin_right", 12);
        inputMargin.AddThemeConstantOverride("margin_top", 8);
        inputMargin.AddThemeConstantOverride("margin_bottom", 8);
        _inputPanel.AddChild(inputMargin);
        var inputRow = new HBoxContainer();
        inputMargin.AddChild(inputRow);
        _inputSpin = new SpinBox();
        _inputSpin.MinValue = 0;
        _inputSpin.MaxValue = int.MaxValue;
        _inputSpin.Step = 1;
        _inputSpin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        inputRow.AddChild(_inputSpin);
        var inputButton = new Button();
        inputButton.Text = "OK";
        inputButton.Pressed += () => InputSubmitted?.Invoke((int)_inputSpin.Value);
        inputRow.AddChild(inputButton);
        _inputPanel.Visible = false;
        AddChild(_inputPanel);

        _pauseShade = new ColorRect();
        _pauseShade.Color = new Color(0, 0, 0, 0.55f);
        _pauseShade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _pauseShade.Visible = false;
        AddChild(_pauseShade);

        _pausePanel = new PanelContainer();
        _pausePanel.AnchorLeft = 0.5f;
        _pausePanel.AnchorRight = 0.5f;
        _pausePanel.AnchorTop = 0.5f;
        _pausePanel.AnchorBottom = 0.5f;
        _pausePanel.OffsetLeft = -230;
        _pausePanel.OffsetRight = 230;
        _pausePanel.GrowVertical = GrowDirection.Both;
        _pausePanel.Visible = false;
        var pauseMargin = new MarginContainer();
        pauseMargin.AddThemeConstantOverride("margin_left", 20);
        pauseMargin.AddThemeConstantOverride("margin_right", 20);
        pauseMargin.AddThemeConstantOverride("margin_top", 16);
        pauseMargin.AddThemeConstantOverride("margin_bottom", 16);
        _pausePanel.AddChild(pauseMargin);
        var pauseColumn = new VBoxContainer();
        pauseColumn.AddThemeConstantOverride("separation", 8);
        pauseMargin.AddChild(pauseColumn);
        _pauseTitle = new Label();
        _pauseTitle.Text = Tr("PAUSE_TITLE");
        _pauseTitle.HorizontalAlignment = HorizontalAlignment.Center;
        pauseColumn.AddChild(_pauseTitle);

        _pauseMain = new VBoxContainer();
        _pauseMain.AddThemeConstantOverride("separation", 8);
        pauseColumn.AddChild(_pauseMain);
        AddPauseButton(_pauseMain, Tr("PAUSE_RESUME"), () => { ClosePause(); ResumeRequested?.Invoke(); });
        AddPauseButton(_pauseMain, Tr("PAUSE_OPTIONS"), () => ShowPausePage(_pauseOptions));
        AddPauseButton(_pauseMain, Tr("PAUSE_CHEATS"), () => ShowPausePage(_pauseCheats));
        AddPauseButton(_pauseMain, Tr("PAUSE_STOP_RUNTIME"), () => { ClosePause(); StopRuntimeRequested?.Invoke(); });
        AddPauseButton(_pauseMain, Tr("PAUSE_CLOSE_PROGRAM"), () => { ClosePause(); CloseProgramRequested?.Invoke(); });

        _pauseOptions = new VBoxContainer();
        _pauseOptions.AddThemeConstantOverride("separation", 8);
        _pauseOptions.Visible = false;
        pauseColumn.AddChild(_pauseOptions);
        var languageRow = new HBoxContainer();
        var languageLabel = new Label();
        languageLabel.Text = Tr("OPTIONS_LANGUAGE");
        languageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        languageRow.AddChild(languageLabel);
        _languageChoice = new OptionButton();
        foreach (var (id, name) in new[]
        {
            ("auto", "System"), ("en", "English"), ("de", "Deutsch"), ("es", "Espanol"),
            ("fr", "Francais"), ("ja", "Japanese"), ("ko", "Korean"), ("zh_CN", "Chinese"),
        })
        {
            _languageChoice.AddItem(name);
            _languageChoice.SetItemMetadata(_languageChoice.ItemCount - 1, id);
        }
        _languageChoice.ItemSelected += index =>
            LanguageRequested?.Invoke(_languageChoice.GetItemMetadata((int)index).AsString());
        languageRow.AddChild(_languageChoice);
        _pauseOptions.AddChild(languageRow);
        _integerScaleBox = new CheckBox();
        _integerScaleBox.Text = Tr("OPTIONS_INTEGER_SCALE");
        _integerScaleBox.Toggled += pressed =>
        {
            _integerScale = pressed;
            IntegerScaleRequested?.Invoke(pressed);
            QueueRedraw();
        };
        _pauseOptions.AddChild(_integerScaleBox);
        AddPauseButton(_pauseOptions, Tr("PAUSE_BACK"), () => ShowPausePage(null));

        _pauseCheats = new VBoxContainer();
        _pauseCheats.AddThemeConstantOverride("separation", 8);
        _pauseCheats.Visible = false;
        pauseColumn.AddChild(_pauseCheats);
        var cheatInfo = new Label();
        cheatInfo.Text = Tr("CHEATS_EMPTY");
        cheatInfo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cheatInfo.CustomMinimumSize = new Vector2(380, 0);
        _pauseCheats.AddChild(cheatInfo);
        AddPauseButton(_pauseCheats, Tr("PAUSE_BACK"), () => ShowPausePage(null));

        AddChild(_pausePanel);
    }

    private void AddPauseButton(VBoxContainer pParent, string pText, Action pPressed)
    {
        var button = new Button();
        button.Text = pText;
        button.CustomMinimumSize = new Vector2(380, 40);
        button.Pressed += pPressed;
        // **Und  jede  Pause-Seite  ist  auch  per  Tastatur  bedienbar** --
        // **denn  der  Spieler  hat  im  Spiel  nur  die  Tastatur,  und
        //  eine  Menue,  das  nur  mit  der  Maus  geht,  ist  in  einem
        //  Spiel  kein  Menue.**
        pParent.AddChild(button);
    }

    /// <summary>
    /// And the pause menu moves its own selection with the arrow keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the key path the game's own mapper does not
    /// cover</strong>, -- <strong>because while the pause menu is open the
    /// launcher's <c>_UnhandledInput</c> returns early and marks every key as
    /// handled, so without this the arrow keys would move the hero behind a
    /// frozen frame.</strong>
    /// </para>
    /// </remarks>
    public bool HandlePauseKey(InputEventKey pKey)
    {
        if (!IsPauseOpen || !pKey.Pressed || pKey.Echo)
        {
            return false;
        }
        var shift = pKey.ShiftPressed;
        if (pKey.Keycode == Key.Down || (pKey.Keycode == Key.Tab && !shift))
        {
            MovePauseFocus(1);
            return true;
        }
        if (pKey.Keycode == Key.Up || (pKey.Keycode == Key.Tab && shift))
        {
            MovePauseFocus(-1);
            return true;
        }
        return false;
    }

    private void MovePauseFocus(int pDirection)
    {
        var page = CurrentPausePage();
        if (page == null) return;
        var buttons = page.GetChildren().OfType<Button>().ToList();
        if (buttons.Count == 0) return;
        var current = buttons.FindIndex(pButton => pButton.HasFocus());
        var next = pDirection > 0
            ? Mathf.PosMod(current + 1, buttons.Count)
            : (current <= 0 ? buttons.Count - 1 : current - 1);
        buttons[next].GrabFocus();
    }

    /// <summary>And the pause page the menu is showing right now.</summary>
    public VBoxContainer? CurrentPausePage() => _pauseMain.Visible ? _pauseMain
        : _pauseOptions.Visible ? _pauseOptions
        : _pauseCheats;

    /// <summary>And the options page shows the language the settings hold.</summary>
    public void SetLanguageSelection(string pLocaleId)
    {
        for (var index = 0; index < _languageChoice.ItemCount; index++)
        {
            if (_languageChoice.GetItemMetadata(index).AsString() == pLocaleId)
            {
                _languageChoice.Selected = index;
                return;
            }
        }
    }

    /// <summary>And the toggle reflects the stored setting.</summary>
    public void SetIntegerScale(bool pEnabled)
    {
        _integerScale = pEnabled;
        _integerScaleBox.ButtonPressed = pEnabled;
        QueueRedraw();
    }

    /// <summary>
    /// And the game state the screen draws from. -- <strong>And the frame is
    /// copied as it is, at its own size</strong> -- **RM2K hands over the
    /// composed 320 by 240 screen, while MV and MZ hand over the whole
    /// painted map** (measured: LegalTruck 816x624, Camellia 672x864).
    /// **Cropping both to 320 by 240 showed the top left corner of a map
    /// that is three screens wide and called it the game.**
    /// </summary>
    public void SetGameState(Rm2kPixelBuffer? pMap, int pFrameCount, string pDiagnostic = "")
    {
        RenderDiagnostic = pDiagnostic;
        _mapPixelWidth = pMap?.Width ?? 0;
        _mapPixelHeight = pMap?.Height ?? 0;
        _frameCount = pFrameCount;
        if (pMap == null || pMap.Width <= 0 || pMap.Height <= 0)
        {
            _mapTexture = null;
            _mapSignature = "";
            Array.Clear(_viewPixels);
            _viewWidth = ScreenWidth;
            _viewHeight = ScreenHeight;
            QueueRedraw();
            return;
        }

        // **Und der Puffer waechst mit dem Bild, statt es zu beschneiden.**
        // Ein 672x864-Frame braucht 2,3 MB statt der 307 KB eines
        // RM2K-Bildschirms; das ist eine Kopie pro Bildwechsel und keine
        // pro Frame, weil die Signatur den Wechsel erkennt.
        if (_viewPixels.Length < pMap.Pixels.Length)
        {
            _viewPixels = new byte[pMap.Pixels.Length];
            _mapSignature = "";
        }
        else
        {
            Array.Clear(_viewPixels, pMap.Pixels.Length, _viewPixels.Length - pMap.Pixels.Length);
        }
        _viewWidth = pMap.Width;
        _viewHeight = pMap.Height;
        Array.Copy(pMap.Pixels, _viewPixels, pMap.Pixels.Length);
        _mapSignature = "";
        // **Und der Neuzeichnungswunsch wird hier ausgeloest, nicht im
        // Aufrufer.**
        //
        // **Und das ist die letzte Stelle, an der ein Schritt unsichtbar
        // blieb:** `GetMapTexture` baut die Textur beim naechsten `_Draw`
        // neu, **und `_Draw` laeuft nur, wenn jemand `QueueRedraw()`
        // sagt.**  `Main._Process` rief `SetGameState` einmal pro Frame auf
        // und markierte die Zeichenflaeche nie -- **also blieb das Fenster
        // auf dem Bild, das beim Start gemalt wurde, waehrend die Logik
        // weiterlief** (gemessen live: `hero=5/11` bei 0 geaenderten
        // Pixeln).
        QueueRedraw();
    }

    /// <summary>
    /// And a command window to draw over the frame, or null to draw none.
    /// </summary>
    /// <param name="pX">Where the engine puts it, in the engine's pixels.</param>
    /// <param name="pY">And where it puts it from the top.</param>
    /// <param name="pWidth">And how wide the engine makes it.</param>
    /// <param name="pHeight">And how tall.</param>
    /// <param name="pRowHeight">And the line height the engine uses.</param>
    /// <param name="pRows">The commands, in the order the engine lists them.</param>
    /// <param name="pIndex">And which of them the cursor is on.</param>
    /// <remarks>
    /// <strong>And this draws a list the engine and its plugins decided
    /// on</strong>, at the place and size the engine's own
    /// <c>commandWindowRect</c> gives -- scaled with the frame, so it lands on
    /// the window where the engine would have put it. It is not a
    /// reimplementation of <c>Window_Command</c>: the skin, the font and the
    /// cursor's own animation are the engine's, and what this has instead is
    /// the list, the placement and the cursor's position.
    /// </remarks>
    public void SetCommandWindow(
        int pX, int pY, int pWidth, int pHeight, int pRowHeight,
        IReadOnlyList<string>? pRows, int pIndex)
    {
        _commandRows.Clear();
        if (pRows != null && pRows.Count > 0 && pWidth > 0 && pHeight > 0)
        {
            _commandRows.AddRange(pRows);
            _commandX = pX;
            _commandY = pY;
            _commandWidth = pWidth;
            _commandHeight = pHeight;
            _commandRowHeight = pRowHeight > 0 ? pRowHeight : 36;
            _commandIndex = Math.Clamp(pIndex, 0, _commandRows.Count - 1);
            _commandVisible = true;
        }
        else
        {
            _commandVisible = false;
        }

        QueueRedraw();
    }

    /// <summary>
    /// And what the command window is showing, for a caller that must check it.
    /// </summary>
    /// <remarks>
    /// <strong>And this exists so the list can be asserted without a
    /// screen.</strong> The drawing itself is Godot's; what a test can check is
    /// that the engine's list, its placement and its cursor reached the view at
    /// all -- which is exactly what was missing: measured before this, nothing
    /// under <c>project/app</c> read <c>TitleCommands</c>, so a player saw the
    /// title image and no menu.
    /// </remarks>
    public (int X, int Y, int Width, int Height, int Index, IReadOnlyList<string> Rows)
        CommandWindow() =>
        (_commandX, _commandY, _commandWidth, _commandHeight, _commandIndex,
            _commandRows);

    /// <summary>And the texture reuses the small view buffer.</summary>
    private ImageTexture? GetMapTexture()
    {
        if (_mapPixelWidth <= 0 || _mapPixelHeight <= 0)
        {
            return null;
        }
        var signature = $"{_frameCount}:{_viewWidth}x{_viewHeight}";
        if (_mapTexture != null && signature == _mapSignature)
        {
            return _mapTexture;
        }
        var image = Image.CreateFromData(
            _viewWidth, _viewHeight, false, Image.Format.Rgba8, _viewPixels);
        if (image == null)
        {
            return null;
        }
        _mapTexture = ImageTexture.CreateFromImage(image);
        _mapSignature = signature;
        return _mapTexture;
    }

    /// <summary>And the presentation state the screen shows.</summary>
    public void SetPresentation(
        bool pMessageVisible, string pMessage,
        IReadOnlyList<string>? pChoices, int pSelectedIndex,
        bool pInputPending, int pInputValue)
    {
        _messageVisible = pMessageVisible;
        _messageText = pMessage ?? "";
        _messagePanel.Visible = pMessageVisible;
        if (pMessageVisible)
        {
            _messageLabel.Text = _messageText + "\n" + Tr("GAME_CONTINUE_HINT");
        }

        var choices = pChoices ?? Array.Empty<string>();
        var choicesChanged = choices.Count != _choiceOptions.Count;
        if (!choicesChanged)
        {
            for (var index = 0; index < choices.Count && !choicesChanged; index++)
            {
                if (choices[index] != _choiceOptions[index])
                {
                    choicesChanged = true;
                }
            }
        }
        if (choicesChanged)
        {
            _choiceOptions.Clear();
            _choiceOptions.AddRange(choices);
            foreach (var child in _choiceBox.GetChildren())
            {
                child.QueueFree();
            }
            for (var index = 0; index < _choiceOptions.Count; index++)
            {
                var choiceIndex = index;
                var button = new Button();
                button.Text = $"{index + 1}. {_choiceOptions[index]}";
                button.CustomMinimumSize = new Vector2(380, 38);
                button.Pressed += () => ChoiceSelected?.Invoke(choiceIndex);
                _choiceBox.AddChild(button);
            }
        }
        _choiceSelected = pSelectedIndex;
        _choicePanel.Visible = _choiceOptions.Count > 0;
        for (var index = 0; index < _choiceBox.GetChildCount(); index++)
        {
            if (_choiceBox.GetChild(index) is Button button)
            {
                button.Modulate = index == pSelectedIndex ? new Color(1.0f, 0.86f, 0.45f) : Colors.White;
            }
        }

        _inputPending = pInputPending;
        _inputPanel.Visible = pInputPending;
        if (pInputPending && _inputSpin.Value != pInputValue)
        {
            _inputSpin.Value = pInputValue;
        }
    }

    /// <summary>And the battle state feeds the battle view.</summary>
    public void SetBattle(GameSimulationState? pState)
    {
        _battleView.SetzeZustand(pState);
        var running = _battleView.LaeuftEinKampf();
        _battleView.Visible = running;
        if (running)
        {
            _battleView.QueueRedraw();
        }
    }

    /// <summary>And a fresh run starts with the pause menu shut.</summary>
    public void Reset()
    {
        _mapPixelWidth = 0;
        _mapPixelHeight = 0;
        _mapTexture = null;
        _mapSignature = "";
        Array.Clear(_viewPixels);
        ClosePause();
        _messageVisible = false;
        _messagePanel.Visible = false;
        _choiceOptions.Clear();
        _choicePanel.Visible = false;
        _inputPending = false;
        _inputPanel.Visible = false;
        _battleView.SetzeZustand(null);
        _battleView.Visible = false;
        QueueRedraw();
    }

    /// <summary>And F4 flips the menu, which also pauses the run.</summary>
    public void TogglePause()
    {
        if (IsPauseOpen)
        {
            ClosePause();
        }
        else
        {
            OpenPause();
        }
    }

    /// <summary>And the menu opens on its first page.</summary>
    public void OpenPause()
    {
        IsPauseOpen = true;
        ShowPausePage(null);
        _pauseShade.Visible = true;
        _pausePanel.Visible = true;
        // **Und  das  Menue  startet  mit  einem  sichtbaren  Auswahlrahmen**
        // -- **denn  eine  Taste  ohne  Fokus  hat  keinen  Rand,  und  dann
        // weiss  der  Spieler  nicht,  was  Enter  ausloest.**
        var first = _pauseMain.GetChildren().OfType<Button>().FirstOrDefault();
        first?.GrabFocus();
    }

    /// <summary>And closing the menu returns to the run.</summary>
    public void ClosePause()
    {
        IsPauseOpen = false;
        _pauseShade.Visible = false;
        _pausePanel.Visible = false;
    }

    private void ShowPausePage(VBoxContainer? pPage)
    {
        _pauseMain.Visible = pPage == null;
        _pauseOptions.Visible = ReferenceEquals(pPage, _pauseOptions);
        _pauseCheats.Visible = ReferenceEquals(pPage, _pauseCheats);
    }

    private void OnMessageInput(InputEvent pEvent)
    {
        if (pEvent is InputEventMouseButton mouse && mouse.Pressed
            && mouse.ButtonIndex == MouseButton.Left)
        {
            ContinueRequested?.Invoke();
        }
    }

    /// <summary>
    /// And the visible slice of the map, scaled into the window with the
    /// pixel aspect kept. -- <strong>And the numbers come from the camera
    /// rules the Player uses</strong>: the same scroll offset, the same
    /// clamp, and for a map smaller than the screen the zero offset that
    /// leaves the black border on the right.
    /// </summary>
    /// <summary>
    /// And the letterbox the composed screen is drawn into: the pixel aspect
    /// is kept, the scale fills the window, and the remainder is black bars.
    /// The source rectangle is always the whole frame, because the frame is
    /// already the game's own view.
    /// </summary>
    public static (Rect2 Dest, Rect2 Src) ComputeGameView(
        Vector2 pCanvas, int pFrameWidth, int pFrameHeight, bool pIntegerScale)
    {
        if (pFrameWidth <= 0 || pFrameHeight <= 0)
        {
            return (new Rect2(Vector2.Zero, Vector2.Zero), new Rect2(Vector2.Zero, Vector2.Zero));
        }
        var scale = Mathf.Min(pCanvas.X / pFrameWidth, pCanvas.Y / pFrameHeight);
        if (!float.IsFinite(scale) || scale <= 0)
        {
            return (new Rect2(Vector2.Zero, Vector2.Zero), new Rect2(Vector2.Zero, Vector2.Zero));
        }
        if (pIntegerScale)
        {
            scale = Mathf.Max(1.0f, Mathf.Floor(scale));
        }
        var drawnWidth = pFrameWidth * scale;
        var drawnHeight = pFrameHeight * scale;
        var origin = new Vector2((pCanvas.X - drawnWidth) / 2.0f, (pCanvas.Y - drawnHeight) / 2.0f);
        return (new Rect2(origin, new Vector2(drawnWidth, drawnHeight)),
            new Rect2(Vector2.Zero, new Vector2(pFrameWidth, pFrameHeight)));
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black, true);
        var texture = GetMapTexture();
        var (dest, src) = ComputeGameView(Size, _viewWidth, _viewHeight, _integerScale);
        if (texture != null)
        {
            DrawTextureRectRegion(texture, dest, src);
        }
        // **Und  ein  fehlendes  Bild  wird  benannt  und  nicht  schwarz
        //  gelassen** -- **denn  ein  schwarzer  Schirm  ohne  Grund  sieht
        //  wie  ein  kaputtes  Programm  aus  und  nicht  wie  ein
        //  fehlendes  Chipset.**
        if (texture == null && !string.IsNullOrEmpty(RenderDiagnostic))
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(24, 40), RenderDiagnostic,
                HorizontalAlignment.Left, Size.X - 48, 16, new Color("e8a24a"));
        }

        // **And the engine's command window, where the engine puts it.**
        // Measured: `Scene_Title.commandWindowRect` reserves three lines, and
        // a project whose plugins add more commands than that has a window
        // `Window_Command` scrolls -- so the rows drawn are the ones that
        // fit, moved down to keep the cursor in sight, which is what
        // `Window_Selectable.ensureCursorVisible` does.
        if (!_commandVisible || _commandRows.Count == 0 || dest.Size.X <= 0
            || _viewWidth <= 0)
        {
            return;
        }

        var massstab = dest.Size.X / _viewWidth;
        var links = dest.Position.X + _commandX * massstab;
        var oben = dest.Position.Y + _commandY * massstab;
        var breite = _commandWidth * massstab;
        var hoehe = _commandHeight * massstab;
        var zeile = _commandRowHeight * massstab;
        if (zeile <= 0)
        {
            return;
        }

        DrawRect(new Rect2(links, oben, breite, hoehe),
            new Color(0.06f, 0.06f, 0.10f, 0.88f), true);
        DrawRect(new Rect2(links, oben, breite, hoehe),
            new Color(0.85f, 0.85f, 0.92f, 0.95f), false, 2f);

        var sichtbar = Math.Max(1, (int)(hoehe / zeile));
        var erster = _commandIndex >= sichtbar ? _commandIndex - sichtbar + 1 : 0;
        var schrift = (int)Math.Max(11f, zeile * 0.6f);
        for (var i = 0; i < sichtbar && erster + i < _commandRows.Count; i++)
        {
            var mitte = oben + (i + 0.5f) * zeile;
            if (erster + i == _commandIndex)
            {
                DrawRect(
                    new Rect2(links + 3, mitte - zeile / 2 + 1, breite - 6, zeile - 2),
                    new Color(0.35f, 0.55f, 1.0f, 0.45f), true);
            }
            DrawString(ThemeDB.FallbackFont,
                new Vector2(links, mitte + schrift * 0.36f), _commandRows[erster + i],
                HorizontalAlignment.Center, breite, schrift, Colors.White);
        }
    }
}
