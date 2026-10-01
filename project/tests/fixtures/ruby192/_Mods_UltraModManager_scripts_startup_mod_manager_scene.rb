# frozen_string_literal: true
TOP_MENU_HEIGHT = 16 * 2 + 20

class Scene_Base

end

class ModManagerScene < Scene_Base
  attr_reader :mod_list_state_save

  def start
    super
    SceneManager.force_recall self
    @background = Sprite.new(@viewport)
    @background.bitmap = Bitmap.new(Graphics.width, Graphics.height)
    @background.bitmap.fill_rect(@background.bitmap.rect, Color.new(0, 0, 0, 255))
    @background.z -= 1
    @mod_list_state_save = $mod_manager.get_state_save
    @mod_preview = ModPreviewWindow.new(self)
    @mods_list = ModListWindow.new(self)
    @top_menu = TopMenuWindow.new(self)
    @mods_list.viewport = @viewport
    @mod_preview.viewport = @viewport
    @top_menu.viewport = @viewport
    @top_menu.activate
  end

  def update
    super
    if Input.trigger?(:B) || WolfPad.trigger?(:X_LINK) || Input.trigger?(:MX_LINK)
      @mods_list.deactivate if @mods_list.active?
      focus_top
    end
    return unless Mouse.enable?
    if Mouse.within_XYWH?(@mods_list.x, @mods_list.y, @mods_list.width, @mods_list.height) &&
      Input.trigger?(:MZ_LINK) && !@mods_list.active?
      @top_menu.deactivate
      @mods_list.activate
      @mods_list.process_input
    elsif Mouse.within_XYWH?(@top_menu.x, @top_menu.y, @top_menu.width, @top_menu.height) &&
      Input.trigger?(:MZ_LINK) && !@top_menu.active?
      @mods_list.deactivate
      @top_menu.activate
      @top_menu.process_input
    end
  end

  def terminate
    super
    @background.dispose
    Graphics.transition 0
  end

  def focus_list
    @mods_list.activate
  end

  def focus_top
    @top_menu.activate
  end

  def redraw
    @top_menu.refresh
    @mods_list.refresh
    @mod_preview.refresh
  end

  def handle_restart
    SndLib.sys_cancel
    restart
  end

  def restart
    begin
      spawn("Game.exe")
    rescue => e
      msgbox $game_text["umm:manager:top_menu/restart_failed"]
      p e.message + "\n" + e.backtrace.join("\n")
    end
    exit 0
  end

  def handle_accept
    SndLib.sys_cancel
    if $mod_manager["umm"].loaded
      SceneManager.goto(Scene_MapTitle)
    else
      SceneManager.force_recall nil
    end
  end

  def handle_save
    @mod_list_state_save = $mod_manager.get_state_save
    redraw
  end

  def handle_revert
    $mod_manager.load_state_save(@mod_list_state_save)
    redraw
  end

  def preview_mod(mod)
    @mod_preview.set_mod(mod)
  end
end

class Window_Base < Window

end

class TopMenuWindow < Window_Base
  def initialize(scene, x = 0, y = 0, width = Graphics.width, height = TOP_MENU_HEIGHT)
    super(x, y, width, height)
    @scene = scene
    @umm_text = $game_text["umm:manager:top_menu/umm"]
    @umm_text_box = Rect.new
    @scroll_bitmap = Bitmap.new(1, 1)
    @options = []
    @boxes = []
    @restart_opt = add_option($game_text["umm:manager:top_menu/restart"], Color.new(0, 0, 0, 0), false) { @scene.handle_restart }
    @accept_opt = add_option($game_text["umm:manager:top_menu/accept"], normal_color, true) { @scene.handle_accept }
    @save_opt = add_option($game_text["umm:manager:top_menu/save"], normal_color, false) { @scene.handle_save }
    @revert_opt = add_option($game_text["umm:manager:top_menu/revert"], normal_color, false) { @scene.handle_revert }
    @show_on_startup = add_option($game_text["umm:manager:top_menu/show_on_startup_#{$mod_manager["umm", "show_on_startup"]}"], normal_color, true) { handle_show_on_startup }
    add_mod_options
    @selected_item_index = @options.find_index { |x| x[:enabled] }
    calc_boxes
    refresh
  end

  def add_mod_options
    # Here you can hook to add your options
  end

  def handle_show_on_startup
    $mod_manager["umm", "show_on_startup"] = !$mod_manager["umm", "show_on_startup"]
    @show_on_startup[:name] = $game_text["umm:manager:top_menu/show_on_startup_#{$mod_manager["umm", "show_on_startup"]}"]
    calc_boxes
    refresh
  end

  def add_option(text, color, enabled, &proc)
    s = { name: text, on_click: proc, color: color, enabled: enabled }
    @options.push(s)
    s
  end

  def calc_boxes
    contents.font.size = 20
    y = 3
    @umm_text_box = Rect.new(0, y, text_size(@umm_text).width + 16, 20)
    @boxes = []
    x = 0
    @options.each do |opt|
      text = opt[:name]
      w = text_size(text).width.round + 3
      @boxes.push(Rect.new(x, y, w, 20))
      x += 16 + w
    end
    @scroll_bitmap.dispose
    @scroll_bitmap = Bitmap.new(contents.width - @umm_text_box.width, contents.height)
  end

  def refresh(draw_box = active?)
    if $mod_manager.get_state_save != @scene.mod_list_state_save
      if $mod_manager["umm"].loaded
        @restart_opt[:color] = Mod::LOADED_DISABLED_COLOR
        @restart_opt[:enabled] = true
      end
      @save_opt[:enabled] = true
      @revert_opt[:enabled] = true
    else
      @save_opt[:enabled] = false
      @revert_opt[:enabled] = false
      @restart_opt[:color] = Color.new(0, 0, 0, 0)
      @restart_opt[:enabled] = false
    end

    unless @options[@selected_item_index][:enabled]
      draw_box = false
      @selected_item_index = @options.find_index { |x| x[:enabled] }
    end

    contents.clear
    contents.font.size = 20
    @scroll_bitmap.clear
    @scroll_bitmap.font.size = 20
    change_color(normal_color)
    draw_text(@umm_text_box, @umm_text)
    shift = [0, @boxes[@selected_item_index].x + @boxes[@selected_item_index].width + 5 - @scroll_bitmap.width].max
    @options.size.times do |i|
      text = @options[i][:name]
      box = @boxes[i].clone
      box.x -= shift
      c = @options[i][:color].clone
      unless @options[i][:enabled] || @options[i][:color].alpha == 0
        c.alpha = translucent_alpha
      end
      @scroll_bitmap.font.color = c
      @scroll_bitmap.draw_text(box, text)
    end
    contents.blt(@umm_text_box.width, 0, @scroll_bitmap, @scroll_bitmap.rect)
    if draw_box
      b = @boxes[@selected_item_index].clone
      b.y -= 3
      b.x -= 5 + shift - @umm_text_box.width
      b.width += 10
      b.height += 6
      self.cursor_rect = b
    else
      self.cursor_rect = Rect.new(0, 0, 0, 0)
    end
  end

  def process_input
    if Input.trigger?(:DOWN)
      refresh(false)
      deactivate
      @scene.focus_list
      SndLib.play_cursor
    elsif Input.trigger?(:RIGHT)
      start = @selected_item_index
      @selected_item_index = (@selected_item_index + 1) % @options.size
      until @options[@selected_item_index][:enabled] || start == @selected_item_index
        @selected_item_index = (@selected_item_index + 1) % @options.size
      end
      SndLib.play_cursor
      refresh
    elsif Input.trigger?(:LEFT)
      start = @selected_item_index
      @selected_item_index = (@selected_item_index - 1) % @options.size
      until @options[@selected_item_index][:enabled] || start == @selected_item_index
        @selected_item_index = (@selected_item_index - 1) % @options.size
      end
      SndLib.play_cursor
      refresh
    elsif Input.trigger?(:C) || WolfPad.trigger?(:Z_LINK)
      @options[@selected_item_index][:on_click].call
    end
    return unless Mouse.enable?
    return unless Mouse.within_XYWH?(x, y, width, height)
    return unless Input.trigger? :MZ_LINK
    shift = [0, @boxes[@selected_item_index].x + @boxes[@selected_item_index].width + 5 - @scroll_bitmap.width].max
    @boxes.each_with_index do |b, i|
      next unless @options[i][:enabled]
      b = b.clone
      b.y += padding + y
      b.x += padding + x
      b.y -= 3
      b.x -= 5 + shift - @umm_text_box.width
      b.width += 10
      b.height += 6
      if Mouse.within? b
        if i == @selected_item_index
          @options[@selected_item_index][:on_click].call
        else
          @selected_item_index = i
          refresh
        end
      end
    end
  end

  def update
    super
    process_input if active?
  end

  def activate
    super
    refresh
  end

  def deactivate
    super
    self.cursor_rect = Rect.new(0, 0, 0, 0)
  end
end

class Window_Selectable < Window_Base

end

class ModListWindow < Window_Selectable
  def initialize(scene, x = 0, y = TOP_MENU_HEIGHT, w = Graphics.width / 3, h = Graphics.height - TOP_MENU_HEIGHT)
    super(x, y, w, h)
    @scene = scene
    draw_all_items
  end

  def draw_item(index)
    text_rect = item_rect_for_text(index)
    mod = $mod_manager[index]
    contents.font.size = 20
    contents.font.outline = false
    name = mod.localized_name
    change_color(mod.get_color)
    draw_text(text_rect, name)
    change_color(normal_color)
    unless mod.enabled
      rect = item_rect(index)
      draw_icon(717, rect.x + rect.width - 24, rect.y)
    end
  end

  def item_max
    $mod_manager.mods_count
  end

  def update
    super
    process_input if active?
  end

  def process_input
    select_current_mod if Input.trigger?(:C) || WolfPad.trigger?(:Z_LINK)
    return unless Mouse.enable?
    return unless Mouse.within_XYWH?(x, y, width, height)
    cursor_pagedown if Input.trigger?(:R)
    cursor_pageup if Input.trigger?(:L)
    return unless Input.trigger? :MZ_LINK
    item_max.times do |i|
      rect = item_rect(i).clone
      rect.x += padding + x
      rect.y += padding + y
      if Mouse.within?(rect)
        if index == i
          select_current_mod
        else
          select i
          refresh
        end
      end
    end
  end

  def select_current_mod
    mod = $mod_manager[index]
    SndLib.sys_ok
    mod.enabled = !mod.enabled
    $mod_manager.fix_dependencies
    @scene.redraw
  end

  def cursor_left(wrap = nil)
    return if index == 0
    mod = $mod_manager[index]
    $mod_manager.swap(index, index - 1)
    select($mod_manager.load_order.find_index(mod))
    @scene.redraw
  end

  def cursor_right(wrap = nil)
    return if index == item_max - 1
    mod = $mod_manager[index]
    $mod_manager.swap(index, index + 1)
    select($mod_manager.load_order.find_index(mod))
    @scene.redraw
  end

  def cursor_up(wrap = nil)
    if index == 0
      deactivate
      @scene.focus_top
    else
      super
    end
  end

  def select(index)
    @scene.preview_mod($mod_manager[index])
    super
    refresh
  end

  def new_line_x
    0
  end

  def activate
    super
    select(0)
  end

  def deactivate
    super
    unselect
  end
end

class ModPreviewWindow < Window_Base
  def initialize(scene, x = Graphics.width / 3, y = TOP_MENU_HEIGHT, width = Graphics.width * 2 / 3, height = Graphics.height - TOP_MENU_HEIGHT)
    super(x, y, width, height)
    @scene = scene
    @mod_preview = nil
    @mod = nil
  end

  def set_mod(mod)
    @mod_preview.dispose unless @mod_preview.nil?
    @mod_preview = mod.load_thumbnail
    @mod = mod
    refresh
  end

  def refresh
    contents.clear
    contents.font.size = 20
    return if @mod.nil?
    x = 0
    draw_text(0, 0, contents.width, 20, @mod.localized_name)
    x += text_size(@mod.localized_name).width
    @mod.get_labels.each do |text, color|
      change_color(color)
      draw_text(x, 0, contents.width - x, 20, text)
      x += text_size(text).width
    end
    change_color(normal_color)

    contents.font.size = 16
    draw_text(0, 0, contents.width, 20, @mod.version, 2)
    unless @mod_preview.nil?
      src_rect = @mod_preview.rect
      dst_height = 124
      dst_width = src_rect.width * 124.0 / src_rect.height
      if dst_width > contents.width
        ratio = dst_width / contents.width
        dst_height /= ratio
        dst_width /= ratio
      end
      contents.stretch_blt(Rect.new((contents.width - dst_width) / 2, 32 + ((124 - dst_height) / 2), dst_width, dst_height), @mod_preview, @mod_preview.rect)
    end
    s = Font.default_size
    Font.default_size = 20
    # if @mod.failed
    #   change_color(Color.new(255, 0, 0))
    #   draw_text(0, 32 + 124, contents.width, contents.height - 32 - 124, @mod.error)
    #   change_color(normal_color)
    # else
    draw_text_ex(0, 32 + 124, @mod.localized_description)
    # end
    Font.default_size = s
  end

  def new_line_x
    0
  end
end

class GameSystemStub
  def window_tone
    Tone.new
  end
end

class DataSystemStub
  def game_title
    "LonaRPG"
  end

  def version_id
    0xDEADBEEF
  end
end

module SceneManager
  def self.force_recall(scene_class)
    @scene = scene_class
  end
end
$game_system = GameSystemStub.new
$data_system = DataSystemStub.new
