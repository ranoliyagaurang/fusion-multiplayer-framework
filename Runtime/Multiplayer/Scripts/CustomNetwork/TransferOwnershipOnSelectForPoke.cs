using System;
using UnityEngine;
using Oculus.Interaction;
using Meta.XR.BuildingBlocks;

namespace PTTI.XR.MultiplayerBlocks.Shared
{
    /// <summary>
    /// Handles transferring ownership of networked game objects when they are poked (selected)
    /// using <see cref="PokeInteractable"/>. Uses <see cref="ITransferOwnership"/> for the networking part.
    /// </summary>
    [RequireComponent(typeof(PokeInteractable))]
    public class TransferOwnershipOnSelectForPoke : MonoBehaviour
    {
        /// <summary>
        /// Whether the object should be affected by gravity when owned locally.
        /// </summary>
        public bool UseGravity;

        private PokeInteractable _pokeInteractable;
        private Rigidbody _rigidbody;
        private ITransferOwnership _transferOwnership;

        private void Awake()
        {
            _pokeInteractable = GetComponent<PokeInteractable>();
            if (_pokeInteractable == null)
            {
                throw new InvalidOperationException("Object requires a PokeInteractable component");
            }

            _transferOwnership = this.GetInterfaceComponent<ITransferOwnership>();
            if (_transferOwnership == null)
            {
                throw new InvalidOperationException("Object requires an ITransferOwnership component");
            }

            if (UseGravity)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    throw new InvalidOperationException(
                        "Object requires a Rigidbody component when UseGravity is enabled");
                }
            }
        }

        private void OnEnable()
        {
            if(_pokeInteractable != null)
                _pokeInteractable.WhenPointerEventRaised += OnPointerEventRaised;
        }

        private void OnDisable()
        {
            if(_pokeInteractable != null)
                _pokeInteractable.WhenPointerEventRaised -= OnPointerEventRaised;
        }

        private void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            // Only handle "Select" events (when poke selects this interactable)
            if (pointerEvent.Type != PointerEventType.Select)
                return;

            if (!_transferOwnership.HasOwnership())
            {
                //Debug.Log("Transferring ownership on poke select");
                _transferOwnership.TransferOwnershipToLocalPlayer();
            }
        }

        private void LateUpdate()
        {
            if (_transferOwnership.HasOwnership() && UseGravity && _rigidbody != null)
            {
                // Ensure Rigidbody is properly kinematic-locked when owned
                _rigidbody.isKinematic = _rigidbody.IsLocked();
            }
        }
    }
}
