using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;

namespace RawInput
{
    /// <summary>
    /// Handles raw input from keyboard devices.
    /// </summary>
    public sealed class InputDevice
    {
        #region const definitions

        // The following constants are defined in Windows.h

        private const int RIDEV_INPUTSINK   = 0x00000100;
        private const int RID_INPUT         = 0x10000003;

        private const int FAPPCOMMAND_MASK  = 0xF000;
        private const int FAPPCOMMAND_MOUSE = 0x8000;
        private const int FAPPCOMMAND_OEM   = 0x1000;

        private const int RIM_TYPEMOUSE     = 0;
        private const int RIM_TYPEKEYBOARD  = 1;
        private const int RIM_TYPEHID       = 2;

        private const int RIDI_DEVICENAME   = 0x20000007;
        
        private const int WM_KEYDOWN	    = 0x0100;
        private const int WM_KEYUP          = 0x101;
        private const int WM_SYSKEYDOWN     = 0x0104;
        private const int WM_SYSKEYUP       = 0x105;
		private const int WM_INPUT		    = 0x00FF;
        private const int VK_OEM_CLEAR      = 0xFE;
        private const int VK_LAST_KEY       = VK_OEM_CLEAR; // this is a made up value used as a sentinel

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
        #endregion const definitions

        #region structs & enums
        
        /// <summary>
        /// An enum representing the different types of input devices.
        /// </summary>
        public enum DeviceType
        {
            Key,
            Mouse,
            OEM
        }

        /// <summary>
        /// Class encapsulating the information about a
        /// keyboard event, including the device it
        /// originated with and what key was pressed
        /// </summary>
        public class DeviceInfo
        {
            public string deviceName;
            public string deviceType;
            public IntPtr deviceHandle;
            public string Name;
            public string source;
            public Keys modifiedKey;
            public Keys pressedKey;
        }

        #region Windows.h structure declarations
        
        // The following structures are defined in Windows.h

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            [FieldOffset(16)]
            public RAWMOUSE mouse;
            [FieldOffset(16)]
            public RAWKEYBOARD keyboard;
            [FieldOffset(16)]
            public RAWHID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTHEADER
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSize;
            public IntPtr hDevice;
            [MarshalAs(UnmanagedType.U4)]
            public int wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWHID
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwSizHid;
            [MarshalAs(UnmanagedType.U4)]
            public int dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BUTTONSSTR
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort usButtonFlags;
            [MarshalAs(UnmanagedType.U2)]
            public ushort usButtonData;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct RAWMOUSE
        {
            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(0)] 
            public ushort usFlags;
            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(4)] 
            public uint ulButtons; 
            [FieldOffset(4)] 
            public BUTTONSSTR buttonsStr;
            [MarshalAs(UnmanagedType.U4)][FieldOffset(8)] 
            public uint ulRawButtons;
            [FieldOffset(12)]
            public int lLastX;
            [FieldOffset(16)]
            public int lLastY;
            [MarshalAs(UnmanagedType.U4)][FieldOffset(20)]
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWKEYBOARD
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort MakeCode;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Flags;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Reserved;
            [MarshalAs(UnmanagedType.U2)]
            public ushort VKey;
            [MarshalAs(UnmanagedType.U4)]
            public uint Message;
            [MarshalAs(UnmanagedType.U4)]
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICE
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsagePage;
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsage;
            [MarshalAs(UnmanagedType.U4)]
            public int dwFlags;
            public IntPtr hwndTarget;
        }
        #endregion Windows.h structure declarations


        #endregion structs & enums
        
        #region DllImports
        
        [DllImport("User32.dll")]
        extern static uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);
        
        [DllImport("User32.dll")]
        extern static uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
        
        [DllImport("User32.dll")]
        extern static bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        extern static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        /// <summary>
        /// Get the current state of the specified key
        /// </summary>
        /// <param name="vKey">The key to get</param>
        /// <returns></returns>
        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion DllImports

        #region Variables and event handling
        
        /// <summary>
        /// List of keyboard devices. Key: the device handle
        /// Value: the device info class
        /// </summary>
        private Hashtable deviceList = new Hashtable();

        /// <summary>
        /// The collections of keys to watch for
        /// </summary>
        public List<Keys> HookedKeys = new List<Keys>();

        /// <summary>
        /// If true, hook all the keys
        /// </summary>
        public bool HookAll = false;

        /// <summary>
        /// The delegate to handle KeyPressed events.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">A set of KeyControlEventArgs information about the key that was pressed and the device it was on.</param>
        public delegate void DeviceEventHandler(object sender, KeyControlEventArgs e);

        /// <summary>
        /// The event raised when InputDevice detects that a key was pressed.
        /// </summary>
        public event DeviceEventHandler KeyDown;

        /// <summary>
        /// The event raised when InputDevice detects that a key is released.
        /// </summary>
        public event DeviceEventHandler KeyUp;

        /// <summary>
        /// Arguments provided by the handler for the KeyPressed
        /// event.
        /// </summary>
        public class KeyControlEventArgs : EventArgs
        {
            private DeviceInfo  m_deviceInfo;
            private DeviceType  m_device;
           
            public KeyControlEventArgs( DeviceInfo dInfo, DeviceType device )
            {
                m_deviceInfo = dInfo;
                m_device = device;
            }
            
            public KeyControlEventArgs()
            {
            }

            public DeviceInfo Keyboard
            {
                get { return m_deviceInfo; }
                set { m_deviceInfo = value; }
            }

            public DeviceType Device
            {
                get { return m_device; }
                set { m_device = value; }
            }
        }

        #endregion Variables and event handling

        #region InputDevice( IntPtr hwnd )

        /// <summary>
        /// InputDevice constructor; registers the raw input devices
        /// for the calling window. Keys to hook are defined in HookedKeys, unless
        /// HookAll is set to true.
        /// </summary>
        /// <param name="hwnd">Handle of the window listening for key presses</param>
        public InputDevice( IntPtr hwnd )
        {
            //Create an array of all the raw input devices we want to 
            //listen to. In this case, only keyboard devices.
            //RIDEV_INPUTSINK determines that the window will continue
            //to receive messages even when it doesn't have the focus.
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
			
            rid[0].usUsagePage  = 0x01;
            rid[0].usUsage      = 0x06;
            rid[0].dwFlags      = RIDEV_INPUTSINK; 
            rid[0].hwndTarget   = hwnd;
           
            if( !RegisterRawInputDevices( rid, (uint)rid.Length, (uint)Marshal.SizeOf( rid[0] )))
            {
                throw new ApplicationException( "Failed to register raw input device(s)." );
            }
        }

        #endregion InputDevice( IntPtr hwnd )

        #region ReadReg( string item, ref bool isKeyboard )
        
        /// <summary>
        /// Reads the Registry to retrieve a friendly description
        /// of the device, and determine whether it is a keyboard.
        /// </summary>
        /// <param name="item">The device name to search for, as provided by GetRawInputDeviceInfo.</param>
        /// <param name="isKeyboard">Determines whether the device's class is "Keyboard".</param>
        /// <returns>The device description stored in the Registry entry's DeviceDesc value.</returns>
        private string ReadReg( string item, ref bool isKeyboard )
        {
            // Example Device Identification string
            // @"\??\ACPI#PNP0303#3&13c0b0c5&0#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}";

            // remove the \??\
            item = item.Substring( 4 ); 

            string[] split = item.Split( '#' );

            string id_01 = split[0];    // ACPI (Class code)
            string id_02 = split[1];    // PNP0303 (SubClass code)
            string id_03 = split[2];    // 3&13c0b0c5&0 (Protocol code)
            //The final part is the class GUID and is not needed here

            //Open the appropriate key as read-only so no permissions
            //are needed.
            RegistryKey OurKey = Registry.LocalMachine;

            string findme = string.Format( @"System\CurrentControlSet\Enum\{0}\{1}\{2}", id_01, id_02, id_03 );
            
            OurKey = OurKey.OpenSubKey( findme, false );

            //Retrieve the desired information and set isKeyboard
            string deviceDesc  = (string)OurKey.GetValue( "DeviceDesc" );
            string deviceClass = (string)OurKey.GetValue( "Class" );
            
            if( deviceClass.ToUpper().Equals( "KEYBOARD" ))
            {
                isKeyboard = true;
            }
            else
            {
                isKeyboard = false;
            }
            return deviceDesc;
        }

        #endregion ReadReg( string item, ref bool isKeyboard )

        #region int EnumerateDevices()

        /// <summary>
        /// Iterates through the list provided by GetRawInputDeviceList,
        /// counting keyboard devices and adding them to deviceList.
        /// </summary>
        /// <returns>The number of keyboard devices found.</returns>
        public int EnumerateDevices()
        {
            
            int NumberOfDevices = 0;
            uint deviceCount = 0;
            int dwSize = ( Marshal.SizeOf( typeof( RAWINPUTDEVICELIST )));

            // Get the number of raw input devices in the list,
            // then allocate sufficient memory and get the entire list
            if( GetRawInputDeviceList( IntPtr.Zero, ref deviceCount, (uint)dwSize ) == 0 )
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                // Iterate through the list, discarding undesired items
                // and retrieving further information on keyboard devices
                for (int i = 0; i < deviceCount; i++)
                {
                    DeviceInfo dInfo;
                    string deviceName;
                    uint pcbSize = 0;

                    RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                                               new IntPtr((pRawInputDeviceList.ToInt32() + (dwSize * i))),
                                               typeof(RAWINPUTDEVICELIST));

                    GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

                    if (pcbSize > 0)
                    {
                        IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                        GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICENAME, pData, ref pcbSize);
                        deviceName = (string)Marshal.PtrToStringAnsi(pData);

                        // Drop the "root" keyboard and mouse devices used for Terminal 
                        // Services and the Remote Desktop
                        if (deviceName.ToUpper().Contains("ROOT"))
                        {
                            continue;
                        }

                        // If the device is identified in the list as a keyboard or 
                        // HID device, create a DeviceInfo object to store information 
                        // about it
                        if (rid.dwType == RIM_TYPEKEYBOARD || rid.dwType == RIM_TYPEHID)
                        {
                            dInfo = new DeviceInfo();

                            dInfo.deviceName = (string)Marshal.PtrToStringAnsi(pData);
                            dInfo.deviceHandle = rid.hDevice;
                            dInfo.deviceType = GetDeviceType(rid.dwType);

                            // Check the Registry to see whether this is actually a 
                            // keyboard, and to retrieve a more friendly description.
                            bool IsKeyboardDevice = false;
                            string DeviceDesc = ReadReg(deviceName, ref IsKeyboardDevice);
                            dInfo.Name = DeviceDesc;

                            // If it is a keyboard and it isn't already in the list,
                            // add it to the deviceList hashtable and increase the
                            // NumberOfDevices count
                            if (!deviceList.Contains(rid.hDevice) && IsKeyboardDevice)
                            {
                                NumberOfDevices++;
                                deviceList.Add(rid.hDevice, dInfo);
                            }
                        }
                        Marshal.FreeHGlobal(pData);
                    }
                }


                Marshal.FreeHGlobal(pRawInputDeviceList);

                return NumberOfDevices;

            }
            else
            {
                throw new ApplicationException( "An error occurred while retrieving the list of devices." );
            }

        }

        #endregion EnumerateDevices()
        
        #region ProcessInputCommand( Message message )
        
        /// <summary>
        /// Processes WM_INPUT messages to retrieve information about any
        /// keyboard events that occur.
        /// </summary>
        /// <param name="message">The WM_INPUT message to process.</param>
        public void ProcessInputCommand( Message message )
        {
            uint dwSize = 0;

            // First call to GetRawInputData sets the value of dwSize,
            // which can then be used to allocate the appropriate amount of memory,
            // storing the pointer in "buffer".
            GetRawInputData( message.LParam, 
                             RID_INPUT, IntPtr.Zero, 
                             ref dwSize, 
                             (uint)Marshal.SizeOf( typeof( RAWINPUTHEADER )));

            IntPtr buffer = Marshal.AllocHGlobal( (int)dwSize );
            try
            {
                // Check that buffer points to something, and if so,
                // call GetRawInputData again to fill the allocated memory
                // with information about the input
                if (buffer != IntPtr.Zero &&
                    GetRawInputData(message.LParam,
                                     RID_INPUT,
                                     buffer,
                                     ref dwSize,
                                     (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    // Store the message information in "raw", then check
                    // that the input comes from a keyboard device before
                    // processing it to raise an appropriate KeyPressed event.

                    RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));

                    if (raw.header.dwType == RIM_TYPEKEYBOARD)
                    {
                        // Filter for Key Down or Up events and then retrieve information 
                        // about the keystroke
                        bool keyIsDown = false;
                        keyIsDown = (raw.keyboard.Message == WM_KEYDOWN || raw.keyboard.Message == WM_SYSKEYDOWN);
                        if (keyIsDown || raw.keyboard.Message == WM_KEYUP || raw.keyboard.Message == WM_SYSKEYUP)
                        {
                            ushort key = raw.keyboard.VKey;

                            // On most keyboards, "extended" keys such as the arrow or 
                            // page keys return two codes - the key's own code, and an
                            // "extended key" flag, which translates to 255. This flag
                            // isn't useful to us, so it can be disregarded.
                            if (key > VK_LAST_KEY)
                                return;

                            // Check if the key is one we are looking for 
                            // (unless we want them all)
                            if (!HookedKeys.Contains((Keys)key) && !HookAll)
                                return;

                            // Retrieve information about the device and the
                            // key that was pressed.
                            DeviceInfo dInfo = null;

                            if (deviceList.Contains(raw.header.hDevice))
                            {
                                Keys myKey = (Keys)key;

                                dInfo = (DeviceInfo)deviceList[raw.header.hDevice];

                                //myKey = (Keys)Enum.Parse(typeof(Keys), Enum.GetName(typeof(Keys), key)); // Not sure why author had it this way

                                // Set the data
                                dInfo.pressedKey = myKey;
                                dInfo.modifiedKey = Modifiers(myKey);
                            }
                            else
                            {
                                string errMessage = String.Format("Handle :{0} was not in hashtable. The device may support more than one handle or usage page, and is probably not a standard keyboard.", raw.header.hDevice);
                                throw new ApplicationException(errMessage);
                            }

                            // If the key that was pressed is valid and there
                            // was no problem retrieving information on the device,
                            // raise the Key pressed event.
                            if (keyIsDown && KeyDown != null && dInfo != null)
                            {
                                KeyDown(this, new KeyControlEventArgs(dInfo, GetDevice(message.LParam.ToInt32())));
                            }
                            else if (!keyIsDown && KeyUp != null && dInfo != null)
                            {
                                KeyUp(this, new KeyControlEventArgs(dInfo, GetDevice(message.LParam.ToInt32())));
                            }
                            else if (dInfo == null)
                            {
                                string errMessage = String.Format("Received Unknown Key: {0}. Possibly an unknown device", key);
                                throw new ApplicationException(errMessage);
                            }
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal( buffer );
            }
        }

        /// <summary>
        /// Checks whether Alt, Shift, Control or CapsLock
        /// is pressed at the same time as the hooked key.
        /// Returns the other key that is being pressed.
        /// </summary>
        /// <param name="key">The key that was pressed</param>
        private Keys Modifiers(Keys key)
        {
            //CapsLock
            if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) key = Keys.CapsLock;
            //Shift
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) key = Keys.Shift;
            //Ctrl
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) key = Keys.Control;
            //Alt
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) key = Keys.Alt;

            return key;
        }
        #endregion ProcessInputCommand( Message message )

        #region DeviceType GetDevice( int param )

        /// <summary>
        /// Determines what type of device triggered a WM_INPUT message.
        /// (Used in the ProcessInputCommand method).
        /// </summary>
        /// <param name="param">The LParam from a WM_INPUT message.</param>
        /// <returns>A DeviceType enum value.</returns>
        private DeviceType GetDevice(int param)
        {
            DeviceType deviceType;

            switch( (int)(((ushort)(param >> 16)) & FAPPCOMMAND_MASK ))
            {
                case FAPPCOMMAND_OEM:
                    deviceType = DeviceType.OEM;
                    break;
                case FAPPCOMMAND_MOUSE:
                    deviceType = DeviceType.Mouse;
                    break;
                default:
                    deviceType = DeviceType.Key;
                    break;
            }

            return deviceType;
        }

        #endregion DeviceType GetDevice( int param )

        #region ProcessMessage( Message message )

        /// <summary>
        /// Filters Windows messages for WM_INPUT messages and calls
        /// ProcessInputCommand if necessary.
        /// </summary>
        /// <param name="message">The Windows message.</param>
        public void ProcessMessage( Message message )
		{
			switch( message.Msg )
			{
				case WM_INPUT:
		        {
		            ProcessInputCommand( message );
		        }
				break;
			}
        }

        #endregion ProcessMessage( Message message )

        #region GetDeviceType( int device )
        
        /// <summary>
        /// Converts a RAWINPUTDEVICELIST dwType value to a string
        /// describing the device type.
        /// </summary>
        /// <param name="device">A dwType value (RIM_TYPEMOUSE, 
        /// RIM_TYPEKEYBOARD or RIM_TYPEHID).</param>
        /// <returns>A string representation of the input value.</returns>
        private string GetDeviceType( int device )
        {
            string deviceType;
            switch( device ) 
            {
                case RIM_TYPEMOUSE: deviceType    = "MOUSE";    break;
                case RIM_TYPEKEYBOARD: deviceType = "KEYBOARD"; break;
                case RIM_TYPEHID: deviceType      = "HID";      break;
                default: deviceType               = "UNKNOWN";  break;
            }
            return deviceType;
        }

        #endregion GetDeviceType( int device )

    }
}
