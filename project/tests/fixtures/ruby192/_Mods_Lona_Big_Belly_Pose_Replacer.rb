# 103_FileGetter에서 참조

module FileGetter	
	class << self		
		def load_lona_portrait_parts_dir
			dirlist=Dir.glob("Data/Pconfig/Pconfig_lona/poses/*")			

			partsHash=Hash.new
			name_order=Hash.new
			for c in 0...dirlist.length
				fileList=getFileList(dirlist[c]+"/*.json")
				pose_name=dirlist[c].split("/").last.downcase

				# 수정된 스킬 파일 불러오는 부분
				mod_path="ModScripts/_Mods/Lona_Big_Belly/Data/Pconfig/Pconfig_lona/poses/"+dirlist[c].split("/").last
				modfileList=getFileList(mod_path+"/*.json")				
				
				prp "lona pose #{pose_name}"
				for d in 0...fileList.length
					prp "load portrait json: #{fileList[d]}"

					# 기존 경로와 비교하여 같은 경우 모드 경로로 교체
					modfileList.each{|modfilepath|
						if modfilepath.split('/').last == fileList[d].split('/').last
							fileList[d] = modfilepath
						end
					}

					file=File.open(fileList[d])
					parts_config=handle_lona_parts_arr(JSON.decode(file.read()),name_order)		#[name_order,parts]
					partsHash[pose_name].nil? ? partsHash[pose_name]=parts_config[1] : partsHash[pose_name]+=parts_config[1]
					name_order=parts_config[0]
				end
			end

			fileList=getFileList("Data/Pconfig/Pconfig_LayeredNPC/poses/*.json")
			for d in 0...fileList.length
					pose_name = fileList[d][38..-6]  #裁到剩檔名本名
					prp "load portrait json: #{fileList[d]}"
					prp "NPC pose #{pose_name}"
					file=File.open(fileList[d])
					parts_config=handle_lona_parts_arr(JSON.decode(file.read()),name_order)		#[name_order,parts]
					partsHash[pose_name].nil? ? partsHash[pose_name]=parts_config[1] : partsHash[pose_name]+=parts_config[1]
					name_order=parts_config[0]
			end
			
			
			save_data([name_order,partsHash],"Data/lona_pconfig.rvdata2") if FileGetter::WRITING_LIST
			prp "Data/lona_parts.rvdata2 written" if FileGetter::WRITING_LIST
			partsHash.each{
				|key,value|
			}
			return [name_order,partsHash]
		end
	end
end