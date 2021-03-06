﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace TestUtilities
{
    /// <summary>
    /// Unmanaged API wrappers.
    /// </summary>
    internal static class NativeMethods
    {
        internal const int SW_SHOW = 5;

        /// <summary>
        /// Get the ROT.
        /// </summary>
        /// <param name="reserved">Reserved.</param>
        /// <param name="prot">Pointer to running object table interface</param>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        internal static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        /// <summary>
        /// Win32 GetWindowThreadProcessId: Get process ID from a window handle
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

        /// <summary>
        /// Create a Bind context.
        /// </summary>
        /// <param name="reserved">Reserved.</param>
        /// <param name="ppbc">Bind context.</param>
        /// <returns>HRESULT</returns>
        [DllImport("ole32.dll")]
        internal static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        /// <summary>
        /// Register message filter for COM.
        /// </summary>
        /// <param name="lpMessageFilter">New filter to register.</param>
        /// <param name="lplpMessageFilter">Old filter. Save it if you need to restore it later.</param>
        [DllImport("ole32.dll")]
        internal static extern int CoRegisterMessageFilter(IMessageFilter lpMessageFilter, out IMessageFilter lplpMessageFilter);

        /// <summary>
        /// Get the foreground window
        /// </summary>
        /// <returns>An HWND for the current foreground window</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Sets the foreground window
        /// </summary>
        /// <param name="hWnd">HWND of the new foreground window</param>
        /// <returns>true if the operation succeeded</returns>
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Posts a windows message to the message queue of a target window and waits for the message to be processed.
        /// </summary>
        /// <param name="hWnd">The HWND for the target window</param>
        /// <param name="nMessage">The message ID</param>
        /// <param name="wParam">Message-specific WPARAM value</param>
        /// <param name="lParam">Message-specific LPARAM value</param>
        /// <returns>Message-specific LRESULT</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint nMessage, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Posts a windows message to the message queue of a target window and returns immediately.
        /// </summary>
        /// <param name="hWnd">The HWND for the target window</param>
        /// <param name="nMessage">The message ID</param>
        /// <param name="wParam">Message-specific WPARAM value</param>
        /// <param name="lParam">Message-specific LPARAM value</param>
        /// <returns>Message-specific LRESULT</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr PostMessage(IntPtr hWnd, uint nMessage, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Registers a new message ID based on the message name; the ID is uniquely tied to the name.
        /// </summary>
        /// <param name="lpString">Message name</param>
        /// <returns>A new message ID</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint RegisterWindowMessage(string lpString);

        /// <summary>
        /// Return the thread id and process id of the thread that owns the given window.
        /// </summary>
        /// <param name="hWnd">The HWND for the target window</param>
        /// <param name="processId">The process id which owns hWnd</param>
        /// <returns>The thread id which owns hWnd</returns>
        [DllImport("user32.dll")]
        internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        /// <summary>
        /// Return the thread id of the thread that owns the given window.
        /// </summary>
        /// <param name="hWnd">The HWND for the target window</param>
        /// <param name="processId">Set this to IntPtr.Zero</param>
        /// <returns>The thread id which owns hWnd</returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        internal static extern int GetWindowThreadId(IntPtr hWnd, IntPtr processId);

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
        /// </summary>
        /// <param name="idAttach">The identifier of the thread to be attached to another thread</param>
        /// <param name="idAttachTo">The identifier of the thread to which idAttach will be attached</param>
        /// <param name="fAttach">If true, the two threads are attached. If false, the threads are detached</param>
        /// <returns>true if the operation succeeded</returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">The HWND for the window to be shown</param>
        /// <param name="cmdShow">A command which controls how the window will be shown</param>
        /// <returns>true if the operation succeeded</returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
    }
}
