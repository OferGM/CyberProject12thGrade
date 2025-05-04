#include "LoginPageDetector.h"
#include "KeywordBasedFieldDetector.h"
#include "KeywordBasedLoginDetector.h"
#include "ContourBasedPasswordDetector.h"
#include "TesseractOcrService.h"
#include <opencv2/imgproc.hpp>
#include <opencv2/imgcodecs.hpp>
#include <chrono>

LoginPageDetector::LoginPageDetector(double threshold) : confidenceThreshold(threshold) {
    // Initialize dependencies (following Dependency Inversion Principle)
    fieldDetector = std::make_unique<KeywordBasedFieldDetector>();
    loginDetector = std::make_unique<KeywordBasedLoginDetector>();
    passwordDetector = std::make_unique<ContourBasedPasswordDetector>();
    ocrService = std::make_unique<TesseractOcrService>();

    // Initialize OCR service
    ocrService->initialize();
}

LoginPageDetector::~LoginPageDetector() {
    if (ocrService) {
        ocrService->cleanup();
    }
}

std::vector<FormField> LoginPageDetector::detectFormFields(const cv::Mat& image) {
    std::vector<FormField> formFields;

    // Convert to grayscale
    cv::Mat gray;
    cv::cvtColor(image, gray, cv::COLOR_BGR2GRAY);

    // Apply Gaussian blur
    cv::Mat blurred;
    cv::GaussianBlur(gray, blurred, cv::Size(5, 5), 0);

    // Apply adaptive thresholding
    cv::Mat thresholded;
    cv::adaptiveThreshold(blurred, thresholded, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
        cv::THRESH_BINARY_INV, 11, 4);

    // Apply Canny edge detection
    cv::Mat edged;
    cv::Canny(thresholded, edged, 30, 200);

    // Find contours
    std::vector<std::vector<cv::Point>> contours;
    std::vector<cv::Vec4i> hierarchy;
    cv::findContours(thresholded.clone(), contours, hierarchy, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

    // Process each contour
    for (const auto& contour : contours) {
        // Calculate perimeter
        double peri = cv::arcLength(contour, true);

        // Approximate the contour
        std::vector<cv::Point> approx;
        cv::approxPolyDP(contour, approx, 0.02 * peri, true);

        // Check if the approximated contour resembles a rectangle (4-8 points)
        if (approx.size() >= 4 && approx.size() <= 8) {
            // Get bounding rectangle
            cv::Rect rect = cv::boundingRect(contour);

            // Skip very small contours
            if (rect.width < 50 || rect.height < 10) {
                continue;
            }

            // Skip very large contours (likely page sections, not form fields)
            if (rect.width > gray.cols * 0.9 || rect.height > gray.rows * 0.2) {
                continue;
            }

            // Skip if aspect ratio indicates a button
            float ratio = static_cast<float>(rect.width) / rect.height;
            if (ratio < 2.0 && rect.width < 150 && rect.height >= 25) {
                // Likely a button, not a form field
                continue;
            }

            int roi_x = std::max(0, rect.x - 50);
            int roi_y = std::max(0, rect.y - 30);
            int roi_width = std::min(gray.cols - roi_x, rect.width + 100);
            int roi_height = std::min(gray.rows - roi_y, rect.height + 40);


            // Add additional validation before creating the ROI rectangle
            if (roi_width <= 0 || roi_height <= 0) {
                // Skip this contour if creating a valid ROI is impossible
                continue;
            }

            // Create ROI rectangle with validated dimensions
            cv::Rect roiRect(roi_x, roi_y, roi_width, roi_height);

            // Ensure the ROI is within image bounds
            if (roiRect.width > 0 && roiRect.height > 0) {
                // Perform OCR on the ROI
                std::string text = ocrService->extractTextFromRegion(image, roiRect);

                // Check if this is a form field I'm interested in
                if (fieldDetector->isFormField(text, rect, image)) {
                    // Get field type and color
                    auto [fieldType, _] = fieldDetector->getFieldType(text);

                    // Skip if identified as "Not a Field"
                    if (fieldType == "Not a Field") {
                        continue;
                    }

                    // Extract text or count dots based on field type
                    std::string fieldContent;
                    if (fieldType == "Password") {
                        // Get dot count for password field
                        int dotCount = passwordDetector->detectPasswordDots(image(rect));
                        fieldContent = "Password field: " + std::to_string(dotCount) + " dots";
                    }
                    else {
                        // For other fields, extract the text
                        fieldContent = ocrService->extractTextFromRegion(image, rect);
                    }

                    // Store the detected field
                    FormField field;
                    field.type = fieldType;
                    field.position = rect;
                    field.text = fieldContent;
                    formFields.push_back(field);
                }
            }
        }
    }

    return formFields;
}

bool LoginPageDetector::isLoginPage(const cv::Mat& image, const std::vector<FormField>& fields,
    std::map<std::string, double>& confidenceFactors, double& confidence) {
    // Extract all text from the image
    std::string allPageText = ocrService->extractText(image);

    // Calculate login page confidence
    confidence = loginDetector->calculateConfidence(fields, allPageText, confidenceFactors);

    // Return true if confidence exceeds threshold
    return (confidence >= confidenceThreshold);
}

InternalDetectionResult LoginPageDetector::processAndAnalyze(const std::string& imagePath) {
    InternalDetectionResult result;
    result.isLoginPage = false;
    result.confidence = 0.0;

    auto startTime = std::chrono::high_resolution_clock::now();

    try {
        // Load the image
        cv::Mat image = cv::imread(imagePath);
        if (image.empty()) {
            result.errors.push_back("Error: Unable to read image from " + imagePath);
            return result;
        }

        // Detect form fields
        std::vector<FormField> formFields = detectFormFields(image);

        // Check if it's a login page
        std::map<std::string, double> confidenceFactors;
        double confidence;
        bool isLoginPage = this->isLoginPage(image, formFields, confidenceFactors, confidence);

        // Fill the result
        result.isLoginPage = isLoginPage;
        result.confidence = confidence;
        result.fields = formFields;
    }
    catch (const std::exception& e) {
        result.errors.push_back(std::string("Exception: ") + e.what());
    }

    auto endTime = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(endTime - startTime);
    result.executionTimeMs = duration.count();

    return result;
}