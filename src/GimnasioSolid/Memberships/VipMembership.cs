namespace GimnasioSolid.Memberships
{
    public sealed class VipMembership : IMembershipPlan
    {
        public decimal CalculatePrice() => 55.00m;
    }
}
