#Idea from Theo - Insane Anti Lag
#Adapted by Teravisor

unless Game_Event.instance_methods(false).include?(:theo_antilag_start)

unless $version
#5_DataManager is not available at this point so need to read game version from rvdata2
$Version = /([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)/.match(load_data("Data/System.rvdata2").game_title)
#$VersionNumber supports up to two numbers in each version through dot, so it can be 99.99.99.99 -> 99999999
#Common version numbers like 0.4.3.2 become 00040302
#0 is unidentified version. Fixes can decide if they should run or it's better to not run on unknown versions.
$VersionNumber = $Version.nil? ? 0 : $Version[4].to_i+ $Version[3].to_i*100+$Version[2].to_i*10000+$Version[1].to_i*1000000
end


if $VersionNumber >= 40304 then

class Game_Map
  attr_accessor :starting_events      # To store activated event

  alias_method :initialize_theo, :initialize
  def initialize
	initialize_theo
	@starting_events=[]
  end
  
	alias_method :setup_theo, :setup
	def setup(mapID)
		@starting_events=[] #remove starting events from previous map. Fixes crash when moving from sewers to OrkindCave1
		setup_theo(mapID)
	end

#copied from Theo's  
  def setup_starting_map_event
    event = @starting_events[0]
    event.clear_starting_flag if event
    @interpreter.setup(event.list, event.id) if event
    event
  end
#copied from Theo's  
  def any_event_starting?
    !@starting_events.empty?
  end
end

#copied from Theo's
class Game_Event < Game_Character

  alias theo_antilag_start start
  def start
    theo_antilag_start
    $game_map.starting_events << self if @starting
  end

  alias theo_antilag_clear_start clear_starting_flag
  def clear_starting_flag
    theo_antilag_clear_start
    $game_map.starting_events.delete(self)
  end	
end

#old saves have @starting_events = nil
module DataManager
	class << self
		alias_method :extract_save_contents_antilag, :extract_save_contents
		def extract_save_contents(contents)
			extract_save_contents_antilag(contents)
			$game_map.starting_events = [] if $game_map.starting_events.nil?
		end
	end
end


end

#Theo - Insane Anti Lag as-is code for updating only sprites on screen.
#in pirate's bane it reduced time to update sprites 0.012->0.0095 sec per frame which is quite nice.
class Sprite_Character
  #-----------------------------------------------------------------------------
  # * Alias method : Initialize
  #-----------------------------------------------------------------------------
  alias theo_antilag_init initialize
  def initialize(viewport, character = nil)
    theo_antilag_init(viewport, character)
  end
  #-----------------------------------------------------------------------------
  # * Alias method : Update
  #-----------------------------------------------------------------------------
  alias theo_antilag_update update
  def update
	return if @character.nil?
	@sx = @character.screen_x
	@sy = @character.screen_y
	if !need_update?
		self.visible = false
		return
	end
	theo_antilag_update
  end
  #-----------------------------------------------------------------------------
  # * New method : Determine if on screen
  #-----------------------------------------------------------------------------
  def need_update?
    return true if graphic_changed?
    return true if @character.animation_id > 0
    return true if @balloon_sprite
    return true if @character.balloon_id != 0
    w = Graphics.width
    h = Graphics.height
    cw = @cw || 32
    ch = @ch || 32
    @sx.between?(-cw,w+cw) && @sy.between?(0,h+ch)
  end
  #-----------------------------------------------------------------------------
  # * Overwrite update position.
  # To limit screen_x and screen_y to be called many times
  #-----------------------------------------------------------------------------
  def update_position
	move_animation(@sx - x, @sy - y)
	self.x = @sx
	self.y = @sy
	self.z = @character.screen_z

#from Scripts/Frames/206_DB_Animation.rb
	if @character.animation
		self.x += @character.animation.frame_data[3] || 0
		self.y += @character.animation.frame_data[4] || 0
	end
  end
  #-----------------------------------------------------------------------------
  # * Overwrite animation origin
  # Since X and Y axis of sprite is not updated when off screen
  #-----------------------------------------------------------------------------
  def set_animation_origin
    if @animation.position == 3
      if viewport == nil
        @ani_ox = Graphics.width / 2
        @ani_oy = Graphics.height / 2
      else
        @ani_ox = viewport.rect.width / 2
        @ani_oy = viewport.rect.height / 2
      end
    else
      @ani_ox = @sx - ox + width / 2
      @ani_oy = @sy - oy + height / 2
      if @animation.position == 0
        @ani_oy -= height / 2
      elsif @animation.position == 2
        @ani_oy += height / 2
      end
    end
  end
  
	alias_method :set_character_bitmap_Optimization, :set_character_bitmap
	def set_character_bitmap
		set_character_bitmap_Optimization
		@using_chs = @character.use_chs?
	end

end

end