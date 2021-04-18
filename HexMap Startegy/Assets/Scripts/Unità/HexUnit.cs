using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;



public class HexUnit : MonoBehaviour //per flexare potrei farla astratta
{
	public static int prefabIndex= 42;

	//varia anche le visibilità
	public HexCell Location
	{
		get {
			return location;
		}
		set {
			if (location)
			{
				Grid.DecreaseVisibility(location, this);
				location.Unit = null;
			}
			location = value;
			value.Unit = this;
			Grid.IncreaseVisibility(value, this);
			transform.localPosition = value.Position;
		}
	}
	protected HexCell location;
	HexCell currentTravelLocation;

	public float Orientation
	{
		get {
			return orientation;
		}
		set {
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}
	protected float orientation;

	public static HexUnit unitPrefab; //instanziato nell'Awake di HexGrid e in OnEnable. Serve solo nei figli

	List<HexCell> pathToTravel;
	const float travelSpeed = 3f; //per l'animazione
	const float rotationSpeed = 180f; //per l'animazione

	public HexGrid Grid { get; set; }
	public virtual int VisionRange 
	{
		get { return 3; }
        set { }
	}

	public int Speed // caselle che si muove
	{
		get {
			return 24;
		}
	}
	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //

	void OnEnable()
	{
		if (location)
		{
			transform.localPosition = location.Position;

			if (currentTravelLocation)
			{
				Grid.IncreaseVisibility(location, this);
				Grid.DecreaseVisibility(currentTravelLocation, this);
				currentTravelLocation = null;
			}
		}

	}	


	public void Travel(List<HexCell> path)
	{
		location.Unit = null;
		location = path[path.Count - 1];
		location.Unit = this;
		pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	IEnumerator TravelPath()
	{//è una coroutine così più unità possono muoversi contemporaneamente
		Vector3 a, b, c = pathToTravel[0].Position;
		yield return LookAt(pathToTravel[1].Position); // prima di muoversi si gira dalla parte giusta
		Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], this);

		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++)
		{
			currentTravelLocation = pathToTravel[i];
			a = c;
			b = pathToTravel[i - 1].Position;
			c = (b + currentTravelLocation.Position) * 0.5f;
			Grid.IncreaseVisibility(pathToTravel[i], this);
			for (  ; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f; // evita che si inclini quando va su celle con altezza diversa
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null; //aspetta il prossimo frame
			}
			Grid.DecreaseVisibility(pathToTravel[i], this);
			t -= 1f;
		}
		currentTravelLocation = null;

		a = c;
		b = location.Position;
		c = b;
		Grid.IncreaseVisibility(location, this);
		for (  ; t < 1f; t += Time.deltaTime * travelSpeed)
		{
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
			Vector3 d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0f; // evita che si inclini quando va su celle con altezza diversa
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null; // aspetta il prossimo frame
		}

		// non mi fido delle cose belline e curvose quindi alla fine forzo nella posizione corretta
		transform.localPosition = location.Position;
		orientation = transform.localRotation.eulerAngles.y;

		ListPool<HexCell>.Add(pathToTravel);
		pathToTravel = null;
	}

	//fa guardare l'unità verso un punto, si occupa di tutti i cambiamenti necessari
	IEnumerator LookAt(Vector3 point)
	{
		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

		float angle = Quaternion.Angle(fromRotation, toRotation);
		if (angle > 0) 
		{// gira solo se serve 
		float speed = rotationSpeed / angle;
		for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
		{
			transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
			yield return null;
		}

		transform.LookAt(point);
		orientation = transform.localRotation.eulerAngles.y;
		}
	}


	//dice se la destinazione è una casella valida
	public virtual bool IsValidDestination(HexCell cell)
	{
		return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
	}

	//aggiusta la posizione quando una cella viene editata
	public void ValidateLocation()
	{
		transform.localPosition = location.Position;
	}

	public void Die()
	{
		if (location)
		{
			Grid.DecreaseVisibility(location, this);
		}
		location.Unit = null;
		Destroy(gameObject);
	}

	public virtual int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
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
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost +=
				toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
		}
		return moveCost;
	}

	public virtual int GetVisionCost(HexCell firstCell ,HexCell fromCell, HexCell toCell, HexDirection direction)
    {
		int visionCost=1;
		if (firstCell.Elevation < toCell.ViewElevation)
		{
			visionCost=-1;
		}

		return visionCost;
    }

	public virtual void Save(BinaryWriter writer)
	{
		location.coordinates.Save(writer);
		writer.Write(orientation);
		writer.Write((byte)prefabIndex);
		Debug.Log("Saving: " + prefabIndex);
	}

	public static void Load(BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		int i = reader.ReadByte();
		grid.AddUnit(i, grid.GetCell(coordinates), orientation);
	}
}
