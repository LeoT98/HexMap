using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
	public int X, Z;
	public int Y {
		get {
			return -X - Z;
		}
	}

	public HexCoordinates(int x, int z)
	{
		X = x;
		Z = z;
	}

//da posiszione da coordinate non allineate
	public static HexCoordinates FromOffsetCoordinates(int x, int z) 
	{
		return new HexCoordinates(x - z / 2, z);
	}
	
	//da le coordinate in base alla posizione nello spazio
	public static HexCoordinates FromPosition(Vector3 position)
	{
		float x = position.x / (HexMetrics.innerRadius * 2f);
		float y = -x;
		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;

		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x - y);
		if (iX + iY + iZ != 0) {
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x - y - iZ);

			if (dX > dY && dX > dZ) {
				iX = -iY - iZ;
			} else if (dZ > dY) {
				iZ = -iX - iY;
			}
		}
		return new HexCoordinates(iX, iZ);
	}








	public override string ToString()
	{
		return "(" +X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}
	public string ToStringOnSeparateLines()
	{
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}



}







