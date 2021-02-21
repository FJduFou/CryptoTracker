﻿using CryptoTracker.APIs;
using CryptoTracker.Constants;
using CryptoTracker.Helpers;
using CryptoTracker.Models;
using CryptoTracker.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CryptoTracker {
	sealed partial class App : Application {
        /// <summary>
        /// Efficient socket usage
        /// https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        /// </summary>
        internal static HttpClient Client = new HttpClient();

        internal static string currency       = "EUR";
        internal static string currencySymbol = "€";

        private LocalSettings _LocalSettings = new LocalSettings(); 

        internal static List<CoinBasicInfo> coinList = new List<CoinBasicInfo>();
        internal static List<string> pinnedCoins;

        internal static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public App() {
            string _theme = _LocalSettings.Get<string>(UserSettingsConstants.Theme);
            string _currency = _LocalSettings.Get<string>(UserSettingsConstants.Currency);
            string _pinned = _LocalSettings.Get<string>(UserSettingsConstants.PinnedCoins);
			
            pinnedCoins = new List<string>(_pinned.Split(new char[] { '|' }));
            pinnedCoins.Remove("");

            switch (_theme) {
				case "Light":
					RequestedTheme = ApplicationTheme.Light;
					break;
				case "Dark":
					RequestedTheme = ApplicationTheme.Dark;
					break;
                default:
                    RequestedTheme = (new UISettings().GetColorValue(UIColorType.Background) == Colors.Black) ? ApplicationTheme.Dark : ApplicationTheme.Light;
                    break;
            }

            currency = _currency ;
            switch (_currency ) {
                default:
                case "EUR":
                    currencySymbol = "€";
                    break;
                case "GBP":
                    currencySymbol = "£";
                    break;
                case "USD":
                case "CAD":
                case "AUD":
                case "MXN":
                    currencySymbol = "$";
                    break;
                case "CNY":
                case "JPY":
                    currencySymbol = "¥";
                    break;
                case "INR":
                    currencySymbol = "₹";
                    break;
            }

            /// Register services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton(RestService.For<ICryptoCompare>("https://min-api.cryptocompare.com/"))
                .BuildServiceProvider());


            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;
        }
		// #########################################################################################
		protected override void OnLaunched(LaunchActivatedEventArgs e) {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null) {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Cargar el estado de la aplicación suspendida previamente
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false) {
                if (rootFrame.Content == null) {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(900, 550));
                Window.Current.Activate();
            }
#if !DEBUG
            AppCenter.Start("37e61258-8639-47d6-9f6a-d47d54cd8ad5", typeof(Analytics), typeof(Crashes));
#endif
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) {
            Analytics.TrackEvent("UNHANDLED1: " + e.Message);
        }

        // ###############################################################################################
        internal static void UpdatePinnedCoins() {
            if (App.pinnedCoins.Count > 0) { 
                string s = "";
                foreach (var item in App.pinnedCoins) {
                    s += item + "|";
                }
                s = s.Remove(s.Length - 1);
                App.localSettings.Values[UserSettingsConstants.PinnedCoins] = s;
            }
        }

        /* ###############################################################################################
         * Gets the list of coins and saves it under App.coinList
         * API: Github
        */
        internal async static Task GetCoinList() {
            // check cache before sending an unnecesary request
            if (localSettings.Values["coinListDate"] != null) {
                DateTime lastUpdate = DateTime.FromOADate((double)localSettings.Values["coinListDate"]);
                var days = DateTime.Today.CompareTo(lastUpdate);

				coinList = await LocalStorageHelper.ReadObject<List<CoinBasicInfo>>("coinList");

				// if empty list OR old cache -> refresh
				if (coinList.Count == 0 || days > 7) {
                    coinList = await GitHub.GetAllCoins();
                }
            }
			else {
                coinList = await GitHub.GetAllCoins();
            }
        }

        
        // ###############################################################################################
        //  (GET) coin description
        internal static async Task<string> GetCoinDescription(string crypto, int lines = 5) {
            String URL = string.Format("https://krausefx.github.io/crypto-summaries/coins/{0}-{1}.txt", crypto.ToLower(), lines);
            Uri uri = new Uri(URL);

            try {
                string data = await GetStringAsync(uri);
                return data;

            } catch (Exception) {
                return "No description found for this coin.";
            }
        }

        /// ###############################################################################################
        /// <summary>
        /// do NOT mess with async methods...
        /// 
        /// Thank god I found this article (thanks Stephen Cleary)
        /// http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
        /// 
        /// </summary>
        // TODO: removing ConfigureAwait breaks everything... why?
        internal static async Task<string> GetStringAsync(Uri uri) {
            return await Client.GetStringAsync(uri).ConfigureAwait(false);
        }
        internal static async Task<string> GetStringFromUrlAsync(string url) {
            return await Client.GetStringAsync(new Uri(url)).ConfigureAwait(false);

        }
    }

}

