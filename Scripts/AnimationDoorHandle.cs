using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VRC_RealFakeDoors
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AnimationDoorHandle : UdonSharpBehaviour
    {
        [SerializeField] private AnimationDoor[] DoorsToControl;

        public override void Interact()
        {
            foreach (AnimationDoor door in DoorsToControl)
            {
                if (door == null)
                    return;

                if (door.IsDoorAnimating)
                    return;

                Networking.SetOwner(Networking.LocalPlayer, door.gameObject);
                door.OwnerUpdateDoor();
            }
        }
    }
}