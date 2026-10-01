
#how to load ur rvdata2. or new map
	#create a new map through RMACE EDITOR.
	#save.
	#copy paste your map and rename to somewhere you like.
	#delete this map through  RMACE EDITOR.
	#save.
	#FileGetter.load_mod_EventLib("YourFolder/YourMap.rvdata2")

#Edit a Mod EventLib.
	#create a new map through RMACE EDITOR, and remember its map_id.
	#save, and close RMACE EDITOR
	#rename ur Event into Map"ID".rvdata2 and copy to Data\.
	#open RMACE EDITOR and load ur map.

#Summon this new event from EventLib.
	#f10 EvLib.sum("DEMO_WitchHat")
	
	
	
#create a custom event with a custom canvas size and custom file location u can refence init in below
	#batch_itemDropEquipSetup #any movement must b4 graphics setting, unless its under "late_common" & "end_late_common" pachget
	#modFolder = "ModScripts/_Mods/DEMO_ImportNewShit/"
	#@manual_character_name = "../../#{modFolder}DEMO_WitchHat"
	#
	#@manual_cw = 1 #canvas witdh(how many item in this PNG's witdh)
	#@manual_ch = 1 #canvas height(how many item in this PNG's height)
	#
	#
	#@pattern = @manual_pattern = 0 #force 0 because only 1x1 on this graphics
	#@direction = @manual_direction = 2 #force to 2 because only 1x1 on this graphics
	#@character_index = @manual_page_index = 0 #force 0 because only 1x1 on this
	
#set file location
module DataManager
	#hack database
	self.singleton_class.send(:alias_method, :load_mod_database_ArmoredLona, :load_mod_database)
	def self.load_mod_database
		load_mod_database_ArmoredLona ##keep me in first
		
		#Sample ---- your mod folder
		modFolder = "ModScripts/_Mods/ArmoredLona/"
		
		#Sample ---- Map001 Import to EvLib, Map002 Import to map info.
		FileGetter.load_mod_EventLib("#{modFolder}Map001.rvdata2")	#load a custom map into EventLib, Include DEMO_WitchHat and CustomApple, this will import events in ur map into eventLIB.
		$data_tag_maps["ArmoredLona"] = ["#{modFolder}Map002.rvdata2"] #add a tag_map to the game. use change_map_enter_tag("ModMap") to enter.
		
		#Sample ---- create new a Armor "DEMO_WitchHat" from Armors.rvdata2 
		#create a custom text Info for this item
		textFlag = Text.new("#{modFolder}Text/#{$lang}")
		load_data("#{modFolder}Armors.rvdata2").each{|item|  #import this Armor.rvdata2
			next if item.nil?
        tmpData = load_data("#{modFolder}Armors.rvdata2")
		
		#HEY! This is the Helmet! 1
		armoredLonaItemName = textFlag["Armored_Description:MercHead/item_name"]
		armoredLonaItemDesp = textFlag["Armored_Description:MercHead/description"]
        $data_armors << tmpData[1].clone
        $data_armors.last.load_additional_data
        $data_armors.last.id = $data_armors.length-1 #create item ID to last array length
        $data_armors.last.name = armoredLonaItemName #change its name to based to textFlag
        $data_armors.last.description = armoredLonaItemDesp #change its desp to based to textFlag
        $data_armors.last.icon_index = "#{modFolder}i1.png" #change this item icon
        $data_ItemName[$data_armors.last.item_name] = $data_armors.last #put this item to item Hash

		#HEY! This is the Boots! 2
		fGBootsItemName = textFlag["Armored_Description:MercBot/item_name"]
		fGBootsItemDesp = textFlag["Armored_Description:MercBot/description"]
        $data_armors << tmpData[2].clone
        $data_armors.last.load_additional_data
        $data_armors.last.id = $data_armors.length-1 #create item ID to last array length
        $data_armors.last.name = fGBootsItemName #change its name to based to textFlag
        $data_armors.last.description = fGBootsItemDesp #change its desp to based to textFlag
        $data_armors.last.icon_index = "#{modFolder}i4.png" #change this item icon
        $data_ItemName[$data_armors.last.item_name] = $data_armors.last #put this item to item Hash
        
		#HEY! This is the Skirt! 3
		fGSkirtItemName = textFlag["Armored_Description:MercMidEx/item_name"]
		fGSkirtItemDesp = textFlag["Armored_Description:MercMidEx/description"]
        $data_armors << tmpData[3].clone
        $data_armors.last.load_additional_data
        $data_armors.last.id = $data_armors.length-1 #create item ID to last array length
        $data_armors.last.name = fGSkirtItemName #change its name to based to textFlag
        $data_armors.last.description = fGSkirtItemDesp #change its desp to based to textFlag
        $data_armors.last.icon_index = "#{modFolder}i3.png" #change this item icon
        $data_ItemName[$data_armors.last.item_name] = $data_armors.last #put this item to item Hash

		#HEY! This is the Cloth! 4
		fGClothItemName = textFlag["Armored_Description:MercMid/item_name"]
		fGClothItemDesp = textFlag["Armored_Description:MercMid/description"]
        $data_armors << tmpData[4].clone
        $data_armors.last.load_additional_data
        $data_armors.last.id = $data_armors.length-1 #create item ID to last array length
        $data_armors.last.name = fGClothItemName #change its name to based to textFlag
        $data_armors.last.description = fGClothItemDesp #change its desp to based to textFlag
        $data_armors.last.icon_index = "#{modFolder}i2.png" #change this item icon
        $data_ItemName[$data_armors.last.item_name] = $data_armors.last #put this item to item Hash

		#HEY! This is the Chest! 5
		fGChestItemName = textFlag["Armored_Description:MercTop/item_name"]
		fGChestItemDesp = textFlag["Armored_Description:MercTop/description"]
        $data_armors << tmpData[5].clone
        $data_armors.last.load_additional_data
        $data_armors.last.id = $data_armors.length-1 #create item ID to last array length
        $data_armors.last.name = fGChestItemName #change its name to based to textFlag
        $data_armors.last.description = fGChestItemDesp #change its desp to based to textFlag
        $data_armors.last.icon_index = "#{modFolder}i5.png" #change this item icon
        $data_ItemName[$data_armors.last.item_name] = $data_armors.last #put this item to item Hash
			
			
		}
		
		############ protrait data hack
		#$data_lona_portrait is a array to save lona's portrait class
		#$data_lona_portrait[0] = nameOrder
		#$data_lona_portrait[1][0][0] = poseName, partData,PartClass
		#Sample ---- import lona CHS, CHSH data from a mod
		tmpLonaCHS = FileGetter.load_lona_chs_settings_from_dir(folder="#{modFolder}L_CHS/JSON/",chsh=false)
		tmpLonaCHSH = FileGetter.load_lona_chs_settings_from_dir(folder="#{modFolder}L_CHSH/JSON/",chsh=true)
		tmpLonaCHS["Lona"].parts[0].each{|data| $chs_data["Lona"].parts[0] << data}
		tmpLonaCHSH["Lona_H"].parts[0].each{|data| $chs_data["Lona_H"].parts[0] << data}
		$chs_data["Lona"].parts[0] = $chs_data["Lona"].parts[0].sort_by { |obj| obj.layer } #sort new chs by layer
        $chs_data["Lona_H"].parts[0] = $chs_data["Lona_H"].parts[0].sort_by { |obj| obj.layer }
		
		if !@portrait_ArmoredLona #only execute one time each launch
			@portrait_ArmoredLona = true #make sure only execute 1 time
			#Sample ---- import lona portrait data from a mod POSE1
			target = FileGetter.load_mod_lona_portrait_parts_dir(folder="#{modFolder}/POSE1_JSON/",pose_name="pose1")
			$data_lona_portrait[0] = $data_lona_portrait[0].merge(target[0])
			target[1]["pose1"].each{|data| $data_lona_portrait[1]["pose1"] << data}
			
			#Sample ---- import lona portrait data from a mod POSE5
			target = FileGetter.load_mod_lona_portrait_parts_dir(folder="#{modFolder}/POSE5_JSON/",pose_name="pose5")
			$data_lona_portrait[0] = $data_lona_portrait[0].merge(target[0])
			target[1]["pose5"].each{|data| $data_lona_portrait[1]["pose5"] << data}
			
			#Sample ---- hack data, this hat mod require a bigger canvas for lona pose1.
			#["poseName"][witdh,height,forced_x,forced_y]
			#this case we want expend canvas 98pix height, default height is 380, target is 380+98=478
			#default forcedY=36, target ForcedY= 36-97= -62.
			tmpCheckHeight = 478 - System_Settings::LONA_PORTRAIT_CANVAS_SETTING["pose1"][1] #get targeted canvas expend size. will get 98
			System_Settings::LONA_PORTRAIT_CANVAS_SETTING["pose1"][0] #dont give a fuck to X in this mode
			System_Settings::LONA_PORTRAIT_CANVAS_SETTING["pose1"][1] += tmpCheckHeight #height +98
			System_Settings::LONA_PORTRAIT_CANVAS_SETTING["pose1"][2] #dont give a fuck to X in this mode
			System_Settings::LONA_PORTRAIT_CANVAS_SETTING["pose1"][3] -= tmpCheckHeight #y -98 (because hat hack y+=98 in every part)
			$data_lona_portrait[1].each{|tmpPose,tmpData|
				next if !["pose1"].include?(tmpPose) #only pose1 need hack its canvas size
				tmpData.each{|tmpPart| #aim to part class
					tmpPart.posY += tmpCheckHeight #canvas expend 98pix for each part
				}	
			}
		end
		
		
		#Sample ---- import npc Portrait (untested) ? todo
		#tmpFile=File.open("path_dir/file.json")
		#npc_data=JSON.decode(remove_json_comments(tmpFile.read))
		#npc_portraits << NPC_Portrait.fromHash(npc_data)
	end
	
	#hack database2 hack in create game obj. after create database
	self.singleton_class.send(:alias_method, :load_mod_game_objects_ArmoredLona, :load_mod_game_objects)
	def self.load_mod_game_objects
		load_mod_game_objects_ArmoredLona ##keep me in first
	end
	
	
end


############################ hack map, add a place u can get the hat
class Game_Map
	alias setup_my_mod_ArmoredLona setup
	def setup(map_id)
		setup_my_mod_ArmoredLona(map_id)
		if @isOverMap #put the same map name you want to add, make sure its the same as the one in EV_Lib
			EvLib.sum("ArmoredLona",29,96)
		end
	end
end



=begin
#Sample ---- import lona CHS, CHSH data from a mod
module FileGetter #or hack in class Lona_CHS_Data?
	#hack Lona chs
	self.singleton_class.send(:alias_method, :load_lona_chs_json_ArmoredLona, :load_lona_chs_json)
	def self.load_lona_chs_json(chs_config)
		load_lona_chs_json_ArmoredLona(chs_config)
		fileList=getFileList("ModScripts/_Mods/ArmoredLona/L_CHS/JSON/*.json")
		fileList.each{|partfile|
			p "partfile=>#{partfile}"
			file= File.open(partfile)
			partSrcArr=JSON.decode(remove_json_comments(file.read))
			chs_config["parts"]+=partSrcArr
		}
		chs_config
	end
	
	#hack Lona chsH
	self.singleton_class.send(:alias_method, :load_lona_chsh_json_ArmoredLona, :load_lona_chsh_json)
	def self.load_lona_chsh_json(chs_config)
		load_lona_chsh_json_ArmoredLona(chs_config)
		fileList=getFileList("ModScripts/_Mods/ArmoredLona/L_CHSH/JSON/*.json")
		fileList.each{|partfile|
			p "partfile=>#{partfile}"
			file= File.open(partfile)
			partSrcArr=JSON.decode(remove_json_comments(file.read))
			chs_config["parts"]+=partSrcArr
		}
		chs_config
	end
end

#how to load ur custom TEXT file through call_msg?
#call_msg("../../#{ArmoredLona}ArmoredLona:text/text")


=end