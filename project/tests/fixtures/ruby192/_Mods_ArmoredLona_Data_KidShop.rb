        shopNerf = ([([$story_stats["WorldDifficulty"].round,100].min)-0,0].max*0.001) #put the varible u want nerf shop,  if none put 0. var1=max  var2=min.   max-min must with in 100 (ex:weak125, -25)
        charStoreTP =  [(  3000+rand(6000)  )*(1-(shopNerf+shopNerf)),0].max.round  #wildHobo 150+rand(800) hobo 300+rand(1000) #Normie 500+rand(2000) #NonTradeShop 1000+rand(2500) #innkeeper 1000+rand(2500) #storeMarket 3000+rand(6000)
        charStoreExpireDate = $game_date.dateAmt+1+rand(4+$story_stats["Setup_Hardcore"]) #if nil. delete after close.  if < date. delete after nap.
        charStoreHashName = "#{@map_id}_#{get_character(0).id}".to_sym  #can be register with character name like "COCONA"
        #data 0item 1:weapon 2:armor
        #original price? 0,0   custom price? nil,price
        good=[
            [*$data_ItemName["ItemMhWoodenClub"].get_type_and_id,nil,(($data_ItemName["ItemMhWoodenClub"].price)*(shopNerf+1)).round,1],
            [*$data_ItemName["Item2MhWoodenSpear"].get_type_and_id,nil,(($data_ItemName["Item2MhWoodenSpear"].price)*(shopNerf+1)).round,1],
            [*$data_ItemName["Item2MhRake"].get_type_and_id,nil,(($data_ItemName["Item2MhRake"].price)*(shopNerf+1)).round,1],
            [*$data_ItemName["ItemMhSickle"].get_type_and_id,nil,(($data_ItemName["ItemMhSickle"].price)*(shopNerf+1)).round,1],
			[*$data_ItemName["MercHead"].get_type_and_id,nil,(($data_ItemName["MercHead"].price)*(shopNerf+1)).round,1],
			[*$data_ItemName["MercBot"].get_type_and_id,nil,(($data_ItemName["MercBot"].price)*(shopNerf+1)).round,1],
			[*$data_ItemName["MercMidEx"].get_type_and_id,nil,(($data_ItemName["MercMidEx"].price)*(shopNerf+1)).round,1],
			[*$data_ItemName["MercMid"].get_type_and_id,nil,(($data_ItemName["MercMid"].price)*(shopNerf+1)).round,1],
			[*$data_ItemName["MercTop"].get_type_and_id,nil,(($data_ItemName["MercTop"].price)*(shopNerf+1)).round,1],
            [*$data_ItemName["ItemCoin1"].get_type_and_id,nil    ,($data_ItemName["ItemCoin1"].price*1.25).round                ,1+rand(3)],    #ItemCoin1 bank1 relay1.1 shop1.2 ppl1.25 fishPPL1.35S Fishhop1.25
            ]
            manual_trade(good,charStoreHashName,charStoreTP,charStoreExpireDate,noSell=false,noBuy=false)