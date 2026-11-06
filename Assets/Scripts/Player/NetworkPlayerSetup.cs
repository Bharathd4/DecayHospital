
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FPSMovement movement;
    [SerializeField] private PistolRaycast shooting;
    [SerializeField] private CoOpPistolRaycast coOpShooting;

    public override void OnNetworkSpawn()
    {
        bool isLocalPlayer = IsOwner;

        // Enable the camera only for the owning player.
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(isLocalPlayer);

        // Enable movement only for the owning player.
        if (movement != null)
            movement.enabled = isLocalPlayer;

        // Disable SOLO shooting on all network players.
        if (shooting != null)
            shooting.enabled = false;

        // Enable CO-OP shooting only for the owning player.
        if (coOpShooting != null)
            coOpShooting.enabled = isLocalPlayer;

        // Enable the crosshair only for the local player.
        if (playerCamera != null)
        {
            Transform crosshair =
                playerCamera.transform.Find("Overlay/Crosshair");

            if (crosshair != null)
                crosshair.gameObject.SetActive(isLocalPlayer);
        }

        // Keep the player body visible to other players.
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            if (rend.transform != playerCamera?.transform &&
                !rend.transform.IsChildOf(
                    playerCamera != null
                        ? playerCamera.transform
                        : transform))
            {
                rend.enabled = true;
            }
        }

        // Lock the cursor for the local player.
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
