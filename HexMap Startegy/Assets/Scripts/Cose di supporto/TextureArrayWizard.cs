using UnityEditor;
using UnityEngine;

//le textures devoo avere tutte lo stesso formato (altezza, larghezza, formato, mipmaps)
public class TextureArrayWizard : ScriptableWizard
{
	public Texture2D[] textures;



	[MenuItem( "Assets/Create/Texture Array" )] //permette di accedervi dall'editor
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create"); //apre il wizard
		//le stringhe sono nome del wizard e cosa scrivere sul bottone
	}

	//chiamato quando do la conferma al wizard premendo il bottone
	void OnWizardCreate()
	{
		if (textures.Length == 0)
		{
			return;
		}

		//apre una finestra per salvare nel progetto. Ritorna il path di dove va il file, stringa vuota se annullo
		string path = EditorUtility.SaveFilePanelInProject(
			"Save Texture Array", "Texture Array", "asset", "Save Texture Array"
		);//le stringhe sono: nome finestra, nome file salvato, estensione file, descrizione

		if (path.Length == 0)
		{
			return;
		}

		//le textures devoo avere tutte lo stesso formato (altezza, larghezza, formato, mipmaps)
		Texture2D t = textures[0];
		Texture2DArray textureArray = new Texture2DArray(
			t.width, t.height, textures.Length, t.format, t.mipmapCount > 1
		);
		//configuro basandomi sulla prima, le altre devono essere uguali
		textureArray.anisoLevel = t.anisoLevel;
		textureArray.filterMode = t.filterMode;
		textureArray.wrapMode = t.wrapMode;

		for (int i = 0; i < textures.Length; i++)
		{
			for (int m = 0; m < t.mipmapCount; m++)
			{
				Graphics.CopyTexture( textures[i], 0, m, textureArray, i, m );
			}
		}

		AssetDatabase.CreateAsset( textureArray, path );
	}


}