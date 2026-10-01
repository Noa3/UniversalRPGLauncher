#So whole situation about this failsafe:
#last_row_address goes through several references in order to find where game's Bitmap data is stored
#But to go through them it uses RtlMoveMemory which is DLL call
#DLL is being called using Win32API that does not support Bignum (hello RPGMaker VX Ace's obsolette Ruby version)
#so when any reference last_row_address wants to pass is above 1073741823 (1GB RAM basically) it crashes.
#And what's even worse there is no way to replicate this error to find a proper fix for it!
#So this is workaround to at least try to make it not crash even if some visual bugs appear (palette changer might not work or save game screenshot might not appear).

$creating_blank_image = true
$blank_image_data = [1024,1024,Bitmap.new(1024,1024)]#store empty bitmap that's confirmed to be within Fixnum memory
$creating_blank_image = false

class Bitmap
	alias_method :initialize_pre_failsafe, :initialize
	def initialize(value1=nil, value2=nil)
		#WARNING: MONKEY CODE!
		#Simulating initial constructor call. It's either Bitmap.new(FILENAME) or Bitmap.new(width,height) so pass to original initializer.
		if value2.nil?
			initialize_pre_failsafe(value1)
		else
			initialize_pre_failsafe(value1, value2) 
		end
		#checking if all references that last_row_address goes through does not go into Bignum
		its_ok = true
		if (__id__*2+16).class == Fixnum then
			RtlMoveMemory.call(buf=[0].pack('L'), __id__*2+16, 4)
			if (buf.unpack('L')[0]+8).class == Fixnum then
				RtlMoveMemory.call(buf, buf.unpack('L')[0]+8 , 4)
				if (buf.unpack('L')[0]+16).class == Fixnum then
					#everything ok! Phew.
					its_ok = true
				else
					its_ok = false
				end
			else
				its_ok = false
			end
		else
			its_ok = false
		end
		
		if !its_ok then #we're in deep trouble.
			if $creating_blank_image then
				#Everything is fucked up. I mean it. What can I do when even very first Bitmap I create will cause crash ! ? ! ?
				raise "Unexpected situation: very first bitmap has gone into 1GB+ ram addresses, RPGMaker VX Ace is so bad that we can't fix this causing crash later. Try restarting game."
			end
			@replace_row_address = $blank_image_data[2].last_row_address
		end
	end
	
	#if initialize detected Bignum address, we go back to failsafe.
	alias_method :last_row_address_pre_failsafe, :last_row_address
	def last_row_address
		return last_row_address_pre_failsafe if @replace_row_address.nil?
		return @replace_row_address
	end
end