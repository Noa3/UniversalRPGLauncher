if x
  a = 1
elsif /^(?:ac_given_)?srcdir=(.*)/ =~ line
  srcdir = $1.strip
end
