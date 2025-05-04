#include "LoginDetectorDLL.h"
#include "LoginPageDetector.h"
#include <new>
#include <cstring> // For string operations
#include <memory>
#include <string>

// Global instance of the detector
static std::unique_ptr<LoginPageDetector> g_detector = nullptr;

// Helper function to safely copy strings
char* createCStringCopy(const std::string& source) {
    if (source.empty()) {
        char* emptyStr = new char[1];
        emptyStr[0] = '\0';
        return emptyStr;
    }

    char* result = new char[source.size() + 1];
#ifdef _WIN32
    strcpy_s(result, source.size() + 1, source.c_str());
#else
    strcpy(result, source.c_str());
#endif
    return result;
}

extern "C" {
    LOGINDETECTOR_API DetectionResult* DetectLoginPage(const char* imagePath, double confidenceThreshold) {
        // Create detector if it doesn't exist
        if (!g_detector) {
            g_detector = std::make_unique<LoginPageDetector>(confidenceThreshold);
        }

        // Allocate a new result structure
        DetectionResult* result = new (std::nothrow) DetectionResult{};
        if (!result) {
            return nullptr;
        }

        // Zero initialize everything
        result->isLoginPage = false;
        result->confidence = 0.0;
        result->executionTimeMs = 0.0;
        result->fields = nullptr;
        result->fieldCount = 0;
        result->errors = nullptr;
        result->errorCount = 0;

        try {
            // Process the image and get the C++ result
            auto internalResult = g_detector->processAndAnalyze(imagePath);

            // Copy basic fields
            result->isLoginPage = internalResult.isLoginPage;
            result->confidence = internalResult.confidence;
            result->executionTimeMs = internalResult.executionTimeMs;

            // Handle fields vector - convert to C array
            if (!internalResult.fields.empty()) {
                // Allocate memory for field array
                result->fieldCount = static_cast<int>(internalResult.fields.size());
                result->fields = new DetectedField[result->fieldCount]();

                // Copy each field
                for (int i = 0; i < result->fieldCount; i++) {
                    // Allocate and copy strings
                    result->fields[i].type = createCStringCopy(internalResult.fields[i].type);
                    result->fields[i].content = createCStringCopy(internalResult.fields[i].text);

                    // Copy numerical values
                    result->fields[i].x = internalResult.fields[i].position.x;
                    result->fields[i].y = internalResult.fields[i].position.y;
                    result->fields[i].width = internalResult.fields[i].position.width;
                    result->fields[i].height = internalResult.fields[i].position.height;
                }
            }

            // Handle errors vector - convert to C array
            if (!internalResult.errors.empty()) {
                // Allocate array of char* pointers
                result->errorCount = static_cast<int>(internalResult.errors.size());
                result->errors = new const char* [result->errorCount]();

                // Copy each error string
                for (int i = 0; i < result->errorCount; i++) {
                    result->errors[i] = createCStringCopy(internalResult.errors[i]);
                }
            }
        }
        catch (const std::exception& e) {
            // Add the exception message to errors
            try {
                result->errorCount = 1;
                result->errors = new const char* [1]();
                result->errors[0] = createCStringCopy(e.what());
                result->isLoginPage = false;
                result->confidence = 0.0;
            }
            catch (...) {
                // If error handling fails, set basic failure state
                result->errors = nullptr;
                result->errorCount = 0;
                result->isLoginPage = false;
                result->confidence = 0.0;
            }
        }

        return result;
    }

    LOGINDETECTOR_API void FreeDetectionResult(DetectionResult* result) {
        if (result) {
            // Free fields array
            if (result->fields && result->fieldCount > 0) {
                for (int i = 0; i < result->fieldCount; i++) {
                    // Free strings allocated for each field
                    delete[] result->fields[i].type;
                    delete[] result->fields[i].content;
                }
                delete[] result->fields;
            }

            // Free errors array
            if (result->errors && result->errorCount > 0) {
                // Free each string then the array
                for (int i = 0; i < result->errorCount; i++) {
                    delete[] const_cast<char*>(result->errors[i]);
                }
                delete[] result->errors;
            }

            // Delete the result struct
            delete result;
        }
    }
}