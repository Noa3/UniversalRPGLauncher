using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rtp;

namespace UniversalRPG.App.Ui;

public partial class Main : Control
{
	private static readonly FontFile InterfaceFont =
		GD.Load<FontFile>("res://assets/fonts/NotoSansCJKsc-Regular.otf");

	// NO_TRANSLATE: Native language names.
	private static readonly (string Locale, string Label)[] InterfaceLocales =
	{
		("en", "English"),
		("auto", "LANGUAGE_SYSTEM"),
		("de", "Deutsch"),
		("es", "Español"),
		("fr", "Français"),
		("ja", "日本語"),
		("ko", "한국어"),
		("zh_CN", "简体中文"),
	};

	private static readonly Color ColorBackground = new("101015");
	private static readonly Color ColorPanel = new("1a1a23");
	private static readonly Color ColorPanelLight = new("232330");
	private static readonly Color ColorText = new("f2efe7");
	private static readonly Color ColorMuted = new("aaa7b5");
	private static readonly Color ColorAccent = new("e8a24a");
	private static readonly Color ColorBorder = new("343443");

	private readonly EnginePluginRegistry _pluginRegistry = BuiltInEnginePluginCatalog.CreateRuntimeRegistry();
	private readonly GameLibrary _library;
	private readonly RuntimeLauncher _launcher;
	private int _appliedRenderFps = -1;
	private GameLibrary.GameEntry? _selectedGame;

	private MarginContainer _pageMargin = null!;
	private BoxContainer _body = null!;
	private PanelContainer _gamesPanel = null!;
	private ItemList _gameList = null!;
	private Label _folderPath = null!;
	private Label _detailsTitle = null!;
	private Label _detailsEngine = null!;
	private OptionButton _engineChoice = null!;
	private Label _detailsPath = null!;
	private Label _detailsEvidence = null!;
	private Label _runtimeState = null!;
	private Label _presentationState = null!;
	private Rm2kMapPreview _mapPreview = null!;

	/// <summary>
	/// And the battle, because a battle that happens but cannot be
	/// seen is a battle that did not happen.
	/// </summary>
	private Rm2kBattleView _battleView = null!;
	private MzAudioOutput _mzAudio = null!;
	private MzMapPreview _mzMap = new();
	private VBoxContainer _presentationControls = null!;
	private Button _dismissMessageButton = null!;
	private HBoxContainer _choiceButtons = null!;

	/// <summary>
	/// And the battle's commands, because a battle the player cannot
	/// enter is a battle that runs without them.
	/// </summary>
	private HBoxContainer _battleCommandButtons = null!;
	private HBoxContainer _zielButtons = null!;

	/// <summary>And the target list's own signature.</summary>
	private string _zielSignature = "";
	private VBoxContainer _menuPanel = null!;
	private VBoxContainer _shopPanel = null!;
	private Label _shopHead = null!;
	private HBoxContainer _shopRows = null!;
	private string _shopSignature = "";
	private Label? _menuGold;
	private Label? _menuRechte;
	private readonly System.Collections.Generic.List<Label> _menuRows = new();

	/// <summary>
	/// And whether the menu panel has been built.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>And the panel is built once</strong>, -- <strong>because a
	/// label per frame is garbage in the frame loop</strong>.
	/// </para>
	/// </remarks>
	private bool menueAufbauen;

	/// <summary>
	/// And which commands were shown last, so a new button is only
	/// built when the list actually changed.
	/// </summary>
	private string _battleCommandSignature = "";
	private SpinBox _inputSpinBox = null!;
	private Button _submitInputButton = null!;
	private string _choiceSignature = "";
	private Button _launchButton = null!;
	private Rm2kGameScreen _gameScreen = null!;
	private Label _status = null!;
	private FileDialog _folderDialog = null!;
	private OptionButton _languageMenu = null!;
	private readonly RenderProfile _renderProfile = new();
	private readonly Rm2kInputMapper _inputMapper = new();
	private readonly System.Collections.Concurrent.ConcurrentQueue<string> _rtpProgress = new();
	private bool _launchInProgress;
	private volatile bool _closing;

	public Main()
	{
		_library = new GameLibrary(pRuntimeRegistry: _pluginRegistry);
		_launcher = new RuntimeLauncher(_pluginRegistry);
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcess(true);
		ApplyRenderFrameRate(30);
		LoadLocale();
		BuildTheme();
		// **Und `BuildInterface` baut das Formular genau einmal** -- **und
		// nicht auch noch, weil `_Ready` ein zweites Mal laeuft.**
		//
		// **Und  das  war  kein  Testartefakt,  sondern  der  Grund,  warum
		// der  Godot-Log  beim  Start  ``Can't add child ... already has
		// a parent``  schrieb** -- **ein  Control  kann  nicht  an  zwei
		//  Eltern  haengen,  und  das  Ergebnis  war  eine  halb  gebaute
		//  Oberflaeche  mit  doppelten  Controls.**
		//
		// **Und  es  ist  idempotent  statt  boolean-getrackt**,  --  **denn
		//  `_Ready`  ist  auch  der  Ort,  an  dem  ein  zurueckgesetzter
		//  Knoten  neu  aufgebaut  wird,  und  ein  Flag  waere  nach  einem
		//  `QueueFree`  der  Elemente  falsch  gewesen.**
		if (GetChildCount() == 0)
		{
			BuildInterface();
		}
		_library.LoadSettings();
		_folderPath.Text = _library.RootPath;
		GetViewport().SizeChanged += ApplyResponsiveLayout;
		_inputMapper.SetTouchViewport(GetViewportRect().Size);
		ApplyResponsiveLayout();
		RefreshLibrary();
	}

	public override void _ExitTree()
	{
		_closing = true;
		if (IsInsideTree()) GetViewport().SizeChanged -= ApplyResponsiveLayout;
		_launcher.Shutdown();
	}

	private void BuildTheme()
	{
		var appTheme = new Theme();
		appTheme.DefaultFont = InterfaceFont;
		appTheme.DefaultFontSize = 17;
		appTheme.SetColor("font_color", "Label", ColorText);
		appTheme.SetColor("font_color", "Button", ColorText);
		appTheme.SetColor("font_hover_color", "Button", Colors.White);
		appTheme.SetColor("font_disabled_color", "Button", ColorMuted.Darkened(0.25f));
		appTheme.SetColor("font_color", "ItemList", ColorText);
		appTheme.SetColor("font_selected_color", "ItemList", new Color("15131a"));
		appTheme.SetFontSize("font_size", "Button", 16);
		appTheme.SetFontSize("font_size", "ItemList", 17);
		appTheme.SetConstant("separation", "VBoxContainer", 12);
		appTheme.SetConstant("separation", "HBoxContainer", 10);
		appTheme.SetConstant("separation", "BoxContainer", 18);
		appTheme.SetStylebox("panel", "PanelContainer", MakeStyleBox(ColorPanel, ColorBorder, 1, 12));
		appTheme.SetStylebox("normal", "Button", MakeStyleBox(ColorPanelLight, ColorBorder, 1, 9));
		appTheme.SetStylebox("hover", "Button", MakeStyleBox(new Color("30303e"), ColorAccent, 1, 9));
		appTheme.SetStylebox("pressed", "Button", MakeStyleBox(new Color("15151d"), ColorAccent, 1, 9));
		appTheme.SetStylebox("disabled", "Button", MakeStyleBox(new Color("17171e"), ColorBorder, 1, 9));
		appTheme.SetStylebox("panel", "ItemList", MakeStyleBox(ColorPanelLight, ColorBorder, 1, 9));
		appTheme.SetStylebox("selected", "ItemList", MakeStyleBox(ColorAccent, ColorAccent, 0, 6));
		appTheme.SetStylebox("selected_focus", "ItemList", MakeStyleBox(ColorAccent, Colors.White, 1, 6));
		Theme = appTheme;
	}

	private void BuildInterface()
	{
		var background = new ColorRect();
		background.Color = ColorBackground;
		background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		background.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(background);

		_pageMargin = new MarginContainer();
		_pageMargin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_pageMargin);

		var page = new VBoxContainer();
		_pageMargin.AddChild(page);

		var header = new HBoxContainer();
		page.AddChild(header);

		var brand = new VBoxContainer();
		brand.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(brand);

		var title = new Label();
		title.Text = "UNIVERSALRPG";
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", ColorAccent);
		brand.AddChild(title);

		var subtitle = new Label();
		subtitle.Text = Tr("APP_SUBTITLE");
		subtitle.AddThemeColorOverride("font_color", ColorMuted);
		subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		brand.AddChild(subtitle);

		_languageMenu = new OptionButton();
		_languageMenu.TooltipText = Tr("LANGUAGE_TOOLTIP");
		foreach (var localeData in InterfaceLocales)
		{
			var label = localeData.Locale == "auto" ? Tr(localeData.Label) : localeData.Label;
			_languageMenu.AddItem(label);
			_languageMenu.SetItemMetadata(_languageMenu.ItemCount - 1, localeData.Locale);
		}
		_languageMenu.Select(GetLocaleMenuIndex());
		_languageMenu.ItemSelected += ChangeLocale;
		header.AddChild(_languageMenu);
		var renderMode = new OptionButton();
		renderMode.TooltipText = "Rendering compatibility mode";
		renderMode.AddItem("Faithful");
		renderMode.AddItem("Enhanced");
		renderMode.ItemSelected += pIndex => _renderProfile.TrySetMode(
			pIndex == 0 ? RenderCompatibilityMode.Faithful : RenderCompatibilityMode.Enhanced);
		header.AddChild(renderMode);
		var integerScale = new CheckButton();
		integerScale.Text = "Integer scale";
		integerScale.ButtonPressed = _renderProfile.IntegerScaling;
		integerScale.Toggled += pEnabled =>
		{
			if (!_renderProfile.TrySetIntegerScaling(pEnabled))
			{
				integerScale.ButtonPressed = true;
			}
		};
		header.AddChild(integerScale);
		var scale = new SpinBox();
		scale.TooltipText = "Integer scale factor (1-8)";
		scale.MinValue = RenderProfile.MinIntegerScale;
		scale.MaxValue = RenderProfile.MaxIntegerScale;
		scale.Step = 1;
		scale.Value = _renderProfile.IntegerScale;
		scale.ValueChanged += pValue => _renderProfile.TrySetIntegerScale((int)pValue);
		header.AddChild(scale);

		var folderPanel = new PanelContainer();
		page.AddChild(folderPanel);
		var folderMargin = new MarginContainer();
		SetMargins(folderMargin, 16);
		folderPanel.AddChild(folderMargin);
		var folderRow = new HBoxContainer();
		folderMargin.AddChild(folderRow);
		var folderText = new VBoxContainer();
		folderText.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		folderRow.AddChild(folderText);
		var folderCaption = new Label();
		folderCaption.Text = Tr("LIBRARY_FOLDER");
		folderCaption.AddThemeFontSizeOverride("font_size", 13);
		folderCaption.AddThemeColorOverride("font_color", ColorAccent);
		folderText.AddChild(folderCaption);
		_folderPath = new Label();
		_folderPath.AddThemeColorOverride("font_color", ColorMuted);
		_folderPath.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		_folderPath.TooltipText = Tr("LIBRARY_SCAN_HINT");
		folderText.AddChild(_folderPath);
		var chooseButton = new Button();
		chooseButton.Text = Tr("ACTION_CHOOSE_FOLDER");
		chooseButton.CustomMinimumSize = new Vector2(170, 46);
		chooseButton.Pressed += ChooseFolder;
		folderRow.AddChild(chooseButton);
		var refreshButton = new Button();
		refreshButton.Text = Tr("ACTION_RESCAN");
		refreshButton.CustomMinimumSize = new Vector2(130, 46);
		refreshButton.Pressed += RefreshLibrary;
		folderRow.AddChild(refreshButton);

		_body = new BoxContainer();
		_body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		page.AddChild(_body);

		_gamesPanel = new PanelContainer();
		_gamesPanel.CustomMinimumSize = new Vector2(390, 260);
		_gamesPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_gamesPanel.SizeFlagsStretchRatio = 0.8f;
		_body.AddChild(_gamesPanel);
		var gamesMargin = new MarginContainer();
		SetMargins(gamesMargin, 16);
		_gamesPanel.AddChild(gamesMargin);
		var gamesColumn = new VBoxContainer();
		gamesMargin.AddChild(gamesColumn);
		var gamesHeading = new Label();
		gamesHeading.Text = Tr("LIBRARY_FOUND_GAMES");
		gamesHeading.AddThemeFontSizeOverride("font_size", 14);
		gamesHeading.AddThemeColorOverride("font_color", ColorAccent);
		gamesColumn.AddChild(gamesHeading);
		_gameList = new ItemList();
		_gameList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_gameList.AllowReselect = true;
		_gameList.ItemSelected += SelectGame;
		_gameList.ItemActivated += SelectGame;
		gamesColumn.AddChild(_gameList);

		var detailsPanel = new PanelContainer();
		detailsPanel.CustomMinimumSize = new Vector2(430, 260);
		detailsPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		detailsPanel.SizeFlagsStretchRatio = 1.2f;
		_body.AddChild(detailsPanel);
		var detailsMargin = new MarginContainer();
		SetMargins(detailsMargin, 22);
		detailsPanel.AddChild(detailsMargin);
		var detailsHost = new VBoxContainer();
		detailsMargin.AddChild(detailsHost);
		var detailsScroll = new ScrollContainer();
		detailsScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		detailsScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		detailsHost.AddChild(detailsScroll);
		var details = new VBoxContainer();
		details.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		detailsScroll.AddChild(details);
		var detailsCaption = new Label();
		detailsCaption.Text = Tr("LIBRARY_SELECTION");
		detailsCaption.AddThemeFontSizeOverride("font_size", 14);
		detailsCaption.AddThemeColorOverride("font_color", ColorAccent);
		details.AddChild(detailsCaption);
		_detailsTitle = new Label();
		_detailsTitle.Text = Tr("DETAIL_NO_SELECTION");
		_detailsTitle.AddThemeFontSizeOverride("font_size", 28);
		_detailsTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		details.AddChild(_detailsTitle);
		_detailsEngine = new Label();
		_detailsEngine.AddThemeColorOverride("font_color", ColorMuted);
		details.AddChild(_detailsEngine);
		_engineChoice = new OptionButton();
		_engineChoice.TooltipText = Tr("ENGINE_CHOICE_TOOLTIP");
		_engineChoice.ItemSelected += SelectDetectedEngine;
		details.AddChild(_engineChoice);
		_detailsPath = new Label();
		_detailsPath.AddThemeColorOverride("font_color", ColorMuted.Darkened(0.08f));
		_detailsPath.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		details.AddChild(_detailsPath);
		_mzAudio = new MzAudioOutput();
		AddChild(_mzAudio);
		_mzMap.CustomMinimumSize = new Vector2(0, 220);
		_mzMap.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		details.AddChild(_mzMap);
		_mapPreview = new Rm2kMapPreview();
		_mapPreview.CustomMinimumSize = new Vector2(0, 180);
		_mapPreview.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		details.AddChild(_mapPreview);

		// **Und  die  Kampfansicht  kommt  neben  die  Karte** --
		// **denn  ein  Kampf  braucht  keine  eigene  Seite,
		//  sondern  eine  eigene  Zeile  im  laufenden  Bild.**
		_battleView = new Rm2kBattleView();
		_battleView.CustomMinimumSize = new Vector2(0, 150);
		_battleView.SizeFlagsVertical =
			Control.SizeFlags.ExpandFill;
		_battleView.Visible = false;
		details.AddChild(_battleView);
		_detailsEvidence = new Label();
		_detailsEvidence.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_detailsEvidence.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		details.AddChild(_detailsEvidence);
		_runtimeState = new Label();
		_runtimeState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		details.AddChild(_runtimeState);
		_presentationState = new Label();
		_presentationState.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_presentationState.AddThemeColorOverride("font_color", ColorAccent);
		details.AddChild(_presentationState);
		_presentationControls = new VBoxContainer();
		_presentationControls.Visible = false;
		details.AddChild(_presentationControls);
		_dismissMessageButton = new Button();
		_dismissMessageButton.Text = "Continue";
		_dismissMessageButton.Pressed += DismissRuntimeMessage;
		_presentationControls.AddChild(_dismissMessageButton);
		_choiceButtons = new HBoxContainer();
		_presentationControls.AddChild(_choiceButtons);

		_battleCommandButtons = new HBoxContainer();
		_presentationControls.AddChild(_battleCommandButtons);

		// **Und  die  Zielleiste  kommt  darunter.**
		//
		// **Und  sie  ist  kein  Zufallsgenerator**, -- **sondern  eine
		//  Liste  der  lebenden  Gegner  in  der  Reihenfolge  der
		//  Truppe**, -- **und  jeder  Knopf  benennt  das  Ziel**, --
		// **denn  `FuehreZugAus`  nahm  vorher  einen  Index  und  wusste
		//  damit  nicht,  ob  er  einen  Gegner  oder  einen  Helden
		//  meinte.**
		_zielButtons = new HBoxContainer();
		_zielButtons.Visible = false;
		_presentationControls.AddChild(_zielButtons);
		// **Und  das  Menuepanel  kommt  in  dieselbe  Steuermenge.**
		//
		// **Und  es  traegt  vier  Zeilen**, -- **denn
		//  `MaxPartyMembers` ist  vier** -- **und  es  wird  einmal
		//  gebaut  und  nicht  bei  jedem  Bild.**
		_menuPanel = new VBoxContainer();
		_menuPanel.Visible = false;
		_presentationControls.AddChild(_menuPanel);
		for (var i = 0; i < UniversalRPG.Rm2k.Simulation
			.GameSimulationState.MaxPartyMembers; i++)
		{
			var zeile = new Label();
			zeile.Visible = false;
			_menuPanel.AddChild(zeile);
			_menuRows.Add(zeile);
		}

		_menuGold = new Label();
		_menuPanel.AddChild(_menuGold);
		_menuRechte = new Label();
		_menuPanel.AddChild(_menuRechte);

		// **Und  die  Ladenflaeche  haengt  an  derselben  Zeile.**
		//
		// **Und  sie  gehoert  neben  das  Menuepanel** -- **denn  beides
		//  ist  ein  Zustand  des  Spiels**, -- **und  ein  Shop,  den
		//  kein  Fenster  zeigt,  ist  ein  toter  Befehl.**
		_shopPanel = new VBoxContainer();
		_shopPanel.Visible = false;
		_presentationControls.AddChild(_shopPanel);
		_shopHead = new Label();
		_shopPanel.AddChild(_shopHead);
		_shopRows = new HBoxContainer();
		_shopPanel.AddChild(_shopRows);
		_inputSpinBox = new SpinBox();
		_inputSpinBox.MinValue = 0;
		_inputSpinBox.MaxValue = int.MaxValue;
		_inputSpinBox.Step = 1;
		_presentationControls.AddChild(_inputSpinBox);
		_submitInputButton = new Button();
		_submitInputButton.Text = "Submit input";
		_submitInputButton.Pressed += SubmitRuntimeInput;
		_presentationControls.AddChild(_submitInputButton);
		_launchButton = new Button();
		_launchButton.Text = Tr("ACTION_NOT_PLAYABLE");
		_launchButton.CustomMinimumSize = new Vector2(0, 50);
		_launchButton.Disabled = true;
		_launchButton.AddThemeColorOverride("font_color", new Color("19151a"));
		_launchButton.AddThemeStyleboxOverride("normal", MakeStyleBox(ColorAccent, ColorAccent, 0, 9));
		_launchButton.AddThemeStyleboxOverride("hover", MakeStyleBox(ColorAccent.Lightened(0.08f), Colors.White, 1, 9));
		_launchButton.Pressed += LaunchSelectedGame;
		detailsHost.AddChild(_launchButton);
		_status = new Label();
		_status.AddThemeColorOverride("font_color", ColorMuted);
		_status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		page.AddChild(_status);

		_folderDialog = new FileDialog();
		_folderDialog.Title = Tr("DIALOG_CHOOSE_FOLDER");
		_folderDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
		_folderDialog.Access = FileDialog.AccessEnum.Filesystem;
		_folderDialog.UseNativeDialog = true;
		_folderDialog.DirSelected += SetFolder;
		AddChild(_folderDialog);

		// **Und  das  Spiel  bekommt  das  ganze  Fenster** -- **und  das
		//  Menue  der  Pause  haengt  an  ihm  und  nicht  mehr  an  einem
		//  Knopf  in  der  Detailspalte.**
		_gameScreen = new Rm2kGameScreen();
		_gameScreen.ContinueRequested += OnGameContinue;
		_gameScreen.ChoiceSelected += OnGameChoice;
		_gameScreen.InputSubmitted += OnGameInputSubmitted;
		_gameScreen.StopRuntimeRequested += StopActiveRuntime;
		_gameScreen.CloseProgramRequested += () =>
		{
			_closing = true;
			GetTree().Quit();
		};
		_gameScreen.LanguageRequested += RequestLocale;
		_gameScreen.IntegerScaleRequested += pEnabled => _renderProfile.TrySetIntegerScaling(pEnabled);
		_gameScreen.SetIntegerScale(_renderProfile.IntegerScaling);
		_gameScreen.SetLanguageSelection(GetSavedLocale());
		AddChild(_gameScreen);
	}

	/// <summary>And the language menu of the pause screen persists the same way.</summary>
	private void RequestLocale(string pLocaleId)
	{
		var config = new ConfigFile();
		config.Load(GameLibrary.SettingsPath);
		config.SetValue("interface", "locale", pLocaleId);
		config.Save(GameLibrary.SettingsPath);
		TranslationServer.SetLocale(pLocaleId == "auto" ? OS.GetLocaleLanguage() : pLocaleId);
		GetTree().ReloadCurrentScene();
	}

	private void UpdateWindowTitle(bool pGameMode)
	{
		if (!pGameMode)
		{
			GetWindow().Title = "UniversalRPG";
			return;
		}
		var title = _selectedGame?.Title ?? "UniversalRPG";
		GetWindow().Title = $"UniversalRPG - {title} - F4: Pause";
	}

	private void OnGameContinue()
	{
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime rm2k)
		{
			rm2k.DrueckeFort();
		}
	}

	private void OnGameChoice(int pIndex)
	{
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime rm2k)
		{
			rm2k.Presentation.SelectChoice(pIndex);
		}
	}

	private void OnGameInputSubmitted(int pValue)
	{
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime rm2k)
		{
			rm2k.Presentation.SetInputValue(pValue);
		}
	}

	private void RefreshLibrary()
	{
		_status.Text = Tr("STATUS_SCANNING");
		_gameList.Clear();
		_selectedGame = null;
		var games = _library.Scan();
		foreach (var game in games)
		{
			var index = _gameList.AddItem($"{game.Title}  |  {game.Detection.GetEngineName()}");
			_gameList.SetItemTooltip(index, game.Path);
		}
		if (games.Count == 0)
		{
			ClearDetails();
			_status.Text = Tr("STATUS_NO_GAMES");
		}
		else
		{
			_gameList.Select(0);
			SelectGame(0);
			_status.Text = TrN("STATUS_ONE_GAME", "STATUS_MANY_GAMES", games.Count)
				.Replace("{count}", games.Count.ToString());
		}
	}

	private void SelectGame(long pIndex)
	{
		_selectedGame = _library.Games[(int)pIndex];
		var detection = _selectedGame.Detection;
		_engineChoice.Clear();
		_engineChoice.AddItem(Tr("ENGINE_CHOICE_AUTO"));
		_engineChoice.SetItemMetadata(0, "");
		foreach (var candidate in detection.Candidates.Where(candidate =>
			candidate.Status == EngineDetectionStatus.Supported))
		{
			var index = _engineChoice.ItemCount;
			_engineChoice.AddItem(candidate.DisplayName);
			_engineChoice.SetItemMetadata(index, candidate.PluginId);
			if (candidate.PluginId == _selectedGame.ExplicitPluginId) _engineChoice.Select(index);
		}
		_engineChoice.Visible = _engineChoice.ItemCount > 1
			&& (detection.Report.IsAmbiguous || !string.IsNullOrEmpty(_selectedGame.ExplicitPluginId));
		_engineChoice.Disabled = _launchInProgress;
		_detailsTitle.Text = _selectedGame.Title;
		_detailsEngine.Text = Tr("DETAIL_ENGINE_CONFIDENCE")
			.Replace("{engine}", detection.GetEngineName())
			.Replace("{confidence}", detection.GetConfidenceString());
		if (detection.EngineVersion != null)
		{
			_detailsEngine.Text += $" · Runtime {detection.EngineVersion}";
		}
		_detailsPath.Text = _selectedGame.Path;
		_mapPreview.SetMapData(null);
		var facts = new List<string>
		{
			$"Plugin: {(_selectedGame.SelectedPluginId == "" ? "none" : _selectedGame.SelectedPluginId)}",
			$"Compatibility: {_selectedGame.CompatibilityStatus}",
		};
		foreach (var candidate in _selectedGame.Candidates)
		{
			facts.Add($"- Candidate {candidate.PluginId}: {candidate.Status}, score {candidate.Score}/1000");
		}
		foreach (var item in detection.Evidence)
		{
			facts.Add("- " + item);
		}
		foreach (var diagnostic in _selectedGame.Diagnostics)
		{
			facts.Add($"- [{diagnostic.Severity}/{diagnostic.Code}] {diagnostic.Message}");
		}
		if (detection.HasNativeLibraries)
		{
			facts.Add("- " + Tr("DETAIL_NATIVE_LIBRARIES"));
		}
		if (!string.IsNullOrEmpty(detection.RtpDependency))
		{
			facts.Add("- " + Tr("DETAIL_RTP").Replace("{rtp}", detection.RtpDependency));
		}
		_detailsEvidence.Text = string.Join("\n", facts);

		var support = _launcher.GetSupport(_selectedGame);
		_runtimeState.Text = Tr("DETAIL_RUNTIME_STATE")
			.Replace("{label}", support.Label)
			.Replace("{reason}", support.Reason);
		_runtimeState.AddThemeColorOverride(
			"font_color",
			support.State == RuntimeLauncher.SupportState.Available ? ColorAccent : ColorMuted
		);
		_launchButton.Disabled = _launchInProgress
			|| support.State != RuntimeLauncher.SupportState.Available;
		_launchButton.Text = !_launchButton.Disabled ? Tr("ACTION_START_GAME") : Tr("ACTION_NOT_PLAYABLE");
	}

	private void SelectDetectedEngine(long pIndex)
	{
		if (_selectedGame == null || _launchInProgress || pIndex < 0
			|| pIndex >= _engineChoice.ItemCount) return;
		var pluginId = _engineChoice.GetItemMetadata((int)pIndex).AsString();
		if (!_library.TrySelectEngine(_selectedGame, pluginId, out var error))
		{
			_status.Text = error;
			return;
		}
		var index = _library.Games.IndexOf(_selectedGame);
		if (index >= 0) SelectGame(index);
	}

	private void ClearDetails()
	{
		_engineChoice.Visible = false;
		_detailsTitle.Text = Tr("DETAIL_NO_GAMES");
		_detailsEngine.Text = "";
		_detailsPath.Text = _library.RootPath;
		_detailsEvidence.Text = Tr("DETAIL_DETECTION_SUPPORT");
		_runtimeState.Text = Tr("DETAIL_RUNTIME_DEVELOPMENT");
		_presentationState.Text = "";
		_launchButton.Disabled = true;
		_launchButton.Text = Tr("ACTION_NOT_PLAYABLE");
	}

	private void ChooseFolder()
	{
		_folderDialog.CurrentDir = _library.RootPath;
		_folderDialog.PopupCenteredRatio(0.82f);
	}

	private void SetFolder(string pPath)
	{
		var error = _library.SetRootPath(pPath);
		if (error != Error.Ok)
		{
			_status.Text = Tr("ERROR_FOLDER").Replace("{error}", ((long)error).ToString());
			return;
		}
		_folderPath.Text = _library.RootPath;
		RefreshLibrary();
	}

	public override void _UnhandledInput(InputEvent pEvent)
	{
		if (_gameScreen.Visible && pEvent is InputEventKey shortcut && shortcut.Pressed
			&& !shortcut.Echo && shortcut.Keycode == Key.F4)
		{
			_gameScreen.TogglePause();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (_gameScreen.Visible && _gameScreen.IsPauseOpen)
		{
			if (pEvent is InputEventKey resumeKey && resumeKey.Pressed && !resumeKey.Echo)
			{
				if (resumeKey.Keycode == Key.Escape)
				{
					_gameScreen.ClosePause();
				}
				else
				{
					// **Und  das  Menue  bewegt  seinen  eigenen  Auswahlrahmen**
					// -- **denn  es  nimmt  jede  Taste  an,  und  ohne  das
					// waere  der  Pfeil  eine  wirkungslose Eingabe.**
					_gameScreen.HandlePauseKey(resumeKey);
				}
			}
			GetViewport().SetInputAsHandled();
			return;
		}
		if (_launcher.ActiveRuntimeState != PluginRuntimeState.Running)
		{
			return;
		}
		// **Und MV und MZ bekommen dieselbe Taste wie RM2K** -- **denn
		// beide Laufzeien bieten `SubmitInput`, und ein Fenster, das den
		// Runtime-Typ prueft, haette zwei Eingabepfade, von denen einer
		// ungetestet bliebe.**  Der ganze Rest des Handlers gehoert zum
		// RM2K-Dialog und laeuft nur dort.
		if (_launcher.ActiveRuntime is MzEngineRuntime mzRuntime)
		{
			var mzAction = _inputMapper.Resolve(pEvent);
			if (mzAction != UniversalRPG.Rm2k.Input.Rm2kInputAction.None
				&& mzRuntime.SubmitInput(mzAction))
			{
				GetViewport().SetInputAsHandled();
			}
			return;
		}
		if (_launcher.ActiveRuntime is not Rm2kEngineRuntime rm2k)
		{
			return;
		}
		var keyEvent = pEvent as InputEventKey;
		if (keyEvent != null && (!keyEvent.Pressed || keyEvent.Echo)) return;
		var action = _inputMapper.Resolve(pEvent);
		if (keyEvent == null && action == Rm2kInputAction.None) return;
		if (rm2k.Presentation.MessageVisible && action == Rm2kInputAction.Confirm)
		{
			// **Und  die  Taste  macht  beides** --
			// **denn  das  Schliessen  des  Fensters  und  das  Aufloesen  der
			//  Wartefrage  sind  zwei  Dinge.**
			//
			// **Und  vorher  wurde  hier  nur  das  Fenster  geschlossen**,
			// **und  die  Seite  blieb  fuer  immer  stehen.**
			//
			// **Und  ein  Kampf  ist  nicht  per  Taste  zu  beenden** --
			// **und  `DrueckeFort`  weigert  sich  genau  das,  und  der
			//  Grund  landet  im  Protokoll.**
			rm2k.DrueckeFort();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (rm2k.Presentation.ActiveChoice != null)
		{
			var choice = rm2k.Presentation.ActiveChoice;
			var index = choice.SelectedIndex < 0 ? 0 : choice.SelectedIndex;
			if (action is Rm2kInputAction.MoveUp or Rm2kInputAction.MoveLeft) index--;
			if (action is Rm2kInputAction.MoveDown or Rm2kInputAction.MoveRight) index++;
			if (action is Rm2kInputAction.MoveUp or Rm2kInputAction.MoveLeft or Rm2kInputAction.MoveDown or Rm2kInputAction.MoveRight)
			{
				index = Mathf.PosMod(index, choice.Options.Count);
				choice.Select(index);
				GetViewport().SetInputAsHandled();
			}
			else if (action == Rm2kInputAction.Confirm)
			{
				if (choice.SelectedIndex < 0)
				{
					choice.Select(0);
				}
				GetViewport().SetInputAsHandled();
			}
			return;
		}
		if (rm2k.Presentation.PendingInputVariableId != null)
		{
			if (keyEvent == null)
			{
				if (action == Rm2kInputAction.Confirm) rm2k.Presentation.SetInputValue(rm2k.Presentation.InputValue ?? 0);
				GetViewport().SetInputAsHandled();
				return;
			}
			if (keyEvent.Keycode == Key.Backspace)
			{
				var currentText = (rm2k.Presentation.InputValue ?? 0).ToString();
				if (currentText.Length > 1)
				{
					rm2k.Presentation.SetInputValue(int.Parse(currentText[..^1]));
				}
				else
				{
					rm2k.Presentation.SetInputValue(0);
				}
				GetViewport().SetInputAsHandled();
				return;
			}
			if (action == Rm2kInputAction.Confirm)
			{
				rm2k.Presentation.SetInputValue(rm2k.Presentation.InputValue ?? 0);
				GetViewport().SetInputAsHandled();
				return;
			}
			if (keyEvent.Unicode >= '0' && keyEvent.Unicode <= '9')
			{
				var current = rm2k.Presentation.InputValue ?? 0;
				var digit = (int)(keyEvent.Unicode - '0');
				var next = (long)current * 10 + digit;
				if (next <= int.MaxValue)
				{
					rm2k.Presentation.SetInputValue((int)next);
				}
				GetViewport().SetInputAsHandled();
			}
			return;
		}
		// The verified turn order lives in the runtime: it decides between the
		// own-tile and the in-front touch path, applies the layer rules and walks
		// counter tiles for the action chain.
		if (rm2k.SubmitInput(action))
		{
			GetViewport().SetInputAsHandled();
		}
		else if (action is Rm2kInputAction.MoveUp or Rm2kInputAction.MoveDown
			or Rm2kInputAction.MoveLeft or Rm2kInputAction.MoveRight
			or Rm2kInputAction.Confirm)
		{
			// A map input that was consumed without moving or triggering, such as a
			// blocked step, must not fall through to the UI.
			GetViewport().SetInputAsHandled();
		}
	}

	private void ApplyRenderFrameRate(int pFramesPerSecond)
	{
		if (pFramesPerSecond <= 0 || _appliedRenderFps == pFramesPerSecond) return;
		Engine.MaxFps = pFramesPerSecond;
		_appliedRenderFps = pFramesPerSecond;
	}

	public override void _Process(double pDelta)
	{
		// Background fetchers only enqueue plain text; all Godot calls stay here.
		string? latestProgress = null;
		while (_rtpProgress.TryDequeue(out var progress)) latestProgress = progress;
		if (latestProgress != null) _status.Text = Tr("RTP_STATUS_WORKING") + " " + latestProgress;
		var running = _launcher.ActiveRuntimeState == PluginRuntimeState.Running;
		// **Und  der  Spielmodus  gilt  fuer  jede  Laufzeit,  die  ein  Bild
		// liefert** -- **und  nicht  nur  fuer  RM2K.**  `MzEngineRuntime`
		// malt  eine  ganze  Karte  (gemessen: LegalTruck 816x624,
		// Camellia 672x864),  und  ein  Lauf,  der  nur  ein  220  Pixel
		// hohes  Vorschaubild  links  unten  bekommen  hat,  ist  kein
		//  spielbares  Fenster.
		var gameMode = running
			&& _launcher.ActiveRuntime is Rm2kEngineRuntime or MzEngineRuntime;
		if (_gameScreen.Visible != gameMode)
		{
			_gameScreen.Visible = gameMode;
			_pageMargin.Visible = !gameMode;
			UpdateWindowTitle(gameMode);
			if (!gameMode)
			{
				_gameScreen.Reset();
			}
		}
		// **Und das Vorschaubild bleibt fuer den Fall, dass die Laufzeit
		// startet, aber kein Bild liefert** -- **das ist bei RM2K der Fall,
		// wenn das Chipset fehlt, und dort ist ein Grund besser als ein
		// leerer Spielschirm.**
		_mzMap.Visible = running && _launcher.ActiveRuntime is MzEngineRuntime && !gameMode;
		_mapPreview.Visible = running && _launcher.ActiveRuntime is Rm2kEngineRuntime && !gameMode;
		if (!running)
		{
			ApplyRenderFrameRate(30);
			_presentationControls.Visible = false;
			_mapPreview.SetFramebuffer(null);
			_mapPreview.SetRenderedMap(null);
			return;
		}
		if (_gameScreen.Visible && _gameScreen.IsPauseOpen)
		{
			// **Und  eine  Pause  haelt  auch  die  Laufzeit  an** -- **denn
			//  ein  Menue,  hinter  dem  die  Welt  weiterlaeuft,  ist  keine
			//  Pause.**
			ApplyRenderFrameRate(30);
			return;
		}
		ApplyRenderFrameRate(60);
		var update = _launcher.Update(pDelta);
		if (!update.Success)
		{
			_status.Text = update.Error?.Message ?? "Runtime update failed.";
			_mapPreview.SetMapData(null);
			_mapPreview.SetFramebuffer(null);
			_mapPreview.SetRenderedMap(null);
			return;
		}
		_status.Text = $"Runtime running: {_launcher.ActiveRuntime?.GetType().Name}";
		_mapPreview.SetMapData(null);
		_mapPreview.SetFramebuffer(null);
		_mapPreview.SetRenderedMap(null);
		// **Und der MZ-Lauf bekommt seine vier Kanaele**, --
		// **und das ist derselbe Ort, an dem die Karte ihre Pixel
		// bekommt**:  ein Frame, ein Host, ein Typ, eine Anzeige.
		if (_launcher.ActiveRuntime is MzEngineRuntime mz)
		{
			_mzAudio.SetzeWurzel(
				mz.GameDirectory,
				UniversalRPG.Mz.MzVerschluesselung.SchluesselDesSpiels());
			_mzAudio.SetzeKanaele(mz.Facts.Screen);
			_mzMap.Grund = mz.PaintReason;
			_mzMap.SetzeKarte(mz.PaintedMap);
			// **Und dieselbe Karte geht in die Vollbildansicht** -- **denn
			// der Spielmodus ist fuer MV und MZ jetzt derselbe wie fuer
			// RM2K, und eine Karte, die nur im Vorschaubild liegt, ist
			// nicht das, was der Spieler sehen soll.**
			//
			// **Und die Diagnose wandert mit**, -- **denn "Map 1 has no
			// drawn tile" ist eine Nachricht, die in der Spielansicht
			// stehen muss und nicht im Detailpanel neben dem Startknopf.**
			// **Und der Bildzaehler ist `Frames`, nicht `SimulationTicks`.**
			//
			// **Und das ist der Grund, warum ein Schritt sich im Fenster
			// nicht zeigte, obwohl er stattfand:** `SetGameState` nutzt den
			// Wert als Teil der Textur-Signatur, und `SimulationTicks`
			// zaehlt die Simulation, nicht die Bilder -- **ein Schritt aendert
			// die Karte, aber keinen Tick, also blieb die Signatur gleich
			// und die Textur wurde nicht neu gebaut.**
			_gameScreen.SetGameState(mz.PaintedMap, mz.Frames, mz.PaintReason);
		}
		else
		{
			_mzAudio.StoppeAlle();
		}

		if (_launcher.ActiveRuntime is Rm2kEngineRuntime rm2k)
		{
			UpdatePresentationControls(rm2k);
			var presentation = rm2k.Presentation;
			if (presentation.MessageVisible)
			{
				_presentationState.Text = $"Message:\n{presentation.MessageText}";
			}
			else if (presentation.ActiveChoice != null)
			{
				_presentationState.Text = $"Choice: {string.Join(" / ", presentation.ActiveChoice.Options)}";
			}
			else if (presentation.PendingInputVariableId != null)
			{
				_presentationState.Text = $"Input variable {presentation.PendingInputVariableId}";
			}
			else
			{
				_presentationState.Text = "Runtime presentation idle";
			}
			_mapPreview.SetMapData(rm2k.CurrentMapData);
			_mapPreview.SetFramebuffer(rm2k.Framebuffer);
			_mapPreview.SetRenderedMap(rm2k.RenderedMap);
			_mapPreview.RenderDiagnostic = rm2k.RenderDiagnostic;
			_mapPreview.SetPlayerPosition(rm2k.Simulation.MapX, rm2k.Simulation.MapY);
			// **Und  dasselbe  Bild  geht  an  die  Vollbildansicht** -- **mit
			//  der  Kamera  des  Spiels  und  nicht  mit  dem  ganzen
			//  Kartenbild.**
			_gameScreen.SetGameState(rm2k.RenderedMap, rm2k.Simulation.FrameCount,
				rm2k.RenderDiagnostic);
			_gameScreen.SetPresentation(
				presentation.MessageVisible, presentation.MessageText,
				presentation.ActiveChoice?.Options, presentation.ActiveChoice?.SelectedIndex ?? -1,
				presentation.PendingInputVariableId != null, presentation.InputValue ?? 0);
			_gameScreen.SetBattle(rm2k.Simulation);

			// **Und  der  Kampf  kommt  in  die  Anzeige** -- **und
			//  die  Anzeige  wird  nur  neu  gezeichnet,  wenn  sich
			//  etwas  geaendert  hat**, -- **denn  ein  Bild  pro
			//  Frame  ist  Arbeit  ohne  Information.**
			_battleView.SetzeZustand(rm2k.Simulation);
			_battleView.Visible = _battleView.LaeuftEinKampf();
			if (_battleView.Visible)
			{
				_battleView.QueueRedraw();
			}
			if (rm2k.CurrentMapData != null && rm2k.CurrentMapData.TryGetValue("width", out var width)
				&& rm2k.CurrentMapData.TryGetValue("height", out var height))
			{
				_presentationState.Text += $"\nMap framebuffer: {width.AsInt32()}x{height.AsInt32()}";
			}
			if (rm2k.RenderedMap != null)
			{
				_presentationState.Text += $"\nRendered pixels: {rm2k.RenderedMap.Width}x{rm2k.RenderedMap.Height}";
			}
		}
	}


	/// <summary>
	/// And the menu panel with the party's own numbers.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>And every number here is the game's</strong>, --
	/// <strong>the hero's name from the database, the hit points from
	/// the state, the gold the game gave, and the four rights that
	/// <c>11960</c> sets</strong>.
	/// </para>
	/// <para>
	/// <strong>And a menu that shows invented numbers teaches the
	/// player nothing about the game</strong>.
	/// </para>
	/// </remarks>
	private void _renderMenuPanel(
		UniversalRPG.Rm2k.Simulation.GameSimulationState pState)
	{
		if (_menuPanel == null)
		{
			// **Und  ein  fehlender  Knoten  ist  ein  Zustand  und
			//  keine  Ausnahme.**
			return;
		}

		var reiter = 0;
		foreach (var zeile in UniversalRPG.Rm2k.Simulation.Rm2kMenueZeile
			.Helden(pState))
		{
			if (reiter >= _menuRows.Count)
			{
				break;
			}

			var knoten = _menuRows[reiter];
			knoten.Visible = true;
			knoten.Text = zeile;
			reiter++;
		}

		for (var i = reiter; i < _menuRows.Count; i++)
		{
			_menuRows[i].Visible = false;
		}

		var zustand = UniversalRPG.Rm2k.Simulation.Rm2kMenueZeile
			.Zustand(pState);
		if (_menuGold != null && zustand.Count > 0)
		{
			_menuGold.Text = zustand[0];
		}

		if (_menuRechte != null && zustand.Count > 1)
		{
			_menuRechte.Text = zustand[1];
		}
	}

	/// <summary>
	/// And the row that names what a strike may be aimed at.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>And every button carries the monster's own name and hit
	/// points</strong>, -- <strong>because a list of numbers is not a
	/// target the player can aim</strong>.
	/// </para>
	/// <para>
	/// <strong>And only the living members appear</strong>, --
	/// <strong>because a fallen monster cannot be struck and offering it
	/// would be a command the game never issued</strong>.
	/// </para>
	/// </remarks>
	private void _renderTargetRow(
		UniversalRPG.Rm2k.Simulation.GameSimulationState pState)
	{
		var ziele = UniversalRPG.Rm2k.Simulation.Rm2kZielwahl
			.MoeglicheZiele(pState);
		_zielButtons.Visible = ziele.Count > 0;
		if (ziele.Count == 0)
		{
			return;
		}

		var namen = new System.Text.StringBuilder();
		foreach (var index in ziele)
		{
			namen.Append(pState.TroopMembers[index]["name"].AsString());
			namen.Append(' ').Append(pState.MonsterHp(index));
			namen.Append('/')
				.Append(pState.MonsterMaxHp(index)).Append(';');
		}

		var signatur = namen.ToString();
		if (signatur != _zielSignature)
		{
			foreach (var child in _zielButtons.GetChildren())
			{
				child.QueueFree();
			}

			_zielSignature = signatur;
			foreach (var index in ziele)
			{
				var knopf = new Button
				{
					Text = pState.TroopMembers[index]["name"].AsString()
						+ "  " + pState.MonsterHp(index) + "/"
						+ pState.MonsterMaxHp(index)
				};
				var zielIndex = index;
				knopf.Pressed += () =>
				{
					if (_launcher.ActiveRuntime
						is Rm2kEngineRuntime lauf)
					{
						lauf.AktuellesZiel.WaehleGegner(lauf.Simulation,
							zielIndex, out var grund);
						_status.Text = grund.Length > 0
							? grund
							: "Ziel: " + pState.TroopMembers[zielIndex]
								["name"].AsString();
					}
				};

				_zielButtons.AddChild(knopf);
			}
		}
	}

	/// <summary>
	/// And the shop, with the bank's prices on the shelves.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>And every shelf is a button that buys one</strong>, --
	/// <strong>because a shop that shows prices and cannot be used is
	/// half a command</strong>.
	/// </para>
	/// <para>
	/// <strong>And the price is the item bank's and not the
	/// command's</strong>, -- <strong>because <c>10720</c> carries no
	/// price at all and the reference reads it from the
	/// item</strong>.
	/// </para>
	/// </remarks>
	private void _renderShopRow(
		UniversalRPG.Rm2k.Simulation.GameSimulationState pState,
		Rm2kEngineRuntime pRuntime)
	{
		_shopPanel.Visible = pState.IsShopOpen;
		if (!pState.IsShopOpen)
		{
			return;
		}

		_shopHead.Text = UniversalRPG.Rm2k.Simulation.Rm2kLadenZeile
			.Kopf(pState);

		var zeilen = UniversalRPG.Rm2k.Simulation.Rm2kLadenZeile
			.Regale(pState, pRuntime.DatabaseData);
		var signatur = string.Join("|", zeilen);
		if (signatur == _shopSignature)
		{
			return;
		}

		_shopSignature = signatur;
		foreach (var child in _shopRows.GetChildren())
		{
			child.QueueFree();
		}

		var itemIds = new int[zeilen.Count];
		var i = 0;
		for (var k = 0; k < pState.ShopItemIds.Count
			&& i < itemIds.Length; k++)
		{
			itemIds[i++] = pState.ShopItemIds[k];
		}

		for (var index = 0; index < zeilen.Count; index++)
		{
			var knopf = new Button { Text = zeilen[index] };
			var itemId = index < itemIds.Length
				? itemIds[index] : 0;
			knopf.Pressed += () =>
			{
				UniversalRPG.Rm2k.Simulation.Rm2kLadenKauf.Kaufe(
					pState, pRuntime.DatabaseData, itemId, 1,
					out var grund);
				_status.Text = grund;
				_shopSignature = "";
			};
			_shopRows.AddChild(knopf);
		}
	}

	private void UpdatePresentationControls(Rm2kEngineRuntime pRuntime)
	{
		var presentation = pRuntime.Presentation;
		_presentationControls.Visible = presentation.MessageVisible
			|| presentation.ActiveChoice != null
			|| presentation.PendingInputVariableId != null;
		_dismissMessageButton.Visible = presentation.MessageVisible;
		_choiceButtons.Visible = presentation.ActiveChoice != null;
		_inputSpinBox.Visible = presentation.PendingInputVariableId != null;
		_submitInputButton.Visible = presentation.PendingInputVariableId != null;

		var choiceSignature = presentation.ActiveChoice == null
			? ""
			: $"{presentation.ActiveChoice.SelectedIndex}:{string.Join("\u001f", presentation.ActiveChoice.Options)}";
		if (choiceSignature != _choiceSignature)
		{
			foreach (var child in _choiceButtons.GetChildren())
			{
				child.QueueFree();
			}
			_choiceSignature = choiceSignature;
			if (presentation.ActiveChoice != null)
			{
				for (var index = 0; index < presentation.ActiveChoice.Options.Count; index++)
				{
					var choiceIndex = index;
					var button = new Button { Text = presentation.ActiveChoice.Options[index] };
					button.ToggleMode = true;
					button.ButtonPressed = presentation.ActiveChoice.SelectedIndex == index;
					button.Pressed += () => presentation.SelectChoice(choiceIndex);
					_choiceButtons.AddChild(button);
				}
			}
		}
		// **Und  das  Menue  wird  sichtbar.**
		//
		// **Und  die  Menüetaste  hat  das  Menue  geoeffnet**, -- **und
		//  das  Menue  war  nirgends  in  dieser  Oberflaeche.**
		//
		// **Und  es  gehoert  zu  `_presentationControls`**, -- **denn  es
		//  ist  genauso  ein  Zustand  des  Spiels  wie  ein  Dialog
		//  und  eine  Auswahl**, -- **und  es  traegt  die  Rechte,  die
		//  `11960`  gesetzt  hat.**
		var simulation = pRuntime.Simulation;
		var menueOffen = simulation.IsMainMenuActive;
		if (menueOffen)
		{
			_renderMenuPanel(simulation);
		}
		if (_menuPanel != null)
		{
			_menuPanel.Visible = menueOffen;
		}

		_presentationControls.Visible =
			_presentationControls.Visible || menueOffen;

		// **Und  die  Kampfbefehle  kommen  daneben.**
		//
		// **Und  dieselbe  Signatur-Logik  wie  bei  der  Auswahl:**
		// **ein  Knopf  entsteht  nur,  wenn  sich  die  Liste
		//  geaendert  hat** -- **denn  ein  Knopf  pro  Bild  ist
		//  Arbeit  ohne  Information.**
		var befehlListe = _launcher.ActiveRuntime
			is Rm2kEngineRuntime rm2kLauf
			&& rm2kLauf.Simulation.IsBattleActive
			? UniversalRPG.Rm2k.Simulation.Rm2kBefehlswahl
				.Verfuegbar(rm2kLauf.Simulation, -1)
			: new System.Collections.Generic.List<
				UniversalRPG.Rm2k.Simulation.Rm2kBefehlswahl
					.Befehl>();

		_battleCommandButtons.Visible = befehlListe.Count > 0;
		_presentationControls.Visible =
			_presentationControls.Visible || befehlListe.Count > 0;

		var befehlSignatur = string.Join(",",
			befehlListe.Select(x => x.ToString()));
		if (befehlSignatur != _battleCommandSignature)
		{
			foreach (var child in _battleCommandButtons.GetChildren())
			{
				child.QueueFree();
			}

			_battleCommandSignature = befehlSignatur;
			foreach (var befehl in befehlListe)
			{
				var gewaehlt = befehl;
				var knopf = new Button { Text = gewaehlt.ToString() };
				knopf.Pressed += () =>
				{
					if (_launcher.ActiveRuntime
						is Rm2kEngineRuntime lauf)
					{
						// **Und  hier  stand  die  Zugfolge  an  der
						//  Zielstelle** -- **und  die  Zugfolge  weiss  nicht,
						//  wer  als  naechstes  dran  ist.**
						//
						// **Und  das  Ziel  steht  im  `AktuellesZiel`.**
						if (!lauf.AktuellesZiel.HatGegner)
						{
							_status.Text = "kein Ziel gewaehlt";
						}
						else if (!lauf.FuehreZugAus(
							gewaehlt, lauf.AktuellesZiel.GegnerIndex,
							out var grund))
						{
							_status.Text = grund;
						}
					}
				};
				_battleCommandButtons.AddChild(knopf);
			}
		}

		_renderTargetRow(simulation);

		_renderShopRow(simulation, pRuntime);

		if (presentation.PendingInputVariableId != null)
		{
			_inputSpinBox.Value = presentation.InputValue ?? 0;
		}
	}

	private void DismissRuntimeMessage()
	{
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime runtime)
		{
			runtime.DrueckeFort();
		}
	}

	private void SubmitRuntimeInput()
	{
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime runtime)
		{
			runtime.Presentation.SetInputValue((int)_inputSpinBox.Value);
		}
	}

	private void StopActiveRuntime()
	{
		var result = _launcher.Stop();
		_status.Text = result.Success ? "Runtime stopped." : result.Error?.Message ?? "Runtime stop failed.";
		_presentationControls.Visible = false;
		// **Und  der  Kampf  gehoert  mit  versteckt** -- **denn  ein
		//  Kampf,  der  nicht  laeuft,  ist  keiner.**
		_battleView.Visible = false;
		_battleView.SetzeZustand(null);
	}

	private async void LaunchSelectedGame()
	{
		if (_selectedGame == null || _launchInProgress || _closing) return;
		var game = _selectedGame;
		var support = _launcher.GetSupport(game);
		if (support.State != RuntimeLauncher.SupportState.Available)
		{
			_status.Text = support.Reason;
			return;
		}
		_launchInProgress = true;
		_launchButton.Disabled = true;
		_engineChoice.Disabled = true;
		try
		{
			var bedarf = RtpPruefer.Pruefe(game.Detection.RtpDependency);
			if (bedarf.Fehlt)
			{
				var gewaehlt = await RtpDialog.Fragen(this, game, bedarf);
				if (_closing || gewaehlt == RtpDialog.Antwort.Abbrechen) return;
				if (gewaehlt == RtpDialog.Antwort.Laden)
				{
					_status.Text = Tr("RTP_STATUS_WORKING");
					var ziel = ProjectSettings.GlobalizePath(RtpAblage.Benutzer(bedarf.EngineId));
					var geholt = await System.Threading.Tasks.Task.Run(() =>
						new RtpAblauf(new RtpHoler()).FuehreAus(true, bedarf.EngineId,
							ziel, teil => _rtpProgress.Enqueue(teil), () => _closing));
					if (_closing) return;
					while (_rtpProgress.TryDequeue(out _)) { }
					_status.Text = geholt.Meldung + "\n" + string.Join("\n", geholt.Schritte);
					if (!geholt.Erfolgreich) return;
				}
			}
			if (_closing) return;
			var result = _launcher.Launch(game);
			_status.Text = result.Message;
			foreach (var diagnostic in result.Diagnostics)
				_status.Text += $"\n[{diagnostic.Severity}/{diagnostic.Code}] {diagnostic.Message}";
		}
		catch (System.Exception exception)
		{
			// async-void UI handlers must surface failures instead of terminating the app.
			if (!_closing) _status.Text = "RTP/launch failed: " + exception.Message;
		}
		finally
		{
			_launchInProgress = false;
			if (!_closing)
			{
				_engineChoice.Disabled = false;
				_launchButton.Disabled = _selectedGame == null
					|| _launcher.GetSupport(_selectedGame).State != RuntimeLauncher.SupportState.Available;
			}
		}
	}

	private void ApplyResponsiveLayout()
	{
		var compact = GetViewportRect().Size.X < 820;
		_body.Vertical = compact;
		_gamesPanel.CustomMinimumSize = new Vector2(compact ? 0 : 390, _gamesPanel.CustomMinimumSize.Y);
		var margin = compact ? 14 : 28;
		SetMargins(_pageMargin, margin);
		_inputMapper.SetTouchViewport(GetViewportRect().Size);
	}

	private void LoadLocale()
	{
		var config = new ConfigFile();
		var locale = "en";
		if (config.Load(GameLibrary.SettingsPath) == Error.Ok)
		{
			locale = config.GetValue("interface", "locale", "en").AsString();
		}
		TranslationServer.SetLocale(locale == "auto" ? OS.GetLocaleLanguage() : locale);
	}

	private string GetSavedLocale()
	{
		var config = new ConfigFile();
		if (config.Load(GameLibrary.SettingsPath) == Error.Ok)
		{
			return config.GetValue("interface", "locale", "en").AsString();
		}
		return "en";
	}

	private int GetLocaleMenuIndex()
	{
		var locale = GetSavedLocale();
		for (var index = 0; index < InterfaceLocales.Length; index++)
		{
			if (InterfaceLocales[index].Locale == locale)
			{
				return index;
			}
		}
		return 0;
	}

	private void ChangeLocale(long pIndex)
	{
		var locale = _languageMenu.GetItemMetadata((int)pIndex).AsString();
		var config = new ConfigFile();
		config.Load(GameLibrary.SettingsPath);
		config.SetValue("interface", "locale", locale);
		config.Save(GameLibrary.SettingsPath);
		TranslationServer.SetLocale(locale == "auto" ? OS.GetLocaleLanguage() : locale);
		GetTree().ReloadCurrentScene();
	}

	private static void SetMargins(MarginContainer pContainer, int pValue)
	{
		foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
		{
			pContainer.AddThemeConstantOverride(side, pValue);
		}
	}

	private static StyleBoxFlat MakeStyleBox(Color pColor, Color pBorder, int pWidth, int pRadius)
	{
		var style = new StyleBoxFlat();
		style.BgColor = pColor;
		style.BorderColor = pBorder;
		style.SetBorderWidthAll(pWidth);
		style.SetCornerRadiusAll(pRadius);
		style.ContentMarginLeft = 10;
		style.ContentMarginTop = 8;
		style.ContentMarginRight = 10;
		style.ContentMarginBottom = 8;
		return style;
	}
}
