using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class SaveLoadMenu : MonoBehaviour
{
	public Text actionButtonLabel;
	public InputField nameInput;

	public RectTransform listContent; //File List >> Viewport >> Content	

	public SaveLoadItem itemPrefab;

	public HexGrid hexGrid;
	bool saveMode;
	const int mapFileVersion = 4;

	public void Open(bool saveMode)
	{
		this.saveMode = saveMode;
		if (saveMode) {
			actionButtonLabel.text = "Save";
		}
		else {
			actionButtonLabel.text = "Load";
		}

		FillList();

		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	public void Action()
	{
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}
		if (saveMode) {
			Save(path);
		}
		else {
			Load(path);
		}
		Close();
	}

	public void SelectItem(string name)
	{
		nameInput.text = name;
	}

	string GetSelectedPath()
	{
		string mapName = nameInput.text;
		if (mapName.Length == 0)
		{
			return null;
		}
		return Path.Combine("Mappe", mapName + ".map");
	}

	void FillList()
	{
		for (int i = 0; i < listContent.childCount; i++)
		{// cancella le cose vecchie prima di mettere le nuove
			Destroy( listContent.GetChild( i ).gameObject );
		}

		string[] paths = Directory.GetFiles("Mappe", "*.map");
		Array.Sort(paths);

		for (int i = 0; i < paths.Length; i++) {
			SaveLoadItem item = Instantiate(itemPrefab);
			item.menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(listContent, false);
		}
	}

	public void Delete()
	{
		string path = GetSelectedPath();
		if (path == null)
		{
			return;
		}
		if (File.Exists( path ))
		{
			File.Delete( path );
		}
		nameInput.text = "";
		FillList();
	}

	public void Save(string path)
	{
		//string path = Path.Combine(Application.persistentDataPath, "test.map"); // lo sbatte in AppData

		using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
			writer.Write(mapFileVersion);  //header
			hexGrid.Save(writer);
		}
	}

	public void Load(string path)
	{
		//string path = Path.Combine(Application.persistentDataPath, "test.map");  // lo sbatte in AppData

		if (!File.Exists(path)) {
			Debug.LogError("File does not exist " + path);
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
			int header = reader.ReadInt32(); //legge header
			if (header <= mapFileVersion) 
			{// controlla che l'header sia valido
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}	



}
