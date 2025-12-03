using Gameplay.Manager;
using System;

namespace Gameplay.Event {
    public class GameEvent {
        public static event Action<SceneType> OnChangeSceneEvent;
        public static event Action OnSceneLoadedEvent;
        public static event Action<bool> OnFinishEvent;
        public static event Action OnUpdateData;

        public static void RaiseChangeScene(SceneType sceneType) => OnChangeSceneEvent?.Invoke(sceneType);
        public static void RaiseSceneLoaded() => OnSceneLoadedEvent?.Invoke();
        public static void RaiseFinish(bool isWin) => OnFinishEvent?.Invoke(isWin);
        public static void RaiseUpdateData() => OnUpdateData?.Invoke();
    }
}