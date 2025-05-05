// LoginDetectorDLL.h
#pragma once

#ifdef LOGINDETECTOR_EXPORTS
#define LOGINDETECTOR_API __declspec(dllexport)
#else
#define LOGINDETECTOR_API __declspec(dllimport)
#endif

// C-compatible data structures for interop
extern "C" {
    // Field structure with plain C types
    struct LOGINDETECTOR_API DetectedField {
        const char* type;        // Field type (allocated string)
        int x;                   // X position
        int y;                   // Y position 
        int width;               // Width
        int height;              // Height
        const char* content;     // Field content (allocated string)
    };

    // Result structure with plain C types
    struct LOGINDETECTOR_API DetectionResult {
        bool isLoginPage;            // Whether this is a login page
        double confidence;           // Detection confidence (0.0 - 1.0)

        DetectedField* fields;       // Array of detected fields (owned by DLL)
        int fieldCount;              // Number of fields in the array

        const char** errors;         // Array of error strings (owned by DLL)
        int errorCount;              // Number of errors

        double executionTimeMs;      // Execution time in milliseconds
    };

    // Main function to detect login page in an image
    LOGINDETECTOR_API DetectionResult* DetectLoginPage(const char* imagePath, double confidenceThreshold = 0.6);

    // Free the detection result memory (must be called after processing the result)
    LOGINDETECTOR_API void FreeDetectionResult(DetectionResult* result);
}