using System.Globalization;

namespace AgValoniaGPS.ViewModels;

public partial class MainViewModel
{
    public string RearAxleDiagnosticPanelText
    {
        get
        {
            var state = _rearAxleRuntimeStateService.Current;

            if (state.UpdatedUtc == DateTime.MinValue)
            {
                return "No rear axle data";
            }

            if (!state.OverallReady)
            {
                return $"NOT READY | {state.Reason}";
            }

            string xte = state.RearCrossTrackErrorMeters.HasValue
                ? $"{state.RearCrossTrackErrorMeters.Value * 100.0:+0;-0;0} cm"
                : "n/a";

            return string.Create(
                CultureInfo.InvariantCulture,
                $"READY | XTE {xte} | Cmd {state.RearSteerCommandDegrees:F2}° | " +
                $"STM32 OK | ADC {state.Stm32FeedbackAdc}/{state.Stm32TargetAdc} | PWM {state.Stm32Pwm}");
        }
    }
}
