#note
#key,  all maunal_trade
#all item,state, id to name
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
			#$story_stats["VerInfo"] = export_full_ver_info # move to  $game_map.setup(map_id), only successed reset map can upgrade game ver
			#$story_stats["VerInfo"] = export_full_ver_info
			boardMsg = "\\board[SavePatched]"
			$game_message.add(boardMsg)
			$game_message.add("\\C[6]Please reset map ASAP\\C[0]\\n")
			$game_message.add(patchedMsg)
			$game_temp.loadEval("$game_map.interpreter.wait_for_message")
		end
	end
	#def self.popup_patch_msg(data)
	#	$game_temp.loadMSG(patchedMsg)
	#	$game_temp.load_MSG = patchedMsg
	#	$game_temp.reserve_common_event(9)
	#end
end
#class Game_Map
#	alias_method :setup_patch_save, :setup
#	def setup_patch_save(map_id)
#		setup_patch_save(map_id)
#		msgbox
#	end
#end
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
		if 0.9804 > gameSaveVer
			patchedMsg += "0.9804 to #{gameCurrVer}, Reset HostageSaved Data\\n add weather BG_EFX_data\\n"
			$story_stats["HostageSaved"] = Hash.new(0) if $story_stats["HostageSaved"].is_a?(Numeric)
			$story_stats["current_weather"] = nil
			$story_stats["current_shadow"] = nil
			$story_stats["BG_EFX_data"] = []
		end
		if 0.9501 > gameSaveVer
			$story_stats["CharacterItems"] = Hash.new
			patchedMsg += "0.9501 to #{gameCurrVer}, Reset Store Data\\n"
		end
		if 0.95 > gameSaveVer
			@parallax_name = nil if @parallax_name == ""
			patchedMsg += "0.95 to #{gameCurrVer}, @parallax_name fix\\n"
		end
		if !$game_map.forced_block_dir
			$game_map.forced_block_dir = []
			patchedMsg += "save 0930 game_map.forced_block_dir added\\n"
		end
		tmp0920immune_tgt_states = $game_map.npcs.any?{|event|
				!event.npc.immune_tgt_states
			}
		if tmp0920immune_tgt_states
			$game_map.npcs.each{|event|
				next if event.npc.immune_tgt_states
				event.npc.immune_tgt_states = []
				event.npc.immune_tgt_state_type = []
			}
			patchedMsg += "save 0920 NPC immune_tgt_states added\\n"
		end
		patchedMsg
	end
end
#GP 
class Game_Player < Game_Character
	def patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg = ""
		if !@stepSndCount
			@stepSndCount = 0
			patchedMsg += "save patch, 0860 add stepSndCount to player class\\n"
		end
		patchedMsg
	end
end

#GP.a
class Game_Actor < Game_Battler
	def patch_old_save(gameCurrVer,gameSaveVer)
		patchedMsg = ""
		if 0.96 > gameSaveVer || !self.banned_receiver_holes
			self.banned_receiver_holes = []
			patchedMsg += "0.96 to #{gameCurrVer}, Missing actor.banned_receiver_holes\\n"
		end
		if 0.95 > gameSaveVer || !$game_switches.data_common_event
			$game_switches.init_common_event
			patchedMsg += "0.95 to #{gameCurrVer}, CommonEV to hash\\n"
		end
		if 0.95 > gameSaveVer
			@ext_items = [nil,nil,nil,nil,nil]
			patchedMsg += "0.95 to #{gameCurrVer}, empty EXT slots\\n"
		end
		if !$story_stats["sex_record_race_count"].is_a?(Hash)
			$story_stats["sex_record_race_count"]			= Hash.new(0)
			patchedMsg += "save patch to 0950 sex_record_race_count\\n"
		end
		if !$story_stats["sex_record_first_vag"] || !$story_stats["sex_record_first_vag"].is_a?(Array)
			$story_stats["sex_record_first_mouth"]				= Array.new
			$story_stats["sex_record_first_vag"]				= Array.new
			$story_stats["sex_record_first_anal"]				= Array.new
			$story_stats["sex_record_last_mouth"]				= Array.new
			$story_stats["sex_record_last_vag"]					= Array.new
			$story_stats["sex_record_last_anal"]				= Array.new
			$story_stats["sex_record_partner_count"]			= Hash.new(0)
			patchedMsg += "save patch to 0940 sex_record basic hole trans to Array\\n"
		end
		if !self.stat["RaceRecord"] || !self.stat["Dizzy"]
			self.stat["RaceRecord"] = @record_lona_race
			self.stat["Dizzy"] = 0
			@hom_effects_data = []
			patchedMsg += "save patch to 0930 record_lona_race renamed to stat.RaceRecord\\n"
		end
		if !@immune_tgt_states
			@immune_tgt_states = []
			@original_immune_tgt_states = []
			@immune_tgt_state_type = []
			patchedMsg += "save patch to 0920 missing stat immune_tgt_states\\n"
		end
		if !self.stat["FeelsHorniness"]
			self.stat["FeelsHorniness"] = self.stat["FeelsHot"]
			self.stat["FeelsHot"]=nil
			patchedMsg += "save patch to 0910 missing stat FeelsHorniness\\n"
		end
		if !self.stat["PregState"]
			if self.state_stack("PregState") >= 1
				self.stat["PregState"] = self.state_stack("PregState")
			else
				self.stat["PregState"] = 0
			end
			patchedMsg += "save patch to 0900 missing stat + renamed fix2\\n"
		end
		if !self.stat["Lactation"] || self.stat["lactation"]
			self.stat["Lactation"] =0
			self.stat["lactation"] = nil
			patchedMsg += "save patch to 0900 missing stat + renamed fix1\\n"
		end
		if self.dodge_frame == 0
			self.actStat.patch_single_stat("dodge_frame",[28, 0, 28, 28, 0, false])
			patchedMsg += "save patch, 0900 dodge_frame addon\\n"
		end
		
		if !@equip_part_covered
			@equip_part_covered = []
			tmpCur = self.battle_stat.get_stat("move_speed",0)
			tmpTMin = self.battle_stat.get_stat("move_speed",2)
			tmpTMax = self.battle_stat.get_stat("move_speed",3)
			self.battle_stat.set_stat_m("move_speed",tmpCur +0.3,[0])
			self.battle_stat.set_stat_m("move_speed",tmpTMin+0.3,[2])
			self.battle_stat.set_stat_m("move_speed",tmpTMax+0.3,[3])
			patchedMsg += "save patch, 0880 equip_part_covered addon\\n"
		end
		if !$game_player.actor.statMap["canvas"]["pose4"]
			$game_player.actor.init_statMap
			patchedMsg += "save patch, POSE4 canvas data not found, reinit actor pose data\\n"
		end
		if !$game_NPCLayerMain || !$game_NPCLayerSub
			patchedMsg += "save patch to 0800 Create Layers actor temp\\n"
			$game_NPCLayerMain	= Game_NPC_Actor.new("NPCLayerMain")
			$game_NPCLayerSub	= Game_NPC_Actor.new("NPCLayerSub")
			$game_player.actor.init_statMap
		end
		if !self.stat["STD_HerpesVag"] || !self.stat["STD_HerpesAnal"] || !self.stat["STD_WartVag"] || !self.stat["STD_WartAnal"]
			self.stat["STD_HerpesVag"] =0
			self.stat["STD_HerpesAnal"] =0
			self.stat["STD_WartVag"] =0
			self.stat["STD_WartAnal"] =0
			patchedMsg += "save patch to 0850 Create STD stat\\n"
		end
		if !@PubicHair_Vag_GrowCount && !@pubicHair_Vag_GrowCount
			patchedMsg += "save patch to 0783 Add PubicHair Basic Stats\\n"
			self.stat["PubicHairVag"] = 0
			self.stat["PubicHairAnal"] = 0
			@PubicHair_Vag_GrowCount = 0 #pubicHair Grow Chk, reset to 0 if grow a lvl
			@PubicHair_Vag_GrowRate = 3+rand(10) #howmany Nap to Grow a LVL
			@PubicHair_Vag_GrowMAX = 2+rand(3) #max Lvl
			@PubicHair_Anal_GrowCount = 0
			@PubicHair_Anal_GrowRate = 3+rand(10)
			@PubicHair_Anal_GrowMAX = 2+rand(3)
		elsif @PubicHair_Vag_GrowCount && !@pubicHair_Vag_GrowCount
			patchedMsg += "save patch to 0820 PubicHairFix\\n"
			@pubicHair_Vag_GrowCount = 		@PubicHair_Vag_GrowCount
			@pubicHair_Vag_GrowRate =		@PubicHair_Vag_GrowRate
			@pubicHair_Vag_GrowMAX = 		@PubicHair_Vag_GrowMAX
			@pubicHair_Anal_GrowCount =		@PubicHair_Anal_GrowCount
			@pubicHair_Anal_GrowRate = 		@PubicHair_Anal_GrowRate
			@pubicHair_Anal_GrowMAX = 		@PubicHair_Anal_GrowMAX
		end
		if @is_object.nil?
			patchedMsg += "save patch to 0770 Lona is_object fix\\n"
			@shieldEV = nil
			@immune_damage = false
			@immune_state_effect = false
			@hit_LinkToMaster = false
			@is_object = false
			@master = nil
			@is_a_ProtectShield = nil
		end
		patchedMsg
	end
end
