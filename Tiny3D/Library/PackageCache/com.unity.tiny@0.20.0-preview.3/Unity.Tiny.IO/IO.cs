using System;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities.Serialization;
using System.Collections.Generic;
using Unity.Tiny;
using System.Diagnostics;

namespace Unity.Tiny.IO
{
    /// <summary>
    /// Asynchronous handle to be held while dealing with file operations. Users should use the provided
    /// member functions to poll for status of the operation before fetching data.
    /// </summary>
    public struct AsyncOp : IDisposable
    {
        /// <summary>
        /// Status of an IO operation's completion state
        /// </summary>
        public enum Status
        {
            NotStarted = 0, // Note this order is important for checking if an op is complete or not by doing 'status <= InProgress'
            InProgress,
            Failure,
            Success
        }

        /// <summary>
        /// Status flags for determining why an IO operation failed.
        /// </summary>
        public enum ErrorStatus
        {
            None = 0,
            FileNotFound,
            Unknown
        }

        internal AsyncOp(int sysHandle)
        {
            m_Handle = sysHandle;
        }

        private int m_Handle;

        /// <summary>
        /// Returns the current state of the AsyncOp
        /// </summary>
        /// <returns></returns>
        public Status GetStatus()
        {
            return (Status)GetStatusImpl(m_Handle);
        }

        /// <summary>
        /// Provides best-effort information as to why an AsyncOp has failed, otherwise returns ErrorStatus.None
        /// </summary>
        /// <returns></returns>
        public ErrorStatus GetErrorStatus()
        {
            return (ErrorStatus)GetErrorStatusImpl(m_Handle);
        }

        /// <summary>
        /// Closes file handles are frees any other resources associated with the AsyncOp
        /// </summary>
        public void Dispose()
        {
            CloseImpl(m_Handle);
            m_Handle = 0;
        }

        /// <summary>
        ///  Provides a pointer to the requested IO data in memory once the
        ///  AsyncOp.GetRequestState() == RequestState.Complete otherwise null and a size of 0 is returned.
        ///  Users should treat memory returned as read-only and to only be valid until the AsynOp is Dispose()'d
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sizeInBytes"></param>
        public unsafe void GetData(out byte* data, out int sizeInBytes)
        {
            byte* inData = null;
            int inSize = 0;

            GetDataImpl(m_Handle, ref inData, ref inSize);

            data = inData;
            sizeInBytes = inSize;
        }

        /// <summary>
        /// Returns true if the AsyncOp is valid and needs to be Dispose()'d
        /// </summary>
        public bool IsCreated
        {
            get { return m_Handle > 0; }
        }

        public override string ToString()
        {
            return $"AsyncOp({m_Handle})";
        }

        [DllImport("lib_unity_tiny_io", EntryPoint = "GetStatus")]
        static extern unsafe int GetStatusImpl(int handle);
        [DllImport("lib_unity_tiny_io", EntryPoint = "GetErrorStatus")]
        static extern unsafe int GetErrorStatusImpl(int handle);
        [DllImport("lib_unity_tiny_io", EntryPoint = "Close")]
        static extern unsafe int CloseImpl(int handle);
        [DllImport("lib_unity_tiny_io", EntryPoint = "GetData")]
        static extern unsafe void GetDataImpl(int handle, ref byte* data, ref int sizeInBytes);
    }

    /// <summary>
    /// Provides utility functions for interating with the files
    /// </summary>
    public static class IOService
    {
        #if UNITY_PLATFORM_WINDOWS
            public const string PathSeparator = "\\";
        #else
            public const string PathSeparator = "/";
        #endif
                
        [DllImport("lib_unity_tiny_io", EntryPoint = "RequestAsyncRead", CharSet = CharSet.Ansi)]
        static extern unsafe int RequestAsyncReadImpl([MarshalAs(UnmanagedType.LPStr)]string path);

        /// <summary>
        /// Issues an asynchronous request to read all data from the given filepath or URI. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public AsyncOp RequestAsyncRead(string path)
        {
            return new AsyncOp(RequestAsyncReadImpl(path));
        }
    }
}

