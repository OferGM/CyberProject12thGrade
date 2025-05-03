#include "KeywordBasedLoginDetector.h"
#include <algorithm>

KeywordBasedLoginDetector::KeywordBasedLoginDetector() {
    // Initialize login detection keyword lists
    loginKeywords = {
        // Basic login terms
        "login", "sign in", "signin", "log in", "username", "password", "email",
        "phone", "forgot password", "reset password", "remember me", "create account",
        // Account creation and registration terms
        "register", "authentication", "verify", "credentials", "account",
        "welcome back", "sign up", "signup", "continue with", "continue", "email address",
        "don't have an account", "new account", "create your account", "join now",
        // Social login options
        "continue with google", "continue with microsoft", "continue with apple",
        "continue with facebook", "sign in with google", "sign in with apple",
        "facebook", "google", "apple", "microsoft", "steam", "epic games",
        // Legal and policy references often found on login screens
        "privacy policy", "terms of service", "terms of use", "terms and conditions",
        // Action buttons typically found on login forms
        "next", "submit", "go", "enter", "send code", "verify email", "get started",
        // Form and field related terms
        "required", "required field", "remember this device", "keep me signed in",
        "stay signed in", "keep me logged in", "not your computer", "guest mode"
    };

    strongKeywords = {
        "sign in with", "sign in to", "log in to", "email address", "password",
        "username and password", "forgot password", "create account", "sign up",
        "continue with google", "continue with microsoft", "continue with apple",
        "remember me", "email or phone", "username", "login", "signin", "sign in",
        "log in", "create your account", "verify your identity", "required field"
    };
}

int KeywordBasedLoginDetector::countKeywordOccurrences(const std::string& text, const std::vector<std::string>& keywords) const {
    std::string textLower = text;
    std::transform(textLower.begin(), textLower.end(), textLower.begin(), ::tolower);

    int count = 0;
    for (const auto& keyword : keywords) {
        size_t pos = 0;
        while ((pos = textLower.find(keyword, pos)) != std::string::npos) {
            count++;
            pos += keyword.length();
        }
    }
    return count;
}

double KeywordBasedLoginDetector::calculateConfidence(
    const std::vector<FormField>& formFields,
    const std::string& pageText,
    std::map<std::string, double>& confidenceFactors) const {

    // Initialize confidence factors
    confidenceFactors["labeled_fields"] = 0.0;
    confidenceFactors["login_keywords"] = 0.0;
    confidenceFactors["strong_keywords"] = 0.0;
    confidenceFactors["password_field"] = 0.0;
    confidenceFactors["username_field"] = 0.0;

    // 1. Check for labeled login-related fields (high importance)
    std::set<std::string> loginFieldTypes = { "Username", "Email", "Password", "Phone", "Name" };
    std::set<std::string> detectedFieldTypes;

    bool hasPasswordField = false;
    bool hasUsernameOrEmailField = false;

    for (const auto& field : formFields) {
        detectedFieldTypes.insert(field.type);

        if (field.type == "Password") {
            hasPasswordField = true;
            confidenceFactors["password_field"] = 0.35; // Password field is a strong indicator
        }

        if (field.type == "Username" || field.type == "Email" || field.type == "Phone" || field.type == "Name") {
            hasUsernameOrEmailField = true;
            confidenceFactors["username_field"] = 0.25; // Username/Email field is an indicator
        }
    }

    // Count how many login-related field types were detected
    int loginFieldCount = 0;
    for (const auto& type : loginFieldTypes) {
        if (detectedFieldTypes.find(type) != detectedFieldTypes.end()) {
            loginFieldCount++;
        }
    }

    // Calculate labeled fields factor (0.0 to 0.3)
    confidenceFactors["labeled_fields"] = std::min(0.3, loginFieldCount * 0.1);

    // 2. Check for regular login keywords in the page text (medium importance)
    int loginKeywordCount = countKeywordOccurrences(pageText, loginKeywords);
    confidenceFactors["login_keywords"] = std::min(0.15, loginKeywordCount * 0.02);

    // 3. Check for strong login keywords in the page text (high importance)
    int strongKeywordCount = countKeywordOccurrences(pageText, strongKeywords);
    confidenceFactors["strong_keywords"] = std::min(0.3, strongKeywordCount * 0.05);

    // Combined confidence score (max 1.0)
    double totalConfidence = 0.0;
    for (const auto& factor : confidenceFactors) {
        totalConfidence += factor.second;
    }

    // Cap at 1.0
    return std::min(1.0, totalConfidence);
}