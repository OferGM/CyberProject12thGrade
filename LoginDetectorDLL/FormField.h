// FormField.h
#pragma once
#include <string>
#include <opencv2/core/types.hpp>

// A structure to represent a detected form field
struct FormField {
    std::string type;
    cv::Rect position;
    std::string text;
};