using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Rlcm.Game
{
    public class Memory
    {
        private bool _active;
        private IntPtr _processHandle;
        private int _baseAddress;
        private bool _loadDelayed;

        private readonly Stopwatch _loadDelay;

        public Action<Process> OnProcessOpened { get; set; }

        public Memory()
        {
            OnProcessOpened = process => { };
            _loadDelay = new Stopwatch();
        }

        public bool Load()
        {
            // don't reopen process if already open
            if (_active)
                return true;

            // don't allow more than one try every 10 seconds
            if (_loadDelay.ElapsedMilliseconds >= 10000)
                _loadDelayed = false;

            if (_loadDelayed)
                return false;

            // restart load delay
            _loadDelay.Restart();
            _loadDelayed = true;

            foreach (var process in Process.GetProcesses())
                try
                {
                    if (process.MainModule?.ModuleName != "Rayman Legends.exe")
                        continue;

                    _processHandle = OpenProcess(0x1f0fff, false, process.Id);
                    _baseAddress = (int) process.MainModule.BaseAddress;
                    _active = true;
                    OnProcessOpened(process);
                    return true;
                }
                catch (Win32Exception)
                {
                    // ignore 64-bit processes
                }
                catch (InvalidOperationException)
                {
                    // ignore unavailable processes
                }

            return false;
        }

        public int ReadInteger(int address)
        {
            var buffer = ReadMemory(address, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteInteger(int address, int value)
        {
            var data = BitConverter.GetBytes(value);
            WriteMemory(address, data);
        }

        public float ReadFloat(int address)
        {
            var buffer = ReadMemory(address, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public void WriteFloat(int address, float value)
        {
            var data = BitConverter.GetBytes(value);
            WriteMemory(address, data);
        }

        public string ReadString(int address)
        {
            var buffer = ReadMemory(address, 100);
            return Encoding.ASCII.GetString(buffer).Split('\x0')[0];
        }

        public int GetAddress(IEnumerable<int> offsets)
        {
            return offsets.Aggregate(_baseAddress, (address, offset) => ReadInteger(address + offset));
        }

        private byte[] ReadMemory(int address, int length)
        {
            var buffer = new byte[length];
            var count = 0;

            _active = ReadProcessMemory((int) _processHandle, address, buffer, buffer.Length, ref count);
            return buffer;
        }

        private void WriteMemory(int address, byte[] data)
        {
            var count = 0;
            _active = WriteProcessMemory((int) _processHandle, address, data, data.Length, ref count);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize,
            ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize,
            ref int lpNumberOfBytesWritten);
    }
}
