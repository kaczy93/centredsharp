/* SPDX-License-Identifier: ZLIB
Copyright (c) 2014 - 2023 Guillaume Vareille http://ysengrin.com
  _________
 /         \ tinyfiledialogsTest.cs v3.15.1 [Nov 19, 2023] zlib licence
 |tiny file| C# bindings created [2015]
 | dialogs |
 \____  ___/ http://tinyfiledialogs.sourceforge.net
      \|     git clone http://git.code.sf.net/p/tinyfiledialogs/code tinyfd
         ____________________________________________
        |                                            |
        |   email: tinyfiledialogs at ysengrin.com   |
        |____________________________________________|

If you like tinyfiledialogs, please upvote my stackoverflow answer
https://stackoverflow.com/a/47651444

- License -
 This software is provided 'as-is', without any express or implied
 warranty.  In no event will the authors be held liable for any damages
 arising from the use of this software.
 Permission is granted to anyone to use this software for any purpose,
 including commercial applications, and to alter it and redistribute it
 freely, subject to the following restrictions:
 1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software.  If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
 2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
 3. This notice may not be removed or altered from any source distribution.
*/

using System.Runtime.InteropServices;

namespace CentrED;

public class TinyFileDialogs
{
    private const string LIB_NAME = "tinyfiledialogs";
    
    // cross platform UTF8
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void tinyfd_beep();

    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_notifyPopup(string aTitle, string aMessage, string aIconType);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_messageBox(string aTitle, string aMessage, string aDialogType, string aIconType, int aDefaultButton);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_inputBox(string aTitle, string aMessage, string aDefaultInput);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_saveFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_colorChooser(string aTitle, string aDefaultHexRGB, byte[] aDefaultRGB, byte[] aoResultRGB);

    // windows only utf16
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_notifyPopupW(string aTitle, string aMessage, string aIconType);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_messageBoxW(string aTitle, string aMessage, string aDialogType, string aIconType, int aDefaultButton);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_inputBoxW(string aTitle, string aMessage, string aDefaultInput);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_saveFileDialogW(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialogW(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_selectFolderDialogW(string aTitle, string aDefaultPathAndFile);
    [DllImport(LIB_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_colorChooserW(string aTitle, string aDefaultHexRGB, byte[] aDefaultRGB, byte[] aoResultRGB);

    // cross platform
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_getGlobalChar(string aCharVariableName);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_getGlobalInt(string aIntVariableName);
    [DllImport(LIB_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_setGlobalInt(string aIntVariableName, int aValue);
        
    private static string stringFromAnsi(IntPtr ptr) // for UTF-8/char
    {
        return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
    }

    private static string stringFromUni(IntPtr ptr) // for UTF-16/wchar_t
    {
        return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
    }

    public static bool TrySelectFolder(string title, string defaultInput, out string result)
    {
        result = stringFromAnsi(tinyfd_selectFolderDialog(title, defaultInput));
        return !string.IsNullOrEmpty(result);
    }

    public static bool TryOpenFile(string title, string defaultPathAndFile, string[] filterPatterns, string singleFilterDescription, bool allowMultipleSelects, out string result)
    {
        result = stringFromAnsi(tinyfd_openFileDialog(title, defaultPathAndFile, filterPatterns.Length, filterPatterns, singleFilterDescription, allowMultipleSelects ? 1 : 0));
        return !string.IsNullOrEmpty(result);
    }
    
    public static bool TrySaveFile(string title, string defaultPathAndFile, string[] filterPatterns, string singleFilterDescription, out string result)
    {
        result = stringFromAnsi(tinyfd_saveFileDialog(title, defaultPathAndFile, filterPatterns.Length, filterPatterns, singleFilterDescription));
        return !string.IsNullOrEmpty(result);
    }
}