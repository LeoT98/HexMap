using UnityEngine;

public class CV33 : HexUnit
{
    new public static HexUnit unitPrefab; //instanziato nell'Awake di HexGrid e in OnEnable
    new public static int prefabIndex = 0;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //




    public override int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
	{//ritorna -1 per celle non accesssibili
		HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
		if (edgeType == HexEdgeType.Cliff)
		{
			return -1;
		}
		int moveCost;
		if (fromCell.HasRoadThroughEdge(direction))
		{
			moveCost = 1;
		}
		else if (fromCell.Walled != toCell.Walled)
		{
			return -1;
		}
		else
		{
			moveCost = edgeType == HexEdgeType.Flat ? 2 : 4;
			moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
		}
		return moveCost;
	}

	public override int GetVisionCost(HexCell firstCell, HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		int visionCost = 1;
		if (firstCell.Elevation < toCell.ViewElevation)
		{
			//visionCost = -1;
		}

		return visionCost;
	}






	public override void Save(System.IO.BinaryWriter writer)
	{
		location.coordinates.Save(writer);
		writer.Write(orientation);
		writer.Write((byte)prefabIndex);
		Debug.Log("Saving: " + prefabIndex);
	}



}
