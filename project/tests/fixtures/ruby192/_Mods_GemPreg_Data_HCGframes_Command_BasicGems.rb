if $game_map.threat
	SndLib.sys_buzzer
	$game_map.popup(0,"QuickMsg:Lona/incombat#{rand(2)}",0,0)
	return
end
$game_portraits.lprt.hide
	tmpCanMasturbate = !$game_player.player_cuffed? && $game_player.actor.sta > 0 #&& able_to_mas_count >=1
	tmpCanCollectGems = $game_player.actor.stat["TraitGemPreg"] == 1
	tmpPicked = ""
	tmpQuestList = []
	tmpQuestList << [$game_text["commonCommands:Lona/BasicNeedsOpt_Cancel"]			,"BasicNeedsOpt_Cancel"]
	tmpQuestList << [$game_text["commonCommands:Lona/BasicNeedsOpt_Masturbate"]		,"BasicNeedsOpt_Masturbate"]		if tmpCanMasturbate
	tmpQuestList << [$game_text_GemPreg["GemPreg_Text:GemPreg/BasicNeedsOpt_CollectGems"]	,"BasicNeedsOpt_CollectGems"]		if tmpCanCollectGems && !$game_player.player_cuffed?
	cmd_sheet = tmpQuestList
	cmd_text =""
	for i in 0...cmd_sheet.length
		cmd_text.concat(cmd_sheet[i].first+",")
	end
	call_msg("commonCommands:Lona/BasicNeeds_begin",0,2,0)
	call_msg("\\optB[#{cmd_text}]")
	$game_temp.choice == -1 ? tmpPicked = false : tmpPicked = cmd_sheet[$game_temp.choice][1]
	$game_temp.choice = -1
	action_blocked = false
	
	case tmpPicked
		when "BasicNeedsOpt_Masturbate";		action_blocked = true if !tmpCanMasturbate
												load_script("Data/HCGframes/Action_CHSH_Masturbation.rb") if !action_blocked
		when "BasicNeedsOpt_CollectGems";		load_script("ModScripts/_Mods/GemPreg/Data/HCGframes/Command_Collect_Gems.rb")
	end

if action_blocked
	SndLib.sys_buzzer
	$game_map.popup(0,"CompElise:tar/Failed_enter",0,0)
end

eventPlayEnd
