namespace GimnasioSolid.Memberships
{
    public sealed class StudentMembership : IMembershipPlan
    {
        public decimal CalculatePrice() => 25.00m;
    }
}
