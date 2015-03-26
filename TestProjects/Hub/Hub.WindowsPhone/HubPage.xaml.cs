using Hub.Common;
using Hub.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using AppAnalytics;

using  System.Reflection ;
 
// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace Hub
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");


//         private void _pointerMoved(object sender, PointerRoutedEventArgs e)
//         {
//             //var CurrentPoint = e.CurrentPoint;
//         }

        public HubPage()
        {
            this.InitializeComponent();

//            this.PointerMoved += _pointerMoved;
            
            AppAnalytics.API.init("IcawZz1SbQA1TA8upkLqgPDg5hka6VMQ");
            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(pointerMoved), true); 
             
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

        }

        void testDgEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("1");
        }
        void lost(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("lost");
        }
        void testingF(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("testing");
        }
        void registerAllChildren(UIElement aRoot)
        {
            if (null != aRoot)
            {

                var currentPage = aRoot;

                var tst = currentPage as Windows.UI.Xaml.Controls.Hub;
               var tst2 = currentPage as Windows.UI.Xaml.Controls.HubSection;
                if (null != tst )//|| tst2 != null)
                {
                    int o = 1;
                    tst.ManipulationMode = ManipulationModes.None;
                    //tst.ena
                    //tst.IsHitTestVisible = false;
                    //tst.Add
                }
                if (null != tst2)
                {
                    //tst2.di
                    tst2.DragEnter += testDgEnter;
                    tst2.PointerCaptureLost += lost;
                    tst2.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(testingF), true);
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(currentPage); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(currentPage, i);

//                    AAGestureRecognizer.Instance.RegisterUIElement(currentPage as UIElement);
                    if (VisualTreeHelper.GetChildrenCount(child) > 0)
                    {
                        registerAllChildren(child as UIElement); // recurs. enumerate children
                    }
                }
            }
        }

        void pointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("-");
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroups = await SampleDataSource.GetGroupsAsync();
            this.DefaultViewModel["Groups"] = sampleDataGroups;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        /// <summary>
        /// Shows the details of a clicked group in the <see cref="SectionPage"/>.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">Details about the click event.</param>
        private void GroupSection_ItemClick(object sender, ItemClickEventArgs e)
        { 
            var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(SectionPage), groupId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Shows the details of an item clicked on in the <see cref="ItemPage"/>
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">Defaults about the click event.</param>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(ItemPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Frame_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var fg = sender as Frame;
           // fg.CapturePointer(e.Pointer);
        }

        private void Frame_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {

        }

        private void Frame_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var t = sender as Frame;
            e.Handled = false;
            //t.CapturePointer(e.Pointer);
        }

        private void Rectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("==pressed");

        }

        private void Rectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("==moved");

        }

        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("<<<==moved");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //registerAllChildren(this);
        }

        private void Rectangle_PointerPressed_1(object sender, PointerRoutedEventArgs e)
        {
            var t = sender as UIElement;
            //t.CapturePointer(e.Pointer);
        }
    }
}