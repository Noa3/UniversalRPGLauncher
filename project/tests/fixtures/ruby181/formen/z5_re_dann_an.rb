      case word
      when 'Dd'
	@date = words.join(' ')
	return nil
      when 'Dt'
	if words.size >= 2 && words[1] == '""' &&
	    /^(.*)\(([0-9])\)$/ =~ words[0]
	  words[0] = $1
	  words[1] = $2
	end
	@id = words.join(' ')
	return nil
      when 'Os'
	retval << '.TH ' << @id << ' "' << @date << '" "' <<
	  words.join(' ') << '"'
	break
      when 'Sh'
	retval << '.SH'
	@synopsis = (words[0] == 'SYNOPSIS')
	next
      when 'Xr'
	retval << '\\fB' << words.shift <<
	  '\\fP(' << words.shift << ')' << words.shift
	break
      when 'Rs'
	@refauthors = []
	@reftitle = ''
	@refissue = ''
	@refdate = ''
	@refopt = ''
	@reference = true
	break
      when 'An'
	next
      when 'Dl'
	retval << ".nf\n" << '\\&  '
	dl = true
	next
      when 'Ux'
	retval << "UNIX"
	next
      end
