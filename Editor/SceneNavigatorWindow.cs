namespace SceneNavigator
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Profiling;
	using Random = System.Random;

	internal class SceneNavigatorWindow : EditorWindow
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

		private class Styles
		{
			public readonly GUIStyle miniButtonLeftAlign = new GUIStyle(EditorStyles.miniButton)
			{
				alignment = TextAnchor.MiddleLeft,
				margin = new RectOffset(0,0,0,0)
			};

			public readonly GUIStyle centeredGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};

			public readonly GUIStyle box = new GUIStyle("Box");
		}

		private static Styles _styles;
		private static Styles styles => _styles ?? (_styles = new Styles());

		private ICollection<string> _allTags;
		private readonly HashSet<string> _activeTags = new HashSet<string>();

		private SceneNavigatorFilterMode _tagFilterMode = SceneNavigatorFilterMode.MatchAnyTag;
		private void OnGUI()
		{
			Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(OnGUI)}");
			//TODO: Save active tags to editorprefs

			if (_filteredObjects.Any(x=> x == null) || _rebuildFilter)
			{
				Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(RebuildFilterCollection)}");
				RebuildFilterCollection();
				Profiler.EndSample();

				_rebuildFilter = false;
			}

			using (new GUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width*.25f)))
				{
					Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(DrawTags)}");
					DrawTags();
					Profiler.EndSample();
				}
				using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
				{
					Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(DrawObjects)}");
					DrawObjects();
					Profiler.EndSample();
				}
			}

			Profiler.EndSample();
		}

		private void DrawTags()
		{
			EditorGUILayout.LabelField("Tags", EditorStyles.centeredGreyMiniLabel);
			using (var scrollScope = new GUILayout.ScrollViewScope(_tagsScrollPosition, styles.box))
			{
				_tagsScrollPosition = scrollScope.scrollPosition;

				if (_allTags.Count > 0)
				{
					foreach (var tag in _allTags)
					{
						bool contains = _activeTags.Contains(tag);

						using (new GUIColorScope(GetSeededColor(tag, contains ? 1f : .5f)))
						{
							var newContains = GUILayout.Toggle(contains, tag, styles.miniButtonLeftAlign);
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
						}
					}
				}
				else
				{
					GUILayout.Label("(empty)", styles.centeredGreyMiniLabel);
				}
			}

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				DrawToggle(SceneNavigatorFilterMode.MatchAnyTag,  "Match Any");
				DrawToggle(SceneNavigatorFilterMode.MatchAllTags, "Match All");

				void DrawToggle(SceneNavigatorFilterMode filterMode, string text)
				{
					bool val    = _tagFilterMode == filterMode;
					bool newVal = GUILayout.Toggle(val, text, EditorStyles.toolbarButton);
					if (!val && newVal)
					{
						_tagFilterMode = filterMode;
						_rebuildFilter = true;
					}
				}
			}
		}

		private void DrawObjects()
		{
			Object isolateTarget = default;

			using (var scrollScope = new EditorGUILayout.ScrollViewScope(_objectsScrollPosition, styles.box))
			{
				_objectsScrollPosition = scrollScope.scrollPosition;

				if (_filteredObjects.Count == 0)
				{
					GUILayout.Label("(empty)", styles.centeredGreyMiniLabel);
				}
				else
				{
					foreach (var obj in _filteredObjects)
					{
						Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(DrawEntry)}");
						if (DrawEntry(obj))
						{
							isolateTarget = obj;
						}

						Profiler.EndSample();
					}
				}
			}

			if (isolateTarget)
			{
				IsolateTarget(isolateTarget);
			}
		}

		private void RebuildFilterCollection()
		{
			_allTags = _activeProvider.GetAllTags();

			_filteredObjects.Clear();
			foreach (var obj in _activeProvider.GetObjects(_activeTags.ToList(), _tagFilterMode))
			{
				if (obj)
				{
					_filteredObjects.Add(obj);
				}
			}
		}

		private bool DrawEntry(Object obj)
		{
			float colorMultiplier;

			float ColorMultiplier(GameObject go)
			{
				return go.activeInHierarchy ? 1 : .5f;
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

			using (new GUIColorScope(GetSeededColor(obj.name, colorMultiplier)))
			{
				return GUILayout.Button(obj.name, styles.miniButtonLeftAlign);
			}
		}

		private Color GetSeededColor(string text, float baseMultiplier = 1f)
		{
			const float strength = .3f;
			float inverseStrength = (1 - strength) * baseMultiplier;
			if (!_colorDataCache.TryGetValue(text, out var colorVec))
			{
				Random rnd = new Random(text.GetHashCode());
				_colorDataCache[text] = colorVec = new Vector3(rnd.Next(255), rnd.Next(255), rnd.Next(255)).normalized * strength;
			}

			return new Color(inverseStrength + colorVec.x, inverseStrength + colorVec.y, inverseStrength + colorVec.z);
		}

		private void IsolateTarget(Object target)
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

		private readonly Dictionary<string, Vector3> _colorDataCache = new Dictionary<string, Vector3>();

		private class GUIColorScope : GUI.Scope
		{
			private readonly Color _existingColor;

			public GUIColorScope(Color color)
			{
				_existingColor = GUI.color;
				GUI.color      = color;
			}

			protected override void CloseScope()
			{
				GUI.color = _existingColor;
			}
		}
	}
}
