using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter
{
    static class I2CAdapterManger
    {
        public static List<I2CAdapterInfo> GetAvailableDisplays()
        {
            var list = new List<I2CAdapterInfo>();

            // 1. nVIDIA NVAPI
            try
            {
                var nvList = NvidiaGetAvailableDisplays();
                if (nvList != null && nvList.Length > 0)
                    list.AddRange(nvList);
            }
            catch
            {
                // ignore NVAPI error
            }

            // 2. Intel IGCL
            try
            {
                var igclList = GetAvailableDisplaysIGCL();
                if (igclList != null && igclList.Length > 0)
                    list.AddRange(igclList);
            }
            catch
            {
                // ignore IGCL error
            }

            return list;
        }
        #region nVIDIA
        // NVAPI function delegates
        private static NvAPI_InitializeDelegate? _nvInitialize;
        private static NvAPI_EnumPhysicalGPUsDelegate? _nvEnumPhysicalGPUs;
        private static NvAPI_EnumNvidiaDisplayHandleDelegate? _nvEnumNvidiaDisplayHandle;
        private static NvAPI_GetAssociatedDisplayOutputIdDelegate? _nvGetAssociatedDisplayOutputId;
        private static NvAPI_GetAssociatedNvidiaDisplayHandleDelegate? _nvGetAssociatedNvidiaDisplayHandle;
        private static NvAPI_GetDisplayPortInfoDelegate? _nvGetDisplayPortInfo;
        private static NvAPI_GetErrorMessageDelegate? _nvGetErrorMessage;
        private const int NvDPInfoV1Size = 44;
        private const uint NvDPInfoV1Version = 0x10000u | (uint)NvDPInfoV1Size;

        private const int NvapiStatusOk = 0x00000000;
        private const int NvapiStatusEndEnumeration = unchecked((int)0xFFFFFFF9);
        private const int NvapiDpAuxTimeout = 0x000000FF;

        // QueryInterface ID
        private const uint Qi_Initialize = 0x0150E828;
        private const uint Qi_EnumPhysicalGPUs = 0xE5AC921F;
        private const uint Qi_EnumNvidiaDisplayHandle = 0x9ABDD40D;
        private const uint Qi_GetAssociatedDisplayOutputID = 0xD995937E;
        private const uint Qi_GetAssociatedNvidiaDisplayHandle = 0x9E4B6097;
        private const uint Qi_GetDisplayPortInfo = 0xC64FF367;
        private const uint Qi_GetErrorMessage = 0x6C2D048C;
        private const uint Qi_Disp_DpAuxChannelControl = 0x8EB56969;
        private const uint Qi_NvUnload = 0xD22BDD7E;
        public static I2CAdapterInfo[] NvidiaGetAvailableDisplays()
        {
            try
            {
                _nvInitialize = GetProc<NvAPI_InitializeDelegate>(Qi_Initialize);
                _nvEnumPhysicalGPUs = GetProc<NvAPI_EnumPhysicalGPUsDelegate>(Qi_EnumPhysicalGPUs);
                _nvEnumNvidiaDisplayHandle = GetProc<NvAPI_EnumNvidiaDisplayHandleDelegate>(Qi_EnumNvidiaDisplayHandle);
                _nvGetAssociatedDisplayOutputId = GetProc<NvAPI_GetAssociatedDisplayOutputIdDelegate>(Qi_GetAssociatedDisplayOutputID);
                _nvGetAssociatedNvidiaDisplayHandle = GetProc<NvAPI_GetAssociatedNvidiaDisplayHandleDelegate>(Qi_GetAssociatedNvidiaDisplayHandle);
                _nvGetDisplayPortInfo = GetProc<NvAPI_GetDisplayPortInfoDelegate>(Qi_GetDisplayPortInfo);
                _nvGetErrorMessage = GetProc<NvAPI_GetErrorMessageDelegate>(Qi_GetErrorMessage);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: nvapi64.dll not found.", ex);
            }

            if (_nvInitialize == null ||
                _nvEnumPhysicalGPUs == null ||
                _nvEnumNvidiaDisplayHandle == null ||
                _nvGetAssociatedDisplayOutputId == null ||
                _nvGetDisplayPortInfo == null)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: required entry points are missing.");
            }

            int status = _nvInitialize();
            if (status != NvapiStatusOk)
                throw StatusError(status, "NvAPI_Initialize");
            if (_nvEnumPhysicalGPUs == null)
                throw new InvalidOperationException("NVIDIA NVAPI: EnumPhysicalGPUs not available.");

            IntPtr[] handles = new IntPtr[64];
            int count = 0;
            status = _nvEnumPhysicalGPUs(handles, ref count);

            if (status != NvapiStatusOk)
                throw StatusError(status, "NvAPI_EnumPhysicalGPUs");

            if (count <= 0)
                throw new InvalidOperationException("NVIDIA NVAPI: no physical GPU detected.");
            if (_nvEnumNvidiaDisplayHandle == null ||
                _nvGetAssociatedDisplayOutputId == null ||
                _nvGetDisplayPortInfo == null)
            {
                throw new InvalidOperationException("NVIDIA NVAPI: required functions are not initialized.");
            }

            var list = new List<I2CAdapterInfo>();
            uint index = 0;

            while (true)
            {
                IntPtr handle;
                status = _nvEnumNvidiaDisplayHandle(index, out handle);

                if (status == NvapiStatusEndEnumeration)
                    break;

                if (status != NvapiStatusOk)
                    throw StatusError(status, "NvAPI_EnumNvidiaDisplayHandle");

                if (handle == IntPtr.Zero)
                {
                    index++;
                    continue;
                }

                uint outputId = 0;
                status = _nvGetAssociatedDisplayOutputId(handle, ref outputId);
                if (status != NvapiStatusOk)
                {
                    // 這裡與 Golang 範例一樣：如果失敗就 skip
                    index++;
                    continue;
                }

                var info = CreateDpInfo();
                status = _nvGetDisplayPortInfo(handle, outputId, ref info);
                if (status != NvapiStatusOk)
                {
                    index++;
                    continue;
                }

                // Flags 的最低位表示該 DP output 是否啟用
                if ((info.Flags & 1) == 0)
                {
                    index++;
                    continue;
                }

                var di = new I2CAdapterInfo
                {
                    DisplayHandle = handle,
                    DeviceIndex = 0, // NVIDIA NVAPI 暫時不支援多 GPU
                    OutputIndex = (int)index,
                    MonitorUid = outputId,
                    Description = "NVIDIA Display " + index,
                    Name = "NVIDIA GPU Output#" + index,
                    IsFromDisplay = true,
                };

                list.Add(di);
                index++;
            }
            return list.ToArray();
        }
        private static NvDPInfoV1 CreateDpInfo()
        {
            return new NvDPInfoV1
            {
                Version = NvDPInfoV1Version,
                Reserved0 = new byte[36],
                Pad = new byte[3]
            };
        }
        private static Exception StatusError(int status, string context)
        {
            if (status == NvapiStatusOk)
                return null;

            string message = string.Format("status 0x{0:X8}", (uint)status);

            if (_nvGetErrorMessage != null)
            {
                var buf = new byte[256];
                _nvGetErrorMessage(status, buf);

                int len = Array.IndexOf(buf, (byte)0);
                if (len < 0) len = buf.Length;

                string s = Encoding.ASCII.GetString(buf, 0, len).Trim();
                if (!string.IsNullOrEmpty(s))
                    message = s;
            }

            if (!string.IsNullOrEmpty(context))
            {
                return new InvalidOperationException(
                    string.Format("{0}: {1} (0x{2:X8})", context, message, (uint)status));
            }

            return new InvalidOperationException(
                string.Format("NVIDIA NVAPI error: {0} (0x{1:X8})", message, (uint)status));
        }
        private static T GetProc<T>(uint id) where T : class
        {
            IntPtr ptr = NvapiNative.NvAPI_QueryInterface(id);
            if (ptr == IntPtr.Zero)
                return null;

            return (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }
        private static class NvapiNative
        {
            [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NvAPI_QueryInterface(uint id);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumPhysicalGPUsDelegate(
            [Out] IntPtr[] handles,
            ref int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumNvidiaDisplayHandleDelegate(
            uint thisEnum,
            out IntPtr displayHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetAssociatedDisplayOutputIdDelegate(
            IntPtr displayHandle,
            ref uint outputId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetAssociatedNvidiaDisplayHandleDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string displayName,
            out IntPtr displayHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetDisplayPortInfoDelegate(
            IntPtr displayHandle,
            uint outputId,
            ref NvDPInfoV1 info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetErrorMessageDelegate(
            int status,
            [Out] byte[] buffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct NvDPInfoV1
        {
            public uint Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] Reserved0;

            public byte Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Pad;
        }

        #endregion
        #region IGCL
        private const uint CtlResultSuccess = 0;
        private const uint CtlInitAppVersion = 0x00010001;

        private static I2CAdapterInfo[] GetAvailableDisplaysIGCL()
        {
            IntPtr _apiHandle = IntPtr.Zero;
            var args = new CtlInitArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlInitArgs)),
                Version = 0,
                Reserved = new byte[3],
                AppVersion = CtlInitAppVersion,
                Flags = CtlInitFlags.None,
                SupportedVersion = 0,
                ApplicationUid = new byte[16]
            };

            uint r;
            try
            {
                r = IGCLNativeMethods.ctlInit(ref args, out _apiHandle);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: ControlLib.dll not found (interface not available).", ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: CtlInit entry point not found (incompatible ControlLib.dll).", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Intel IGCL: unexpected error calling CtlInit.", ex);
            }

            if (r != CtlResultSuccess || _apiHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlInit failed: 0x{0:X8}", r));
            }
            if (_apiHandle == IntPtr.Zero)
                throw new InvalidOperationException("Intel IGCL: API handle is null.");

            var list = new List<I2CAdapterInfo>();

            // 1. Enumerate devices (GPU)
            uint devCount = 0;
            r = IGCLNativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(count) failed: 0x{0:X8}", r));

            if (devCount == 0)
                return list.ToArray();

            var devs = new IntPtr[devCount];
            r = IGCLNativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, devs);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    string.Format("Intel IGCL: ctlEnumerateDevices(get) failed: 0x{0:X8}", r));

            // 2. For each GPU (device)，enumerate display outputs
            for (int devIndex = 0; devIndex < devs.Length; devIndex++)
            {
                var dev = devs[devIndex];
                if (dev == IntPtr.Zero)
                    continue;

                uint outCount = 0;
                r = IGCLNativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, null);
                if (r != CtlResultSuccess || outCount == 0)
                    continue;

                var outs = new IntPtr[outCount];
                r = IGCLNativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, outs);
                if (r != CtlResultSuccess)
                    continue;

                for (int outIndex = 0; outIndex < outs.Length; outIndex++)
                {
                    var outHandle = outs[outIndex];
                    if (outHandle == IntPtr.Zero)
                        continue;

                    var props = new CtlDisplayProperties
                    {
                        Size = (uint)Marshal.SizeOf(typeof(CtlDisplayProperties)),
                        Version = 0,
                    };

                    r = IGCLNativeMethods.ctlGetDisplayProperties(outHandle, ref props);
                    if (r != CtlResultSuccess)
                        continue;
                    bool isATTACHED = (props.DisplayConfigFlags & (1u << 1)) != 0;
                    if (!isATTACHED)
                        continue;
                    var info = new I2CAdapterInfo
                    {
                        DisplayHandle = outHandle,
                        DeviceIndex = devIndex,
                        OutputIndex = outIndex,
                        Name = $"Intel IGCL GPU#{devIndex} Output#{outIndex}",
                        IsNeedPrivilege = true,
                        IsFromDisplay = true,
                    };
                    list.Add(info);
                }
            }
            if (_apiHandle != IntPtr.Zero)
            {
                try
                {
                    IGCLNativeMethods.ctlClose(_apiHandle);
                }
                catch
                {
                    // ignore
                }
                _apiHandle = IntPtr.Zero;
            }
            return list.ToArray();

        }
        [Flags]
        private enum CtlInitFlags : uint
        {
            None = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CtlInitArgs
        {
            public uint Size;
            public byte Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;

            public uint AppVersion;
            public CtlInitFlags Flags;
            public uint SupportedVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ApplicationUid;
        }

        // 對應 ctl_generic_void_datatype_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlGenericVoidDatatype
        {
            public IntPtr pData;   // void*
            public uint size;      // uint32_t
        }

        // 對應 union ctl_os_display_encoder_identifier_t
        [StructLayout(LayoutKind.Explicit)]
        internal struct CtlOsDisplayEncoderIdentifier
        {
            [FieldOffset(0)]
            public uint WindowsDisplayEncoderID;       // Windows 用

            [FieldOffset(0)]
            public CtlGenericVoidDatatype DisplayEncoderID; // 非 Windows 用
        }

        // 對應 ctl_revision_datatype_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlRevisionDatatype
        {
            public byte major_version;
            public byte minor_version;
            public byte revision_version;
            public byte _padding; // 對齊到 4 bytes（C 那邊也是 3 byte + padding）
        }

        // 對應 ctl_display_timing_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlDisplayTiming
        {
            public uint Size;
            public byte Version;
            public byte _pad1;
            public byte _pad2;
            public byte _pad3;

            public ulong PixelClock;
            public uint HActive;
            public uint VActive;
            public uint HTotal;
            public uint VTotal;
            public uint HBlank;
            public uint VBlank;
            public uint HSync;
            public uint VSync;
            public float RefreshRate;
            public uint SignalStandard; // ctl_signal_standard_type_t (enum -> uint32_t)
            public byte VicId;
            public byte _pad4;
            public byte _pad5;
            public byte _pad6;
        }

        // 最重要：對應 _ctl_display_properties_t / ctl_display_properties_t
        [StructLayout(LayoutKind.Sequential)]
        internal struct CtlDisplayProperties
        {
            public uint Size;       // uint32_t
            public byte Version;    // uint8_t
            public byte _pad1;
            public byte _pad2;
            public byte _pad3;

            public CtlOsDisplayEncoderIdentifier OsDisplayEncoderHandle; // union

            // 下面這些在 header 裡都是 typedef 到 uint32_t 的 enum/flags，
            // 用 uint 對應即可（之後你可以再自行包裝成 enum）
            public uint Type;                     // ctl_display_output_types_t
            public uint AttachedDisplayMuxType;   // ctl_attached_display_mux_type_t
            public uint ProtocolConverterOutput;  // ctl_display_output_types_t
            public CtlRevisionDatatype SupportedSpec; // ctl_revision_datatype_t
            public uint SupportedOutputBPCFlags;      // ctl_output_bpc_flags_t
            public uint ProtocolConverterType;        // ctl_protocol_converter_location_flags_t
            public uint DisplayConfigFlags;           // ctl_display_config_flags_t
            public uint FeatureEnabledFlags;          // ctl_std_display_feature_flags_t
            public uint FeatureSupportedFlags;        // ctl_std_display_feature_flags_t
            public uint AdvancedFeatureEnabledFlags;  // ctl_intel_display_feature_flags_t
            public uint AdvancedFeatureSupportedFlags;// ctl_intel_display_feature_flags_t

            public CtlDisplayTiming DisplayTimingInfo; // ctl_display_timing_t

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] ReservedFields;             // uint32_t ReservedFields[16]
        }


        private static class IGCLNativeMethods
        {
            private const string DllName = "ControlLib.dll";

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlInit(
                ref CtlInitArgs args,
                out IntPtr apiHandle);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlClose(
                IntPtr apiHandle);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlEnumerateDevices(
                IntPtr apiHandle,
                ref uint count,
                [In, Out] IntPtr[] devices);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlEnumerateDisplayOutputs(
                IntPtr deviceHandle,
                ref uint count,
                [In, Out] IntPtr[] outputs);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlGetDisplayProperties(
                IntPtr outputHandle,
                ref CtlDisplayProperties props);
        }
    }
    #endregion
}
