using UnityEngine;

// valori numerici sulle dimensioni dell'esagono
public static class HexMetrics
{
	//dimensioni esagoni
	public const float outerToInner = 0.866025404f; //serve per conversione
	public const float innerToOuter = 1f / outerToInner; //serve per conversione
	public const float outerRadius = 10f;
	public const float innerRadius = outerRadius * outerToInner;
	public const float elevationStep = 4f;

	//per il terrazzamento
	public const int terracesPerSlope = 2;
	public const int terraceSteps = terracesPerSlope * 2 + 1;
	public const float horizontalTerraceStepSize = 1f / terraceSteps;
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

	//divido la mappa in chunks per farla più grande
	public const int chunkSizeX = 6, chunkSizeZ = 6; //dimensione di un chunks

	//servono per il blending dei colori
	public const float solidFactor = 0.8f;  // 1= no bordo
	public const float blendFactor = 1f - solidFactor;

	// roba del rumore
	public static Texture2D noiseSource; //serve per rumore sui vertici
	public const float cellPerturbStrength = 3f; // rumore orizzontale, 0 per non fare rumore (moltiplica il rumore)
	public const float noiseScale = 0.004f; //numero piccolo da meno variazioni sulla distanza (iniziale  0.003)
	public const float elevationPerturbStrength = 0f; //rumore sull'altezza della cella (0 non fa niente)

	//Acqua
	//lo scorrere del fiume è fatto nello shader River.
	//per il colore devo guardare RiverShaderMaterial
	public const float streamBedElevationOffset = -1.2f; //profondità letto del fiume, 0 non c'è
	public const float waterElevationOffset = -0.3f; //livello dell'acqua, 0 è la superficie
	public const float waterFactor = 0.6f; //come il solid factor ma per il bordo con le ondine dell'acqua, minore del solid factor
	public const float waterBlendFactor = 1f - waterFactor;

	//posizione dei vertici in relazione al centro
	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius) //serve per non uscire dal vettore se faccio for, guale al primo
	};


	// cose casuali prefatte, quindi costanti
	public const int hashGridSize = 256;
	static HexHash[] hashGrid;
	public const float hashGridScale = 0.25f; //un valore piccolo mi fa muovere piano nella griglia
	public static void InitializeHashGrid(int seed)
	{
		hashGrid = new HexHash[hashGridSize * hashGridSize];
		Random.State currentState = Random.state; //cosi il resto non segue questo seed
		Random.InitState(seed);
		for (int i = 0; i < hashGrid.Length; i++) {
			hashGrid[i] = HexHash.Create();
		}
		Random.state = currentState;//cosi il resto non segue questo seed
	}

	static float[][] featureThresholds = { //dale probabilita di scelta delle features
		new float[] {0.0f, 0.0f, 0.5f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.9f}
	};


	/// ////////////////////////////////////////////////////////


	public static Vector3 GetFirstCorner(HexDirection direction)
	{
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner(HexDirection direction)
	{
		return corners[(int)direction + 1];
	}


	public static Vector3 GetFirstSolidCorner(HexDirection direction)
	{
		return corners[(int)direction] * solidFactor;
	}

	public static Vector3 GetSecondSolidCorner(HexDirection direction)
	{
		return corners[(int)direction + 1] * solidFactor;
	}

	public static Vector3 GetFirstWaterCorner(HexDirection direction)
	{
		return corners[(int)direction] * waterFactor;
	}

	public static Vector3 GetSecondWaterCorner(HexDirection direction)
	{
		return corners[(int)direction + 1] * waterFactor;
	}

	public static Vector3 GetBridge(HexDirection direction)
	{
		return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
	}

	public static Vector3 GetWaterBridge(HexDirection direction)
	{
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			waterBlendFactor;
	}

	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp(Color a, Color b, int step)
	{
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	//da il tipo di connessione per fare la mesh
	public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
	{
		return
			(corners[(int)direction] + corners[(int)direction + 1]) *
			(0.5f * solidFactor);
	}

	public static Vector4 SampleNoise(Vector3 position)
	{
		return noiseSource.GetPixelBilinear(position.x * noiseScale,position.z * noiseScale);
	}

	//serve a applicare rumore
	public static Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		//		position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength; commento per mantenere le cose piatte
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}

	//da valore casuale (fisso e genrato all'inizio) tra 0 e 1 in base alle componenti x,z
	public static HexHash SampleHashGrid(Vector3 position)
	{
		int x = (int)(position.x * hashGridScale) % hashGridSize;
		if (x < 0) {
			x += hashGridSize;
		}
		int z = (int)(position.z * hashGridScale) % hashGridSize;
		if (z < 0) {
			z += hashGridSize;
		}
		return hashGrid[x + z * hashGridSize];
	}

	public static float[] GetFeatureThresholds(int level)
	{
		return featureThresholds[level];
	}



}
