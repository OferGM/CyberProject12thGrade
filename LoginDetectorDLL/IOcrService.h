// IOcrService.h
#pragma once
#include <string>
#include <opencv2/core/mat.hpp>

// Interface for OCR operations
class IOcrService {
public:
    virtual ~IOcrService() = default;
    virtual std::string extractText(const cv::Mat& image) = 0;
    virtual std::string extractTextFromRegion(const cv::Mat& image, const cv::Rect& region) = 0;
    virtual void initialize() = 0;
    virtual void cleanup() = 0;
};