using Lab.W01.AdjustablePlate.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.AdjustablePlate.Tests
{
    public class PlateSeatTests
    {
        [Test]
        public void TheSeat_IsTheFirstFit_MovedAndTiltedInTheFitsOwnAxes()
        {
            var fit = new FibulaFrame.Fit
            {
                position = new Vector3(1f, 2f, 3f),
                rotation = Quaternion.Euler(0f, 90f, 0f),
            };

            var (position, rotation) = PlateSeat.From(fit);

            var expected = fit.position + fit.rotation * PlateSeat.OwnerMove;
            Assert.That(Vector3.Distance(expected, position), Is.LessThan(1e-6f));
            Assert.AreEqual(Mathf.Abs(PlateSeat.OwnerTiltDeg), Quaternion.Angle(fit.rotation, rotation), 1e-3f);
            // The fit's Z points out of the bone, so a negative Z move brings the plate towards it.
            Assert.That(Vector3.Dot(position - fit.position, fit.rotation * Vector3.forward), Is.LessThan(0f));
        }
    }
}
