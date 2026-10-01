return "" if desc.nil?
begin
  desc.gsub "\\n", "\n"
rescue => e
  p "Got #{e.message}"
  ""
end
