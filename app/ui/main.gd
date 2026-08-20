extends Control

const GameLibraryScript = preload("res://app/library/game_library.gd")
const RuntimeLauncherScript = preload("res://app/launcher/runtime_launcher.gd")
const InterfaceFont = preload("res://assets/fonts/NotoSansCJKsc-Regular.otf")

const INTERFACE_LOCALES := [
	["en", "English"], # NO_TRANSLATE: Native language name.
	["auto", "LANGUAGE_SYSTEM"],
	["de", "Deutsch"], # NO_TRANSLATE: Native language name.
	["es", "Español"], # NO_TRANSLATE: Native language name.
	["fr", "Français"], # NO_TRANSLATE: Native language name.
	["ja", "日本語"], # NO_TRANSLATE: Native language name.
	["ko", "한국어"], # NO_TRANSLATE: Native language name.
	["zh_CN", "简体中文"], # NO_TRANSLATE: Native language name.
]

const COLOR_BACKGROUND := Color("101015")
const COLOR_PANEL := Color("1a1a23")
const COLOR_PANEL_LIGHT := Color("232330")
const COLOR_TEXT := Color("f2efe7")
const COLOR_MUTED := Color("aaa7b5")
const COLOR_ACCENT := Color("e8a24a")
const COLOR_BORDER := Color("343443")

var _library = GameLibraryScript.new()
var _launcher = RuntimeLauncherScript.new()
var _selected_game

var _page_margin: MarginContainer
var _body: BoxContainer
var _games_panel: PanelContainer
var _game_list: ItemList
var _folder_path: Label
var _details_title: Label
var _details_engine: Label
var _details_path: Label
var _details_evidence: Label
var _runtime_state: Label
var _launch_button: Button
var _status: Label
var _folder_dialog: FileDialog
var _language_menu: OptionButton


func _ready() -> void:
	_load_locale()
	_build_theme()
	_build_interface()
	_library.load_settings()
	_folder_path.text = _library.root_path
	get_viewport().size_changed.connect(_apply_responsive_layout)
	_apply_responsive_layout()
	_refresh_library()


func _build_theme() -> void:
	var app_theme := Theme.new()
	app_theme.default_font = InterfaceFont
	app_theme.set_default_font_size(17)
	app_theme.set_color("font_color", "Label", COLOR_TEXT)
	app_theme.set_color("font_color", "Button", COLOR_TEXT)
	app_theme.set_color("font_hover_color", "Button", Color.WHITE)
	app_theme.set_color("font_disabled_color", "Button", COLOR_MUTED.darkened(0.25))
	app_theme.set_color("font_color", "ItemList", COLOR_TEXT)
	app_theme.set_color("font_selected_color", "ItemList", Color("15131a"))
	app_theme.set_font_size("font_size", "Button", 16)
	app_theme.set_font_size("font_size", "ItemList", 17)
	app_theme.set_constant("separation", "VBoxContainer", 12)
	app_theme.set_constant("separation", "HBoxContainer", 10)
	app_theme.set_constant("separation", "BoxContainer", 18)
	app_theme.set_stylebox("panel", "PanelContainer", _style_box(COLOR_PANEL, COLOR_BORDER, 1, 12))
	app_theme.set_stylebox("normal", "Button", _style_box(COLOR_PANEL_LIGHT, COLOR_BORDER, 1, 9))
	app_theme.set_stylebox("hover", "Button", _style_box(Color("30303e"), COLOR_ACCENT, 1, 9))
	app_theme.set_stylebox("pressed", "Button", _style_box(Color("15151d"), COLOR_ACCENT, 1, 9))
	app_theme.set_stylebox("disabled", "Button", _style_box(Color("17171e"), COLOR_BORDER, 1, 9))
	app_theme.set_stylebox("panel", "ItemList", _style_box(COLOR_PANEL_LIGHT, COLOR_BORDER, 1, 9))
	app_theme.set_stylebox("selected", "ItemList", _style_box(COLOR_ACCENT, COLOR_ACCENT, 0, 6))
	app_theme.set_stylebox("selected_focus", "ItemList", _style_box(COLOR_ACCENT, Color.WHITE, 1, 6))
	theme = app_theme


func _build_interface() -> void:
	var background := ColorRect.new()
	background.color = COLOR_BACKGROUND
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(background)

	_page_margin = MarginContainer.new()
	_page_margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(_page_margin)

	var page := VBoxContainer.new()
	_page_margin.add_child(page)

	var header := HBoxContainer.new()
	page.add_child(header)

	var brand := VBoxContainer.new()
	brand.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(brand)

	var title := Label.new()
	title.text = "UNIVERSALRPG"
	title.add_theme_font_size_override("font_size", 32)
	title.add_theme_color_override("font_color", COLOR_ACCENT)
	brand.add_child(title)

	var subtitle := Label.new()
	subtitle.text = tr("APP_SUBTITLE")
	subtitle.add_theme_color_override("font_color", COLOR_MUTED)
	subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	brand.add_child(subtitle)

	_language_menu = OptionButton.new()
	_language_menu.tooltip_text = tr("LANGUAGE_TOOLTIP")
	for locale_data in INTERFACE_LOCALES:
		var label: String = tr(locale_data[1]) if locale_data[0] == "auto" else locale_data[1]
		_language_menu.add_item(label)
		_language_menu.set_item_metadata(_language_menu.item_count - 1, locale_data[0])
	_language_menu.select(_get_locale_menu_index())
	_language_menu.item_selected.connect(_change_locale)
	header.add_child(_language_menu)

	var folder_panel := PanelContainer.new()
	page.add_child(folder_panel)
	var folder_margin := MarginContainer.new()
	_set_margins(folder_margin, 16)
	folder_panel.add_child(folder_margin)
	var folder_row := HBoxContainer.new()
	folder_margin.add_child(folder_row)
	var folder_text := VBoxContainer.new()
	folder_text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	folder_row.add_child(folder_text)
	var folder_caption := Label.new()
	folder_caption.text = tr("LIBRARY_FOLDER")
	folder_caption.add_theme_font_size_override("font_size", 13)
	folder_caption.add_theme_color_override("font_color", COLOR_ACCENT)
	folder_text.add_child(folder_caption)
	_folder_path = Label.new()
	_folder_path.add_theme_color_override("font_color", COLOR_MUTED)
	_folder_path.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	_folder_path.tooltip_text = tr("LIBRARY_SCAN_HINT")
	folder_text.add_child(_folder_path)
	var choose_button := Button.new()
	choose_button.text = tr("ACTION_CHOOSE_FOLDER")
	choose_button.custom_minimum_size = Vector2(170, 46)
	choose_button.pressed.connect(_choose_folder)
	folder_row.add_child(choose_button)
	var refresh_button := Button.new()
	refresh_button.text = tr("ACTION_RESCAN")
	refresh_button.custom_minimum_size = Vector2(130, 46)
	refresh_button.pressed.connect(_refresh_library)
	folder_row.add_child(refresh_button)

	_body = BoxContainer.new()
	_body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	page.add_child(_body)

	_games_panel = PanelContainer.new()
	_games_panel.custom_minimum_size = Vector2(390, 260)
	_games_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_games_panel.size_flags_stretch_ratio = 0.8
	_body.add_child(_games_panel)
	var games_margin := MarginContainer.new()
	_set_margins(games_margin, 16)
	_games_panel.add_child(games_margin)
	var games_column := VBoxContainer.new()
	games_margin.add_child(games_column)
	var games_heading := Label.new()
	games_heading.text = tr("LIBRARY_FOUND_GAMES")
	games_heading.add_theme_font_size_override("font_size", 14)
	games_heading.add_theme_color_override("font_color", COLOR_ACCENT)
	games_column.add_child(games_heading)
	_game_list = ItemList.new()
	_game_list.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_game_list.allow_reselect = true
	_game_list.item_selected.connect(_select_game)
	_game_list.item_activated.connect(_select_game)
	games_column.add_child(_game_list)

	var details_panel := PanelContainer.new()
	details_panel.custom_minimum_size = Vector2(430, 260)
	details_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	details_panel.size_flags_stretch_ratio = 1.2
	_body.add_child(details_panel)
	var details_margin := MarginContainer.new()
	_set_margins(details_margin, 22)
	details_panel.add_child(details_margin)
	var details := VBoxContainer.new()
	details_margin.add_child(details)
	var details_caption := Label.new()
	details_caption.text = tr("LIBRARY_SELECTION")
	details_caption.add_theme_font_size_override("font_size", 14)
	details_caption.add_theme_color_override("font_color", COLOR_ACCENT)
	details.add_child(details_caption)
	_details_title = Label.new()
	_details_title.text = tr("DETAIL_NO_SELECTION")
	_details_title.add_theme_font_size_override("font_size", 28)
	_details_title.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	details.add_child(_details_title)
	_details_engine = Label.new()
	_details_engine.add_theme_color_override("font_color", COLOR_MUTED)
	details.add_child(_details_engine)
	_details_path = Label.new()
	_details_path.add_theme_color_override("font_color", COLOR_MUTED.darkened(0.08))
	_details_path.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	details.add_child(_details_path)
	_details_evidence = Label.new()
	_details_evidence.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_details_evidence.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	details.add_child(_details_evidence)
	_runtime_state = Label.new()
	_runtime_state.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	details.add_child(_runtime_state)
	_launch_button = Button.new()
	_launch_button.text = tr("ACTION_NOT_PLAYABLE")
	_launch_button.custom_minimum_size = Vector2(0, 50)
	_launch_button.disabled = true
	_launch_button.add_theme_color_override("font_color", Color("19151a"))
	_launch_button.add_theme_stylebox_override("normal", _style_box(COLOR_ACCENT, COLOR_ACCENT, 0, 9))
	_launch_button.add_theme_stylebox_override("hover", _style_box(COLOR_ACCENT.lightened(0.08), Color.WHITE, 1, 9))
	_launch_button.pressed.connect(_launch_selected_game)
	details.add_child(_launch_button)

	_status = Label.new()
	_status.add_theme_color_override("font_color", COLOR_MUTED)
	_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	page.add_child(_status)

	_folder_dialog = FileDialog.new()
	_folder_dialog.title = tr("DIALOG_CHOOSE_FOLDER")
	_folder_dialog.file_mode = FileDialog.FILE_MODE_OPEN_DIR
	_folder_dialog.access = FileDialog.ACCESS_FILESYSTEM
	_folder_dialog.use_native_dialog = true
	_folder_dialog.dir_selected.connect(_set_folder)
	add_child(_folder_dialog)


func _refresh_library() -> void:
	_status.text = tr("STATUS_SCANNING")
	_game_list.clear()
	_selected_game = null
	var games := _library.scan()
	for game in games:
		var index := _game_list.add_item("%s  |  %s" % [game.title, game.detection.get_engine_name()])
		_game_list.set_item_metadata(index, game)
		_game_list.set_item_tooltip(index, game.path)
	if games.is_empty():
		_clear_details()
		_status.text = tr("STATUS_NO_GAMES")
	else:
		_game_list.select(0)
		_select_game(0)
		_status.text = tr_n("STATUS_ONE_GAME", "STATUS_MANY_GAMES", games.size()).format({"count": games.size()})


func _select_game(p_index: int) -> void:
	_selected_game = _game_list.get_item_metadata(p_index)
	var detection = _selected_game.detection
	_details_title.text = _selected_game.title
	_details_engine.text = tr("DETAIL_ENGINE_CONFIDENCE").format({
		"engine": detection.get_engine_name(),
		"confidence": detection.get_confidence_string(),
	})
	_details_path.text = _selected_game.path
	var facts: Array[String] = []
	for item in detection.evidence:
		facts.append("- " + item)
	if detection.has_native_libraries:
		facts.append("- " + tr("DETAIL_NATIVE_LIBRARIES"))
	if not detection.rtp_dependency.is_empty():
		facts.append("- " + tr("DETAIL_RTP").format({"rtp": detection.rtp_dependency}))
	_details_evidence.text = "\n".join(facts)

	var support := _launcher.get_support(detection.engine)
	_runtime_state.text = tr("DETAIL_RUNTIME_STATE").format({"label": support.label, "reason": support.reason})
	_runtime_state.add_theme_color_override(
		"font_color",
		COLOR_ACCENT if support.state == RuntimeLauncherScript.SupportState.AVAILABLE else COLOR_MUTED
	)
	_launch_button.disabled = support.state != RuntimeLauncherScript.SupportState.AVAILABLE
	_launch_button.text = tr("ACTION_START_GAME") if not _launch_button.disabled else tr("ACTION_NOT_PLAYABLE")


func _clear_details() -> void:
	_details_title.text = tr("DETAIL_NO_GAMES")
	_details_engine.text = ""
	_details_path.text = _library.root_path
	_details_evidence.text = tr("DETAIL_DETECTION_SUPPORT")
	_runtime_state.text = tr("DETAIL_RUNTIME_DEVELOPMENT")
	_launch_button.disabled = true
	_launch_button.text = tr("ACTION_NOT_PLAYABLE")


func _choose_folder() -> void:
	_folder_dialog.current_dir = _library.root_path
	_folder_dialog.popup_centered_ratio(0.82)


func _set_folder(p_path: String) -> void:
	var error := _library.set_root_path(p_path)
	if error != OK:
		_status.text = tr("ERROR_FOLDER").format({"error": error})
		return
	_folder_path.text = _library.root_path
	_refresh_library()


func _launch_selected_game() -> void:
	if _selected_game == null:
		return
	var result := _launcher.launch(_selected_game)
	_status.text = result.message


func _apply_responsive_layout() -> void:
	var compact := get_viewport_rect().size.x < 820
	_body.vertical = compact
	_games_panel.custom_minimum_size.x = 0 if compact else 390
	var margin := 14 if compact else 28
	_set_margins(_page_margin, margin)


func _load_locale() -> void:
	var config := ConfigFile.new()
	var locale := "en"
	if config.load(GameLibraryScript.SETTINGS_PATH) == OK:
		locale = str(config.get_value("interface", "locale", "en"))
	TranslationServer.set_locale(OS.get_locale_language() if locale == "auto" else locale)


func _get_saved_locale() -> String:
	var config := ConfigFile.new()
	if config.load(GameLibraryScript.SETTINGS_PATH) == OK:
		return str(config.get_value("interface", "locale", "en"))
	return "en"


func _get_locale_menu_index() -> int:
	var locale := _get_saved_locale()
	for index in range(INTERFACE_LOCALES.size()):
		if INTERFACE_LOCALES[index][0] == locale:
			return index
	return 0


func _change_locale(p_index: int) -> void:
	var locale: String = _language_menu.get_item_metadata(p_index)
	var config := ConfigFile.new()
	config.load(GameLibraryScript.SETTINGS_PATH)
	config.set_value("interface", "locale", locale)
	config.save(GameLibraryScript.SETTINGS_PATH)
	TranslationServer.set_locale(OS.get_locale_language() if locale == "auto" else locale)
	get_tree().reload_current_scene()


func _set_margins(p_container: MarginContainer, p_value: int) -> void:
	for side in ["margin_left", "margin_top", "margin_right", "margin_bottom"]:
		p_container.add_theme_constant_override(side, p_value)


func _style_box(p_color: Color, p_border: Color, p_width: int, p_radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = p_color
	style.border_color = p_border
	style.set_border_width_all(p_width)
	style.set_corner_radius_all(p_radius)
	style.content_margin_left = 10
	style.content_margin_top = 8
	style.content_margin_right = 10
	style.content_margin_bottom = 8
	return style
