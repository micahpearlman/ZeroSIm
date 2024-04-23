﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ZO.Util;
using ZO.Document;


namespace ZO.ROS.Unity {
    public abstract class ZOROSUnityGameObjectBase : ZOGameObjectBase, ZOROSUnityInterface {


        [Header("ROS Info")]
        public string _ROSTopic = "";

        /// <summary>
        /// The ROS Topic.  For example "/zerosim/joint_states"
        /// </summary>
        /// <value></value>
        public virtual string ROSTopic {
            get => _ROSTopic;
            set => _ROSTopic = value;
        }

        /// <summary>
        /// The ROS Bridge singleton shortcut access.
        /// </summary>
        /// <value></value>
        protected ZOROSBridgeConnection ROSBridgeConnection {
            get {
                return ZOROSBridgeConnection.Instance;
            }
        }

        /// <summary>
        /// The ROS Unity Manger singleton shortcut access.
        /// </summary>
        /// <value></value>
        protected ZOROSUnityManager ROSUnityManager {
            get {
                return ZOROSUnityManager.Instance;
            }
        }


        public string _name;

        /// <summary>
        /// Unique name of the object.  For example: "joint.hinge_from_left_wheel"
        /// </summary>
        /// <value></value>
        public virtual string Name {
            get {
                if (string.IsNullOrEmpty(_name)) {
                    _name = Type + "_" + gameObject.name;
                }
                return _name;
            }

            set => _name = value;
        }

        // TODO: make a ZOReset in ZOGameObjectBase
        protected override void ZOReset() {
            base.ZOReset();
            // generate the default name
            string dummy = Name;
        }


        #region ZOSerializationInterface


        /// <summary>
        /// The ZeroSim object type.  For example: "joint.hinge"
        /// </summary>
        /// <value></value>
        public virtual string Type {
            get { return "undefined"; }
        }



        /// <summary>
        /// The ZOSim document root
        /// </summary>
        /// <value></value>
        public ZOSimDocumentRoot DocumentRoot {
            get {
                // first check if we are the document root
                ZOSimDocumentRoot docRoot = this.GetComponent<ZOSimDocumentRoot>();
                if (docRoot == null) {
                    // search the parents
                    docRoot = this.GetComponentInParent<ZOSimDocumentRoot>();
                }

                if (docRoot == null) {
                    Debug.LogWarning("WARNING: document root cannot be accessed!!!");
                    throw new System.Exception("WARNING: document root cannot be accessed!!!");
                }

                return docRoot;
            }
        }

        #endregion // ZOSerializationInterface

        #region ZOGameObjectBase

        /// <summary>
        /// On Unity Start will connect to ROS bridge connect and disconnect events.
        /// </summary>
        protected override void ZOStart() {
            if (ZOROSUnityManager.Instance == null) {
                return;
            }

            // auto-connect to ROS Bridge connection and disconnect events
            ZOROSUnityManager.Instance.ROSBridgeConnectEvent += OnROSBridgeConnected;
            ZOROSUnityManager.Instance.ROSBridgeDisconnectEvent += OnROSBridgeDisconnected;
        }

        /// <summary>
        /// On Unity Destroy will disconnect to ROS bridge connect and disconnect events.
        /// </summary>
        protected override void ZOOnDestroy() {
            if (ZOROSUnityManager.Instance == null) {
                return;
            }
            ZOROSUnityManager.Instance.ROSBridgeConnectEvent -= OnROSBridgeConnected;
            ZOROSUnityManager.Instance.ROSBridgeDisconnectEvent -= OnROSBridgeDisconnected;
        }

        #endregion // ZOGameObjectBase
        
        #region ZOROSUnityInterface
        public abstract void OnROSBridgeConnected(ZOROSUnityManager rosUnityManager);

        public abstract void OnROSBridgeDisconnected(ZOROSUnityManager rosUnityManager);
        #endregion // ZOROSUnityInterface



    }
}