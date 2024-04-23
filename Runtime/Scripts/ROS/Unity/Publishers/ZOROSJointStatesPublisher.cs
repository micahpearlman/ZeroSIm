using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ZO.ROS.MessageTypes.Sensor;
using ZO.Physics;
using ZO.ROS.Unity;
using ZO.Document;

namespace ZO.ROS.Publisher {
    /// <summary>
    /// Publishes sensor_msgs/JointState messages for a robot.
    /// </summary>
    public class ZOROSJointStatesPublisher : ZOROSUnityGameObjectBase {

        private JointStateMessage _jointStatesMessage = new JointStateMessage();

        protected override void ZOReset() {
            base.ZOReset();
            UpdateRateHz = 25.0f;
            ROSTopic = "/joint_states";
        }

        protected override void ZOStart() {
            base.ZOStart();
            if (ZOROSBridgeConnection.Instance.IsConnected) {
                Initialize();
            }
        }

        protected override void ZOOnDestroy() {
            ROSBridgeConnection?.UnAdvertise(ROSTopic);
        }


        private void Initialize() {
            // advertise
            ROSBridgeConnection.Advertise(ROSTopic, _jointStatesMessage.MessageType);

        }


        protected override void ZOUpdateHzSynchronized() {
            _jointStatesMessage.header.Update();
            // _jointStatesMessage.header.frame_id = FrameID; TODO???

            // get all the joints
            // TODO: cache the joints assuming they don't change
            // TODO: we don't quite yet have a Joint base class fully implemented so we look for specific joints.
            ZOJointInterface[] jointsArray = this.GetComponentsInChildren<ZOJointInterface>();

            // remove any fixed joints as it makes no sense to have state
            List<ZOJointInterface> joints = new List<ZOJointInterface>();
            foreach (ZOJointInterface joint in jointsArray) {
                string[] typeHierarchy = joint.Type.Split('.');
                if (typeHierarchy.Contains("fixedjoint")) {
                    // skip
                    continue;
                }
                if (typeHierarchy.Contains("fixed")) {
                    // skip 
                    continue;
                }
                joints.Add(joint);
            }

            // setup the message arrays
            _jointStatesMessage.name = new string[joints.Count];
            _jointStatesMessage.position = new double[joints.Count];
            _jointStatesMessage.velocity = new double[joints.Count];
            _jointStatesMessage.effort = new double[joints.Count];

            // fill in the arrays
            int i = 0;
            foreach (ZOJointInterface joint in joints) {
                _jointStatesMessage.name[i] = joint.Name;
                _jointStatesMessage.position[i] = joint.Position;
                _jointStatesMessage.velocity[i] = joint.Velocity;
                _jointStatesMessage.effort[i] = joint.Effort;
                i++;
            }
            if (ROSBridgeConnection.IsConnected) {
                ROSBridgeConnection.Publish(_jointStatesMessage, ROSTopic, Name);
            }

        }

        #region ZOSerializationInterface

        public override string Type {
            get { return "ros.publisher.joint_states"; }
        }

        #endregion // ZOSerializationInterface


        #region ZOROSUnityInterface
        public override void OnROSBridgeConnected(ZOROSUnityManager rosUnityManager) {
            Debug.Log("INFO: ZOROSJointStatesPublisher::OnROSBridgeConnected");
            Initialize();

        }

        public override void OnROSBridgeDisconnected(ZOROSUnityManager rosUnityManager) {
            Debug.Log("INFO: ZOROSJointStatesPublisher::OnROSBridgeDisconnected");
            ROSBridgeConnection?.UnAdvertise(ROSTopic);
        }
        #endregion // ZOROSUnityInterface

    }
}