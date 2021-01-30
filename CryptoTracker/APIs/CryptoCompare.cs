﻿using CryptoTracker.Helpers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoTracker.APIs {
	class CryptoCompare {

        public class HistoricPrice {
            public int time { get; set; }
            public float high { get; set; } = 0;
            public float low { get; set; } = 0;
            public float open { get; set; } = 0;
            public float close { get; set; } = 0;
            public float volumefrom { get; set; } = 0;
            public float volumeto { get; set; } = 0;

            internal float Average { get; set; } = 0;
            internal string Date { get; set; }
            internal DateTime DateTime { get; set; }
        }

        /* ###############################################################################################
         * Gets the current price of a coin (in the currency set by App.coin)
         * 
         * Arguments: 
         * - crypto: BTC ETH...
         * - market: CCCAGG Bitstamp Bitfinex Coinbase HitBTC Kraken Poloniex
         * 
        */
        internal static double GetPrice(string crypto, string market = "defaultMarket") {
            var currency = App.coin;
            string URL = string.Format("https://min-api.cryptocompare.com/data/price?fsym={0}&tsyms={1}", crypto, currency);

            if (market != "defaultMarket")
                URL += "&e=" + market;

            Uri uri = new Uri(URL);

            try {
                var data = App.GetStringAsync(uri).Result;

                double price = double.Parse(data.Split(":")[1].Replace("}", ""));

				if (price > 99)
					return Math.Round(price, 2);
				else if (price > 10)
					return Math.Round(price, 4);
                else
                    return Math.Round(price, 6);

            }
            catch (Exception) {
                return 0;
            }
        }

        /* ###############################################################################################
         * Gets the current price of a coin (in the currency set by App.coin)
         * 
         * Arguments: 
         * - crypto: BTC ETH...
         * - time: minute hour day
         * - limit: 1 - 2000
         * 
        */
        internal static async Task<List<HistoricPrice>> GetHistoric(string crypto, string time, int limit) {
            var coin = App.coin;

            string URL = string.Format("https://min-api.cryptocompare.com/data/histo{0}?e=CCCAGG&fsym={1}&tsym={2}&limit={3}", time, crypto, coin, limit);

            if (limit == 0)
                URL = string.Format("https://min-api.cryptocompare.com/data/histoday?e=CCCAGG&fsym={0}&tsym={1}&allData=true", crypto, coin);

            
            try {
                var responseString = await App.GetStringAsync(new Uri(URL));

                var response = JsonSerializer.Deserialize<object>(responseString);

                var okey = ((JsonElement)response).GetProperty("Response").ToString();

                // TODO: add NOT OKEY value
                if (okey == "")
                    return new List<HistoricPrice>(3);
                
                var data = ((JsonElement)response).GetProperty("Data").ToString();
                var historic = JsonSerializer.Deserialize<List<HistoricPrice>>(data);

                // Add calculation of dates and average values
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
				foreach (var h in historic) {
                    h.Average = (h.high + h.low) / 2;
                    DateTime d = dtDateTime.AddSeconds(h.time).ToLocalTime();
                    h.DateTime = d;
                    h.Date = d.ToString();
                }

                return historic;
            }
            catch (Exception ex) {
                return new List<HistoricPrice>(3);
            }
        }


        /* ###############################################################################################
         * Gets the exchanges for a crypto (with the price and volume)
         * TODO: unused endpoint
         * 
         * Arguments: 
         * - crypto: BTC ETH...
         * 
        */
        internal async static Task GetExchanges(string crypto) {
            var currency = App.coin;

            var URL = string.Format("https://min-api.cryptocompare.com/data/top/exchanges?fsym={0}&tsym={1}&limit={2}", crypto, currency, 8);

            try {
                var responseString = await App.GetStringFromUrlAsync(URL);

                var response = JsonSerializer.Deserialize<object>(responseString);

                var okey = ((JsonElement)response).GetProperty("Response").ToString();

                var data = ((JsonElement)response).GetProperty("Data").ToString();
                var exchanges = JsonSerializer.Deserialize<List<Exchange>>(data);
            }
            catch (Exception ex) {
                var message = ex.Message;
            }
        }

        public class Exchange {
            public string exchange { get; set; } = "NULL";
            public string fromSymbol { get; set; } = "NULL";
            public string toSymbol { get; set; } = "NULL";
            public double volume24h { get; set; } = 0;
            public double volume24hTo { get; set; } = 0;
            public double price { get; set; } = 0;
            public string exchangeGrade { get; set; } = "null";
        }
    }
}
