using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
	public HexGrid hexGrid;
	public HexMapGenerator mapGenerator;
	bool generateMaps = true;



	public void ToggleMapGeneration(bool toggle)
	{
		generateMaps = toggle;
	}

	public void Open()
	{
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}
	void CreateMap(int x, int z)
	{
		if (generateMaps)
		{
			mapGenerator.GenerateMap(x, z);
		}
		else
		{
			hexGrid.CreateMap(x, z);
		}
		HexMapCamera.ValidatePosition();
		Close();
	}

	public void CreateSmallMap()
	{
		CreateMap(18, 18);
	}

	public void CreateMediumMap()
	{
		CreateMap(30, 30);
	}

	public void CreateLargeMap()
	{
		CreateMap(90, 60);
	}


}