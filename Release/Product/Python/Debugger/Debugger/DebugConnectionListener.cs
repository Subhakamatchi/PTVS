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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Microsoft.PythonTools.Debugger {
    /// <summary>
    /// Handles connections from all debuggers.
    /// </summary>
    class DebugConnectionListener {
        private static Socket _listenerSocket;
        private static readonly Dictionary<Guid, WeakReference> _targets = new Dictionary<Guid, WeakReference>();

        public static void RegisterProcess(Guid id, PythonProcess process) {
            lock (_targets) {
                EnsureListenerSocket();

                _targets[id] = new WeakReference(process);
            }
        }

        public static int ListenerPort {
            get {
                lock (_targets) {
                    EnsureListenerSocket();
                }

                return ((IPEndPoint)_listenerSocket.LocalEndPoint).Port;
            }
        }

        private static void EnsureListenerSocket() {
            if (_listenerSocket == null) {
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                _listenerSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                _listenerSocket.Listen(0);
                var listenerThread = new Thread(ListenForConnection);
                listenerThread.Name = "Python Debug Connection Listener";
                listenerThread.Start();
            }
        }

        public static void UnregisterProcess(Guid id) {
            _targets.Remove(id);
        }

        private static void ListenForConnection() {
            try {
                try {
                    for (; ; ) {
                        var socket = _listenerSocket.Accept();
                        try {
                            socket.Blocking = true;
                            string debugId = socket.ReadString();

                            lock (_targets) {
                                Guid debugGuid;
                                WeakReference weakProcess;
                                PythonProcess targetProcess;

                                if (Guid.TryParse(debugId, out debugGuid) && _targets.TryGetValue(debugGuid, out weakProcess) &&
                                    (targetProcess = weakProcess.Target as PythonProcess) != null) {
                                    targetProcess.Connected(socket);
                                } else {
                                    Debug.WriteLine("Unknown debug target: {0}", debugId);
                                    socket.Close();
                                }
                            }
                        } catch (SocketException) {
                        }
                    }

                } finally {
                    _listenerSocket.Close();
                    _listenerSocket = null;
                }
            } catch (SocketException) {
            }                            
        }

    }
}
