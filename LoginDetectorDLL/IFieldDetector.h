// IFieldDetector.h
#pragma once
#include <vector>
#include "FormField.h"
#include <opencv2/core/mat.hpp>

// Interface for field detection strategies
class IFieldDetector {
public:
    virtual ~IFieldDetector() = default;
    virtual bool isFormField(const std::string& text, const cv::Rect& rect, const cv::Mat& image) const = 0;
    virtual std::pair<std::string, cv::Scalar> getFieldType(const std::string& text) const = 0;
};