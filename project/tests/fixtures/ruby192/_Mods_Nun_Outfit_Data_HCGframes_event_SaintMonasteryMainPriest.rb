if $game_map.threat
	SndLib.sys_buzzer
	$game_map.popup(0,"QuickMsg:Lona/incombat#{rand(2)}",0,0)
	return
end

$game_temp.choice  = -1
tmpPriestX,tmpPriestY,tmpPriestID=$game_map.get_storypoint("MainPriest")
tmpBedX,tmpBedY,tmpBedID=$game_map.get_storypoint("SexPoint3")

if $story_stats["QuProgScoutCampOrkind2"] ==2
	call_msg("TagMapSaintMonastery:priest/QuProgScoutCampOrkind2_done")
	$story_stats["QuProgScoutCampOrkind2"] = 3
	get_character(0).call_balloon(0)
else
	tmpAbout = $story_stats["RecordBaptizeByPriest"] == 0 && get_character(0).switch1_id == 0
	tmpMoreGrace = $story_stats["RecordBaptizeByPriest"] >=1 && get_character(0).switch1_id == 0

	tmp_Begging = $game_player.actor.wisdom_trait >= 3 && $game_player.actor.weak >= 50 && get_character(0).summon_data[:SexTradeble]
	tmpGotoTar = ""
	tmpTarList = []
	tmpTarList << [$game_text["commonNPC:commonNPC/Cancel"]				,"Cancel"]
	tmpTarList << [$game_text["commonNPC:commonNPC/Barter"]				,"Barter"]
	tmpTarList << [$game_text["TagMapSaintMonastery:opt/about"]			,"about"]		if tmpAbout
	tmpTarList << [$game_text["TagMapSaintMonastery:opt/moreGrace"]		,"moreGrace"]	if tmpMoreGrace
	cmd_sheet = tmpTarList
	cmd_text =""
	for i in 0...cmd_sheet.length
		cmd_text.concat(cmd_sheet[i].first+",")
	end
	call_msg("TagMapSaintMonastery:priest/common",0,2,0)
	call_msg("\\optB[#{cmd_text}]")
	$game_temp.choice == -1 ? tmpPicked = false : tmpPicked = cmd_sheet[$game_temp.choice][1]
	$game_temp.choice = -1
	
	
	case tmpPicked
		when "Barter"
			call_msg("TagMapSaintMonastery:priest/trade")
			
			shopNerf = ([([$story_stats["WorldDifficulty"].round,100].min)-0,0].max*0.001) #put the varible u want nerf shop,  if none put 0. var1=max  var2=min.   max-min must with in 100 (ex:weak125, -25)
			charStoreTP = [(  0  )*(1-(shopNerf+shopNerf)),0].max.round  #wildHobo 150+rand(800) hobo 300+rand(1000) #Normie 500+rand(2000) #NonTradeShop 1000+rand(2500) #innkeeper 1000+rand(2500) #storeMarket 3000+rand(6000)
			charStoreExpireDate = $game_date.dateAmt+1+rand(4+$story_stats["Setup_Hardcore"]) #if nil. delete after close.  if < date. delete after nap.
			charStoreHashName = "#{@map_id}_#{get_character(0).id}".to_sym  #can be register with character name like "COCONA"
			#data 0item 1:weapon 2:armor
			#original price? 0,0   custom price? nil,price
			good=[
				[*$data_ItemName["ItemBread"].get_type_and_id,nil,(($data_ItemName["ItemBread"].price*1.3)*(shopNerf+1)).round		,rand(2)],
				[*$data_ItemName["ItemDryFood"].get_type_and_id,nil,(($data_ItemName["ItemDryFood"].price*1.3)*(shopNerf+1)).round	,rand(2)],
				[*$data_ItemName["ItemHolyWater"].get_type_and_id,nil,2000,99],
				[*$data_ItemName["ItemNeckleAtoner"].get_type_and_id,nil,2000,99],
				[*$data_ItemName["ItemShSaintPurge"].get_type_and_id,nil,(($data_ItemName["ItemShSaintPurge"].price*20)*(shopNerf+1)),1],
				[*$data_ItemName["ItemShSaintProtect"].get_type_and_id,nil,(($data_ItemName["ItemShSaintProtect"].price*20)*(shopNerf+1)),1],
				
				[*$data_ItemName["NunMid"].get_type_and_id		,0,0,1]
			]
			tmpGoldRoll = [40,41,42].sample #WastePoo0,WasteVomit0,ItemSopMeat
			good.insert(rand(good.length),[0,tmpGoldRoll ,nil,($data_items[tmpGoldRoll].price).round					,[0,0,rand(2)].sample])		#ItemContraceptivePills #####Gold roll 3:<300, 4:<500  5:>500
			manual_trade(good,charStoreHashName,charStoreTP,charStoreExpireDate,noSell=false,noBuy=false)
			
			
			
			
		when "about" #關於聖徒會
			call_msg("TagMapSaintMonastery:priest/about") 
			
			
			if $game_temp.choice == 0
				call_msg("TagMapSaintMonastery:priest/about_optY")
				chcg_background_color(0,0,0,0,7)
					portrait_hide
					$game_player.moveto(tmpBedX,tmpBedY)
					$game_player.direction = 4
					get_character(0).npc_story_mode(true)
					get_character(0).moveto(tmpBedX-1,tmpBedY)
					get_character(0).direction = 6
				chcg_background_color(0,0,0,255,-7)
				call_msg("TagMapSaintMonastery:priest/BaptizePlayer1")
				get_character(0).jump_to(tmpBedX+1,tmpBedY)
				get_character(0).direction=4
				$game_player.direction = 6
				call_msg("TagMapSaintMonastery:priest/BaptizePlayer2")
				get_character(0).jump_to(tmpBedX,tmpBedY+1)
				get_character(0).direction=8
				$game_player.direction = 2
				
				

				!equip_slot_removetable?(6) ? equips_6_id = -1 : equips_6_id = $game_player.actor.equips[6].id #BELT
				!equip_slot_removetable?(2) ? equips_2_id = -1 : equips_2_id = $game_player.actor.equips[2].id #TOP
				!equip_slot_removetable?(4) ? equips_4_id = -1 : equips_4_id = $game_player.actor.equips[4].id #BOT
				!equip_slot_removetable?(3) ? equips_3_id = -1 : equips_3_id = $game_player.actor.equips[3].id #MID

				
				#remove a dress
				$game_temp.choice == -1
				if equips_6_id == -1 && equips_2_id == -1 && equips_4_id == -1 && equips_3_id == -1
					$game_temp.choice = 1
				else
				call_msg("TagMapSaintMonastery:priest/BaptizePlayer3") #\optD[不要,脫]
				end
				
				if $game_temp.choice == 0
					get_character(0).actor.set_aggro($game_player.actor,$data_arpgskills["BasicNormal"],6000)
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_N")
				else
					get_character(0).switch1_id = 1
					$story_stats["RecordBaptizeByPriest"] =1
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_Y")
					if equips_6_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(6, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_2_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(2, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_4_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(4, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_3_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(3, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					
					event_key_cleaner
					$game_player.actor.stat["EventVagRace"] = "Human"
					$game_player.actor.stat["EventAnalRace"] = "Human"
					$game_player.actor.stat["EventMouthRace"] = "Human"
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_touch0")
					load_script("Data/HCGframes/Grab_EventAnal_AnalTouch.rb")
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_touch1")
					load_script("Data/HCGframes/Grab_EventVag_VagTouch.rb")
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_touch2")
					load_script("Data/HCGframes/Grab_EventMouth_kissed.rb")
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer3_touch3")
					event_key_cleaner
					
					
					############################################################## Deep t
					
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer4")
					$game_portraits.setLprt("nil")
					$game_player.actor.stat["EventMouthRace"] = "Human"
					load_script("Data/HCGframes/UniqueEvent_DeepThroat.rb")
					event_key_cleaner
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer5")
					check_half_over_event
					
					##############################################################本番
					get_character(0).moveto($game_player.x,$game_player.y)
					ev_target = get_character(0)
					temp_race=ev_target.actor.race
					plus = 1
					$game_player.manual_sex = true
					$game_player.actor.stat["SexEventScore"] = 0
					$game_player.actor.stat["SexEventLast"] =0
					$game_player.actor.stat["SexEventTotalScore"] = 0
					play_sex_service_main(ev_target,"vag",true)
					play_sex_service_get_reward(plus)
					play_sex_service_main(ev_target,"vag",true)
					play_sex_service_get_reward(plus)
					play_sex_service_main(ev_target,"vag",true)
					play_sex_service_get_reward(plus)
					play_sex_service_main(ev_target,"vag",true)
					play_sex_service_chcg(ev_target)
					play_sex_service_get_reward(plus)
					
					$game_player.actor.stat["SexEventScore"] = 0
					$game_player.actor.stat["SexEventTotalScore"] = 0
					$game_player.actor.stat["SexEventInput"] = 0
					$game_player.actor.stat["SexEventLast"] = 0
					$game_player.actor.set_action_state(:none)
					$game_player.unset_event_chs_sex
					$game_player.manual_sex = false
					ev_target.unset_event_chs_sex
					event_key_cleaner_whore_work
					$game_player.actor.prtmood("normal")
					$game_portraits.lprt.hide
					$game_portraits.rprt.hide
					##############################################################本番 end
					get_character(0).npc_story_mode(true)
					1.times{
						get_character(0).direction = 2 ; get_character(0).move_forward_force
						get_character(0).move_speed = 3
						until !get_character(0).moving? ; wait(1) end
					}
					get_character(0).direction = 8
					get_character(0).npc_story_mode(false)
					$game_player.direction = 2
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer6")
					
					if equips_3_id != -1#檢查裝備 並穿裝
						$game_actors[1].change_equip(3, $data_armors[equips_3_id])
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_4_id != -1#檢查裝備 並穿裝
						$game_actors[1].change_equip(4, $data_armors[equips_4_id])
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_2_id != -1#檢查裝備 並穿裝
						$game_actors[1].change_equip(2, $data_armors[equips_2_id])
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_6_id != -1#檢查裝備 並穿裝
						$game_actors[1].change_equip(6, $data_armors[equips_6_id])
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					
					
					chcg_background_color(0,0,0,0,7)
						portrait_hide
						get_character(0).moveto(tmpPriestX,tmpPriestY)
						get_character(0).direction = 2
						$game_player.moveto(tmpPriestX+1,tmpPriestY)
						$game_player.direction = 2
					chcg_background_color(0,0,0,255,-7)
					call_msg("TagMapSaintMonastery:priest/BaptizePlayer_end")
				end
				
	
			
			
	
			
			get_character(0).npc_story_mode(false)
			
			elsif $game_temp.choice ==1
				get_character(0).actor.set_aggro($game_player.actor,$data_arpgskills["BasicNormal"],6000)
				call_msg("TagMapSaintMonastery:priest/about_optN")
			end
			#####################################################################################################################################################################################
			#####################################################################################################################################################################################
			#####################################################################################################################################################################################
			######################################################################      更多的聖徒恩惠       ##################################################################################################
			#####################################################################################################################################################################################
			#####################################################################################################################################################################################
			#####################################################################################################################################################################################
		when "moreGrace"# 更多的聖徒恩惠
			get_character(0).switch1_id = 1
			call_msg("TagMapSaintMonastery:priest/GraceAgain0")
			chcg_background_color(0,0,0,0,7)
				portrait_hide
				$game_player.moveto(tmpBedX,tmpBedY)
				$game_player.direction = 8
				get_character(0).npc_story_mode(true)
				get_character(0).moveto(tmpBedX,tmpBedY-1)
				get_character(0).direction = 2
				get_character(0).animation = get_character(0).animation_peeing
			chcg_background_color(0,0,0,255,-7)
			

			!equip_slot_removetable?(6) ? equips_6_id = -1 : equips_6_id = $game_player.actor.equips[6].id #BELT
			!equip_slot_removetable?(2) ? equips_2_id = -1 : equips_2_id = $game_player.actor.equips[2].id #TOP
			!equip_slot_removetable?(4) ? equips_4_id = -1 : equips_4_id = $game_player.actor.equips[4].id #BOT
			!equip_slot_removetable?(3) ? equips_3_id = -1 : equips_3_id = $game_player.actor.equips[3].id #MID
			
			
			if equips_6_id != -1 || equips_2_id != -1 || equips_4_id != -1 || equips_3_id != -1
				call_msg("TagMapSaintMonastery:priest/GraceAgain1")
					if equips_6_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(6, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_2_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(2, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_4_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(4, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
					if equips_3_id != -1#檢查裝備 並脫裝
						$game_actors[1].change_equip(3, nil)
						SndLib.sound_equip_armor(125)
						player_force_update
						wait(30)
					end
			end
			
			call_msg("TagMapSaintMonastery:priest/GraceAgain2")
			$game_player.actor.stat["EventMouthRace"] = "Human"
			tmpRace = $game_player.actor.stat["EventMouthRace"]
			tmpPenisID = "Hairly"
			#############################################################SUCKA PART
			tmp_loop_time= 5
			tmp_current_loop = 0
			until tmp_current_loop >= tmp_loop_time
				tmp_current_loop +=1
				
				lona_mood "chcg3fuck_#{chcg_shame_mood_decider}"
				$game_player.actor.stat["mouth"] = [3,5,6,7,8].sample
				$game_player.actor.stat["HeadGround"] =1
				$game_player.actor.portrait.update
				$game_player.actor.portrait.angle=90
				$game_portraits.rprt.set_position(-100+rand(5),880+rand(5))
				$game_portraits.setLprt("#{tmpRace}Penis#{tmpPenisID}CHCG")
				$game_portraits.lprt.set_position(233+rand(4),-77+rand(4))
				wait(37+rand(5))
				SndLib.sound_chcg_full(rand(100)+50)
				$game_portraits.rprt.set_position(-100+rand(5),880+rand(5))
				wait(2+rand(3))
				
				lona_mood "chcg3fuck_#{chcg_shame_mood_decider}"
				$game_player.actor.stat["mouth"] = [9,4].sample
				$game_player.actor.stat["HeadGround"] =0
				$game_player.actor.portrait.update
				$game_player.actor.portrait.angle=90
				$game_portraits.rprt.set_position(-100-rand(5),850-rand(5))
				$game_portraits.setLprt("#{tmpRace}Penis#{tmpPenisID}CHCG")
				$game_portraits.lprt.set_position(233+rand(4),-77+rand(4))
				load_script("Data/Batch/chcg_basic_frame_mouth.rb") if tmp_current_loop % 2 == 0 
				wait(17+rand(5))
				$game_portraits.rprt.set_position(-100-rand(5),850-rand(5))
				wait(2+rand(3))
			end
			check_half_over_event
			call_msg("TagMapSaintMonastery:priest/GraceAgain3")
			$game_player.actor.add_wound("head")
			
			tmp_loop_time= 8
			tmp_current_loop = 0
			until tmp_current_loop >= tmp_loop_time
				tmp_current_loop +=1
				
				lona_mood "chcg3fuck_#{chcg_shame_mood_decider}"
				$game_player.actor.stat["mouth"] = [3,5,6,7,8].sample
				$game_player.actor.stat["HeadGround"] =1
				$game_player.actor.portrait.update
				$game_player.actor.portrait.angle=90
				$game_portraits.rprt.set_position(-100+rand(5),880+rand(5))
				$game_portraits.setLprt("#{tmpRace}Penis#{tmpPenisID}CHCG")
				$game_portraits.lprt.set_position(233+rand(4),-77+rand(4))
				wait(27+rand(4))
				SndLib.sound_chcg_full(rand(100)+50)
				$game_portraits.rprt.set_position(-100+rand(5),880+rand(5))
				wait(2+rand(3))
				
				lona_mood "chcg3fuck_#{chcg_shame_mood_decider}"
				$game_player.actor.stat["mouth"] = [9,4].sample
				$game_player.actor.stat["HeadGround"] =0
				$game_player.actor.portrait.update
				$game_player.actor.portrait.angle=90
				$game_portraits.rprt.set_position(-100-rand(5),850-rand(5))
				$game_portraits.setLprt("#{tmpRace}Penis#{tmpPenisID}CHCG")
				$game_portraits.lprt.set_position(233+rand(4),-77+rand(4))
				load_script("Data/Batch/chcg_basic_frame_mouth.rb") if tmp_current_loop % 2 == 0 
				wait(17+rand(4))
				$game_portraits.rprt.set_position(-100-rand(5),850-rand(5))
				wait(2+rand(3))
			end
			check_half_over_event
			call_msg("TagMapSaintMonastery:priest/GraceAgain4")
			$game_portraits.setLprt("nil")
			$game_player.actor.stat["EventMouthRace"] = "Human"
			load_script("Data/HCGframes/EventMouth_CumInside_Overcum.rb")
			#############################################################SUCKA PART END
			
			
			$game_portraits.setLprt("nil")
			$game_portraits.setRprt("nil")
			call_msg("TagMapSaintMonastery:priest/GraceAgain5")
			portrait_hide
			##############################################################本番
			get_character(0).moveto($game_player.x,$game_player.y)
			ev_target = get_character(0)
			temp_race=ev_target.actor.race
			plus = 1
			$game_player.manual_sex = true
			$game_player.actor.stat["SexEventScore"] = 0
			$game_player.actor.stat["SexEventLast"] =0
			$game_player.actor.stat["SexEventTotalScore"] = 0
			play_sex_service_main(ev_target,"vag",true)
			play_sex_service_get_reward(plus)
			play_sex_service_main(ev_target,"vag",true)
			play_sex_service_get_reward(plus)
			play_sex_service_main(ev_target,"vag",true)
			play_sex_service_get_reward(plus)
			play_sex_service_main(ev_target,"vag",true)
			play_sex_service_chcg(ev_target)
			play_sex_service_get_reward(plus)
			
			$game_player.actor.stat["SexEventScore"] = 0
			$game_player.actor.stat["SexEventTotalScore"] = 0
			$game_player.actor.stat["SexEventInput"] = 0
			$game_player.actor.stat["SexEventLast"] = 0
			$game_player.actor.set_action_state(:none)
			$game_player.unset_event_chs_sex
			$game_player.manual_sex = false
			ev_target.unset_event_chs_sex
			event_key_cleaner_whore_work
			$game_player.actor.prtmood("normal")
			$game_portraits.lprt.hide
			$game_portraits.rprt.hide
			##############################################################本番 end
			
					get_character(0).npc_story_mode(true)
					1.times{
						get_character(0).direction = 2 ; get_character(0).move_forward_force
						get_character(0).move_speed = 3
						until !get_character(0).moving? ; wait(1) end
					}
					get_character(0).direction = 8
					$game_player.direction = 2
					get_character(0).npc_story_mode(false)
			
			call_msg("TagMapSaintMonastery:priest/GraceAgain6")
			if equips_6_id != -1 || equips_2_id != -1 || equips_4_id != -1 || equips_3_id != -1
				if equips_3_id != -1#檢查裝備 並穿裝
					$game_actors[1].change_equip(3, $data_armors[equips_3_id])
					SndLib.sound_equip_armor(125)
					player_force_update
					wait(30)
				end
				if equips_4_id != -1#檢查裝備 並穿裝
					$game_actors[1].change_equip(4, $data_armors[equips_4_id])
					SndLib.sound_equip_armor(125)
					player_force_update
					wait(30)
				end
				if equips_2_id != -1#檢查裝備 並穿裝
					$game_actors[1].change_equip(2, $data_armors[equips_2_id])
					SndLib.sound_equip_armor(125)
					player_force_update
					wait(30)
				end
				if equips_6_id != -1#檢查裝備 並穿裝
					$game_actors[1].change_equip(6, $data_armors[equips_6_id])
					SndLib.sound_equip_armor(125)
					player_force_update
					wait(30)
				end
			end
			call_msg("TagMapSaintMonastery:priest/GraceAgain7")
			
			chcg_background_color(0,0,0,0,7)
				portrait_hide
				$game_player.moveto(tmpPriestX,tmpPriestY+2)
				$game_player.direction = 8
				get_character(0).moveto(tmpPriestX,tmpPriestY)
				get_character(0).direction = 2
				get_character(0).animation = nil
				get_character(0).npc_story_mode(false)
			chcg_background_color(0,0,0,255,-7)
			if $game_player.actor.record_lona_title != "WhoreJob/SaintBeliever"
				$game_player.actor.record_lona_title = "WhoreJob/SaintBeliever"
				call_msg("TagMapSaintMonastery:priest/add_title1")
			end
			$story_stats["RecordBaptizeByPriest"] += 1
			if $game_player.actor.record_lona_title == "WhoreJob/SaintBeliever" && $story_stats["RecordBaptizeByPriest"] >= 4 && $story_stats["RecordRelicByPriest"] == 0
				$story_stats["RecordRelicByPriest"] = 1
				call_msg("TagMapSaintMonastery:priest/get_sexReward")
				optain_morality(5)
				wait(30)
				optain_item($data_ItemName["ItemShSaintPurge"],1)
				wait(30)
				optain_item($data_ItemName["ItemShSaintProtect"],1)
			end
	end
end


eventPlayEnd
