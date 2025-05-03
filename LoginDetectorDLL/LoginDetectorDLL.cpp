#include "LoginDetectorDLL.h"
#include "LoginPageDetector.h"
#include <new>

// Global instance of the detector
static std::unique_ptr<LoginPageDetector> g_detector = nullptr;

extern "C" {
    LOGINDETECTOR_API DetectionResult* DetectLoginPage(const char* imagePath, double confidenceThreshold) {
        // Create detector if it doesn't exist
        if (!g_detector) {
            g_detector = std::make_unique<LoginPageDetector>(confidenceThreshold);
        }

        // Allocate a new result structure
        DetectionResult* result = new (std::nothrow) DetectionResult();
        if (!result) {
            return nullptr;
        }

        // Process the image
        *result = g_detector->processAndAnalyze(imagePath);

        return result;
    }

    LOGINDETECTOR_API void FreeDetectionResult(DetectionResult* result) {
        if (result) {
            delete result;
        }
    }
}