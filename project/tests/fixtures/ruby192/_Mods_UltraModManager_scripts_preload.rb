# frozen_string_literal: true

$mod_load_script["Data/Scripts/Frames/121_Dialog_Control_System.rb"] =
  $mod_manager.get_resource("umm", "scripts/replacers/121_Dialog_Control_System.rb")
$mod_load_script["Data/Scripts/Frames/126_MailTexts.rb"] =
  $mod_manager.get_resource("umm", "scripts/replacers/126_MailTexts.rb")

# too lazy to understand why original implementation is broken just copy original and patch it
$mod_load_script["ModScripts/500_Mod_loader.rb"] =
  $mod_manager.get_resource("umm", "scripts/replacers/500_Mod_loader.rb")

$mod_manager.declare_setting("umm", "show_on_startup", $game_text["umm:umm:setting/show_on_startup"], [true, false], true) {}
