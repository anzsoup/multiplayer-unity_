using UnityEngine;

namespace Soysaeu.Networking.Demo
{
	/// ---------------------------------------------------------------------------
	/// [Scene 에 존재하는 RMPNetworkView]
	///		런타임에 Replicate 되는 것이 아니고 미리 Scene 에 존재하는 것들은
	///		자동으로 Replicate 된 객체로 취급된다.
	/// ---------------------------------------------------------------------------

	public class DemoReplicate : MonoBehaviour
	{
		[SerializeField]
		private RMPNetworkView _view;
		[SerializeField]
		private Camera _camera;
		[SerializeField]
		private DemoCube _cubePrefab;

		void OnDisable()
		{
			DemoCube.Clear();
		}

		void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit hit;
				var ray = _camera.ScreenPointToRay(Input.mousePosition);

				if (Physics.Raycast(ray, out hit))
				{
					var hitPoint = hit.point;
					var pos = hitPoint + (Vector3.up * 1.5f);
					_view.RPC(RPCOption.ToServer, "svRPC_ReplicateCube", pos);
				}
			}
		}

		[RMP]
		[ServerOnly]
		private void svRPC_ReplicateCube(Vector3 pos)
		{
			var cube = Instantiate(_cubePrefab, pos, Quaternion.identity);
			cube.transform.up = Random.insideUnitSphere;
			cube.View.Replicate();
		}
	}
}