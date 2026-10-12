using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Entities
{
	[DisallowMultipleComponent]
	public class RendererFlicker : MonoBehaviour
	{
		[SerializeField] private Renderer[] targetRenderers;
		[SerializeField] private Color flickerColor = Color.white;
		[SerializeField, Min(0.01f)] private float duration = 0.12f;
		[SerializeField, Min(0.01f)] private float interval = 0.04f;
		[SerializeField] private string colorProperty = "_BaseColor";
		[SerializeField] private string textureProperty = "_BaseMap";
		[Tooltip("Replaces the base texture with white during the flash so dark textures also flicker.")]
		[SerializeField] private bool replaceBaseTexture = true;

		private sealed class MaterialSlot
		{
			public Renderer Renderer;
			public int Index;
			public bool HasTexture;
			public readonly MaterialPropertyBlock Original = new MaterialPropertyBlock();
			public readonly MaterialPropertyBlock Flicker = new MaterialPropertyBlock();
		}

		private MaterialSlot[] _slots;
		private int _colorId;
		private int _textureId;
		private float _elapsed;
		private bool _isLit;
		private bool _isPlaying;

		private void Reset()
		{
			targetRenderers = GetComponentsInChildren<Renderer>(true);
		}

		private void Awake()
		{
			_colorId = Shader.PropertyToID(colorProperty);
			_textureId = Shader.PropertyToID(textureProperty);
			List<MaterialSlot> slots = new List<MaterialSlot>();
			if (targetRenderers == null) targetRenderers = new Renderer[0];
			foreach (Renderer target in targetRenderers)
			{
				if (target == null) continue;
				Material[] materials = target.sharedMaterials;
				for (int i = 0; i < materials.Length; i++)
				{
					Material material = materials[i];
					if (material == null || !material.HasProperty(_colorId)) continue;
					slots.Add(new MaterialSlot
					{
						Renderer = target,
						Index = i,
						HasTexture = material.HasProperty(_textureId)
					});
				}
			}
			_slots = slots.ToArray();
		}

		[ContextMenu("Play Flicker")]
		public void Play()
		{
			if (!Application.isPlaying || !isActiveAndEnabled || _slots == null) return;
			// Restart on repeated hits without capturing the flash as the original appearance.
			if (!_isPlaying)
			{
				foreach (MaterialSlot slot in _slots)
				{
					if (slot.Renderer == null) continue;
					slot.Renderer.GetPropertyBlock(slot.Original, slot.Index);
					slot.Renderer.GetPropertyBlock(slot.Flicker, slot.Index);
					if (slot.Flicker.isEmpty) slot.Renderer.GetPropertyBlock(slot.Flicker);
					slot.Flicker.SetColor(_colorId, flickerColor);
					if (replaceBaseTexture && slot.HasTexture)
						slot.Flicker.SetTexture(_textureId, Texture2D.whiteTexture);
				}
			}
			_elapsed = 0f;
			_isPlaying = true;
			SetLit(true);
		}

		private void Update()
		{
			if (!_isPlaying) return;
			_elapsed += Time.deltaTime;
			if (_elapsed >= Mathf.Max(0.01f, duration))
			{
				Stop();
				return;
			}
			bool lit = Mathf.FloorToInt(_elapsed / Mathf.Max(0.01f, interval)) % 2 == 0;
			if (lit != _isLit) SetLit(lit);
		}

		public void Stop()
		{
			if (!_isPlaying) return;
			SetLit(false);
			_isPlaying = false;
		}

		private void SetLit(bool lit)
		{
			_isLit = lit;
			foreach (MaterialSlot slot in _slots)
			{
				if (slot.Renderer == null) continue;
				slot.Renderer.SetPropertyBlock(lit ? slot.Flicker :
					(slot.Original.isEmpty ? null : slot.Original), slot.Index);
			}
		}

		private void OnDisable()
		{
			Stop();
		}
	}
}
