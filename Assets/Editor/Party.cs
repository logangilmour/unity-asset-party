using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;


public class Party : EditorWindow {
		
	
	private AssetPartyRoot container;

	public WriteTable writes;
	
	
	
	[XmlRoot]
	public class AssetPartyRoot
	{
		
		public static AssetPartyRoot Read(){
			AssetPartyRoot container;
			var serializer = new XmlSerializer(typeof(AssetPartyRoot));
			FileStream stream = null;
			try{
				stream = new FileStream(Application.dataPath+"/party.xml", FileMode.Open);
				container = serializer.Deserialize(stream) as AssetPartyRoot;
				
			}
			catch (FileNotFoundException ex)
			{
				container = new AssetPartyRoot();
			}
			finally{
				if(stream!=null)stream.Close();
			}
			return container;
		}
		
		public void Write(){
		
			var serializer = new XmlSerializer(typeof(AssetPartyRoot));
			var stream = new FileStream(Application.dataPath+"/party.xml", FileMode.Create);
			serializer.Serialize(stream, this);
			stream.Close();
		}

		[XmlArray("AssetPaths"),XmlArrayItem("AssetPath")]
		public List<AssetSync> paths = new List<AssetSync>();
		
		public AssetPartyRoot ()
		{
			
		}
	}
	
	public class AssetSync{
		[XmlAttribute("localpath")]
		public string localPath = "";
		[XmlAttribute("remotepath")]
		public string remotePath = "";
		[XmlIgnore]
		public FileSystemWatcher watcher = null;
	}
	
	public Party(){
		
		container = AssetPartyRoot.Read();
		Debug.Log ("Class instantiated");
		
	}
	
	[MenuItem("Window/Asset Party")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow(typeof(Party));
	}

	void OnGUI()
	{
		int i=0;
		AssetSync toRemove = null;
		foreach(var a in container.paths){
			GUILayout.BeginHorizontal();
			
			GUILayout.Label ("File Path", EditorStyles.boldLabel);
			GUILayout.Label (a.remotePath);
			if (GUILayout.Button ("Find")) {
				a.remotePath = EditorUtility.OpenFolderPanel("Asset Path","","");
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Asset Directory Name",EditorStyles.boldLabel);
			a.localPath=GUILayout.TextField(a.localPath,GUILayout.Width(100));
			if (GUILayout.Button ("Remove")) {
				toRemove = a;
				
			}
				
			GUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}
		if(toRemove!=null){
			container.paths.Remove(toRemove);
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("Add")) {
			container.paths.Add (new AssetSync());
		}
		if(GUILayout.Button ("Update")){
			Changes ();
		}
		if(GUILayout.Button ("Clean")){
			writes = new WriteTable();
		}
		
	}
	
	public static string MakeRelativePath(string fromPath, string toPath)
	{
		
		Uri fromUri = new Uri(fromPath);
		Uri toUri = new Uri(toPath);
		
		if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.
		
		Uri relativeUri = fromUri.MakeRelativeUri(toUri);
		String relativePath = Uri.UnescapeDataString(relativeUri.ToString());
		
		if (toUri.Scheme.ToUpperInvariant() == "FILE")
		{
			relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
		
		return relativePath;
	}
	
	private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
	{
		// Get the subdirectories for the specified directory.
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);
		DirectoryInfo[] dirs = dir.GetDirectories();
		
		if (!dir.Exists)
		{
			throw new DirectoryNotFoundException(
				"Source directory does not exist or could not be found: "
				+ sourceDirName);
		}
		
		// If the destination directory doesn't exist, create it. 
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}
		
		// Get the files in the directory and copy them to the new location.
		FileInfo[] files = dir.GetFiles();
		
		foreach (FileInfo file in files)
		{
			if(file.Extension==".meta")continue;
			if(writes.ContainsKey(file.FullName) && file.LastWriteTime.Ticks<=Convert.ToInt64(writes[file.FullName])){
				continue;
			}
			writes[file.FullName]=file.LastWriteTime.Ticks.ToString();
			
			Debug.Log ("Copying File "+file.FullName);
			string temppath = Path.Combine(destDirName, file.Name);
			
			file.CopyTo(temppath, true);
		}
		
		// If copying subdirectories, copy them and their contents to new location. 
		if (copySubDirs)
		{
			foreach (DirectoryInfo subdir in dirs)
			{
				string temppath = Path.Combine(destDirName, subdir.Name);
				DirectoryCopy(subdir.FullName, temppath, copySubDirs);
			}
		}
	}
	
	void Changes(){
		if(writes==null){
			Debug.Log("Making lookup");
			writes = new WriteTable();
		}
		
		foreach(var a in container.paths){
			EditorApplication.LockReloadAssemblies();
			DirectoryCopy (a.remotePath,Application.dataPath+"/"+a.localPath,true);
			EditorApplication.UnlockReloadAssemblies();
			Debug.Log ("Path: "+a.remotePath);
			
			//AssetDatabase.ImportAsset("Assets/"+a.localPath+"/",ImportAssetOptions.ImportRecursive);
			//
		}
		container.Write();

		AssetDatabase.Refresh();

	}

}

