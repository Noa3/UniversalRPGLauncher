#==============================================================================
# This script has been altered by Kslander for the need of LonaRPG
#==============================================================================
=begin

Lets text commands load text from external files

Example in rpgm event editor:
\t[demo:greeting1]
Will load block "greeting1" from file "Text/EN/demo".

Terminology:
box: typically corresponds to a message "page".
block: a group of boxes displayed in sequence.

=end

#==============================================================================
# Text
#------------------------------------------------------------------------------
# Maps keys to text Strings. Reference at $game_text.
#==============================================================================
=begin

class Text
  
  attr_accessor :base_folder
  ###
  def initialize(base_folder="Text/CHT")
    @map = {}
	@base_folder=base_folder
  end
  
  # Separate file name and block id at first colon.
  def split_key(key)
	return if key.nil?
    key.match(/([^:]+):(.+)/)
    [$1, $2]
  end

#	def [](key)
#		file, id = split_key(key)
#		if file.nil? || id.nil?
#			$text_log.log_text(key,"no data found") if $correcting_text
#			return key 
#		end
#		unless @map.has_key?(file)
#			@map[file] = load_file(file)
#		end
#		$text_log.log_text(key,@map[file][id]) if $correcting_text
#		@map[file][id]
#	end
#end

	def [](key,noError=false)
		file, id = split_key(key)
		return key if file.nil? || id.nil?
		unless @map.has_key?(file)
			@map[file] = load_file(file)
		end
		res = @map[file][id]
		if !res && !noError #若有錯誤則回報文字FLAG
			res = key
		elsif !res && noError #若有錯誤則回報""
			res = ""
		end
		
		return res
	end

	def load_file(file)
		begin
			sth=File.read("#{@base_folder}/#{file}.txt")
			return parse(sth.to_s.encode("utf-8"))
		rescue => ex
			msgbox "ERROR: missing translation file #{@base_folder}/#{file}.txt"
			return Hash.new
		end
	end

	def parse(string)
		blocks = {}
		lines = string.split("\n")
		until lines.empty?
			line = lines.shift
			next if line.empty?
			next if line[0] == '#'
			id = line
			boxes = []
			until lines.empty?
				line = lines.shift
				next if line[0] == '#'
				break if line.empty?
				#temporary name handling -- give name its own line 
				line.match(/(([^:]+): ?)?(.*)/)
				if $1.nil?
				boxes << $3
				else
				body = $3
				#name = $2.gsub(/_/, ' ')  #becaues : crash?
				name = $2 #make : didnt crash?
				boxes << "#{name}\n#{body}"
				end
			end
			blocks[id] = boxes.join("\f")
		end
		blocks
	end
	end
=end

#==============================================================================
# DataManager
#------------------------------------------------------------------------------
# $game_text does not get saved.
#==============================================================================
#module DataManager
#	self.singleton_class.send(:alias_method, :create_game_objects_pre_dialog, :create_game_objects)
#	def self.create_game_objects
#		create_game_objects_pre_dialog
#	end
#end

#==============================================================================
# Window_Base
#------------------------------------------------------------------------------
# Escape for text.
#==============================================================================
class Window_Base < Window

# Let params be Strings instead of just Ints. Used by moods: \m[sad]
	def obtain_escape_param(text)
		# Gather non-bracket characters inside the brackets.
		param_string = text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/]
		if param_string.match(/^\d+$/)
			# Old behavior is to return an Int for digits.
			param_string.to_i
		else
			# New behavior is to return Strings
			param_string
		end
		rescue StandardError
		0
	end

	alias_method :convert_escape_characters_pre_dialog, :convert_escape_characters
	def convert_escape_characters(text)
		# Inject external text before processing escapes.
		convert_escape_characters_pre_dialog(text.gsub(/\\T\[([^\]]+)\]/i) { $game_text[$1] })
	end

end

#==============================================================================
 Struct.new("ConditionalChoice",:condition,:choice,:real_index)
#==============================================================================


#==============================================================================
# Window_Message
#------------------------------------------------------------------------------
# Escape for mood.
#==============================================================================
class Window_Message < Window_Base
  include GIM_PRT
  alias_method :process_escape_character_pre_dialog, :process_escape_character
  def process_escape_character(code, text, pos)
    case code
	when 'm'
		$game_player.actor.prtmood(obtain_escape_param(text))
		$game_portraits.setRprt("Lona")
	when 'PRF'
		$game_portraits.rprt.focus
	when 'prf'
		$game_portraits.rprt.fade
	when 'prh'
		$game_portraits.rprt.hide
	when 'SETpr'
		$game_portraits.setRprt(obtain_escape_param(text))
	when 'Rshake'
		$game_portraits.rprt.shake
	when 'PLF'
		$game_portraits.lprt.focus
	when 'plf'
		$game_portraits.lprt.fade
	when 'plh'
		$game_portraits.lprt.hide
	when 'SETpl'
		$game_portraits.setLprt(obtain_escape_param(text))
	when 'Lshake'
		$game_portraits.lprt.shake
	when 'psw'
		$game_portraits.swprt
	when 'ph'
		$game_portraits.lprt.hide
		$game_portraits.rprt.hide
	when 'call'
	load_script(obtain_escape_param(text))
	when 'cg'
	$cg = TempCG.new([text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/]])
	when 'cgoff'
	$cg.erase
	when 'bg'
	$bg = TempBG.new([text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/]])
	when 'bgoff'
	$bg.erase
	when 'Rflash'
	$game_map.interpreter.flash_screen(Color.new(255,0,0,200),8,true)
	when 'n'
	process_new_line(text, pos)
	when 'SND'
	Audio.se_play("Audio/#{obtain_escape_param(text)}",$data_SNDvol)
	when 'SndLib'
	eval("SndLib.#{obtain_escape_param(text)}")
	when 'CamMP'
		begin ;temp_id=$game_map.get_storypoint("#{obtain_escape_param(text)}")[2].to_i;rescue ;temp_id= -999 ;end
		if !$game_map.events[temp_id].nil?
			$game_map.interpreter.cam_follow(temp_id,0)
		else; prp "CamMP not found",1
		end
	when 'CamCT'
	$game_map.interpreter.cam_center(0)
	when 'CamID'
		temp_id = obtain_escape_param(text).to_i
		temp_id = $game_map.interpreter.get_character(0).id if temp_id == -1
		if !$game_map.events[temp_id].nil?
			$game_map.interpreter.cam_follow(temp_id,0)
		else; prp "CamID not found",1
		end
	when 'Bon'
		$game_player.call_balloon(obtain_escape_param(text))
	when 'BonID'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		temp_id= temp_id_bon[0].to_i
		temp_id = $game_map.interpreter.get_character(0).id if temp_id == -1
		temp_balloon= temp_id_bon[1].to_i
		if !$game_map.events[temp_id].nil?
			$game_map.events[temp_id].call_balloon(temp_balloon)
		else; prp "BonID not found",1
		end
	when 'BonMP'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		begin ;temp_id=$game_map.get_storypoint(temp_id_bon[0])[2].to_i;rescue ;temp_id= -999 ;end
		temp_balloon= temp_id_bon[1].to_i
		if !$game_map.events[temp_id].nil?
			$game_map.events[temp_id].call_balloon(temp_balloon)
		else; prp "BonMP not found",1
		end
	when 'CBid'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		temp_id= temp_id_bon[0].to_i
		temp_id = $game_map.interpreter.get_character(0).id if temp_id == -1
		temp_balloon= temp_id_bon[1].to_i
		if !$game_map.events[temp_id].nil?
			$game_map.interpreter.cam_follow(temp_id,0)
			$game_map.events[temp_id].call_balloon(temp_balloon)
		else; prp "BonID not found",1
		end
	when 'CBmp'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		begin ;temp_id=$game_map.get_storypoint(temp_id_bon[0])[2].to_i;rescue ;temp_id= -999 ;end
		temp_balloon= temp_id_bon[1].to_i
		if !$game_map.events[temp_id].nil?
			$game_map.interpreter.cam_follow(temp_id,0)
			$game_map.events[temp_id].call_balloon(temp_balloon)
		else; prp "BonID not found #{temp_id} #{obtain_escape_param(text)}",1
		end
		
	when 'CBct'
		$game_map.interpreter.cam_center(0)
		$game_player.call_balloon(obtain_escape_param(text))
		
	when 'CBfB'
		if !$game_map.isOverMap
			temp_id =$game_player.get_followerID(0)
			if !temp_id.nil? && !$game_map.events[temp_id].nil?
				$game_map.interpreter.cam_follow(temp_id,0)
				$game_map.events[temp_id].call_balloon(obtain_escape_param(text))
			else; prp "BonID not found #{temp_id} #{obtain_escape_param(text)}",1
			end
		end
	when 'CBfF'
		if !$game_map.isOverMap
			temp_id =$game_player.get_followerID(1)
			if !temp_id.nil? && !$game_map.events[temp_id].nil?
				$game_map.interpreter.cam_follow(temp_id,0)
				$game_map.events[temp_id].call_balloon(obtain_escape_param(text))
			else; prp "BonID not found #{temp_id} #{obtain_escape_param(text)}",1
			end
		end
				
	when 'CBfE'
		if !$game_map.isOverMap
			temp_id =$game_player.get_followerID(-1)
			if !temp_id.nil? && !$game_map.events[temp_id].nil?
				$game_map.interpreter.cam_follow(temp_id,0)
				$game_map.events[temp_id].call_balloon(obtain_escape_param(text))
			else; prp "BonID not found #{temp_id} #{obtain_escape_param(text)}",1
			end
		end
	when 'optD'
		process_select_markup(text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip,0)
	when 'optB'
		process_select_markup(text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip,100)
	when 'WT'
		process_timeout_markup(text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip)
	when 'WF'
		process_timeout_markup_frame(text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip)
	when 'board'
		SndLib.sys_DialogBoard
		process_board_markup(text,pos)
	when 'narr'
		SndLib.sys_DialogNarr
		process_narrator_start
	when 'narrOFF'
		process_narrator_off
		
	when 'SETlpl'
		$game_NPCLayerMain.prtmood(obtain_escape_param(text))
		$game_portraits.setLprt("NPCLayerMain")
	when 'SETlpr'
		$game_NPCLayerSub.prtmood(obtain_escape_param(text))
		$game_portraits.setRprt("NPCLayerSub")
	when 'SFXset'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		setdialogSndVol(temp_id_bon[0].to_i)
		setdialogSndPitch(temp_id_bon[1].to_i)
		setdialogSndDense(temp_id_bon[2].to_i)
	when 'SFXfile'
		temp_id_bon = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
		setdialogSndFile(temp_id_bon[0])
	else
		process_escape_character_pre_dialog(code, text, pos)
end

  end
  
  alias_method :create_all_windows_pre_dialog, :create_all_windows
  def create_all_windows
	create_all_windows_pre_dialog
  end
  
	def process_narrator_start
		prp "process_narrator_start",5
		@line_show_fast = true
		$game_message.position = 1
		$game_message.background = 2
		$game_message.narrator = true
		@narrator_back.visible = true
		self.opacity = 0
		update_background
		update_placement
	end
  
	def process_narrator_off
		prp "process_narrator_off",5
		@line_show_fast = false
		$game_message.position = 2
		$game_message.background = 0
		$game_message.narrator = false
		@narrator_back.visible = false
		self.opacity = 255
		update_background
		update_placement
	end
  
  def process_cam_mp_markup(text)
	ev_key = obtain_escape_param(text)
	begin 
		temp_id=$game_map.get_storypoint(ev_key)[2]
		$game_map.interpreter.cam_follow(temp_id,0)
	rescue 
		prp "CamMP point:#{ev_key} not found",1
	end
	if !$game_map.events[temp_id].nil?
		
	else; 
	end
  end
  
  def process_cam_id_markup(param)
	ev_id = obtain_escape_param(text).to_i
	if !$game_map.events[ev_id].nil?
		$game_map.interpreter.cam_follow(ev_id,8)
	else
		prp "CamID id:#{ev_id} not found",1
	end
  end
  
  def process_bon_id_markup(ev_id,balloon_id)
	ev = $game_map.events[ev_id.to_i]
	balloon_id = temp_id_bon[1].to_i
	ev.nil? ? prp("BonID :#{ev_id} not found",1) :ev.call_balloon(balloon_id.to_i)
  end
  
  def process_bon_point_markup(text)
	ev_key,balloon_id = (text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip).split(%r{,\s*})
	begin 
		ev_id=$game_map.get_storypoint(temp_id_bon[0])[2]
		process_bon_id_markup(ev_id,balloon_id.to_i)
	rescue 
		prp "BonMP point name :#{ev_key} not found",1
	end
  end
  
  
  def process_select_markup(text,disallow)
		options=text.split(",")
		@textcmds=Array.new()
		$game_message.timeout=nil
		$game_temp.preserved_message=nil
		for i in 0...options.length
			timeout=/<t=(\d+)>/.match(options[i])
			timeout=timeout[1] unless timeout.nil?
			condition=/<r=(.*)>/.match(options[i])
			condition=condition[1] unless condition.nil?
			options[i]=options[i].gsub(/<t=(\d+)>/,"") if timeout
			options[i]=options[i].gsub(/<r=(.*)>/,"") if condition
			opt=options[i].split("=")
			#處理Timeout
			if timeout && $game_message.timeout.nil?
				$game_message.timeout=timeout.to_i
				$game_message.choice_timeout_type= i if timeout
			end
			#處理Conditional
			#if(!condition.nil? && $story_stats[condition]!="1")
			#	next
			if !condition.nil?
				$game_message.choices.push(Struct::ConditionalChoice.new(condition,opt[0],i))
				@textcmds.push(nil)
			else
				$game_message.choices.push(opt[0])
				@textcmds.push(opt[1])
			end
		end
		$game_temp.knot_mode=false
		$game_message.choice_proc=Proc.new{|n| 
			if (n!= $game_message.choice_cancel_type)
				if(!@textcmds[n].nil?)#如果目標節點不是nil才呼叫2號公用事件
					$game_temp.preserved_message="\\t[#{@textcmds[n].gsub(/\s+/, ":")}]"
					$game_temp.knot_mode=true
					$game_temp.reserve_common_event(2)
				end			
			end
		}
		$game_message.choice_cancel_type=disallow
  end
  
  def process_timeout_markup(time)
		set_timeout(time.to_i)
  end
  
  def process_timeout_markup_frame(frame)
	set_timeout_frame(frame.to_i)
  end
  
  def on_timeout
	@timeout=true
  end
  
  def timeout?
   @timeout
  end

  #board system
	def process_board_markup(text,pos)
		title = text.slice!(/^\[[^\[\]]+\]/)[/[^\[\]]+/].strip
		text.gsub!("\f","\n")
		set_board_parameter
		draw_board_title(title,pos)
		reset_font_settings
		#@line_show_fast = true
	end
  
  
#overrides 121_Dialog_Control_System.rb line 460
#it had not big enough nor fittingly small bitmap so make it big. Make it (300,230) size to disable scrolling, it'll turn into multipage.
	def draw_board_title(title_text,pos)
		self.contents=Bitmap.new(300,600)#was 300,300
		self.contents.font.size = 25
		self.contents.font.outline = false
		self.contents.font.color = Color.new(255,255,255,255)
		text_rect = contents.text_size(title_text)
		text_rect.x = (self.contents.rect.width - text_rect.width) /2 - standard_padding 
		text_rect.y = 0
		pos[:y] = text_rect.height + 5
		self.contents.draw_text(text_rect,title_text,0)
		@message_board = true
	end
  
  #set_everything to board measure
  def set_board_parameter
	self.x	= 160
	self.y	= 40
	self.width	= 320
	self.height	= 260
	self.opacity= 255
	create_contents
	$game_message.position = 3
	@position	= 3
	$game_message.background = 2
	@background	= 2
  end



end


#debugging class for TextSystem,prepared for the game Text debugging ,logs recent 300 text request and its content. 
#enable it by setting 	$correcting_text  to true in F11 script
#referenced by $text_log
#class TextLogger
#	MAX_LOG_COUNT=300
#	def initialize
#		@message_log=Array.new(MAX_LOG_COUNT,[])
#	end
#	
#	def log_text(key,content)
#		p "log_text log_textlog_text"
#		@message_log.shift
#		@message_log<<[key,content]
#	end
#	
#	def get_log(index)
#		@message_log[index]
#	end
#	
#	
#	def show_log(start_index=280,range=20)
#		end_index=start_index + range
#		for i in start_index..end_index
#			if @message_log.length <=i
#				p "********no_more messages********"
#				break
#			end
#			p "log index=>#{i} key = #{@message_log[i][0]}"
#			p "message=#{@message_log[i][1]} "
#			p "-------------------------------------------------------------------------------------"
#		end
#	end
#	
#	
#	def show_all
#		self.show_log(0,TextLogger::MAX_LOG_COUNT)
#	end
#	
#	def show_log_popup(start_index,end_index)
#		msgx=""
#		for i in start_index..end_index
#			next if @message_log[i].nil?
#			msgx += "log index=>#{i} key = #{@message_log[i][0]} \n message=#{@message_log[i][1]} \n ##########################"
#		end
#		msgbox "Showing recent messages:#{msgx}"	
#	end
#	
#	
#
#end
