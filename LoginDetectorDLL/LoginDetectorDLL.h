// LoginDetectorDLL.h
#pragma once

#ifdef LOGINDETECTOR_EXPORTS
#define LOGINDETECTOR_API __declspec(dllexport)
#else
#define LOGINDETECTOR_API __declspec(dllimport)
#endif

#include <string>
#include <vector>

// Data structures to be returned
struct LOGINDETECTOR_API DetectedField {
    std::string type;
    int x, y, width, height;
    std::string content; // For password fields, this contains "Password field: X dots"
};

struct LOGINDETECTOR_API DetectionResult {
    bool isLoginPage;
    double confidence;
    std::vector<DetectedField> fields;
    std::vector<std::string> errors;
    double executionTimeMs;
};

// DLL exported functions
extern "C" {
    // Main function to detect login page in an image
    LOGINDETECTOR_API DetectionResult* DetectLoginPage(const char* imagePath, double confidenceThreshold = 0.6);

    // Free the detection result memory (must be called after processing the result)
    LOGINDETECTOR_API void FreeDetectionResult(DetectionResult* result);
}