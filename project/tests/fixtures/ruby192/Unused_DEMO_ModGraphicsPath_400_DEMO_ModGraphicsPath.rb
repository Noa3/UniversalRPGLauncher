#load ModScripts/Graphics/
#if none load /Graphics/

class << Bitmap
	alias_method :alias_new, :new unless method_defined?(:alias_new)
	def new(*args)
		modPath = "ModScripts/"
		defultPath = args[0]
		args[0] = modPath+defultPath if args[0][0, 8] == "Graphics"
		alias_new(*args)
		rescue
		args[0] = defultPath
		alias_new(*args)
	end
end