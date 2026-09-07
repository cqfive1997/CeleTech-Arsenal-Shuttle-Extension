namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class CrewProfile
    {
        public CrewProfile(int crewCapacity)
        {
            this.CrewCapacity = crewCapacity;
        }

        public int CrewCapacity { get; private set; }
    }
}
