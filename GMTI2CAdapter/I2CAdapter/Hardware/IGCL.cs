// Target: .NET Framework 4.8
// Language: C#
using System.Runtime.InteropServices;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    /// <summary>
    /// 對應 IGCL 中 ctl*** 系列函式的回傳碼。
    /// </summary>
    internal enum CtlResult : uint
    {
        CTL_RESULT_SUCCESS = 0x00000000,
        CTL_RESULT_SUCCESS_STILL_OPEN_BY_ANOTHER_CALLER = 0x00000001,
        CTL_RESULT_ERROR_SUCCESS_END = 0x0000FFFF,
        CTL_RESULT_ERROR_GENERIC_START = 0x40000000,
        CTL_RESULT_ERROR_NOT_INITIALIZED = 0x40000001,
        CTL_RESULT_ERROR_ALREADY_INITIALIZED = 0x40000002,
        CTL_RESULT_ERROR_DEVICE_LOST = 0x40000003,
        CTL_RESULT_ERROR_OUT_OF_HOST_MEMORY = 0x40000004,
        CTL_RESULT_ERROR_OUT_OF_DEVICE_MEMORY = 0x40000005,
        CTL_RESULT_ERROR_INSUFFICIENT_PERMISSIONS = 0x40000006,
        CTL_RESULT_ERROR_NOT_AVAILABLE = 0x40000007,
        CTL_RESULT_ERROR_UNINITIALIZED = 0x40000008,
        CTL_RESULT_ERROR_UNSUPPORTED_VERSION = 0x40000009,
        CTL_RESULT_ERROR_UNSUPPORTED_FEATURE = 0x4000000a,
        CTL_RESULT_ERROR_INVALID_ARGUMENT = 0x4000000b,
        CTL_RESULT_ERROR_INVALID_API_HANDLE = 0x4000000c,
        CTL_RESULT_ERROR_INVALID_NULL_HANDLE = 0x4000000d,
        CTL_RESULT_ERROR_INVALID_NULL_POINTER = 0x4000000e,
        CTL_RESULT_ERROR_INVALID_SIZE = 0x4000000f,
        CTL_RESULT_ERROR_UNSUPPORTED_SIZE = 0x40000010,
        CTL_RESULT_ERROR_UNSUPPORTED_IMAGE_FORMAT = 0x40000011,
        CTL_RESULT_ERROR_DATA_READ = 0x40000012,
        CTL_RESULT_ERROR_DATA_WRITE = 0x40000013,
        CTL_RESULT_ERROR_DATA_NOT_FOUND = 0x40000014,
        CTL_RESULT_ERROR_NOT_IMPLEMENTED = 0x40000015,
        CTL_RESULT_ERROR_OS_CALL = 0x40000016,
        CTL_RESULT_ERROR_KMD_CALL = 0x40000017,
        CTL_RESULT_ERROR_UNLOAD = 0x40000018,
        CTL_RESULT_ERROR_ZE_LOADER = 0x40000019,
        CTL_RESULT_ERROR_INVALID_OPERATION_TYPE = 0x4000001a,
        CTL_RESULT_ERROR_NULL_OS_INTERFACE = 0x4000001b,
        CTL_RESULT_ERROR_NULL_OS_ADAPATER_HANDLE = 0x4000001c,
        CTL_RESULT_ERROR_NULL_OS_DISPLAY_OUTPUT_HANDLE = 0x4000001d,
        CTL_RESULT_ERROR_WAIT_TIMEOUT = 0x4000001e,
        CTL_RESULT_ERROR_PERSISTANCE_NOT_SUPPORTED = 0x4000001f,
        CTL_RESULT_ERROR_PLATFORM_NOT_SUPPORTED = 0x40000020,
        CTL_RESULT_ERROR_UNKNOWN_APPLICATION_UID = 0x40000021,
        CTL_RESULT_ERROR_INVALID_ENUMERATION = 0x40000022,
        CTL_RESULT_ERROR_FILE_DELETE = 0x40000023,
        CTL_RESULT_ERROR_RESET_DEVICE_REQUIRED = 0x40000024,
        CTL_RESULT_ERROR_FULL_REBOOT_REQUIRED = 0x40000025,
        CTL_RESULT_ERROR_LOAD = 0x40000026,
        CTL_RESULT_ERROR_UNKNOWN = 0x4000FFFF,
        CTL_RESULT_ERROR_RETRY_OPERATION = 0x40010000,
        CTL_RESULT_ERROR_IGSC_LOADER = 0x40010001,
        CTL_RESULT_ERROR_RESTRICTED_APPLICATION = 0x40010002,
        CTL_RESULT_ERROR_GENERIC_END = 0x4000FFFF,
        CTL_RESULT_ERROR_CORE_START = 0x44000000,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_NOT_SUPPORTED = 0x44000001,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_VOLTAGE_OUTSIDE_RANGE = 0x44000002,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_FREQUENCY_OUTSIDE_RANGE = 0x44000003,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_POWER_OUTSIDE_RANGE = 0x44000004,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_TEMPERATURE_OUTSIDE_RANGE = 0x44000005,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_IN_VOLTAGE_LOCKED_MODE = 0x44000006,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_RESET_REQUIRED = 0x44000007,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_WAIVER_NOT_SET = 0x44000008,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_DEPRECATED_API = 0x44000009,
        CTL_RESULT_ERROR_CORE_LED_GET_STATE_NOT_SUPPORTED_FOR_I2C_LED = 0x4400000a,
        CTL_RESULT_ERROR_CORE_LED_SET_STATE_NOT_SUPPORTED_FOR_I2C_LED = 0x4400000b,
        CTL_RESULT_ERROR_CORE_LED_TOO_FREQUENT_SET_REQUESTS = 0x4400000c,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_VRAM_MEMORY_SPEED_OUTSIDE_RANGE = 0x4400000d,
        CTL_RESULT_ERROR_CORE_OVERCLOCK_INVALID_CUSTOM_VF_CURVE = 0x4400000e,
        CTL_RESULT_ERROR_CORE_END = 0x0440FFFF,
        CTL_RESULT_ERROR_3D_START = 0x60000000,
        CTL_RESULT_ERROR_3D_END = 0x6000FFFF,
        CTL_RESULT_ERROR_MEDIA_START = 0x50000000,
        CTL_RESULT_ERROR_MEDIA_END = 0x5000FFFF,
        CTL_RESULT_ERROR_DISPLAY_START = 0x48000000,
        CTL_RESULT_ERROR_INVALID_AUX_ACCESS_FLAG = 0x48000001,
        CTL_RESULT_ERROR_INVALID_SHARPNESS_FILTER_FLAG = 0x48000002,
        CTL_RESULT_ERROR_DISPLAY_NOT_ATTACHED = 0x48000003,
        CTL_RESULT_ERROR_DISPLAY_NOT_ACTIVE = 0x48000004,
        CTL_RESULT_ERROR_INVALID_POWERFEATURE_OPTIMIZATION_FLAG = 0x48000005,
        CTL_RESULT_ERROR_INVALID_POWERSOURCE_TYPE_FOR_DPST = 0x48000006,
        CTL_RESULT_ERROR_INVALID_PIXTX_GET_CONFIG_QUERY_TYPE = 0x48000007,
        CTL_RESULT_ERROR_INVALID_PIXTX_SET_CONFIG_OPERATION_TYPE = 0x48000008,
        CTL_RESULT_ERROR_INVALID_SET_CONFIG_NUMBER_OF_SAMPLES = 0x48000009,
        CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_ID = 0x4800000a,
        CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_TYPE = 0x4800000b,
        CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_NUMBER = 0x4800000c,
        CTL_RESULT_ERROR_INSUFFICIENT_PIXTX_BLOCK_CONFIG_MEMORY = 0x4800000d,
        CTL_RESULT_ERROR_3DLUT_INVALID_PIPE = 0x4800000e,
        CTL_RESULT_ERROR_3DLUT_INVALID_DATA = 0x4800000f,
        CTL_RESULT_ERROR_3DLUT_NOT_SUPPORTED_IN_HDR = 0x48000010,
        CTL_RESULT_ERROR_3DLUT_INVALID_OPERATION = 0x48000011,
        CTL_RESULT_ERROR_3DLUT_UNSUCCESSFUL = 0x48000012,
        CTL_RESULT_ERROR_AUX_DEFER = 0x48000013,
        CTL_RESULT_ERROR_AUX_TIMEOUT = 0x48000014,
        CTL_RESULT_ERROR_AUX_INCOMPLETE_WRITE = 0x48000015,
        CTL_RESULT_ERROR_I2C_AUX_STATUS_UNKNOWN = 0x48000016,
        CTL_RESULT_ERROR_I2C_AUX_UNSUCCESSFUL = 0x48000017,
        CTL_RESULT_ERROR_LACE_INVALID_DATA_ARGUMENT_PASSED = 0x48000018,
        CTL_RESULT_ERROR_EXTERNAL_DISPLAY_ATTACHED = 0x48000019,
        CTL_RESULT_ERROR_CUSTOM_MODE_STANDARD_CUSTOM_MODE_EXISTS = 0x4800001a,
        CTL_RESULT_ERROR_CUSTOM_MODE_NON_CUSTOM_MATCHING_MODE_EXISTS = 0x4800001b,
        CTL_RESULT_ERROR_CUSTOM_MODE_INSUFFICIENT_MEMORY = 0x4800001c,
        CTL_RESULT_ERROR_ADAPTER_ALREADY_LINKED = 0x4800001d,
        CTL_RESULT_ERROR_ADAPTER_NOT_IDENTICAL = 0x4800001e,
        CTL_RESULT_ERROR_ADAPTER_NOT_SUPPORTED_ON_LDA_SECONDARY = 0x4800001f,
        CTL_RESULT_ERROR_SET_FBC_FEATURE_NOT_SUPPORTED = 0x48000020,
        CTL_RESULT_ERROR_DISPLAY_END = 0x4800FFFF,
        CTL_RESULT_MAX
    }

    internal static class CtlResultExtensions
    {
        private static readonly IReadOnlyDictionary<CtlResult, string> ResultDescriptions =
            new Dictionary<CtlResult, string>
            {
                { CtlResult.CTL_RESULT_SUCCESS, "success" },
                { CtlResult.CTL_RESULT_SUCCESS_STILL_OPEN_BY_ANOTHER_CALLER, "success but still open by another caller" },
                { CtlResult.CTL_RESULT_ERROR_SUCCESS_END, "success group error code end value" },
                { CtlResult.CTL_RESULT_ERROR_GENERIC_START, "generic error code start value" },
                { CtlResult.CTL_RESULT_ERROR_NOT_INITIALIZED, "result not initialized" },
                { CtlResult.CTL_RESULT_ERROR_ALREADY_INITIALIZED, "already initialized" },
                { CtlResult.CTL_RESULT_ERROR_DEVICE_LOST, "device hung, reset, was removed, or driver update occurred" },
                { CtlResult.CTL_RESULT_ERROR_OUT_OF_HOST_MEMORY, "insufficient host memory" },
                { CtlResult.CTL_RESULT_ERROR_OUT_OF_DEVICE_MEMORY, "insufficient device memory" },
                { CtlResult.CTL_RESULT_ERROR_INSUFFICIENT_PERMISSIONS, "access denied due to permission level" },
                { CtlResult.CTL_RESULT_ERROR_NOT_AVAILABLE, "resource was removed" },
                { CtlResult.CTL_RESULT_ERROR_UNINITIALIZED, "library not initialized" },
                { CtlResult.CTL_RESULT_ERROR_UNSUPPORTED_VERSION, "unsupported version" },
                { CtlResult.CTL_RESULT_ERROR_UNSUPPORTED_FEATURE, "unsupported feature" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_ARGUMENT, "invalid argument" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_API_HANDLE, "API handle is invalid" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_NULL_HANDLE, "handle argument is not valid" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_NULL_POINTER, "pointer argument may not be null" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_SIZE, "size argument is invalid" },
                { CtlResult.CTL_RESULT_ERROR_UNSUPPORTED_SIZE, "size argument not supported by device" },
                { CtlResult.CTL_RESULT_ERROR_UNSUPPORTED_IMAGE_FORMAT, "image format not supported" },
                { CtlResult.CTL_RESULT_ERROR_DATA_READ, "data read error" },
                { CtlResult.CTL_RESULT_ERROR_DATA_WRITE, "data write error" },
                { CtlResult.CTL_RESULT_ERROR_DATA_NOT_FOUND, "data not found" },
                { CtlResult.CTL_RESULT_ERROR_NOT_IMPLEMENTED, "function not implemented" },
                { CtlResult.CTL_RESULT_ERROR_OS_CALL, "operating system call failure" },
                { CtlResult.CTL_RESULT_ERROR_KMD_CALL, "kernel mode driver call failure" },
                { CtlResult.CTL_RESULT_ERROR_UNLOAD, "library unload failure" },
                { CtlResult.CTL_RESULT_ERROR_ZE_LOADER, "Level Zero loader not found" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_OPERATION_TYPE, "invalid operation type" },
                { CtlResult.CTL_RESULT_ERROR_NULL_OS_INTERFACE, "null OS interface" },
                { CtlResult.CTL_RESULT_ERROR_NULL_OS_ADAPATER_HANDLE, "null OS adapter handle" },
                { CtlResult.CTL_RESULT_ERROR_NULL_OS_DISPLAY_OUTPUT_HANDLE, "null display output handle" },
                { CtlResult.CTL_RESULT_ERROR_WAIT_TIMEOUT, "wait timed out" },
                { CtlResult.CTL_RESULT_ERROR_PERSISTANCE_NOT_SUPPORTED, "persistence not supported" },
                { CtlResult.CTL_RESULT_ERROR_PLATFORM_NOT_SUPPORTED, "platform not supported" },
                { CtlResult.CTL_RESULT_ERROR_UNKNOWN_APPLICATION_UID, "unknown application UID" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_ENUMERATION, "invalid enumeration" },
                { CtlResult.CTL_RESULT_ERROR_FILE_DELETE, "file delete error" },
                { CtlResult.CTL_RESULT_ERROR_RESET_DEVICE_REQUIRED, "reset required" },
                { CtlResult.CTL_RESULT_ERROR_FULL_REBOOT_REQUIRED, "full reboot required" },
                { CtlResult.CTL_RESULT_ERROR_LOAD, "library load failure" },
                { CtlResult.CTL_RESULT_ERROR_UNKNOWN, "unknown or internal error" },
                { CtlResult.CTL_RESULT_ERROR_RETRY_OPERATION, "operation failed, retry" },
                { CtlResult.CTL_RESULT_ERROR_IGSC_LOADER, "IGSC library loader not found" },
                { CtlResult.CTL_RESULT_ERROR_RESTRICTED_APPLICATION, "unsupported application" },
                { CtlResult.CTL_RESULT_ERROR_GENERIC_END, "generic error code end value" },
                { CtlResult.CTL_RESULT_ERROR_CORE_START, "core error code start value" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_NOT_SUPPORTED, "overclock not supported" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_VOLTAGE_OUTSIDE_RANGE, "voltage outside acceptable range" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_FREQUENCY_OUTSIDE_RANGE, "frequency outside acceptable range" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_POWER_OUTSIDE_RANGE, "power outside acceptable range" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_TEMPERATURE_OUTSIDE_RANGE, "temperature outside acceptable range" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_IN_VOLTAGE_LOCKED_MODE, "overclock in voltage locked mode" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_RESET_REQUIRED, "change requires device reset" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_WAIVER_NOT_SET, "overclock waiver not set" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_DEPRECATED_API, "deprecated API" },
                { CtlResult.CTL_RESULT_ERROR_CORE_LED_GET_STATE_NOT_SUPPORTED_FOR_I2C_LED, "cannot get LED state for I2C LED" },
                { CtlResult.CTL_RESULT_ERROR_CORE_LED_SET_STATE_NOT_SUPPORTED_FOR_I2C_LED, "cannot set LED state for I2C LED" },
                { CtlResult.CTL_RESULT_ERROR_CORE_LED_TOO_FREQUENT_SET_REQUESTS, "LED set requests too frequent" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_VRAM_MEMORY_SPEED_OUTSIDE_RANGE, "VRAM memory speed outside range" },
                { CtlResult.CTL_RESULT_ERROR_CORE_OVERCLOCK_INVALID_CUSTOM_VF_CURVE, "invalid custom VF curve" },
                { CtlResult.CTL_RESULT_ERROR_CORE_END, "core error code end value" },
                { CtlResult.CTL_RESULT_ERROR_3D_START, "3D error code start value" },
                { CtlResult.CTL_RESULT_ERROR_3D_END, "3D error code end value" },
                { CtlResult.CTL_RESULT_ERROR_MEDIA_START, "media error code start value" },
                { CtlResult.CTL_RESULT_ERROR_MEDIA_END, "media error code end value" },
                { CtlResult.CTL_RESULT_ERROR_DISPLAY_START, "display error code start value" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_AUX_ACCESS_FLAG, "invalid AUX access flag" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_SHARPNESS_FILTER_FLAG, "invalid sharpness filter flag" },
                { CtlResult.CTL_RESULT_ERROR_DISPLAY_NOT_ATTACHED, "display not attached" },
                { CtlResult.CTL_RESULT_ERROR_DISPLAY_NOT_ACTIVE, "display attached but not active" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_POWERFEATURE_OPTIMIZATION_FLAG, "invalid power optimization flag" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_POWERSOURCE_TYPE_FOR_DPST, "DPST supported only in DC mode" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_PIXTX_GET_CONFIG_QUERY_TYPE, "invalid pixel transformation get configuration query" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_PIXTX_SET_CONFIG_OPERATION_TYPE, "invalid pixel transformation set configuration operation" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_SET_CONFIG_NUMBER_OF_SAMPLES, "invalid number of samples for configuration" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_ID, "invalid pixel transformation block id" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_TYPE, "invalid pixel transformation block type" },
                { CtlResult.CTL_RESULT_ERROR_INVALID_PIXTX_BLOCK_NUMBER, "invalid pixel transformation block number" },
                { CtlResult.CTL_RESULT_ERROR_INSUFFICIENT_PIXTX_BLOCK_CONFIG_MEMORY, "insufficient memory for block configuration" },
                { CtlResult.CTL_RESULT_ERROR_3DLUT_INVALID_PIPE, "invalid pipe for 3D LUT" },
                { CtlResult.CTL_RESULT_ERROR_3DLUT_INVALID_DATA, "invalid 3D LUT data" },
                { CtlResult.CTL_RESULT_ERROR_3DLUT_NOT_SUPPORTED_IN_HDR, "3D LUT not supported in HDR" },
                { CtlResult.CTL_RESULT_ERROR_3DLUT_INVALID_OPERATION, "invalid 3D LUT operation" },
                { CtlResult.CTL_RESULT_ERROR_3DLUT_UNSUCCESSFUL, "3D LUT call unsuccessful" },
                { CtlResult.CTL_RESULT_ERROR_AUX_DEFER, "AUX defer failure" },
                { CtlResult.CTL_RESULT_ERROR_AUX_TIMEOUT, "AUX timeout failure" },
                { CtlResult.CTL_RESULT_ERROR_AUX_INCOMPLETE_WRITE, "AUX incomplete write" },
                { CtlResult.CTL_RESULT_ERROR_I2C_AUX_STATUS_UNKNOWN, "I2C/AUX unknown failure" },
                { CtlResult.CTL_RESULT_ERROR_I2C_AUX_UNSUCCESSFUL, "I2C/AUX unsuccessful" },
                { CtlResult.CTL_RESULT_ERROR_LACE_INVALID_DATA_ARGUMENT_PASSED, "invalid LACE data argument" },
                { CtlResult.CTL_RESULT_ERROR_EXTERNAL_DISPLAY_ATTACHED, "external display attached" },
                { CtlResult.CTL_RESULT_ERROR_CUSTOM_MODE_STANDARD_CUSTOM_MODE_EXISTS, "standard custom mode exists" },
                { CtlResult.CTL_RESULT_ERROR_CUSTOM_MODE_NON_CUSTOM_MATCHING_MODE_EXISTS, "non-custom matching mode exists" },
                { CtlResult.CTL_RESULT_ERROR_CUSTOM_MODE_INSUFFICIENT_MEMORY, "insufficient memory for custom mode" },
                { CtlResult.CTL_RESULT_ERROR_ADAPTER_ALREADY_LINKED, "adapter already linked" },
                { CtlResult.CTL_RESULT_ERROR_ADAPTER_NOT_IDENTICAL, "adapter not identical for linking" },
                { CtlResult.CTL_RESULT_ERROR_ADAPTER_NOT_SUPPORTED_ON_LDA_SECONDARY, "adapter not supported on LDA secondary" },
                { CtlResult.CTL_RESULT_ERROR_SET_FBC_FEATURE_NOT_SUPPORTED, "set FBC feature not supported" },
                { CtlResult.CTL_RESULT_ERROR_DISPLAY_END, "display error code end value" },
            };

        public static string Describe(this CtlResult result)
        {
            string name = Enum.IsDefined(typeof(CtlResult), result) ? result.ToString() : "UNKNOWN";
            return ResultDescriptions.TryGetValue(result, out var desc)
                ? $"{name}: {desc} (0x{(uint)result:X8})"
                : $"{name} (0x{(uint)result:X8})";
        }
    }

    /// <summary>
    /// Intel Graphics Control Library (ControlLib.dll) 封裝。
    /// 透過 IGCL 提供 AUX(DPCD) / I2C 讀寫。
    ///
    /// - 不實作任何共用 interface（先專心做 IGCL 版本）。
    /// - DisplayInfo 會包含 GPU LUID 與輸出索引，可以辨識不同 GPU。
    /// </summary>
    public sealed class IntelIGCLApi : IDisposable
    {
        #region 常數與欄位

        private const CtlResult CtlResultSuccess = CtlResult.CTL_RESULT_SUCCESS;

        private const uint CtlOperationTypeRead = 1;
        private const uint CtlOperationTypeWrite = 2;

        private const uint CtlAuxFlagNativeAux = 1 << 0;
        private const uint CtlAuxFlagI2CAux = 1 << 1;
        private const uint CtlAuxFlagI2CAuxMot = 1 << 2;

        private const uint CtlI2CFlag1ByteIndex = 1 << 0;

        private const uint CtlI2CFlag2ByteIndex = 2 << 0;

        private const int CtlAuxMaxDataSize = 0x0084; // 132 bytes

        private const int MaxI2cReadChunk = 0x04;  // 每次 I2C 讀取最大長度
        private const int MaxI2cWriteChunk = 0x04; // 每次 I2C 寫入最大資料 byte 數

        private const uint CtlInitAppVersion = 0x00010001;

        private IntPtr _apiHandle = IntPtr.Zero;
        private bool _disposed;
        private ushort _i2cDelayMs = 20; // 預設 I2C 操作之後等 20ms

        #endregion

        #region 公開屬性 / 建構 / 釋放

        public string Name =
            "Intel Graphics Control Library (IGCL)";

        public IntelIGCLApi()
        {
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

            CtlResult r;
            try
            {
                r = (CtlResult)NativeMethods.ctlInit(ref args, out _apiHandle);
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
                    $"Intel IGCL: ctlInit failed: {r.Describe()}");
            }
        }

        ~IntelIGCLApi()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_apiHandle != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.ctlClose(_apiHandle);
                }
                catch
                {
                    // ignore
                }
                _apiHandle = IntPtr.Zero;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("IntelIgcl");
        }

        #endregion

        #region 公開 API：設定 / 列舉顯示器

        /// <summary>設定 I2C 寫入後延遲（毫秒）。</summary>
        public void SetDelay(ushort milliseconds)
        {
            _i2cDelayMs = milliseconds;
        }

        /// <summary>
        /// 列舉目前 IGCL 所能看到的所有顯示輸出。
        /// 這邊會把「第幾張 GPU」和「第幾個輸出」記在 DisplayInfo 內，
        /// 同時也帶出 AdapterLUID / TargetId / VidPnSourceId，方便你辨識不同 GPU。
        /// </summary>
        public I2CAdapterInfo[] GetAvailableDisplays()
        {
            EnsureNotDisposed();
            if (_apiHandle == IntPtr.Zero)
                throw new InvalidOperationException("Intel IGCL: API handle is null.");

            var list = new List<I2CAdapterInfo>();

            // 1. Enumerate devices (GPU)
            uint devCount = 0;
            CtlResult r = (CtlResult)NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlEnumerateDevices(count) failed: {r.Describe()}");

            if (devCount == 0)
                return list.ToArray();

            var devs = new IntPtr[devCount];
            r = (CtlResult)NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, devs);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlEnumerateDevices(get) failed: {r.Describe()}");

            // 2. For each GPU (device)，enumerate display outputs
            for (int devIndex = 0; devIndex < devs.Length; devIndex++)
            {
                var dev = devs[devIndex];
                if (dev == IntPtr.Zero)
                    continue;

                uint outCount = 0;
                r = (CtlResult)NativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, null);
                if (r != CtlResultSuccess || outCount == 0)
                    continue;

                var outs = new IntPtr[outCount];
                r = (CtlResult)NativeMethods.ctlEnumerateDisplayOutputs(dev, ref outCount, outs);
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

                    r = (CtlResult)NativeMethods.ctlGetDisplayProperties(outHandle, ref props);
                    if (r != CtlResultSuccess)
                        continue;
                    bool isATTACHED = (props.DisplayConfigFlags & 1u << 1) != 0;
                    if (!isATTACHED)
                        continue;
                    var info = new I2CAdapterInfo
                    {
                        //DisplayHandle = outHandle,
                        DeviceIndex = devIndex,
                        OutputIndex = outIndex,
                        Name = $"Intel,裝置{devIndex}營幕{outIndex}",
                        IsNeedPrivilege = true,
                        IsFromDisplay = true,
                    };
                    list.Add(info);
                }
            }

            return list.ToArray();
        }

        #endregion

        #region 公開 API：DPCD (AUX)

        public byte[] ReadDpcd(I2CAdapterInfo display, uint addr, uint length)
        {
            EnsureNotDisposed();

            if (length == 0)
                throw new ArgumentException("DPCD read length must be greater than zero.", "Length");

            var result = new byte[length];
            uint remaining = length;
            uint offset = addr;
            int writePos = 0;

            while (remaining > 0)
            {
                uint chunk = remaining;
                if (chunk > CtlAuxMaxDataSize)
                    chunk = CtlAuxMaxDataSize;

                byte[] data = Inner_ReadDpcd(display, offset, (int)chunk);
                if (data == null || data.Length != (int)chunk)
                    throw new InvalidOperationException("Intel IGCL: DPCD read chunk size mismatch.");

                Buffer.BlockCopy(data, 0, result, writePos, (int)chunk);
                writePos += (int)chunk;
                offset += chunk;
                remaining -= chunk;
            }

            return result;
        }

        public void WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            uint offset = addr;
            int remaining = data.Length;
            int srcPos = 0;

            while (remaining > 0)
            {
                int chunk = remaining;
                if (chunk > CtlAuxMaxDataSize)
                    chunk = CtlAuxMaxDataSize;

                var payload = new byte[chunk];
                Buffer.BlockCopy(data, srcPos, payload, 0, chunk);

                Inner_WriteDpcd(display, offset, payload);

                offset += (uint)chunk;
                srcPos += chunk;
                remaining -= chunk;
            }
        }

        private IntPtr SelectDisplay(I2CAdapterInfo display)
        {
            // 1. Enumerate devices (GPU)
            uint devCount = 0;
            CtlResult r = (CtlResult)NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlEnumerateDevices(count) failed: {r.Describe()}");

            var devs = new IntPtr[devCount];
            r = (CtlResult)NativeMethods.ctlEnumerateDevices(_apiHandle, ref devCount, devs);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlEnumerateDevices(get) failed: {r.Describe()}");
            uint outCount = 0;
            r = (CtlResult)NativeMethods.ctlEnumerateDisplayOutputs(devs[display.DeviceIndex], ref outCount, null);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlEnumerateDisplayOutputs(get) failed: {r.Describe()}");
            var outs = new IntPtr[outCount];
            r = (CtlResult)NativeMethods.ctlEnumerateDisplayOutputs(devs[display.DeviceIndex], ref outCount, outs);
            return outs[display.OutputIndex];
        }

        private byte[] Inner_ReadDpcd(I2CAdapterInfo display, uint addr, int length)
        {
            if (length <= 0 || length > CtlAuxMaxDataSize)
                throw new ArgumentOutOfRangeException("Length",
                    string.Format("Intel IGCL: invalid DPCD length {0} (1..{1}).", length, CtlAuxMaxDataSize)
                  );

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeRead,
                Flags = CtlAuxFlagNativeAux,
                Address = addr,
                Rad = 0,
                PortId = 0,
                DataSize = (uint)length,
                Data = new byte[CtlAuxMaxDataSize]
            };

            CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);
            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(read) failed: {r.Describe()}");

            var buf = new byte[length];
            Array.Copy(args.Data, 0, buf, 0, length);
            return buf;
        }

        private void Inner_WriteDpcd(I2CAdapterInfo display, uint addr, byte[] data)
        {
            if (data == null || data.Length == 0 || data.Length > CtlAuxMaxDataSize)
                throw new ArgumentOutOfRangeException("Data", string.Format("Intel IGCL: invalid DPCD payload {0} (1..{}).", CtlAuxMaxDataSize)
                    );

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeWrite,
                Flags = CtlAuxFlagNativeAux,
                Address = addr,
                Rad = 0,
                PortId = 0,
                DataSize = (uint)data.Length,
                Data = new byte[CtlAuxMaxDataSize]
            };

            Array.Copy(data, 0, args.Data, 0, data.Length);

            CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);

            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(write) failed: {r.Describe()}");
        }

        #endregion

        #region I2C over AUX (using CtlAuxAccess)

        // 無 index，寫入單一 byte 到指定 I2C slave
        public void WriteI2CWithoutIndex(I2CAdapterInfo display, byte address, byte data)
        {
            EnsureNotDisposed();

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeWrite,
                Flags = CtlAuxFlagI2CAux,       // Last
                Address = address,                // I2C 7-bit address
                Rad = 0,
                PortId = 0,
                DataSize = 1,
                Data = new byte[CtlAuxMaxDataSize]
            };

            args.Data[0] = data;

            CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);

            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(I2C write) failed: {r.Describe()}");

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);
        }

        // 1-byte index，從 index 起連續寫入 data
        public void WriteI2CByteIndex(I2CAdapterInfo display, byte address, byte index, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            // 可用資料空間 = CtlAuxMaxDataSize - 1 (index 佔 1 byte)
            const int PayloadMax = MaxI2cWriteChunk;

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int chunkLen = Math.Min(PayloadMax, remaining);
                int totalLen = 1 + chunkLen;              // index + chunk

                byte effectiveIndex = (byte)(index + offset);

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAux,            // 每一段都是獨立 write，直接當作 last
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)totalLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                args.Data[0] = effectiveIndex;
                Array.Copy(data, offset, args.Data, 1, chunkLen);

                CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);

                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write byte-index) failed: {r.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += chunkLen;
            }
        }

        // 16-bit index（大端），從 index 起連續寫入 data
        public void WriteI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, byte[] data)
        {
            EnsureNotDisposed();

            if (data == null || data.Length == 0)
                return;

            // 可用資料空間 = CtlAuxMaxDataSize - 2 (index 佔 2 bytes)
            const int PayloadMax = MaxI2cWriteChunk;

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            int offset = 0;
            while (offset < data.Length)
            {
                int remaining = data.Length - offset;
                int chunkLen = Math.Min(PayloadMax, remaining);
                int totalLen = 2 + chunkLen;              // 2 bytes index + chunk

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAux,            // 每一段都是獨立 write
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)totalLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                args.Data[0] = hi;
                args.Data[1] = lo;
                Array.Copy(data, offset, args.Data, 2, chunkLen);

                CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);

                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write UInt16-index) failed: {r.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                offset += chunkLen;
            }
        }

        // 無 index，讀取單一 byte
        public byte ReadI2CWithoutIndex(I2CAdapterInfo display, byte address)
        {
            EnsureNotDisposed();

            var args = new CtlAuxAccessArgs
            {
                Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                Version = 0,
                Reserved = new byte[3],
                OpType = CtlOperationTypeRead,
                Flags = CtlAuxFlagI2CAux,        // Last
                Address = address,
                Rad = 0,
                PortId = 0,
                DataSize = 1,
                Data = new byte[CtlAuxMaxDataSize]
            };

            CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(SelectDisplay(display), ref args);

            if (r != CtlResultSuccess)
                throw new InvalidOperationException(
                    $"Intel IGCL: ctlAUXAccess(I2C read) failed: {r.Describe()}");

            if (_i2cDelayMs != 0)
                Thread.Sleep(_i2cDelayMs);

            return args.Data[0];
        }

        // 1-byte index，從 index 起連續讀取 length 個 byte
        public byte[] ReadI2CByteIndex(I2CAdapterInfo display, byte address, byte index, int length)
        {
            IntPtr deviceHandle = SelectDisplay(display);
            EnsureNotDisposed();

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // 1) 先送出 index，使用 MOT（非最後一包）
            {
                var argsIndex = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAuxMot, // NOT last → MOT
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = 1,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                argsIndex.Data[0] = index;

                CtlResult rIndex = (CtlResult)NativeMethods.ctlAUXAccess(deviceHandle, ref argsIndex);

                if (rIndex != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write index) failed: {rIndex.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);
            }

            // 2) 再分段讀資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int chunkLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + chunkLen >= length;

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeRead,
                    Flags = isLast ? CtlAuxFlagI2CAux : CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)chunkLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(deviceHandle, ref args);

                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C read byte-index) failed: {r.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(args.Data, 0, result, offset, chunkLen);
                offset += chunkLen;
            }

            return result;
        }

        // 16-bit index（大端），從 index 起連續讀取 length 個 byte
        public byte[] ReadI2CUInt16Index(I2CAdapterInfo display, byte address, ushort index, int length)
        {
            IntPtr deviceHandle = SelectDisplay(display);
            EnsureNotDisposed();

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            var result = new byte[length];

            // big-endian index
            byte hi = (byte)(index >> 8);
            byte lo = (byte)(index & 0xFF);

            // 1) 先送出 16-bit index，用 MOT
            {
                var argsIndex = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeWrite,
                    Flags = CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = 2,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                argsIndex.Data[0] = hi;
                argsIndex.Data[1] = lo;

                CtlResult rIndex = (CtlResult)NativeMethods.ctlAUXAccess(deviceHandle, ref argsIndex);

                if (rIndex != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C write UInt16-index) failed: {rIndex.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);
            }

            // 2) 再分段讀資料
            const int ChunkSize = MaxI2cReadChunk;
            int offset = 0;

            while (offset < length)
            {
                int remaining = length - offset;
                int chunkLen = Math.Min(ChunkSize, remaining);
                bool isLast = offset + chunkLen >= length;

                var args = new CtlAuxAccessArgs
                {
                    Size = (uint)Marshal.SizeOf(typeof(CtlAuxAccessArgs)),
                    Version = 0,
                    Reserved = new byte[3],
                    OpType = CtlOperationTypeRead,
                    Flags = isLast ? CtlAuxFlagI2CAux : CtlAuxFlagI2CAuxMot,
                    Address = address,
                    Rad = 0,
                    PortId = 0,
                    DataSize = (uint)chunkLen,
                    Data = new byte[CtlAuxMaxDataSize]
                };

                CtlResult r = (CtlResult)NativeMethods.ctlAUXAccess(deviceHandle, ref args);

                if (r != CtlResultSuccess)
                    throw new InvalidOperationException(
                        $"Intel IGCL: ctlAUXAccess(I2C read UInt16-index) failed: {r.Describe()}");

                if (_i2cDelayMs != 0)
                    Thread.Sleep(_i2cDelayMs);

                Array.Copy(args.Data, 0, result, offset, chunkLen);
                offset += chunkLen;
            }

            return result;
        }

        #endregion


        #region Interop 結構 & P/Invoke

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


        [StructLayout(LayoutKind.Sequential)]
        private struct CtlAuxAccessArgs
        {
            public uint Size;
            public byte Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;

            public uint OpType;
            public uint Flags;
            public uint Address;
            public ulong Rad;
            public uint PortId;
            public uint DataSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CtlAuxMaxDataSize)]
            public byte[] Data;
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


        private static class NativeMethods
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
                [In, Out] IntPtr[]? devices);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlEnumerateDisplayOutputs(
                IntPtr deviceHandle,
                ref uint count,
                [In, Out] IntPtr[]? outputs);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlGetDisplayProperties(
                IntPtr outputHandle,
                ref CtlDisplayProperties props);

            [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
            public static extern uint ctlAUXAccess(
                IntPtr outputHandle,
                ref CtlAuxAccessArgs args);

        }

        #endregion
    }
}
