module DataManager
	class << self
		alias_method :update_Lang_UMM, :update_Lang
		def update_Lang
			update_Lang_UMM
			$mod_manager.link_texts
		end

		alias_method :load_mod_game_objects_UMM, :load_mod_game_objects
		def load_mod_game_objects
			load_mod_game_objects_UMM
			update_Lang
		end
	end
end
