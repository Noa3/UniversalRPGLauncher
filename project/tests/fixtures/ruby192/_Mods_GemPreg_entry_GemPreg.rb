module DataManager
	class << self
		alias_method :load_mod_lang_GemPreg, :update_Lang
	end
	
	def self.update_Lang
		load_mod_lang_GemPreg
        $game_text_GemPreg = Text.new("ModScripts/_Mods/GemPreg/Text/#{$lang}")
    end
end

module DataManager
    self.singleton_class.send(:alias_method, :load_mod_database_GemPreg, :load_mod_database)
    def self.load_mod_database
        load_mod_database_GemPreg ##keep me in first
        modFolder = "ModScripts/_Mods/GemPreg/"
		$gempreg_modFolder = modFolder
		
		mod_events = FileGetter.load_mod_EventLib("#{modFolder}Data/Effect.rvdata2")
		mod_events.each{|evName,evData| ############### change its graphics if needed.  u can design ur own condition
			evData[1].pages.each{|tmpPage|
				next if !tmpPage.graphic.character_name
				next if tmpPage.graphic.character_name == ""
				next if tmpPage.graphic.character_name.include?("-char-") #do nothing to CHS characters
				next if FileTest.exist?("Graphics/Characters/#{tmpPage.graphic.character_name}.png") # check if original file source exist.  if yes.  dont do anything
				tgtPath = "../../#{modFolder}Graphics/Characters/#{tmpPage.graphic.character_name}.png" #create new path for new file on ur mod folder
				next if !FileTest.exist?("Graphics/Characters/"+tgtPath) #if ur mod also not include the file,  do nothing
				tmpPage.graphic.character_name = tgtPath #rewrite file path
			}
		}
		$data_EventLib = $data_EventLib.merge(mod_events)
		
		arpgSkills = FileGetter.load_skill_from_json("#{modFolder}Data/Effects/Skill/")
		$data_arpgskills = $data_arpgskills.merge(arpgSkills)
        
		System_Settings::TRAIT::LIST[4][8] = "TraitGemPreg"
       
		
		
		#new Items, file contains with ItemGem1 2 and 3. Handley overwrites as well as new items
		types = ["Skill", "States", "Items"]
		types.each{|type|
			FileGetter.getFileList("#{modFolder}Data/Effects/#{type}/*.json").each{|file|
				add_new_smth(file, type)
			}
		}
	
	end
	
	
	def self.add_new_smth(path, type)  #If you want to use it to completely overwrite an existing stat, the json file should have “id” “name” and “item_name” as in the original one. Taking into account the stat data hidden in rvdata2. And if you want the stat to be new, then don't specify id, but name and item_name should be unique. If you specify id, it will be overwritten.
		
		case type
			when "States"
				data = $data_states
				data_name = $data_StateName
			when "Skill"
				data = $data_skills
				data_name = $data_SkillName
			when "Items"
				data = $data_items
				data_name = $data_ItemName
			when "Weapons"
				data = $data_weapons
				data_name = $data_ItemName
			when "Armors"
				data = $data_armors
				data_name = $data_ItemName
			else
				return
		end
		
		
		temp_hash = nil
		begin; temp_hash = JSON.decode(open(path).read); rescue msgbox("Error in Json == [[ #{path} ]] :: def self.add_new_smth"); end
		return if temp_hash == nil
		
		if temp_hash["name"] == nil && temp_hash["item_name"] == nil  && temp_hash["id"] == nil
			file_name = path.split('/').last
			data_name.each{|wut,state|
				next if state.addData == nil
				next if state.addData["eff_cfg"] == nil
				if state.addData["eff_cfg"].include?("#{file_name}")
					data_name[wut].load_additional_data(path)
				end
			}
			return
		
		elsif temp_hash["id"] != nil
			tmpid = temp_hash["id"]
			data[tmpid].load_additional_data(path)
			return
		elsif temp_hash["name"] != nil && data_name[temp_hash["name"]] != nil
			data_name[temp_hash["name"]].load_additional_data(path)
			return
		elsif temp_hash["item_name"] != nil && data_name[temp_hash["item_name"]] != nil
			data_name[temp_hash["item_name"]].load_additional_data(path)
			return
		end
		
		if type == "Skill" && temp_hash["name"] == nil #arpgSkill
			return
		end
		
		if temp_hash["item_name"] == nil
			msgbox("No item_name in [#{path}]")
			return
		end
		
		tmpid = data.length
		case type
			when "States"
				data << RPG::State.new
			when "Skill"
				data << RPG::Skill.new
			when "Items"
				data << RPG::Item.new
			when "Weapons"
				data << RPG::Weapon.new
			when "Armors"
				data << RPG::Armor.new
		end
		data.last.id = tmpid
		data.last.load_additional_data(path)
	end
	
    
    #hack database2 hack in create game obj. after create database
    self.singleton_class.send(:alias_method, :load_mod_game_objects_GemPreg, :load_mod_game_objects)
    def self.load_mod_game_objects
        load_mod_game_objects_GemPreg ##keep me in first
    end
	
end


### adding a graphic
   

############################################################### mod trait list
class Game_Actor < Game_Battler
	alias_method :gift_trait_addable_list_GemPreg, :gift_trait_addable_list
	def gift_trait_addable_list(current_selected)
		originalHASH = gift_trait_addable_list_GemPreg(current_selected)
		originalHASH["TraitGemPreg"] = trait_TraitGemPreg_addable?(current_selected)
		originalHASH
	end
	
	###  1 can take, but not yet,    2 blocked by trait      0 can take
	def trait_TraitGemPreg_addable?(current_selected) #115
		return 3 if state_stack("TraitGemPreg") == 1 #self
		return 2 if trait_Succubus_addable?(current_selected) == 2
		return 4 if current_selected.include?("Succubus")
		return 1 if @level < 20
        return 1 if state_stack("Succubus") != 1
		return 0
	end
	
	
	
end

############################ redirect load_script("Command_BasicNeeds.rb") to somewhere else.
$mod_load_script["Data/Command_BasicGems.rb"] = "ModScripts/_Mods/GemPreg/Data/HCGframes/Command_BasicGems.rb"

class Text
	
	alias_method :alias_load_file_Orig_gem, :load_file unless method_defined?(:alias_load_file_Orig_gem)
	
	def load_file(part = nil, file)
		mod_file = File.exists?("#{$gempreg_modFolder}/#{@parts[part]}/#{file}.txt")
		orig_file = File.exists?("#{@parts[part]}/#{file}.txt")

		if mod_file && orig_file
			hashh = alias_load_file_Orig_gem(part, file).merge(load_file_gempreg(part, file))
		elsif mod_file
			hashh = load_file_gempreg(part, file)
		else
			hashh = alias_load_file_Orig_gem(part, file)
		end
		
		return hashh
	end
	
	def load_file_gempreg(part = nil, file)
		begin
			sth=File.read("#{$gempreg_modFolder}/#{@parts[part]}/#{file}.txt")
			return parse(sth.to_s.encode("utf-8"))
		rescue => ex
			msgbox "ERROR: missing translation file #{$gempreg_modFolder}/#{@parts[part]}/#{file}.txt"
			return Hash.new
		end
	end

end