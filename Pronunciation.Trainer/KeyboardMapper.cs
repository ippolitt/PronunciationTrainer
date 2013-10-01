using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Pronunciation.Trainer
{
    public class KeyboardMapper
    {
        private static HashSet<Key> _nonRegularKeys;

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // http://www.pinvoke.net/default.aspx/user32/MapVirtualKey.html
        const uint MAPVK_VK_TO_VSC = 0x00;
        const uint MAPVK_VSC_TO_VK = 0x01;
        const uint MAPVK_VK_TO_CHAR = 0x02;
        const uint MAPVK_VSC_TO_VK_EX = 0x03;
        const uint MAPVK_VK_TO_VSC_EX = 0x04;

        static KeyboardMapper()
        {
            _nonRegularKeys = new HashSet<Key> 
            {
                Key.Enter, 
                Key.Escape,
                Key.Delete,
                Key.Back,
                Key.System
            };
        }

        public static bool IsRegularKey(KeyEventArgs args)
        {
            // When Alt modifier is pressed 'e.Key' contains 'Key.System' and the real key is in the 'e.SystemKey'
            if (args.KeyboardDevice.Modifiers != ModifierKeys.None)
                return false;

            if(_nonRegularKeys.Contains(args.Key))
                return false;

            char keyChar = ToCharInternal(KeyInterop.VirtualKeyFromKey(args.Key));
            return (keyChar != '\0');
        }

        public static char ToChar(Key key)
        {
            char keyChar = ToCharInternal(KeyInterop.VirtualKeyFromKey(key));
            if (keyChar == '\0')
                throw new ArgumentException();

            return keyChar;
        }

        private static char ToCharInternal(int virtualKeyCode)
        {
            // 2 is used to translate into an unshifted character value
            uint nonVirtualKey = MapVirtualKey((uint)virtualKeyCode, MAPVK_VK_TO_CHAR);
            return Convert.ToChar(nonVirtualKey);
        }
    }
}
