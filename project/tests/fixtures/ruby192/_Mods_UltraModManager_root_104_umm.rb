def load_script(path)
  use_alternative = FileGetter::COMPRESSED && File.file?(path)
  path = $mod_load_script.fetch(path, path)
  prp "#{use_alternative ? "alt: " : ""}load_script path=#{path}", 6
  begin
    script = use_alternative ? File.read(path) : File.open(path, 'rb', &:read)
    if path.start_with?("Data/HCGframes/") || path.start_with?("Data/Batch/")
      self.instance_eval(script, path)
    else
      eval(script, binding, path)
    end
  rescue => ex
    msgbox ex.message + "\n" + ex.backtrace.join("\n")
  end
end

if File.exist? "ModScripts/_Mods/UltraModManager/scripts/startup/umm.rb"
  load_script "ModScripts/_Mods/UltraModManager/scripts/startup/umm.rb"
end
