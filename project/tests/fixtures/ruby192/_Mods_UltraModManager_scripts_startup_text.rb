# frozen_string_literal: true

class Text
  attr_accessor :base_folder

  def initialize(base_folder = "Text/CHT")
    @base_folder = base_folder
    @parts = { nil => base_folder }
    @map = {}
    @map[nil] = {}
  end

  def split_key(key)
    return if key.nil?
    a = key.split(":")
    a.size == 2 ? [nil, a[0], a[1]] : a
  end

  def [](key, noError = false)
    part, file, id = split_key(key)
    return key if file.nil? || id.nil? || !@map.include?(part)
    unless @map[part].has_key?(file)
      @map[part][file] = load_file(part, file)
    end
    res = @map[part][file][id]
    if !res && !noError # 若有錯誤則回報文字FLAG
      res = key
    elsif !res && noError # 若有錯誤則回報""
      res = ""
    end
    res
  end

  def load_file(part = nil, file)
    begin
      sth = File.read("#{@parts[part]}/#{file}.txt")
      return parse(sth.to_s.encode("utf-8"))
    rescue => ex
      msgbox "ERROR: missing translation file #{@parts[part]}/#{file}.txt"
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

  def add_part(part_id, folder)
    @parts[part_id] = folder
    @map[part_id] = {} unless @map.include? part_id
  end
end

$lang = $LonaINI["LonaRPG"]["Language"]
$game_text = Text.new("Text/#{$lang}")
$mod_manager.link_texts
