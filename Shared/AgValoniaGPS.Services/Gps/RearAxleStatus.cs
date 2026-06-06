namespace AgValoniaGPS.Services.Gps;

public sealed record RearAxleStatus(
    ushort Seq,
    bool CommandValid,
    bool Accepted,
    int XteMm,
    int SteerCdeg,
    int TargetAdc,
    int FeedbackAdc,
    int Pwm,
    bool Enabled,
    bool Timeout,
    bool CrcOk,
    string RawMessage,
    DateTime ReceivedUtc);
