using System;
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

        public static KeyGesture PreviousWord
        {
            get { return new KeyGesture(Key.Up, ModifierKeys.Alt, "Alt+up arrow"); }
        }

        public static KeyGesture NextWord
        {
            get { return new KeyGesture(Key.Down, ModifierKeys.Alt, "Alt+down arrow"); }
        }

        public static KeyGesture ClearText
        {
            get { return new KeyGesture(Key.C, ModifierKeys.Alt, "Alt+C"); }
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

        public static KeyGesture StopAudio
        {
            get { return new KeyGesture(Key.X, ModifierKeys.Alt, "Alt+X"); }
        }

        public static KeyGesture ShowWaveform
        {
            get { return new KeyGesture(Key.W, ModifierKeys.Alt, "Alt+W"); }
        }

        public static KeyGesture ShowHistory
        {
            get { return new KeyGesture(Key.H, ModifierKeys.Alt, "Alt+H"); }
        }
    }
}
