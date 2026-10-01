#Optimization mod by Teravisor

#Removes no more check_stat loop each frame

#Problem: 25_Game_Actor.rb calls check_stat which loops through all stats two times per frame in update_lonastat

#What it does:
#Removes all previous calls to check_stat
#set_stat checks for min/max of specified stat instead
#Moves check_stat call to refresh call that only happens when status or equipment gets changed
#Deprecates update_lonaStat call in order to remove it from everywhere; calls it during refresh; fixes few cases where refresh wasn't called properly when changing states. For old method use update_lonaStat_
#Intercepts set calls to Game_Actor.stat to report portrait stat changes to 1_O_StatPortraitOptimization.rb

#NOTE: check_stat is used in following files:
#\Frames\25_Game_Actor.rb  (was called in update_lonaStat that was moved to refresh call)
#\Frames\281_Game_NonPlayerCharacter.rb  (used in update_npc_stat before applying effects)
#\Editables\4000_Npc_AbomHiveLink.rb  (Redundant call: @stat gets replaced by @master.actor.stat after check_stat)
#\Frames\Modules\Mood.rb  (Used to refresh stat after calling update_persona that does call set_stat so should be fine)

unless $statsOptimized
$statsOptimized=true

=begin Broke arrows: they are NPCs with min/max stats 0
class ActorStat
	#saving old method in check_stat_old and removing check_stat method
	alias_method :check_stat_old, :check_stat
	def check_stat
	end

	def check_single_stat(key)
		#return if @stat[key][MAX_TRUE].nil? || @stat[key][MIN_TRUE].nil? || @stat[key][MAX_STAT].nil? || @stat[key][MIN_STAT].nil?
		check_min_def_within_range(key)
		check_max_def_within_range(key)
		check_max_within_range(key)
		check_min_within_range(key)
		remove_changed_mark(key)
	end
	
	def check_current_stat(key)
		#return if @stat[key][MAX_STAT].nil? || @stat[key][MIN_STAT].nil?
		check_max_within_range(key)
		check_min_within_range(key)
	end
	
	#redefining set_stat to also check that value is in range
	def set_stat(stat_name,value,type=CURRENT_STAT)
		return p "stats not exist on target stat_name=>#{stat_name}" if @stat[stat_name].nil?
		value=value.round(3) if value.is_a?(Float)
	        @stat[stat_name][type]=value
		#when setting MAX_STAT or MIN_STAT we need to not check them in order to prevent situation when we first increase MAX_STAT, then round to MAX_TRUE, then apply reduction by decreasing MAX_STAT
		#setting MAX_TRUE and MIN_TRUE will break whole chain of checks with how they're made anyway, so it'll be applied only on next refresh.
		p "#{@npc_name} set_stat #{stat_name} #{value} #{type}"
		check_current_stat(stat_name) if type == CURRENT_STAT
	end

	#should also check that value is in range.
	def set_stat_m(stat_name,value,types=nil)
		types=[CURRENT_STAT,MAX_STAT,MAX_TRUE] if types.nil?
		raise "set_stat_m requires array of types" if !types.kind_of?(Array)
		types.each{
			|type|
			@stat[stat_name][type]=value
		}
		check_single_stat(stat_name)
	end

	#this is called before check_stat each update_npc_stat so I do what check_stat did here
	def reset_definition
		@stat.keys.each{
			|key|
			@stat[key][MIN_STAT]=@stat[key][MIN_TRUE]
			@stat[key][MAX_STAT]=@stat[key][MAX_TRUE]
			check_single_stat(key)
		}
	end
end
=end

class LonaActorStat < ActorStat
	#there are a lot of set_stat calls, and there should be a way to hook to them in place where you know who your actor is.
	attr_accessor :actor

	#saving old method in check_stat_old (with last underscore) and removing check_stat method
	alias_method :check_stat_old, :check_stat
	def check_stat
		#If situation of Player getting hit and speed getting stuck at slowed is possible then uncomment next line.
		#check_single_stat("move_speed")
	end

	def set_stat(stat_name, value, type=CURRENT_STAT)
		return p "stats not exist on target stat_name=>#{stat_name}" if @stat[stat_name].nil?
		value=value.round(3) if value.is_a?(Float)
	        @stat[stat_name][type]=value
		#when setting MAX_STAT or MIN_STAT we need to not check them in order to prevent situation when we first increase MAX_STAT, then round to MAX_TRUE, then apply reduction by decreasing MAX_STAT
		#setting MAX_TRUE and MIN_TRUE will break whole chain of checks with how they're made anyway, so it'll be applied only on next refresh.
		check_current_stat(stat_name) if type == CURRENT_STAT

		#those calls were in update_lonaStat each frame
		if type == CURRENT_STAT && !@actor.nil? then
			if stat_name == "health".freeze then
				@actor.check_prev_health_to_mood
				@actor.check_prev_health_to_BabyHealth
				@actor.prev_health = @actor.health
			elsif stat_name == "sta".freeze then
				@actor.stat["sta".freeze]=value
				@actor.check_prev_sta_to_arousal
				@actor.check_prev_sta_to_exp
				@actor.check_prev_sta_to_BabyHealth
				@actor.prev_sta = @actor.sta
			elsif stat_name == "mood".freeze
				@actor.stat["mood".freeze]=value
				@actor.check_mood_to_state
			elsif stat_name == "dirt".freeze
				@actor.stat["dirt".freeze]=value
				@actor.check_dirt_to_state
				if @dirt_opt.nil? || (@dirt_opt - value).abs > 25 then #Only update portrait and chs if dirt changed by more than 1% (25/255) opacity
					@dirt_opt = value
					$game_player.refresh_chs
					if $statPortraitOptimize==true && @actor.portrait
						@actor.portrait.set_stat("dirt".freeze, value)
						@actor.portrait.assemble_portrait
					end
				end
			end
		end
		if type == MAX_STAT && !@actor.nil?
			if stat_name == "sta".freeze then
				@actor.calculate_weight_carried
			end
		end

	end

	def check_single_stat(key)
		return if unknown_behave_stats?(key) #|| !stat_changed?(key)
		sync_max(key) if reality_stats?(key) || ground_stats?(key)
		if reality_stats?(key) || regualr_stats?(key) then
			check_max_def_within_range(key)
			check_min_def_within_range(key)
			check_def_cross_section(key)
	                check_max_within_range(key)
			check_min_within_range(key)
		else
			if ground_stats?(key) then
				check_min_def_within_range(key)
				check_def_cross_section(key)
				check_min_within_range(key)
			end
		end
		remove_changed_mark(key)
	end

	def check_current_stat(key)
		return if unknown_behave_stats?(key)
		sync_max(key) if reality_stats?(key) || ground_stats?(key)
		if reality_stats?(key) || regualr_stats?(key) then
			check_def_cross_section(key)
	                check_max_within_range(key)
			check_min_within_range(key)
		else
			if ground_stats?(key) then
				check_def_cross_section(key)
				check_min_within_range(key)
			end
		end
	end

	#should also check that value is in range.
	def set_stat_m(stat_name,value,types=nil)
		types=[CURRENT_STAT,MAX_STAT,MAX_TRUE] if types.nil?
		raise "set_stat_m requires array of types" if !types.kind_of?(Array)
		types.each{
			|type|
			@stat[stat_name][type]=value
		}
		check_single_stat(stat_name)
	end

	#this is called before check_stat each update_npc_stat so I do what check_stat did here
	def reset_definition
		@stat.keys.each{
			|key|
			next if reality_stats?(key)
			@stat[key][MIN_STAT]=@stat[key][MIN_TRUE]
			@stat[key][MAX_STAT]=@stat[key][MAX_TRUE]
			check_single_stat(key)
		}
		@actor.calculate_weight_carried if !@actor.nil?
	end
	#does exactly what reset_definition does with a typo (MAX_STAT=MAX_TRUE twice) and only place it's called is right after it
	def reset_stat_def
	end
end

#there are a lot of set_stat calls, and there should be a way to hook to them in place where you know who your actor is.
class Game_Actor < Game_Battler
	alias_method :setup_TERFIX1, :setup
	def setup(actor_id)
		setup_TERFIX1(actor_id)
		@actStat.check_stat_old
		@actStat.actor = self
	end

	alias_method :post_setup_TERFIX1, :post_setup
	def post_setup
		post_setup_TERFIX1
		update_lonaStat_
	end
	
	#process_nap_change calls a lot of erase_state so we need to refresh stats after.
	alias_method :process_nap_change_TERFIX1, :process_nap_change
	def process_nap_change
		process_nap_change_TERFIX1
		refresh
	end
	
	#so many things refresh does, but it doesn't call check_stat for some reason even though it's being called on every remove_state and add_state and change_equip
	alias_method :refresh_TERFIX1, :refresh
	alias_method :update_lonaStat_TERFIX1, :update_lonaStat
	def refresh
		refresh_TERFIX1
		@actStat.check_stat_old
		#update_lonaStat_TERFIX1
		recalculate_stats
	end
	#simulates behavior of old update_lonaStat to not call whole refresh where old update_lonaStat was used
	def update_lonaStat_
		update_lonaStat_TERFIX1
		@actStat.check_stat_old
	end
	#removes all calls to old update_lonaStat
	def update_lonaStat
		#It was called in update_lonaStat so I guess it might be important for some states.
		#But update_lonaStat is called from so many places that it's strange.
		#for example, when loading. In other words, one can save/load until status that can be removed by walking is gone...
		update_state_frames
	end

	def recalculate_stats
		tmpSexAtk = (self.stat["Nymph"]+self.stat["Prostitute"])*25
		tmpSexAtkMax = 100+self.stat["Lilith"]*50
		self.constitution=		self.constitution_trait+self.constitution_plus
		self.survival=			self.survival_trait+self.survival_plus
		self.wisdom=			self.wisdom_trait+self.wisdom_plus
		self.combat=			self.combat_trait+self.combat_plus
		self.scoutcraft=		self.scoutcraft_trait+self.scoutcraft_plus
		self.atk=				(self.combat*0.5) + self.atk_plus
		self.def=				self.def_plus
	
		sex_Con = self.constitution/2
		self.sex_vag_atk=		([sex_Con+(@exp_vag  *(2+self.stat["Lilith"]))+tmpSexAtk,tmpSexAtkMax].min - self.vag_damage/100).round
		self.sex_anal_atk=		([sex_Con+(@exp_anal *(2+self.stat["Lilith"]))+tmpSexAtk,tmpSexAtkMax].min - self.anal_damage/100).round
		self.sex_mouth_atk=		([sex_Con+(@exp_mouth*(2+self.stat["Lilith"]))+tmpSexAtk,tmpSexAtkMax].min * [0.01*self.sta,1].min).round
		self.sex_limbs_atk=		([sex_Con+(@exp_limbs*(2+self.stat["Lilith"]))+tmpSexAtk,tmpSexAtkMax].min * [0.01*self.sta,1].min).round
	
		self.morality=			self.morality_lona + (self.morality_plus-200)
		self.hud_weight_count=	2*self.attr_dimensions["sta"][2] - self.weight_carried
	end

	#init_equip calls refresh before everything is setup for update_lonaStat.
	def init_equips(equips)
		@equips = Array.new(equip_slots.size) { Game_BaseItem.new }
		equips.each_with_index do |item_id, i|
			etype_id = index_to_etype_id(i)
			slot_id = empty_slot(etype_id)
			report_portrait_stat(@equips[slot_id].object.lonaStat.statName, nil) if !@equips[slot_id].object.nil?
			@equips[slot_id].set_equip(etype_id == 0, item_id) if slot_id
			report_portrait_stat(@equips[slot_id].object.lonaStat.statName, @equips[slot_id].object.lonaStat.affectValue) if !@equips[slot_id].object.nil?
		end
	#refresh # not everything is set up for update_lonaStat at this point. So refresh will come later.

#from 119_ACE_Equip.rb equip_extra_starting_equips
		for equip_id in actor.extra_starting_equips
			armour = $data_armors[equip_id]
			next if armour.nil?
			etype_id = armour.etype_id
			next unless equip_slots.include?(etype_id)
			slot_id = empty_slot(etype_id)
			report_portrait_stat(@equips[slot_id].object.lonaStat.statName, nil) if !@equips[slot_id].object.nil?
			@equips[slot_id].set_equip(etype_id == 0, armour.id)
			report_portrait_stat(@equips[slot_id].object.lonaStat.statName, @equips[slot_id].object.lonaStat.affectValue) if !@equips[slot_id].object.nil?
		end
		#refresh # It also calls refresh. And later yet again.
		refresh_equip_effects #update_lonaStat will be called soon and it needs carryWeight
	end

	alias_method :change_equip_StatOptimization, :change_equip
	def change_equip(slot_id, item)
		oldItem = @equips[slot_id].object
		change_equip_StatOptimization(slot_id, item)
		newItem = @equips[slot_id].object
		if newItem!=oldItem then
			report_portrait_stat(oldItem.lonaStat.statName, nil) if !oldItem.nil?
			report_portrait_stat(newItem.lonaStat.statName, newItem.lonaStat.affectValue) if !newItem.nil?
		end
	end
	alias_method :discard_equip_optimization, :discard_equip
	def discard_equip(item)
		report_portrait_stat(item.lonaStat.statName, nil) if item && item.lonaStat
		discard_equip_optimization(item)
#		report_portrait_stat("dirt",stat["dirt"])
	end

	def release_unequippable_items(item_gain = true)
		loop do
			last_equips = equips.dup
			@equips.each_with_index do |item, i|
				if !equippable?(item.object) || item.object.etype_id != equip_slots[i]
					if !item.object.nil?
						report_portrait_stat(item.object.lonaStat.statName, nil)
					end
					trade_item_with_party(nil, item.object) if item_gain
					item.object = nil
				end
			end
			return if equips == last_equips
		end
	end
	
	alias_method :actor_ForceUpdate_TERFIX1, :actor_ForceUpdate
	def actor_ForceUpdate
		update_lonaStat_
		actor_ForceUpdate_TERFIX1
	end
	
end

#Because CG scripts call actor.stat[<name>]=<something> I have to intercept them.
class Portrait_Stat_Proxy
	attr_accessor :actor
	def initialize(actor)
		@actor = actor
	end
	def [](key)
		return @actor.portrait_stat_value[key]
	end
	def []=(key, value)
		@actor.report_portrait_stat(key,value)
	end
	def each
		@actor.portrait_stat_value.each{|k,v|
						oldVal = v
						yield(k,v)
						@actor.report_portrait_stat(k,v) if oldVal != v
					}
	end
end

#Affects portrait stats. If left as it-is, mood gets infinitely updates so portrait gets perma shown
module Moods
	def prtmood(moodName,update_first=true)
		curMood=get_mood(moodName)
		return p "mood not found for #{moodName}" if curMood.nil?
		update_lonaStat if update_first
		curMood.affectStat(self)
		@stat_changed=true
		portrait.reset_rotation 
		portrait.update
	end

	class Mood
	      def affectStat(actor)
			randomValue=rand(@facesets.length)
			randomSet=@facesets[randomValue]
			randomKeys=randomSet.keys
			randomKeys.each{
				|rKey|
				rKeyVal=rand(randomSet[rKey].length)
				if(randomValue==1)
					actor.report_portrait_stat(rKey, randomSet[rKey][0])
				else
					actor.report_portrait_stat(rKey, randomSet[rKey][rKeyVal])
				end
	          
			}
		end
	end

	class CHCGMood < Mood
		def affectStat(actor)
			statNames=@facesets.keys
			for x in 0...statNames.length
				statName=statNames[x]
				randomRange=@facesets[statName].length-1
				actor.report_portrait_stat(statName, @facesets[statName][rand(0,randomRange)])
			end
		end
	end
end

=begin NPC stats - broke arrows, see above
#For some reason in specific cases (Cocona "morality" in tavern) MAX_STAT is set to 0 during setup
class Game_NonPlayerCharacter  < Game_Battler
	alias_method :setup_npc_TERFIX1, :setup_npc
	def setup_npc
		setup_npc_TERFIX1
		@stat.reset_definition
		@stat.check_stat_old
	end
end

class Game_DestroyableObject < Game_NonPlayerCharacter
	def setup_npc
		super
		p "Game_DestroyableObject setup_npc"
		@stat.stat.each{|k,v|
			v[1]=-9999
			v[2]=9999
			v[3]=9999
			v[4]=-9999
		}
	end
	def refresh_combat_attribute
	end
end
=end

#Player's check for Fatigue (stamina<0) happens using erase_state which doesn't call refresh
class Game_Player
	alias_method :handle_on_move_step_TERFIX1, :handle_on_move_step
	def handle_on_move_step(multiplier=1)
		if actor.sta >0 && actor.stat["Fatigue".freeze] ==1
			actor.remove_state(14)
		end
		handle_on_move_step_TERFIX1(multiplier)
	end
end

#old update_lonaStat calls that are not frame-dependant and no refresh call near them
module DataManager
	class << self
		alias_method :setup_new_game_TERFIX1, :setup_new_game
		def self.setup_new_game(tmp_tomap=true)
			setup_new_game_TERFIX1(tmp_tomap)
			$game_actors[1].refresh
		end

		alias_method :extract_save_contents_TERFIX1, :extract_save_contents
		def extract_save_contents(contents)
			extract_save_contents_TERFIX1(contents)
			$game_player.actor.actStat.actor = $game_player.actor
		end
	end
end


class Game_Actor < Game_Battler
#Freezing strings to not allocate them each time getter and setter is used.
#getter
def health; @actStat.get_stat("health".freeze);end
def always; @actStat.get_stat("always".freeze);end					
def mood; @actStat.get_stat("mood".freeze);end					
def sta; @actStat.get_stat("sta".freeze);end						
def sat; @actStat.get_stat("sat".freeze);end						
def weak; @actStat.get_stat("weak".freeze);end					
def will; @actStat.get_stat("will".freeze);end					
def sexy; @actStat.get_stat("sexy".freeze);end					
def baby_health; @actStat.get_stat("baby_health".freeze);end				
def morality; @actStat.get_stat("morality".freeze);end				
def morality_plus; @actStat.get_stat("morality_plus".freeze);end				
def morality_lona; @actStat.get_stat("morality_lona".freeze);end				
def dirt; @actStat.get_stat("dirt".freeze);end					
def sex_vag_atk; @actStat.get_stat("sex_vag_atk".freeze);end					
def sex_anal_atk; @actStat.get_stat("sex_anal_atk".freeze);end					
def sex_mouth_atk; @actStat.get_stat("sex_mouth_atk".freeze);end					
def sex_limbs_atk; @actStat.get_stat("sex_limbs_atk".freeze);end					
def constitution; @actStat.get_stat("constitution".freeze);end			
def survival; @actStat.get_stat("survival".freeze);end				
def wisdom; @actStat.get_stat("wisdom".freeze);end					
def combat; @actStat.get_stat("combat".freeze);end					
def scoutcraft; @actStat.get_stat("scoutcraft".freeze);end				

def constitution_trait; @actStat.get_stat("constitution_trait".freeze);end			
def survival_trait; @actStat.get_stat("survival_trait".freeze);end				
def wisdom_trait; @actStat.get_stat("wisdom_trait".freeze);end					
def combat_trait; @actStat.get_stat("combat_trait".freeze);end					
def scoutcraft_trait; @actStat.get_stat("scoutcraft_trait".freeze);end				

def melaninNipple; @actStat.get_stat("melaninNipple".freeze);end			
def melaninVag; @actStat.get_stat("melaninVag".freeze);end				
def melaninAnal; @actStat.get_stat("melaninAnal".freeze);end				
def arousal; @actStat.get_stat("arousal".freeze);end					
def atk; @actStat.get_stat("atk".freeze);end				
def def; @actStat.get_stat("def".freeze);end				
def move_speed; @actStat.get_stat("move_speed".freeze);end				
def atk_plus; @actStat.get_stat("atk_plus".freeze);end				
def def_plus; @actStat.get_stat("def_plus".freeze);end				
def constitution_plus; @actStat.get_stat("constitution_plus".freeze);end		
def combat_plus; @actStat.get_stat("combat_plus".freeze);end		
def survival_plus; @actStat.get_stat("survival_plus".freeze);end			
def wisdom_plus; @actStat.get_stat("wisdom_plus".freeze);end				
def scoutcraft_plus; @actStat.get_stat("scoutcraft_plus".freeze);end			
def persona; @actStat.get_stat("persona".freeze);end					
def race; @actStat.get_stat("race".freeze);end					
def urinary_level; @actStat.get_stat("urinary_level".freeze);end			
def lactation_level; @actStat.get_stat("lactation_level".freeze);end			
def defecate_level; @actStat.get_stat("defecate_level".freeze);end			
def vag_damage; @actStat.get_stat("vag_damage".freeze);end				
def urinary_damage; @actStat.get_stat("urinary_damage".freeze);end			
def anal_damage; @actStat.get_stat("anal_damage".freeze);end				
def state_preg_rate; @actStat.get_stat("state_preg_rate".freeze);end			
def puke_value_normal; @actStat.get_stat("puke_value_normal".freeze);end		
def drug_addiction_level; @actStat.get_stat("drug_addiction_level".freeze);end	
def ograsm_addiction_level; @actStat.get_stat("ograsm_addiction_level".freeze);end	
def semen_addiction_level; @actStat.get_stat("semen_addiction_level".freeze);end	
def drug_addiction_damage; @actStat.get_stat("drug_addiction_damage".freeze);end	
def ograsm_addiction_damage; @actStat.get_stat("ograsm_addiction_damage".freeze);end	
def semen_addiction_damage; @actStat.get_stat("semen_addiction_damage".freeze);end	
def sex; @actStat.get_stat("sex".freeze);end	


#setter
def health=(val);@actStat.set_stat("health".freeze,val);end					
def always=(val);@actStat.set_stat("always".freeze,val);end					
def mood=(val);@actStat.set_stat("mood".freeze,val);end					
def sta=(val);@actStat.set_stat("sta".freeze,val);end						
def sat=(val);@actStat.set_stat("sat".freeze,val);end						
def weak=(val);@actStat.set_stat("weak".freeze,val);end					
def will=(val);@actStat.set_stat("will".freeze,val);end					
def baby_health=(val);@actStat.set_stat("baby_health".freeze,val);end				
def morality=(val);@actStat.set_stat("morality".freeze,val);end				
def morality_plus=(val);@actStat.set_stat("morality_plus".freeze,val);end				
def morality_lona=(val);@actStat.set_stat("morality_lona".freeze,val);end				
def dirt=(val); @actStat.set_stat("dirt".freeze,val);end					
def sex_vag_atk=(val);@actStat.set_stat("sex_vag_atk".freeze,val);end					
def sex_anal_atk=(val);@actStat.set_stat("sex_anal_atk".freeze,val);end					
def sex_mouth_atk=(val);@actStat.set_stat("sex_mouth_atk".freeze,val);end					
def sex_limbs_atk=(val);@actStat.set_stat("sex_limbs_atk".freeze,val);end					
def constitution=(val);@actStat.set_stat("constitution".freeze,val);end			
def survival=(val);@actStat.set_stat("survival".freeze,val);end				
def wisdom=(val);@actStat.set_stat("wisdom".freeze,val);end					
def combat=(val);@actStat.set_stat("combat".freeze,val);end					
def scoutcraft=(val);@actStat.set_stat("scoutcraft".freeze,val);end				

def constitution_trait=(val);@actStat.set_stat("constitution_trait".freeze,val);end			
def survival_trait=(val);@actStat.set_stat("survival_trait".freeze,val);end				
def wisdom_trait=(val);@actStat.set_stat("wisdom_trait".freeze,val);end					
def combat_trait=(val);@actStat.set_stat("combat_trait".freeze,val);end					
def scoutcraft_trait=(val);@actStat.set_stat("scoutcraft_trait".freeze,val);end				

def melaninNipple=(val);@actStat.set_stat("melaninNipple".freeze,val);end			
def melaninVag=(val);@actStat.set_stat("melaninVag".freeze,val);end				
def melaninAnal=(val);@actStat.set_stat("melaninAnal".freeze,val);end				
def arousal=(val);@actStat.set_stat("arousal".freeze,val);end					
def move_speed=(val);@actStat.set_stat("move_speed".freeze,val);end				
def atk_plus=(val);@actStat.set_stat("atk_plus".freeze,val);end				
def def_plus=(val);@actStat.set_stat("def_plus".freeze,val);end				
def atk=(val);@actStat.set_stat("atk".freeze,val);end				
def def=(val);@actStat.set_stat("def".freeze,val);end				
def scoutcraft_plus=(val);@actStat.set_stat("scoutcraft_plus".freeze,val);end			
def constitution_plus=(val);@actStat.set_stat("constitution_plus".freeze,val);end		
def combat_plus=(val);@actStat.set_stat("combat_plus".freeze,val);end		
def survival_plus=(val);@actStat.set_stat("survival_plus".freeze,val);end			
def wisdom_plus=(val);@actStat.set_stat("wisdom_plus".freeze,val);end				
def persona=(val);@actStat.set_stat("persona".freeze,val);end					
def race=(val);@actStat.set_stat("race".freeze,val);end					
def urinary_level=(val);@actStat.set_stat("urinary_level".freeze,val);end			
def lactation_level=(val);@actStat.set_stat("lactation_level".freeze,val);end			
def defecate_level=(val);@actStat.set_stat("defecate_level".freeze,val);end			
def vag_damage=(val);@actStat.set_stat("vag_damage".freeze,val);end				
def urinary_damage=(val);@actStat.set_stat("urinary_damage".freeze,val);end			
def anal_damage=(val);@actStat.set_stat("anal_damage".freeze,val);end				
def state_preg_rate=(val);@actStat.set_stat("state_preg_rate".freeze,val);end			
def puke_value_normal=(val);@actStat.set_stat("puke_value_normal".freeze,val);end		
def drug_addiction_level=(val);@actStat.set_stat("drug_addiction_level".freeze,val);end	
def ograsm_addiction_level=(val);@actStat.set_stat("ograsm_addiction_level".freeze,val);end	
def semen_addiction_level=(val);@actStat.set_stat("semen_addiction_level".freeze,val);end	
def drug_addiction_damage=(val);@actStat.set_stat("drug_addiction_damage".freeze,val);end	
def ograsm_addiction_damage=(val);@actStat.set_stat("ograsm_addiction_damage".freeze,val);end	
def semen_addiction_damage=(val);@actStat.set_stat("semen_addiction_damage".freeze,val);end	
def sex=(val);@actStat.set_stat("sex".freeze,val);end	

def fetishPeePee(tmpOnOff=true)
	if tmpOnOff
		self.battle_stat.set_stat_m("urinary_level".freeze,5000,[2,3])
	else
		self.battle_stat.set_stat_m("urinary_level".freeze,1,[2,3])
	end
end
	
def fetishPooPoo(tmpOnOff=true)
	if tmpOnOff
		self.battle_stat.set_stat_m("defecate_level".freeze,5000,[2,3])
	else
		self.battle_stat.set_stat_m("defecate_level".freeze,1,[2,3])
	end
end

def fetishHardcore(tmpOnOff=true)
	if tmpOnOff
		self.battle_stat.set_stat_m("puke_value_normal".freeze,10000,[2,3])
		self.battle_stat.set_stat_m("drug_addiction_level".freeze,5000,[2,3])
		self.battle_stat.set_stat_m("ograsm_addiction_level".freeze,5000,[2,3])
		self.battle_stat.set_stat_m("semen_addiction_level".freeze,5000,[2,3])
		self.battle_stat.set_stat_m("drug_addiction_damage".freeze,5000,[2,3])
		self.battle_stat.set_stat_m("ograsm_addiction_damage".freeze,5000,[2,3])
		self.battle_stat.set_stat_m("semen_addiction_damage".freeze,5000,[2,3])
	else
		self.battle_stat.set_stat_m("puke_value_normal".freeze,0,[2,3])
		self.battle_stat.set_stat_m("drug_addiction_level".freeze,0,[2,3])
		self.battle_stat.set_stat_m("ograsm_addiction_level".freeze,0,[2,3])
		self.battle_stat.set_stat_m("semen_addiction_level".freeze,0,[2,3])
		self.battle_stat.set_stat_m("drug_addiction_damage".freeze,0,[2,3])
		self.battle_stat.set_stat_m("ograsm_addiction_damage".freeze,0,[2,3])
		self.battle_stat.set_stat_m("semen_addiction_damage".freeze,0,[2,3])
	end
end

end


end