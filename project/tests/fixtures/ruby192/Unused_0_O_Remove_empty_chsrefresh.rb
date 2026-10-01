#Optimization mod by Teravisor

#Removes CHS rebuild call after exiting menu. Might be obsolette because of other performance fixes.

#Quite expensive call, so remove it where I can.
class Game_Player
	
	attr_accessor :cancel_refresh
	@cancel_refresh = false

	def refresh_chs
		Cache.chs_character(self,true) if !@cancel_refresh
		cancel_refresh = false
		@chs_need_update=true
		actor.stat_changed=false
	end
end

#equipment menu already refreshes chs so no need to refresh it again after exiting menu
class Scene_Menu < Scene_MenuBase
	alias_method :update_input_Optimization, :update_input
	def update_input
		update_input_Optimization
		if SceneManager.scene.is_a?(Scene_Map) then
			$game_player.cancel_refresh=true
		end
	end
end