// KeywordBasedFieldDetector.cpp
#include "KeywordBasedFieldDetector.h"
#include <opencv2/imgproc.hpp>

KeywordBasedFieldDetector::KeywordBasedFieldDetector() {
    // Initialize keywords for different field types
    usernameKeywords = { "username", "user name", "user id", "id" };
    emailKeywords = { "email", "e-mail", "mail", "gmail", "email address", "email or mobile phone number" };
    phoneKeywords = { "phone", "mobile", "cell", "telephone", "mobile phone number" };
    nameKeywords = { "name", "first name", "last name", "full name" };
    passwordKeywords = { "password", "pass", "pwd", "passcode" };
    creditKeywords = {"card number", "expiration date", "security code", "payment method"};

    // Keywords that indicate elements I want to exclude (not actual input fields)
    excludeKeywords = {
        "forgot", "forgot password", "remember me", "sign up", "register", "submit", "login with",
        "sign in with", "continue with", "terms", "privacy", "policy", "help",
        "support", "cancel", "reset", "captcha", "verification", "recover",
        "facebook", "google", "apple", "twitter", "oauth", "create account",
        "new user", "checkbox", "button", "click here", "here", "agree", "manage",
        "continue", "login"
    };

    // Combine all field type keywords
    allKeywords.insert(allKeywords.end(), usernameKeywords.begin(), usernameKeywords.end());
    allKeywords.insert(allKeywords.end(), emailKeywords.begin(), emailKeywords.end());
    allKeywords.insert(allKeywords.end(), phoneKeywords.begin(), phoneKeywords.end());
    allKeywords.insert(allKeywords.end(), nameKeywords.begin(), nameKeywords.end());
    allKeywords.insert(allKeywords.end(), passwordKeywords.begin(), passwordKeywords.end());
}

bool KeywordBasedFieldDetector::containsKeyword(const std::string& text, const std::vector<std::string>& keywords) const {
    std::string textLower = text;
    std::transform(textLower.begin(), textLower.end(), textLower.begin(), ::tolower);

    for (const auto& keyword : keywords) {
        if (textLower.find(keyword) != std::string::npos) {
            return true;
        }
    }
    return false;
}

bool KeywordBasedFieldDetector::isFormField(const std::string& text, const cv::Rect& rect, const cv::Mat& image) const {
    // Text-based filtering - convert to lowercase for case-insensitive matching
    std::string textLower = text;
    std::transform(textLower.begin(), textLower.end(), textLower.begin(), ::tolower);

    // Check if text contains any exclusion keywords
    if (containsKeyword(textLower, excludeKeywords)) {
        // Special case: "password" + exclusion word (e.g., "forgot password")
        // Only exclude if "password" isn't standing alone
        size_t passPos = textLower.find("password");
        if (passPos != std::string::npos) {
            // Check if "password" appears alone or as the main input field label
            // This is a simplistic check; could be improved
            if (textLower.length() > passPos + 8 + 5) { // "password" + some buffer
                // If it's a longer phrase, look for exclusion terms
                for (const auto& exclude : excludeKeywords) {
                    if (exclude != "password" && textLower.find(exclude) != std::string::npos) {
                        return false; // Exclude cases like "forgot password"
                    }
                }
            }
        }
        else {
            return false; // Contains exclusion keywords but not "password"
        }
    }

    // Size heuristics for input fields
    float width = rect.width;
    float height = rect.height;
    float ratio = width / height;

    // Button filtering - buttons are often closer to square or have text length similar to their width
    bool looksLikeButton = (ratio < 2.5 && ratio > 0.75) || (width < 150 && height > 30);

    // Checkbox filtering - checkboxes are usually square or nearly square
    bool looksLikeCheckbox = (ratio < 1.5 && ratio > 0.6 && width < 50);

    // Form fields typically have width-to-height ratio greater than 2.5
    bool isFieldShape = (ratio > 2.5 && height > 20 && width > 100 && width < 500);

    // Check if I'm dealing with a dark theme
    cv::Scalar meanColor = cv::mean(image(rect));
    bool isDarkThemed = (meanColor[0] + meanColor[1] + meanColor[2]) / 3.0 < 128;

    // Search for an actual input field in the vicinity of this text
    bool hasNearbyInputField = false;

    // Define search regions based on field label position
    // I'll look for an input field to the right, below, or above the current text
    std::vector<cv::Rect> searchRegions = {
        // Right of the text (most common position for an input field)
        cv::Rect(rect.x + rect.width, rect.y - rect.height / 2,
                 std::min(500, image.cols - (rect.x + rect.width)),
                 rect.height * 2),

                 // Below the text
                 cv::Rect(rect.x - 50, rect.y + rect.height,
                          rect.width + 100,
                          std::min(100, image.rows - (rect.y + rect.height)))
    };

    // Ensure all search regions are within image bounds
    for (auto& region : searchRegions) {
        region.x = std::max(0, region.x);
        region.y = std::max(0, region.y);
        region.width = std::min(region.width, image.cols - region.x);
        region.height = std::min(region.height, image.rows - region.y);

        // Skip invalid regions
        if (region.width <= 0 || region.height <= 0) continue;

        // Extract the region and look for potential input field shapes
        cv::Mat regionImg = image(region);
        cv::Mat grayRegion;
        cv::cvtColor(regionImg, grayRegion, cv::COLOR_BGR2GRAY);

        // Apply edge detection
        cv::Mat edges;
        cv::Canny(grayRegion, edges, 50, 150);

        // Find contours in this region
        std::vector<std::vector<cv::Point>> regionContours;
        cv::findContours(edges, regionContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

        // Check each contour for input field characteristics
        for (const auto& contour : regionContours) {
            cv::Rect contourRect = cv::boundingRect(contour);

            float contourRatio = static_cast<float>(contourRect.width) / contourRect.height;

            // Check if this looks like an input field
            if ((contourRatio > 2.0 && contourRatio < 15.0) ||
                (isDarkThemed && contourRect.width > 100 && contourRect.height > 20)) {
                hasNearbyInputField = true;
                break;
            }
        }

        if (hasNearbyInputField) break;
    }

    // Check if text contains any field-indicating keywords
    bool containsFieldKeyword = containsKeyword(textLower, allKeywords);

    // Stricter criteria for field detection
    if (containsFieldKeyword && !looksLikeButton && !looksLikeCheckbox) {
        return true;
    }

    // If shape strongly resembles a form field and I found a nearby input field
    if (isFieldShape && !looksLikeButton && !looksLikeCheckbox) {
        return true;
    }

    return false;
}

std::pair<std::string, cv::Scalar> KeywordBasedFieldDetector::getFieldType(const std::string& text) const {
    std::string textLower = text;
    std::transform(textLower.begin(), textLower.end(), textLower.begin(), ::tolower);

    // First, check for edge cases I want to avoid
    if ((textLower.find("forgot") != std::string::npos && textLower.find("password") != std::string::npos) ||
        (textLower.find("reset") != std::string::npos && textLower.find("password") != std::string::npos) ||
        (textLower.find("remember") != std::string::npos && textLower.find("me") != std::string::npos) ||
        textLower.find("sign up") != std::string::npos ||
        textLower.find("register") != std::string::npos ||
        textLower.find("submit") != std::string::npos ||
        textLower.find("login with") != std::string::npos ||
        textLower.find("sign in with") != std::string::npos) {
        // These are not input fields I want to detect
        return { "Not a Field", cv::Scalar(0, 0, 0) }; // Black in BGR (will be filtered out)
    }

    // Process actual field types
    // Username field
    if (textLower.find("username") != std::string::npos ||
        textLower.find("user name") != std::string::npos ||
        textLower.find("user id") != std::string::npos ||
        (textLower.find("login") != std::string::npos &&
            textLower.find("login with") == std::string::npos &&
            textLower.find("button") == std::string::npos)) {
        return { "Username", cv::Scalar(255, 0, 0) }; // Blue in BGR
    }
    // Email field
    else if (textLower.find("email") != std::string::npos ||
        textLower.find("e-mail") != std::string::npos ||
        (textLower.find("mail") != std::string::npos &&
            textLower.find("@") != std::string::npos)) {
        return { "Email", cv::Scalar(0, 255, 0) }; // Green in BGR
    }
    // Phone field
    else if (textLower.find("phone") != std::string::npos ||
        textLower.find("mobile") != std::string::npos ||
        textLower.find("cell") != std::string::npos ||
        textLower.find("telephone") != std::string::npos) {
        return { "Phone", cv::Scalar(255, 255, 0) }; // Yellow in BGR
    }
    // Name field
    else if ((textLower.find("name") != std::string::npos &&
        textLower.find("user") == std::string::npos) || // Exclude username
        textLower.find("first name") != std::string::npos ||
        textLower.find("last name") != std::string::npos ||
        textLower.find("full name") != std::string::npos) {
        return { "Name", cv::Scalar(255, 0, 255) }; // Magenta in BGR
    }
    // Password field
    else if ((textLower.find("password") != std::string::npos &&
        textLower.find("forgot") == std::string::npos && // Exclude "forgot password"
        textLower.find("reset") == std::string::npos &&
        textLower.find("contain") == std::string::npos && // Exclude "password must contain"
        textLower.find("manage") == std::string::npos && // Exclude "manage passwords"
        textLower.find("enter") == std::string::npos &&
        textLower.find("your") == std::string::npos) || // Exclude "enter your password"
        (textLower.find("pass") != std::string::npos &&
            textLower.find("word") == std::string::npos) || // Just "pass" not "password"
        textLower.find("pwd") != std::string::npos ||
        textLower.find("passcode") != std::string::npos) {
        return { "Password", cv::Scalar(0, 0, 255) }; // Red in BGR
    }
    // Default unknown field
    else {
        return { "Unknown Field", cv::Scalar(128, 128, 128) }; // Gray in BGR
    }
}