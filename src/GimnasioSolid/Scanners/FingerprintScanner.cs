namespace GimnasioSolid.Scanners
{
    public sealed class FingerprintScanner : IAccessScanner
    {
        public string Scan(string presentedData)
        {
            return presentedData.Trim().ToUpperInvariant();
        }
    }
}
