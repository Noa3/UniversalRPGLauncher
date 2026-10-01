module DataManager
	def self.patch_old_save
		gameCurrVer = translate_game_ver(DataManager.export_full_ver_info)
		gameSaveVer = translate_game_ver($story_stats["VerInfo"])
		lowestSuppotVer = 0.79
		if gameSaveVer < lowestSuppotVer
			return msgbox "ERROR > Lowest patchable ver = #{lowestSuppotVer}"
			SceneManager.exit
		end
		patchedMsg = ""
		patchedMsg += $game_player.patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg += $game_player.actor.patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg += $game_map.patch_old_save(gameCurrVer,gameSaveVer)
		if patchedMsg != ""
			boardMsg = "\\board[SavePatched]"
			$game_message.add(boardMsg)
			$game_message.add("\\C[6]Please reset map ASAP\\C[0]\\n")
			$game_message.add(patchedMsg)
			$game_temp.loadEval("$game_map.interpreter.wait_for_message")
		end
	end
end
class Scene_Load < Scene_File
	alias_method :on_load_success_fix, :on_load_success
	def on_load_success
		on_load_success_fix
		DataManager.patch_old_save
	end
end

class Scene_CustomModeLoad < Scene_MenuBase
	alias_method :on_load_success_fix, :on_load_success
	def on_load_success
		on_load_success_fix
		DataManager.patch_old_save
	end
end

#########################################
#map
class Game_Map
	def patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg = ""
		if 1.003 > gameSaveVer
			$game_map.npcs.each{|event|
			 event.npc.stat.default_stat.each{|id,val|
					event.npc.stat.default_stat[id][5] =0
					event.npc.stat.default_stat[id][6] =0
					event.npc.stat.default_stat[id][7] =true
					p "#{id} #{val}"
				}
			 event.npc.stat.stat.each{|id,val|
					event.npc.stat.stat[id][5] =0
					event.npc.stat.stat[id][6] =0
					event.npc.stat.stat[id][7] =true
					p "#{id} #{val}"
				}
			}
			patchedMsg += "Add NPC BTmax, BTmin\\n"
		end
		patchedMsg
	end
end
#GP
class Game_Player < Game_Character
	def patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg = ""
		#if !@stepSndCount
		#	@stepSndCount = 0
		#	patchedMsg += "save patch, 0860 add stepSndCount to player class\\n"
		#end
		patchedMsg
	end
end

#GP.a
class Game_Actor < Game_Battler
	def patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg = ""
		
		#if 1.06 > gameSaveVer && self.bladder_fullness == 0
		#	#self.stat["bladder_fullness"] = 0
		#	#self.stat["bowel_fullness"] = 0
		#	#self.actStat.patch_single_stat("bladder_fullness",[50, 0, 100, 100, 0, false])
		#	#self.actStat.patch_single_stat("bowel_fullness",[50, 0, 100, 100, 0, false])
		#	patchedMsg += "save patch to 01060 init bowel_fullness, bladder_fullness.\\n"
		#end
		if 1.003 > gameSaveVer
			#self.clear_states

			#equip_slots.size.times do |i|
				#@equips[i] = nil
			#end
		#	$game_player.actor.actStat.default_stat{|id,val|
		#		$game_player.actor.actStat.default_stat[id][5]=0
		#		$game_player.actor.actStat.default_stat[id][6]=0
		#		#$game_player.actor.actStat.default_stat[id][7]=false
		#	}
		#	$game_player.actor.actStat.stat.each{|id,val|
		#		$game_player.actor.actStat.stat[id][5]=0
		#		$game_player.actor.actStat.stat[id][6]=0
		#		$game_player.actor.actStat.stat[id][7]=false
		#	}

			asd = $game_player.actor.actStat.stat.clone
			@actStat=LonaActorStat.new
			$game_player.actor.actStat.stat.each{|id,val|
				$game_player.actor.actStat.stat[id]=asd[id].clone
			}
			asd = nil
			$game_player.actor.actStat.update_1003
			patchedMsg += "Add Player BTmax, BTmin\\n"
		end

		if 1.02 > gameSaveVer
			self.stat["SacredAegis"] = 0
			patchedMsg += "save patch to 01020 init SacredAegis trait.\\n"
		end
		if 1.03 > gameSaveVer
			@equips[17] = Game_BaseItem.new
			@equips[18] = Game_BaseItem.new
			@equips[19] = Game_BaseItem.new
			patchedMsg += "save patch to 01030 init eqp slot 17~19.\\n"
		end
		patchedMsg
	end
end

class LonaActorStat < ActorStat #1.003 patch only
	def update_1003

		@stat.keys.each{|key|
			@stat[key][BUFF_TMIN]=0
			@stat[key][BUFF_TMAX]=0
		}
	end
end
