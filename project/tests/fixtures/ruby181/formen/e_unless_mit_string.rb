unless File.exist? "./#{CONFIG['ruby_install_name']}#{CONFIG['EXEEXT']}"
  print "./#{CONFIG['ruby_install_name']} is not found.\n"
  print "Try `make' first, then `make test', please.\n"
  exit 1
end
