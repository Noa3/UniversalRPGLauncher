print <<EOS
	val.gsub!(/\\\\$\\\\$|x/) do |var|
	'$'
end
EOS
