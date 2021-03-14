using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection
{

	public Transform[] prefabs;

	public Transform Pick(float choice) //choice tra 0 e 1
	{
		return prefabs[(int)(choice * prefabs.Length)];
	}
}