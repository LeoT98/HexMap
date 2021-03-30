using UnityEngine;


//fa una curva
public static class Bezier
{
	// t deve essere nell'intervallo [0 ; 1]
	public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		float r = 1f - t;
		return r * r * a + 2f * r * t * b + t * t * c;
	}

	// La derivata mi da la tangente nel punto; t deve essere nell'intervallo (0 ; 1)
	public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		return 2f * ((1f - t) * (b - a) + t * (c - b));
	}
}