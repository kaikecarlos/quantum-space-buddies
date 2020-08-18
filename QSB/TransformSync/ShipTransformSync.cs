﻿using QSB.Utility;
using System;
using UnityEngine;

namespace QSB.TransformSync
{
    public class ShipTransformSync : TransformSync
    {
        public static ShipTransformSync LocalInstance { get; private set; }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        public override uint PlayerId
        {
            get
            {
                uint id = uint.MaxValue;
                try
                {
                    id = netId.Value - 1;
                }
                catch
                {
                    DebugLog.ToConsole($"Error while geting netId of {GetType().Name}! " +
                        $"{Environment.NewLine}     - Did you destroy the TransformSync without destroying the {GetType().Name}?" +
                        $"{Environment.NewLine}     - Did a destroyed TransformSync/{GetType().Name} still have an active action/event listener?" +
                        $"{Environment.NewLine}     If you are a user seeing this, please report this error.", OWML.Common.MessageType.Error);
                }
                return id;
            }
        }

        private Transform GetShipModel()
        {
            return Locator.GetShipTransform();
        }

        protected override Transform InitLocalTransform()
        {
            return GetShipModel().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior");
        }

        protected override Transform InitRemoteTransform()
        {
            var shipModel = GetShipModel();

            var remoteTransform = new GameObject().transform;

            Instantiate(shipModel.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior"), remoteTransform);
            Instantiate(shipModel.Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior"), remoteTransform);
            Instantiate(shipModel.Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior"), remoteTransform);
            Instantiate(shipModel.Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Exterior"), remoteTransform);
            Instantiate(shipModel.Find("Module_Engine/Geo_Engine/Engine_Geometry/Engine_Exterior"), remoteTransform);

            var landingGearFront = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Front/Geo_LandingGear_Front"), remoteTransform);
            var landingGearLeft = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Left/Geo_LandingGear_Left"), remoteTransform);
            var landingGearRight = Instantiate(shipModel.Find("Module_LandingGear/LandingGear_Right/Geo_LandingGear_Right"), remoteTransform);

            Destroy(landingGearFront.Find("LandingGear_FrontCollision").gameObject);
            Destroy(landingGearLeft.Find("LandingGear_LeftCollision").gameObject);
            Destroy(landingGearRight.Find("LandingGear_RightCollision").gameObject);

            landingGearFront.localPosition
                = landingGearLeft.localPosition
                = landingGearRight.localPosition
                += Vector3.up * 3.762f;

            return remoteTransform;
        }

        public override bool IsReady => GetShipModel() != null && Player != null && Player.IsReady;
    }
}
