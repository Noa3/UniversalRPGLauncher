######## File containing examples on how to use BitmapChanger #######


#### How to load setting from custom directory ####

#.json files in ModScripts/PaletteChanger/*.json are loaded automatically.
#To choose what mod file to load use simple loader below.
#Set mod to -1 to disable; 0 and above will load palette from mod_files[mod] path.

mod = -1

mod_files=[]
mod_files[0] = "ModScripts/PaletteChanger/1/portrait_replace.json"
mod_files[1] = "ModScripts/PaletteChanger/SomeOtherFolder/SomeOtherFile.json"

if mod >= 0 && !mod_files[mod].nil?
	BitmapChanger.load_setting(mod_files[mod])
end



=begin
#### How to apply palettes depending on parameters ####

#Moot lona palette swap
#Make sure you have .json that affects "names" = ["moot_portrait"]
class Lona_Part
	def get_bitmap(target)
		return if @bitmaps[target].nil?

		settings_key = nil
		#Checking if "moot" flag is set on this part
		is_moot = read_stat("Race") == "Moot"
		settings_key = "moot_portrait" if is_moot

		Cache.load_part(@root_folder,"/"+@bitmaps[target], settings_key)
	end

	def read_stat(name)
		for i in 0...@nameOrder.length
			if @nameOrder[i]==name then
				return @prtArrKey[i]
			end
		end
		return nil
	end
end

class Lona_CHS_Part <Lona_Part
	def read_stat(name)
		partNameOrder=@name_order[@part_name]
		for i in 0...partNameOrder.length
			if partNameOrder[i]==name then
				return @prtArrKey[i]
			end
		end
		return nil
	end


	def get_bitmap(target,h_mode=-1,position=-1)
		if h_mode >=0
			ptarget=@h_bitmaps[0][target]
		else 
			ptarget=@bitmaps[target]
		end
		return nil if  ptarget.nil? || ptarget.eql?("NA") || ptarget.eql?("nil")

		settings_key = nil
		#Checking if "moot" flag is set on this part
		is_moot = read_stat("Race") == "Moot"
		settings_key = "moot_CHS" if is_moot

		Cache.chs_material("#{@root_folder}/#{h_mode>=0 ? "CHSF0/" : ""}#{ptarget}", settings_key)
	end
end
=end