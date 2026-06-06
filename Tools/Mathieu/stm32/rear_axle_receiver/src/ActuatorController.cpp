#include "ActuatorController.h"

void ActuatorController::begin()
{
    pinMode(RPWM_PIN, OUTPUT);
    pinMode(LPWM_PIN, OUTPUT);
    pinMode(REN_PIN, OUTPUT);
    pinMode(LEN_PIN, OUTPUT);
    pinMode(FEEDBACK_PIN, INPUT_ANALOG);

    analogWrite(RPWM_PIN, 0);
    analogWrite(LPWM_PIN, 0);

    digitalWrite(REN_PIN, HIGH);
    digitalWrite(LEN_PIN, HIGH);

    _targetSteerCdeg = 0;
    _targetAdc = ADC_AT_CENTER;
    _feedbackAdc = analogRead(FEEDBACK_PIN);
    _lastPwm = 0;
    _integral = 0.0f;
    _lastError = 0;
    _lastUpdateMs = millis();
}

void ActuatorController::setEnabled(bool enabled)
{
    _enabled = enabled;

    if (!_enabled)
    {
        stop();
    }
}

void ActuatorController::setTargetSteerCdeg(int32_t steerCdeg)
{
    if (steerCdeg < STEER_MIN_CDEG)
    {
        steerCdeg = STEER_MIN_CDEG;
    }

    if (steerCdeg > STEER_MAX_CDEG)
    {
        steerCdeg = STEER_MAX_CDEG;
    }

    _targetSteerCdeg = steerCdeg;
    _targetAdc = steerCdegToAdc(steerCdeg);
}

void ActuatorController::stop()
{
    analogWrite(RPWM_PIN, 0);
    analogWrite(LPWM_PIN, 0);
    _lastPwm = 0;
    _integral = 0.0f;
    _lastError = 0;
}

void ActuatorController::update()
{
    _feedbackAdc = analogRead(FEEDBACK_PIN);

    if (!_enabled)
    {
        stop();
        return;
    }

    uint32_t now = millis();
    float dt = (now - _lastUpdateMs) / 1000.0f;
    if (dt <= 0.0f || dt > 1.0f)
    {
        dt = 0.02f;
    }
    _lastUpdateMs = now;

    int error = _targetAdc - _feedbackAdc;

    if (abs(error) <= DEAD_BAND_ADC)
    {
        driveMotor(0);
        _integral = 0.0f;
        _lastError = error;
        return;
    }

    _integral += error * dt;

    // Anti-windup simple.
    if (_integral > 800.0f)
    {
        _integral = 800.0f;
    }
    else if (_integral < -800.0f)
    {
        _integral = -800.0f;
    }

    float derivative = (error - _lastError) / dt;
    _lastError = error;

    float output = KP * error + KI * _integral + KD * derivative;

    if (output > PWM_MAX)
    {
        output = PWM_MAX;
    }
    else if (output < -PWM_MAX)
    {
        output = -PWM_MAX;
    }

    driveMotor(static_cast<int>(output));
}

int ActuatorController::getFeedbackAdc() const
{
    return _feedbackAdc;
}

int ActuatorController::getTargetAdc() const
{
    return _targetAdc;
}

int ActuatorController::getLastPwm() const
{
    return _lastPwm;
}

bool ActuatorController::isEnabled() const
{
    return _enabled;
}

int ActuatorController::steerCdegToAdc(int32_t steerCdeg) const
{
    if (steerCdeg < 0)
    {
        long adc = map(
            steerCdeg,
            STEER_MIN_CDEG,
            0,
            ADC_AT_LEFT,
            ADC_AT_CENTER);

        return constrain(adc, ADC_MIN, ADC_MAX);
    }

    long adc = map(
        steerCdeg,
        0,
        STEER_MAX_CDEG,
        ADC_AT_CENTER,
        ADC_AT_RIGHT);

    return constrain(adc, ADC_MIN, ADC_MAX);
}

void ActuatorController::driveMotor(int pwm)
{
    if (pwm > PWM_MAX)
    {
        pwm = PWM_MAX;
    }
    else if (pwm < -PWM_MAX)
    {
        pwm = -PWM_MAX;
    }

    _lastPwm = pwm;

    if (pwm > 0)
    {
        analogWrite(RPWM_PIN, pwm);
        analogWrite(LPWM_PIN, 0);
    }
    else if (pwm < 0)
    {
        analogWrite(RPWM_PIN, 0);
        analogWrite(LPWM_PIN, -pwm);
    }
    else
    {
        analogWrite(RPWM_PIN, 0);
        analogWrite(LPWM_PIN, 0);
    }
}
