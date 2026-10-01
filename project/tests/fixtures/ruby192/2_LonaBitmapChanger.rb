#Made by Teravisor
#
#JSON format:
#{
#[Optional]	"name":"value"				Stores name modifier for cache. If it's applied, cache treats it as modifier to settings_name or filename.
#[Optional]	"condition":"code"			Code to evaluate to understand whether to execute this palette change or not. Return false or nil if not execute, anything else if execute.
#		"names":["name1","name2@X,Y,W,H"]	Names of graphics files to apply palette swap to. Not case-sensitive, but sensitive to file extension in calls ("Iconset" != "Iconset.png")
#							@X,Y,W,H define what rectangle on picture to apply change to. X,Y are bottom left pixel of rectangle, W,H are width and height of rectangle.
#		"palette_old:["name1.act", "name2.act"]	List of color tables containing old colors
#		"palette_new:["name1.act", "name2.act"] List of color tables containing new colors
#}

#V2: added rectangle limited application to files
#Format change: now "names":["name1","name2"] allows for "names":["name1","name2@X,Y,W,H"]
#Recommended name formats: "filename.png@1,2,1,2"
#"filename.png@x1,y2,w1,h2
#"filename.png@x=1,y=2,w=1,h=2
#symbols x,y,w,h,= are ignored and purely for understandability. In other words they do not do anything, numbers are read in order X,Y,Width,Height regardless of what letters you put in.

#Added ability to make array of files inside one file.
#For example
#[
#	{
#		"names": ["name1"],
#		"palette_old":["nameX1.act"],
#		"palette_new":["nameY1.act"]
#	},
#	{
#		"names": ["name2"],
#		"palette_old":["nameX2.act"],
#		"palette_new":["nameY2.act"]
#	}
#]

#Added ability to make multiple swaps with conditions for same list of files in single file.
#Note that name is used for caching images. Two settings without a name would have a problem when switching between them.
#Example
#{
#	"names": ["name1","name2","name3"],
#	"palettes":[
#		{
#			"name":"TrueDeepone",
#			"condition":"if $game_actors.length>0 then $game_player.actor.record_lona_race == 'TrueDeepone' end",
#			"palette_old":["nameX1.act"],
#			"palette_new":["nameY1.act"]
#		},
#		{
#			"name":"PreDeepone",
#			"condition":"if $game_actors.length>0 then $game_player.actor.record_lona_race == 'PreDeepone' end",
#			"palette_old:["nameX2.act"],
#			"palette_new":["nameY2.act"]
#		}
#	]
#}

#V2_1: allow packing when WRITING_LIST and using pack when COMPRESSED (attempt to make palette changer joiplay compatible)


#Need to apply BitmapChanger when new bitmap is loaded
#Also adds cache identificators to separate non-modified pictures from modified in cache.
module Cache
	class << self
		def load_bitmap(folder_name, filename, settings_name = nil)
#p "DEBUG: load_bitmap #{folder_name} / #{filename}"
			@cache ||= {}
			
			full_name="#{folder_name}#{filename}"
			path = "#{full_name}:#{settings_name.nil? ? BitmapChanger.find_cache_name(full_name) : settings_name}"

			if filename.empty?
				empty_bitmap
			else
				@cache[path] = BitmapChanger.load_bitmap(full_name, settings_name) unless include?(path)
				return @cache[path]
			end
		end
		def chs_material(filename, settings_name=nil)
#p "DEBUG: chs_material #{filename}"
			@chs_materials ||= {}
			path =  "#{filename}:#{settings_name.nil? ? BitmapChanger.find_cache_name(filename) : settings_name}"
			return @chs_materials[path] if @chs_materials.include?(path)
			return @chs_materials[path] = BitmapChanger.load_bitmap(filename, settings_name)
		end

		def load_part(folder_name, filename, settings_name = nil)
#p "DEBUG: load_part #{folder_name} / #{filename}"
			@parts_cache ||= {}
			full_name="#{folder_name}#{filename}"
			path = "#{full_name}:#{settings_name.nil? ? BitmapChanger.find_cache_name(full_name) : settings_name}"
			return @parts_cache[path] if @parts_cache.include?(path)
			return @parts_cache[path] = BitmapChanger.load_bitmap(full_name, settings_name)
		end
	end
end


###merged
#if .json files in condition had $game_player.actor it caused crash when returning to title menu.
#class Portrait_System
#	alias_method :initialize_pre_BitmapChanger, :initialize
#	def initialize
#		$game_player.actor if $game_player #making sure $game_player.actor is created before we request lona portrait
#		initialize_pre_BitmapChanger
#	end
#end

#Stores swap settings and allows to apply them to bitmap
module BitmapChanger
	REVERSE_FILE_ORDER = true #with true loads files 4,3,2,1,0 with false loads files 0,1,2,3,4
	class << self
		attr_accessor :filters #contains filters that apply swap setting it links to to pixels
		attr_accessor :name_to_setting #contains hash of name -> swap setting
	end
	
	#Apply settings to bitmap, settings are looked up by filename
	def self.change_bitmap(bitmap, filename)
		filters = find_filters(filename)
		return bitmap if filters.nil?

		if filters.length == 1
			filters[0].process_bitmap(bitmap) if filters[0].check_condition
		elsif filters.length == 0 #just in case erroneous key in hash
			@filters.delete(filename.downcase)
		else
			execute_filters(bitmap, filters)
		end
		return bitmap
	end
	
	def self.execute_filters(bitmap, filters_list)
		filters_used = optimize_filter_call_list(filters_list)
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.schedule_mode(bitmap.__id__, bitmap.width, bitmap.height)
			filters_used.each{|f|f.process_bitmap(bitmap)}
			BitmapChangerOptimization.execute
		elsif $D3D then #if D3D mod then need to use RGDirect built in method to access pixel array
			bitmap.process_color{|color|
				for x in 0...bitmap.width
					for y in 0...bitmap.height
						offset = x+y*bitmap.width
						old_color = color_to_integer(color, offset)
						current_color = old_color
						filters_used.each{|f|current_color = f.process_pixel_D3D(old_color, current_color, x, y, color, offset)}
						color[offset].set(current_color.red, current_color.green, current_color.blue, color[offset].alpha)
					end
				end
			}
		else #if not D3D using zeus Bitmap Export script provided by game to get pixel array pointer, it's 4 times faster to do that instead get_pixel/set_pixel
			color = bitmap.get_data.unpack('C*')
			for x in 0...bitmap.width
				for y in 0...bitmap.height
					offset = (x+y*bitmap.width)*4
					old_color = color[offset..offset+2]
					current_color = old_color
					filters_used.each{|f|current_color = f.process_pixel(old_color, current_color, x, y, color, offset)}
					color[offset] = current_color[0]
					color[offset+1] = current_color[1]
					color[offset+2] = current_color[2]
				end
			end
			bitmap.set_data(color.pack('C*'))
		end
	end

	def self.optimize_filter_call_list(filters_list)
		#We only need filters that have their conditions fulfilled
		filters_to_use = []
		filters_list.each{|v|filters_to_use << v if v.check_condition}

		#Optimization: reducing amount of passes through bitmap
		#for each limited -> ..... -> global remove what global overrides in limited
		#after that merge all global ones into last one
		#and remove limited filters from being executed

		#First calculate how much settings we'll need in the end
		amount_of_settings_needed = 0
		found_global = false
		filters_to_use.each{|f|
			if f.type == 2 then
				amount_of_settings_needed += 1
			elsif f.type == 1 && !found_global then
				found_global = true
				amount_of_settings_needed += 1
			end
		}
		if amount_of_settings_needed >= filters_to_use.length
			#we can't reduce amount of calls and reduction in what limited ones change is too limited so just execute everything
			return filters_to_use
		end
		#We can reduce amount of calls so allocating temporary settings
		while @temporary_swap_settings.length < amount_of_settings_needed do
			@temporary_swap_settings << BitmapChangerSwapSettings.new
		end

		#clear them before usage
		for num in 0...amount_of_settings_needed
			@temporary_swap_settings[num].clear
		end
		
		#order of execution in the end will not matter so element #0 will be global, rest will be limited
		limited_passed = 0 #amount of limited filters we have in array, also points to last filter in array
		has_global_filter=false
		rect_list = [] #to store limited filter rectangles
		filters_to_use.each{|filter|
			if filter.type == 1 then #global filter
				@temporary_swap_settings[0].merge(filter.swapsettings)#merge with previous global filter
				for i in 0...limited_passed#for each limited filter we passed before, remove collisions in them
					@temporary_swap_settings[i+1].remove_intersecting_settings(@temporary_swap_settings[0])
				end
				has_global_filter = true
			elsif filter.type == 2 then #for limited filters just copy it
				limited_passed += 1
				@temporary_swap_settings[limited_passed].merge(filter.swapsettings)
				rect_list << filter.rectangle_limit
			end
		}
		
		#Create filter list from swap settings
		res = []
		res << BitmapChangerFilter_Global.new(@temporary_swap_settings[0]) if has_global_filter
		for i in 1..limited_passed
			res << BitmapChangerFilter_Limited.new(@temporary_swap_settings[i], rect_list[i-1])
		end
		return res
	end
	
	def self.find_filters(filename)
		if @filters.nil? then
			return nil
		end
		return @filters[filename.downcase]
	end

	def self.find_cache_name(filename)
		res_name=""
		filters = find_filters(filename)
		return res_name if filters.nil?
		first=true
		filters.each{|v|
			next if !v.check_condition || v.name.nil?
			res_name << ":" if !first
			res_name << v.name
			first=false
		}
		return res_name
	end

	def self.load_bitmap(filename, settings_name=nil)
		bitmap = Bitmap.new(filename)
		settings_name = filename if settings_name.nil?
		change_bitmap(bitmap, settings_name)
		return bitmap
	end

	def self.initialize
		@temporary_swap_settings = [] #used by filter execution as cache for temporary settings when optimizing to reduce amount of filters
		if $D3D then
			BitmapChangerOptimization.invalidate #D3D doesn't use that .dll file, not sure if it'll even optimize something.
		else
			BitmapChangerOptimization.load_library
		end
	end

	def self.clear
		@filters = {}
		@name_to_setting = {}
		BitmapChangerOptimization.clear_settings if BitmapChangerOptimization.valid?
	end

	def self.load_settings
		clear
		
		@filedb = load_data("Data/Palettes.rvdata2") if FileGetter::COMPRESSED
		@files_to_compress = [] if FileGetter::WRITING_LIST
		@files_to_compress_binary = [] if FileGetter::WRITING_LIST

		if FileGetter::COMPRESSED then
			@filedb[1].each{|k,v|
				if File.extname(k) == ".json" then
					load_setting_file(k)
				end
			}
		end

		file_list = FileGetter.getFileList("ModScripts/PaletteChanger/*.json")
		file_list.reverse! if REVERSE_FILE_ORDER
		file_list.each{|json_path|
			@files_to_compress << json_path if FileGetter::WRITING_LIST
			load_setting_file(json_path)
		}

		if FileGetter::WRITING_LIST && !FileGetter::COMPRESSED
			@filedb = [[],{}]#[0] is filedata {} is filename=>[0]id
			@files_to_compress.each{|file_name|
				File.open(file_name,'r'){|file|
					@filedb[0] << file.read
					@filedb[1][file_name]=@filedb[0].length-1
				}
			}
			@files_to_compress_binary.each{|file_name|
				File.open(file_name,'rb'){|file|
					@filedb[0] << file.read
					@filedb[1][file_name]=@filedb[0].length-1
				}
			}
			save_data(@filedb, "Data/Palettes.rvdata2")
		end
	end

	#2.1 reading files from compressed rvdata2
	def self.read_file(file_name, file_mode='r')
		if !FileGetter::COMPRESSED then
			File.open(file_name, file_mode){|file|yield file.read
			}
		else
			yield @filedb[0][@filedb[1][file_name]]
		end
	end
	def self.file_size(file_name)
		if !FileGetter::COMPRESSED then
			File.size(file_name)
		else
			@filedb[0][@filedb[1][file_name]].length
		end
	end
	
	#Should return array [R,G,B,R,G,B,R,G,B,...] that has 3*byte length, each 3 bytes are RGB color.
	def self.read_palette_file(in_file) 
		file_path = "ModScripts/PaletteChanger/#{in_file}"
		extension = File.extname(file_path)
		@files_to_compress_binary << file_path if FileGetter::WRITING_LIST

		res_arr = read_palette(file_path, extension)

		#fallback to .act file
		if res_arr.nil? then
			res_arr = read_act(file_path)
		end
		return res_arr
	end

#REDEFINE THIS METHOD TO ADD MORE FILE FORMATS
#Must return array of RGB bytes (0-255 values): [R,G,B, R,G,B, ...]
	def self.read_palette(file_path, file_extension)
		if file_extension == '.act' then
			return read_act(file_path)
		end
	end

	def self.read_act(in_file)
		file_len = file_size(in_file)
		file_size = 768
		arr_result = nil
		read_file(in_file, 'rb'){|file_data|
			file_size = file_data[768..769].unpack('n')[0] if file_len == 772
			arr_result = file_data[0..file_size].unpack('C*')
		}
		return arr_result
	end
	
	def self.load_setting_file(json_path)
		begin
		read_file(json_path, 'r'){|file_data|
			j_data=JSON.decode(file_data)
			if j_data.is_a?(Array) then
				j_data.each{
					|json_data|
					json_data["filename"]=json_path
					load_setting(json_data)
				}
			elsif j_data.is_a?(Hash) then
				j_data["filename"]=json_path
				load_setting(j_data)
			else
				p "Bitmap Changer ERROR: not an array or hash!"
			end
		}
		BitmapChanger.GC_DLL
		rescue =>ex
			msgbox "Bitmap Changer ERROR: loading file #{json_path}: #{ex}"
		end
	end

	def self.load_setting(json_data)
		names = json_data["names"]
		if names == nil then
			p "Bitmap Changer: file #{json_path} does not have 'names'"
			return
		end

		settings = []
		if json_data["palette_old"] && json_data["palette_new"] then
			read_palette_setting(json_data, settings)
		end
		if json_data["palettes"].is_a?(Array) then
			json_data["palettes"].each{|palette_hash|
				palette_hash["filename"]=json_data["filename"]
				read_palette_setting(palette_hash, settings)
			}
		end
		
		#if at least one swap is set up, add setting into all listed file names to be applied on them
		settings.each{|setting|
			#settings.each{|set|
			names.each{|name|
				name.downcase!
				#Check for limits for specific file. Acceptable format is 4 numbers delimited by comma and any amount of spaces,x,y,w,h,=: "filename.png@x = 1, y=2, w 1, h 2"
				#x,y,w,h,= are ignored and purely for writer.
				#Recommended formats: "filename.png@1,2,1,2"
				#"filename.png@x1,y2,w1,h2
				#"filename.png@x=1,y=2,w=1,h=2
				split_name = name.split("@")
				if split_name.length>1 then #file name contains @10,15,32,64 so it's limited filter
					split_name[1].delete!("xywh= ")
					split_coordinates = split_name[1].split(",")
					#Assumed we get numbers now
					if split_coordinates.length >=4 then
						rectangle = Rect.new(split_coordinates[0].to_i,split_coordinates[1].to_i,split_coordinates[2].to_i,split_coordinates[3].to_i)
						name = split_name[0] #so that it gets put into settings without @XXXX
						@filters[name] ||= []
						@filters[name] << BitmapChangerFilter_Limited.new(setting, rectangle)
					else
						p "BitmapChanger ERROR: settings file #{filename} has file listed #{name} which has incorrect coordinates. Ignored."
					end
				else
					@filters[name] ||= []
					@filters[name] << BitmapChangerFilter_Global.new(setting)
				end
			}
			@name_to_setting[setting.name] = setting if !setting.name.nil?
		}
	end

	#def self.asdasdasdasd
	#	#p @filters[name]
	#	#msgbox "Asdasdasd1"
	#	#p @name_to_setting[name]
	#	#msgbox "Asdasdasd1"
	#end

	def self.read_palette_setting(json_hash, settings)
		palette_old_path = json_hash["palette_old"]
		if palette_old_path == nil then
			p "Bitmap Changer: file #{json_path} does not have 'palette_old'"
			return
		end
		palette_new_path = json_hash["palette_new"]
		if palette_new_path == nil then
			p "Bitmap Changer: file #{json_path} does not have 'palette_new'"
			return
		end

		if palette_old_path.is_a?(String)
			palette_old_path = [palette_old_path]
		end

		if palette_new_path.is_a?(String)
			palette_new_path = [palette_new_path]
		end
		len = [palette_new_path.length, palette_old_path.length].min
		if palette_new_path.length != palette_old_path.length
			p "Bitmap Changer: WARNING: file #{json_path} has different amount of elements in palette_old and palette_new"
		end

		#Loading palette files themselves: arr_new and arr_old contain RGB colors (3 array elements per color)
		arr_old = []
		arr_new = []
		
		total_len = 0

		for i in 0...len
			arr_old_tmp = read_palette_file(palette_old_path[i])
			arr_new_tmp = read_palette_file(palette_new_path[i])
       
			len = [arr_old_tmp.length, arr_new_tmp.length].min
			len = (len / 3) * 3 #array size should be 3*N
			arr_old.concat(arr_old_tmp[0...len])
			arr_new.concat(arr_new_tmp[0...len])
			total_len += len/3
		end
		
		#Finding if we already have setting with specified name and creating new one if not
		json_hash["name"] = json_hash["filename"] if !json_hash["name"]
		setting = @name_to_setting[json_hash["name"]] if !json_hash["name"].nil?
		setting = BitmapChangerSwapSettings.new if setting.nil?
		setting.condition = json_hash["condition"] if !json_hash["condition"].nil?
		setting.name = json_hash["name"]
		
		has_settings = false;
		
		#Filling swaps from arr_old and arr_new into setting
		for i in 0...total_len
			if arr_old[3*i]!=arr_new[3*i] || arr_old[3*i+1]!=arr_new[3*i+1] || arr_old[3*i+2]!=arr_new[3*i+2] then
				setting.add_swap(arr_old[3*i..3*i+2], arr_new[3*i..3*i+2])
				has_settings = true
			else
				setting.remove_swap(arr_old[3*i..3*i+2])
			end
		end
		settings << setting if has_settings
	end
	
	#might free some memory used by BitmapChanger_Optimization after merges of multiple json
	def self.GC_DLL
		return if !BitmapChangerOptimization.valid?
		max_number = 0
		@filters.each_value{|val|
			val.each{|setting|
				max_number=setting.swapsettings.palette_swap_settings if setting.swapsettings.palette_swap_settings > max_number
			}
		}
		numbers = *(0..max_number)
		@filters.each_value{|val|
			val.each{|setting|
				numbers.delete(setting.swapsettings.palette_swap_settings)
			}
		}
		numbers.each{|val|BitmapChangerOptimization.remove_setting(val)}
	end

	def self.color_to_integer(color)
		return color.red.to_i * 256 * 256 + color.green.to_i * 256 + color.blue.to_i
	end
end

module BitmapChangerOptimization

	DLL_PATH = "ModScripts/PaletteChanger/BitmapChanger_Optimization.dll"

	def self.load_library
		begin

			@add_setting = Win32API.new(DLL_PATH, "add_setting", "V", "I")
			@add_swap = Win32API.new(DLL_PATH, "add_swap", "IIIIIII", "V")
			@remove_swap = Win32API.new(DLL_PATH, "remove_swap", "IIII", "V")
			@process_bitmap = Win32API.new(DLL_PATH, "process_bitmap", "IIII", "V")
			@merge_settings = Win32API.new(DLL_PATH, "merge_settings", "II", "I")
			@remove_setting = Win32API.new(DLL_PATH, "remove_setting", "I", "V")
			@clear_settings = Win32API.new(DLL_PATH, "clear_settings", "V", "V")
			@process_bitmap_limited = Win32API.new(DLL_PATH, "process_bitmap_limited", "IIIIIIII", "V")
			@remove_intersecting_settings = Win32API.new(DLL_PATH, "remove_intersecting_settings", "II", "V")
			@schedule_mode = Win32API.new(DLL_PATH, "schedule_mode", "III", "V")
			@execute = Win32API.new(DLL_PATH, "execute", "V", "V")
			@exclusive_merge = Win32API.new(DLL_PATH, "exclusive_merge", "II", "I")
			@use_library = true
		rescue =>ex
			p "Bitmap Changer optimization failed: #{ex}"
			@use_library = false
		end		
	end

	def self.valid?
		return @use_library
	end

	def self.invalidate #D3D mode doesn't support library as or writing this so needs to disable it
		@use_library = false
	end

	def self.add_setting
		return @add_setting.call
	end

	def self.add_swap(setting, r, g, b, new_r, new_g, new_b)
		@add_swap.call(setting,r,g,b,new_r,new_g,new_b)
	end

	def self.remove_swap(setting, r,g,b)
		@remove_swap.call(setting,r,g,b)
	end
	
	def self.process_bitmap(bitmap_data_addr, bitmap_width, bitmap_height, setting_number)
		@process_bitmap.call(bitmap_data_addr, bitmap_width, bitmap_height, setting_number)
	end

	def self.process_bitmap_limited(bitmap_data_addr, bitmap_width, bitmap_height, setting_number, limit_x, limit_y, limit_width, limit_height)
		@process_bitmap_limited.call(bitmap_data_addr, bitmap_width, bitmap_height, setting_number, limit_x, limit_y, limit_width, limit_height)
	end

	def self.merge_setting(setting1, setting2) #adds all colors from setting2 to setting1 and returns setting1
		@merge_settings.call(setting1,setting2)
	end

	def self.remove_setting(setting)
		@remove_setting.call(setting)
	end

	def self.clear_settings
		@clear_settings.call
	end
	
	def self.remove_intersecting_settings(setting1, setting2)
		@remove_intersecting_settings.call(setting1, setting2)
	end

	def self.schedule_mode(bitmap_data_addr, bitmap_width, bitmap_height)
		@schedule_mode.call(bitmap_data_addr, bitmap_width, bitmap_height)
	end

	def self.execute
		@execute.call
	end

	def self.exclusive_merge(setting1, setting2)
		@exclusive_merge.call(setting1,setting2)
	end
end

#Use its inherited classes below
class BitmapChangerFilter_Base
	attr_accessor :swapsettings
	def initialize(settings)
		@swapsettings = settings
	end
	def name
		swapsettings.name
	end
	def condition
		swapsettings.condition
	end
	def check_condition
		res = swapsettings.check_condition
		return res
	end
	
	def change_color(array, offset)#D3D version
		new_color = @palette_swap_settings[color_to_integer(array[offset])]
		if new_color then
			array[offset].set(new_color.red, new_color.green, new_color.blue, array[offset].alpha)
		end
	end

	def change_rgba(array, offset)#Base game no DLL version
		new_color = swapsettings.palette_swap_settings[array[offset..offset+2]]
		if new_color then
			array[offset] = new_color[0]
			array[offset+1] = new_color[1]
			array[offset+2] = new_color[2]
		end
	end
end

#Applies to all pixels of bitmap
class BitmapChangerFilter_Global < BitmapChangerFilter_Base
	#apply to whole bitmap
	def process_bitmap(bitmap)
		if BitmapChangerOptimization.valid? && !$D3D then
			change_DLL(bitmap.__id__, bitmap.width, bitmap.height) #uses address provided by zeus bitmap export
		elsif $D3D then #if D3D mod then need to use RGDirect built in method to access pixel array
			bitmap.process_color{|color|
				for i in 0...bitmap.width*bitmap.height
					change_color(color, i)
				end
			}
		else #if not D3D using zeus Bitmap Export script provided by game to get pixel array pointer, it's 4 times faster to do that instead get_pixel/set_pixel
			color = bitmap.get_data.unpack('C*')
			for i in 0...color.length/4
				change_rgba(color, i*4)
			end
			bitmap.set_data(color.pack('C*'))
		end
	end
	
	def process_pixel(old_color, current_color, x, y, array, offset)
		res = swapsettings.palette_swap_settings[array[offset..offset+2]]
		return res if !res.nil?
		return current_color
	end
	
	def process_pixel_D3D(old_color, current_color, x, y, array, offset)
		res = swapsettings.palette_swap_settings[BitmapChanger.color_to_integer(array[offset])]
		return res if !res.nil?
		return current_color
	end
	
	def change_DLL(addr, bitmap_width, bitmap_height, rectlimit = nil)#DLL version
		BitmapChangerOptimization.process_bitmap(addr, bitmap_width, bitmap_height, swapsettings.palette_swap_settings)
	end
	
	def type
		1
	end
end

#Applies only to pixels within rectangle area
class BitmapChangerFilter_Limited < BitmapChangerFilter_Base
	attr_accessor :rectangle_limit
	def initialize(settings, rectangle)
		@swapsettings = settings
		@rectangle_limit = rectangle
	end
	#apply to rectangle
	def process_bitmap(bitmap)
		#check what rectangle out of rectangle_limit is actually within bitmap
		viable_rectangle = Rect.new
		viable_rectangle.x = [[@rectangle_limit.x,bitmap.width].min,0].max
		viable_rectangle.y = [[@rectangle_limit.y,bitmap.height].min,0].max
		viable_rectangle.width = [[@rectangle_limit.width, bitmap.width - viable_rectangle.x].min,0].max
		viable_rectangle.height = [[@rectangle_limit.height, bitmap.height - viable_rectangle.y].min,0].max
		if BitmapChangerOptimization.valid? && !$D3D then
			change_DLL(bitmap.__id__, bitmap.width, bitmap.height, viable_rectangle) #uses address provided by zeus bitmap export
		elsif $D3D then #if D3D mod then need to use RGDirect built in method to access pixel array
			bitmap.process_color{|color|
				for x in viable_rectangle.x...(viable_rectangle.x+viable_rectangle.width)
					for y in viable_rectangle.y...(viable_rectangle.y+viable_rectangle.height)
						change_color(color, x+y*bitmap.width)
					end
				end
			}
		else #if not D3D using zeus Bitmap Export script provided by game to get pixel array pointer, it's 4 times faster to do that instead get_pixel/set_pixel
			return if !swapsettings.palette_swap_settings
			color = bitmap.get_data.unpack('C*')
			for x in viable_rectangle.x...(viable_rectangle.x+viable_rectangle.width)
				for y in viable_rectangle.y...(viable_rectangle.y+viable_rectangle.height)
					change_rgba(color, (x+y*bitmap.width)*4)
				end
			end
			bitmap.set_data(color.pack('C*'))
		end
	end

	def process_pixel(old_color, current_color, x, y, array, offset)
		return current_color if x < @rectangle_limit.x || x >= @rectangle_limit.x+@rectangle_limit.width || y<@rectangle_limit.y || y>=@rectangle_limit.y+@rectangle_limit.height
		res = swapsettings.palette_swap_settings[array[offset..offset+2]]
		return res if !res.nil?
		return current_color
	end
	
	def process_pixel_D3D(old_color, current_color, x, y, array, offset)
		return current_color if x < @rectangle_limit.x || x >= @rectangle_limit.x+@rectangle_limit.width || y<@rectangle_limit.y || y>=@rectangle_limit.y+@rectangle_limit.height
		res = swapsettings.palette_swap_settings[BitmapChanger.color_to_integer(array[offset])]
		return res if !res.nil?
		return current_color
	end


	def change_DLL(addr, bitmap_width, bitmap_height, rectlimit)#DLL version
		BitmapChangerOptimization.process_bitmap_limited(addr, bitmap_width, bitmap_height, swapsettings.palette_swap_settings, rectlimit.x, rectlimit.y, rectlimit.width, rectlimit.height)		
	end

	def type
		2
	end
end

#Bitmap swap change settings and application to pixels
class BitmapChangerSwapSettings
	attr_accessor :palette_swap_settings
	attr_accessor :condition
	attr_accessor :name

	def initialize
		has_global_paint = true
		if BitmapChangerOptimization.valid?
			@palette_swap_settings = BitmapChangerOptimization.add_setting
		else
			@palette_swap_settings = {}
		end
	end

	def clear
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.remove_setting(palette_swap_settings) #it doesn't actually delete setting inside DLL, just clears it
		else
			palette_swap_settings = {}
		end
	end
	
	def my_clone #1 level deep cloning.
		new_clone = self.clone
		if palette_swap_settings.class == Hash
			new_clone.palette_swap_settings = palette_swap_settings.clone
		elsif palette_swap_settings.is_a?(Numeric) && BitmapChangerOptimization.valid?
			new_clone.palette_swap_settings = BitmapChangerOptimization.add_setting #Creating new setting
			BitmapChangerOptimization.merge_setting(new_clone.palette_swap_settings, palette_swap_settings) #Adding all colors to it
		end
		return new_clone
	end

	def check_condition
		begin
			return true if condition.nil?
			return eval(condition)
		rescue
			return false
		end
	end
	
	#exclusive merging is for backwards file sort order
	def merge(another_setting, exclusive = false)
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.merge_setting(palette_swap_settings, another_setting.palette_swap_settings)
		else
			another_setting.palette_swap_settings.each{|k,v|
				palette_swap_settings[k]=v
			}
		end
	end

	def remove_intersecting_settings(another_setting)
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.remove_intersecting_settings(palette_swap_settings, another_setting.palette_swap_settings)
		else
			another_setting.palette_swap_settings.each_key{|k|
				palette_swap_settings.delete(k)
			}
		end
	end

	def color_arr_to_integer(red,green,blue)
		return red * 256 * 256 + green * 256 + blue
	end

	def add_swap(old_color, new_color)
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.add_swap(palette_swap_settings, old_color[0], old_color[1], old_color[2], new_color[0], new_color[1], new_color[2])
		elsif $D3D then
			@palette_swap_settings[color_arr_to_integer(old_color[0],old_color[1],old_color[2])] = Color.new(new_color[0],new_color[1],new_color[2])
		else
			old_color.reverse! #RPGMaker uses BGRA
			new_color.reverse!
			@palette_swap_settings[old_color]=new_color
		end
	end

	def remove_swap(old_color)
		if BitmapChangerOptimization.valid?
			BitmapChangerOptimization.remove_swap(palette_swap_settings, old_color[0], old_color[1], old_color[2])
		elsif $D3D then
			@palette_swap_settings.delete(color_arr_to_integer(old_color[0],old_color[1],old_color[2]))
		else
			old_color.reverse! #RPGMaker uses BGRA
			@palette_swap_settings.delete(old_color)
		end
	end
end

#Initialize BitmanChanger only when game starts so that mods can affect it.
module SceneManager
	class << self
		alias_method :run_afterbitmapchanger, :run
		def run
			begin
				BitmapChanger.initialize
				BitmapChanger.load_settings
			rescue => ex
				msgbox("BitmapChanger error #{ex}")
			end
			run_afterbitmapchanger
		end
	end
end
