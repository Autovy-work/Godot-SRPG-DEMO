using Godot;
using System.Collections.Generic;

namespace CSharpTestGame.Managers
{
	[GlobalClass]
	public partial class ResourceManager : RefCounted
	{
		// 缓存已加载的纹理，避免重复加载
		private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

		/// <summary>
		/// 加载纹理资源
		/// </summary>
		/// <param name="path">纹理路径</param>
		/// <returns>加载的纹理，如果加载失败则返回null</returns>
		public Texture2D LoadTexture(string path)
		{
			// 检查缓存中是否已有该纹理
			if (textureCache.TryGetValue(path, out var cachedTexture))
			{
				return cachedTexture;
			}

			try
			{
				// 加载纹理
				var texture = ResourceLoader.Load<Texture2D>(path);
				if (texture != null)
				{
					// 将纹理添加到缓存
					textureCache[path] = texture;
				}
				else
				{
					GD.PrintErr($"Failed to load texture: {path}");
				}
				return texture;
			}
			catch (System.Exception e)
			{
				GD.PrintErr($"Error loading texture {path}: {e.Message}");
				return null;
			}
		}

		/// <summary>
		/// 清理缓存的资源
		/// </summary>
		public void ClearCache()
		{
			textureCache.Clear();
		}
	}
}