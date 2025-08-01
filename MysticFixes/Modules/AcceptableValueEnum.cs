using System;
using System.Linq;
using BepInEx.Configuration;

namespace MiscFixes.Modules
{
    public class AcceptableValueEnum<T> : AcceptableValueBase where T : Enum
    {
        public virtual T[] AcceptableValues { get; }

        public AcceptableValueEnum(params T[] acceptableValues)
            : base(typeof(T))
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("type must be an enum");
            }

            if (acceptableValues == null)
            {
                acceptableValues = [.. Enum.GetValues(typeof(T)).OfType<T>()];
            }

            if (acceptableValues.Length == 0)
            {
                throw new ArgumentException("At least one acceptable value is needed", "acceptableValues");
            }

            AcceptableValues = acceptableValues;
        }

        public override object Clamp(object value)
        {
            if (IsValid(value))
            {
                return value;
            }

            return AcceptableValues[0];
        }

        public override bool IsValid(object value)
        {
            if (value is T v)
            {
                return AcceptableValues.Any((T x) => x.Equals(v));
            }

            return false;
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values: " + string.Join(", ", AcceptableValues.Select((T x) => x.ToString()).ToArray());
        }
    }
}
