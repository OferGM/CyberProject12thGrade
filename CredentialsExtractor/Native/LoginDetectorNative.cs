// LoginDetectorNative.cs
using System.Runtime.InteropServices;

namespace CredentialsExtractor.Native
{
    // Native structures that match the C++ DLL definitions
    [StructLayout(LayoutKind.Sequential)]
    public struct DetectedField
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string Type;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        [MarshalAs(UnmanagedType.LPStr)]
        public string Content;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DetectionResult
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool IsLoginPage;
        public double Confidence;

        // Arrays represented as pointer + count
        public IntPtr Fields;     // Pointer to DetectedField array
        public int FieldCount;    // Number of fields

        public IntPtr Errors;     // Pointer to string array
        public int ErrorCount;    // Number of errors

        public double ExecutionTimeMs;
    }

    // Class to handle P/Invoke calls to the LoginDetector DLL
    public static class LoginDetectorNative
    {
        // Use DLL name only - it will be searched in application directory
        private const string DllName = "LoginDetectorDLL.dll";

        // Define function delegates that match the exported functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr DetectLoginPageDelegate(string imagePath, double confidenceThreshold);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FreeDetectionResultDelegate(IntPtr result);

        // Function pointers
        private static DetectLoginPageDelegate _detectLoginPage;
        private static FreeDetectionResultDelegate _freeDetectionResult;
        private static IntPtr _dllHandle = IntPtr.Zero;

        // Windows API functions for dynamic DLL loading
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static bool Initialize(string dllPath)
        {
            try
            {
                // If already initialized, clean up first
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                }

                // Load the DLL
                _dllHandle = LoadLibrary(dllPath);
                if (_dllHandle == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new DllNotFoundException($"Failed to load LoginDetector DLL. Error code: {errorCode}");
                }

                // Get function addresses
                IntPtr detectLoginPageAddr = GetProcAddress(_dllHandle, "DetectLoginPage");
                IntPtr freeDetectionResultAddr = GetProcAddress(_dllHandle, "FreeDetectionResult");

                if (detectLoginPageAddr == IntPtr.Zero || freeDetectionResultAddr == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new EntryPointNotFoundException($"Failed to find DLL functions. Error code: {errorCode}");
                }

                // Create delegates
                _detectLoginPage = Marshal.GetDelegateForFunctionPointer<DetectLoginPageDelegate>(detectLoginPageAddr);
                _freeDetectionResult = Marshal.GetDelegateForFunctionPointer<FreeDetectionResultDelegate>(freeDetectionResultAddr);

                return true;
            }
            catch (Exception)
            {
                // Clean up on failure
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                }
                throw;
            }
        }

        public static void Cleanup()
        {
            if (_dllHandle != IntPtr.Zero)
            {
                FreeLibrary(_dllHandle);
                _dllHandle = IntPtr.Zero;
                _detectLoginPage = null;
                _freeDetectionResult = null;
            }
        }

        public static IntPtr DetectLoginPage(string imagePath, double confidenceThreshold = 0.6)
        {
            if (_detectLoginPage == null)
                throw new InvalidOperationException("LoginDetector DLL not initialized");

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            return _detectLoginPage(imagePath, confidenceThreshold);
        }

        public static void FreeDetectionResult(IntPtr result)
        {
            if (_freeDetectionResult == null)
                throw new InvalidOperationException("LoginDetector DLL not initialized");

            if (result != IntPtr.Zero)
                _freeDetectionResult(result);
        }

        // Helper method to safely extract detection result
        public static DetectionResult? ExtractDetectionResult(IntPtr resultPtr)
        {
            if (resultPtr == IntPtr.Zero)
                return null;

            // Marshal the base structure
            DetectionResult result = Marshal.PtrToStructure<DetectionResult>(resultPtr);
            return result;
        }

        // Helper to read fields from the result
        public static List<DetectedField> ExtractFields(IntPtr fieldsPtr, int fieldCount)
        {
            var fields = new List<DetectedField>();
            if (fieldsPtr == IntPtr.Zero || fieldCount <= 0)
                return fields;

            // Calculate size of each field structure for pointer arithmetic
            int structSize = Marshal.SizeOf<DetectedField>();

            for (int i = 0; i < fieldCount; i++)
            {
                // Calculate offset for current field
                IntPtr currentFieldPtr = IntPtr.Add(fieldsPtr, i * structSize);

                // Marshal the structure
                DetectedField field = Marshal.PtrToStructure<DetectedField>(currentFieldPtr);
                fields.Add(field);
            }

            return fields;
        }

        // Helper to read error strings
        public static List<string> ExtractErrors(IntPtr errorsPtr, int errorCount)
        {
            var errors = new List<string>();
            if (errorsPtr == IntPtr.Zero || errorCount <= 0)
                return errors;

            // The errors are stored as an array of char pointers (const char**)
            for (int i = 0; i < errorCount; i++)
            {
                // Get the pointer to the string
                IntPtr strPtr = Marshal.ReadIntPtr(errorsPtr, i * IntPtr.Size);
                if (strPtr != IntPtr.Zero)
                {
                    // Convert to C# string
                    string error = Marshal.PtrToStringAnsi(strPtr);
                    if (!string.IsNullOrEmpty(error))
                        errors.Add(error);
                }
            }

            return errors;
        }
    }
}