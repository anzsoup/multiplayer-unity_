using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ChickenIngot.Build
{
	[ExecuteInEditMode]
	public class BuildManager : MonoBehaviour
	{
		[Header("Build Info")]
		[SerializeField]
		private string _companyName = "Chicken Ingot";
		[SerializeField]
		private string _productName = "Multiplayer Starter Kit";
		[SerializeField]
		private string _binaryName = "bin";
		[SerializeField]
		private string _version = "1.0";

		public static string CompanyName { get; private set; }
		public static string ProductName { get; private set; }
		public static string BinaryName { get; private set; }
		public static string Version { get; private set; }

		void Awake()
		{
			if (Application.isPlaying)
			{
				DontDestroyOnLoad(gameObject);

				CompanyName = _companyName;
				ProductName = _productName;
				BinaryName = _binaryName;
				Version = _version;
			}
		}

#if UNITY_EDITOR
		[Header("Build Settings")]
		[SerializeField] [ReadOnly]
		private SceneAsset _firstScene;

		[SerializeField]
		[Tooltip("빌드에 포함시킬 Scene들.")]
		private SceneAsset[] _otherScenes;

		[SerializeField]
		[Tooltip("빌드 시 PlayerSettings에 추가할 심볼. 여러개의 심볼은 세미콜론으로 구분한다.")]
		private string _symbols;

		[SerializeField]
		[Tooltip("Scene이 열릴 때마다 심볼을 적용한다.")]
		private bool _autoSetSymbols;

		[SerializeField]
		[Tooltip("Server Only 스팀 어플리케이션을 빌드하는 경우, " +
			"Facepuch.Steamworks 의 문제점으로 인해 32비트 환경에서는 동작하지 않는 점 유의.")]
		private BuildTarget _buildTarget = BuildTarget.StandaloneWindows64;

		[SerializeField]
		private BuildOptions _buildOptions = BuildOptions.None;

		[SerializeField]
		[Tooltip("어플리케이션을 배치모드로 열 수 있는 .bat 파일을 빌드 결과물에 포함시킨다.")]
		private bool _createBatchModeRunFile;

		[CustomEditor(typeof(BuildManager))]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				GUILayout.Space(10);

				var build = target as BuildManager;

				if (!string.IsNullOrEmpty(build._symbols))
				{
					GUILayout.BeginHorizontal();

					if (GUILayout.Button("Set Symbol"))
						build.SetSymbol();
					if (GUILayout.Button("Reset Symbol"))
						build.ResetSymbol();

					GUILayout.EndHorizontal();
				}
				if (GUILayout.Button("Build Project"))
				{
					build.BuildProject();
				}
			}
		}

		[MenuItem("GameObject/Build Manager", priority = 30)]
		static void CreateGameObject()
		{
			var find = FindObjectOfType<BuildManager>();
			if (find != null)
			{
				Debug.LogError("Build Manager Object already exists.");
				return;
			}

			var go = new GameObject("Build Manager", typeof(BuildManager));
			Undo.RegisterCreatedObjectUndo(go, "Create Build Manager");
		}

		void Start()
		{
			if (!Application.isPlaying)
			{
				if (_firstScene == null)
				{
					var curScenePath = gameObject.scene.path;
					_firstScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(curScenePath);
				}

				if (_autoSetSymbols)
					SetSymbol();
			}
		}

		private void SetSymbol()
		{
			Symbol.Add(_symbols.Split(';'));
		}

		private void ResetSymbol()
		{
			Symbol.Remove(_symbols.Split(';'));
		}

		private void BuildProject()
		{
			var prevSymbols = Symbol.CurrentSymbols;
			Symbol.Add(_symbols.Split(';'));

			var path = string.Format("{0}/{1}.exe", BuildDirectory(), _binaryName);

			var scenes = new List<string>();
			scenes.Add(AssetDatabase.GetAssetPath(_firstScene));
			foreach (var s in _otherScenes)
			{
				if (s == _firstScene) continue;
				scenes.Add(AssetDatabase.GetAssetPath(s));
			}

			var prevCompanyName = PlayerSettings.companyName;
			PlayerSettings.companyName = _companyName;

			var buildPlayerOptions = new BuildPlayerOptions();
			buildPlayerOptions.scenes = scenes.ToArray();
			buildPlayerOptions.locationPathName = path;
			buildPlayerOptions.target = _buildTarget;
			buildPlayerOptions.options = _buildOptions;

			var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			var summary = report.summary;

			if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
			{
				Debug.Log(string.Format("Build succeeded : {0}\nOutput : {1}\nPlatform : {2}", 
					summary.totalSize + " bytes",
					summary.outputPath,
					summary.platform));

				if (_createBatchModeRunFile)
					CreateBatchModeRunFile();
			}

			if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
			{
				Debug.Log("Build failed");
			}

			PlayerSettings.companyName = prevCompanyName;
		}

		private string BuildDirectory()
		{
			return string.Format("Build/{0} {1}/",
				_productName, _version);
		}

		private void CreateBatchModeRunFile()
		{
			string content = "@echo off\n{0}.exe -quit -batchmode -nographics";
			content = string.Format(content, _binaryName);

			File.WriteAllText(BuildDirectory() + "/run.bat", content);
		}
#endif
	}
}