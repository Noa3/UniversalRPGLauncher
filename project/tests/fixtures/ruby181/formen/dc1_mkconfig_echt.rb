print <<EOS
  CONFIG["ruby_version"] = "$(MAJOR).$(MINOR)"
  CONFIG["compile_dir"] = "#{Dir.pwd}"
end
EOS
$stdout.flush
