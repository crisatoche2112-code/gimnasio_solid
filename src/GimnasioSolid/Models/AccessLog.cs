using System;

namespace GimnasioSolid.Models
{
    public sealed class AccessLog
    {
        public AccessLog(string? memberId, string? memberName, DateTime timestamp, bool allowed, string scannerType, string presentedData)
        {
            MemberId = memberId;
            MemberName = memberName;
            Timestamp = timestamp;
            Allowed = allowed;
            ScannerType = scannerType;
            PresentedData = presentedData;
        }

        public string? MemberId { get; }
        public string? MemberName { get; }
        public DateTime Timestamp { get; }
        public bool Allowed { get; }
        public string ScannerType { get; }
        public string PresentedData { get; }
    }
}
