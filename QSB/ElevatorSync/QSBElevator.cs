﻿using OWML.ModHelper.Events;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.ElevatorSync
{
    public class QSBElevator : WorldObject
    {
        private Elevator _elevator;
        private Vector3 _startLocalPos;
        private Vector3 _endLocalPos;

        private SingleInteractionVolume _interactVolume;
        private OWAudioSource _owAudioSourceOneShot;
        private OWAudioSource _owAudioSourceLP;

        public void Init(Elevator elevator, int id)
        {
            _elevator = elevator;
            ObjectId = id;
            QSB.Helper.Events.Unity.RunWhen(() => _elevator.GetValue<SingleInteractionVolume>("_interactVolume") != null, InitValues);
        }

        private void InitValues()
        {
            _startLocalPos = _elevator.GetValue<Vector3>("_startLocalPos");
            _endLocalPos = _elevator.GetValue<Vector3>("_endLocalPos");
            _interactVolume = _elevator.GetValue<SingleInteractionVolume>("_interactVolume");
            _owAudioSourceOneShot = _elevator.GetValue<OWAudioSource>("_owAudioSourceOneShot");
            _owAudioSourceLP = _elevator.GetValue<OWAudioSource>("_owAudioSourceLP");
        }

        public void RemoteCall(ElevatorDirection direction)
        {
            SetDirection(direction);
            RemoteStartLift();
        }

        private void SetDirection(ElevatorDirection direction)
        {
            var isGoingUp = direction == ElevatorDirection.Up;
            var targetPos = isGoingUp ? _endLocalPos : _startLocalPos;
            _elevator.SetValue("_goingToTheEnd", isGoingUp);
            _elevator.SetValue("_targetLocalPos", targetPos);
            _interactVolume.transform.Rotate(0f, 180f, 0f);
        }

        private void RemoteStartLift()
        {
            _elevator.enabled = true;
            _elevator.SetValue("_initLocalPos", _elevator.transform.localPosition);
            _elevator.SetValue("_initLiftTime", Time.time);
            _owAudioSourceOneShot.PlayOneShot(AudioType.TH_LiftActivate);
            _owAudioSourceLP.FadeIn(0.5f);
            _interactVolume.DisableInteraction();
        }
    }
}
