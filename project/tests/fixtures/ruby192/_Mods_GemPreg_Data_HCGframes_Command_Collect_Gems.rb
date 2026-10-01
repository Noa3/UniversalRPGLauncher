

# gem preg
$gp_dbgVerbose = false
$gp_dbgVerbose = true
def gemPreg_getCounter()
    tmpAllOs = $story_stats["sex_record_orgasm_Shame"] + $story_stats["sex_record_orgasm"] + $story_stats["sex_record_orgasm_Mouth"] + $story_stats["sex_record_orgasm_Torture"] + $story_stats["sex_record_orgasm_Vag"] + $story_stats["sex_record_orgasm_Milking"] + $story_stats["sex_record_orgasm_Pee"] + $story_stats["sex_record_orgasm_Poo"] + $story_stats["sex_record_orgasm_Birth"] + $story_stats["sex_record_orgasm_Anal"] + $story_stats["sex_record_orgasm_Semen"] + $story_stats["sex_record_orgasm_Breast"]
    tmpCounter = $story_stats["sex_record_orgasm"] + tmpAllOs + $story_stats["sex_record_semen_swallowed"] + $story_stats["sex_record_cumin_anal"] + $story_stats["sex_record_cumin_vaginal"] + $story_stats["sex_record_cumin_mouth"]
    return tmpCounter
end
def init_gemPreg()
    $story_stats["record_gempreg_counter_offset"] = Integer(gemPreg_getCounter() *0.9)
    p "  gemPreg.: initilized with #{$story_stats['record_gempreg_counter_offset']} offset."
end
def gemPreg_getCrystalValue()
    tmpCrystalValue = gemPreg_getCounter() - $story_stats["record_gempreg_counter_offset"]
    if tmpCrystalValue < 0 then
        p "gemPreg.: error, crystal-value is negative?? (#{tmpCrystalValue}, offset was: #{$story_stats['record_gempreg_counter_offset']}) resetting offset... " 
        init_gemPreg()
    elsif $gp_dbgVerbose then
        p "gemPreg. (debug): crystal-value is #{tmpCrystalValue}, offset was  #{$story_stats['record_gempreg_counter_offset']}." 
    end
    return tmpCrystalValue
end

def gemPreg_can_excrete_gems()    
    if $story_stats["record_gempreg_counter_offset"].nil? or $story_stats["record_gempreg_counter_offset"] == 0 then
        $story_stats["record_gempreg_counter_offset"] = 0
        init_gemPreg()
    elsif $gp_dbgVerbose then
        p "  gemPreg. (debug): story-stat is not nil #{$story_stats['record_gempreg_counter_offset']}"
    end
    tmpCrystalValue = gemPreg_getCrystalValue()
    if gemPreg_getCrystalValue >= 5
        return true
    else
        return false
    end
end

$gp_LargeCrystalWorth = 100
$gp_CommonCrystalWorth = 20
$gp_TinyCrystalWorth = 5

def gemPreg_calculateGemSizes()
    tmpCrystalValue = gemPreg_getCrystalValue()
    num_large_gems = Integer([0, tmpCrystalValue / $gp_LargeCrystalWorth].max)
    _remaining_levels = tmpCrystalValue - num_large_gems*$gp_LargeCrystalWorth
    num_common_gems = Integer([0, (_remaining_levels / $gp_CommonCrystalWorth)].max)
    _remaining_levels -= num_common_gems*$gp_CommonCrystalWorth
    num_tiny_gems = Integer([0, _remaining_levels / $gp_TinyCrystalWorth].max)
    return num_large_gems, num_common_gems, num_tiny_gems
end

def gemPreg_update_offset(numTiny, numCommon, numLarge)
    tmpOffsetDelta = numTiny*$gp_TinyCrystalWorth + numCommon*$gp_CommonCrystalWorth + numLarge*$gp_LargeCrystalWorth
    $story_stats["record_gempreg_counter_offset"] += tmpOffsetDelta
    p "  gemPreg.: updated offset by #{tmpOffsetDelta} to #{$story_stats['record_gempreg_counter_offset']}."
end
#

if $game_player.actor.stat["AllowOgrasm"] == true then $game_player.actor.stat["allow_ograsm_record"]=true
	else 
	$game_player.actor.stat["allow_ograsm_record"] = false 
end
$game_player.actor.stat["AllowOgrasm"] = true if $game_actors[1].state_stack(105) !=0
p "Playing HCGframe : Collect Gems"
#$game_portraits.lprt.hide

def obtain_item(item,amount=1)  # just ambiguating annoying typo
    return optain_item(item, amount)
end

# load gem-pregnancy
num_large_gems = 0
num_common_gems = 0
num_tiny_gems = 0
allow_collect = gemPreg_can_excrete_gems()
def gempreg_give_gems(num_tiny_gems=0, num_common_gems=0, num_large_gems=0)
    if num_large_gems >= 1
        obtain_item("ItemGem3", num_large_gems)
        p "  gemPreg.: just gave #{num_large_gems} large gems..."
    end
    if num_common_gems >= 1
        obtain_item("ItemGem2", num_common_gems)
        p "  gemPreg.: just gave #{num_common_gems} common gems..."
    end
    if num_tiny_gems >= 1
        obtain_item("ItemGem1", num_tiny_gems)
        p "  gemPreg.: just gave #{num_tiny_gems} tiny gems..."
    end
	p "  gemPreg.: should be done giving items"
end
def do_HCG_things()  # from Command_Collect_Excretion
	!equip_slot_removetable?(8) ? equips_8_id = -1 : equips_8_id = $game_player.actor.equips[8].id #Head
	!equip_slot_removetable?(6) ? equips_6_id = -1 : equips_6_id = $game_player.actor.equips[6].id #BELT
	!equip_slot_removetable?(2) ? equips_2_id = -1 : equips_2_id = $game_player.actor.equips[2].id #TOP
	!equip_slot_removetable?(4) ? equips_4_id = -1 : equips_4_id = $game_player.actor.equips[4].id #BOT
	!equip_slot_removetable?(3) ? equips_3_id = -1 : equips_3_id = $game_player.actor.equips[3].id #MID
	!equip_slot_removetable?(5) ? equips_5_id = -1 : equips_5_id = $game_player.actor.equips[5].id #TOP EXT
	!equip_slot_removetable?(0) ? equips_0_id = -1 : equips_0_id = $game_player.actor.equips[0].id #Weapon

    tmp_fucker_id = nil
    $game_map.npcs.each do |event| 
        next if event.summon_data == nil
        #next if event.summon_data[:NapFucker] == nil
        #next if !event.summon_data[:NapFucker]
        next if event.npc.target != nil
        next if !["Human","Moot","Deepone"].include?(event.npc.race)
        next if event.npc.friendly?($game_player) || event.npc.master == $game_player
        next if event.actor.action_state != nil && event.actor.action_state !=:none
        next if !event.near_the_target?($game_player,5)
        next if !event.actor.target.nil?
        next if event.opacity != 255
        next if event.actor.sensors[0].get_signal(event,$game_player)[2] <=15 #[target,distance,signal_strength,sensortype]
        event.summon_data[:NapFucker] = false
        tmp_fucker_id = event.id
    end
    
    $story_stats["dialog_dress_out"] = 0
    if !$game_player.innocent_spotted? || !tmp_fucker_id.nil?
        $game_message.add("\\t[commonCommands:Lona/Excretion_begin2]")
        $game_map.interpreter.wait_for_message
    else
    $game_message.add("\\t[commonCommands:Lona/Bath_begin3_FuckerSight#{talk_style}]") if $game_player.actor.stat["Exhibitionism"] !=1
    $game_message.add("\\t[commonCommands:Lona/Bath_begin3_FuckerSight_slut]") if $game_player.actor.stat["Exhibitionism"] ==1
        $game_map.interpreter.wait_for_message
        case $game_player.actor.stat["persona"] #主角處於視線下 檢測PERSONA來決定屬性變化
            when "typical"
                $game_player.actor.mood -=rand(10)+5
            when "gloomy"
                $game_player.actor.mood -=rand(5)+3
            when "tsundere"
                $game_player.actor.mood -=rand(10)+20
            when "slut"
                $game_player.actor.mood +=rand(10)+5
        end
        $game_player.actor.mood +=rand(10)+20 if $game_player.actor.stat["Exhibitionism"] ==1
    end
    chcg_background_color(0,0,0,0,7)
		if equips_6_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip(6, nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_2_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip(2, nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_4_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip(4, nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end

    
    # ----------------
	$game_map.interpreter.wait_for_message
	
	##############################################################normal on#################################################################
	$cg = TempCG.new(["event_GroinClearn"])
	lona_mood "flirty"
	#stats_batch3
	#stats_batch4
	#stats_batch5
	#stats_batch6
	#stats_batch7
	#message control
	$game_message.add("\\t[commonH:Lona/MilkSpray#{rand(5)}]")
	#$game_map.interpreter.wait_for_message
	##############################################################normal off#################################################################
	lona_mood "flirty"
	#stats_batch3
	#stats_batch4
	#stats_batch5
	#stats_batch6
	#stats_batch7
	#message control
	$game_message.add("\\t[commonH:Lona/MilkSpray#{rand(5)}]")
	$game_map.interpreter.wait_for_message
	
	##################################################	
	##################################################	
    
    if tmp_fucker_id == nil
		if equips_4_id != -1#檢查裝備 並穿裝
			$game_player.actor.change_equip(4, $data_armors[equips_4_id])
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_2_id != -1#檢查裝備 並穿裝
			$game_player.actor.change_equip(2, $data_armors[equips_2_id])
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_6_id != -1#檢查裝備 並穿裝
			$game_player.actor.change_equip(6, $data_armors[equips_6_id])
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end

    else # not nil
        get_character(tmp_fucker_id).opacity = 255
        get_character(tmp_fucker_id).animation = nil
        get_character(tmp_fucker_id).npc.stat.set_stat("mood",0) #so they dont yell for help
        get_character(tmp_fucker_id).call_balloon(5)
        wait(80)
        get_character(tmp_fucker_id).turn_toward_character($game_player)
        get_character(tmp_fucker_id).actor.set_aggro($game_player.actor,$data_arpgskills["BasicNormal"],300)
    end

end

if allow_collect then
    num_large_gems, num_common_gems, num_tiny_gems = gemPreg_calculateGemSizes()
    p "  gemPreg.: level was #{gemPreg_getCrystalValue()}, which will yield #{num_large_gems} large, #{num_common_gems} common, and #{num_tiny_gems} tiny gems."
    do_HCG_things()
    gempreg_give_gems(num_tiny_gems, num_common_gems, num_large_gems)
    gemPreg_update_offset(num_tiny_gems, num_common_gems, num_large_gems)
else
    p "  gemPreg.: too less crystals. Can't collect. Abort"
    call_msg($game_text_GemPreg["GemPreg_Text:GemPreg/TooLessCrystals"])
end
# done gem-pregnancy

$game_player.actor.stat["EventTargetPart"] = nil
$game_player.actor.stat["AllowOgrasm"] = false if $game_player.actor.stat["allow_ograsm_record"] == false
