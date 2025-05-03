#pragma once
#include "ILoginDetector.h"
#include <set>

class KeywordBasedLoginDetector : public ILoginDetector {
private:
    std::vector<std::string> loginKeywords;
    std::vector<std::string> strongKeywords;

    // Function to count keyword occurrences in text
    int countKeywordOccurrences(const std::string& text, const std::vector<std::string>& keywords) const;

public:
    KeywordBasedLoginDetector();
    double calculateConfidence(
        const std::vector<FormField>& formFields,
        const std::string& pageText,
        std::map<std::string, double>& confidenceFactors) const override;
};
