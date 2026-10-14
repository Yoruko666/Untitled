using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    /// <summary>Targeted Play Mode traversal of the imported cave using the real PlayerEntity input path.</summary>
    [InitializeOnLoad]
    internal static class AdventureCavePlayCheck
    {
        private const string RequestKey = "AdventureCave.RouteCheck";
        private static readonly string[][] RouteIds = {
            new[] { "A_01_02","A_02_O1","A_O1_Entry","A_Entry_14","A_14_08","A_08_09","Final_09_10" },
            new[] { "B_01_15","B_15_11","B_11_12_SupplyBridge","B_12_09","Final_09_10" },
            new[] { "C_01_03","C_03_09","Final_09_10" }
        };
        private static AdventureCaveSession session;
        private static List<Vector3> targets;
        private static int route, waypoint, recoveries;
        private static double started, lastProgress;
        private static float bestDistance;
        private static JArray results;

        static AdventureCavePlayCheck()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RequestKey,false))
                    EditorApplication.delayCall += Begin;
            };
        }

        [MenuItem("Untitled/Adventure/Check A B C Routes in Play Mode")]
        private static void Request()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start this check from Edit Mode.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Adventure.unity")
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                    throw new InvalidOperationException("Save the current scene before checking Adventure.");
                EditorSceneManager.OpenScene("Assets/Scenes/Adventure.unity",OpenSceneMode.Single);
            }
            AdventureCaveImporter.Validate();
            SessionState.SetBool(RequestKey,true);
            EditorApplication.isPlaying = true;
        }

        private static void Begin()
        {
            SessionState.SetBool(RequestKey,false);
            session = Object.FindObjectOfType<AdventureCaveSession>();
            if (session == null) return;
            Application.runInBackground = true;
            results = new JArray(); route = 0;
            StartRoute();
            EditorApplication.update += Tick;
        }

        private static void StartRoute()
        {
            session.TeleportToZone(0);
            recoveries = session.RecoveryCount;
            Transform[] markers = session.ZoneMarkers;
            Vector3 y = (markers[1].position-markers[0].position)/16f;
            Vector3 x = (markers[12].position-markers[0].position-y*12f)/35f;
            Vector3 z = (markers[13].position-markers[0].position-x*6f-y*57f)/9f;
            Vector3 origin = markers[0].position+y*54f;
            Vector3 Convert(JToken p) => origin+x*(float)p[0]+y*(float)p[1]+z*(float)p[2];
            JArray paths = (JArray)JObject.Parse(File.ReadAllText("Assets/Art/Models/GoblinCave/layout_manifest.json"))["paths"];
            targets = new List<Vector3>();
            foreach (string id in RouteIds[route])
            {
                JObject path = (JObject)paths.First(p => (string)p["id"] == id);
                if (path["a"].Type == JTokenType.Integer) targets.Add(markers[(int)path["a"]-1].position);
                targets.AddRange(((JArray)path["p"]).Select(Convert));
                if (path["b"].Type == JTokenType.Integer) targets.Add(markers[(int)path["b"]-1].position);
            }
            waypoint = 0; bestDistance = float.PositiveInfinity;
            started = lastProgress = EditorApplication.timeSinceStartup;
            Write("running");
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || session == null)
            {
                EditorApplication.update -= Tick;
                return;
            }
            Vector3 position = session.Player.transform.position;
            Vector3 delta = targets[waypoint]-position; delta.y = 0;
            if (delta.magnitude < .18f)
            {
                if (Mathf.Abs(position.y-targets[waypoint].y)>.65f) { FinishRoute(false,"Wrong platform height"); return; }
                waypoint++; bestDistance = float.PositiveInfinity; lastProgress = EditorApplication.timeSinceStartup;
                if (waypoint == targets.Count) { FinishRoute(true,"Reached zone 10"); return; }
                delta = targets[waypoint]-position; delta.y = 0;
            }
            if (delta.magnitude < bestDistance-.025f)
            {
                bestDistance = delta.magnitude; lastProgress = EditorApplication.timeSinceStartup;
            }
            if (session.RecoveryCount != recoveries) { FinishRoute(false,"Fell into abyss"); return; }
            if (EditorApplication.timeSinceStartup-lastProgress>5) { FinishRoute(false,"Movement blocked at waypoint " + waypoint); return; }
            Transform camera = session.FollowCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
            Vector3 direction = delta.normalized;
            // Near endpoints, reduce speed instead of overshooting on an editor frame.
            float intensity = Mathf.Clamp(delta.magnitude/.45f,.2f,1f);
            InputManager.Instance.MoveInput = new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward))*intensity;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void FinishRoute(bool passed,string message)
        {
            InputManager.Instance.MoveInput = Vector2.zero;
            results.Add(new JObject {
                ["route"] = "ABC"[route].ToString(), ["passed"] = passed, ["message"] = message,
                ["waypoint"] = waypoint, ["position"] = new JArray(session.Player.transform.position.x,session.Player.transform.position.y,session.Player.transform.position.z),
                ["seconds"] = EditorApplication.timeSinceStartup-started
            });
            Debug.Log($"[AdventureCave Play] {"ABC"[route]}: {message}");
            route++;
            if (route < RouteIds.Length) { StartRoute(); return; }
            Write("complete");
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
        }

        private static void Write(string state)
        {
            Directory.CreateDirectory("output/adventure-cave");
            File.WriteAllText("output/adventure-cave/play_routes.json",new JObject {
                ["state"] = state, ["activeRoute"] = route < 3 ? "ABC"[route].ToString() : "finished",
                ["results"] = results.DeepClone()
            }.ToString());
        }
    }
}
