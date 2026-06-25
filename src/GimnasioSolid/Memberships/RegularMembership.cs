namespace GimnasioSolid.Memberships
{
    public sealed class RegularMembership : IMembershipPlan
    {
        public decimal CalculatePrice() => 40.00m;
    }
}
