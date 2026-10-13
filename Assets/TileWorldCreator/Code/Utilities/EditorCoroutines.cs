/*

  _____ _ _    __        __         _     _  ____                _
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|

	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace GiantGrey.TileWorldCreator.Utilities {

	public static class EditorCoroutines
	{

		public class Coroutine
		{
			public IEnumerator enumerator;
			public System.Action<bool> OnUpdate;
			public List<IEnumerator> history = new List<IEnumerator> ();
			public float waitTime = -1;
		}

		static readonly List<Coroutine> coroutines = new List<Coroutine> ();
		static readonly List<Coroutine> toAdd = new List<Coroutine>();
		static bool isUpdating;

		public static void Execute (IEnumerator enumerator, System.Action<bool> OnUpdate = null)
		{
			var coroutine = new Coroutine { enumerator = enumerator, OnUpdate = OnUpdate };
			if (isUpdating)
			{
				toAdd.Add(coroutine);
			}
			else
			{
				if (coroutines.Count == 0)
				{
					EditorApplication.update += Update;
				}
				coroutines.Add (coroutine);
			}
		}

		static void Update ()
		{
			isUpdating = true;
			for (int i = 0; i < coroutines.Count; i++)
			{
				var coroutine = coroutines[i];

				if (coroutine.waitTime > 0)
				{
					if (EditorApplication.timeSinceStartup < coroutine.waitTime)
					{
						continue;
					}
					else
					{
						coroutine.waitTime = -1;
					}
				}

				bool done = false;
				while (true)
				{
					done = !coroutine.enumerator.MoveNext();
					if (done)
					{
						if (coroutine.history.Count == 0)
						{
							coroutines.RemoveAt(i);
							i--;
							break;
						}
						else
						{
							done = false;
							coroutine.enumerator = coroutine.history[coroutine.history.Count - 1];
							coroutine.history.RemoveAt(coroutine.history.Count - 1);
							// Continue loop to MoveNext on parent immediately
						}
					}
					else
					{
						if (coroutine.enumerator.Current is IEnumerator)
						{
							coroutine.history.Add(coroutine.enumerator);
							coroutine.enumerator = (IEnumerator)coroutine.enumerator.Current;
							// Continue loop to MoveNext on child immediately
						}
						else if (coroutine.enumerator.Current is UnityEngine.WaitForSeconds waitForSeconds)
						{
							// Use reflection to get duration as it is private
							var field = typeof(UnityEngine.WaitForSeconds).GetField("m_Seconds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
							float seconds = field != null ? (float)field.GetValue(waitForSeconds) : 0;
							coroutine.waitTime = (float)EditorApplication.timeSinceStartup + seconds;
							break;
						}
						else
						{
							// yield return null or something else, wait for next frame
							break;
						}
					}
				}

				if (coroutine.OnUpdate != null) coroutine.OnUpdate (done);
			}
			isUpdating = false;

			if (toAdd.Count > 0)
			{
				if (coroutines.Count == 0 && toAdd.Count > 0)
				{
					EditorApplication.update += Update;
				}
				coroutines.AddRange(toAdd);
				toAdd.Clear();
			}

			if (coroutines.Count == 0) EditorApplication.update -= Update;
		}

		internal static void StopAll ()
		{
			coroutines.Clear ();
			toAdd.Clear();
			EditorApplication.update -= Update;
		}

	}
}
#endif
