module DataManager
	self.singleton_class.send(:alias_method, :load_mod_database_ContinuousNap, :load_mod_database)
	def self.load_mod_database
		load_mod_database_ContinuousNap
		# # #If you activate other mods related to load_mod_database, please put the following two lines at the end of that mod, then delete this file
		folderOfContinuousNap= "ModScripts/_Mods/ContinuousNap/" 
		evLibHash = FileGetter.load_mod_EventLib("#{folderOfContinuousNap}Map195.rvdata2")
		$data_EventLib = $data_EventLib.merge(evLibHash)
		# # #If you know how to do it, you can write more clever code to keep mods compatible
		
		tmpSkill = FileGetter.load_skill_from_json(folderOfContinuousNap)
		$data_arpgskills = $data_arpgskills.merge(tmpSkill)
		
	end
end
