using UnityEngine;
using System.IO;


[System.Serializable]
public struct HexCoordinates
{
	public int x, z;
	public int y{
		get {
			return -x - z;
		}
	}

	public HexCoordinates(int x, int z)
	{
		this.x = x;
		this.z = z;
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

	//distanza di 2 celle basata sulle coordinate
	public int DistanceTo(HexCoordinates other)
	{
		return
			((x < other.x ? other.x - x : x - other.x) +
			(y < other.y ? other.y - y : y - other.y) +
			(z < other.z ? other.z - z : z - other.z)) / 2;
	}

	public override string ToString()
	{
		return "(" +x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")";
	}
	public string ToStringOnSeparateLines()
	{
		return x.ToString() + "\n" + y.ToString() + "\n" + z.ToString();
	}



	public void Save(BinaryWriter writer)
	{
		writer.Write(x);
		writer.Write(z);
	}

	public static HexCoordinates Load(BinaryReader reader)
	{
		HexCoordinates c;
		c.x = reader.ReadInt32();
		c.z = reader.ReadInt32();
		return c;
	}
}







