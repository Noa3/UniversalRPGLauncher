module HimeCut
  class << Bitmap
    alias_method :alias_new_himecut, :new unless method_defined?(:alias_new_himecut)

    def new(*args)
      modPath = File.expand_path("../", __FILE__) 
      defaultPath = args[0]

      if defaultPath.nil?
        raise ArgumentError, "defaultPath cannot be nil"
      end

      if defaultPath.start_with?("Graphics")
        args[0] = File.join(modPath, defaultPath)
      end

      alias_new_himecut(*args)
    rescue => e
      args[0] = defaultPath
      alias_new_himecut(*args)
    end
  end
end
