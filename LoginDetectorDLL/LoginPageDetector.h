#pragma once
#include <string>
#include <vector>
#include <map>
#include "FormField.h"
#include "IFieldDetector.h"
#include "ILoginDetector.h"
#include "IPasswordFieldDetector.h"
#include "IOcrService.h"
#include <opencv2/core/mat.hpp>
#include <memory>

// Internal detection result structure (distinct from DLL interface)
struct InternalDetectionResult {
    bool isLoginPage;
    double confidence;
    std::vector<FormField> fields;
    std::vector<std::string> errors;
    double executionTimeMs;
};

class LoginPageDetector {
private:
    std::unique_ptr<IFieldDetector> fieldDetector;
    std::unique_ptr<ILoginDetector> loginDetector;
    std::unique_ptr<IPasswordFieldDetector> passwordDetector;
    std::unique_ptr<IOcrService> ocrService;
    double confidenceThreshold;

public:
    LoginPageDetector(double threshold = 0.6);
    ~LoginPageDetector();

    std::vector<FormField> detectFormFields(const cv::Mat& image);

    bool isLoginPage(const cv::Mat& image, const std::vector<FormField>& fields,
        std::map<std::string, double>& confidenceFactors, double& confidence);

    InternalDetectionResult processAndAnalyze(const std::string& imagePath);
};