﻿using FSControl;
using Tutorials;
using Modules;
using UnityEngine;
using AutopilotCommon;
using GameEvents;

namespace Autopilot
{
    public class Autopilot : MonoBehaviour
    {
        DataStore data;
        ProtocolLogic protocol;
        NetworkHandler handler;

        public void Start()
        {
            Log("Start!");
            data = new DataStore();
            protocol = new ProtocolLogic(data, Log);
            handler = new NetworkHandler(protocol, Log);
            handler.StartServer();
            //If you want to stick around
            //GameEvents.Vehicles.OnVehicleSpawned.AddListener(VehicleSpawned);
            DontDestroyOnLoad(this);
        }


        private void VehicleSpawned(Vehicle vehicle)
        {
            Autopilot.Log("OVS Main");
            if (!vehicle.gameObject.TryGetComponent(out AutopilotComponent ac))
            {
                Log("Adding autopilot controller to " + vehicle.name);
                AutopilotComponent ac2 = vehicle.gameObject.AddComponent(typeof(AutopilotComponent)) as AutopilotComponent;
                ac2.OnVehicleSpawn(vehicle);

            }
            else
            {
                Log("Autopilot controller already exists for " + vehicle.name);
            }
        }

        //running this as fast as possible
        public void Update()
        {
            if (!GameLogic.inGame || !GameLogic.SceneryLoaded || GameLogic.LocalPlayerVehicle == null || !GameLogic.LocalPlayerVehicle.InitComplete)
            {
                data.ch1 = data.ch2 = data.ch3 = data.ch4 = data.ch5 = data.ch6 = data.ch7 = data.ch8 = 1500;
                data.radpitch = 0;
                data.radroll = 0;
                data.radyaw = 0;
                data.pitch = 0;
                data.roll = 0;
                data.yaw = 0;
                data.cr = 0;
                data.dth = 0;
                data.accx = 0;
                data.accy = 0;
                data.accz = 0;
                data.rssi = 0;
                data.armed = (int)MAVLink.MAV_MODE.MANUAL_DISARMED;
                data.avrrpm = 0;
                data.latitude = 0;
                data.longitude = 0;
                data.altitude = 0;
                data.heading = 0;
                data.iaspeed = 0;
                data.name = "";
                return;
            }
            Vehicle v = GameLogic.LocalPlayerVehicle;

            var props = v.GetModules<Propeller>();
            if (props.Count != 0)
            {
                data.avrrpm = 0;
                foreach (var p in props)
                {
                    data.avrrpm += p.GetRotSpeed();
                }
                data.avrrpm /= props.Count;
                data.avrrpm *= 1.66667f;
}

            var armed = InputSettings.EngineAutoStart.button;

            if (armed.GetButtonDown())
            {
                data.armed = (int)MAVLink.MAV_MODE.MANUAL_ARMED;
            }
            else
            {
                data.armed = (int)MAVLink.MAV_MODE.MANUAL_DISARMED;

            }


            data.radpitch = FSControlUtil.GetVehiclePitch(v);
            data.pitch = data.radpitch * Mathf.Rad2Deg;
            data.radroll = FSControlUtil.GetVehicleRoll(v);
            data.roll = data.radroll * Mathf.Rad2Deg;
            data.radyaw = FSControlUtil.GetVehicleYaw(v);
            data.yaw = data.radyaw * Mathf.Rad2Deg;

            //Metres -> mm
            data.altitude = v.Physics.Altitude * 1000f;
            data.iaspeed = v.Physics.Speed;
            data.heading = v.Physics.HeadingDegs;
            data.cr = v.Physics.VerticalSpeed;

            data.accx = v.Physics.Acceleration.x;
            data.accy = v.Physics.Acceleration.y;
            data.accz = v.Physics.Acceleration.z;
            data.name = v.name;

            //Balsa is YUp
            //Mavlink is degE7, 1° = 111 km 1E7/111000 = ~90
            data.latitude = (int)(FloatingOrigin.GetAbsoluteWPos(v.transform.position).z * 90.09f);
            data.longitude = (int)(FloatingOrigin.GetAbsoluteWPos(v.transform.position).x * 90.09f);

            //controller stuff
            data.rssi = map(v.SignalStrength.SignalDegradation, 0, 1, 255, 0);

            data.ch1 = 1500 + InputSettings.Axis_Roll.GetAxis() * 500;
            data.ch2 = 1500 + InputSettings.Axis_Pitch.GetAxis() * 500;
            data.ch3 = 1500 + InputSettings.Axis_Throttle.GetAxis() * 500;
            data.ch4 = 1500 + InputSettings.Axis_Yaw.GetAxis() *500;
            data.ch5 = 1500 + InputSettings.Axis_A.GetAxis() * 500;
            data.ch6 = 1500 + InputSettings.Axis_B.GetAxis() * 500;
            data.ch7 = 1500 + InputSettings.Axis_C.GetAxis() * 500;
            data.ch8 = 1500 + InputSettings.Axis_D.GetAxis() * 500;
        }

        public void FixedUpdate()
        {
        }

        private float map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }

        //It's nice to identify in the log where things came from
        public static void Log(string text)
        {
            Debug.Log($"{Time.realtimeSinceStartup} [Autopilot] {text}");
        }
    }
}
