// KeywordBasedFieldDetector.h
#pragma once
#include "IFieldDetector.h"
#include <algorithm>
#include <vector>

class KeywordBasedFieldDetector : public IFieldDetector {
private:
    std::vector<std::string> usernameKeywords;
    std::vector<std::string> emailKeywords;
    std::vector<std::string> phoneKeywords;
    std::vector<std::string> nameKeywords;
    std::vector<std::string> passwordKeywords;
    std::vector<std::string> excludeKeywords;
    std::vector<std::string> allKeywords;

    // I check if text contains any of the specified keywords
    bool containsKeyword(const std::string& text, const std::vector<std::string>& keywords) const;

public:
    KeywordBasedFieldDetector();
    bool isFormField(const std::string& text, const cv::Rect& rect, const cv::Mat& image) const override;
    std::pair<std::string, cv::Scalar> getFieldType(const std::string& text) const override;
};
