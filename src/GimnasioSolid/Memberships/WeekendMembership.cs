namespace GimnasioSolid.Memberships
{
    public sealed class WeekendMembership : IMembershipPlan
    {
        public decimal CalculatePrice() => 20.00m;
    }
}
