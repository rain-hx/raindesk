using System;

namespace raindesk.Util
{
    public class KeyUtil
    {
        public class KeyProp
        {
            public Windows.System.VirtualKey key;
            public bool printable;
            public char character;
            public bool shift;
            public bool ctrl;
            public bool alt;
            public int type; //sideband
            public string s;    //sideband
        };

        public static void KeyToChar(Windows.System.VirtualKey key,
            ref KeyProp keyProp,
            bool downCap = false,
            bool downShift = false
            )
        {
            bool iscap;
            bool caplock = false;
            bool shift;

            keyProp.key = key;

            var keyState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftMenu);
            keyProp.alt = keyState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            keyState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftControl);
            keyProp.ctrl = keyState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            keyState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftShift);
            keyProp.shift = keyState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (keyProp.alt || keyProp.ctrl)
            {
                keyProp.printable = false;
                keyProp.type = 1;
            }
            else
            {
                keyProp.printable = true;
                keyProp.type = 0;
            }

            shift = downShift || keyProp.shift;
            iscap = downCap || ((caplock && !shift) || (!caplock && shift));

            switch (key)
            {
                case Windows.System.VirtualKey.Enter: keyProp.character = '\n'; return;
                case Windows.System.VirtualKey.A: keyProp.character = (iscap ? 'A' : 'a'); return;
                case Windows.System.VirtualKey.B: keyProp.character = (iscap ? 'B' : 'b'); return;
                case Windows.System.VirtualKey.C: keyProp.character = (iscap ? 'C' : 'c'); return;
                case Windows.System.VirtualKey.D: keyProp.character = (iscap ? 'D' : 'd'); return;
                case Windows.System.VirtualKey.E: keyProp.character = (iscap ? 'E' : 'e'); return;
                case Windows.System.VirtualKey.F: keyProp.character = (iscap ? 'F' : 'f'); return;
                case Windows.System.VirtualKey.G: keyProp.character = (iscap ? 'G' : 'g'); return;
                case Windows.System.VirtualKey.H: keyProp.character = (iscap ? 'H' : 'h'); return;
                case Windows.System.VirtualKey.I: keyProp.character = (iscap ? 'I' : 'i'); return;
                case Windows.System.VirtualKey.J: keyProp.character = (iscap ? 'J' : 'j'); return;
                case Windows.System.VirtualKey.K: keyProp.character = (iscap ? 'K' : 'k'); return;
                case Windows.System.VirtualKey.L: keyProp.character = (iscap ? 'L' : 'l'); return;
                case Windows.System.VirtualKey.M: keyProp.character = (iscap ? 'M' : 'm'); return;
                case Windows.System.VirtualKey.N: keyProp.character = (iscap ? 'N' : 'n'); return;
                case Windows.System.VirtualKey.O: keyProp.character = (iscap ? 'O' : 'o'); return;
                case Windows.System.VirtualKey.P: keyProp.character = (iscap ? 'P' : 'p'); return;
                case Windows.System.VirtualKey.Q: keyProp.character = (iscap ? 'Q' : 'q'); return;
                case Windows.System.VirtualKey.R: keyProp.character = (iscap ? 'R' : 'r'); return;
                case Windows.System.VirtualKey.S: keyProp.character = (iscap ? 'S' : 's'); return;
                case Windows.System.VirtualKey.T: keyProp.character = (iscap ? 'T' : 't'); return;
                case Windows.System.VirtualKey.U: keyProp.character = (iscap ? 'U' : 'u'); return;
                case Windows.System.VirtualKey.V: keyProp.character = (iscap ? 'V' : 'v'); return;
                case Windows.System.VirtualKey.W: keyProp.character = (iscap ? 'W' : 'w'); return;
                case Windows.System.VirtualKey.X: keyProp.character = (iscap ? 'X' : 'x'); return;
                case Windows.System.VirtualKey.Y: keyProp.character = (iscap ? 'Y' : 'y'); return;
                case Windows.System.VirtualKey.Z: keyProp.character = (iscap ? 'Z' : 'z'); return;
                case Windows.System.VirtualKey.Number0: keyProp.character = (shift ? ')' : '0'); return;
                case Windows.System.VirtualKey.Number1: keyProp.character = (shift ? '!' : '1'); return;
                case Windows.System.VirtualKey.Number2: keyProp.character = (shift ? '@' : '2'); return;
                case Windows.System.VirtualKey.Number3: keyProp.character = (shift ? '#' : '3'); return;
                case Windows.System.VirtualKey.Number4: keyProp.character = (shift ? '$' : '4'); return;
                case Windows.System.VirtualKey.Number5: keyProp.character = (shift ? '%' : '5'); return;
                case Windows.System.VirtualKey.Number6: keyProp.character = (shift ? '^' : '6'); return;
                case Windows.System.VirtualKey.Number7: keyProp.character = (shift ? '&' : '7'); return;
                case Windows.System.VirtualKey.Number8: keyProp.character = (shift ? '*' : '8'); return;
                case Windows.System.VirtualKey.Number9: keyProp.character = (shift ? '(' : '9'); return;
                case Windows.System.VirtualKey.Add: keyProp.character = '+'; return;
                case Windows.System.VirtualKey.Subtract: keyProp.character = '-'; return;
                case Windows.System.VirtualKey.Decimal: keyProp.character = '.'; return;
                case Windows.System.VirtualKey.Divide: keyProp.character = '/'; return;
                case Windows.System.VirtualKey.Multiply: keyProp.character = '*'; return;
                case Windows.System.VirtualKey.Space: keyProp.character = ' '; return;
                case Windows.System.VirtualKey.Tab: keyProp.character = '\t'; return;

                default:
                    keyProp.type = 1;
                    keyProp.printable = false;
                    keyProp.character = '\x00';
                    return;
            }
        }


    }
}
