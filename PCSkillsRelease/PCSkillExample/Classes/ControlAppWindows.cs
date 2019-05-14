// Copyright 2019 Intel Corporation.
//
// The source code, information and material ("Material") contained herein is owned by
// Intel Corporation or its suppliers or licensors, and title to such Material remains
// with Intel Corporation or its suppliers or licensors. The Material contains
// proprietary information of Intel or its suppliers and licensors. The Material is
// protected by worldwide copyright laws and treaty provisions. No part of the
// Material may be used, copied, reproduced, modified, published, uploaded, posted,
// transmitted, distributed or disclosed in any way without Intel's prior express
// written permission. No license under any patent, copyright or other intellectual
// property rights in the Material is granted to or conferred upon you, either
// expressly, by implication, inducement, estoppel or otherwise. Any license under
// such intellectual property rights must be express and approved by Intel in writing.
//
// Unless otherwise agreed by Intel in writing, you may not remove or alter this
// notice or any other notice embedded in Materials by Intel or Intel's suppliers or
// licensors in any way.

#define EXCLUDE_SUSPENDED_WINDOWS      // get suspended app PIDs so that they can be excluded from windows list
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// contains hWnd of applications
using hWndList = System.Collections.Generic.List<System.IntPtr>;
// contains PID of applications that are suspended
using pidList = System.Collections.Generic.List<int>;
// contains hWnd & window title
using windowsInfo = System.Collections.Generic.Dictionary<System.IntPtr, string>;
// contains hwnd, title and window placement info
using winStateInfo = System.Collections.Generic.Dictionary<System.IntPtr, PCSkillExample.Placement>;

namespace PCSkillExample
{
    internal class Placement
    {
        internal string Title;
        internal NativeMethods.WINDOWPLACEMENT WinPlacement;

        private string WinPlacementToString()
        {
            string str = "";

            str = "Length: " + WinPlacement.length;
            str += " MaxP.X: " + WinPlacement.ptMaxPosition.X;
            str += " MaxP.Y: " + WinPlacement.ptMaxPosition.Y;
            str += " MinP.X: " + WinPlacement.ptMinPosition.X;
            str += " MinP.Y: " + WinPlacement.ptMinPosition.Y;
            str += " NorP.X: " + WinPlacement.rcNormalPosition.X;
            str += " NorP.Y: " + WinPlacement.rcNormalPosition.Y;
            str += " NorP.W: " + WinPlacement.rcNormalPosition.Width;
            str += " NorP.H: " + WinPlacement.rcNormalPosition.Height;
            str += " shoCmd: " + WinPlacement.showCmd;

            return str;
        }
        override public string ToString()
        {
            string str = "";
            str += "\tTitle: " + Title;
            str += "\n\tPlacement: " + WinPlacementToString();
            return str;
        }
    }

    // contains all the interop functions from user32.dll
    internal class NativeMethods
    {
        #region ImportDefs
        // --------------------------------------------------------------------
        // Use DllImport to import these Win32 functions

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        // to minimize/maximize windows
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr hWnd);

        internal struct RECT
        {
            public int x;
            public int y;
            public int width;
            public int height;

        }

        [DllImport("User32.dll")]
        internal static extern Int32 SetForegroundWindow(int hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        internal static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        internal static extern bool UpdateWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindow", SetLastError = true)]
        internal static extern IntPtr GetNextWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.U4)] int wFlag);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        internal const int HWND_TOP = 0;
        internal const int SWP_SHOWWINDOW = 0x40;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_NOSIZE = 0x0001;
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        internal const int GW_HWNDNEXT = 2; // The next window is below the specified window
        internal const int GW_HWNDPREV = 3; // The previous window is above

        internal const int SW_HIDE = 0;
        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_NORMAL = 1;
        internal const int SW_SHOWMINIMIZED = 2;
        internal const int SW_SHOWMAXIMIZED = 3;
        internal const int SW_MAXIMIZE = 3;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const int SW_SHOW = 5;
        internal const int SW_MINIMIZE = 6;
        internal const int SW_SHOWMINNOACTIVE = 7;
        internal const int SW_SHOWNA = 8;
        internal const int SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        #endregion ImportDefs
    }

    // ------------------------------------------------------------------------
    // contains methods to manipulate app windows min/max/position etc
    class ControlAppWindows
    {
        ConfusionMatrix cMatrix;
        public ControlAppWindows()
        {
            cMatrix = new ConfusionMatrix();
            cMatrix.MakeList("ConfusionText.txt");
        }

        // --------------------------------------------------------------------
        // data related to class
        // to save window handles & names
        //private static windowsInfo windowInfo;
        private const int MaxWindowsCount = 4;
        private winStateInfo StateInfo;              // contains the saved state of the desktop windows
        private IntPtr TopWindowhwnd = IntPtr.Zero;  // to restore top most window
        private const string DebugLogFileName = "logData.txt";  // dump log information
        private const int DefaultWaitTime = 200;            // wait time before retrying window placement operation

//        private void LogData(string txt)
//        {
//#if DEBUG
//            string str = DateTime.Now.ToString();
//            str = str + ": " + txt /*+ "\n"*/;
//            // do file logging only in debug mode
//            string logFile = DebugLogFileName;
//            if (!System.IO.File.Exists(logFile))
//            {
//                using (System.IO.StreamWriter sw = System.IO.File.CreateText(logFile))
//                {
//                    sw.WriteLine(str);
//                }
//            }
//            else
//            {
//                using (System.IO.StreamWriter sw = System.IO.File.AppendText(logFile))
//                {
//                    sw.WriteLine(str);
//                }
//            }
//            //            Debug.Write(str);
//#endif
//        }

        string ShowStateInfo(winStateInfo stateInfo)
        {
            string str = "";

            foreach (var state in stateInfo)
            {
                str += "\nIntPtr: " + state.Key.ToString("x");
                str += " Value: " + state.Value.ToString();
            }

            return str;
        }
        // save current state of desktop windows in a global variable
        public windowsInfo SaveCurrentState()
        {
            TopWindowhwnd = NativeMethods.GetTopWindow(IntPtr.Zero);
            windowsInfo windowInfo = GetWindowsList();       // get windows info in windowInfo
            Task task = Task.Run(() =>
            {
                StateInfo = SaveState(windowInfo);
            });
            return windowInfo;
        }
        public bool RestoreSavedState()
        {
            bool result = true;
            if (StateInfo == null || StateInfo.Count == 0)
            {
                return false;
            }

            Task task = Task.Run(() =>
            {
                RestoreState(StateInfo);
                if (TopWindowhwnd != IntPtr.Zero)
                {
                    Thread.Sleep(DefaultWaitTime);
                    SetForegroundWindow(TopWindowhwnd);
                    TopWindowhwnd = IntPtr.Zero;    // clear it so that we dont accidently use it next time
                    //nativeMethods.SetForegroundWindow(topWindowhwnd.ToInt32());
                    //nativeMethods.ShowWindow(topWindowhwnd, nativeMethods.SW_SHOW);
                }
            });
            return result;
        }


        // Some apps like calculator, system settings, windows store etc are present in the list of windows
        // but are in suspended state and do not appear on screen. A list of these suspended apps is 
        // needed so that these apps can be excluded from the list of windows found on screen by getWindowsList
        // function and thus not get operated upon.
        //
        // read all the processes, get their thread info and from there check if any of the apps are
        // in suspended state. Collect info related to those suspended apps and return that in a list
        // Mainly we need PID of the suspended apps so that we do not operate on those app windows while
        // performing the placement of the app windows.
        public pidList GetSuspendedAppPIDList()
        {
            pidList suspendedAppsPidList = new pidList();
            Process[] processes = Process.GetProcesses();
            int totalThreadChecked = 0, totalApps = 0, totalSuspended = 0;    // total threads and apps looked into
            foreach (var p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    totalApps++;
                    int foundSuspended = 0;
                    int threadsChecked = 0;
                    foreach (ProcessThread t in p.Threads)
                    {
                        if (t.ThreadState == System.Diagnostics.ThreadState.Wait &&
                                t.WaitReason == ThreadWaitReason.Suspended)
                        {
                            foundSuspended++;
                            totalSuspended++;

                        }
                        threadsChecked++;

                    }
                    totalThreadChecked += threadsChecked; // p.Threads.Count;
                    // if atleast half the threads are suspended, consider it a suspended app
                    // for more precise control, we can check if all the threads are suspended
                    if (foundSuspended > 0 && foundSuspended >= (p.Threads.Count / 2))
                    {
                        suspendedAppsPidList.Add(p.Id);
                    }
                }
            }
            return suspendedAppsPidList;
        }

        // get PID of the app from the window handle hWnd
        uint GetProcessIDFromHWnd(IntPtr hwnd)
        {
            uint threadID = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processID);
            return processID;
        }

        // save current state of windows in a list (hwnd, title, placement info)
        private winStateInfo SaveState(windowsInfo winInfo)
        {
            winStateInfo stateInfo = new winStateInfo();
            if (winInfo.Count == 0)
            {
                return stateInfo;       // return empty stateInfo if input list is empty
            }

            // step through windowsInfo list
            foreach (KeyValuePair<IntPtr, string> item in winInfo)
            {
                Placement p = new Placement();      // info to be save(title & placement)
                IntPtr hwnd = item.Key;             // window handle

                p.WinPlacement.length = Marshal.SizeOf(p.WinPlacement);
                NativeMethods.GetWindowPlacement(hwnd, ref p.WinPlacement);   // get information from windows
                p.Title = GetWindowTitle(hwnd);

                stateInfo.Add(hwnd, p);     // save this window's state

            }

            return stateInfo;
        }

        // restore the windows placement as specified in input
        private bool RestoreState(winStateInfo stateInfo)
        {
            if (stateInfo == null || stateInfo.Count == 0)
            {
                return false;     // do nothing if input is empty
            }

            // restore the window sequence (topmost, second topmost .....)
            IntPtr[] hwndArray = stateInfo.Keys.ToArray<IntPtr>();
            int count = hwndArray.Count<IntPtr>();
            for (int i = 0; i < count - 1; i++)
            {
                IntPtr hwnd = hwndArray[i];     // current window handle
                IntPtr hwndInsertAfter = hwndArray[i + 1];  // window below current window
                NativeMethods.SetWindowPos(hwnd, hwndInsertAfter.ToInt32(), 0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
            }

            // restore windows position along with minimize/maximize etc status
            foreach (KeyValuePair<IntPtr, Placement> item in stateInfo.Reverse())
            {
                IntPtr hwnd = item.Key;     // get window handle
                // check if the window handle is still valid
                if (NativeMethods.IsWindow(hwnd) != false)
                {
                    Placement p = item.Value;
                    NativeMethods.SetWindowPlacement(hwnd, ref p.WinPlacement);       // restore window placement info
                }
            }
            stateInfo.Clear();      // no more saved states available
            return true;
        }


        // get screen info in a RECT struct (exclude taskbar area)
        private NativeMethods.RECT GetRect()
        {
            NativeMethods.RECT rect = new NativeMethods.RECT();
            rect.y = 0;
            rect.x = 0;
            rect.width = Screen.PrimaryScreen.WorkingArea.Width;
            rect.height = Screen.PrimaryScreen.WorkingArea.Height;
            return rect;
        }


        // if windows is minimized, show it and bring it to foreground
        void SetForegroundWindow(IntPtr hwnd)
        {
            if (NativeMethods.IsIconic(hwnd))
            {
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNORMAL);
            }
            NativeMethods.SetForegroundWindow(hwnd.ToInt32());
        }


        // --------------------------------------------------------------------
        // maximize all windows matching appName
        public int MaximizeWindow(string appName)
        {
            windowsInfo windowInfo = SaveCurrentState();         // save desktop window placement state
            string name = cMatrix.GetCorrectName(appName);
            hWndList hwndList = FindWindowsForApp(name, windowInfo);
            if (hwndList.Count == 0)
            {
                return 0;   // no window found
            }
            int count = 1;      // by default maximize only one window
            if (string.Equals(appName, "All", StringComparison.OrdinalIgnoreCase))
            {
                // if "all", maximize all windows & not just top 4
                //count = hwndList.Count > maxWindowsCount ? maxWindowsCount : hwndList.Count;
                count = hwndList.Count;
            }

            int returnValue = 0;
            int localCount = count;
            Task task = Task.Run(() =>
            {
                foreach (var hwnd in hwndList)
                {
                    string t1 = GetWindowTitle(hwnd);       // debug
                    if (NativeMethods.IsIconic(hwnd))
                    {
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNORMAL);
                    }
                    returnValue++;  // count number of windows operated upon
                    NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWMAXIMIZED);
                    NativeMethods.SetForegroundWindow(hwnd.ToInt32());
                    localCount--;
                    if (localCount == 0)
                    {
                        break;
                    }
                }
            });
            return count;    // number of windows maximised
        }


        // minimize all windows matching appName
        public int MinimizeWindow(string appName)
        {
            windowsInfo windowInfo = SaveCurrentState();         // save desktop window placement state
            string name = cMatrix.GetCorrectName(appName);
            hWndList hwndList = FindWindowsForApp(name, windowInfo);
            if (hwndList.Count == 0)
            {
                return 0;   // no window found
            }
            int count = 0;
            count = 1;
            if (string.Equals(appName, "All", StringComparison.OrdinalIgnoreCase))
            {
                // if "all", minimize all windows & not just top 4
                //count = hwndList.Count > maxWindowsCount ? maxWindowsCount : hwndList.Count;
                count = hwndList.Count;
            }

            Task task = Task.Run(() =>
            {
                int localCount = count;
                int returnValue = 0;
                foreach (var hwnd in hwndList)
                {
                    string t1 = GetWindowTitle(hwnd);
                    if (NativeMethods.IsIconic(hwnd))
                    {
                        continue;
                    }
                    returnValue++;  // count number of windows minimised
                    NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWMINIMIZED);
                    localCount--;
                    if (localCount == 0)
                    {
                        break;
                    }
                }
            });
            return count;    // number of windows minimised
        }


        // --------------------------------------------------------------------
        string GetWindowTitle(IntPtr hwnd)
        {
            StringBuilder sbTitle = new StringBuilder(1024);
            int length = NativeMethods.GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
            string title = sbTitle.ToString();
            return title;

        }
        // get list of hwnd & window titles based on the window's Z-order
        private windowsInfo GetWindowsList()
        {
            windowsInfo windowInfo = new windowsInfo();
#if EXCLUDE_SUSPENDED_WINDOWS
            pidList suspendedAppsPidList = GetSuspendedAppPIDList();    // get list of suspended app's PIDs
#endif
            // start from top most window
            IntPtr hwnd = NativeMethods.GetTopWindow(IntPtr.Zero);
            if (hwnd != IntPtr.Zero)
            {
                do
                {
                    if (NativeMethods.IsWindowVisible(hwnd))
                    {
                        // Get the window's title.
                        string title = GetWindowTitle(hwnd);

                        if (!string.IsNullOrEmpty(title))
                        {
                            // exclude program manager and current application windows from the list
                            if (string.Equals(title, "Program Manager") == false && string.Equals(title, "ManageDesktopWindowsPosition") == false)
                            {
                                // Process ID of the windows is need so that we can check if this process is 
                                // currently active or is in suspended state coz we do not want to include
                                // suspended app windows into our list of windows
                                uint currentPid = GetProcessIDFromHWnd(hwnd); // get process ID of the window
                                bool excludeApp = false;            // flag to exclude suspended apps from list

                                // check process ID of the application in the suspended Apps PID List and exclude the 
                                // processes that are in suspended state
#if EXCLUDE_SUSPENDED_WINDOWS
                                foreach (int pid in suspendedAppsPidList)
                                {
                                    if (pid == (int)currentPid)
                                    {
                                        excludeApp = true;  // this app is suspended, don't include it
                                        break;
                                    }
                                }
#endif
                                if (excludeApp == false)
                                {
                                    windowInfo.Add(hwnd, title);
                                }
                            }
                        }
                    }
                    // Get next window under the current window
                    hwnd = NativeMethods.GetNextWindow(hwnd, NativeMethods.GW_HWNDNEXT);
                } while (hwnd != IntPtr.Zero);
            }

            return windowInfo;

        }

        // find first "count" instances of the app in windowsInfo list
        private hWndList FindFirstApp(windowsInfo winInfo, string appName, int count = 1)
        {
            IntPtr hwnd = IntPtr.Zero;
            hWndList hwndList = new List<IntPtr>();
            foreach (KeyValuePair<IntPtr, string> item in winInfo)
            {
                hwnd = item.Key;
                string name = item.Value.ToLower();     // get window name
                // check if current name matches the desired name
                if (name.Contains(appName.ToLower()) == true)
                {
                    // first instance of app found
                    hwndList.Add(hwnd);
                    count--;
                    if (count < 1)
                    {
                        break;  // added count number of items, stop iterating
                    }
                }
            }
            return hwndList;    // return list of window handles
        }

        // add max of 2 windows per app
        private const int MaxAppCount = 2;
        // find app instances for multiple instances in windowsInfo list
        private hWndList FindWindowsForAppMultiple(string[] appNames, windowsInfo windowInfo)
        {
            hWndList hwndList = new List<IntPtr>();

            foreach (var app in appNames)
            {
                if (!string.IsNullOrEmpty(app))
                {
                    hWndList hwndListApp = FindFirstApp(windowInfo, app, MaxAppCount);
                    if (hwndListApp.Count != 0)
                    {
                        hwndList.AddRange(hwndListApp); // add all instances to main list
                    }
                }
            }

            return hwndList;
        }
        // find the app or file name in the window name titles list, add corresponding window handle to a list
        // and return that list. If appname is "all" or "", return all windows
        private hWndList FindWindowsForApp(string appName, windowsInfo windowInfo)
        {
            hWndList hwndList = new List<IntPtr>();

            // get window handles and titles
            if (string.Equals(appName, "All", StringComparison.OrdinalIgnoreCase))
            {
                foreach (KeyValuePair<IntPtr, string> item in windowInfo)
                {
                    IntPtr hwnd = item.Key;
                    // add the window handle to list
                    hwndList.Add(hwnd);
                }
            }
            else
            {
                foreach (KeyValuePair<IntPtr, string> item in windowInfo)
                {
                    IntPtr hwnd = item.Key;     // window handle
                    string name = item.Value.ToLower();     // get window name
                    // check if current name matches the desired name
                    if (name.Contains(appName.ToLower()) == true)
                    {
                        // add the window handle to list
                        hwndList.Add(hwnd);
                    }
                }
            }

            return hwndList;
        }


        // ---------------------------------------------------------------------------------------
        // check if window is minimized/maximized or in normal mode
        private int GetWindowPlacement(IntPtr hwnd)
        {
            NativeMethods.WINDOWPLACEMENT placement = new NativeMethods.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            NativeMethods.GetWindowPlacement(hwnd, ref placement);
            return placement.showCmd;
        }

        // wrapp set windows position function
        private void SetWindowsPosition(IntPtr hwnd, int x, int y, int width, int height)
        {
            // if window is maximized, restore it to normal size
            int windowSize = GetWindowPlacement(hwnd);
            if (windowSize == NativeMethods.SW_MAXIMIZE)
            {
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNORMAL);
            }

            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOP, x, y, width, height, NativeMethods.SWP_SHOWWINDOW);
            NativeMethods.UpdateWindow(hwnd);
        }

        // This one contains some hard coded values for the number of windows to be displayed in rows
        // and columns based on the total number of windows. The best appearance values are chosen.
        // Expected max value is 12. For a bigger number, the results may not be very pretty to look at.
        private void GetDivisorsQuadrants(int count, out int widthDiv, out int heightDiv)
        {
            widthDiv = 1;       // default 1 windows horizontally
            heightDiv = 1;      // default 1 windows vertically
            if (count > 1 && count <= 2)
            {
                // tile side by side
                widthDiv = 2;   // 2 windows on x axis
                heightDiv = 1;  // 1 windows on y axis
            }
            if (count > 2 && count <= 4)
            {
                widthDiv = 2;   // 2 windows on x axis
                heightDiv = 2;  // 2 windows on y axis
            }
            else if (count > 4 && count <= 6)
            {
                widthDiv = 3;   // 3 windows horizontally
                heightDiv = 2;  // 2 windows vertically
            }
            else if (count > 6 && count <= 9)
            {
                widthDiv = 3;   // 3 windows horizontally
                heightDiv = 3;  // 3 windows vertically
            }
            else if (count > 9 && count <= 12)
            {
                widthDiv = 4;   // 4 windows horizontally
                heightDiv = 3;  // 3 windows vertically
            }
            else if (count > 12)
            {
                // shouldn't happen as windows number is limited to 12
                heightDiv = 4;                          // 4 windows vertically
                widthDiv = (count / heightDiv) + 1;     // calculate horizontal window count
            }

        }

        // compute width and height divisors for side by side windows placement
        private void GetDivisorsSideBySide(int count, out int widthDiv, out int heightDiv)
        {
            widthDiv = count;       // windows will be shown in columns
            heightDiv = 1;          // windows will span from top to bottom
        }
        // compute width and height divisors for top and bottom windows placement
        private void getDivisorsTopAndBottom(int count, out int widthDiv, out int heightDiv)
        {
            widthDiv = 1;           // windows will span from left to right
            heightDiv = count;      // windows will be shown in rows
        }


        // calculate the position of the co-ordinates where windows will be placed based on number of
        // rows (widthDiv) & number of columns (heightDiv).
        // return the list of RECT array, each containing position (x, y, width, height) values
        private NativeMethods.RECT[] GetPosRects(int count, int widthDiv, int heightDiv)
        {
            NativeMethods.RECT winSize = GetRect();      // screen info
            NativeMethods.RECT[] rect = new NativeMethods.RECT[widthDiv * heightDiv];

            // compute width/height of individual window
            int winWidth = winSize.width / widthDiv;
            int winHeight = winSize.height / heightDiv;
            int index = 0;
            // setup position info for the windows in rows (w) & columns (h) fashion
            for (int h = 0; h < heightDiv; h++)
            {
                for (int w = 0; w < widthDiv; w++)
                {
                    rect[index].x = w * winWidth;
                    rect[index].y = h * winHeight;
                    rect[index].width = winWidth;
                    rect[index].height = winHeight;
                    index++;
                }
            }
            return rect;
        }

        // actually arrange windows in horizontally, vertically or in quadrants
        public enum Arrange
        {
            Quadrants = 1,
            SideBySide =2,
            TopAndBottom =3
        }
        //private const int ARRANGE_IN_QUADRANTS = 1;
        //private const int ARRANGE_SIDE_BY_SIDE = 2;
        //private const int ARRANGE_TOP_AND_BOTTOM = 3;

        private int ArrangeWindows(hWndList hwndList, int placement, int count)
        {
            if (count == 0)
            {
                return 0;       // oops, no windows to arrange
            }

            NativeMethods.RECT[] rect = null;
            int widthDiv = 1;
            int heightDiv = 1;
            // get divisor values for rows & columns of windows
            if (placement == (int) Arrange.Quadrants)
            {
                // get horiozontal and vertical divisors for quadrants
                GetDivisorsQuadrants(count, out widthDiv, out heightDiv);
            }
            else if (placement == (int)Arrange.SideBySide)
            {
                // get horiozontal and vertical divisors for side by side placement
                GetDivisorsSideBySide(count, out widthDiv, out heightDiv);
            }
            else if (placement == (int)Arrange.TopAndBottom)
            {
                // get horiozontal and vertical divisors for horizontal placement
                getDivisorsTopAndBottom(count, out widthDiv, out heightDiv);
            }

            rect = GetPosRects(count, widthDiv, heightDiv); // get position rectangles of the windows

            // set the placement and position of the windows
            for (int i = 0; i < count; i++)
            {
                IntPtr hwnd = hwndList[i];

                SetWindowsPosition(hwnd, rect[i].x, rect[i].y, rect[i].width, rect[i].height);
                SetForegroundWindow(hwnd);
            }
            return count;       // number of windows operated upon
        }

        // place windows horizontally, vertically or in quadrants (input: single app or file name)
        private int PlaceWindows(string appName, int placement)
        {
            windowsInfo windowInfo = SaveCurrentState();         // save desktop window placement state
            hWndList hwndList = FindWindowsForApp(appName, windowInfo);

            if (hwndList.Count == 0)
            {
                return 0;   // no window found
            }
            int count = hwndList.Count;
            // limit count to 4 if input string is empty or contains "all"
            //if(string.IsNullOrEmpty(appName) || string.Equals(appName, "All", StringComparison.OrdinalIgnoreCase))
            {
                count = count > MaxWindowsCount ? MaxWindowsCount : count;
            }

            Task task = Task.Run(() =>
            {
                // repeat again after a short delay (sometimes windows fails to place windows correctly in one go)
                ArrangeWindows(hwndList, placement, count);
                Thread.Sleep(DefaultWaitTime);
                ArrangeWindows(hwndList, placement, count);
            });
            return count;
        }

        // place windows horizontally, vertically or in quadrants (input: multiple app or file names)
        private int PlaceWindows(string[] appName, int placement)
        {
            windowsInfo windowInfo = SaveCurrentState();         // save desktop window placement state
            hWndList hwndList = FindWindowsForAppMultiple(appName, windowInfo);

            if (hwndList.Count == 0)
            {
                return 0;   // no window found
            }
            int count = hwndList.Count;

            Task task = Task.Run(() =>
            {
                // repeat again after a short delay (sometimes windows fails to place windows correctly in one go)
                ArrangeWindows(hwndList, placement, count);
                Thread.Sleep(DefaultWaitTime);
                ArrangeWindows(hwndList, placement, count);
            });
            return count;
        }


        // --------------------------------------------------------------------
        // exposed functions for arrange windows operations
        public int ArrangeWindows(IRCExample.ArrangeAction action, params string[] appNames)
        {
            int result = 0;
            string[] names = cMatrix.GetCorrectName(appNames);

            if(action == IRCExample.ArrangeAction.DoQuadrants)
                result = PlaceWindows(names, (int)Arrange.Quadrants);
            else if (action == IRCExample.ArrangeAction.DoTopAndBottom)
                result = PlaceWindows(names, (int)Arrange.TopAndBottom);
            else if (action == IRCExample.ArrangeAction.DoSideBySide)
                result = PlaceWindows(names, (int)Arrange.SideBySide);

            return result;
        }

        public int ArrangeWindows(IRCExample.ArrangeAction action, string appName)
        {
            int result = 0;
            string name = cMatrix.GetCorrectName(appName);

            if (action == IRCExample.ArrangeAction.DoQuadrants)
                result = PlaceWindows(name, (int)Arrange.Quadrants);
            else if (action == IRCExample.ArrangeAction.DoTopAndBottom)
                result = PlaceWindows(name, (int)Arrange.TopAndBottom);
            else if (action == IRCExample.ArrangeAction.DoSideBySide)
                result = PlaceWindows(name, (int)Arrange.SideBySide);

            return result;
        }

        public int TileWindows(string appName)
        {
            
            windowsInfo windowInfo = SaveCurrentState();         // save desktop window placement state
            string name = cMatrix.GetCorrectName(appName);
            hWndList hwndList = FindWindowsForApp(name, windowInfo);

            int count = hwndList.Count;
            if (count == 0)
            {
                return 0;       // no windows found
            }

            int placement = 0;
            if (count <= 3)
            {
                // for upto 3 windows, arrange as side by side
                placement = (int)Arrange.SideBySide;
            }
            else
            {
                // for more than 3 windows, arrange in quadrants
                placement = (int)Arrange.Quadrants;

            }
            const int maxCount = 12;
            // limit number of windows to 12 at max if no appName is specified
            if (count > maxCount)
            {
                count = maxCount;
            }

            // if appName is specified and is not "all", limit windows to 4
            if (!string.IsNullOrEmpty(appName) && !string.Equals("all", appName, StringComparison.OrdinalIgnoreCase))
            {
                count = count > MaxWindowsCount ? MaxWindowsCount : count;
            }

            Task task = Task.Run(() =>
            {
                // perform the arrange operation & repeat again after a short delay 
                // (sometimes windows fails to place windows correctly in one go)
                ArrangeWindows(hwndList, placement, count);
                Thread.Sleep(DefaultWaitTime);
                ArrangeWindows(hwndList, placement, count);
            });
            return count;
        }
    }
}
