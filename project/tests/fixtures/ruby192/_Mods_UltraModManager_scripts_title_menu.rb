# frozen_string_literal: true

# =============== Title Menu re-implementation ===============
class TitleMenu < Sprite

  def initialize
    super(nil)
    create_foreground
    create_background
    tmp_w = 320
    tmp_h = Graphics.height
    self.bitmap = Bitmap.new(tmp_w, tmp_h)
    self.x = 0
    self.y = 57
    self.bitmap.font.name = System_Settings::MESSAGE_WINDOW_FONT_NAME
    self.bitmap.font.outline = false
    self.bitmap.font.bold = true
    self.z = System_Settings::TITLE_COMMAND_WINDOW_Z
    self.bitmap.font.size = 24
    @entries = []
    @mouse_all_rects = []

    build_menu_entries
    @index = SceneManager.prevOptChoose || (DataManager.SaveFileExistsRGSS? ? find_entry(:continue) : find_entry(:new_game))
    draw_items
  end

  def build_menu_entries
    add_entry_at(:new_game, $game_text["menu:title/NEW_GAME"]) { handle_new_game }
    add_entry_at(:continue, $game_text["menu:title/CONTINUE"]) { handle_continue }
    add_entry_at(:load_doom, $game_text["menu:system/DoomCoreMode_Load0"]) { handle_load_doom } if DataManager.SaveFileExistsDOOM?
    add_entry_at(:load_auto, $game_text["menu:system/autosave"]) { handle_load_auto } if DataManager.SaveFileExistsAUTO?
    add_entry_at(:options, $game_text["menu:title/OPTIONS"]) { handle_options }
    add_entry_at(:achievement_list, $game_text["menu:title/ACH"]) { handle_achievement_list }
    add_entry_at(:credits, $game_text["menu:title/CREDITS"]) { handle_credit }
    add_entry_at(:exit, $game_text["menu:title/EXIT_GAME"]) { handle_exit }
    build_menu_entries_mod_hook
  end

  def build_menu_entries_mod_hook
  end

  def add_entry_at(label, text, pos = -1, &handler)
    add_entry_at_p(label, text, pos, handler)
  end

  def add_entry_after(label_before, label, text, &handler)
    before_index = find_entry(label_before) || -2
    add_entry_at_p(label, text, before_index + 1, handler)
  end

  def find_entry(label)
    @entries.each_with_index { |el, i| break i if el[:label] == label }
  end

  def add_entry_at_p(label, text, pos, handler)
    if pos.nil?
      @entries.append({ label: label, text: text, on_click: handler })
      @mouse_all_rects.append Rect.new(0, 0, 0, 0)
    else
      @entries.insert(pos, { label: label, text: text, on_click: handler })
      @mouse_all_rects.insert(pos, Rect.new(0, 0, 0, 0))
    end
  end

  def handle_new_game
    $titleCreateActorReq = true
    if $TEST
      $game_map.setup($data_system.start_map_id)
      $game_player.moveto($data_system.start_x, $data_system.start_y)
      $game_map.interpreter.new_game_setup ##29_Functions_417
      $hudForceHide = false
      $balloonForceHide = false
    else
      $game_map.setup($data_tag_maps["TutorialOP"].sample)
      $game_player.moveto(0, 0)
      $hudForceHide = true
      $balloonForceHide = true
    end
    SceneManager.scene.fadeout_all
    SceneManager.prevOptChooseSet(nil)
    SceneManager.prevTitleOptChooseSet(nil)
    $game_map.interpreter.change_map_weather_cleaner
    $game_player.force_update = true
    $game_system.menu_disabled = false
    SceneManager.goto(Scene_Map)
  end

  def handle_continue
    $game_map.interpreter.chcg_background_color_off
    SceneManager.goto(Scene_Load)
  end

  def handle_load_doom
    $game_map.interpreter.chcg_background_color_off
    SceneManager.scene.gotoLoadCustomScene("SavDoomMode")
  end

  def handle_load_auto
    $game_map.interpreter.chcg_background_color_off
    SceneManager.scene.gotoLoadCustomScene("SavAuto")
  end

  def handle_options
    SceneManager.goto(Scene_TitleOptions)
  end

  def handle_credit
    $titleCreateActorReq = true
    $game_player.force_update = true
    $game_map.interpreter.chcg_background_color_off
    SceneManager.scene.fadeout_all
    SceneManager.goto(Scene_Credits)
  end

  def handle_achievement_list
    SndLib.openChest
    SceneManager.goto(Scene_ACHlistMenu)
  end

  def handle_exit
    SndLib.ppl_Boo
    SceneManager.scene.fadeout_all
    SceneManager.exit
  end

  def update
    return SndLib.sys_buzzer if Input.trigger?(:B) || WolfPad.trigger?(:X_LINK)
    @foreground_sprite_joy.opacity = WolfPad.plugged_in? ? 255 : 0
    refresh_index(@index + 1) if Input.trigger?(:DOWN)
    refresh_index(@index - 1) if Input.trigger?(:UP)
    runCommand if Input.trigger?(:C) || WolfPad.trigger?(:Z_LINK)
    mouse_input_check
  end

  def dispose_foreground
    @foreground_sprite.dispose
    @foreground_sprite_joy.dispose
  end

  def dispose_background
    @sprite1.bitmap.dispose
    @sprite1.dispose
    @sprite2.bitmap.dispose
    @sprite2.dispose
  end

  def dispose
    SceneManager.snapshot_for_background
    dispose_background
    dispose_foreground
    self.bitmap.dispose
    @warning.dispose if @warning
    super
  end

  def refresh_index(i)
    SndLib.play_cursor
    old_index = @index
    @index = i % @entries.size
    SceneManager.prevOptChooseSet(@index)
    clear_item(old_index)
    draw_item(old_index)
    clear_item(@index)
    draw_item(@index)
  end

  def create_background
    @sprite1 = Sprite.new
    @sprite2 = Sprite.new
    @sprite1.bitmap = Cache.title1($data_system.title1_name)
    @sprite2.bitmap = Cache.title2($data_system.title2_name)
    @sprite1.z = System_Settings::TITLE_BACKGROUND1
    @sprite2.z = System_Settings::TITLE_BACKGROUND2
    center_sprite(@sprite1)
    center_sprite(@sprite2)
  end

  def center_sprite(sprite)
    sprite.ox = sprite.bitmap.width / 2
    sprite.oy = sprite.bitmap.height / 2
    sprite.x = Graphics.width / 2
    sprite.y = Graphics.height / 2
  end

  def create_foreground
    @foreground_sprite = Sprite.new
    @foreground_sprite_joy = Sprite.new
    @foreground_sprite.bitmap = Bitmap.new(Graphics.width, Graphics.height)
    @foreground_sprite_joy.bitmap = Bitmap.new(Graphics.width, Graphics.height)
    @foreground_sprite.z = System_Settings::TITLE_FOREGROUND_Z
    @foreground_sprite_joy.z = System_Settings::TITLE_FOREGROUND_Z
    draw_game_title # if $data_system.opt_draw_title
    draw_joystick_mode
  end

  $GameINI = IniFile.load("Game.ini")

  def draw_game_title
    version_id_file = $GameINI["Game"]["Title"]
    version_id_base = "#{$data_system.game_title}"
    @foreground_sprite.bitmap.font.size = 14
    @foreground_sprite.bitmap.font.outline = false
    rect = Rect.new(0, 330, Graphics.width - 10, 40)
    @foreground_sprite.bitmap.draw_text(rect, "#{version_id_file || version_id_base}", 2)
  end

  def draw_joystick_mode
    @foreground_sprite_joy.bitmap.font.size = 14
    @foreground_sprite_joy.bitmap.font.outline = false
    rect = Rect.new(0, 315, Graphics.width - 10, 40)
    @foreground_sprite_joy.bitmap.draw_text(rect, $game_text["menu:title/GamepadMode"], 2)
    @foreground_sprite_joy.opacity = 0
  end

  def draw_items
    @entries.length.times { |i| draw_item(i) }
  end

  def draw_item(i)
    c = (i == @index ? 255 : 192)
    activeX = (i == @index ? 5 : 0)
    bitmap.font.color.set(c, c, c)
    bitmap.draw_text(58 + activeX, 70 + i * 20, 320, 20, @entries[i][:text], 0)
    @mouse_all_rects[i] = Rect.new(58 + activeX, 70 + i * 20, bitmap.text_size(@entries[i][:text]).width, 20)
  end

  def clear_item(i)
    self.bitmap.clear_rect(0, 70 + i * 20, 320, 20)
  end

  def runCommand
    SndLib.openChest
    @entries[@index][:on_click].call
  end

  def mouse_input_check
    return unless Mouse.enable?
    return Mouse.ForceIdle if Input.MouseWheelForceIdle?
    return SndLib.sys_buzzer if Input.trigger?(:MX_LINK)
    return unless Input.trigger?(:MZ_LINK)
    @mouse_all_rects.each_with_index do |rect, i|
      rect = rect.clone
      rect.y += y
      rect.x += x
      next unless Mouse.within?(rect)
      if i != @index
        refresh_index(i)
        SndLib.sys_buzzer
      else
        runCommand
      end
    end
  end
end

# =========== Actual UMM changes =============

class TitleMenu
  alias_method :build_menu_entries_UMM, :build_menu_entries

  def build_menu_entries
    build_menu_entries_UMM
    add_entry_after(:options, :mods_manager, $game_text["umm:umm:title/UMM"]) { handle_mods_manager }
  end

  def handle_mods_manager
    SceneManager.call(ModManagerScene)
  end

  alias_method :create_foreground_UMM, :create_foreground

  def create_foreground
    create_foreground_UMM
    @foreground_sprite_umm = Sprite.new
    @foreground_sprite_umm.bitmap = Bitmap.new(Graphics.width, Graphics.height)
    @foreground_sprite_umm.z = System_Settings::TITLE_FOREGROUND_Z
    @foreground_sprite_umm.bitmap.font.size = 14
    @foreground_sprite_umm.bitmap.font.outline = false
    rect = Rect.new(0, 300, Graphics.width - 10, 40)
    loaded = ($mod_manager.load_order.map { |mod| mod.loaded ? 1 : 0 }).sum
    enabled = ($mod_manager.load_order.map { |mod| mod.enabled ? 1 : 0 }).sum
    found = $mod_manager.load_order.size
    @foreground_sprite_umm.bitmap.draw_text(rect, $game_text["umm:umm:title/status"] % [loaded, enabled, found], 2)
  end

  alias_method :dispose_foreground_UMM, :dispose_foreground

  def dispose_foreground
    dispose_foreground_UMM
    @foreground_sprite_umm.dispose
  end
end