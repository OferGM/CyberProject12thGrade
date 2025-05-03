// TesseractOcrService.h
#pragma once
#include "IOcrService.h"
#include <tesseract/baseapi.h>

class TesseractOcrService : public IOcrService {
private:
    tesseract::TessBaseAPI tessApi;
    bool initialized;

public:
    TesseractOcrService();
    ~TesseractOcrService() override;

    void initialize() override;
    void cleanup() override;
    std::string extractText(const cv::Mat& image) override;
    std::string extractTextFromRegion(const cv::Mat& image, const cv::Rect& region) override;
};