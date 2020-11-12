# BoomTrader

![BoomTrader](https://boomtrader.info/wp-content/uploads/2020/10/nastrojka-2.png)

> `Donate: `   
> USDT (TRC20): TVZYRSS2oY1gNcxSokhUtcJ3aRgvsmetCR    
> BTC: 13yqAtVZL4PgTFU7fEZ3Y1ZZBNAp5JunCq    
> ETH (ERC20): 0x5d680c19b566020e2297b4a5905ef302ac03cf14    



Market neutral basket trading bot for Binance Futures
THE ALGORITHM OF THE BOT
The bot tracks discrepancies in real time (for selected futures contracts from the General list) and automatically generates currency groups for Long and Short baskets. When the selected Entry spread value is reached% the bot enters trades, buys and sells baskets at the same time (I recommend 4X4, with these settings, the impact of each individual futures is only 12.5%). this way, the bot generates a synthetic index for Long and Short positions. When trading a synthetic index against a synthetic index, the influence of each individual instrument on trading is reduced, and divergence and convergence are improved (faster and more smoothly), due to the presence of volatile altcoins. If after entering a trade, the Delta between the index start to converge, and the bot will see the drawdown of up to Average before%, the basket will be averaged, but not 4X4 compromising some entrance and 1X1 the most unfavorable P&L Long against the most unfavorable P&L Short, thereby improving the price of entry only to those coins which Calculated P&L is the biggest drawdown, and this method of averaging increases the basket yield even more. If the Delta between the buckets continues to expand after the entry and reaches Close profit$, the trailing stop will be activated, and when Calculated P&L$ is rolled back by 10%, all positions will be closed. The bot will start a new cycle, but since we specify the volume of positions in%, all the profit received will be automatically reinvested.



Description: https://boomtrader.info

