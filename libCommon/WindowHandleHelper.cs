﻿using libCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static libCommon.NativeMethods;

namespace libUIHelpers
{
    public static class WindowHandleHelper
    {
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        const int WM_GETTEXT = 0x0D;
        const int WM_SETTEXT = 0x000C;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int Param, StringBuilder text);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static IntPtr? GetRootWindowByTitle(int pid, IntPtr? desktopHandle, Func<string, bool> condition)
        {
            var rootWindows = GetRootWindowsOfProcess(pid, desktopHandle);

            var titles = rootWindows
                            .Select(hWnd => new
                            {
                                Title = GetWindowText(hWnd),
                                Handle = hWnd
                            })
                            .ToList();

            var result = titles
                            .FirstOrDefault(child => condition(child.Title));

            return result?.Handle;
        }

        public static IntPtr? GetChildHandleByText(int pid, IntPtr? desktopHandle, Func<string, bool> condition)
        {
            var result = GetChildHandle(pid, desktopHandle, childHandle =>
            {
                var text = GetWindowText(childHandle);
                var matchesCondition = condition(text);
                return matchesCondition;
            });

            return result;
        }

        public static IntPtr? GetChildHandleByText(IntPtr parentHandle, Func<string, bool> condition)
        {
            var result = GetChildHandle(parentHandle, childHandle =>
            {
                var text = GetWindowText(childHandle);
                var matchesCondition = condition(text);
                return matchesCondition;
            });

            return result;
        }

        public static IntPtr? GetChildHandle(int pid, IntPtr? desktopHandle, Func<IntPtr, bool> childFilter)
        {
            var rootWindows = GetRootWindowsOfProcess(pid, desktopHandle);

            var childWindows = rootWindows
                                .SelectMany(rootWindow => GetChildWindows(rootWindow))
                                .ToList();

            var result = childWindows
                            .FirstOrDefault(child => childFilter(child));

            return result;
        }

        public static IntPtr? GetChildHandle(IntPtr parentHandle, Func<IntPtr, bool> childFilter)
        {
            var childWindows = GetChildWindows(parentHandle);

            var result = childWindows
                            .FirstOrDefault(child => childFilter(child));

            return result;
        }

        public static List<IntPtr> GetRootWindowsOfProcess(int pid, IntPtr? desktopHandle)
        {
            List<IntPtr> rootWindows;
            if (desktopHandle == null)
            {
                rootWindows = GetChildWindows(IntPtr.Zero); //todo: Couldn't we just give the pid to this call, to save us iterating over all windows?
            }
            else
            {
                rootWindows = new();

                EnumDesktopWindows(desktopHandle.Value, (handle, lParam) =>
                {
                    rootWindows.Add(handle);
                    return true;
                },
                IntPtr.Zero);
            }

            List<IntPtr> dsProcRootWindows = new();
            foreach (IntPtr hWnd in rootWindows)
            {
                GetWindowThreadProcessId(hWnd, out uint lpdwProcessId);
                if (lpdwProcessId == pid)
                {
                    dsProcRootWindows.Add(hWnd);
                }
            }
            return dsProcRootWindows;
        }

        public static List<(IntPtr Handle, string ControlClass, string Text, string ClassNN)> GetChildWindowsDetailsRecursively(IntPtr handle)
        {
            var result = GetChildWindowsDetailsRecursively(new List<IntPtr>() { handle });
            return result;
        }

        public static List<(IntPtr Handle, string ControlClass, string Text, string ClassNN)> GetChildWindowsDetailsRecursively(List<IntPtr> handles)
        {
            var allHandles = handles
                                .SelectMany(h => GetChildWindowsRecursively(h))
                                .ToList();

            var classCounts = new Dictionary<string, int>();

            var result = allHandles
                            .Select(h =>
                            {
                                var text = GetWindowText(h);
                                var className = GetClassName(h);

                                if (!classCounts.ContainsKey(className))
                                {
                                    classCounts.Add(className, 0);
                                }

                                classCounts[className] = classCounts[className] + 1;

                                var classNN = $"{className}{classCounts[className]}";

                                return (h, className, text, classNN);
                            })
                            .ToList();

            return result;
        }

        public static string GetClassName(IntPtr handle)
        {
            var sb = new StringBuilder(256);
            GetClassName(handle, sb, sb.Capacity);

            var result = sb.ToString();
            return result;
        }

        //SendMessage(textBox1.Handle, WM_SETTEXT, IntPtr.Zero,
        public static void SetWindowText(IntPtr handle, string text)
        {
            var sb = new StringBuilder();
            sb.Append(text);
            SendMessage(handle, WM_SETTEXT, 0, sb);
        }

        public static string GetWindowText(IntPtr handle)
        {
            var sb = new StringBuilder(255);
            SendMessage(handle, WM_GETTEXT, sb.Capacity, sb);

            var result = sb.ToString();
            return result;
        }

        public static List<IntPtr> GetChildWindowsRecursively(IntPtr hWnd)
        {
            var result = Extensions.Recurse(new[] { hWnd }, hParent =>
            {
                var childWindows = GetChildWindows(hParent);

                return childWindows;
            }).ToList();

            return result;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parentHandle)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                var childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parentHandle, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }
    }
}
