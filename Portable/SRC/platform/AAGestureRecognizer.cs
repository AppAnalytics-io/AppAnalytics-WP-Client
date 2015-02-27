using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace AppAnalytics
{
    // change to hidden (internal) later - to-do

    public class AAGestureRecognizer
    {
        protected static AAGestureRecognizer mInstance = null;
        public static AAGestureRecognizer Instance
        { get { return mInstance ?? (mInstance = new AAGestureRecognizer()); } }
        protected AAGestureRecognizer()
        {
        }

        List<UIElement> mElementList = new List<UIElement>();

        public bool RegisterUIElement( UIElement aElement)
        {
            if (null == aElement || mElementList.Contains(aElement)) return false;

            aElement.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(globalPointerMovedHook), false);
            aElement.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(globalPointerPressedHook), false);
            mElementList.Add(aElement);

            return true;
        }


        private void globalPointerMovedHook(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("moved");
        }
        private void globalPointerPressedHook(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("pressed");
        }
    }
}
