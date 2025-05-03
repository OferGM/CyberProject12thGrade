#pragma once
#pragma once
#include <vector>
#include <map>
#include "FormField.h"

// Interface for login page detection
class ILoginDetector {
public:
    virtual ~ILoginDetector() = default;
    virtual double calculateConfidence(
        const std::vector<FormField>& formFields,
        const std::string& pageText,
        std::map<std::string, double>& confidenceFactors) const = 0;
};