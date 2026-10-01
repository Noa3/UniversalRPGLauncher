print %[
# This file was created by #{mkconfig} when ruby was built.  Any
# changes made to this file will be lost the next time ruby is built.

module Config
  RUBY_VERSION == "#{version}" or
    raise "ruby lib version (#{version}) doesn't match executable version (\#{RUBY_VERSION})"

]
