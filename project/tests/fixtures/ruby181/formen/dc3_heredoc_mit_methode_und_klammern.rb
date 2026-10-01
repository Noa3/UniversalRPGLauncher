print <<EOS
  CONFIG.each{|k,v| MAKEFILE_CONFIG[k] = v.dup}
EOS
