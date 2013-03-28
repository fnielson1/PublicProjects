#define DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace Monitor_Client
{
	/// <summary>
	/// A class that manages a global low level keyboard hook
	/// </summary>
	class GlobalKeyboardHook 
    {
		#region Constant, Structure and Delegate Definitions
		/// <summary>
		/// defines the callback type for the hook
		/// </summary>
		public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);


		public struct keyboardHookStruct 
        {
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;

        /// <summary>
        /// windows virtual key codes
        /// </summary>
        private const byte VK_RETURN = 0X0D; //Enter
        private const byte VK_SPACE = 0X20; //Space
        private const byte VK_SHIFT = 0x10;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_CAPITAL = 0x14; // Caps lock
        private const byte VK_MENU = 0x12; // alt
        private const byte VK_LMENU = 0xA4; // Left alt
        private const byte VK_RMENU = 0xA5; // Right alt
		#endregion

		#region Instance Variables
#if DEBUG
        /// <summary>
        /// A debug log for our hook
        /// </summary>
        private System.IO.StreamWriter _debugLog = new System.IO.StreamWriter(System.IO.Path.Combine(Environment.GetFolderPath
            (Environment.SpecialFolder.MyDocuments), "client.log"));
#endif

		/// <summary>
		/// The collections of keys to watch for
		/// </summary>
		public List<Keys> HookedKeys = new List<Keys>();

		/// <summary>
		/// Handle to the hook, need this to unhook and call the next hook
		/// </summary>
		private IntPtr hhook = IntPtr.Zero;
        
        /// <summary>
        /// Whether the special key is being pressed or not (that will show the form)
        /// </summary>
        private bool _isDownSpecial;
		#endregion

        #region Properties
        /// <summary>
        /// Returns true if the special key is being pressed
        /// </summary>
        public bool IsDownSpecial { get { return _isDownSpecial; } }
        #endregion

        #region Events
        /// <summary>
		/// Occurs when one of the hooked keys is pressed
		/// </summary>
		public event KeyEventHandler KeyDown;
		/// <summary>
		/// Occurs when one of the hooked keys is released
		/// </summary>
		public event KeyEventHandler KeyUp;
		#endregion


		#region Constructors and Destructors
		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalKeyboardHook"/> class and installs the keyboard hook.
		/// </summary>
		public GlobalKeyboardHook() 
        {
            _debugLog.AutoFlush = true;
			hook();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="GlobalKeyboardHook"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
		/// </summary>
		~GlobalKeyboardHook() 
        {
			unhook();
		}
		#endregion

		#region Methods
		/// <summary>
		/// Installs the global hook
		/// </summary>
		public void hook() 
        {
			IntPtr hInstance = LoadLibrary("User32");
			hhook = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, hInstance, 0);
		}

		/// <summary>
		/// Uninstalls the global hook and aborts the Monitor Hook thread
		/// </summary>
        /// <returns>True if the unhooked successfully</returns>
		public bool unhook() 
        {
			return UnhookWindowsHookEx(hhook);
		}
        
		/// <summary>
		/// The callback for the keyboard hook
		/// </summary>
		/// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
		/// <param name="wParam">The event type</param>
		/// <param name="lParam">The keyhook event information</param>
		/// <returns></returns>
		public int hookProc(int code, int wParam, ref keyboardHookStruct lParam) 
        {
            // Use a new thread to check the key data so that we can return from the
            // hook immediately so that windows will not disconnect the hook. Windows
            // will do this if the hook is taking too much time to process. 
            // Our hook probably takes too much time when the form is currently
            // trying to connect to the server
            object keyData = new object[] { code, wParam, lParam };
            ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessKeyEvent), keyData);
            
#if DEBUG
            _debugLog.WriteLine(DateTime.Now);
            if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                _debugLog.Write("KeyDown: ");
            else
                _debugLog.Write("KeyUp: ");
            _debugLog.WriteLine(((Keys)lParam.vkCode).ToString() + _debugLog.NewLine);
#endif
            
            // FOR REFERENCE ONLY
            //if (code >= 0)
            //{
            //    Keys key = (Keys)lParam.vkCode;
            //    if (HookedKeys.Contains(key))
            //    {
            //        bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
            //        bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);
            //        bool isDownControl = ((GetKeyState(VK_CONTROL) & 0x80) == 0x80 ? true : false);

            //        KeyEventArgs kea = new KeyEventArgs(key);
            //        _isDownSpecial = false;

            //        if (isDownShift && !isDownControl)
            //            _isDownSpecial = true;

            //        if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
            //        {
            //            KeyDown(this, kea);
            //        }
            //        else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
            //        {
            //            KeyUp(this, kea);
            //        }

            //        if (kea.Handled)
            //            return 1;
            //    }
            //}
			return CallNextHookEx(hhook, code, wParam, ref lParam); // Always call in any hook you ever make
		}
        private static object keyEventLock = new object();
        /// <summary>
        /// Process the key event and see if we shall show the form
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private void ProcessKeyEvent(object keyData)
        {
            lock (keyEventLock)
            {
                object[] keyDataArr = (object[])keyData;
                int code = (int)keyDataArr[0];
                int wParam = (int)keyDataArr[1];
                keyboardHookStruct lParam = (keyboardHookStruct)keyDataArr[2];

                if (code >= 0)
                {
                    Keys key = (Keys)lParam.vkCode;
                    if (HookedKeys.Contains(key))
                    {
                        key = AddModifiers(key);
                        KeyEventArgs kea = new KeyEventArgs(key);
                        _isDownSpecial = false;

                        if (key.HasFlag(Keys.Alt))
                            _isDownSpecial = true;

#if DEBUG
                        _debugLog.WriteLine("_isDownSpecial: " + _isDownSpecial + _debugLog.NewLine + "------------------------" + _debugLog.NewLine);
#endif

                        if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                        {
                            KeyDown(this, kea);
                        }
                        else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                        {
                            KeyUp(this, kea);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks whether Alt, Shift, Control or CapsLock
        /// is pressed at the same time as the hooked key.
        /// Modifies the keyCode to include the pressed keys.
        /// </summary>
        /// <param name="key">The key that was pressed</param>
        private Keys AddModifiers(Keys key)
        {
            //CapsLock
            if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) key = key | Keys.CapsLock;
            //Shift
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) key = key | Keys.Shift;
            //Ctrl
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) key = key | Keys.Control;
            //Alt
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) key = key | Keys.Alt;

            return key;
        }
		#endregion

		#region DLL imports
		/// <summary>
		/// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
		/// </summary>
		/// <param name="idHook">The id of the event you want to hook</param>
		/// <param name="callback">The callback.</param>
		/// <param name="hInstance">The handle you want to attach the event to, can be null</param>
		/// <param name="threadId">The thread you want to attach the event to, can be null</param>
		/// <returns>a handle to the desired hook</returns>
		[DllImport("user32.dll")]
		static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

		/// <summary>
		/// Unhooks the windows hook.
		/// </summary>
		/// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
		/// <returns>True if successful, false otherwise</returns>
		[DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(IntPtr hInstance);

		/// <summary>
		/// Calls the next hook.
		/// </summary>
		/// <param name="idHook">The hook id</param>
		/// <param name="nCode">The hook code</param>
		/// <param name="wParam">The wparam.</param>
		/// <param name="lParam">The lparam.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

		/// <summary>
		/// Loads the library.
		/// </summary>
		/// <param name="lpFileName">Name of the library</param>
		/// <returns>A handle to the library</returns>
		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// Get the current state of the specified key
        /// </summary>
        /// <param name="vKey">The key to get</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);
		#endregion
	}
}
