﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using AppAnalytics;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        static bool mTested = false;
        public MainPage()
        {
            this.InitializeComponent();
            this.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(pointerMoved), true);
            AppAnalytics.API.init("2miKqKyeGhoQgvIImX9UfAf17fuwnyvP");
//          this.NavigationCacheMode = NavigationCacheMode.Required;

            // < Testing
 //           AppAnalytics.API.DebugLogEnabled = false;
//             if (!mTested)
//             {
//              TAppAnalytics.BigDataSimulation.PushRandomEvents(40000);
//              TAppAnalytics.Simulation.CheckInsertionTime();
//                 TAppAnalytics.BigDataSimulationPeriodical.PushRandomSamplesWithPeriod(100, 1);
//                 TAppAnalytics.BigDataSimulationPeriodical.PushRandowEventsWithPeriod(100, 1);
//                 mTested = true;
//             }
        }

        void pointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //Debug.WriteLine("-");
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var ui = sender as UIElement;
            ui.CapturePointer(e.Pointer);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Navigation
            this.Frame.Navigate(typeof(SecondRT));
        }
    }
}
