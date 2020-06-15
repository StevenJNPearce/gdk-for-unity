using System;
using NUnit.Framework.Internal;
using Unity.PerformanceTesting;

namespace Improbable.Gdk.TestUtils
{
    [Flags]
    public enum Burst
    {
        Default = 0,
        Disabled = 1
    }

    [Flags]
    public enum Backend
    {
        Mono = 2,
        Il2Cpp = 4
    }

    public class PerformanceTestAttribute : PerformanceAttribute
    {
        private readonly Burst burst;
        private readonly Backend backend;

        public PerformanceTestAttribute(Burst burst = Burst.Default, Backend backend = Backend.Mono | Backend.Il2Cpp)
        {
            this.burst = burst;
            this.backend = backend;
        }

        public new void ApplyToTest(Test test)
        {
            base.ApplyToTest(test);

            var burstStr = burst.ToString()
                .Replace(", ", ".Burst");

            var backendStr = backend.ToString()
                .Replace(", ", ".Backend");

            test.Properties.Add("Category", $"Burst{burstStr}.Backend{backendStr}");
        }
    }
}
