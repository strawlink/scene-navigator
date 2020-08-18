namespace SceneNavigator
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using Debug = UnityEngine.Debug;
	using Object = UnityEngine.Object;

	public class SceneNavigatorWindow : EditorWindow
	{

		[MenuItem("Window/General/Scene Navigator")]
		public static void OpenWindow()
		{
			GetWindow<SceneNavigatorWindow>("Scene Navigator");
		}

		private readonly HashSet<Object> _filteredObjects = new HashSet<Object>();
		private bool _rebuildFilter = false;

		private ISceneNavigatorProvider _activeProvider;
		private Vector2 _objectsScrollPosition = Vector2.zero;
		private Vector2 _tagsScrollPosition = Vector2.zero;

		private void OnEnable()
		{
			_activeProvider = SceneNavigatorSettings.provider;
			_activeProvider.onCollectionChanged += SetRebuildFilter;
			SetRebuildFilter();
		}

		private void OnDisable()
		{
			_activeProvider.onCollectionChanged -= SetRebuildFilter;
		}

		private void SetRebuildFilter()
		{
			_rebuildFilter = true;
			Repaint();
		}

		private const string TagsTab = "Tags";
		private const string ObjectTab = "Objects";

		private readonly string[] _tabs = new[] {TagsTab, ObjectTab};
		private string _activeTab = ObjectTab;

		private class Styles
		{
			public readonly GUIStyle miniButtonLeftAlign = new GUIStyle(EditorStyles.miniButton)
			{
				alignment = TextAnchor.MiddleLeft,
				margin = new RectOffset(0,0,0,0)
			};
			public readonly GUIStyle tagsToggle = new GUIStyle(EditorStyles.miniButton)
			{

				alignment = TextAnchor.MiddleLeft,
				margin = new RectOffset(0,0,0,0)
			};
		}

		private static Styles _styles;
		private static Styles styles => _styles ?? (_styles = new Styles());

		private IEnumerable<string> _allTags;
		private readonly HashSet<string> _activeTags = new HashSet<string>();
		private void OnGUI()
		{
			//TODO: Save filter in editorprefs

			// using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			// {
			// 	foreach (var tab in _tabs)
			// 	{
			// 		if (GUILayout.Toggle(tab == _activeTab, tab, EditorStyles.toolbarButton))
			// 		{
			// 			_activeTab = tab;
			// 			_rebuildFilter = true;
			// 		}
			// 	}
			// }

			if (_filteredObjects.Any(x=> x == null) || _rebuildFilter)
			{
				RebuildFilterCollection();
				_rebuildFilter = false;
			}

			// switch (_activeTab)
			// {
			// 	case TagsTab:
			// 		break;
			// 	case ObjectTab:
			// 		break;
			// }

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(100)))
				{
					DrawTagsTab();
				}
				using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
				{
					DrawObjectsTab();
				}
			}

			void DrawTagsTab()
			{
				EditorGUILayout.LabelField("Tags", EditorStyles.centeredGreyMiniLabel);
				using (var scrollScope = new EditorGUILayout.ScrollViewScope(_tagsScrollPosition, "Box", GUILayout.ExpandHeight(true)))
				{
					_tagsScrollPosition = scrollScope.scrollPosition;

					if (_allTags.Any())
					{
						foreach (var tag in _allTags)
						{
							var col = GUI.color;
							bool contains = _activeTags.Contains(tag);
							GUI.color = GetSeededColor(tag, contains ? 1f : .5f);
							// using (new EditorGUILayout.HorizontalScope())
							// {

								var newContains = GUILayout.Toggle(contains, tag, styles.tagsToggle);
								if (contains && !newContains)
								{
									_activeTags.Remove(tag);
									_rebuildFilter = true;
								}
								else if (!contains && newContains)
								{
									_activeTags.Add(tag);
									_rebuildFilter = true;
								}
							// }

							GUI.color = col;
						}
					}
					else
					{
						EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
					}
				}
			}

			void DrawObjectsTab()
			{
				Object isolateTarget = default;

				using (var scrollScope = new EditorGUILayout.ScrollViewScope(_objectsScrollPosition, "Box", GUILayout.ExpandHeight(true)))
				{
					_objectsScrollPosition = scrollScope.scrollPosition;

					if (_filteredObjects.Count == 0)
					{
						EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
					}
					else
					{
						foreach (var obj in _filteredObjects)
						{
							if (DrawEntry(obj))
							{
								isolateTarget = obj;
							}
						}
					}
				}

				if (isolateTarget)
				{
					IsolateTarget(isolateTarget);
				}
			}

			// DrawFooter();

			void RebuildFilterCollection()
			{
				_allTags = _activeProvider.GetAllTags();

				_filteredObjects.Clear();
				foreach (var tag in _activeTags)
				{
					foreach (var obj in _activeProvider.GetObjects(tag))
					{
						if (obj)
						{
							_filteredObjects.Add(obj);
						}
					}
				}
			}

			bool DrawEntry(Object obj)
			{
				var col = GUI.color;
				float colorMultiplier;

				float ColorMultiplier(GameObject go)
				{
					return go.activeInHierarchy ? 1 : go.activeSelf ? .7f : .5f;
				}
				switch (obj)
				{
					case GameObject go:
						colorMultiplier = ColorMultiplier(go);
						break;
					case Component comp:
						colorMultiplier = ColorMultiplier(comp.gameObject);
						break;
					default:
						colorMultiplier = 1f;
						break;
				}
				GUI.color = GetSeededColor(obj.name, colorMultiplier);
				bool clicked = GUILayout.Button(obj.name, styles.miniButtonLeftAlign);
				GUI.color = col;
				return clicked;
			}

			// void DrawFooter()
			// {
			// 	using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			// 	{
			// 		EditorGUILayout.LabelField(_activeProvider.GetType().Name, EditorStyles.centeredGreyMiniLabel);
			// 		GUILayout.Button("Rebuild", EditorStyles.miniButton);
			// 	}
			// }

			Color GetSeededColor(string text, float baseMultiplier = 1f)
			{
				System.Random rnd = new System.Random(text.GetHashCode());
				const float strength = .3f;
				float inverseStrength = (1 - strength) * baseMultiplier;
				Vector3 v3 = new Vector3(rnd.Next(255), rnd.Next(255), rnd.Next(255)).normalized * strength;
				return new Color(inverseStrength + v3.x, inverseStrength + v3.y, inverseStrength + v3.z);
			}

			void IsolateTarget(Object target)
			{
				GameObject isolateGo;
				switch (target)
				{
					case GameObject go:
						isolateGo = go;
						break;
					case Component component:
						isolateGo = component.gameObject;
						break;
					default:
						Debug.LogWarning($"Unable to isolate type '{target.GetType()}'", target);
						return;
				}

				SceneVisibilityManager.instance.Isolate(isolateGo, true);
				Selection.activeObject = target;
			}
		}
	}
}
