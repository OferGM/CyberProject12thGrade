// FormField.h
#pragma once
#include <string>
#include <opencv2/core/types.hpp>

// A structure to represent a detected form field
struct FormField {
    std::string type;     // The type of field (Username, Password, etc.)
    cv::Rect position;    // The position of the field in the image
    std::string text;     // The text content (for password fields, this contains "Password field: X dots")
};