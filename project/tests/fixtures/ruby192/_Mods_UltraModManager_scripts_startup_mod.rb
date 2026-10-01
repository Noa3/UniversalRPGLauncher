# frozen_string_literal: true

class Mod
  attr_accessor :id, :version, :name, :description, :entry, :texts, :requires, :before, :thumbnail, :path, :error,
                :enabled, :loaded, :type, :preload, :settings

  def initialize(path)
    basename = File.basename(path)
    if File.exist? "#{path}/mod.json"
      @type = "normal"
      json_desc = JSON.decode(File.read("#{path}/mod.json"))
      @id = json_desc["id"] || basename
      @version = json_desc["version"] || "too old scheme"
      @name = json_desc["name"] || basename
      @description = (json_desc["description"] || "").gsub("\\n", "\n")
      @entry = json_desc["entry"] || ""
      @texts = json_desc["texts"] || ""
      @requires = json_desc["requires"] || {}
      @before = json_desc["before"] || []
      @thumbnail = json_desc["thumbnail"] || ""
      @preload = json_desc["preload"] || ""
    else
      @type = "old"
      name = File.basename path
      @id = name
      @version = "too old scheme"
      @name = name
      if File.exist? "#{path}/info.txt"
        @description = File.read "#{path}/info.txt"
      else
        @description = ""
      end
      @entry = ""
      @texts = ""
      @requires = {}
      @before = []
      if File.exist? "#{path}/preview.png"
        @thumbnail = "preview.png"
      else
        @thumbnail = ""
      end
      @preload = ""
    end

    @path = path
    @error = ""
    @enabled = false
    @loaded = false
    @settings = {}
  end

  def error=(msg)
    @error = msg
    @enabled = false
  end

  def clear_error
    @error = ""
  end

  def failed
    !@error.empty?
  end

  def load_thumbnail
    if @thumbnail.empty?
      a = Bitmap.new(536, 166)
      a.font.size *= 2
      a.draw_text(0, 0, 536, 166, $game_text["umm:umm:thumbnail/no_preview"], 1)
      a
    else
      Bitmap.new("#{@path}/#{@thumbnail}")
    end
  end

  def localized_name
    @name.start_with?("$") ? $game_text[@name.sub("$", "")] : @name
  end

  def localized_description
    @description.start_with?("$") ? $game_text[@description.sub("$", "")] : @description
  end

  def load_scripts(bnd)
    begin
      if @entry.empty?
        eval("load_from_list(getFileList(\"#{@path}/*.rb\"))", bnd)
      else
        load_script "#{@path}/#{@entry}"
      end
      @loaded = true
    rescue => e
      self.error = e.message + "\n" + e.backtrace.join("\n")
    end
  end

  def preload_scripts
    begin
      unless @preload.empty?
        load_script "#{@path}/#{@preload}"
      end
    rescue => e
      self.error = e.message + "\n" + e.backtrace.join("\n")
    end
  end

  FAILED_COLOR = Color.new(255, 0, 0)
  UNLOADED_DISABLED_COLOR = Color.new(200, 0, 0)
  UNLOADED_ENABLED_COLOR = Color.new(255, 255, 0)
  LOADED_DISABLED_COLOR = Color.new(0, 255, 255)
  LOADED_ENABLED_COLOR = Color.new(0, 255, 0)

  def get_labels
    labels = []
    if @loaded
      labels.push([$game_text["umm:manager:preview/loaded"], LOADED_ENABLED_COLOR])
    end
    if @loaded != @enabled && $mod_manager["umm"].loaded
      labels.push([$game_text["umm:manager:preview/wait_for_restart"], LOADED_DISABLED_COLOR])
    end
    if failed
      labels.push([$game_text["umm:manager:preview/failed"], FAILED_COLOR])
    end
    labels
  end

  def get_color
    if @loaded
      @enabled ? LOADED_ENABLED_COLOR : LOADED_DISABLED_COLOR
    elsif @enabled
      UNLOADED_ENABLED_COLOR
    elsif @failed
      FAILED_COLOR
    else
      UNLOADED_DISABLED_COLOR
    end
  end
end
