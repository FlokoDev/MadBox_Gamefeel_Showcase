using System;
using System.Collections.Generic;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Generic.Tweening
{
	public class TweenService : IService
	{
		private readonly List<TweenHandle> _tweens = new List<TweenHandle>();
		private CancellationTokenSource _cancellationTokenSource;

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			UpdateLoop(_cancellationTokenSource.Token).Forget();
			return UniTask.FromResult(true);
		}

		public TweenHandle DOPunchScale(Transform target, Vector3 strength, float duration)
		{
			if (target == null || _cancellationTokenSource == null || !target.gameObject.activeInHierarchy) return null;
			StopChannel(target, TweenChannel.LocalScale);
			Vector3 initialScale = target.localScale;
			return Add(new TweenHandle(Tween.DOPunchScale(initialScale, strength, duration),
				value => target.localScale = value, target: target, channel: TweenChannel.LocalScale,
				onStop: () =>
			{
				if (target != null) target.localScale = initialScale;
			}));
		}

		public TweenHandle DOMove(Transform target, Vector3 destination, float duration)
		{
			if (target == null || _cancellationTokenSource == null || !target.gameObject.activeInHierarchy) return null;
			StopChannel(target, TweenChannel.Position);
			return Add(new TweenHandle(Tween.DOMove(target.position, destination, duration),
				value => target.position = value, target: target, channel: TweenChannel.Position));
		}

		public TweenHandle DOMove(Vector3 start, Vector3 destination, float duration,
			Action<Vector3> onUpdate, Action onComplete = null)
		{
			if (_cancellationTokenSource == null) return null;
			if (onUpdate == null) throw new ArgumentNullException(nameof(onUpdate));
			return Add(new TweenHandle(Tween.DOMove(start, destination, duration), onUpdate, onComplete));
		}

		public void Stop(Transform target)
		{
			StopChannel(target, TweenChannel.LocalScale);
			StopChannel(target, TweenChannel.Position);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
			foreach (TweenHandle tween in _tweens) tween.Stop();
			_tweens.Clear();
			return UniTask.CompletedTask;
		}

		private TweenHandle Add(TweenHandle tween)
		{
			_tweens.Add(tween);
			return tween;
		}

		private void StopChannel(Transform target, TweenChannel channel)
		{
			for (int i = _tweens.Count - 1; i >= 0; i--)
			{
				TweenHandle tween = _tweens[i];
				if (tween.Target == target && tween.Channel == channel) tween.Stop();
			}
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, cancellationToken).SuppressCancellationThrow()) return;
				for (int i = _tweens.Count - 1; i >= 0; i--)
				{
					if (i >= _tweens.Count) continue;
					TweenHandle tween = _tweens[i];
					tween.Tick(Time.deltaTime);
					// Callbacks may reset the service or start another tween.
					if (!tween.IsActive && i < _tweens.Count && ReferenceEquals(_tweens[i], tween))
						_tweens.RemoveAt(i);
				}
			}
		}

	}
}
