#include "OpenCVImageProcessor.h"
#include <opencv2/imgproc.hpp>

cv::Mat OpenCVImageProcessor::preprocess(const cv::Mat& image) {
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

    return thresholded;
}

cv::Mat OpenCVImageProcessor::createVisualization(const cv::Mat& original, const cv::Mat& processed,
    const cv::Mat& edges, const cv::Mat& result,
    bool isLoginPage, double confidence) {
    const int panelWidth = 800;
    const int panelHeight = 600;

    // Create a 2x2 grid for visualization
    cv::Mat visualizationImg(panelHeight * 2, panelWidth * 2, CV_8UC3, cv::Scalar(255, 255, 255));

    // Resize images for display
    cv::Mat displayOriginal, displayThresholded, displayEdged, displayResult;
    cv::resize(original, displayOriginal, cv::Size(panelWidth, panelHeight));

    // Convert single-channel images to 3-channel for display
    cv::Mat thresholdedColor, edgedColor;
    cv::cvtColor(processed, thresholdedColor, cv::COLOR_GRAY2BGR);
    cv::cvtColor(edges, edgedColor, cv::COLOR_GRAY2BGR);

    cv::resize(thresholdedColor, displayThresholded, cv::Size(panelWidth, panelHeight));
    cv::resize(edgedColor, displayEdged, cv::Size(panelWidth, panelHeight));
    cv::resize(result, displayResult, cv::Size(panelWidth, panelHeight));

    // Place images in the grid
    displayOriginal.copyTo(visualizationImg(cv::Rect(0, 0, panelWidth, panelHeight)));
    displayThresholded.copyTo(visualizationImg(cv::Rect(panelWidth, 0, panelWidth, panelHeight)));
    displayEdged.copyTo(visualizationImg(cv::Rect(0, panelHeight, panelWidth, panelHeight)));
    displayResult.copyTo(visualizationImg(cv::Rect(panelWidth, panelHeight, panelWidth, panelHeight)));

    // Add titles
    cv::putText(visualizationImg, "Original Image", cv::Point(10, 30),
        cv::FONT_HERSHEY_SIMPLEX, 1, cv::Scalar(0, 0, 0), 2);
    cv::putText(visualizationImg, "Thresholded", cv::Point(panelWidth + 10, 30),
        cv::FONT_HERSHEY_SIMPLEX, 1, cv::Scalar(0, 0, 0), 2);
    cv::putText(visualizationImg, "Edge Detection", cv::Point(10, panelHeight + 30),
        cv::FONT_HERSHEY_SIMPLEX, 1, cv::Scalar(0, 0, 0), 2);
    cv::putText(visualizationImg, "Detected Form Fields", cv::Point(panelWidth + 10, panelHeight + 30),
        cv::FONT_HERSHEY_SIMPLEX, 1, cv::Scalar(0, 0, 0), 2);

    return visualizationImg;
}
