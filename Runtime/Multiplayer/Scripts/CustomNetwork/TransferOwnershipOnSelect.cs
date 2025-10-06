/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Meta.XR.BuildingBlocks;
using UnityEngine;
using Oculus.Interaction;

namespace PTTI.XR.MultiplayerBlocks.Shared
{
    /// <summary>
    /// The interface used to transfer the ownership of networked game objects to the local player for when
    /// there are objects that may be owned by multiple clients (depending on the game state and player actions).
    /// </summary>
    /// <remarks>Currently there are implementations for Photon Fusion (<see cref="TransferOwnershipFusion"/>) and Unity Netcode for Gameobjects (<see cref="TransferOwnershipNGO"/>)
    /// for transferring ownership, but more may be added using this interface.</remarks>
    public interface ITransferOwnership
    {
        /// <summary>
        /// Transfers the ownership of the networked game object to the local player.
        /// </summary>
        public void TransferOwnershipToLocalPlayer();

        /// <summary>
        /// Indicates whether the local player has ownership of the networked game object.
        /// </summary>
        /// <returns>'true' if the local player has ownership of the networked game object</returns>
        public bool HasOwnership();
    }

    /// <summary>
    /// The class responsible for the logic of transferring the ownership of networked game objects when they are selected/grabbed
    /// (excluding the networking part which is handled via the <see cref="ITransferOwnership"/> interface).
    /// </summary>
    /// <remarks>This class requires a <see cref="Grabbable"/> component to be present in the same game object or in its children
    /// to be able to handle the grabbing/selection logic.
    /// When the networked game object should also be influenced by the game's gravity simulation, you need to set
    /// <see cref="UseGravity"/> to <c>true</c> and also add a <see cref="Rigidbody"/> component to the same game object.</remarks>
    public class TransferOwnershipOnSelect : MonoBehaviour
    {
        /// <summary>
        /// Indicates whether the game's gravity simulation influences this networked game object.
        /// </summary>
        /// <remarks>Must be set before the game object <c>Awake()</c> method is called.</remarks>
        private Grabbable _grabbable;
        private ITransferOwnership _transferOwnership;

        private void Awake()
        {
            _grabbable = GetComponentInChildren<Grabbable>();

            if (_grabbable == null)
            {
                throw new InvalidOperationException("Object requires a Grabbable component");
            }

            _transferOwnership = this.GetInterfaceComponent<ITransferOwnership>();
            if (_transferOwnership == null)
            {
                throw new InvalidOperationException("Object requires an ITransferOwnership component");
            }
        }

        void OnEnable()
        {
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised += OnPointerEventRaised;
            }
        }

        private void OnDisable()
        {
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
            }
        }

        private void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            if (_grabbable == null || pointerEvent.Type != PointerEventType.Select)
            {
                return;
            }

            if (_grabbable.SelectingPointsCount >= 1)
            {
                if (!_transferOwnership.HasOwnership())
                {
                    //Debug.Log("Transferring ownership");
                    _transferOwnership.TransferOwnershipToLocalPlayer();
                }
            }
        }
    }
}