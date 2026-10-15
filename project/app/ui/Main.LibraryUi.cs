using System;
using System.Linq;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.App.Ui;

public partial class Main
{
    private LineEdit _librarySearch = null!;
    private OptionButton _librarySort = null!;
    private Label _librarySummary = null!;
    private Label _libraryEmpty = null!;
    private PanelContainer _preferencesPanel = null!;
    private VBoxContainer _technicalDetails = null!;
    private BoxContainer _headerRow = null!;
    private BoxContainer _folderRow = null!;
    private BoxContainer _libraryHeader = null!;
    private PanelContainer _detailsPanel = null!;
    private CheckButton _integerScaleToggle = null!;

    private void BuildLauncherPreferences(VBoxContainer pPage, BoxContainer pHeader)
    {
        var toggle = new Button
        {
            Name = "LauncherPreferencesButton", Text = Tr("LAUNCHER_SETTINGS"),
            ToggleMode = true, CustomMinimumSize = new Vector2(120, 44),
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        pHeader.AddChild(toggle);
        _preferencesPanel = new PanelContainer { Name = "LauncherPreferences", Visible = false };
        pPage.AddChild(_preferencesPanel);
        toggle.Toggled += pOpen => _preferencesPanel.Visible = pOpen;
        var margin = new MarginContainer();
        SetMargins(margin, 16);
        _preferencesPanel.AddChild(margin);
        var flow = new HFlowContainer();
        margin.AddChild(flow);

        VBoxContainer Group(string pKey)
        {
            var column = new VBoxContainer { CustomMinimumSize = new Vector2(180, 0) };
            flow.AddChild(column);
            column.AddChild(new Label { Text = Tr(pKey), AutowrapMode = TextServer.AutowrapMode.WordSmart });
            return column;
        }

        _languageMenu = new OptionButton { TooltipText = Tr("LANGUAGE_TOOLTIP") };
        foreach (var locale in InterfaceLocales)
        {
            _languageMenu.AddItem(locale.Locale == "auto" ? Tr(locale.Label) : locale.Label);
            _languageMenu.SetItemMetadata(_languageMenu.ItemCount - 1, locale.Locale);
        }
        _languageMenu.Select(GetLocaleMenuIndex());
        _languageMenu.ItemSelected += ChangeLocale;
        Group("LAUNCHER_LANGUAGE").AddChild(_languageMenu);

        var mode = new OptionButton { TooltipText = Tr("LAUNCHER_RENDER_MODE") };
        mode.AddItem(Tr("LAUNCHER_FAITHFUL"));
        mode.AddItem(Tr("LAUNCHER_ENHANCED"));
        Group("LAUNCHER_RENDER_MODE").AddChild(mode);
        var integerScale = _integerScaleToggle = new CheckButton
        {
            TooltipText = Tr("LAUNCHER_INTEGER_SCALE"), ButtonPressed = _renderProfile.IntegerScaling,
        };
        integerScale.Toggled += SetLauncherIntegerScaling;
        mode.ItemSelected += pIndex =>
        {
            _renderProfile.TrySetMode(pIndex == 0 ? RenderCompatibilityMode.Faithful : RenderCompatibilityMode.Enhanced);
            SynchronizeLauncherScaling();
        };
        Group("LAUNCHER_INTEGER_SCALE").AddChild(integerScale);
        // The game view fits its source to the window; a manual factor was
        // previously editable here but was never consumed by the game view.
        Group("LAUNCHER_SCALE_FACTOR").AddChild(new Label { Text = Tr("LAUNCHER_SCALE_AUTO") });
    }

    private void SetLauncherIntegerScaling(bool pEnabled)
    {
        _renderProfile.TrySetIntegerScaling(pEnabled);
        SynchronizeLauncherScaling();
    }

    private void SynchronizeLauncherScaling()
    {
        _integerScaleToggle.SetPressedNoSignal(_renderProfile.IntegerScaling);
        _gameScreen.SetIntegerScale(_renderProfile.IntegerScaling);
    }

    private void BuildLibrarySearch(VBoxContainer pColumn)
    {
        _librarySearch = new LineEdit
        {
            Name = "LibrarySearch", PlaceholderText = Tr("LAUNCHER_SEARCH"),
            TooltipText = Tr("LAUNCHER_SEARCH"), ClearButtonEnabled = true,
            CustomMinimumSize = new Vector2(0, 44),
        };
        pColumn.AddChild(_librarySearch);
        _librarySearch.TextChanged += _ => RefreshLibraryView();
        _librarySearch.TextSubmitted += _ =>
        {
            if (_gameList.ItemCount > 0 && _scanOperation == null) _gameList.GrabFocus();
        };
        _librarySummary = new Label { Name = "LibrarySummary" };
        _librarySummary.AddThemeColorOverride("font_color", ColorMuted);
        pColumn.AddChild(_librarySummary);
        _libraryEmpty = new Label
        {
            Name = "LibraryEmpty", Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pColumn.AddChild(_libraryEmpty);
    }

    private void RefreshLibraryView()
    {
        // Search/sort are projections of the current snapshot, never a disk rescan.
        if (_scanOperation == null && !_launchInProgress && !_closing) DisplayLibrary();
    }

    private void DisplayLibraryProjection()
    {
        var selectedPath = _selectedGame?.Path;
        var query = _librarySearch.Text.Trim();
        var games = _library.Games.Select((entry, index) => (Entry: entry, Index: index))
            .Where(p => query.Length == 0 || p.Entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || p.Entry.Detection.GetEngineName().Contains(query, StringComparison.OrdinalIgnoreCase)
                || p.Entry.Path.Contains(query, StringComparison.OrdinalIgnoreCase));
        var visible = (_sortMode == GameSortMode.Engine
                ? games.OrderBy(p => p.Entry.Detection.GetEngineName(), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.Entry.Title, StringComparer.OrdinalIgnoreCase)
                : games.OrderBy(p => p.Entry.Title, StringComparer.OrdinalIgnoreCase))
            .ThenBy(p => p.Entry.Path, StringComparer.OrdinalIgnoreCase).ToList();

        _gameList.Clear();
        foreach (var (entry, index) in visible)
        {
            var row = _gameList.AddItem($"{entry.Title}  |  {entry.Detection.GetEngineName()}");
            _gameList.SetItemMetadata(row, index);
            _gameList.SetItemTooltip(row, entry.Path);
        }
        _librarySummary.Text = Tr("LAUNCHER_RESULTS").Replace("{shown}", visible.Count.ToString())
            .Replace("{total}", _library.Games.Count.ToString());
        _gameList.Visible = visible.Count > 0;
        _libraryEmpty.Visible = visible.Count == 0;
        _libraryEmpty.Text = Tr(_library.Games.Count == 0 ? "LAUNCHER_EMPTY_BODY" : "LAUNCHER_NO_MATCHES_BODY");
        if (visible.Count == 0)
        {
            ClearDetails();
            _detailsTitle.Text = Tr(_library.Games.Count == 0 ? "LAUNCHER_EMPTY_TITLE" : "LAUNCHER_NO_MATCHES");
            _status.Text = _library.Games.Count == 0 ? Tr("STATUS_NO_GAMES") : Tr("LAUNCHER_NO_MATCHES");
            return;
        }
        var selectedRow = visible.FindIndex(p => p.Entry.Path == selectedPath);
        if (selectedRow < 0) selectedRow = 0;
        _gameList.Select(selectedRow);
        SelectVisibleGame(selectedRow);
        _status.Text = TrN("STATUS_ONE_GAME", "STATUS_MANY_GAMES", _library.Games.Count)
            .Replace("{count}", _library.Games.Count.ToString());
    }

    public override void _ShortcutInput(InputEvent pEvent)
    {
        if (!_gameScreen.Visible && _scanOperation == null && !_launchInProgress
            && pEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F } key
            && (key.CtrlPressed || key.MetaPressed))
        {
            _librarySearch.GrabFocus();
            _librarySearch.SelectAll();
            GetViewport().SetInputAsHandled();
        }
    }
}
