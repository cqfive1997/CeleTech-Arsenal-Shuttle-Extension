namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullDamageResult
    {
        private ShuttleHullDamageResult()
        {
        }

        public bool Handled { get; private set; }

        public bool FullyAbsorbed { get; private set; }

        public float IncomingDamage { get; private set; }

        public float ArmorPoolDamage { get; private set; }

        public float ReducedDamage
        {
            get
            {
                return this.ArmorPoolDamage;
            }
        }

        public float AbsorbedByHull { get; private set; }

        public float RemainingDamage { get; private set; }

        public float HullHitPointsBefore { get; private set; }

        public float HullHitPointsAfter { get; private set; }

        public static ShuttleHullDamageResult NotHandled()
        {
            return new ShuttleHullDamageResult();
        }

        public static ShuttleHullDamageResult CreateFullyAbsorbed(
            float incomingDamage,
            float armorPoolDamage,
            float absorbedByHull,
            float remainingDamage,
            float hullHitPointsBefore,
            float hullHitPointsAfter)
        {
            return new ShuttleHullDamageResult
            {
                Handled = true,
                FullyAbsorbed = true,
                IncomingDamage = incomingDamage,
                ArmorPoolDamage = armorPoolDamage,
                AbsorbedByHull = absorbedByHull,
                RemainingDamage = remainingDamage,
                HullHitPointsBefore = hullHitPointsBefore,
                HullHitPointsAfter = hullHitPointsAfter
            };
        }

        public static ShuttleHullDamageResult CreatePartiallyAbsorbed(
            float incomingDamage,
            float armorPoolDamage,
            float absorbedByHull,
            float remainingDamage,
            float hullHitPointsBefore,
            float hullHitPointsAfter)
        {
            return new ShuttleHullDamageResult
            {
                Handled = true,
                FullyAbsorbed = false,
                IncomingDamage = incomingDamage,
                ArmorPoolDamage = armorPoolDamage,
                AbsorbedByHull = absorbedByHull,
                RemainingDamage = remainingDamage,
                HullHitPointsBefore = hullHitPointsBefore,
                HullHitPointsAfter = hullHitPointsAfter
            };
        }
    }
}
