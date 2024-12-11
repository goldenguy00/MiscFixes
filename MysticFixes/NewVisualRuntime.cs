using System.ComponentModel;
using System;
using System.Linq;
using HG;
using RoR2;
using TanksMod.Modules.Components;
using TanksMod.Modules.Components.BasicTank;
using UnityEngine;

namespace MiscFixes
{
    public class NewVisualRuntime : MonoBehaviour, IOnTakeDamageServerReceiver
    {
        public VisualRuntime visualRuntime;
        public TankController tank;

        public CharacterBody characterBody;
        public ModelLocator modelLoc;
        public ChildLocator childLoc;
        public HealthComponent health;
        public CharacterMotor motor;
        public SkillLocator skillLoc;
        public Rigidbody weightPoint;
        public InputBankTest inputBank;
        public CharacterModel model;

        public Transform root, anchor, anchorLerp;
        public GameObject missile1, missle2;
        public GameObject body, turret, gun, pivot;
        public MeshRenderer overheat;

        public ParticleSystem[] hurtboxSystem, bodySystem;
        public ParticleSystem[][] thrusterSystems;
        public ParticleSystem[][] jumpSystems;

        public readonly string[] thrusters = ["ThrusterRearLeft", "ThrusterRearRight", "ThrusterCenter", "ExhaustThruster"];
        public readonly string[] jumpBoosters = ["JumpBoosterFL", "JumpBoosterFR", "JumpBoosterRL", "JumpBoosterRR"];

        public void OnEnable()
        {
            characterBody = this.GetComponent<CharacterBody>();
            tank = this.GetComponent<TankController>();
            visualRuntime = this.GetComponent<VisualRuntime>();
            modelLoc = base.GetComponent<ModelLocator>();
            model = modelLoc.modelTransform.GetComponent<CharacterModel>();
            childLoc = modelLoc.modelTransform.GetComponent<ChildLocator>();
            health = base.GetComponent<HealthComponent>();
            motor = base.GetComponent<CharacterMotor>();
            skillLoc = base.GetComponent<SkillLocator>();
            inputBank = base.GetComponent<InputBankTest>();

            root = this.childLoc.FindChild("ROOT");
            anchor = childLoc.FindChild("Anchor");
            anchorLerp = childLoc.FindChild("AnchorLerp");
            weightPoint = childLoc.FindChild("BodyWeightPoint").GetComponent<Rigidbody>();
            childLoc.FindChild("BodyWeightPoint").parent = null;

            missile1 = childLoc.FindChild("MicroMissle1").gameObject;
            missle2 = childLoc.FindChild("MicroMissle2").gameObject;

            var thrusterL = childLoc.FindChild("ThrusterLeftMesh").gameObject;
            var thrusterR = childLoc.FindChild("ThrusterRightMesh").gameObject;
            var thrusterC = childLoc.FindChild("ThrusterCenter").gameObject;
            thrusterC.SetActive(!thrusterL.activeSelf && !thrusterR.activeSelf);

            body = childLoc.FindChild("Body").gameObject;
            turret = childLoc.FindChild("Turret").gameObject;
            gun = childLoc.FindChild("Gun").gameObject;
            pivot = childLoc.FindChild("SmallWeapons").GetComponentsInChildren<Transform>().FirstOrDefault(t => t.gameObject.name == "Pivot")?.gameObject;

            jumpSystems = new ParticleSystem[4][];
            thrusterSystems = new ParticleSystem[4][];
        }


        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (!damageReport.damageInfo.canRejectForce && damageReport.damageDealt > 0f)
            {
                //weightPoint.AddForceAtPosition((base.transform.position - health.lastHitAttacker.transform.position).normalized * 1000f, health.lastHitAttacker.transform.position, ForceMode.Acceleration);
                weightPoint.AddForce(damageReport.damageInfo.force, ForceMode.Acceleration);
            }
        }

        public void Update()
        {
            if (!health.isInFrozenState && health.alive)
            {
                if (modelLoc.modelTransform)
                {
                    float t = Vector3.Dot(anchorLerp.up, anchor.up);
                    float num = Mathf.SmoothStep(5f, 15f, t);
                    anchorLerp.rotation = Quaternion.Lerp(anchorLerp.rotation, anchor.rotation, num * Time.deltaTime);
                    anchorLerp.position = Vector3.Lerp(anchorLerp.position, anchor.position, 15f * Time.deltaTime);

                    if (tank.IsNotCrashed())
                    {
                        root.localPosition = Vector3.Lerp(root.localPosition, Vector3.zero, 10f * Time.deltaTime);
                        UpdateGeneratedTankAnimations();
                    }

                    if (!tank.IsStateNormal() && !motor.isGrounded)
                    {
                        visualRuntime.resetRootRotationToGround = false;
                    }
                }

                missile1.SetActive(skillLoc.primary.stock > 1);
                missle2.SetActive(skillLoc.primary.stock > 0);
            }

            GroundDustEffect();
        }

        public void RotateTowardVectorDirection(Vector3 moveVector, GameObject objectPlane, GameObject objectToRotate, Vector3 lockAxises, Vector3 limitAxis, string driveAxis, float speed)
        {
            var transformToRotate = objectToRotate.transform;
            var transformPlane = objectPlane.transform;
            Vector3 forward = transformPlane.forward;
            if (driveAxis == "Up")
            {
                forward = Vector3.ProjectOnPlane(moveVector - transformToRotate.position, transformPlane.up);
            }

            if (driveAxis == "Right")
            {
                forward = Vector3.ProjectOnPlane(moveVector - transformToRotate.position, transformPlane.right);
            }

            Quaternion b = Quaternion.LookRotation(forward, transformPlane.up);
            transformToRotate.rotation = Quaternion.Lerp(transformToRotate.rotation, b, speed * Time.deltaTime);
            Quaternion localRotation = transformToRotate.localRotation;
            if (lockAxises.x == 1f)
            {
                localRotation.x = 0f;
            }

            if (lockAxises.y == 1f)
            {
                localRotation.y = 0f;
            }

            if (lockAxises.z == 1f)
            {
                localRotation.z = 0f;
            }

            transformToRotate.localRotation = localRotation;
            if (limitAxis != Vector3.zero && limitAxis.x != 0f)
            {
                float x = transformToRotate.localEulerAngles.x;
                transformToRotate.localRotation = Quaternion.Euler(Mathf.Clamp((x > 180f) ? (x - 360f) : x, 0f - limitAxis.x, 20f), 0f, 0f);
            }
        }

        public void UpdateGeneratedTankAnimations()
        {
            RotateTowardVectorDirection(turret.transform.position + inputBank.aimDirection, body, turret, new Vector3(1f, 0f, 1f), Vector3.zero, "Up", 12.5f);
            RotateTowardVectorDirection(gun.transform.position + inputBank.aimDirection, turret, gun, new Vector3(0f, 1f, 1f), new Vector3(45f, 0f, 0f), "Right", 10f);

            if (pivot != null)
            {
                RotateTowardVectorDirection(pivot.transform.position + inputBank.aimDirection, turret, pivot, new Vector3(0f, 1f, 1f), new Vector3(45f, 0f, 0f), "Right", 10f);
            }

            for (int j = 0; j < visualRuntime.beltRenders.Length; j++)
            {
                visualRuntime.beltRenders[j].offset = this.tank.forwardVelocity;
            }

            if (visualRuntime.trackLeftOffset > 1f)
            {
                visualRuntime.trackLeftOffset = 0f;
            }

            if (visualRuntime.trackRightOffset > 1f)
            {
                visualRuntime.trackRightOffset = 0f;
            }
        }

        public void GroundDustEffect()
        {
            Color color = new Color(0f, 0f, 0f, 0f);
            ParticleSystem[] systems = visualRuntime.particlesMain.FindGroupByName("TracksAndWheels").FindParticlesByName("Dust", containsNameInstead: true);

            if (Physics.Raycast(new Ray(motor.transform.position, Vector3.down), out var hitInfo, 4f, LayerIndex.world.mask | LayerIndex.water.mask, QueryTriggerInteraction.Collide))
            {
                SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitInfo.collider, hitInfo.point);
                if (objectSurfaceDef)
                    color = objectSurfaceDef.approximateColor;

                for (int i = 0; i < systems.Length; i++)
                {
                    var main = systems[i].main;
                    main.startColor = color + Color.black;

                    if (motor.isGrounded && visualRuntime.direction.moveVector.magnitude > 0f)
                        systems[i].Play();
                    else
                        systems[i].Stop();
                }
            }
            else
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    systems[i].Stop();
                }
            }
        }

        public void GroundFireTrailEffect(bool play)
        {
            ParticleSystem[] array = visualRuntime.particlesMain.FindGroupByName("TracksAndWheels").FindParticlesByName("Fire", containsNameInstead: true);
            for (int i = 0; i < array.Length; i++)
            {
                PlaySystem(array[i], play);
            }
        }

        public void GroundThrusterEffects(bool play, bool loop)
        {
            for (int i = 0; i < thrusters.Length; i++)
            {
                var child = this.childLoc.FindChild(thrusters[i]);
                if (child && child.gameObject.activeSelf)
                {
                    var system = thrusterSystems[i] ??= childLoc.FindChild(thrusters[i])?.GetComponentsInChildren<ParticleSystem>(true);
                    if (system is not null)
                    {
                        for (int j = 0; j < system.Length; j++)
                        {
                            PlaySystem(system[j], play, loop);
                        }
                    }
                }
            }
        }

        public void ThrusterFailEffect()
        {
        }

        public void MinigunOverheatEffect(float heatLevel)
        {
            overheat ??= childLoc.FindChild("MinigunBarrelsHeat").GetComponent<MeshRenderer>();
            overheat.enabled = heatLevel > 0f;

            var material = overheat.material;
            var color = material.color;
            color.a = heatLevel;

            material.SetColor("_Color", color);
        }

        public void FlameThrowerEffect(bool play)
        {
            var flames = visualRuntime.particlesMain.FindParticleByName("FlamethrowerFireEffect");
            if (flames)
            {
                if (play)
                    flames.Play();
                else
                    flames.Stop();
            }
        }

        public void PlaySystem(ParticleSystem system, bool play, bool loop)
        {
            var main = system.main;
            main.loop = loop;

            if (play != system.isPlaying)
            {
                if (play)
                    system.Play();
                else
                    system.Stop();
            }
        }

        public void PlaySystem(ParticleSystem system, bool play)
        {
            if (play != system.isPlaying)
            {
                if (play)
                    system.Play();
                else
                    system.Stop();
            }
        }

        public void JumpThrusterEffects(bool play, bool loop)
        {
            for (int i = 0; i < jumpSystems.Length; i++)
            {
                var system = jumpSystems[i] ??= childLoc.FindChild(jumpBoosters[i])?.GetComponentsInChildren<ParticleSystem>(true);
                if (system is not null)
                {
                    for (int j = 0; j < system.Length; j++)
                    {
                        PlaySystem(system[j], play, loop);
                    }
                }
            }
        }

        public void JumpThrusterEffectScale(float scale)
        {
            for (int i = 0; i < jumpSystems.Length; i++)
            {
                var system = jumpSystems[i] ??= childLoc.FindChild(jumpBoosters[i])?.GetComponentsInChildren<ParticleSystem>(true);
                if (system is not null)
                {
                    for (int j = 0; j < system.Length; j++)
                    {
                        var newScale = system[j].transform.localScale;
                        newScale.y = scale;

                        system[j].transform.localScale = newScale;
                    }
                }
            }
        }

        public void JumpBoosterEffects(bool play)
        {
            for (int i = 0; i < jumpSystems.Length; i++)
            {
                var system = jumpSystems[i] ??= childLoc.FindChild(jumpBoosters[i])?.GetComponentsInChildren<ParticleSystem>(true);
                if (system is not null)
                {
                    for (int j = 0; j < system.Length; j++)
                    {
                        PlaySystem(system[j], play, true);
                    }
                }
            }
        }

        public void JumpBoosterEffectScale(float scale)
        {
            for (int i = 0; i < jumpSystems.Length; i++)
            {
                var system = jumpSystems[i] ??= childLoc.FindChild(jumpBoosters[i])?.GetComponentsInChildren<ParticleSystem>(true);
                if (system is not null)
                {
                    for (int j = 0; j < system.Length; j++)
                    {
                        var newScale = system[j].transform.localScale;
                        newScale.y = scale;

                        system[j].transform.localScale = newScale;
                    }
                }
            }
        }

        public void JumpBoosterLid(bool openLid)
        {
            for (int i = 0; i < jumpBoosters.Length; i++)
            {
                var child = this.childLoc.FindChild(jumpBoosters[i])?.Find("BoosterHinge");
                if (child)
                {
                    var rotation = openLid ? Quaternion.Euler(0f, 0f, 30f) : Quaternion.Euler(0f, 0f, 0f);
                    child.localRotation = Quaternion.Lerp(child.localRotation, rotation, 20f * Time.deltaTime);
                }
            }
        }

        public void ThrusterEffects(bool play, bool loop, float scale, bool open)
        {
            JumpThrusterEffects(play, loop);
            JumpThrusterEffectScale(scale);
            JumpBoosterEffects(play);
            JumpBoosterEffectScale(scale);
            JumpBoosterLid(open);
        }
    }
}
