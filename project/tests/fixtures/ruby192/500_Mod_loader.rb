

$lang = $LonaINI["LonaRPG"]["Language"]
$game_text = Text.new("Text/#{$lang}")
#module FileGetter
#end
$mod_manager.link_texts
$mod_manager.load_mods(binding)


#if !File.exists?("GameMods.ini")
#	new_file = IniFile.new
#	# set properties
#	new_file["Mods"]={}
#	# pretty print object
#	p new_file
#	# set file path
#	new_file.filename = "GameMods.ini"
#	# save file
#	new_file.write()
#end
#$ModINI = IniFile.load("GameMods.ini")
#
#module FileGetter
#	def self.mods_remove_sections_folder_not_found
#		$ModINI.sections.each{|folder|
#			next if Dir.exist?("ModScripts/_Mods/#{folder}")
#			prp "Mod folder not found, removed from GameMods.ini=>#{folder}"
#			$ModINI.delete_section(folder)
#		}
#		$ModINI.save
#	end
#	def self.mods_scan_folder
#		modFolder = Dir.entries("ModScripts/_Mods/")
#		modFolder.delete(".")
#		modFolder.delete("..")
#		modFolder.each{|folder|
#			next prp "Mod already added => #{folder}" if $ModINI.has_section?(folder)
#			prp "New mod folder found => #{folder}"
#			$ModINI[folder]["Banned"] = 1
#			$ModINI[folder]["LoadOrder"] = 0
#		}
#		$ModINI.save
#	end
#
#	def self.mods_create_loadorder_and_execute
#		iniHash = $ModINI.to_h
#		exportArr = Array.new
#		iniHash.each{|modName,data| #get ini to a array
#			next if !data["Banned"].is_a?(Numeric) || !data["LoadOrder"].is_a?(Numeric)
#			exportArr << [modName,data["Banned"],data["LoadOrder"]]
#		}
#		return if exportArr.empty?
#		exportArr = exportArr.sort_by{ |arr| [arr[1],arr[2]]}
#		chk = 0
#		exportArr.each{|mod| #rebuild load order and load it
#			mod[2] = $ModINI[mod[0]]["LoadOrder"] = chk
#			chk +=1
#			if mod[1] == 0
#				prp "Active Mod => #{mod[0]}"
#				mod_ruby_list = getFileList("ModScripts/_Mods/#{mod[0]}/*.rb")
#				load_from_list(mod_ruby_list)
#			end
#		}
#		$ModINI.save
#	end
#end
#FileGetter.mods_remove_sections_folder_not_found
#FileGetter.mods_scan_folder
#FileGetter.mods_create_loadorder_and_execute
