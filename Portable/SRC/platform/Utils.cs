using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Input;
//using AppAnalytics.SystemInfo;

namespace AppAnalytics
{
    enum TouchAction
    {
        Up,
        Down,
        Move
    }
    internal class TouchPoint
    {
        TouchAction mAction = TouchAction.Down;

        public TouchAction Action { get { return mAction; } set { mAction = value;} }

        public float X { get; set; }
        public float Y { get; set; }
    }

    internal class Vector2
    {
        private float _X;

        public float X
        {
            get { return _X; }
            set { _X = value; }
        }
        private float _Y;

        public float Y
        {
            get { return _Y; }
            set { _Y = value; }
        }

        public Vector2(float x, float y)
        {
            this._X = x;
            this._Y = y;
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2 operator *(Vector2 v1, float m)
        {
            return new Vector2(v1.X * m, v1.Y * m);
        }

        public static float operator *(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public static Vector2 operator /(Vector2 v1, float m)
        {
            return new Vector2(v1.X / m, v1.Y / m);
        }

        public static float Distance(Vector2 v1, Vector2 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2));
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }
    }

    static class Utils
    {
        static private Version _cache = new Version("0.0.0.0");

//        public static Task<string> GetOSAsync()
//        {
//             var t = new TaskCompletionSource<string>();
//             var w = new WebView();
//             w.AllowedScriptNotifyUris = WebView.AnyScriptNotifyUri;
//             w.NavigateToString("<html />");
//             NotifyEventHandler h = null;
//             h = (s, e) =>
//             {
//                 try
//                 {
//                     var match = Regex.Match(e.Value, @"\d+(\.\d+)?");
//                     if (match.Success)
//                         t.SetResult(match.Value);
//                     else
//                         t.SetResult("Unknowm");
//                 }
//                 catch (Exception ex) { t.SetException(ex); }
//                 finally { /* release */ w.ScriptNotify -= h; }
//             };
//             w.ScriptNotify += h;
//             w.InvokeScript("execScript", new[] { "window.external.notify(navigator.appVersion); " });
//             return t.Task;
 //       }

        static public async Task<int> GetOSVersion()
        {
            Version v = new Version("0.0.0.0"); 

            _cache = v;
            return 0;
        }
        
        static public Version OSVersion
        {
            get
            {
                return _cache;
            }
        }
    }
}
