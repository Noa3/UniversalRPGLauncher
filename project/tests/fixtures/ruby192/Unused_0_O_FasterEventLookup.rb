#Optimization mod by Teravisor

#Hash for (x,y)=>event[]
#Adds a hashmap for (x,y) position as key into Array of events for fast lookup
#Adds hook to movement methods in order to update position in hashmap

unless Game_Map.instance_methods(false).include?(:add_event_to_pos_hash)

unless $Version
#5_DataManager is not available at this point so need to read game version from rvdata2
$Version = /([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)/.match(load_data("Data/System.rvdata2").game_title)
#$VersionNumber supports up to two numbers in each version through dot, so it can be 99.99.99.99 -> 99999999
#Common version numbers like 0.4.3.2 become 00040302
#0 is unidentified version. Fixes can decide if they should run or it's better to not run on unknown versions.
$VersionNumber = $Version.nil? ? 0 : $Version[4].to_i+ $Version[3].to_i*100+$Version[2].to_i*10000+$Version[1].to_i*1000000
end

#0.4.3.2 or later. There's a lot of traps and some random placed NPCs that first init themselves as npc then delete themselves if there's something in their tile. So they delete themselves.
if $VersionNumber==0 || $VersionNumber >= 40302 then

class Game_Map
	attr_accessor :pos_to_event

	def add_event_to_pos_hash(event)
		if has_event?(event.x,event.y) then
			@pos_to_event[hash_func(event.x,event.y)].push(event)
		else
			@pos_to_event[hash_func(event.x,event.y)]=[event]
		end
		return event
	end
	
	def has_event?(x,y)
		!@pos_to_event[hash_func(x,y)].nil? && @pos_to_event[hash_func(x,y)].length>0
	end

	def eventarray_xy(x,y)
		@pos_to_event[hash_func(x,y)] || []
	end

	def delete_event_from_pos_hash(event)
		res=@pos_to_event[hash_func(event.x,event.y)].delete(event) if has_event?(event.x,event.y)
		@pos_to_event.delete(hash_func(event.x,event.y)) if !@pos_to_event[hash_func(event.x,event.y)].nil? && @pos_to_event[hash_func(event.x,event.y)].empty?
		return res
	end
	
	def TERFIX2_remove_pos_to_event (event)
		elem = $game_map.delete_event_from_pos_hash(event)
		if elem.nil? then #when characters spawn, moveto sees previous coordinates (0,0) and cannot properly remove previous hash.
			success = $game_map.delete_event_from_pos_hash_erroneous(event)
		end
	end
	
	#looks through whole array to delete specified event. Should be called as few times as possible.
	def delete_event_from_pos_hash_erroneous(event)
		@pos_to_event.each_value{|arr| arr.delete(event) if !arr.nil?}
	end

	def reload_events_into_pos_hash
		@pos_to_event = {}
		@events.each_value do |event|
			add_event_to_pos_hash(event)
		end
	end
	#All methods below are overrides

	alias_method :initialize_fixTER2, :initialize
	def initialize
		initialize_fixTER2
		@pos_to_event = Hash.new
	end
	
	alias_method :setup_fixTER2, :setup
	def setup(map_id)
		setup_fixTER2(map_id)
		reload_events_into_pos_hash
	end
	
	def events_xy(x,y)
		eventarray_xy(x,y).select{|event| !event.nil? && !event.deleted?}
	end

	def events_xy_nt(x, y)
		eventarray_xy(x,y).select {|event| !event.deleted? && !event.through }
	end

	alias_method :add_event_TERFIX2, :add_event
	def add_event(mapid, eventid, x, y)
		#Copied from 31_Game_Map.rb, but this will make non-unique IDs if you try to delete key. Be extra careful and will need fixing if ever fixed in original.
#		eventid=@events.keys.max
#		eventid=new_event_id.nil? ? 1 : new_event_id+1
		#var = add_event_TERFIX2(mapid, eventid, x, y)
		#p var.class #DEBUG, maybe I can do less calls if this works
		event_itself = add_event_TERFIX2(mapid, eventid, x, y)
#		event_itself = @events[eventid]
		add_event_to_pos_hash(event_itself)
	end

	alias_method :summon_event_TERFIX2, :summon_event
	def summon_event(event_name,x=nil,y=nil,id= -1,data=nil)
		event_itself = summon_event_TERFIX2(event_name,x,y,id,data)
		add_event_to_pos_hash(event_itself)
	end

	
	#Simple hash function to check which hash to put object in. Result is integer for speed.
	def hash_func(x,y)
		(x << 16) + y
	end

	#events that are going to be deleted, code taken from 31_Game_Map.rb update_events
	alias_method :update_events_terFIX2, :update_events
	def update_events
		@events.keys.each{
		|key| 
			event=@events[key]
			if !event.nil? && event.deleted? 
				if event.foreign_event || !event.nappable? || !event.reset_strong
					delete_event_from_pos_hash(event)
				end
			end
		}
		update_events_terFIX2
	end

	alias_method :refresh_terFIX2, :refresh
	def refresh
		refresh_terFIX2
		reload_events_into_pos_hash
	end
end

#things that move character now need to change pos_to_event
#overrides Game_Character methods for Game_Event because Game_Player is not event and must not be placed into pos_to_event
class Game_Event < Game_Character
	alias_method :move_straight_TERFIX2, :move_straight
	def move_straight(d, turn_ok = true)
		$game_map.TERFIX2_remove_pos_to_event(self)
		#prevX = @x
		#prevY = @y
		move_straight_TERFIX2(d,turn_ok)
		#newX = @x
		#newY = @y
		$game_map.add_event_to_pos_hash(self)
	end

	alias_method :moveto_TERFIX2, :moveto
	def moveto(x, y)
		$game_map.TERFIX2_remove_pos_to_event(self)
		moveto_TERFIX2(x,y)
		$game_map.add_event_to_pos_hash(self)
	end

	alias_method :move_diagonal_TERFIX2, :move_diagonal
	def move_diagonal(horz, vert)
		$game_map.TERFIX2_remove_pos_to_event(self)
		move_diagonal_TERFIX2(horz, vert)
		$game_map.add_event_to_pos_hash(self)
	end
	alias_method :jump_TERFIX2, :jump
	def jump(x_plus, y_plus,tmpPeak=10)
		$game_map.TERFIX2_remove_pos_to_event(self)
		jump_TERFIX2(x_plus, y_plus, tmpPeak)
		$game_map.add_event_to_pos_hash(self)
	end
	alias_method :jump_low_TERFIX2, :jump_low
	def jump_low(x_plus, y_plus)
		$game_map.TERFIX2_remove_pos_to_event(self)
		jump_low_TERFIX2(x_plus, y_plus)
		$game_map.add_event_to_pos_hash(self)
	end
end

end


end