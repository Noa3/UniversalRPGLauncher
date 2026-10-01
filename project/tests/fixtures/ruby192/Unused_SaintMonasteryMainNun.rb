if $game_map.threat
	SndLib.sys_buzzer
	$game_map.popup(0,"QuickMsg:Lona/incombat#{rand(2)}",0,0)
	return
end

	call_msg("TagMapSaintMonastery:MainNun/begin#{rand(3)}")

	manual_barters("SaintMonasteryMainNun")
			
#SndLib.sound_QuickDialog
#call_msg_popup("TagMapSaintMonastery:healer/Qmsg#{rand(4)}",get_character(0).id)
eventPlayEnd
