      case word
      when 'Re'
	retval << "\n"

	# authors
	while @refauthors.size > 1
	  retval << @refauthors.shift << ', '
	end
	retval << 'and ' unless retval.empty?
	retval << @refauthors.shift

	# title 
	retval << ', \\fI' << @reftitle << '\\fP'

	# issue
	retval << ', ' << @refissue unless @refissue.empty?

	# date
	retval << ', ' << @refdate unless @refdate.empty?

	# optional info
	retval << ', ' << @refopt unless @refopt.empty?

	retval << ".\n"

	@reference = false
	break
end
