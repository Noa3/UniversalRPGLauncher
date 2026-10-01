
module TKG::ErrorLog::Extend_GameTroop
  def conditions_met?(page)
    result = super(page)
    if result
      @interpreter.troop_page = $game_troop.troop.pages.index(page) + 1
    end
    return result
  end
end


class Game_Troop
	unless private_method_defined?('_tkg_debuglog__initialize')
		alias _tkg_debuglog__initialize initialize
	end
	def initialize(*args)
		_tkg_debuglog__initialize(*args)
		self.extend TKG::ErrorLog::Extend_GameTroop
	end
end


module GIM_OVC
	def check_lona_way_of_death(tmpForcedWay=nil)
	end #check_lona_way_of_death
end
#$game_player.actor.last_attacker = $game_player ; $game_player.actor.last_attacker.actor.last_used_skill = "asdasdasd"; $game_player.actor.health = -100
