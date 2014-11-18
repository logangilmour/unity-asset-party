using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class WriteTable : Hashtable, ISerializationCallbackReceiver
{
	[SerializeField]
	private List<string> keys = new List<string>();
	
	[SerializeField]
	private List<string> values = new List<string>();
	
	// save the dictionary to lists
	public void OnBeforeSerialize()
	{
	
		keys.Clear();
		values.Clear();
		foreach(DictionaryEntry pair in this)
		{
			keys.Add(pair.Key as string);
			values.Add(pair.Value as string);
		}
	}
	
	// load dictionary from lists
	public void OnAfterDeserialize()
	{
		this.Clear();
		
		
		if(keys.Count != values.Count)
			Debug.Log("Bad hashtable serialized somehow: key count of "+keys.Count+" different than value count of "+values.Count+".");
		
		for(int i = 0; i < keys.Count; i++){
			this.Add(keys[i], values[i]);
			}
	}
}

