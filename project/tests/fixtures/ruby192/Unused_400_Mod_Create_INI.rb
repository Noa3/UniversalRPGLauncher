#by 417 added in 0730
tgtDir = "ModScripts/"
unless Dir.exist?(tgtDir)
	Dir.mkdir(tgtDir)
	puts "Directory created: #{tgtDir}"
else
	puts "Directory already exists: #{tgtDir}"
end

tgtDir = "ModScripts/_Mods/"
unless Dir.exist?(tgtDir)
	Dir.mkdir(tgtDir)
	puts "Directory created: #{tgtDir}"
else
	puts "Directory already exists: #{tgtDir}"
end

userDir = System_Settings::USER_DATA_PATH
unless Dir.exist?(userDir)
	Dir.mkdir(userDir)
	puts "Directory created: #{userDir}"
else
	puts "Directory already exists: #{userDir}"
end


if !File.exists?(userDir + "GameMods.ini")
	new_file = IniFile.new
	# set properties
	new_file["Mods"]={}
	# pretty print object
	p new_file
	# set file path
	new_file.filename = userDir + "GameMods.ini"
	# save file
	new_file.write()
end
$ModINI = IniFile.load(userDir + "GameMods.ini")
