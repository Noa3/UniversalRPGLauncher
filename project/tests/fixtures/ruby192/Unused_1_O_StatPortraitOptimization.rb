#Optimization mod by Teravisor

#Changes logic of portrait parts stats: when portrait stat changes instead of looping through all possible portrait parts, only loop through all parts that use that stat.

#Must load after 0_O_StatCheckOptimization.rb

unless $statPortraitOptimize || !$statsOptimized
$statPortraitOptimize=true

class Lona_Portrait < Moveable_Portrait
	def optimized_check #for checking if this optimization is already installed
	end

	alias_method :initialize_optimization, :initialize
	def initialize(base_char,parts,defPose,name_order,canvas=[300,400,0,0])
		initialize_optimization(base_char, parts, defPose, name_order, canvas)
		setup_parts_hashes
	end

	def refresh
		base_char.stat.each{|k,v|
					set_stat(k,v)
				}
	end

	def setup_parts_hashes
		if @stat_to_prts.nil? then#hash for fast search of which parts might be added or removed on that set_stat call
			@stat_to_prts = {}
		else
			@stat_to_prts.clear
		end
		@allprts = @prts
		@dirtPart = nil
		@prts = []#parts currently in portrait. Array for iteration.
		@parts_changed = false #do we need to reassemble portrait?
		@allprts.each{|prt|
					if prt.isDirt && prt.dirtKey != ""
						if @stat_to_prts[prt.dirtKey].nil? then
							@stat_to_prts[prt.dirtKey] = [prt]
						else
							@stat_to_prts[prt.dirtKey] << prt
						end
					end
					prt.set_name_order(@name_order)
					prt.nameOrder.each{|statName|

					if @stat_to_prts[statName].nil? then
						@stat_to_prts[statName] = [prt] 
					else
						@stat_to_prts[statName] << prt
					end
					prt.set_value(statName, base_char.stat[statName])
					if !prt.bitmap.nil?
						@prts << prt
						@parts_changed=true
					end
				}
			}
	end
	
	def set_stat(statName, value)
		if value.nil? then
			value = 0
		end
		if statName == 'dirt'.freeze && @dirt_level != value then
			@dirt_level = value
			@parts_changed=true
		end
		arr = @stat_to_prts[statName]
		if !arr.nil? then
			arr.each{|prt|
					prtOldBitmap = prt.bitmap
					prt.set_value(statName,value)
					prtNewBitmap = prt.bitmap
					if prtNewBitmap.nil? && @prts.include?(prt)
						@prts.delete(prt)
						@parts_changed = true
					elsif !prtNewBitmap.nil? && !@prts.include?(prt)
						sorted_insert(@prts, prt)
						@parts_changed = true
					elsif !prtNewBitmap.nil?
						if prtNewBitmap != prtOldBitmap
							@parts_changed=true
						end
					end
				}
		end
	end

	def test
		p "Current parts: "
		@prts.each{|prt| p "#{prt.part_name}" }
		p "All parts: "
		@allprts.each{|prt| p "#{prt.part_name} #{prt.nameOrder} #{prt.prtArrKey} #{prt.bitmaps}"}
	end


	def sorted_insert(arr, elem)
		for i in 0...arr.length
			if arr[i].layer >= elem.layer
				arr.insert(i, elem)
				return
			end
		end
		arr << elem
	end

	def update_parts
	end

	alias_method :updatePose_PortraitOptimize, :updatePose
	def updatePose
		return if pose.eql?(@pose)
		updatePose_PortraitOptimize
		setup_parts_hashes
		set_stat('dirt', @dirt_level)
	end  
	
	def assemble_portrait
		return if !@parts_changed
		super
		@parts_changed = false
	end
end

#debug
def porttst
	$game_player.actor.portrait.test
end

class Lona_Part < Portrait_Part
	attr_reader :nameOrder
	attr_reader :bitmaps
	attr_reader :prtArrKey
	attr_reader :isDirt
	attr_accessor :opacity

	alias_method :initialize_optimization, :initialize
	def initialize(bmps,psx,psy,layer=255,name="default",stat="default",isDirt=false)
		initialize_optimization(bmps,psx,psy,layer,name,stat,isDirt)
        end

	def set_name_order(name_order)
		@prtArrKey = Array.new(name_order[@part_name].length)
		@nameOrder = name_order[@part_name]
	end
	def set_value(key, value)
		if key == dirtKey && @isDirt then
			value = 0 if value == false
			value = 255 if value == true
			dirtLevel = value.to_i
			dirtLevel = 255 if dirtLevel>255
			@opacity = dirtLevel
		else
			for i in 0...@nameOrder.length
				if @nameOrder[i]==key then
					@prtArrKey[i] = 0 if value.nil?
					if value.instance_of?(String)
						@prtArrKey[i]=value
					else
						@prtArrKey[i]=value.to_i
					end
					break
	      			end
			end
		end
		@bitmap = get_bitmap(@prtArrKey)
	end

	def update(ref,name_order)
	end
end


end
