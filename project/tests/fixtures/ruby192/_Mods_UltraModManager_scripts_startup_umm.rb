# frozen_string_literal: true

MODS_PATH = "ModScripts/_Mods"
UMM_PATH = "#{MODS_PATH}/UltraModManager"

load_script "Data/Scripts/Frames/Modules/jsonEnDecoder.rb"

load_script "#{UMM_PATH}/scripts/startup/setting.rb"
load_script "#{UMM_PATH}/scripts/startup/mod.rb"
load_script "#{UMM_PATH}/scripts/startup/mod_manager.rb"
load_script "#{UMM_PATH}/scripts/startup/text.rb"
load_script "#{UMM_PATH}/scripts/startup/mod_manager_scene.rb"

$mod_manager.preload_mods

if $mod_manager["umm", "show_on_startup"]
  load_script "Data/Scripts/Frames/4_Cache.rb"
  load_script "Data/Scripts/Frames/6_SceneManager.rb"
  load_script "Data/Scripts/Frames/5_DataManager.rb"
  load_script "Data/Scripts/Frames/43_Sprite_Base.rb"
  load_script "Data/Scripts/Frames/53_Window_Base.rb"
  load_script "Data/Scripts/Frames/54_Window_Selectable.rb"
  load_script "Data/Scripts/Frames/55_Window_Command.rb"
  load_script "Data/Scripts/Frames/63_Window_ItemList.rb"
  load_script "Data/Scripts/Frames/100_Scene_Base.rb"
  load_script "Data/Scripts/Frames/103_Scene_MenuBase.rb"
  load_script "Data/Scripts/103_FileGetter.rb"
  load_script "Data/Scripts/Editables/50_System_Settings.rb"
  load_script "Data/Scripts/Frames/RVscript/520_YanflyF10.rb"
  load_script "Data/Scripts/Frames/RVscript/530_Hime_AllKey.rb"
  load_script "Data/Scripts/Frames/RVscript/540_GamePad.rb"
  load_script "Data/Scripts/Frames/RVscript/550_InputMenu.rb"
  load_script "Data/Scripts/Frames/RVscript/560_Mouse_Support.rb"
  load_script "Data/Scripts/Editables/509_SoundsLibs.rb"

  InputUtils.load_input_settings
  DataManager.load_peripheral_devices

  $loading_screen.hide
  begin
    ModManagerScene.new.main
  rescue => e
    msgbox e.message + "\n" + (e.backtrace.join("\n"))
    exit -1
  end

  $loading_screen.show
end