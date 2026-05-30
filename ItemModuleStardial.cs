using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace BaronsBounties
{
    public class ItemModuleStardial : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<Stardial>().Init(item);
        }

        public class Stardial : ThunderBehaviour
        {
            public ConfigurableJoint configurableJoint;
            public Transform kayosva;
            public Item item;
            
            public float maxForce = 1000f;
            public float spring = 500f;
            public float damper = 50f;
            public bool hasStar;

            private Quaternion initialLocalRotation;

            public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

            public void Init(Item item)
            {
                this.item = item;
                configurableJoint = item.GetComponentInChildren<ConfigurableJoint>();
                kayosva = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name.Contains("Azure"));
                hasStar = kayosva != null;

                if (configurableJoint != null)
                {
                    initialLocalRotation = configurableJoint.transform.localRotation;
                    SetupAngularDrive();
                }
            }

            private void SetupAngularDrive()
            {
                configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
                configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
                configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
                JointDrive drive = new()
                {
                    positionSpring = spring,
                    positionDamper = damper,
                    maximumForce = maxForce
                };

                configurableJoint.angularXDrive = drive;
                configurableJoint.angularYZDrive = drive;
                configurableJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
            }

            protected override void ManagedUpdate()
            {
                base.ManagedUpdate();

                if (!hasStar || kayosva == null || configurableJoint == null)
                {
                    Debug.Log("Disabled");
                    enabled = false;
                    return;
                }

                Quaternion lookRotation = Quaternion.LookRotation((kayosva.position - configurableJoint.transform.position).normalized, Vector3.up);
                Quaternion relativeToParent = Quaternion.Inverse(configurableJoint.transform.parent.rotation) * lookRotation;
                configurableJoint.targetRotation = Quaternion.Inverse(relativeToParent) * initialLocalRotation;
            }
        }
    }
}