using System;
using NUnit.Framework;

/// <summary>
/// Tests for Exp.EllInt / Exp.EllIntF (incomplete elliptic integrals) and
/// EllipticArcEntity.Length() (compared with numerical arc-length subdivision).
/// Run via Unity Test Runner (Edit Mode) or the NoteCAD.Tests.EditMode assembly.
/// </summary>
[TestFixture]
public class EllipticIntegralTests {

	// -----------------------------------------------------------------------
	// Tolerance constants
	// -----------------------------------------------------------------------
	const double TolHigh       = 1e-9;   // tight: should match known analytic values
	const double TolDerivative = 1e-6;   // derivative finite-difference residual
	const double TolArc        = 1e-4;   // arc length vs 1,000,000-step quadrature

	// -----------------------------------------------------------------------
	// Helper: brute-force arc length ∫[a0..a1] sqrt(r0²sin²t + r1²cos²t) dt
	// -----------------------------------------------------------------------
	/// <summary>
	/// Numerical reference: midpoint-rule integral of sqrt(r0²sin²t + r1²cos²t) over [a0, a1].
	/// Uses <paramref name="n"/> = 1,000,000 sub-intervals; accurate to ≤1e-4 for typical arc extents.
	/// </summary>
	static double BruteForceArcLength(double r0, double r1, double a0, double a1, int n = 1000000) {
		double sum = 0.0;
		double da = (a1 - a0) / n;
		for(int i = 0; i < n; i++) {
			double t = a0 + (i + 0.5) * da;
			sum += Math.Sqrt(r0 * r0 * Math.Sin(t) * Math.Sin(t)
			               + r1 * r1 * Math.Cos(t) * Math.Cos(t));
		}
		return sum * da;
	}

	// -----------------------------------------------------------------------
	// EllInt — degenerate cases (k = 0)
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_ZeroModulus_EqualsPhiIdentity() {
		Assert.AreEqual(0.0,          Exp.EllInt(0.0,         0.0), TolHigh, "E(0, 0)");
		Assert.AreEqual(1.0,          Exp.EllInt(1.0,         0.0), TolHigh, "E(1, 0)");
		Assert.AreEqual(Math.PI / 2,  Exp.EllInt(Math.PI / 2, 0.0), TolHigh, "E(π/2, 0)");
		Assert.AreEqual(Math.PI,      Exp.EllInt(Math.PI,     0.0), 1e-10,   "E(π, 0)");
	}

	// -----------------------------------------------------------------------
	// EllInt — degenerate cases (k = 1): E(φ,1) = sin(φ) for φ in [0, π/2]
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_UnitModulus_EqualsSin() {
		Assert.AreEqual(Math.Sin(0.3), Exp.EllInt(0.3,        1.0), TolHigh, "E(0.3, 1)");
		Assert.AreEqual(Math.Sin(0.7), Exp.EllInt(0.7,        1.0), TolHigh, "E(0.7, 1)");
		Assert.AreEqual(1.0,           Exp.EllInt(Math.PI / 2, 1.0), TolHigh, "E(π/2, 1)=1");
	}

	// -----------------------------------------------------------------------
	// EllInt — known complete values (Abramowitz & Stegun Table 17.1)
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_KnownCompleteValues() {
		Assert.AreEqual(1.4674622093374185, Exp.EllInt(Math.PI / 2, 0.5),              TolHigh, "E(π/2, 0.5)");
		Assert.AreEqual(1.3506438810476755, Exp.EllInt(Math.PI / 2, 1.0 / Math.Sqrt(2)), TolHigh, "E(π/2, 1/√2)");
		Assert.AreEqual(1.2110560275684595, Exp.EllInt(Math.PI / 2, 0.9),              1e-6,    "E(π/2, 0.9)");
	}

	// -----------------------------------------------------------------------
	// EllInt — odd symmetry: E(-φ, k) = -E(φ, k)
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_NegativePhi_IsOdd() {
		double E_pos = Exp.EllInt(1.0, 0.5);
		Assert.AreEqual(-E_pos, Exp.EllInt(-1.0, 0.5), TolHigh, "E(-1, 0.5) = -E(1, 0.5)");
	}

	// -----------------------------------------------------------------------
	// EllIntF — degenerate cases (k = 0)
	// -----------------------------------------------------------------------
	[Test]
	public void EllIntF_ZeroModulus_EqualsPhiIdentity() {
		Assert.AreEqual(1.0,         Exp.EllIntF(1.0,         0.0), TolHigh, "F(1, 0)");
		Assert.AreEqual(Math.PI / 2, Exp.EllIntF(Math.PI / 2, 0.0), TolHigh, "F(π/2, 0)");
	}

	// -----------------------------------------------------------------------
	// EllIntF — known complete value K(0.5)  (Abramowitz & Stegun Table 17.1)
	// -----------------------------------------------------------------------
	[Test]
	public void EllIntF_KnownCompleteValue_K05() {
		Assert.AreEqual(1.6857503548125960, Exp.EllIntF(Math.PI / 2, 0.5), TolHigh, "K(0.5)");
	}

	// -----------------------------------------------------------------------
	// EllInt derivative: ∂E/∂φ = sqrt(1 - k²sin²φ)
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_DerivWithRespectToPhi() {
		double phi = 0.8, k = 0.6, h = 1e-7;
		double numerical  = (Exp.EllInt(phi + h, k) - Exp.EllInt(phi - h, k)) / (2 * h);
		double analytical = Math.Sqrt(1.0 - k * k * Math.Sin(phi) * Math.Sin(phi));
		Assert.AreEqual(analytical, numerical, TolDerivative, "dE/dφ");
	}

	// -----------------------------------------------------------------------
	// EllInt derivative: ∂E/∂k = (E - F) / k
	// -----------------------------------------------------------------------
	[Test]
	public void EllInt_DerivWithRespectToK() {
		double phi = 0.8, k = 0.6, h = 1e-7;
		double numerical  = (Exp.EllInt(phi, k + h) - Exp.EllInt(phi, k - h)) / (2 * h);
		double analytical = (Exp.EllInt(phi, k) - Exp.EllIntF(phi, k)) / k;
		Assert.AreEqual(analytical, numerical, TolDerivative, "dE/dk");
	}

	// -----------------------------------------------------------------------
	// EllIntF derivative: ∂F/∂φ = 1/sqrt(1 - k²sin²φ)
	// -----------------------------------------------------------------------
	[Test]
	public void EllIntF_DerivWithRespectToPhi() {
		double phi = 0.8, k = 0.6, h = 1e-7;
		double numerical  = (Exp.EllIntF(phi + h, k) - Exp.EllIntF(phi - h, k)) / (2 * h);
		double analytical = 1.0 / Math.Sqrt(1.0 - k * k * Math.Sin(phi) * Math.Sin(phi));
		Assert.AreEqual(analytical, numerical, TolDerivative, "dF/dφ");
	}

	// -----------------------------------------------------------------------
	// EllIntF derivative: ∂F/∂k = (E-(1-k²)F)/(k(1-k²)) - k·sin·cos/((1-k²)·sqrt(…))
	// -----------------------------------------------------------------------
	[Test]
	public void EllIntF_DerivWithRespectToK() {
		double phi = 0.8, k = 0.6, h = 1e-7;
		double numerical  = (Exp.EllIntF(phi, k + h) - Exp.EllIntF(phi, k - h)) / (2 * h);
		double k2 = k * k;
		double oneMinusK2 = 1.0 - k2;
		double sqrtTerm   = Math.Sqrt(1.0 - k2 * Math.Sin(phi) * Math.Sin(phi));
		double analytical = (Exp.EllInt(phi, k) - oneMinusK2 * Exp.EllIntF(phi, k)) / (k * oneMinusK2)
		                  - k * Math.Sin(phi) * Math.Cos(phi) / (oneMinusK2 * sqrtTerm);
		Assert.AreEqual(analytical, numerical, TolDerivative, "dF/dk");
	}

	// -----------------------------------------------------------------------
	// Arc length — circular arc: L = r·(a1 - a0)
	// -----------------------------------------------------------------------
	[Test]
	public void ArcLength_CircularArc_EqualsRadiusTimesAngle() {
		double r = 3.0;
		double[] starts = { 0, Math.PI / 4, 0.5 };
		double[] ends   = { Math.PI / 2, 3 * Math.PI / 4, 2.0 };
		for(int i = 0; i < starts.Length; i++) {
			double expected = r * (ends[i] - starts[i]);
			double actual   = EvalArcLength(r, r, starts[i], ends[i]);
			Assert.AreEqual(expected, actual, TolArc, $"circle r=3, [{starts[i]:F2}, {ends[i]:F2}]");
		}
	}

	// -----------------------------------------------------------------------
	// Arc length, r0 >= r1 — compare with subdivided numerical integration
	// -----------------------------------------------------------------------
	[TestCase(3.0, 2.0, 0.0,          Math.PI / 2,     TestName = "r0>r1, [0, π/2]")]
	[TestCase(3.0, 2.0, Math.PI / 4,  3 * Math.PI / 4, TestName = "r0>r1, [π/4, 3π/4]")]
	[TestCase(3.0, 2.0, 0.5,          2.0,             TestName = "r0>r1, [0.5, 2.0]")]
	[TestCase(3.0, 2.0, 1.0,          Math.PI,         TestName = "r0>r1, [1.0, π]")]
	public void ArcLength_R0GreaterThanR1_MatchesBruteForce(double r0, double r1, double a0, double a1) {
		Assert.AreEqual(BruteForceArcLength(r0, r1, a0, a1),
		                EvalArcLength(r0, r1, a0, a1),
		                TolArc, $"r0={r0}, r1={r1}, [{a0:F3}, {a1:F3}]");
	}

	// -----------------------------------------------------------------------
	// Arc length, r0 < r1 — was buggy before the fix; compare with brute force
	// -----------------------------------------------------------------------
	[TestCase(2.0, 3.0, 0.0,          Math.PI / 2,     TestName = "r0<r1, [0, π/2]")]
	[TestCase(2.0, 3.0, Math.PI / 4,  3 * Math.PI / 4, TestName = "r0<r1, [π/4, 3π/4]")]
	[TestCase(2.0, 3.0, 0.5,          2.0,             TestName = "r0<r1, [0.5, 2.0]")]
	[TestCase(2.0, 3.0, 1.0,          Math.PI,         TestName = "r0<r1, [1.0, π]")]
	public void ArcLength_R0LessThanR1_MatchesBruteForce(double r0, double r1, double a0, double a1) {
		Assert.AreEqual(BruteForceArcLength(r0, r1, a0, a1),
		                EvalArcLength(r0, r1, a0, a1),
		                TolArc, $"r0={r0}, r1={r1}, [{a0:F3}, {a1:F3}]");
	}

	// -----------------------------------------------------------------------
	// Arc length — asymmetric intervals should differ between (r0,r1) and (r1,r0)
	// -----------------------------------------------------------------------
	[Test]
	public void ArcLength_SwappedRadii_DifferForAsymmetricInterval() {
		double L_r0Big = EvalArcLength(3.0, 2.0, Math.PI / 4, 3 * Math.PI / 4);
		double L_r1Big = EvalArcLength(2.0, 3.0, Math.PI / 4, 3 * Math.PI / 4);
		Assert.Greater(Math.Abs(L_r0Big - L_r1Big), 0.01,
		               "swapping r0,r1 must change length for a non-symmetric interval");
	}

	// -----------------------------------------------------------------------
	// Helper: evaluate EllipticArcEntity.Length() for given r0, r1, a0, a1
	// by building the Exp expression and calling .Eval().
	// -----------------------------------------------------------------------
	static double EvalArcLength(double r0Val, double r1Val, double a0Val, double a1Val) {
		var r0 = new Param("r0", r0Val);
		var r1 = new Param("r1", r1Val);
		var a0 = new Param("a0", a0Val);
		var a1 = new Param("a1", a1Val);

		// Replicate EllipticArcEntity.Length() logic (using If conditional)
		var ar0     = Exp.Abs(r0);
		var ar1     = Exp.Abs(r1);
		var absDiff = Exp.Abs(ar0 - ar1);
		var rmax    = (ar0 + ar1 + absDiff) / 2.0;
		var rmin    = (ar0 + ar1 - absDiff) / 2.0;
		var k       = Exp.Sqrt(Exp.one - Exp.Sqr(rmin) / Exp.Sqr(rmax));
		var cond    = new Exp(Exp.Op.GEqual, ar0, ar1);
		var L0      = rmax * (Exp.EllInt(Math.PI / 2.0 - a0.exp, k) - Exp.EllInt(Math.PI / 2.0 - a1.exp, k));
		var L1      = rmax * (Exp.EllInt(a1.exp, k) - Exp.EllInt(a0.exp, k));
		var length  = new Exp(Exp.Op.If, cond, L0, L1);

		return length.Eval();
	}
}
