using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A class to store global variables.
/// </summary>
public class Settings {

	/// <summary>
	/// Whether or not we use Cholesky decomposition to accelerate the calculation (which will however make initialization slower).
	/// </summary>
	public static bool useCholesky = true;

	/// <summary>
	/// The boundary condition interpolation.
	/// 0 - Neumann condition, 1 - Dirichlet condition
	/// </summary>
	public static float boundaryCondition = 0.5f;

	/// <summary>
	/// Distance on the surface that correspond to one period of the texture.
	/// </summary>
	public static float mappingDistance = 4f;

	/// <summary>
	/// Scrolling offset per second.
	/// </summary>
	public static float ScrollSpeed = 0.05f;

	/// <summary>
	/// The limit of the cotangent value (to prevent solver error).
	/// </summary>
	public static double cotLimit = 10000;

	/// <summary>
	/// The time step t = tFactor * maxEdgeLength ^ 2.
	/// </summary>
	public static double tFactor = 1;

	/// <summary>
	/// Default index of the source vertex.
	/// </summary>
	public static List<int> defaultSource = new List<int>();

	/// <summary>
	/// Default index of the triangle where the walking man is initially standing.
	/// </summary>
	public static int initialManPos = 42;

}
