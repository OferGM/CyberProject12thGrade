#pragma once
#include "IPasswordFieldDetector.h"

class ContourBasedPasswordDetector : public IPasswordFieldDetector {
public:
    int detectPasswordDots(const cv::Mat& fieldImage) const override;
};