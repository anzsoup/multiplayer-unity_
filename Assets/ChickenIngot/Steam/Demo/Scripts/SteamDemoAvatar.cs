using UnityEngine;
using System.Collections;
using Facepunch.Steamworks;
using UnityEngine.UI;

namespace ChickenIngot.Steam.Demo
{
	public class SteamDemoAvatar : MonoBehaviour
	{
		[SerializeField]
		private RawImage _ui;
		[SerializeField]
		private Friends.AvatarSize _size;

		IEnumerator Start()
		{
			while (Client.Instance == null)
				yield return null;

			var me = SteamService.Me;
			Client.Instance.Friends.GetAvatar(_size, me.SteamId, (image) => OnImage(image));
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