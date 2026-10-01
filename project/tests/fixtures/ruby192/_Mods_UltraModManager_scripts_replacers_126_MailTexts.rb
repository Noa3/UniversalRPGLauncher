#==============================================================================
# This script is created by Kslander 
#==============================================================================
#==============================================================================
# Text
#------------------------------------------------------------------------------
# Maps keys to text Strings. Reference at $game_text.
# Based on Text class written by DB
#==============================================================================

class MailText

  attr_accessor :base_folder
  attr_reader :filenameList

  def initialize(base_folder = "Text/CHT/mail")
    @map = {}
    @base_folder = base_folder
    @filenameList = {}
    load_file_and_create_list
  end

  # Separate file name and block id at first colon.
  def split_key(key)
    return if key.nil?
    key.match(/([^:]+):(.+)/)
    [$1, $2]
  end

  def [](key, noError = false)
    file, id = split_key(key)
    return key if file.nil? || id.nil?
    unless @map.has_key?(file)
      @map[file] = load_file(file)
    end
    res = @map[file][id]
    if !res && !noError # 若有錯誤則回報文字FLAG
      res = key
    elsif !res && noError # 若有錯誤則回報""
      res = ""
    end

    return res
  end

  def load_file(file)
    begin
      sth = File.read("#{@base_folder}/#{file}.txt")
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
        # temporary name handling -- give name its own line
        line.match(/(([^:]+): ?)?(.*)/)
        if $1.nil?
          boxes << $3
        else
          body = $3
          # name = $2.gsub(/_/, ' ')  #becaues : crash?
          name = $2 # make : didnt crash?
          boxes << "#{name}\n#{body}"
        end
      end
      blocks[id] = boxes.join("\f")
    end
    blocks
  end

  def load_file_and_create_list
    p "load_file_and_create_list #{@base_folder}"
    Dir[@base_folder + "/*.txt"].select {
      |item|
      p "item =>#{item}"
      next unless File.file?(item)
      @map[item] = load_file(File.basename(item, ".txt"))
      create_key_list(item)
    }

  end

  def create_key_list(file)
    p " create_key_list for file =>#{file}"
    @map[file].keys.each {
      |key|
      @filenameList[key.split("/")[0]] = file
    }
  end

end