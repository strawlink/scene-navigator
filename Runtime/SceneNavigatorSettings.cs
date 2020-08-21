namespace SceneNavigator
{
	using System;
	using JetBrains.Annotations;

	[PublicAPI]
	public static class SceneNavigatorSettings
	{
		private static ISceneNavigatorProvider _provider;

		public static ISceneNavigatorProvider provider
		{
			get => _provider;
			set
			{
				if (_provider == value)
				{
					return;
				}

				_provider = value;
				onProviderChanged?.Invoke(value);
			}
		}

		public static ISceneNavigatorRegistration registration { get; set; }

		public static event ProviderChangedDelegate onProviderChanged;

		public delegate void ProviderChangedDelegate(ISceneNavigatorProvider newProvider);

		static SceneNavigatorSettings()
		{
			var obj = new SceneNavigatorProvider();
			provider = obj;
			registration = obj;
		}
	}
}