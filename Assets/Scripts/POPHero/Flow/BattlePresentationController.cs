using System;
using System.Collections;
using UnityEngine;

namespace POPHero
{
    public sealed class BattlePresentationController
    {
        readonly MonoBehaviour coroutineHost;
        readonly Func<RoundResolveResult, IEnumerator> playRoutineFactory;
        readonly Action onInterrupted;
        Coroutine activeRoutine;

        public BattlePresentationController(MonoBehaviour coroutineHost, Func<RoundResolveResult, IEnumerator> playRoutineFactory, Action onInterrupted)
        {
            this.coroutineHost = coroutineHost;
            this.playRoutineFactory = playRoutineFactory;
            this.onInterrupted = onInterrupted;
        }

        public bool IsPlaying { get; private set; }

        public void Play(RoundResolveResult result)
        {
            Stop();
            if (coroutineHost == null || playRoutineFactory == null)
                return;

            IsPlaying = true;
            activeRoutine = coroutineHost.StartCoroutine(PlayRoutine(result));
        }

        public void Stop()
        {
            if (activeRoutine == null || coroutineHost == null)
                return;

            coroutineHost.StopCoroutine(activeRoutine);
            activeRoutine = null;
            IsPlaying = false;
            onInterrupted?.Invoke();
        }

        public void MarkCompleted()
        {
            activeRoutine = null;
            IsPlaying = false;
        }

        IEnumerator PlayRoutine(RoundResolveResult result)
        {
            yield return playRoutineFactory(result);
            MarkCompleted();
        }
    }
}
