// OpenCVImageProcessor.h
#pragma once
#include "IImageProcessor.h"

class OpenCVImageProcessor : public IImageProcessor {
public:
    cv::Mat preprocess(const cv::Mat& image) override;
    cv::Mat createVisualization(const cv::Mat& original, const cv::Mat& processed,
        const cv::Mat& edges, const cv::Mat& result,
        bool isLoginPage, double confidence) override;
};