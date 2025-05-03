#pragma once
#include <opencv2/core/mat.hpp>
#include <string>

// Interface for image processing operations
class IImageProcessor {
public:
    virtual ~IImageProcessor() = default;
    virtual cv::Mat preprocess(const cv::Mat& image) = 0;
    virtual cv::Mat createVisualization(const cv::Mat& original, const cv::Mat& processed,
        const cv::Mat& edges, const cv::Mat& result,
        bool isLoginPage, double confidence) = 0;
};