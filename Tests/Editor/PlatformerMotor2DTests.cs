using NUnit.Framework;

namespace Yenotsoftware.KSE2DPlatformerFramework.Editor.Tests
{
	public class PlatformerMotor2DTests
	{
		private const float Accel = 100f;
		private const float Decel = 10f;
		private const float Dt = 0.01f;

		[Test]
		public void AccelerateTowards_SpeedingUp_UsesAcceleration()
		{
			Assert.AreEqual(1f, PlatformerMotor2D.AccelerateTowards(0f, 5f, Accel, Decel, Dt), 1e-5f);
		}

		[Test]
		public void AccelerateTowards_Stopping_UsesDeceleration()
		{
			Assert.AreEqual(4.9f, PlatformerMotor2D.AccelerateTowards(5f, 0f, Accel, Decel, Dt), 1e-5f);
		}

		[Test]
		public void AccelerateTowards_Turning_UsesDeceleration()
		{
			Assert.AreEqual(4.9f, PlatformerMotor2D.AccelerateTowards(5f, -5f, Accel, Decel, Dt), 1e-5f);
		}

		[Test]
		public void AccelerateTowards_AboveTargetSpeed_UsesDeceleration()
		{
			Assert.AreEqual(9.9f, PlatformerMotor2D.AccelerateTowards(10f, 5f, Accel, Decel, Dt), 1e-5f);
		}

		[Test]
		public void AccelerateTowards_DoesNotOvershootTarget()
		{
			Assert.AreEqual(5f, PlatformerMotor2D.AccelerateTowards(4.5f, 5f, Accel, Decel, Dt), 1e-5f);
		}

		[Test]
		public void JumpVelocity_ReachesRequestedHeight()
		{
			const float gravity = 9.81f;
			const float height = 3f;
			float v = PlatformerMotor2D.JumpVelocity(height, gravity);

			// Peak height of a ballistic jump: v^2 / (2g).
			Assert.AreEqual(height, v * v / (2f * gravity), 1e-4f);
		}
	}
}
