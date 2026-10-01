module DataManager
  class << self
    alias_method :load_mod_database_Nuno, :load_mod_database
  end

  def self.load_mod_database
    load_mod_database_Nuno
	
	modFolder = "ModScripts/_Mods/Nun Outfit/"
	
	#Sample ---- Map001 Import to EvLib, Map002 Import to map info.
		evLibHash = FileGetter.load_mod_EventLib("#{modFolder}MapNun001.rvdata2")	
		$data_EventLib = $data_EventLib.merge(evLibHash) #merge EvLib Hash	
		
	
    $data_armors << RPG::Armor.new 
		$data_armors.last.id = $data_armors.length-1 
		$data_armors.last.load_additional_data("#{modFolder}items/NunMid.json")
		
		
		
		tmpLonaCHS = FileGetter.load_lona_chs_settings_from_dir(folder="#{modFolder}Data/L_CHS/",chsh=false)
		tmpLonaCHSH = FileGetter.load_lona_chs_settings_from_dir(folder="#{modFolder}Data/L_CHSH/",chsh=true)
		tmpLonaCHS["Lona"].parts[0].each{|data| $chs_data["Lona"].parts[0] << data}
		tmpLonaCHSH["Lona_H"].parts[0].each{|data| $chs_data["Lona_H"].parts[0] << data}
		$chs_data["Lona"].parts[0] = $chs_data["Lona"].parts[0].sort_by { |obj| obj.layer } #sort new chs by layer
		$chs_data["Lona_H"].parts[0] = $chs_data["Lona_H"].parts[0].sort_by { |obj| obj.layer }
		
		if !@portrait_Nuno #only execute one time each launch
			@portrait_Nuno = true #make sure only execute 1 time
			#Sample ---- import lona portrait data from a mod POSE1
			target = FileGetter.load_mod_lona_portrait_parts_dir(folder="#{modFolder}Data/POSE1_JSON/",pose_name="pose1")
			$data_lona_portrait[0] = $data_lona_portrait[0].merge(target[0])
			target[1]["pose1"].each{|data| $data_lona_portrait[1]["pose1"] << data}
			
			#Sample ---- import lona portrait data from a mod POSE5
			target = FileGetter.load_mod_lona_portrait_parts_dir(folder="#{modFolder}Data/POSE5_JSON/",pose_name="pose5")
			$data_lona_portrait[0] = $data_lona_portrait[0].merge(target[0])
			target[1]["pose5"].each{|data| $data_lona_portrait[1]["pose5"] << data}
			
			target = FileGetter.load_mod_lona_portrait_parts_dir(folder="#{modFolder}Data/POSE4_JSON/",pose_name="pose4")
			$data_lona_portrait[0] = $data_lona_portrait[0].merge(target[0])
			target[1]["pose4"].each{|data| $data_lona_portrait[1]["pose4"] << data}
			
		end
		
		    $mod_load_script["Data/HCGframes/event/SaintMonasteryMainPriest.rb"] = "#{modFolder}Data/HCGframes/event/SaintMonasteryMainPriest.rb"

  end
  end
  
  
  
  