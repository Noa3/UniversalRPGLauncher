
#==============================================================================
# ** Map_Hud
#==============================================================================

class Map_Hud
	def initialize
		@actor = $game_player.actor
		@player = $game_player
		@viewport = Viewport.new
		@viewport.z = System_Settings::MAP_HUD_Z
		setting = [ 
			["Layout",				18,		13],
			["Small_Number",		37,		40],
			["main_bars",			37,		49],
			["main_bars",			37,		49],
			["main_bars",			37,		49],
			["main_bars",			37,		49],
			["main_bars",			38,		50],
			["Small_Number",		37,		54],
			["main_bars",			37,		63],
			["main_bars",			37,		63],
			["main_bars",			37,		63],
			["main_bars",			37,		63],
			["main_bars",			38,		64],
			["Small_Number",		37,		68],
			["main_bars",			37,		77],
			["main_bars",			37,		77],
			["main_bars",			37,		77],
			["main_bars",			37,		77],
			["main_bars",			38,		78],
		]
		@list = [	:layout,
					:health_number,
					:health_bar_cap,
					:health_bar,
					:health_bar_background_cap,
					:health_bar_background,
					:health_bar_transition,
					:sta_number,
					:sta_bar_cap,
					:sta_bar,
					:sta_bar_background_cap,
					:sta_bar_background,
					:sta_bar_transition,
					:sat_number,
					:sat_bar_cap,
					:sat_bar,
					:sat_bar_background_cap,
					:sat_bar_background,
					:sat_bar_transition,
				].collect{|i| 
					instance_variable_set("@#{i}", Sprite.new(@viewport))
				}
		@list.each_with_index do |sprite, index|
			sprite.bitmap = File.exists?("ModScripts/_Mods/NewBars/#{setting[index][0]}.png") ? Bitmap.new("ModScripts/_Mods/NewBars/#{setting[index][0]}") : sprite.bitmap = Bitmap.new("#{System_Settings::MAP_HUD_FOLDER}#{setting[index][0]}")
			sprite.x = setting[index][1]
			sprite.y = setting[index][2]
		end

		@ratio = 0.1 #control length

		
		@health_bar_cap.src_rect = Rect.new(0, 0, 1, 4)
		@health_bar.src_rect = Rect.new(0,0,(@ratio*@actor.health).ceil+1,4)
		@health_bar_transition.src_rect = Rect.new(0, 16, (@ratio*@actor.health).ceil, 2)
		@health_bar_background_cap.src_rect = Rect.new(202, 0, 1, 4)
		@health_bar_background.src_rect = Rect.new(202, 0, (@ratio*@actor.attr_dimensions["health"][2]).ceil+1, 4)
		
		@health_bar_cap.z+=1
		@health_bar.z+=3
		@health_bar_transition.z+=2
		@health_bar_background_cap.z-=1
		@health_bar_background.z-=1
		@health_bar_cap.x=37+@health_bar.src_rect.width
		@prev_health = @actor.health
		@health_bar_background_cap.x=37+@health_bar_background.src_rect.width
		@prev_health_max = @actor.attr_dimensions["health"][2]
		
		
		@sta_bar_cap.src_rect = Rect.new(0, 4, 1, 4)
		@sta_bar.src_rect = Rect.new(0,4,[@ratio*@actor.sta,0].max.ceil+1,4)
		@sta_bar_transition.src_rect = Rect.new(0, 16, [@ratio*@actor.sta,0].max.ceil, 2)
		@sta_bar_background_cap.src_rect = Rect.new(202, 4, 1, 4)
		@sta_bar_background.src_rect = Rect.new(202, 4, (@ratio*@actor.attr_dimensions["sta"][2]).ceil+1, 4)
		
		@sta_bar_cap.z+=1
		@sta_bar.z+=3
		@sta_bar_transition.z+=2
		@sta_bar_background_cap.z-=1
		@sta_bar_background.z-=1
		@sta_bar_cap.x=37+@sta_bar.src_rect.width
		@prev_sta = @actor.sta
		@sta_bar_background_cap.x=37+@sta_bar_background.src_rect.width
		@prev_sta_max = @actor.attr_dimensions["sta"][2]
		
		if @actor.sta<=0
			@sta_bar.opacity=0 
			@sta_bar_cap.opacity=0 
		else
			@sta_bar.opacity=255 
			@sta_bar_cap.opacity=255
		end
		
		
		@sat_bar_cap.src_rect = Rect.new(0, 8, 1, 4)
		@sat_bar.src_rect = Rect.new(0,8,(@ratio*@actor.sat).ceil+1,4)
		@sat_bar_transition.src_rect = Rect.new(0, 16, (@ratio*@actor.sat).ceil, 2)
		@sat_bar_background_cap.src_rect = Rect.new(202, 8, 1, 4)
		@sat_bar_background.src_rect = Rect.new(202, 8, (@ratio*@actor.attr_dimensions["sat"][2]).ceil+1, 4)
		
		@sat_bar_cap.z+=1
		@sat_bar.z+=3
		@sat_bar_transition.z+=2
		@sat_bar_background_cap.z-=1
		@sat_bar_background.z-=1
		@sat_bar_cap.x=37+@sat_bar.src_rect.width
		@prev_sat = @actor.sat
		@sat_bar_background_cap.x=37+@sat_bar_background.src_rect.width
		@prev_sat_max = @actor.attr_dimensions["sat"][2]


		
		@number_bmp = Bitmap.new("#{System_Settings::MAP_HUD_FOLDER}health_Number")
		w = @number_bmp.width/10
		@number_rect = {}
		(0..9).each {|num|
		@number_rect[num] = Rect.new(w * num, 0, w, @number_bmp.height / 2)}
		
		
		@small_number_bmp = Bitmap.new("ModScripts/_Mods/NewBars/Small_Number")
		w = @small_number_bmp.width/12
		@small_number_rect = {}
		(0..11).each {|num|
		@small_number_rect[num] = Rect.new(w * num, 0, w, @small_number_bmp.height / 2)}
		
		
		draw_number_with_max(@health_number.bitmap, @small_number_bmp, @actor.health,@actor.attr_dimensions["health"][2],@small_number_rect, @actor.health <= 0)
		draw_number_with_max(@sta_number.bitmap, @small_number_bmp, @actor.sta,@actor.attr_dimensions["sta"][2],@small_number_rect, @actor.sta <= 0)
		draw_number_with_max(@sat_number.bitmap, @small_number_bmp, @actor.sat,@actor.attr_dimensions["sat"][2],@small_number_rect, @actor.attr_dimensions["sat"][2] <= 0.25)
		states_fadeout_init
		
		refresh
	end

	def states_fadeout_init
		states_list = []
		@state_fadeout_icons = {}
		@state_fadeout_bar_list = {}
		@state_changed = false
		$data_StateName.each{|stName,stData|
			next unless stData.remove_by_walking
			next unless (stData.icon_index && stData.icon_index != 0 && stData.icon_index != -1) || stData.hud_trace_icon
			states_list << [stData.item_name,stData.id,stData.icon_index,stData.steps_to_remove*60,stData.hud_trace_icon]
		}

		states_list.each_with_index{|(stName, stID, stIconID, stStepFrame), index|
			tmpSprite = Sprite.new(@viewport)
			tmpSprite.bitmap = Cache.normal_bitmap("ModScripts/_Mods/NewBars/state_bar.png")
			@state_fadeout_bar_list[stName] = tmpSprite
		}
		states_list.each_with_index{|(stName, stID, stIconID, stStepFrame,tmp_hud_trace), index|
				tmpSprite = Sprite.new(@viewport)
				states_list[index][4] = tmpSprite
				if tmp_hud_trace
					cachedBitmapICON = Cache.normal_bitmap(tmp_hud_trace)
					icon_src_rect = Rect.new(0, 0, 12, 12)
				elsif stIconID.is_a?(String)
					cachedBitmapICON = Cache.normal_bitmap(stIconID)
					icon_src_rect = Rect.new(0, 0, 24, 24)
				else
					cachedBitmapICON = Cache.system("Iconset")
					icon_src_rect = Rect.new(stIconID % 16 * 24, stIconID / 16 * 24, 24, 24)
				end
				tmpSprite.bitmap = Bitmap.new(24,24)
				tmpSprite.bitmap.blt(0 , 0 ,cachedBitmapICON , icon_src_rect)
				tmpSprite.zoom_x = 0.5 if !tmp_hud_trace #original icon is 24x24. this ui support 12x12
				tmpSprite.zoom_y = 0.5 if !tmp_hud_trace
				tmpSprite.opacity = 0
				@state_fadeout_icons[stName] = tmpSprite
		}
	end
	def states_fadeout_dispose
		@state_fadeout_icons.each{|tmpName,spr|
			spr.bitmap.dispose
			spr.dispose
		}
		@state_fadeout_bar_list.each{|tmpName,spr|
			spr.bitmap.dispose
			spr.dispose
		}
		@state_fadeout_icons = nil
		@state_fadeout_bar_list = nil
	end

	def refresh
		return unless @viewport.visible
		refresh_numbers
		#refresh_states
		#refresh_sight_icon
	end

	
	def refresh_numbers
		refresh_health
		refresh_health_bar
		refresh_health_transition
		refresh_health_background
		
		
		refresh_sta
		refresh_sta_bar
		refresh_sta_transition
		refresh_sta_background
		
		
		refresh_sat
		refresh_sat_bar
		refresh_sat_transition
		refresh_sat_background


		@actor.skill_changed=false 
		#refresh_unread_mail mod
	end

  def refresh_health
    return if @prev_health == @actor.health
    draw_number_with_max(@health_number.bitmap, @small_number_bmp, @actor.health,@actor.attr_dimensions["health"][2],@small_number_rect, @actor.health <= 0)
    @prev_health = @actor.health
  end
  def refresh_health_bar
	return if @health_bar.src_rect.width==(@ratio*@actor.health).ceil+1
	if @health_bar.src_rect.width>(@ratio*@actor.health).ceil+1
		@health_bar.src_rect.width=(@ratio*@actor.health).ceil+1
	else
		@health_bar.src_rect.width+=1
	end
	@health_bar_cap.x=37+@health_bar.src_rect.width
  end
  def refresh_health_transition
	return if @health_bar.src_rect.width - @health_bar_transition.src_rect.width ==1
	if @health_bar.src_rect.width - @health_bar_transition.src_rect.width >1
		@health_bar_transition.src_rect.width =-1+@health_bar.src_rect.width
	else 
		@health_bar_transition.src_rect.width-=1 if !$game_map.threat
	end
  end
  def refresh_health_background
    return if @prev_health_max == @actor.attr_dimensions["health"][2]
	@health_bar_background.src_rect.width=(@ratio*@actor.attr_dimensions["health"][2]).ceil+1
	@health_bar_background_cap.x=37+@health_bar_background.src_rect.width
    @prev_health_max = @actor.attr_dimensions["health"][2]
  end
  
  def refresh_sta
    return if @prev_sta == @actor.sta
    draw_number_with_max(@sta_number.bitmap, @small_number_bmp, @actor.sta,@actor.attr_dimensions["sta"][2],@small_number_rect, @actor.sta <= 0)
    @prev_sta = @actor.sta
	if @actor.sta<=0
		@sta_bar.opacity=0 
		@sta_bar_cap.opacity=0 
	else
		@sta_bar.opacity=255 
		@sta_bar_cap.opacity=255
	end
  end
  def refresh_sta_bar
	return if @sta_bar.src_rect.width==[@ratio*@actor.sta,0].max.ceil+1
	if @sta_bar.src_rect.width>[@ratio*@actor.sta,0].max.ceil+1
		@sta_bar.src_rect.width=[@ratio*@actor.sta,0].max.ceil+1
	else
		@sta_bar.src_rect.width+=1
	end
	@sta_bar_cap.x=37+@sta_bar.src_rect.width
  end
  def refresh_sta_transition
	return if @sta_bar.src_rect.width - @sta_bar_transition.src_rect.width ==1
	if @sta_bar.src_rect.width - @sta_bar_transition.src_rect.width >1
		@sta_bar_transition.src_rect.width =-1+@sta_bar.src_rect.width
	else 
		@sta_bar_transition.src_rect.width-=1 if !$game_map.threat
	end
  end
   def refresh_sta_background
    return if @prev_sta_max == @actor.attr_dimensions["sta"][2]
	@sta_bar_background.src_rect.width=(@ratio*@actor.attr_dimensions["sta"][2]).ceil+1
	@sta_bar_background_cap.x=37+@sta_bar_background.src_rect.width
    @prev_sta_max = @actor.attr_dimensions["sta"][2]
  end
  def refresh_sat_bar
	return if @sat_bar.src_rect.width==[@ratio*@actor.sat,0].max.ceil+1
	if @sat_bar.src_rect.width>[@ratio*@actor.sat,0].max.ceil+1
		@sat_bar.src_rect.width=[@ratio*@actor.sat,0].max.ceil+1
	else
		@sat_bar.src_rect.width+=1
	end
	@sat_bar_cap.x=37+@sat_bar.src_rect.width
  end
  def refresh_sat
    return if @prev_sat == @actor.sat
    draw_number_with_max(@sat_number.bitmap, @small_number_bmp, @actor.sat,@actor.attr_dimensions["sat"][2],@small_number_rect, @actor.attr_dimensions["sat"][2] <= 0.25)
	
    @prev_sat = @actor.sat
	if @actor.sat<=0
		@sat_bar.opacity=0 
		@sat_bar_cap.opacity=0 
	else
		@sat_bar.opacity=255 
		@sat_bar_cap.opacity=255
	end
  end
  def refresh_sat_transition
	return if @sat_bar.src_rect.width - @sat_bar_transition.src_rect.width ==1
	if @sat_bar.src_rect.width - @sat_bar_transition.src_rect.width >1
		@sat_bar_transition.src_rect.width =-1+@sat_bar.src_rect.width
	else 
		@sat_bar_transition.src_rect.width-=1
	end
  end
  def refresh_sat_background
    return if @prev_sat_max == @actor.attr_dimensions["sat"][2]
	@sat_bar_background.src_rect.width=@ratio*@actor.attr_dimensions["sat"][2].ceil+1
	@sat_bar_background_cap.x=37+@sat_bar_background.src_rect.width
    @prev_sat_max = @actor.attr_dimensions["sat"][2]
  end

#############################################################################################################################################################
#############################################################################################################################################################
  def draw_number(tar_bmp, src_bmp, number, rects, crisis = false)
    tar_bmp.clear
    w = rects[0].width
    number.ceil.split_digits.map.with_index do |char, index|
      rect = rects[char].dup
      rect.y += rect.height if crisis
      tar_bmp.blt(index*w, 0, src_bmp, rect)
    end
  end
  def draw_number_with_max(tar_bmp, src_bmp, number, max, rects, crisis = false)
    tar_bmp.clear
    w = rects[0].width
	if number.ceil == max.ceil
		array=max.ceil.split_digits
	else 
		array=number.ceil.split_digits
		array << 10
		array+=max.ceil.split_digits
	end
    array.map.with_index do |char, index|
      rect = rects[char].dup
	  crisis=false if char==10
      rect.y += rect.height if crisis
      tar_bmp.blt(index*w, 0, src_bmp, rect)
    end
  end

  
	def hide
		@viewport.visible = false
		#@list.each{|spr|spr.visible=false}
	end
  
	def show
		return if @viewport.visible
		refresh
		@viewport.visible = true
	end
  
  def hud_opa(tmpOpa=255)
	@viewport.opacity = tmpOpa
  end
  
	def dispose
		@list.each do |spr| 
			spr.bitmap.dispose
			spr.dispose
		end
		states_fadeout_dispose
		@number_bmp.dispose
		@small_number_bmp.dispose
		@viewport.dispose
	end

end

