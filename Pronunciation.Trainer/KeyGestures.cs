﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Pronunciation.Trainer
{
    public static class KeyGestures
    {
        public static KeyGesture NavigateBack
        {
            get { return new KeyGesture(Key.Left, ModifierKeys.Alt, "Alt+left arrow"); }
        }

        public static KeyGesture NavigateForward
        {
            get { return new KeyGesture(Key.Right, ModifierKeys.Alt, "Alt+right arrow"); }
        }

        public static KeyGesture PlayReference
        {
            get { return new KeyGesture(Key.A, ModifierKeys.Alt, "Alt+A"); }
        }
        
        public static KeyGesture PlayRecorded
        {
            get { return new KeyGesture(Key.S, ModifierKeys.Alt, "Alt+S"); }
        }

        public static KeyGesture StartRecording
        {
            get { return new KeyGesture(Key.R, ModifierKeys.Alt, "Alt+R"); }
        }

        public static KeyGesture PauseAudio
        {
            get { return new KeyGesture(Key.X, ModifierKeys.Alt, "Alt+X"); }
        }

        public static string GetTooltipString(this KeyGesture gesture)
        {
            if (gesture == null)
                return null;

            return string.Format(" ({0})", gesture.DisplayString);
        }
    }
}
