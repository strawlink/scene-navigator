namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Profiling;
	using Object = UnityEngine.Object;
	using Random = System.Random;

	internal class SceneNavigatorWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private enum TagFilterMode
		{
			MatchAnyTag,
			MatchAllTags
		}

		private class Styles
		{
			public readonly GUIStyle miniButtonLeftAlign = new GUIStyle(EditorStyles.miniButton)
			{
				alignment = TextAnchor.MiddleLeft,
				margin    = new RectOffset(0, 0, 0, 0)
			};

			public readonly GUIStyle centeredGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};

			public readonly GUIStyle box = new GUIStyle("Box");
		}

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

		[MenuItem("Window/General/Scene Navigator")]
		public static void OpenWindow()
		{
			GetWindow<SceneNavigatorWindow>("Scene Navigator");
		}

		private readonly SortedSet<SceneObjectMetaData> _filteredObjects = new SortedSet<SceneObjectMetaData>();
		private bool _rebuildFilter = false;

		private ISceneNavigatorProvider _activeProvider;
		private Vector2 _objectsScrollPosition = Vector2.zero;
		private Vector2 _tagsScrollPosition = Vector2.zero;

		private void OnEnable()
		{
			_activeProvider = SceneNavigatorSettings.provider;
			_activeProvider.onCollectionChanged += SetRebuildFilter;
			SceneNavigatorSettings.onProviderChanged += OnProviderChanged;

			SetRebuildFilter();
			LoadActiveTags();
		}

		private void OnDisable()
		{
			_activeProvider.onCollectionChanged -= SetRebuildFilter;
			SceneNavigatorSettings.onProviderChanged -= OnProviderChanged;
		}

		private void OnProviderChanged(ISceneNavigatorProvider newProvider)
		{
			_activeProvider.onCollectionChanged -= SetRebuildFilter;
			newProvider.onCollectionChanged += SetRebuildFilter;
			SetRebuildFilter();
		}

		private void SetRebuildFilter()
		{
			_rebuildFilter = true;
			Repaint();
		}

		private static Styles _styles;
		private static Styles styles => _styles ?? (_styles = new Styles());

		private IReadOnlyDictionary<string,int> _tagData;
		private HashSet<string> _activeTags = new HashSet<string>();

		private const string EditorPrefsActiveTags = "SceneNavigator_ActiveTags";

		private void SaveActiveTags()
		{
			var serialized = string.Join(SerializationSeparatorStr, _activeTags);
			EditorPrefs.SetString(EditorPrefsActiveTags, serialized);
		}

		private const char SerializationSeparator = '\0';
		private const string SerializationSeparatorStr = "\0";

		private void LoadActiveTags()
		{
			try
			{
				var str = EditorPrefs.GetString(EditorPrefsActiveTags, "");
				if (!string.IsNullOrEmpty(str))
				{
					_activeTags = new HashSet<string>(str.Split(SerializationSeparator));
				}
			}
			catch
			{
				// ignored
			}
		}

		private TagFilterMode _tagFilterMode = TagFilterMode.MatchAnyTag;
		private void OnGUI()
		{
			Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(OnGUI)}");

			if (_filteredObjects.Any(x=> x.targetObject == null) || _rebuildFilter)
			{
				RebuildFilterCollection();

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

				if (_tagData.Count > 0)
				{
					foreach (var tag in _tagData)
					{
						bool contains = _activeTags.Contains(tag.Key);

						using (new GUIColorScope(GetSeededColor(tag.Key, contains ? 1f : .5f)))
						{
							var newContains = GUILayout.Toggle(contains, $"[{tag.Value}] {tag.Key}", styles.miniButtonLeftAlign);
							if (contains && !newContains)
							{
								_activeTags.Remove(tag.Key);
								_rebuildFilter = true;
							}
							else if (!contains && newContains)
							{
								_activeTags.Add(tag.Key);
								_rebuildFilter = true;
							}

							if (contains != newContains)
							{
								SaveActiveTags();
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
				DrawToggle(TagFilterMode.MatchAnyTag,  "Match Any");
				DrawToggle(TagFilterMode.MatchAllTags, "Match All");

				void DrawToggle(TagFilterMode filterMode, string text)
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
			SceneObjectMetaData isolateTarget = default;

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

			if (isolateTarget != default)
			{
				IsolateTarget(isolateTarget.targetObject);
			}
		}

		private void RebuildFilterCollection()
		{
			Profiler.BeginSample($"{nameof(SceneNavigatorWindow)}.{nameof(RebuildFilterCollection)}");

			_tagData = _activeProvider.tagData;

			_filteredObjects.Clear();

			// ActiveTags may contain old data => make sure we only get the tags that are valid
			var tags = _activeTags.Where(_tagData.ContainsKey);
			foreach (var obj in FilterObjects(_activeProvider.GetObjects(), tags, _tagFilterMode))
			{
				_filteredObjects.Add(obj);
			}
			Profiler.EndSample();
		}

		[Pure]
		private IEnumerable<SceneObjectMetaData> FilterObjects(IEnumerable<SceneObjectMetaData> objects, IEnumerable<string> tags, TagFilterMode tagFilterMode)
		{
			switch (tagFilterMode)
			{
				case TagFilterMode.MatchAnyTag:
					return objects.Where(x => x.tags.Overlaps(tags));
				case TagFilterMode.MatchAllTags:
					return objects.Where(x => x.tags.IsSubsetOf(tags));
				default:
					throw new ArgumentOutOfRangeException(nameof(tagFilterMode), tagFilterMode, null);
			}
		}

		private bool DrawEntry(SceneObjectMetaData obj)
		{
			float colorMultiplier;

			float ColorMultiplier(GameObject go)
			{
				return go.activeInHierarchy ? 1 : .5f;
			}

			switch (obj.targetObject)
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

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			LoadActiveTags();
		}
	}
}
