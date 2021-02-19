using UnityEngine;

// valori numerici sulle dimensioni dell'esagono
public static class HexMetrics
{
	public const float outerRadius = 10f;
	public const float innerRadius = outerRadius * 0.866025404f;

//posizione dei vertici in relazione al centro
	public static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius) //serve per non uscire dal vettore se faccio for, guale al primo
	};
}
