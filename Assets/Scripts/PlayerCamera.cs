using FishNet.Object;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class PlayerCamera : NetworkBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 8f, -6f);

        private Camera playerCamera;

        public override void OnStartClient()
        {
            if (!base.IsOwner)
            {
                enabled = false;
                return;
            }

            playerCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    return;
                }
            }

            playerCamera.transform.position = transform.position + offset;
            playerCamera.transform.LookAt(transform.position);
        }
    }
}
