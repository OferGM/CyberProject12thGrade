#pragma once
#include <opencv2/core/mat.hpp>

// Interface for password field detection
class IPasswordFieldDetector {
public:
    virtual ~IPasswordFieldDetector() = default;
    virtual int detectPasswordDots(const cv::Mat& fieldImage) const = 0;
};