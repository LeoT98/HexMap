using UnityEngine;

public class SferaBlu : HexUnit
{
	new public static  HexUnit unitPrefab; //instanziato nell'Awake di HexGrid e in OnEnable
	new public static  int prefabIndex = 1;

	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //












	public override void Save(System.IO.BinaryWriter writer)
	{
		location.coordinates.Save(writer);
		writer.Write(orientation);
		writer.Write((byte)prefabIndex);
		Debug.Log("Saving: " + prefabIndex);
	}



}
