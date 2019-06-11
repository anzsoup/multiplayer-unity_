using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Salgu.Steam.Demo
{
	/// -----------------------------------------------------------------------------------
	/// [스팀 기능 사용 예 : Avatar 불러오기]
	///		Facepunch.Steamworks 는 다양한 스팀 기능을 지원한다.
	///		이 스크립트에는 스팀 프로필 이미지를 불러오는 예제가 작성되어 있다.
	/// -----------------------------------------------------------------------------------
	
	public class DemoAvatar : MonoBehaviour
	{
		[SerializeField] RawImage _ui = null;
		[SerializeField] Facepunch.Steamworks.Friends.AvatarSize _size = Facepunch.Steamworks.Friends.AvatarSize.Small;

		IEnumerator Start()
		{
			while (Steam.Client == null)
				yield return null;

			var me = Steam.Me;
			Steam.Client.Friends.GetAvatar(_size, me.SteamId, (image) => OnImage(image));
		}

		private void OnImage(Facepunch.Steamworks.Image image)
		{
			if (image == null)
			{
				Debug.LogWarning("Failed to get avatar.");
				return;
			}

			var texture = new Texture2D(image.Width, image.Height);

			for (int x = 0; x < image.Width; x++)
				for (int y = 0; y < image.Height; y++)
				{
					var p = image.GetPixel(x, y);

					texture.SetPixel(x, image.Height - y, new UnityEngine.Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
				}

			texture.Apply();

			ApplyTexture(texture);
		}

		private void ApplyTexture(Texture texture)
		{
			if (_ui != null)
				_ui.texture = texture;
		}
	}
}