#pragma once

#include <Arduino.h>

class ActuatorController
{
public:
    void begin();

    void setEnabled(bool enabled);
    void setTargetSteerCdeg(int32_t steerCdeg);
    void stop();

    void update();

    int getFeedbackAdc() const;
    int getTargetAdc() const;
    int getLastPwm() const;
    bool isEnabled() const;

private:
    static constexpr int RPWM_PIN = PB0;
    static constexpr int LPWM_PIN = PB1;
    static constexpr int REN_PIN = PB10;
    static constexpr int LEN_PIN = PB11;
    static constexpr int FEEDBACK_PIN = PA0;

    static constexpr int ADC_MIN = 0;
    static constexpr int ADC_MAX = 4095;

    static constexpr int STEER_MIN_CDEG = -2500;
    static constexpr int STEER_MAX_CDEG = 2500;

    // Calibration provisoire.
    // À mesurer sur le vrai vérin.
    static constexpr int ADC_AT_LEFT = 900;     // -25°
    static constexpr int ADC_AT_CENTER = 2048;  // 0°
    static constexpr int ADC_AT_RIGHT = 3196;   // +25°

    static constexpr int PWM_MAX = 255;
    static constexpr int DEAD_BAND_ADC = 12;

    // Gains PID provisoires.
    static constexpr float KP = 0.45f;
    static constexpr float KI = 0.02f;
    static constexpr float KD = 0.00f;

    bool _enabled = false;

    int32_t _targetSteerCdeg = 0;
    int _targetAdc = ADC_AT_CENTER;
    int _feedbackAdc = ADC_AT_CENTER;
    int _lastPwm = 0;

    float _integral = 0.0f;
    int _lastError = 0;
    uint32_t _lastUpdateMs = 0;

    int steerCdegToAdc(int32_t steerCdeg) const;
    void driveMotor(int pwm);
};
