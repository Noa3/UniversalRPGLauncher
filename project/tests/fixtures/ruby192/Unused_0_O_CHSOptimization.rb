#Optimization mod by Teravisor

#Changes CHS cache to store at least MAX_ENTRIES_TO_STORE of @parts_cache or @chs_material
#Separates parts data update logic from bitmap rebuild in order to not rebuild CHS when parts composition didn't change.

unless $CHSOptimization
$CHSOptimization = true

module Cache

	MAX_ENTRIES_TO_STORE = 100 #maximum stored parts bitmaps < MAX_ENTRIES_TO_STORE and maximum stored chs bitmaps < MAX_ENTRIES_TO_STORE

	class << self
#	alias_method :create_chs_character_bitmap_optimized, :create_chs_character_bitmap
	def create_chs_character_bitmap(character)
		chs_data=$chs_data[character.chs_type]
		chs_parts=chs_data.parts[character.charset_index]

		arr = [character.sex_mode?]
		chs_parts.each{|part|
			begin
			next if part.nil?
			part.update(character)
			next if part.bitmap.nil?
			rescue => err
				p "chs missing file err.message=>#{err.message}"
			end
			part_opacity = part.opacity
			part_opacity = 255 if part.opacity>255
			arr << [part.bitmap, part_opacity] if part.bitmap && part_opacity>0
		}
		@last_parts = {} if @last_parts.nil?
		oldArr = @last_parts[character]
		if arr != oldArr then
			@last_parts[character] = arr
			return apply_parts(arr, chs_data.cell_width, chs_data.cell_height, chs_data.sex_cell_width, chs_data.sex_cell_height)
		end
		return @chs_chars[character.chs_name]
	end
	def apply_parts(parts_arr, cell_width, cell_height, sex_cell_width, sex_cell_height)
		sex_mode = parts_arr[0]
		parts = parts_arr[1...parts_arr.length]
		if sex_mode
			chs_bitmap=Bitmap.new(sex_cell_width*3,sex_cell_height*8)
		else
			chs_bitmap=Bitmap.new(cell_width*12,cell_height*8)
		end
		parts.each{
			|part|
			part_bitmap=part[0]
			chs_bitmap.blt(0,0,part_bitmap,part_bitmap.rect,part[1])
		}
		return chs_bitmap
	end
	alias_method :clear_chs_character_optimization, :clear_chs_character
	def clear_chs_character(regenerate=false)
		clear_chs_character_optimization(regenerate)
		@last_parts = {}
	end

	#parts bmps should be cached for longer time
	alias :clear_chs_material_optimized :clear_chs_material
	def clear_chs_material(gc=false)
		@chs_material ||={}
		@parts_cache ||={}
		clear_chs_material_optimized if @chs_material.length > MAX_ENTRIES_TO_STORE || @parts_cache.length > MAX_ENTRIES_TO_STORE
	end
	

	end
end

end