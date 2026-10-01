# frozen_string_literal: true
class Scene_MapTitle
  @@mod_achievements_shown = false
  alias_method :start_UMM, :start

  def start
    start_UMM
    @new_mods = $mod_manager.new_mods.dup
  end

  alias_method :updateAchievement_UMM, :updateAchievement

  def updateAchievement
    unless @achNeedUpdate || @new_mods.empty? || @@mod_achievements_shown
      mod = @new_mods.pop
      achievementPopup($game_text["umm:umm:achievement/top_text"], mod.localized_name)
    end
    updateAchievement_UMM
  end

  alias_method :pre_terminate_UMM, :pre_terminate
  def pre_terminate
    @@mod_achievements_shown = true
    @tmpSprite.dispose unless @tmpSprite.nil? || @tmpSprite.disposed?
  end
end
