

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Internal.Scripts.MapScripts.Editor
{
	[CustomEditor (typeof(LevelCampaign))]
	public class LevelsMapEditor : LevelsEditorBase {
		private LevelCampaign _levelCampaign;

		private float _width;
		private float _height;
		bool fixToggle;

		public void OnEnable () {
			_levelCampaign = target as LevelCampaign;
		}

		public override void OnInspectorGUI () {
			GUILayout.BeginVertical ();
			fixToggle = EditorGUILayout.Toggle ("fix map", fixToggle);
			if (!fixToggle) {
				if (_levelCampaign.IsGenerated) {
					DrawLevelsSettings ();
					DrawStarsSettings ();
					DrawMapCameraSettings ();

					DrawLevelClickSettings ();

					if (GUILayout.Button ("Clear all", GUILayout.MinWidth (120)) &&
					    EditorUtility.DisplayDialog ("Delete all?",
						    "Are you sure that you want to delete all levels map settings?", "Delete", "Cancel")) {
						Clear ();
					}
				} else {
					DrawGenerateDraft ();
				}
			} else {
				DrawDefaultInspector ();
			}
			GUILayout.Space (16);
			GUILayout.EndVertical ();

			EditorUtility.SetDirty (_levelCampaign);
		}

		private void DrawLevelsSettings () {
			GUILayout.BeginVertical ("Box");
			EditorGUILayout.LabelField ("General:");

			_levelCampaign.WaypointsMover.Speed = EditorGUILayout.FloatField ("Character speed", _levelCampaign.WaypointsMover.Speed);

			_levelCampaign.TranslationType = (TranslationType)EditorGUILayout.EnumPopup ("Translation type", _levelCampaign.TranslationType);

			if (_levelCampaign.WaypointsMover.Path == null)
				_levelCampaign.WaypointsMover.Path = _levelCampaign.transform.Find ("Path").GetComponent<Path> ();

			Path path = _levelCampaign.WaypointsMover.Path;
			path.IsCurved = EditorGUILayout.Toggle ("Curved", path.IsCurved);
			path.GizmosColor = EditorGUILayout.ColorField ("Gizmos Path Color", path.GizmosColor);
			path.GizmosRadius = EditorGUILayout.FloatField ("Gizmos Path Pivot Radius", path.GizmosRadius);

			GUILayout.EndVertical ();

			EditorUtility.SetDirty (path);
		}

		private void Clear () {
			while (_levelCampaign.transform.childCount > 0) {
				DestroyImmediate (_levelCampaign.transform.GetChild (0).gameObject);
			}
			_levelCampaign.IsGenerated = false;
			DisableScrolling ();
		}

		#region Generation

		private void DrawGenerateDraft () {
			GUILayout.BeginVertical ("Box");
			_levelCampaign.Count = EditorGUILayout.IntField ("Count", _levelCampaign.Count);
			_levelCampaign.MapLevelPrefab = EditorGUILayout.ObjectField ("Level prefab", _levelCampaign.MapLevelPrefab, typeof(MapLevel), false) as MapLevel;
			_levelCampaign.CharacterPrefab = EditorGUILayout.ObjectField ("Character prefab", _levelCampaign.CharacterPrefab, typeof(Transform), false) as Transform;
			GUILayout.EndVertical ();

			if (GUILayout.Button ("Generate draft", GUILayout.MinWidth (120))) {
				Generate ();
				_levelCampaign.IsGenerated = true;
				SetStarsEnabled (_levelCampaign, false);
			}
		}

		private void Generate () {
			InitBounds ();
			List<MapLevel> levels = GenerateLevels ();
			Path path = GeneratePath (levels);
			_levelCampaign.WaypointsMover = GenerateCharacter (path);
			_levelCampaign.WaypointsMover.transform.position = levels [0].transform.position;
		}

		private void InitBounds () {
			_height = Camera.main.orthographicSize * 2 * 0.9f;
			_width = _height * 1.33333f * 0.9f;
		}

		private List<MapLevel> GenerateLevels () {
			GameObject goLevels = new GameObject ("Levels");
			goLevels.transform.parent = _levelCampaign.transform;
			float[] points = DevideLineToPoints (_levelCampaign.Count);
			List<MapLevel> levels = new List<MapLevel> ();
			for (int i = 0; i < _levelCampaign.Count; i++) {
				MapLevel mapLevel = CreateMapLevel (points [i], i + 1);
				mapLevel.transform.parent = goLevels.transform;
				levels.Add (mapLevel);
			}
			UpdateLevelsNumber (levels);
			return levels;
		}

		private MapLevel CreateMapLevel (float point, int number) {
			Vector3 position;
			if (point < 1f / 3f)
				position = GetPosition (point * 3f, _width, 0, _height / 3f, 0);
			else if (point < 2f / 3f)
				position = GetPosition ((point - 1f / 3f) * 3f, -_width, _width, _height / 3f, _height / 3f);
			else
				position = GetPosition ((point - 2f / 3f) * 3f, _width, 0, _height / 3f, _height * 2f / 3f);
			return CreateMapLevel (position, number, _levelCampaign.MapLevelPrefab);
		}

		private Path GeneratePath (List<MapLevel> levels) {
			Path path = new GameObject ("Path").AddComponent<Path> ();
			path.IsCurved = false;
			path.GizmosRadius = Camera.main.orthographicSize / 40f;
			path.transform.parent = _levelCampaign.transform;
			UpdatePathWaypoints (levels);
			return path;
		}

		private Vector3 GetPosition (float p, float width, float xOffset, float height, float yOffset) {
			return new Vector3 (
				xOffset + p * width - _width / 2f,
				yOffset + p * height - _height / 2f,
				0f);
		}

		/// <summary>
		/// Devide [0,1] line to array of points.
		/// If count = 1, ret {0}
		/// If count = 2, ret {0, 1}
		/// If count = 3, ret {0, 0.5, 1}
		/// If count = 4, ret {0, 0.25, 0.25, 1}
		/// </summary>
		private float[] DevideLineToPoints (int pointsCount) {
			if (pointsCount <= 0)
				return new float[0];

			float[] points = new float[pointsCount];
			for (int i = 0; i < pointsCount; i++)
				points [i] = i * 1f / (pointsCount - 1);

			return points;
		}

		private WaypointsMover GenerateCharacter (Path path) {
			Transform character = PrefabUtility.InstantiatePrefab (_levelCampaign.CharacterPrefab) as Transform;
			character.transform.parent = _levelCampaign.transform;
			WaypointsMover waypointsMover = character.gameObject.AddComponent<WaypointsMover> ();
			waypointsMover.Path = path;
			waypointsMover.Speed = Camera.main.orthographicSize;
			return waypointsMover;
		}

		#endregion

		#region Stars

		private void DrawStarsSettings () {
			if (_levelCampaign.StarsEnabled) {
				if (GUILayout.Button ("Disable stars")) {
					SetStarsEnabled (_levelCampaign, false);
				} else {
					DrawEnableState ();
				}
			} else {
				if (GUILayout.Button ("Enable stars")) {
					SetStarsEnabled (_levelCampaign, true);
				}
			}
		}

		private void DrawEnableState () {
			GUILayout.BeginVertical ("Box");
			GUILayout.Label ("Stars enabled:");
			StarsType starsType = (StarsType)EditorGUILayout.EnumPopup ("Stars type", _levelCampaign.StarsType);
			if (starsType != _levelCampaign.StarsType)
				_levelCampaign.SetStarsType (starsType);
			GUILayout.EndVertical ();
		}

		#endregion

		#region Map camera

		private void DrawMapCameraSettings () {
			if (_levelCampaign.ScrollingEnabled) {
				if (GUILayout.Button ("Disable map scrolling"))
					DisableScrolling ();
				else
					DrawMapCameraBounds ();
			} else {
				if (GUILayout.Button ("Enable map scrolling"))
					EnableScrolling (Camera.main);
			}
		}

		private void EnableScrolling (Camera camera) {
			_levelCampaign.ScrollingEnabled = true;
			_levelCampaign.MapCamera = camera.gameObject.AddComponent<MapCamera> ();
			_levelCampaign.MapCamera.Camera = camera;
			_levelCampaign.MapCamera.Bounds.size = new Vector2 (camera.orthographicSize * 3f, camera.orthographicSize * 3f);
			EditorUtility.SetDirty (_levelCampaign);
		}

		private void DisableScrolling () {
			_levelCampaign.ScrollingEnabled = false;
			DestroyImmediate (_levelCampaign.MapCamera);
			EditorUtility.SetDirty (_levelCampaign);
		}

		private void DrawMapCameraBounds () {
			MapCamera mapCamera = _levelCampaign.MapCamera;

			GUILayout.BeginVertical ("Box");

			EditorGUILayout.LabelField ("Map bounds:");

			mapCamera.Bounds.center = new Vector3 (
				EditorGUILayout.FloatField ("Center X", mapCamera.Bounds.center.x),
				mapCamera.Bounds.center.y,
				mapCamera.Bounds.center.z);
			mapCamera.Bounds.center = new Vector3 (
				mapCamera.Bounds.center.x,
				EditorGUILayout.FloatField ("Center Y", mapCamera.Bounds.center.y),
				mapCamera.Bounds.center.z);
			mapCamera.Bounds.size = new Vector3 (
				EditorGUILayout.FloatField ("Size X", mapCamera.Bounds.size.x),
				mapCamera.Bounds.size.y,
				mapCamera.Bounds.size.z);
			mapCamera.Bounds.size = new Vector3 (
				mapCamera.Bounds.size.x,
				EditorGUILayout.FloatField ("Size Y", mapCamera.Bounds.size.y),
				mapCamera.Bounds.size.z);

			GUILayout.EndVertical ();
			if(!Application.isPlaying)
			{
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
				EditorUtility.SetDirty(mapCamera);
			}

			Camera camera = EditorGUILayout.ObjectField ("Map Camera", mapCamera.Camera, typeof(Camera), true) as Camera;
			if (camera != mapCamera.Camera) {
				if (camera == null) {
					DisableScrolling ();
				} else {
					Bounds bounds = mapCamera.Bounds;
					DisableScrolling ();
					EnableScrolling (camera);
					mapCamera = _levelCampaign.MapCamera;
					mapCamera.Bounds = bounds;
					EditorUtility.SetDirty (mapCamera);
				}
			}
		}

		#endregion

		#region Level selection confirmation

		private void DrawLevelClickSettings () {
			if (_levelCampaign.IsClickEnabled) {
				if (GUILayout.Button ("Disable levels click/tap")) {
					_levelCampaign.IsClickEnabled = false;
					EditorUtility.SetDirty (_levelCampaign);
				}
				DrawConfirmationSettings ();
			} else {
				if (GUILayout.Button ("Enable levels click/tap")) {
					_levelCampaign.IsClickEnabled = true;
					EditorUtility.SetDirty (_levelCampaign);
				}
			}
		}

		private void DrawConfirmationSettings () {
			GUILayout.BeginVertical ("Box");
			string helpString = "Level click/tap enabled.\n";

			if (_levelCampaign.IsConfirmationEnabled) {
				helpString +=
					"Confirmation enabled: Click/tap level on map and catch 'LevelsMap.LevelSelected' event. After confirmation call 'LevelsMap.GoToLevel' method.";
				GUILayout.Box (helpString);
				if (GUILayout.Button ("Disable confirmation")) {
					_levelCampaign.IsConfirmationEnabled = false;
					EditorUtility.SetDirty (_levelCampaign);
				}
			} else {
				helpString += "Confirmation disabled: Click/tap level on map for character moving to level.";
				GUILayout.Box (helpString);
				if (GUILayout.Button ("Enable confirmation")) {
					_levelCampaign.IsConfirmationEnabled = true;
					EditorUtility.SetDirty (_levelCampaign);
				}
			}

			GUILayout.EndVertical ();
		}

		#endregion
	}
}