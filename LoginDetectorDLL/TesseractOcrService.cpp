// TesseractOcrService.cpp
#include "TesseractOcrService.h"
#include <opencv2/imgproc.hpp>
#include <algorithm>

TesseractOcrService::TesseractOcrService() : initialized(false) {}

TesseractOcrService::~TesseractOcrService() {
    cleanup();
}

void TesseractOcrService::initialize() {
    if (!initialized) {
        if (tessApi.Init(NULL, "eng", tesseract::OEM_LSTM_ONLY)) {
            throw std::runtime_error("Could not initialize Tesseract.");
        }
        tessApi.SetPageSegMode(tesseract::PSM_AUTO);
        initialized = true;
    }
}

void TesseractOcrService::cleanup() {
    if (initialized) {
        tessApi.End();
        initialized = false;
    }
}

std::string TesseractOcrService::extractText(const cv::Mat& image) {
    if (!initialized) {
        initialize();
    }

    // Convert image to grayscale
    cv::Mat gray;
    if (image.channels() == 3) {
        cv::cvtColor(image, gray, cv::COLOR_BGR2GRAY);
    }
    else {
        gray = image.clone();
    }

    // Apply adaptive thresholding to handle different lighting conditions
    cv::Mat binaryImg;
    cv::adaptiveThreshold(gray, binaryImg, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
        cv::THRESH_BINARY, 11, 2);

    // Set the image for OCR
    tessApi.SetImage(binaryImg.data, binaryImg.cols, binaryImg.rows, 1, binaryImg.step);

    // Get the text
    char* text = tessApi.GetUTF8Text();
    std::string result(text);
    delete[] text;

    // Convert to lowercase for easier processing
    std::transform(result.begin(), result.end(), result.begin(), ::tolower);

    return result;
}

std::string TesseractOcrService::extractTextFromRegion(const cv::Mat& image, const cv::Rect& region) {
    if (!initialized) {
        initialize();
    }

    // Ensure rectangle is within image bounds
    cv::Rect safeRect = region;
    safeRect.x = std::max(0, safeRect.x);
    safeRect.y = std::max(0, safeRect.y);
    safeRect.width = std::min(image.cols - safeRect.x, safeRect.width);
    safeRect.height = std::min(image.rows - safeRect.y, safeRect.height);

    if (safeRect.width <= 0 || safeRect.height <= 0) {
        return ""; // Invalid rectangle
    }

    cv::Mat fieldImage = image(safeRect);

    // Preprocess for better OCR
    cv::Mat fieldGray;
    cv::cvtColor(fieldImage, fieldGray, cv::COLOR_BGR2GRAY);

    // Apply threshold to make text more visible
    cv::Mat fieldThresh;
    cv::threshold(fieldGray, fieldThresh, 127, 255, cv::THRESH_BINARY_INV);

    // Run OCR on the field
    tessApi.SetImage(fieldThresh.data, fieldThresh.cols, fieldThresh.rows, 1, fieldThresh.step);
    char* ocrText = tessApi.GetUTF8Text();
    std::string text(ocrText);
    delete[] ocrText;

    // Clean up the text (remove newlines, excess spaces)
    text.erase(std::remove(text.begin(), text.end(), '\n'), text.end());
    text.erase(std::remove(text.begin(), text.end(), '\r'), text.end());

    // Trim leading/trailing spaces
    text.erase(0, text.find_first_not_of(" \t"));
    text.erase(text.find_last_not_of(" \t") + 1);

    return text;
}