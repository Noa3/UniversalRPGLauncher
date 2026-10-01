  if /^s[%,]@program_transform_name@[%,]s,(.*)/ =~ line
    next if $install_name
    ptn = $1.sub(/\$\$/, '$').split(/,/)	#'
    v_fast << "  CONFIG[\"ruby_install_name\"] = \"" + "ruby".sub(/#{ptn[0]}/,ptn[1]) + "\"\n"
  elsif /^s[%,]@(\w+)@[%,](.*)[%,]/ =~ line
    name = $1
    val = $2 || ""
    next if /^(INSTALL|DEFS|configure_input|srcdir|top_srcdir)$/ =~ name
    next if $install_name and /^RUBY_INSTALL_NAME$/ =~ name
    next if $so_name and /^RUBY_SO_NAME$/ =~  name
    v = "  CONFIG[\"" + name + "\"] = " +
      val.gsub(/\$(?:\$|\{?(\w+)\}?)/) {$1 ? "$(#{$1})" : $&}.dump + "\n"
    if fast[name]
      v_fast << v
    else
      v_others << v
    end
    has_version = true if name == "MAJOR"
  elsif /^(?:ac_given_)?srcdir=(.*)/ =~ line
    srcdir = $1.strip
  elsif /^ac_given_INSTALL=(.*)/ =~ line
    v_fast << "  CONFIG[\"INSTALL\"] = " + $1 + "\n"
  end
