p "Playing HCGframe : Command_Pee.rb"
$game_portraits.lprt.hide
if !$game_map.threat
	$game_temp.choice = -1 #還原OPTION
	temp_vag_cums =$game_player.actor.cumsMeters["CumsCreamPie"]
	temp_anal_cums =$game_player.actor.cumsMeters["CumsMoonPie"]
	temp_groin_cums = $game_player.actor.cumsMeters["CumsCreamPie"] + $game_player.actor.cumsMeters["CumsMoonPie"]
	
	total_wounds = $game_player.actor.get_total_wounds

	slotList = data_system.equip_type_name
	!equip_slot_removetable?("MH")		? equips_MH_id = -1 :		equips_MH_id =		$game_player.actor.equips[slotList["MH"]].item_name		#0
	!equip_slot_removetable?("SH")		? equips_SH_id = -1 :		equips_SH_id =		$game_player.actor.equips[slotList["SH"]].item_name		#1
	!equip_slot_removetable?("Top")		? equips_Top_id = -1 :		equips_Top_id =		$game_player.actor.equips[slotList["Top"]].item_name	#2
	!equip_slot_removetable?("Mid")		? equips_Mid_id = -1 :		equips_Mid_id =		$game_player.actor.equips[slotList["Mid"]].item_name	#3
	!equip_slot_removetable?("Bot")		? equips_Bot_id = -1 :		equips_Bot_id =		$game_player.actor.equips[slotList["Bot"]].item_name	#4
	!equip_slot_removetable?("TopExt")	? equips_TopExt_id = -1 :	equips_TopExt_id =	$game_player.actor.equips[slotList["TopExt"]].item_name	#5
	!equip_slot_removetable?("MidExt")	? equips_MidExt_id = -1 :	equips_MidExt_id =	$game_player.actor.equips[slotList["MidExt"]].item_name	#6
	!equip_slot_removetable?("Hair")	? equips_Hair_id = -1 :		equips_Hair_id =	$game_player.actor.equips[slotList["Hair"]].item_name	#7
	!equip_slot_removetable?("Head")		? equips_Head_id = -1 :		equips_Head_id =		$game_player.actor.equips[slotList["Head"]].item_name	#8
	!equip_slot_removetable?("Neck")	? equips_Neck_id = -1 :		equips_Neck_id =	$game_player.actor.equips[slotList["Neck"]].item_name	#14
	!equip_slot_removetable?("Vag")		? equips_Vag_id = -1 :		equips_Vag_id =		$game_player.actor.equips[slotList["Vag"]].item_name	#15
	!equip_slot_removetable?("Anal")	? equips_Anal_id = -1 :		equips_Anal_id =	$game_player.actor.equips[slotList["Anal"]].item_name	#16
	
	if temp_groin_cums ==0
	call_msg("commonCommands:Lona/Bath_begin1")
	else
	call_msg("commonCommands:Lona/Bath_begin3_FuckerSight#{talk_style}")
	end
	
	if $game_temp.choice == 1
		$story_stats["dialog_dress_out"] = 0
		$game_player.actor.sta -=15 #扣除本行動STA
		if !$game_player.innocent_spotted?
			$game_message.add("\\t[commonCommands:Lona/Bath_begin3_NoFuckerSight]")
			$game_map.interpreter.wait_for_message
		else
			$game_message.add("\\t[commonCommands:Lona/Bath_begin3_FuckerSight#{talk_style}]")
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
		end
		chcg_background_color(0,0,0,0,7)
		if equips_Head_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Head", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_MidExt_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("MidExt", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_Top_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Top", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_Bot_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Bot", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_Mid_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Mid", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_Vag_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Vag", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		if equips_Anal_id != -1#檢查裝備 並脫裝
			$game_player.actor.change_equip("Anal", nil)
			SndLib.sound_equip_armor(100)
			player_force_update
			wait(30)
		end
		
		if $game_player.player_cuffed? #檢查手銬
			$game_message.add("\\t[commonCommands:Lona/Bath_cuffed]")
			$game_map.interpreter.wait_for_message
		end
		
		if total_wounds ==0 #正在洗 檢測是否負傷 並執行對應狀態
			$game_message.add("\\t[commonCommands:Lona/Bath_washing]")
			$game_map.interpreter.wait_for_message
			elsif total_wounds !=0 && $game_player.actor.stat["Masochist"] !=1 #有受傷  不是M
			$game_message.add("\\t[commonCommands:Lona/Bath_washing_IfWounds]")
			$game_map.interpreter.wait_for_message
			$game_player.actor.mood -= (total_wounds*0.5).round
			elsif total_wounds !=0 && $game_player.actor.stat["Masochist"] ==1 #有受傷 是個M
			$game_message.add("\\t[commonCommands:Lona/Bath_washing_IfWoundsM]")
			$game_map.interpreter.wait_for_message
			$game_player.actor.sta +=2
			$game_player.actor.mood += (total_wounds*0.5).round
		end
		chcg_background_color(0,0,0,255,-7)
		$game_message.add("\\t[commonCommands:Lona/Bath_end]")
		$game_map.interpreter.wait_for_message
		
		if !Input.press?(:SHIFT)
			if equips_Anal_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Anal", $data_ItemName[equips_Anal_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Vag_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Vag", $data_ItemName[equips_Vag_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Mid_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Mid", $data_ItemName[equips_Mid_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Mid_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Mid", $data_ItemName[equips_Mid_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Bot_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Bot", $data_ItemName[equips_Bot_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Top_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Top", $data_ItemName[equips_Top_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_MidExt_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("MidExt", $data_ItemName[equips_MidExt_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
			if equips_Head_id != -1#檢查裝備 並穿裝
				$game_player.actor.change_equip("Head", $data_ItemName[equips_Head_id])
				SndLib.sound_equip_armor(100)
				player_force_update
				wait(30)
			end
		end
	end
	
	if $game_temp.choice == 2 #轉去執行清理下半身
	load_script("Data/HCGframes/Command_GroinClearn.rb")
	end
else
	SndLib.sys_buzzer
	$game_map.popup(0,"QuickMsg:Lona/incombat#{rand(2)}",0,0)
end
