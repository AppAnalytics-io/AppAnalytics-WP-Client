using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Testing.Resources;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Xna.Framework.Input.Touch;
 

using TouchLib;

namespace Testing
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Global Transform used to change the position of the Rectangle.
        private TranslateTransform move = new TranslateTransform();
        private ScaleTransform resize = new ScaleTransform();
        private TransformGroup rectangleTransforms = new TransformGroup();

        private Brush stationaryBrush;
        private Brush transformingBrush = new SolidColorBrush(Colors.Orange);

        // Constructor
        public MainPage()
        {
            InitializeComponent();

           // TouchPanel.GetState().

            //Thread workerThread = new Thread(updateLoop);
            //workerThread.Start();

            rectangleTransforms.Children.Add(move);
            rectangleTransforms.Children.Add(resize);
            TestRectangle.RenderTransform = rectangleTransforms;


            // Handle manipulation events.
            TestRectangle.ManipulationStarted +=
                new EventHandler<ManipulationStartedEventArgs>(Rectangle_ManipulationStarted);
            TestRectangle.ManipulationDelta +=
                new EventHandler<ManipulationDeltaEventArgs>(Drag_ManipulationDelta);
            TestRectangle.ManipulationCompleted +=
                new EventHandler<ManipulationCompletedEventArgs>(Rectangle_ManipulationCompleted);

            TouchLib.Detector.init("8QwD7Z4icUkAR5Ka8HgK2wEhvm7zOwzB");
            //workerThread.Join();
           // LayoutRoot.ManipulationCompleted += new EventHandler<ManipulationCompletedEventArgs>(LayoutRoot_ManipulationCompleted);
        }

        void Drag_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Move the rectangle.
            move.X += e.DeltaManipulation.Translation.X;
            move.Y += e.DeltaManipulation.Translation.Y;

            // Resize the rectangle.
            if (e.DeltaManipulation.Scale.X > 0 && e.DeltaManipulation.Scale.Y > 0)
            {
                // Scale the rectangle.
                resize.ScaleX *= e.DeltaManipulation.Scale.X;
                resize.ScaleY *= e.DeltaManipulation.Scale.Y;
            }
            Uri k = this.NavigationService.CurrentSource;
        }

        void Rectangle_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            // Save the original color before changing the color.
            stationaryBrush = TestRectangle.Fill;
            TestRectangle.Fill = transformingBrush;
        }

        void Rectangle_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            // Restore the original color.
            TestRectangle.Fill = stationaryBrush;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Second.xaml", UriKind.Relative));
        }
    }
}