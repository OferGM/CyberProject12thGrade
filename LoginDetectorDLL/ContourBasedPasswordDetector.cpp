#include "ContourBasedPasswordDetector.h"
#include <opencv2/imgproc.hpp>

int ContourBasedPasswordDetector::detectPasswordDots(const cv::Mat& fieldImage) const {
    // Convert to grayscale if needed
    cv::Mat grayField;
    if (fieldImage.channels() == 3) {
        cv::cvtColor(fieldImage, grayField, cv::COLOR_BGR2GRAY);
    }
    else {
        grayField = fieldImage.clone();
    }

    // Check if this is a dark-themed password field
    cv::Scalar meanColor = cv::mean(fieldImage);
    double avgBrightness = 0;

    // Handle both grayscale and color images
    if (fieldImage.channels() == 1) {
        avgBrightness = meanColor[0];
    }
    else {
        avgBrightness = (meanColor[0] + meanColor[1] + meanColor[2]) / 3.0;
    }

    bool isDarkThemed = (avgBrightness < 128);

    // Use adaptive thresholding to handle varied lighting conditions
    cv::Mat adaptiveThresh;
    if (isDarkThemed) {
        // For dark backgrounds, use adaptive threshold to find light dots
        cv::adaptiveThreshold(grayField, adaptiveThresh, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
            cv::THRESH_BINARY, 11, -3);
    }
    else {
        // For light backgrounds, use adaptive threshold to find dark dots
        cv::adaptiveThreshold(grayField, adaptiveThresh, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
            cv::THRESH_BINARY_INV, 11, 3);
    }

    // Clean up with morphological operations
    cv::Mat kernel = cv::getStructuringElement(cv::MORPH_ELLIPSE, cv::Size(3, 3));
    cv::morphologyEx(adaptiveThresh, adaptiveThresh, cv::MORPH_OPEN, kernel);

    // Find contours in the thresholded image
    std::vector<std::vector<cv::Point>> contours;
    cv::findContours(adaptiveThresh, contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

    // Filter contours by various properties to identify dots
    int contourCount = 0;
    for (const auto& contour : contours) {
        double area = cv::contourArea(contour);
        if (area < 2 || area > 300) continue; // Skip too small or too large

        cv::Rect boundRect = cv::boundingRect(contour);
        float aspect = static_cast<float>(boundRect.width) / boundRect.height;

        // Skip non-dot-like shapes (dots should be roughly circular)
        if (aspect > 3.0 || aspect < 0.33) continue;

        // A valid dot was found
        contourCount++;
    }

    return contourCount;
}