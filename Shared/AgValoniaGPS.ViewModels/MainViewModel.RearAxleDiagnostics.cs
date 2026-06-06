using System.Globalization;
using AgValoniaGPS.Models.Base;

namespace AgValoniaGPS.ViewModels;

public partial class MainViewModel
{
    public string FrontAxleDiagnosticPanelText
    {
        get
        {
            try
            {
                var gps = _gpsService.CurrentData;
                var position = gps.CurrentPosition;

                bool frontGpsOk = _gpsService.IsConnected && _gpsService.IsGpsDataOk();
                bool trackOk = _gpsPipelineService.HasActiveTrack;

                if (!frontGpsOk)
                {
                    return "Front NOT READY | GPS not OK";
                }

                if (!trackOk)
                {
                    return "Front NOT READY | no active track";
                }

                var track = _gpsPipelineService.CurrentActiveTrack;
                if (track == null || track.Points.Count < 2)
                {
                    return "Front NOT READY | invalid track";
                }

                var frontPoint = new Vec2(position.Easting, position.Northing);
                var pointA = new Vec2(track.Points[0].Easting, track.Points[0].Northing);
                var pointB = new Vec2(track.Points[1].Easting, track.Points[1].Northing);

                double frontXte = _guidanceGeometryService.CrossTrackErrorMeters(
                    frontPoint,
                    pointA,
                    pointB);

                string xte = $"{frontXte * 100.0:+0;-0;0} cm";

                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"Front READY | XTE {xte} | Cmd n/a | GPS OK");
            }
            catch (Exception ex)
            {
                return $"Front NOT READY | {ex.Message}";
            }
        }
    }

    public string RearAxleDiagnosticPanelText
    {
        get
        {
            var state = _rearAxleRuntimeStateService.Current;

            if (state.UpdatedUtc == DateTime.MinValue)
            {
                return "Rear  NOT READY | no rear axle data";
            }

            if (!state.OverallReady)
            {
                return $"Rear  NOT READY | {state.Reason}";
            }

            string xte = state.RearCrossTrackErrorMeters.HasValue
                ? $"{state.RearCrossTrackErrorMeters.Value * 100.0:+0;-0;0} cm"
                : "n/a";

            return string.Create(
                CultureInfo.InvariantCulture,
                $"Rear  READY | XTE {xte} | Cmd {state.RearSteerCommandDegrees:F2}° | " +
                $"STM32 OK | ADC {state.Stm32FeedbackAdc}/{state.Stm32TargetAdc} | PWM {state.Stm32Pwm}");
        }
    }
}
