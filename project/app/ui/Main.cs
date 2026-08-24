using System.Collections.Generic;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.Plugins;

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
	private GameLibrary.GameEntry? _selectedGame;

	private MarginContainer _pageMargin = null!;
	private BoxContainer _body = null!;
	private PanelContainer _gamesPanel = null!;
	private ItemList _gameList = null!;
	private Label _folderPath = null!;
	private Label _detailsTitle = null!;
	private Label _detailsEngine = null!;
	private Label _detailsPath = null!;
	private Label _detailsEvidence = null!;
	private Label _runtimeState = null!;
	private Label _presentationState = null!;
	private Rm2kMapPreview _mapPreview = null!;
	private Button _launchButton = null!;
	private Label _status = null!;
	private FileDialog _folderDialog = null!;
	private OptionButton _languageMenu = null!;

	public Main()
	{
		_library = new GameLibrary(pRuntimeRegistry: _pluginRegistry);
		_launcher = new RuntimeLauncher(_pluginRegistry);
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcess(true);
		LoadLocale();
		BuildTheme();
		BuildInterface();
		_library.LoadSettings();
		_folderPath.Text = _library.RootPath;
		GetViewport().SizeChanged += ApplyResponsiveLayout;
		ApplyResponsiveLayout();
		RefreshLibrary();
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
		var details = new VBoxContainer();
		detailsMargin.AddChild(details);
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
		_detailsPath = new Label();
		_detailsPath.AddThemeColorOverride("font_color", ColorMuted.Darkened(0.08f));
		_detailsPath.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		details.AddChild(_detailsPath);
		_mapPreview = new Rm2kMapPreview();
		_mapPreview.CustomMinimumSize = new Vector2(0, 180);
		_mapPreview.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		details.AddChild(_mapPreview);
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
		_launchButton = new Button();
		_launchButton.Text = Tr("ACTION_NOT_PLAYABLE");
		_launchButton.CustomMinimumSize = new Vector2(0, 50);
		_launchButton.Disabled = true;
		_launchButton.AddThemeColorOverride("font_color", new Color("19151a"));
		_launchButton.AddThemeStyleboxOverride("normal", MakeStyleBox(ColorAccent, ColorAccent, 0, 9));
		_launchButton.AddThemeStyleboxOverride("hover", MakeStyleBox(ColorAccent.Lightened(0.08f), Colors.White, 1, 9));
		_launchButton.Pressed += LaunchSelectedGame;
		details.AddChild(_launchButton);

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
		_detailsTitle.Text = _selectedGame.Title;
		_detailsEngine.Text = Tr("DETAIL_ENGINE_CONFIDENCE")
			.Replace("{engine}", detection.GetEngineName())
			.Replace("{confidence}", detection.GetConfidenceString());
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

		var support = _launcher.GetSupport(detection.Engine);
		_runtimeState.Text = Tr("DETAIL_RUNTIME_STATE")
			.Replace("{label}", support.Label)
			.Replace("{reason}", support.Reason);
		_runtimeState.AddThemeColorOverride(
			"font_color",
			support.State == RuntimeLauncher.SupportState.Available ? ColorAccent : ColorMuted
		);
		_launchButton.Disabled = support.State != RuntimeLauncher.SupportState.Available;
		_launchButton.Text = !_launchButton.Disabled ? Tr("ACTION_START_GAME") : Tr("ACTION_NOT_PLAYABLE");
	}

	private void ClearDetails()
	{
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

	public override void _Process(double pDelta)
	{
		if (_launcher.ActiveRuntimeState != PluginRuntimeState.Running)
		{
			return;
		}
		var update = _launcher.Update(pDelta);
		if (!update.Success)
		{
			_status.Text = update.Error?.Message ?? "Runtime update failed.";
			return;
		}
		_status.Text = $"Runtime running: {_launcher.ActiveRuntime?.GetType().Name}";
		if (_launcher.ActiveRuntime is Rm2kEngineRuntime rm2k)
		{
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
			_mapPreview.SetPlayerPosition(rm2k.Simulation.MapX, rm2k.Simulation.MapY);
			if (rm2k.CurrentMapData != null && rm2k.CurrentMapData.TryGetValue("width", out var width)
				&& rm2k.CurrentMapData.TryGetValue("height", out var height))
			{
				_presentationState.Text += $"\nMap framebuffer: {width.AsInt32()}x{height.AsInt32()}";
			}
		}
	}


	private void LaunchSelectedGame()
	{
		if (_selectedGame == null)
		{
			return;
		}
		var result = _launcher.Launch(_selectedGame);
		_status.Text = result.Message;
		foreach (var diagnostic in result.Diagnostics)
		{
			_status.Text += $"\n[{diagnostic.Severity}/{diagnostic.Code}] {diagnostic.Message}";
		}
	}

	private void ApplyResponsiveLayout()
	{
		var compact = GetViewportRect().Size.X < 820;
		_body.Vertical = compact;
		_gamesPanel.CustomMinimumSize = new Vector2(compact ? 0 : 390, _gamesPanel.CustomMinimumSize.Y);
		var margin = compact ? 14 : 28;
		SetMargins(_pageMargin, margin);
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
