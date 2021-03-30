﻿using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;



public class HexUnit : MonoBehaviour
{
	public HexCell Location
	{
		get {
			return location;
		}
		set {
			if (location)
			{
				location.Unit = null;
			}
			location = value;
			value.Unit = this;
			transform.localPosition = value.Position;
		}
	}
	HexCell location;

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
	float orientation;

	public static HexUnit unitPrefab; //instanziato nell'Awake di HexGrid e in OnEnable

	List<HexCell> pathToTravel;
	const float travelSpeed = 3f;
	const float rotationSpeed = 180f;
	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //

	void OnEnable()
	{
		if (location)
		{
			transform.localPosition = location.Position;
		}
	}	


	public void Travel(List<HexCell> path)
	{
		Location = path[path.Count - 1];
		pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	IEnumerator TravelPath()
	{//è una coroutine così più unità possono muoversi contemporaneamente
		Vector3 a, b, c = pathToTravel[0].Position;
		transform.localPosition = c;
		yield return LookAt(pathToTravel[1].Position); // prima di muoversi si gira dalla parte giusta
		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++)
		{
			a = c;
			b = pathToTravel[i - 1].Position;
			c = (b + pathToTravel[i].Position) * 0.5f;
			for (  ; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f; // evita che si inclini quando va su celle con altezza diversa
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null; //aspetta il prossimo frame
			}
			t -= 1f;
		}

		a = c;
		b = pathToTravel[pathToTravel.Count - 1].Position;
		c = b;
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
	public bool IsValidDestination(HexCell cell)
	{
		return !cell.IsUnderwater && !cell.Unit;
	}

	//aggiusta la posizione quando una cella viene editata
	public void ValidateLocation()
	{
		transform.localPosition = location.Position;
	}

	public void Die()
	{
		location.Unit = null;
		Destroy(gameObject);
	}





	public void Save(BinaryWriter writer)
	{
		location.coordinates.Save(writer);
		writer.Write(orientation);
	}

	public static void Load(BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();

		grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
	}
}