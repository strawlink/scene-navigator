namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;

	public class SceneObjectMetaData : IComparable<SceneObjectMetaData>
	{
		public readonly Object targetObject;
		public readonly string name;
		public readonly HashSet<string> tags;

		public SceneObjectMetaData(Object targetObject, IEnumerable<string> tags)
		{
			this.targetObject = targetObject;
			this.tags = new HashSet<string>(tags);
			name = targetObject.name;
		}

		public SceneObjectMetaData(Object targetObject, string tag)
		{
			this.targetObject = targetObject;
			tags = new HashSet<string> {tag};
			name = targetObject.name;
		}

		public int CompareTo(SceneObjectMetaData other)
		{
			if (ReferenceEquals(this, other))
			{
				return 0;
			}

			if (ReferenceEquals(null, other))
			{
				return 1;
			}

			return string.Compare(name, other.name, StringComparison.Ordinal);
		}
	}
}