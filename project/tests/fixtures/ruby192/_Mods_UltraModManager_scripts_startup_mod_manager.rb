# frozen_string_literal: true

MODS_DIR = "ModScripts/_Mods"

class ModManager
  attr_reader :new_mods

  def initialize
    @mods = {}
    @loaded_mods = false
    @preloaded_mods = false
    unless File.exist? "GameMods.ini"
      File.open("GameMods.ini", "w").close
    end
    @mods_ini = IniFile.load("GameMods.ini")
    @new_mods = []
    Dir["#{MODS_DIR}/*"].each do |path|
      next unless File.directory? path
      begin
        mod = load_mod_info path
        basename = File.basename path
        if @mods_ini.sections.include? basename
          @mods_ini[basename]["Banned"] = 1 unless @mods_ini[basename].include? "Banned"
          @mods_ini[basename]["LoadOrder"] = 0 unless @mods_ini[basename].include? "LoadOrder"
          mod.enabled = @mods_ini[basename]["Banned"] == 0
        else
          @mods_ini[basename]["Banned"] = 1
          @mods_ini[basename]["LoadOrder"] = 0
          @new_mods.push(mod)
        end
      rescue => e
        print "raised exception while loading mod info #{e}\n#{e.backtrace.join("\n")}\n"
      end
    end

    load_state_save(get_state_save)
  end

  def [](mod_id, setting_id = nil)
    if setting_id.nil?
      (mod_id.is_a? Integer) ? @load_order[mod_id] : @mods[mod_id]
    else
      self[mod_id].settings[setting_id].value
    end
  end

  def []=(mod_id, setting_id, value)
    s = self[mod_id].settings[setting_id]
    s.value = value
    @mods_ini[File.basename self[mod_id].path][setting_id] = value
    save_ini
    s.callback.call(value)
  end

  def load_order
    @load_order
  end

  def mods_count
    @mods.size
  end

  def ids
    @mods.keys
  end

  def load_mods(bnd)
    return if @loaded_mods
    @loaded_mods = true
    @load_order.each { |mod| mod.load_scripts(bnd) if mod.enabled }
  end

  def preload_mods
    return if @preloaded_mods
    @preloaded_mods = true
    @load_order.each { |mod| mod.preload_scripts if mod.enabled }
  end

  def load_mod_info(path)
    p "read mod info #{path}"
    mod = Mod.new(path)
    if @mods.include? mod.id
      p "ERROR mod ids conflict for id #{mod.id}: #{@mods[mod.id].path}, #{mod.path}"
      @mods[mod.id].error = "mod ids conflict: #{@mods[mod.id].path}, #{mod.path}"
    else
      @mods[mod.id] = mod
    end
    @mods[mod.id]
  end

  def fix_dependencies
    m = @mods["umm"]
    @load_order.delete(m)
    @load_order.insert(0, m)
    m.enabled = true

    load_ord = @load_order
    @load_order = []
    started_loading = []

    @mods.each { |_, mod| mod.clear_error }

    load_ord.each do |mod|
      begin
        check_dependencies_for(mod, started_loading)
      rescue => e
        @mods[mod.id].error = "raised exception #{e} while loading mod info"
      end
    end
    save_ini
  end

  def check_dependencies_for(mod, started_loading)
    return if @load_order.include? mod
    if started_loading.include? mod
      mod.error = "cyclic dependency"
      return
    end
    started_loading.push(mod)

    loaded_all_before = @mods.all? do |id, m|
      if m.before.include?(mod.id)
        check_dependencies_for m, started_loading
        unless @load_order.include? m
          mod.error = "see error in #{id}"
          next false
        end
      end
      true
    end
    mod.enabled &= loaded_all_before

    passed_checks = mod.requires.all? do |required_id, version_constrains|
      if version_constrains.is_a? String
        version_constrains = [version_constrains]
      end
      unless @mods.include? required_id
        mod.error = "required mod #{required_id} not found"
        next false
      end
      m = @mods[required_id]
      m_ver = m.version
      checks = version_constrains.all? { |constr| check_constrain(constr, m_ver) }
      unless checks
        mod.error = "required mod #{required_id} has wrong version"
        next false
      end
      m.enabled
    end
    mod.enabled &= passed_checks

    @load_order.push mod
  end

  def check_constrain(constr, version)
    spl_ver = version.split(".").map { |x| x.to_s }
    cspl_ver = version.slice(2..version.size).split(".").map { |x| x.to_s }
    comps = spl_ver.zip(cspl_ver).map { |arr| arr[0] <=> arr[1] }
    one_index = comps.index(1)
    m_one_index = comps.index(-1)
    if constr.start_with? ">="
      m_one_index.nil? || m_one_index > one_index
    elsif constr.start_with? "<="
      one_index.nil? || one_index > m_one_index
    elsif constr.start_with? ">"
      !(one_index.nil? || one_index > m_one_index)
    elsif constr.start_with? "<"
      !(one_index.nil? || one_index > m_one_index)
    elsif constr.start_with? "=="
      one_index.nil? && m_one_index.nil?
    elsif constr.start_with? "!="
      !(one_index.nil? && m_one_index.nil?)
    else
      false
    end
  end

  def get_state_save
    a = @mods_ini.to_h
    a.each { |k, v| a[k] = v.dup }
  end

  def load_state_save(save)
    save.each { |seg, part| part.each { |k, v| @mods_ini[seg][k] = v } }
    @load_order = @mods.values
    @load_order.each { |mod| mod.enabled = @mods_ini[File.basename mod.path]["Banned"] == 0 }
    @load_order.sort! do |a, b|
      ini_a = @mods_ini[File.basename a.path]
      ini_b = @mods_ini[File.basename b.path]
      if ini_a["LoadOrder"] != ini_b["LoadOrder"]
        next ini_a["LoadOrder"] <=> ini_b["LoadOrder"]
      end
      a.id <=> b.id
    end
    fix_dependencies
  end

  def swap(a, b)
    a, b = *([a, b].sort!)
    mod_a = @load_order[a]
    mod_b = @load_order[b]
    @load_order.delete mod_b
    @load_order.delete mod_a
    @load_order.insert(a, mod_b)
    @load_order.insert(b, mod_a)
    @mods_ini[File.basename mod_a.path]["LoadOrder"] = b
    @mods_ini[File.basename mod_b.path]["LoadOrder"] = a
    fix_dependencies
  end

  def save_ini
    @load_order.each_with_index do |m, i|
      @mods_ini[File.basename m.path]["LoadOrder"] = i
      @mods_ini[File.basename m.path]["Banned"] = m.enabled ? 0 : 1
    end
    @mods_ini.write
  end

  def link_texts
    @mods.each_value do |mod|
      $game_text.add_part(mod.id, "#{mod.path}/#{mod.texts}/#{$lang}")
    end
  end

  def found_mode(name)
    @mods.include? name
  end

  def declare_setting(mod_id, setting_id, localized_name, possible_values, default_value, &on_change)
    mod = @mods[mod_id]
    init_value = @mods_ini[File.basename mod.path].include?(setting_id) ? @mods_ini[File.basename mod.path][setting_id] : default_value
    mod.settings[setting_id] = Setting.new(setting_id, localized_name, possible_values, init_value, on_change)
    @mods_ini[File.basename mod.path][setting_id] = init_value
    save_ini
  end

  def get_resource(mod_id, relative_path)
    File.join @mods[mod_id].path, relative_path
  end
end

$mod_manager = ModManager.new