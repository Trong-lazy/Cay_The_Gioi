using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using UnityEditor;
using UnityEngine;
namespace CayTheGioi.Editor
{
    public static class RebirthDebug
    {
        [MenuItem("Cây Thế Giới/T06/Debug/Force DEAD (Validation Only)")]
        public static void ForceDead()
        {
            if(string.IsNullOrEmpty(SaveService.ValidationDirectory))throw new System.InvalidOperationException("Use isolated validation save first.");
            var b=Object.FindAnyObjectByType<GameBootstrapper>();
            if(b==null||!b.IsInitialized||b.SaveCtrl.Suspended||b.SaveCtrl.AwaitingOffline)return;
            if(b.Evolution.IsTransitioning){b.Transition.enabled=false;b.Transition.enabled=true;}
            b.State.run.hp=0;b.State.run.life="DEAD";b.Input.Cancel();b.Resources.NotifyExternalCommit();
            b.SaveCtrl.ForceSave("T06_DEBUG_DEAD");
        }
        [MenuItem("Cây Thế Giới/T06/Debug/Force DEAD (Validation Only)",true)]
        public static bool CanForceDead()=>EditorApplication.isPlaying&&!string.IsNullOrEmpty(SaveService.ValidationDirectory);
    }
}
